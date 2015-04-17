using System; 
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using RMS.ContractObjectModel;
using Rms.Utilities;

namespace RMS.ContractGraphModel
{
    [Serializable]
    [ProtoContract]
    class TermGraph_ObsoleteV1 : 
        DirectedGraph<TermCollection>,
        IEnumerable<INode<TermCollection>>,
        IIdentifiableINodeCollection<Subject, TermCollection>
    {
        #region Fields

        [ProtoMember(1)]
        Dictionary<Subject, TermNode_ObsoleteV1> IdentityMap;

        [ProtoMember(2)]
        HashSet<TermNode_ObsoleteV1> RootNodes;

        [ProtoMember(3)]
        HashSet<TermNode_ObsoleteV1> LeafNodes;

        [ProtoMember(4)]
        public ConcurrentDictionary<TermNode_ObsoleteV1, TermExecutionPosition_ObsoleteV1> ExecutionRegister = null;
        public ConcurrentDictionary<TermNode_ObsoleteV1, Subject> ResidualSubjectCache = null;
        public ConcurrentDictionary<TermNode_ObsoleteV1, TermAllocationPosition> Allocation = null;

        [ProtoMember(6)]
        public ConcurrentDictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocation = null;

        [ProtoMember(7)]
        GraphOperationState ExecutionState;

        static readonly Subject EMPTYSUBJECTCONSTRAINT = new Subject();

        #endregion
        
        #region Overlap Fields
        public bool IsOverlapping { private set; get; }
        private Dictionary<INode<TermCollection>, int> TopoSort = null;
        #endregion

        #region Constructors
        public TermGraph_ObsoleteV1() : base()
        {
            IdentityMap
                = new Dictionary<Subject, TermNode_ObsoleteV1>();
            RootNodes = new HashSet<TermNode_ObsoleteV1>();
            LeafNodes = new HashSet<TermNode_ObsoleteV1>();
            ExecutionState = new GraphOperationState();
            ResidualSubjectCache = new ConcurrentDictionary<TermNode_ObsoleteV1, Subject>();
            IsOverlapping = false;
        }

        public TermGraph_ObsoleteV1(TermGraph_ObsoleteV1 CopyFromThisTermGraph) : base(CopyFromThisTermGraph)
        {
            this.IdentityMap = CopyFromThisTermGraph.IdentityMap;
            this.RootNodes = CopyFromThisTermGraph.RootNodes;
            this.LeafNodes = CopyFromThisTermGraph.LeafNodes;
            this.ExecutionState = CopyFromThisTermGraph.ExecutionState;
            this.ResidualSubjectCache = CopyFromThisTermGraph.ResidualSubjectCache;
            this.IsOverlapping = CopyFromThisTermGraph.IsOverlapping;
            this.TopoSort = CopyFromThisTermGraph.TopoSort;
        }
        #endregion

        #region Graph Methods

        public bool Add(ITerm<Value> Term, Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<string, HashSet<long>> ResolvedSchedule, bool CheckForPerRisk = true)
        {
            Subject NodeIdentity = Term.GetSubject();

            bool IsAddSuccessful = true;

            // if Term is PerRisk, then explode and add

            if (CheckForPerRisk && NodeIdentity.PerRisk)
            {
                // Explode the term
                // NOTE: This call mutates the input data 'ResolvedSchedule'
                List<ITerm<Value>> PerRiskExplodedTerms = Explode(Term, CoverageIdAttrMap, ResolvedSchedule);

                // Add exploded terms
                foreach (ITerm<Value> PerRiskExplodedTerm in PerRiskExplodedTerms)
                {
                    IsAddSuccessful &= Add(PerRiskExplodedTerm, CoverageIdAttrMap, ResolvedSchedule, false);
                }

                return IsAddSuccessful;
            }

            if (NodeIdentity == null)
                NodeIdentity = EMPTYSUBJECTCONSTRAINT;

            TermCollection TermCollection = (IdentityMap.ContainsKey(NodeIdentity) ? 
                IdentityMap[NodeIdentity].GetContent() : new TermCollection(NodeIdentity));

            // Switching off the following check intentionally, in order to allow redundant terms
            //if (TermCollection.Contains(Term))
            //    throw new ArgumentException("A term node with the same identity (i.e. subject) already contains this term in its collection!");

            IsAddSuccessful = TermCollection.Add(Term);

            if (IsAddSuccessful && !IdentityMap.ContainsKey(NodeIdentity))
            {
                TermNode_ObsoleteV1 _TermNode = new TermNode_ObsoleteV1(TermCollection);

                if (NodeIdentity.PerRisk
                        && (NodeIdentity.Schedule.ScheduleSymbols.Count == 1)
                        && (ResolvedSchedule.ContainsKey(NodeIdentity.Schedule.ScheduleSymbols.First().ToString()))
                        && (ResolvedSchedule[NodeIdentity.Schedule.ScheduleSymbols.First().ToString()]
                            .Any(x => CoverageIdAttrMap.ContainsKey(x) && CoverageIdAttrMap[x].NumBuildings > 1))
                   )
                {
                    _TermNode.MarkAsMultiBuildingPerRisk();
                    NodeIdentity.NumBuildings = 0;
                    foreach (string ScheduleSymbol in NodeIdentity.Schedule.ScheduleSymbols.Select(x => x.ToString()))
                    {
                        if (ResolvedSchedule.ContainsKey(ScheduleSymbol))
                        {
                            if (CoverageIdAttrMap.ContainsKey(ResolvedSchedule[ScheduleSymbol].First()))
                                NodeIdentity.NumBuildings += CoverageIdAttrMap[ResolvedSchedule[ScheduleSymbol].First()].NumBuildings;
                        }
                    }
                }

                IsAddSuccessful &= base.Add(_TermNode);
                if (IsAddSuccessful)
                {
                    IdentityMap.Add(NodeIdentity, _TermNode);
                    // A newly added term node (i.e. with no parent or child links) is both a root and a leaf, trivially.
                    RootNodes.Add(_TermNode);
                    LeafNodes.Add(_TermNode);
                    ExecutionState.RegisterModificationInGraphTopology();
                }
            }

            return IsAddSuccessful;
        }

        // NOTE: MUTATION OF INPUT DATA 'ResolvedSchedule'
        private List<ITerm<Value>> Explode(ITerm<Value> PerRiskTerm, Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<string, HashSet<long>> ResolvedSchedule)
        {
            Subject _Subject = PerRiskTerm.GetSubject();

            List<ITerm<Value>> ExplodedTerms = new List<ITerm<Value>>();

            Dictionary<string, HashSet<long>> ExplodedSchedule
                = new Dictionary<string, HashSet<long>>();

            foreach (SymbolicValue ScheduleSymbol in _Subject.Schedule.ScheduleSymbols)
            {
                string PerRiskifiedScheduleSymbol = (ResolvedSchedule.ContainsKey(ScheduleSymbol.ToString())) ? ScheduleSymbol.ToString() : ScheduleSymbol.ToString() + ".#";
                if (ResolvedSchedule.ContainsKey(PerRiskifiedScheduleSymbol))
                {
                    HashSet<long> RITEIDs_ResolvedSchedulePerRiskifiedScheduleSymbol = ResolvedSchedule[PerRiskifiedScheduleSymbol];
                    foreach (long RiskID in RITEIDs_ResolvedSchedulePerRiskifiedScheduleSymbol)
                    {
                        if (CoverageIdAttrMap.ContainsKey(RiskID))
                        {
                            string TrimmedPerRiskifiedScheduleSymbol = PerRiskifiedScheduleSymbol.Replace(".#", "");
                            string NewScheduleSymbol = TrimmedPerRiskifiedScheduleSymbol + "." + CoverageIdAttrMap[RiskID].RITExposureId.ToString();
                            if (!ExplodedSchedule.ContainsKey(NewScheduleSymbol))
                                ExplodedSchedule.Add(NewScheduleSymbol, new HashSet<long>());
                            ExplodedSchedule[NewScheduleSymbol].Add(RiskID);
                        }
                    }
                }
            }

            foreach (string ExplodedScheduleSymbol in ExplodedSchedule.Keys)
            {
                //Copy
                ITerm<Value> ClonedTerm = (ITerm<Value>)Activator.CreateInstance(PerRiskTerm.GetType(), PerRiskTerm);

                // NOTE: MUTATION OF INPUT DATA 'ResolvedSchedule'
                // add to schedule dictionary
                if (!ResolvedSchedule.ContainsKey(ExplodedScheduleSymbol))
                    ResolvedSchedule.Add(ExplodedScheduleSymbol, new HashSet<long>(ExplodedSchedule[ExplodedScheduleSymbol]));

                ClonedTerm.HardResetSchedule(new HashSet<SymbolicValue>() { new SymbolicValue(ExplodedScheduleSymbol) }, ResolvedSchedule);

                // set PerRisk (i.e. resolution) flag to false
                // perhaps, use line below only for single building RITEs
                // ClonedTerm.HardResetResolution(false);

                // add cloned term to list
                ExplodedTerms.Add(ClonedTerm);
            }

            return ExplodedTerms;
        }

        public bool Remove(ITerm<Value> Term)
        {
            Subject NodeIdentity = Term.GetSubject();

            if (NodeIdentity == null)
                NodeIdentity = EMPTYSUBJECTCONSTRAINT;

            if (Contains(NodeIdentity))
            {
                if (IdentityMap[NodeIdentity].GetContent().Contains(Term))
                {
                    bool IsRemoveSuccessful = IdentityMap[NodeIdentity].GetContent().Remove(Term);
                    if (IsRemoveSuccessful)
                    {
                        if (IdentityMap[NodeIdentity].GetContent().Count == 0)
                        {
                            IsRemoveSuccessful &= base.Remove(IdentityMap[NodeIdentity]);
                            if (IsRemoveSuccessful)
                                IsRemoveSuccessful &= IdentityMap.Remove(NodeIdentity);
                        }
                        ExecutionState.RegisterModificationInGraphTopology();
                    }
                    return IsRemoveSuccessful;
                }
            }

            return false;
        }

        // TODO : IsRebuildSuccessful should only return FALSE on exception; redundant rebuild should return TRUE 
        // (currently, it returns FALSE on redundancy; this behaviour is incorrect)
        public bool Rebuild(ITerm<Value> Term = null)
        {
            IEnumerable<INode<TermCollection>> TermNodes = this;

            if (Term != null && IdentityMap.ContainsKey(Term.GetSubject()))
            {
                if (IdentityMap[Term.GetSubject()].GetContent().Count > 1) 
                /// the concerned term is not the first and only term in its node,
                /// in other words, this node has already been processed before
                    return true;
                TermNodes = new INode<TermCollection>[] { IdentityMap[Term.GetSubject()] };
            }

            bool IsRebuildSuccessful = true;

            #region O(m * N^2) algorithm to produce all ancestors for all nodes

            Dictionary<Subject, TermNodeWithGuid_ObsoleteV1> IdentityMapWithGuid 
                = new Dictionary<Subject, TermNodeWithGuid_ObsoleteV1>();
            
            // O(N) to construct TermNodeWithGuid_ObsoleteV1 versions for each node
            foreach (TermNode_ObsoleteV1 TermNode in this)
            {
                IdentityMapWithGuid.Add(TermNode.GetSubject(), new TermNodeWithGuid_ObsoleteV1(TermNode));
            }
            
            // O(m * N^2) to produce all ancestors for all nodes
            foreach (TermNode_ObsoleteV1 TermNode in TermNodes)
            {
                TermNodeWithGuid_ObsoleteV1 TermNodeWithGUID = IdentityMapWithGuid[TermNode.GetSubject()];
                foreach (TermNode_ObsoleteV1 OtherTermNode in TermNodes.Where(TN => !TN.Equals(TermNode)))
                {
                    if (TermNode.IsChildOf(OtherTermNode))
                        TermNodeWithGUID.AddParent(IdentityMapWithGuid[OtherTermNode.GetSubject()]);
                }
            }

            // O(m * N) to replace IdentityMap with TermNodeWithGuid_ObsoleteV1 versions
            foreach (Subject Subject in IdentityMapWithGuid.Keys)
            {
                TermNode_ObsoleteV1 TermNode = IdentityMap[Subject];
                TermNodeWithGuid_ObsoleteV1 TermNodeWithGUID = IdentityMapWithGuid[Subject];

                IdentityMap.Remove(Subject);
                Remove(TermNode);

                Add(TermNodeWithGUID);
                IdentityMap.Add(Subject, TermNodeWithGUID);

                if (RootNodes.Contains(TermNode))
                {
                    RootNodes.Remove(TermNode);
                    RootNodes.Add(TermNodeWithGUID);
                }
                if (LeafNodes.Contains(TermNode))
                {
                    LeafNodes.Remove(TermNode);
                    LeafNodes.Add(TermNodeWithGUID);
                }
            }

            #endregion

            foreach (TermNode_ObsoleteV1 TermNode in TermNodes)
            {
                HashSet<INode<TermCollection>> NoNeedToConsiderForTermNode = new HashSet<INode<TermCollection>>();

                NoNeedToConsiderForTermNode.Add(TermNode);

                foreach (TermNode_ObsoleteV1 OtherTermNode in TermNodes)
                {
                    if (NoNeedToConsiderForTermNode.Contains(OtherTermNode))
                        continue;

                    #region if (TermNode.IsChildOf(OtherTermNode))
                    if (TermNode.IsChildOf(OtherTermNode))
                    {
                        // find lowest-level parent
                        TermNode_ObsoleteV1 CurrentParentNode = OtherTermNode;
                        do
                        {
                            bool keepGoing = false;
                            foreach (TermNode_ObsoleteV1 ChildNodeOfCurrentParent in GetChildrenOfNode(CurrentParentNode).Where(TN => (TN != TermNode)))
                            {
                                if (TermNode.IsChildOf(ChildNodeOfCurrentParent))
                                {
                                    CurrentParentNode = ChildNodeOfCurrentParent;
                                    NoNeedToConsiderForTermNode.Add(CurrentParentNode);
                                    keepGoing = true;
                                    break;
                                }
                            }
                            if (!keepGoing)
                                break;
                        }
                        while (TermNode.IsChildOf(OtherTermNode));

                        //// search for and delete non-direct ancestral links
                        //foreach (TermNode ParentTermNode in GetParentsOfNode(CurrentParentNode))
                        //{
                        //    if (IsReachable(ParentTermNode, TermNode))
                        //    {
                        //        DeleteParentChildEdge(ParentTermNode, TermNode);
                        //        // RootNodes & LeafNodes update
                        //        if ((GetParentsOfNode(TermNode) == null) 
                        //            || ((GetParentsOfNode(TermNode)).Count() == 0))
                        //            RootNodes.Add(TermNode);
                        //        if ((GetChildrenOfNode(ParentTermNode) == null)
                        //            || ((GetChildrenOfNode(ParentTermNode)).Count() == 0))
                        //            LeafNodes.Add(ParentTermNode);
                        //    }
                        //}

                        // search for and delete non-direct descendant links
                        foreach (TermNode_ObsoleteV1 ChildTermNode in GetChildrenOfNode(TermNode))
                        {
                            if (IsReachable(CurrentParentNode, ChildTermNode))
                            {
                                DeleteParentChildEdge(CurrentParentNode, ChildTermNode);
                                // RootNodes & LeafNodes update
                                if ((GetParentsOfNode(ChildTermNode) == null)
                                    || ((GetParentsOfNode(ChildTermNode)).Count() == 0))
                                    RootNodes.Add(ChildTermNode);
                                if ((GetChildrenOfNode(CurrentParentNode) == null)
                                    || ((GetChildrenOfNode(CurrentParentNode)).Count() == 0))
                                    LeafNodes.Add(CurrentParentNode);
                            }
                        }

                        IsRebuildSuccessful &= MakeParentChildEdge(CurrentParentNode, TermNode);

                        // RootNodes & LeafNodes update
                        if ((GetParentsOfNode(CurrentParentNode) == null) || ((GetParentsOfNode(CurrentParentNode)).Count() == 0))
                            RootNodes.Add(CurrentParentNode);
                        RootNodes.Remove(TermNode);
                        if ((GetChildrenOfNode(TermNode) == null) || ((GetChildrenOfNode(TermNode)).Count() == 0))
                            LeafNodes.Add(TermNode);
                        LeafNodes.Remove(CurrentParentNode);
                    }
                    #endregion if (TermNode.IsChildOf(OtherTermNode))

                    #region DEPRECATED: else if (OtherTermNodeSubject.IsChildOf(TermNodeSubject))
                    //else if (OtherTermNodeSubject.IsChildOf(TermNodeSubject))
                    //{
                    //    // find lowest-level parent
                    //    TermNode CurrentParentNode = TermNode;
                    //    while (OtherTermNodeSubject.IsChildOf(CurrentParentNode.GetSubject()))
                    //    {
                    //        bool keepGoing = false;
                    //        foreach (TermNode ChildNodeOfCurrentParent in GetChildrenOfNode(CurrentParentNode).Where(TN => (TN != OtherTermNode)))
                    //        {
                    //            Subject ChildNodeOfCurrentParentSubject = ChildNodeOfCurrentParent.GetSubject();
                    //            if (OtherTermNodeSubject.IsChildOf(ChildNodeOfCurrentParentSubject))
                    //            {
                    //                CurrentParentNode = ChildNodeOfCurrentParent;
                    //                keepGoing = true;
                    //                break;
                    //            }
                    //        }
                    //        if (!keepGoing)
                    //            break;
                    //    }

                    //    // search for and delete ancestral links
                    //    foreach (TermNode ParentOtherTermNode in GetParentsOfNode(CurrentParentNode))
                    //    {
                    //        if (IsReachable(ParentOtherTermNode, OtherTermNode))
                    //        {
                    //            DeleteParentChildEdge(ParentOtherTermNode, OtherTermNode);
                    //            // RootNodes & LeafNodes update
                    //            if ((GetParentsOfNode(OtherTermNode) == null) || ((GetParentsOfNode(OtherTermNode)).Count() == 0))
                    //                RootNodes.Add(OtherTermNode);
                    //            if ((GetChildrenOfNode(ParentOtherTermNode) == null) || ((GetChildrenOfNode(ParentOtherTermNode)).Count() == 0))
                    //                LeafNodes.Add(ParentOtherTermNode);
                    //        }
                    //    }

                    //    IsRebuildSuccessful &= MakeParentChildEdge(CurrentParentNode, OtherTermNode);

                    //    // RootNodes & LeafNodes update
                    //    if ((GetParentsOfNode(CurrentParentNode) == null) || (GetParentsOfNode(CurrentParentNode).Count() == 0))
                    //        RootNodes.Add(CurrentParentNode);
                    //    RootNodes.Remove(OtherTermNode);
                    //    if ((GetChildrenOfNode(OtherTermNode) == null) || ((GetChildrenOfNode(OtherTermNode)).Count() == 0))
                    //        LeafNodes.Add(OtherTermNode);
                    //    LeafNodes.Remove(CurrentParentNode);
                    //}
                    #endregion DEPRECATED:  if (OtherTermNodeSubject.IsChildOf(TermNodeSubject))

                    // else, TermNode can ignore all descendants of OtherTermNode
                    //else
                    //{
                    //    foreach (TermNode IgnorableTermNode in GetChildrenOfNode(OtherTermNode))
                    //        NoNeedToConsiderForTermNode.Add(IgnorableTermNode);
                    //}
                }
            }

            // O(N^2) Overlap Detection + O(N) Topological Sorting
            if (ExecutionState.HasGraphBeenModifiedSinceLastOperation())
            {
                DetectOverlap();
                // O(N) Do Topological Sort, In Any Case, Regardless of Overlap!!!!
                //if (IsOverlapping)
                    PerformTopologicalSort();
            }

            PopulateResidualSubjects();

            ExecutionState.RegisterModificationInGraphTopology();

            return IsRebuildSuccessful;
        }

        public bool MakeParentChildEdge(TermNode_ObsoleteV1 parent, TermNode_ObsoleteV1 child, bool AddAnyNodesNotPresent = false)
        {
            child.AddParent(parent);
            return base.MakeParentChildEdge(parent, child, AddAnyNodesNotPresent);
        }

        public bool DeleteParentChildEdge(TermNode_ObsoleteV1 parent, TermNode_ObsoleteV1 child)
        {
            child.RemoveParent(parent);
            return base.DeleteParentChildEdge(parent, child);
        }

        private void DetectOverlap()
        {
            #region NOTE: The following code is wrong!
            // Check if any node has more than one parent
            //foreach (TermNode ANode in this)
            //{
            //    if ((GetParentsOfNode(ANode) != null) && (GetParentsOfNode(ANode).Count > 1))
            //    {
            //        IsOverlapping = true;
            //        PerformTopologicalSort();
            //        return;
            //    }
            //}
            #endregion

            foreach (TermNode_ObsoleteV1 ANode in this)
            {
                foreach (TermNode_ObsoleteV1 AnotherNode in this.Where(N => (N != ANode)))
                {
                    if (ANode.OverlapsWithoutInclusion(AnotherNode))
                    {
                        IsOverlapping = true;
                        return;
                    }
                }
            }

            //// Check if any leaf nodes overlap
            //foreach (TermNode LeafNode in LeafNodes)
            //{
            //    foreach (TermNode OtherLeafNode in LeafNodes.Where(LN => (LN != LeafNode)))
            //    {
            //        if (LeafNode.Overlaps(OtherLeafNode))
            //        {
            //            IsOverlapping = true;
            //            PerformTopologicalSort();
            //            return;
            //        }
            //    }
            //}
        }

        private void PerformTopologicalSort()
        {
            TopoSort = new Dictionary<INode<TermCollection>, int>();
            // Initialize every node to -1
            foreach (TermNode_ObsoleteV1 ANode in this)
            {
                TopoSort.Add(ANode, -1);
            }
            // Set RootNodes to 0 and recurse
            foreach (TermNode_ObsoleteV1 RootNode in RootNodes)
            {
                TopoSort.Remove(RootNode);
                TopoSort.Add(RootNode, 0);
                RecursiveTopologicalSort(RootNode, TopoSort[RootNode]);
            }
        }

        private void RecursiveTopologicalSort(TermNode_ObsoleteV1 ANode, int ANodeTopoVal)
        {
            foreach (TermNode_ObsoleteV1 AChildNode in GetChildrenOfNode(ANode))
            {
                int CurTopoVal = TopoSort[AChildNode];
                TopoSort.Remove(AChildNode);
                TopoSort.Add(AChildNode, Math.Max(ANodeTopoVal + 1, CurTopoVal));
                RecursiveTopologicalSort(AChildNode, TopoSort[AChildNode]);
            }
        }

        #endregion

        #region API
        public void Execute(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<TermNode_ObsoleteV1, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings,
            bool RedoEvenIfCached = true)
        {
            if (IdentityMap.Count == 0)
            {
                // Update execution state
                ExecutionState = new GraphOperationState(false,
                    new Dictionary<string, HashSet<long>>(Schedule),
                    new Dictionary<long, Loss>(CoverageIdGULossMap));
                // ALLOCATE
                this.Allocate(Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
                return;
            }

            if (!RedoEvenIfCached)
            {
                if(!ExecutionState.HasOperationStateChanged(Schedule, CoverageIdGULossMap))
                    return;
            }
            else
            {
                // DISABLING RESET (TERM NODES NO LONGER HAVE STATE)
                //// Reset all nodes
                //foreach (TermNode _TermNode in IdentityMap.Values)
                //{
                //    _TermNode.Reset();
                //}

                // DISABLING OVERLAP DETECTION PER EVENT
                //// Detect overlap ONLY IF graph has changed (in the future: detect overlap even if graph has not changed, since GU can render overlap vacuous
                //if (ExecutionState.HasGraphBeenModifiedSinceLastOperation())
                //    DetectOverlap();

                if (IsOverlapping)
                {
                    ExecuteAssumingOverlap(Schedule, CoverageIdAttrMap,
                            CoverageIdGULossMap,
                            RCVCoveredAndAffectedBySubject, Bindings);
                    return;
                }
            }

            ExecutionRegister = new ConcurrentDictionary<TermNode_ObsoleteV1, TermExecutionPosition_ObsoleteV1>();
            Allocation = new ConcurrentDictionary<TermNode_ObsoleteV1, TermAllocationPosition>();

            #region OLD APPROACH : arbitrary multi-threading
            //// Go through each node: (a) try to register for execution; (b) if successful, execute
            //HashSet<Task> TermNodeTasks = new HashSet<Task>();
            //foreach (TermNode _RootNode in RootNodes)
            //{
            //    Task<TermExecutionPosition> _TermNodeExecutionTask = ExecutionRegister.GetOrAdd(_RootNode, 
            //        new Task<TermExecutionPosition>(
            //            () => ExecuteTermNode(_RootNode, Schedule, CoverageIdAttrMap,
            //                    CoverageIdGULossMap, RCVCoveredAndAffectedBySubject, Bindings).Result));
            //    _TermNodeExecutionTask.Start();
            //    TermNodeTasks.Add(_TermNodeExecutionTask);
            //    #if DEBUG
            //        // SINGLETHREADED
            //        _TermNodeExecutionTask.Wait();
            //    #endif
            //}

            //#if !DEBUG
            //    // Wait for all tasks to complete
            //    Task.WaitAll(TermNodeTasks.ToArray<Task>());
            //#endif
            #endregion

            #region NEW APPROACH : Bottom-up multi-threading on topo-sort
            //// Find deepest (i.e. highest) level
            //int level = TopoSort.Values.Max();

            //while (level >= 0)
            //{
            //    IEnumerable<INode<TermCollection>> TermNodesForLevel =
            //        this.Where(t => (TopoSort[t] == level));
            //    Parallel.ForEach(TermNodesForLevel, ANode =>
            //        {
            //            TermNode TermNode = (TermNode)ANode;
            //            Task<TermExecutionPosition> _TermNodeExecutionTask
            //                = ExecutionRegister.GetOrAdd(TermNode,
            //                    Task.Factory.StartNew(
            //                        () => ExecuteTermNode(TermNode, Schedule, CoverageIdAttrMap,
            //                                CoverageIdGULossMap, RCVCoveredAndAffectedBySubject,
            //                                Bindings)));
            //        }
            //    );

            //    level--;
            //}

            ////MULTI-THREADED
            //Parallel.ForEach(RootNodes, RootNode =>
            //        {
            //            Task<TermExecutionPosition> _TermNodeExecutionTask
            //                = ExecutionRegister.GetOrAdd(RootNode,
            //                    Task.Factory.StartNew(
            //                        () => ExecuteTermNode(RootNode, Schedule, CoverageIdAttrMap,
            //                                CoverageIdGULossMap, RCVCoveredAndAffectedBySubject,
            //                                Bindings)));
            //        }
            //    );

            ////SINGLE-THREADED
            foreach (TermNode_ObsoleteV1 RootNode in RootNodes)
            {
                ExecutionRegister.GetOrAdd(RootNode,
                                                ExecuteTermNode(RootNode, Schedule, CoverageIdAttrMap,
                                                    CoverageIdGULossMap, RCVCoveredAndAffectedBySubject,
                                                    Bindings)
                                                );
            }
            #endregion

            // Update execution state
            ExecutionState = new GraphOperationState(false, new Dictionary<string, HashSet<long>>(Schedule), new Dictionary<long, Loss>(CoverageIdGULossMap));

            // ALLOCATE
            this.Allocate(Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
        }

        /// <summary>
        /// Execute node; recurse over children, if any.
        /// </summary>
        /// <param name="_TermNode"></param>
        /// <param name="Schedule"></param>
        /// <param name="CoverageIdGULossMap"></param>
        /// <returns></returns>
        private TermExecutionPosition_ObsoleteV1 ExecuteTermNode(TermNode_ObsoleteV1 _TermNode,
            Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<TermNode_ObsoleteV1, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings)
        {
            // A. Go through each child node: (a) try to register for execution; (b) if successful, execute

            #region OLD APPROACH : arbitrary multi-threading
            //HashSet<Task<TermExecutionPosition>> ChildTermNodeTasks = new HashSet<Task<TermExecutionPosition>>();
            //foreach (TermNode _ChildTermNode in GetChildrenOfNode(_TermNode))
            //{
            //    if (!ExecutionRegister.ContainsKey(_ChildTermNode))
            //    {
            //        Task<TermExecutionPosition> _ChildTermNodeExecutionTask =
            //            new Task<TermExecutionPosition>(
            //                () => ExecuteTermNode(_ChildTermNode, Schedule, CoverageIdValueMap,
            //                            CoverageIdExposureTypeMap, CoverageIdNumBuildingsMap,
            //                            CoverageIdGULossMap, RCVCoveredAndAffectedBySubject, Bindings).Result
            //                                /*TODO : , TaskCreationOptions.AttachedToParent*/);
            //        if (ExecutionRegister.GetOrAdd(_ChildTermNode, _ChildTermNodeExecutionTask)
            //                == _ChildTermNodeExecutionTask)
            //            _ChildTermNodeExecutionTask.Start();
            //    }
            //    ChildTermNodeTasks.Add(ExecutionRegister[_ChildTermNode]);
            //    #if DEBUG
            //        // SINGLETHREADED
            //        ExecutionRegister[_ChildTermNode].Wait();
            //    #endif
            //}
            #endregion

            ICollection<INode<TermCollection>> ChildTermNodes = GetChildrenOfNode(_TermNode);

            ////MULTI-THREADED
            //Parallel.ForEach(ChildTermNodes, _ChildNode =>
            //    {
            //        TermNode _ChildTermNode = (TermNode)_ChildNode;
            //        Task<TermExecutionPosition> _TermNodeExecutionTask
            //            = ExecutionRegister.GetOrAdd(_ChildTermNode,
            //                Task.Factory.StartNew(
            //                    () => ExecuteTermNode(_ChildTermNode, Schedule, CoverageIdAttrMap,
            //                            CoverageIdGULossMap, RCVCoveredAndAffectedBySubject,
            //                            Bindings)));
            //    }
            //);

            //SINGLE-THREADED
            foreach (TermNode_ObsoleteV1 _ChildTermNode in ChildTermNodes)
            {
                TermExecutionPosition_ObsoleteV1 _TermNodeExecutionTask
                    = ExecutionRegister.GetOrAdd(_ChildTermNode,
                                                    ExecuteTermNode(_ChildTermNode, Schedule, CoverageIdAttrMap,
                                                        CoverageIdGULossMap, RCVCoveredAndAffectedBySubject,
                                                        Bindings)
                                                 );
            }

            // B. Construct SubjectPosition for this _TermNode from ResultPosition(s) of child nodes and residual subject

            List<TermExecutionPosition_ObsoleteV1> SubjectPosition
                = new List<TermExecutionPosition_ObsoleteV1>(ChildTermNodes.Count + 1);

            // C.1 Get (or, construct, once) residual subject position & add

            Subject ResidualSubject = ResidualSubjectCache[_TermNode];

            TermExecutionPosition_ObsoleteV1 ResidualSubjectPosition = 
                GetResidualSubjectPosition(ResidualSubject,
                    CoverageIdAttrMap,
                    CoverageIdGULossMap,
                    _TermNode.IsMultiBuildingPerRisk());

            SubjectPosition.Add(ResidualSubjectPosition);

            // D.1 Prepare bindings

            Dictionary<SimpleExpression<SymbolicValue>, double> BindingsForNode =
                        GetBindingsForNode(_TermNode, RCVCoveredAndAffectedBySubject, Bindings);

            // D.2 Wait for all child tasks to complete; then add to SubjectPosition

            foreach (TermNode_ObsoleteV1 _ChildTermNode in ChildTermNodes)
            {
                SubjectPosition.Add(ExecutionRegister[_ChildTermNode]);
            }

            // E. Execute this node

            return _TermNode.Execute(SubjectPosition, BindingsForNode);
        }

        private void Allocate(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap)
        {
            if (ExecutionState.HasOperationStateChanged())
                throw new Exception("Term Graph not yet executed or has changed since last execution!");

            RITEAllocation = new ConcurrentDictionary<long, Dictionary<string,TermAllocationPosition>>();

            // Allocate all root nodes
            foreach (TermNode_ObsoleteV1 RootNode in RootNodes)
            {
                // Coalesce (applicable to per-risk multi-buidling roots)
                ExecutionRegister[RootNode].Coalesce();

                // Allocate root nodes trivially
                TermAllocationPosition AllocationPosition =
                    new TermAllocationPosition((ExecutionRegister[RootNode].S - ExecutionRegister[RootNode].D - ExecutionRegister[RootNode].X),
                        ExecutionRegister[RootNode].D);

                if (!Allocation.ContainsKey(RootNode))
                    Allocation.TryAdd(RootNode, AllocationPosition);

                AllocateSubtree(RootNode, AllocationPosition, Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
            }

            // Allocate residual subject (i.e. all subject loss position RITEs not yet allocated) trivially
            foreach (string ScheduleSymbol in Schedule.Keys)
            {
                foreach (long RITEId in Schedule[ScheduleSymbol])
                {
                    if (!CoverageIdGULossMap.ContainsKey(RITEId))
                        continue;
                    if (!RITEAllocation.ContainsKey(RITEId))
                    {
                        Loss GULossForRITEID = CoverageIdGULossMap[RITEId];
                        GULossForRITEID.NumBuildings = CoverageIdAttrMap[RITEId].NumBuildings;
                        foreach (SymbolicValue COL in GULossForRITEID.AmountByCOL.Keys)
                        {
                            if (!RITEAllocation.ContainsKey(RITEId))
                                RITEAllocation.TryAdd(RITEId, new Dictionary<string, TermAllocationPosition>());
                            RITEAllocation[RITEId].Add(COL.ToString(), new TermAllocationPosition(Loss.WeightedSum(GULossForRITEID.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings), 0.0));
                        }
                    }
                }
            }
        }

        private void AllocateSubtree(TermNode_ObsoleteV1 RootNode, TermAllocationPosition AllocationPosition,
            Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap)
        {
            // A. Get term node children

            ICollection<INode<TermCollection>> Children = GetChildrenOfNode(RootNode);

            // B. Get (or, construct, once) residual subject

            Subject ResidualSubject = ResidualSubjectCache[RootNode];

            // Get all RITEs that constitute this term's residual subject
            
            HashSet<long> RITEs =
                GetResidualSubjectRITEs(ResidualSubject, CoverageIdAttrMap, CoverageIdGULossMap);

            // If term has no children (either, nodes/terms of residual/RITES), then return

            if ((Children == null) || (Children.Count == 0) && (RITEs.Count == 0))
                return;

            // Coalesce children (applicable to per-risk multi-buidling nodes)
            foreach (TermNode_ObsoleteV1 Child in Children)
                ExecutionRegister[Child].Coalesce();

            double SumD =
                Children.Aggregate(0.0, (accumulator, it) => accumulator + ExecutionRegister[(TermNode_ObsoleteV1)it].D);

            double DeltaD =
                AllocationPosition.D - SumD;

            double SumR =
                Children.Aggregate(0.0, (accumulator, it) => accumulator +
                                (ExecutionRegister[(TermNode_ObsoleteV1)it].S - ExecutionRegister[(TermNode_ObsoleteV1)it].D - ExecutionRegister[(TermNode_ObsoleteV1)it].X))
                    +
                RITEs.Aggregate(0.0, (accumulator, it) => 
                                            accumulator + 
                                            CoverageIdGULossMap[it].AmountByCOL
                                                                   .Where(x => ResidualSubject.CausesOfLoss.Contains(x.Key))
                                                                   .Select(x => x.Value.Sum())
                                                                   .Sum()
                               );
            
            
            double RootNodeR = Allocation[RootNode].R;

            // C.1 Compute R_ for term children & allocate D

            Dictionary<TermNode_ObsoleteV1, double> R_ForTermChildren = new Dictionary<TermNode_ObsoleteV1, double>(Children.Count);

            foreach (TermNode_ObsoleteV1 ChildNode in Children)
            {
                //TermAllocationPosition AllocationPosition = new TermAllocationPosition();
                if (!Allocation.ContainsKey(ChildNode))
                    Allocation.TryAdd(ChildNode, new TermAllocationPosition());

                TermAllocationPosition ChildNodeAllocationPosition = Allocation[ChildNode];

                TermExecutionPosition_ObsoleteV1 ChildNodeExecutionPosition = ExecutionRegister[ChildNode];

                double R = ChildNodeExecutionPosition.S - ChildNodeExecutionPosition.X - ChildNodeExecutionPosition.D;

                double R_ = 0.0;

                if (DeltaD >= 0.0)
                {
                    R_ = R - DeltaD * (R / SumR);
                    ChildNodeAllocationPosition.D = ChildNodeExecutionPosition.S - ChildNodeExecutionPosition.X - R_;
                }
                else
                {
                    ChildNodeAllocationPosition.D = ChildNodeExecutionPosition.D + (ChildNodeExecutionPosition.D / SumD) * DeltaD;
                    R_ = ChildNodeExecutionPosition.S - ChildNodeExecutionPosition.X - ChildNodeAllocationPosition.D;
                }

                R_ForTermChildren.Add(ChildNode, R_);
            }

            // C.2 Compute R_ for RITE children

            Dictionary<long, Dictionary<string, double>> R_ForRITEChildren = new Dictionary<long, Dictionary<string, double>>(RITEs.Count);

            foreach (long RITEId in RITEs)
            {
                Loss GULossForRITEId = CoverageIdGULossMap[RITEId];

                foreach (SymbolicValue COL in ResidualSubject.CausesOfLoss)
                {
                    if (!GULossForRITEId.AmountByCOL.ContainsKey(COL))
                        continue;
                    if (!R_ForRITEChildren.ContainsKey(RITEId))
                        R_ForRITEChildren.Add(RITEId, new Dictionary<string, double>());
                    R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                }
            }

            // D. Modify R_, optionally for children

            double RSum = R_ForTermChildren.Values.Sum() + R_ForRITEChildren.Values.Select(x => x.Values.Sum()).Sum();

            if ((DeltaD >= 0.0) && (RSum == 0.0))
            {
                foreach (TermNode_ObsoleteV1 ChildNode in Children)
                {
                    TermExecutionPosition_ObsoleteV1 ChildNodeExecutionPosition = ExecutionRegister[ChildNode];

                    double R = ChildNodeExecutionPosition.S - ChildNodeExecutionPosition.X - ChildNodeExecutionPosition.D;

                    R_ForTermChildren[ChildNode] = R;
                }
                foreach (long RITEId in RITEs)
                {
                    Loss GULossForRITEId = CoverageIdGULossMap[RITEId];

                    foreach (SymbolicValue COL in GULossForRITEId.AmountByCOL.Keys)
                        if (!R_ForRITEChildren[RITEId].ContainsKey(COL.ToString()))
                            R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                        else
                            R_ForRITEChildren[RITEId][COL.ToString()] = Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings);
                }
            }

            RSum = R_ForTermChildren.Values.Sum() + R_ForRITEChildren.Values.Select(x => x.Values.Sum()).Sum();

            // E. Allocate R

            foreach (TermNode_ObsoleteV1 ChildNode in Children)
            {
                TermAllocationPosition ChildNodeAllocationPosition = Allocation[ChildNode];

                ChildNodeAllocationPosition.R = (RootNodeR == 0.0) ? 0.0 : RootNodeR * (R_ForTermChildren[ChildNode] / RSum);
            }

            foreach (long RITEId in RITEs)
            {
                if (!RITEAllocation.ContainsKey(RITEId))
                    RITEAllocation.TryAdd(RITEId, new Dictionary<string, TermAllocationPosition>());
                Dictionary<string, double> R_ForRITEChildren_RITEId = R_ForRITEChildren[RITEId];
                foreach (string COL in R_ForRITEChildren_RITEId.Keys)
                {
                    if (!RITEAllocation[RITEId].ContainsKey(COL))
                        RITEAllocation[RITEId].Add(COL, new TermAllocationPosition(((RootNodeR == 0.0) ? 0.0 : RootNodeR * R_ForRITEChildren_RITEId[COL] / RSum), 0.0));
                }
            }

            // F. Recursively, allocate children

            foreach (TermNode_ObsoleteV1 ChildNode in Children)
            {
                AllocateSubtree((TermNode_ObsoleteV1)ChildNode, Allocation[(TermNode_ObsoleteV1)ChildNode], Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
            }
        }

        public void PopulateResidualSubjects()
        {
            foreach (TermNode_ObsoleteV1 _TermNode in this)
            {
                ResidualSubjectCache.GetOrAdd((TermNode_ObsoleteV1)_TermNode, GetResidualSubject((TermNode_ObsoleteV1)_TermNode));
            }
        }

        private Subject GetResidualSubject(TermNode_ObsoleteV1 TermNode)
        {
            Subject ResidualSubject = TermNode.GetSubject();

            foreach (TermNode_ObsoleteV1 ChildTermNode in GetChildrenOfNode(TermNode))
            {
                ResidualSubject -= ChildTermNode.GetSubject();
            }

            return ResidualSubject;
        }

        private TermExecutionPosition_ObsoleteV1 GetResidualSubjectPosition(Subject ResidualSubject,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap,
            bool IsMultiBuildingPerRisk)
        {
            TermExecutionPosition_ObsoleteV1 ResidualSubjectPosition = new TermExecutionPosition_ObsoleteV1();

            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> ResidualSubjectComponents2
                = ResidualSubject.GetComponents();

            foreach (SymbolicValue Component2COL in ResidualSubjectComponents2.Keys)
            {
                foreach (int Component2ResolvedExposureType in ResidualSubjectComponents2[Component2COL].Keys)
                {
                    HashSet<long> Component2RITEIds =
                        ResidualSubjectComponents2[Component2COL][Component2ResolvedExposureType].Item1;

                    foreach (long Component2RITEId in Component2RITEIds)
                    {
                        if (!CoverageIdGULossMap.ContainsKey(Component2RITEId))
                            continue;

                        Loss loss = CoverageIdGULossMap[Component2RITEId];

                        if (IsMultiBuildingPerRisk)
                        {
                            List<TermExecutionPosition_ObsoleteV1.Component> Components = new List<TermExecutionPosition_ObsoleteV1.Component>();
                            if (loss.AmountByCOL.ContainsKey(Component2COL))
                            {
                                List<double> Losses_AmountByCOL_Component2COL = loss.AmountByCOL[Component2COL];
                                foreach (double LossAmountByCOL_ComponentS in Losses_AmountByCOL_Component2COL)
                                    Components.Add(new TermExecutionPosition_ObsoleteV1.Component(LossAmountByCOL_ComponentS, 0.0, 0.0));
                            }
                            ResidualSubjectPosition += new TermExecutionPosition_ObsoleteV1(Components);
                        }
                        else
                        {
                            ResidualSubjectPosition.Coalesce();
                            if (loss.AmountByCOL.ContainsKey(Component2COL))
                            {
                                ResidualSubjectPosition.S +=
                                    Loss.WeightedSum(loss.AmountByCOL[Component2COL], CoverageIdAttrMap[Component2RITEId].NumBuildings);
                            }
                        }
                    }
                }
            }

            return ResidualSubjectPosition;
        }

        private HashSet<long> GetResidualSubjectRITEs(Subject ResidualSubject,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap)
        {
            HashSet<long> RITEs = new HashSet<long>();

            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> ResidualSubjectComponents2
                = ResidualSubject.GetComponents();

            foreach (SymbolicValue Component2COL in ResidualSubjectComponents2.Keys)
            {
                foreach (int Component2ResolvedExposureType in ResidualSubjectComponents2[Component2COL].Keys)
                {
                    HashSet<long> Component2RITEIds =
                        ResidualSubjectComponents2[Component2COL][Component2ResolvedExposureType].Item1;

                    foreach (long Component2RITEId in Component2RITEIds)
                    {
                        if (!CoverageIdGULossMap.ContainsKey(Component2RITEId))
                            continue;

                        if (!ResidualSubject.CausesOfLoss.ContainsAny(CoverageIdGULossMap[Component2RITEId].CausesOfLoss))
                            continue;

                        RITEs.Add(Component2RITEId);
                    }
                }
            }

            return RITEs;
        }

        private Dictionary<SimpleExpression<SymbolicValue>, double> GetBindingsForNode(TermNode_ObsoleteV1 _TermNode,
            Dictionary<TermNode_ObsoleteV1, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings)
        {
            // copy Bindings
            Dictionary<SimpleExpression<SymbolicValue>, double> BindingsForNode
                = new Dictionary<SimpleExpression<SymbolicValue>, double>(Bindings.Count);
            foreach (KeyValuePair<SimpleExpression<SymbolicValue>, double> binding in Bindings)
                BindingsForNode.Add(binding.Key, binding.Value);

            // add RCVCovered and RCVAffected to Bindings
            BindingsForNode.Add(new SymbolicExpression("RCVCovered"),
                RCVCoveredAndAffectedBySubject[_TermNode].Item1);
            BindingsForNode.Add(new SymbolicExpression("RCVAffected"),
                RCVCoveredAndAffectedBySubject[_TermNode].Item2);

            return BindingsForNode;
        }


        private void ExecuteAssumingOverlap(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<TermNode_ObsoleteV1, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings)
        {
            #region Initialize execution & allocation positions for RITEs
            ConcurrentDictionary<long, Dictionary<string, TermExecutionPosition_ObsoleteV1>> RITEExecutionPosition 
                = new ConcurrentDictionary<long, Dictionary<string, TermExecutionPosition_ObsoleteV1>>();
            ConcurrentDictionary<long, Dictionary<string, TermAllocationPositionVector>> RITEAllocationPositionVector 
                = new ConcurrentDictionary<long, Dictionary<string, TermAllocationPositionVector>>();

            foreach (long RITEId in CoverageIdGULossMap.Keys)
            {
                if (!RITEExecutionPosition.ContainsKey(RITEId))
                    RITEExecutionPosition.TryAdd(RITEId, new Dictionary<string, TermExecutionPosition_ObsoleteV1>());
                foreach (SymbolicValue COL in CoverageIdGULossMap[RITEId].AmountByCOL.Keys)
                {
                    List<double> SampledLosses = CoverageIdGULossMap[RITEId].AmountByCOL[COL];
                    List<TermExecutionPosition_ObsoleteV1.Component> Components = new List<TermExecutionPosition_ObsoleteV1.Component>(SampledLosses.Count);
                    foreach (double SampledLoss in SampledLosses)
                    {
                        Components.Add(new TermExecutionPosition_ObsoleteV1.Component(SampledLoss, 0.0, 0.0));
                    }
                    RITEExecutionPosition[RITEId].Add(COL.ToString(), new TermExecutionPosition_ObsoleteV1(Components, CoverageIdAttrMap[RITEId].NumBuildings));
                }
            }

            foreach (long RITEId in RITEExecutionPosition.Keys)
            {
                if (!RITEAllocationPositionVector.ContainsKey(RITEId))
                    RITEAllocationPositionVector.TryAdd(RITEId, new Dictionary<string, TermAllocationPositionVector>());
                foreach (string COL in RITEExecutionPosition[RITEId].Keys)
                {
                    List<TermAllocationPositionVector.Component> Components
                        = new List<TermAllocationPositionVector.Component>(RITEExecutionPosition[RITEId][COL].components.Count);
                    foreach (TermExecutionPosition_ObsoleteV1.Component TermExecutionPositionComponent in RITEExecutionPosition[RITEId][COL].components)
                    {
                        Components.Add(new TermAllocationPositionVector.Component(TermExecutionPositionComponent.S, 0.0));
                    }
                    RITEAllocationPositionVector[RITEId].Add(COL.ToString(), new TermAllocationPositionVector(Components, CoverageIdAttrMap[RITEId].NumBuildings));
                }
            }
            #endregion

            // Find deepest (i.e. highest) level
            int level = TopoSort.Values.Max();

            while (level >= 0)
            {
                #region A. Initialize RITE allocation and execution position maps for level

                Dictionary<long, Dictionary<string, TermAllocationPositionVector>> RITEAllocationForLevel
                    = new Dictionary<long, Dictionary<string, TermAllocationPositionVector>>();

                #endregion

                #region B. Iterate through all nodes in the level

                foreach (TermNode_ObsoleteV1 ANode in this.Where(t => (TopoSort[t] == level)))
                {
                    #region B-1. Compute Subject Position for term from RITE Execution Positions of subject RITEs

                    TermExecutionPosition_ObsoleteV1 SubjectPosition = new TermExecutionPosition_ObsoleteV1();

                    Subject ANodeSubject = ANode.GetSubject();

                    HashSet<long> SubjectRITEs = new HashSet<long>();

                    HashSet<SymbolicValue> SubjectCOLs = new HashSet<SymbolicValue>();

                    Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> ANodeSubjectComponents2 
                        = ANodeSubject.GetComponents();

                    foreach (SymbolicValue Component2COL in ANodeSubjectComponents2.Keys)
                    {
                        foreach (int Component2ResolvedExposureType in ANodeSubjectComponents2[Component2COL].Keys)
                        {
                            HashSet<long> Component2RITEIds =
                                ANodeSubjectComponents2[Component2COL][Component2ResolvedExposureType].Item1;

                            foreach (long Component2RITEId in Component2RITEIds)
                            {
                                if (!RITEExecutionPosition.ContainsKey(Component2RITEId))
                                    continue;

                                if (!RITEExecutionPosition[Component2RITEId].ContainsKey(Component2COL.ToString()))
                                    continue;

                                // TODO: verify this is unnecessary
                                //if (!ANodeSubject.ResolvedExposureTypes.Contains(CoverageIdExposureTypeMap[Component.Item2]))
                                //    continue;

                                SubjectRITEs.Add(Component2RITEId);

                                SubjectCOLs.Add(Component2COL);

                                if (ANode.IsMultiBuildingPerRisk())
                                    SubjectPosition += new TermExecutionPosition_ObsoleteV1(RITEExecutionPosition[Component2RITEId][Component2COL.ToString()]);
                                else
                                {
                                    SubjectPosition.Coalesce(1); // TODO: is this unnecessary?
                                    TermExecutionPosition_ObsoleteV1 CoalescedSubjectRITEExecutionPosition =
                                        new TermExecutionPosition_ObsoleteV1(RITEExecutionPosition[Component2RITEId][Component2COL.ToString()]);
                                    CoalescedSubjectRITEExecutionPosition.Coalesce();
                                    SubjectPosition += CoalescedSubjectRITEExecutionPosition;
                                    SubjectPosition.Coalesce(1);
                                    // reset num buildings?
                                }
                            }
                        }
                    }

                    #endregion

                    #region B-2. Execute Term Node
                    Dictionary<SimpleExpression<SymbolicValue>, double> BindingsForNode =
                        GetBindingsForNode(ANode, RCVCoveredAndAffectedBySubject, Bindings);

                    if (!ANode.IsMultiBuildingPerRisk())
                        SubjectPosition.NumBuildings = 1;

                    TermExecutionPosition_ObsoleteV1 ANodeResultPosition =
                        ANode.Execute(new List<TermExecutionPosition_ObsoleteV1> { SubjectPosition }, BindingsForNode);
                    #endregion

                    #region B-3. Allocate to RITEs (for this level)

                    List<TermAllocationPositionVector.Component> ANodeAllocationPositionComponents =
                        new List<TermAllocationPositionVector.Component>(ANodeResultPosition.components.Count);

                    foreach (TermExecutionPosition_ObsoleteV1.Component TermExecutionPositionComponent in ANodeResultPosition.components)
                    {
                        ANodeAllocationPositionComponents.Add(new TermAllocationPositionVector.Component((TermExecutionPositionComponent.S - TermExecutionPositionComponent.D - TermExecutionPositionComponent.X), 
                            TermExecutionPositionComponent.D));
                    }

                    TermAllocationPositionVector ANodeAllocationPosition =
                        new TermAllocationPositionVector(ANodeAllocationPositionComponents, ANodeResultPosition.NumBuildings);


                    // Temporary cache of RITE allocations
                    Dictionary<long, Dictionary<string, TermAllocationPositionVector>> ChildRITEAllocationPosition
                        = new Dictionary<long, Dictionary<string, TermAllocationPositionVector>>();

                    if (ANodeAllocationPosition.components.Count == 1) // Singleton output
                    {
                        #region Singleton CASE (summed term)

                        Dictionary<long, Dictionary<string, double>> R_ForRITEChildren
                            = new Dictionary<long, Dictionary<string, double>>();

                        TermAllocationPositionVector ANodeAllocationPositionCoalesion =
                            new TermAllocationPositionVector(ANodeAllocationPosition);
                        ANodeAllocationPositionCoalesion.Coalesce();

                        double SumD = 0.0;
                        double SumR = 0.0;

                        foreach (long RITEId in SubjectRITEs)
                        {
                            foreach (string COL in SubjectCOLs.Select(x => x.ToString()))
                            {
                                TermAllocationPositionVector ExistingRITEAllocationPositionCoalescion =
                                    new TermAllocationPositionVector(RITEAllocationPositionVector[RITEId][COL]);
                                ExistingRITEAllocationPositionCoalescion.Coalesce();

                                SumD += ExistingRITEAllocationPositionCoalescion.D;

                                SumR += ExistingRITEAllocationPositionCoalescion.R;
                            }
                        }

                        double DeltaD =
                            ANodeAllocationPositionCoalesion.D - SumD;

                        double RootNodeR = ANodeAllocationPositionCoalesion.R;

                        #region Compute R_ for RITE children

                        foreach (long RITEId in SubjectRITEs)
                        {
                            if (!ChildRITEAllocationPosition.ContainsKey(RITEId))
                                ChildRITEAllocationPosition.Add(RITEId, new Dictionary<string, TermAllocationPositionVector>());

                            if (!R_ForRITEChildren.ContainsKey(RITEId))
                                R_ForRITEChildren.Add(RITEId, new Dictionary<string, double>());

                            foreach (string COL in SubjectCOLs.Select(x => x.ToString()))
                            {
                                TermExecutionPosition_ObsoleteV1 RITEExecutionPositionForRITEIdForCOL_Copy
                                    = new TermExecutionPosition_ObsoleteV1(RITEExecutionPosition[RITEId][COL]);

                                List<TermAllocationPositionVector.Component> TermAllocationPositionVectorComponents =
                                    new List<TermAllocationPositionVector.Component>(RITEExecutionPositionForRITEIdForCOL_Copy.components.Count);

                                foreach (TermExecutionPosition_ObsoleteV1.Component RITEExecutionPositionComponent in RITEExecutionPositionForRITEIdForCOL_Copy.components)
                                {
                                    double R = RITEExecutionPositionComponent.S - RITEExecutionPositionComponent.X - RITEExecutionPositionComponent.D;

                                    double R_ = 0.0; double D_ = 0.0;

                                    if (DeltaD >= 0.0 && SumR > 0)
                                    {
                                        R_ = R - DeltaD * (R / SumR);
                                        D_ = RITEExecutionPositionComponent.S - RITEExecutionPositionComponent.X - R_;
                                    }
                                    else if (DeltaD >= 0 && SumR == 0)
                                    {
                                        R_ = 0;
                                        D_ = RITEExecutionPositionComponent.S - RITEExecutionPositionComponent.X - R_;
                                    }
                                    else
                                    {
                                        D_ = RITEExecutionPositionComponent.D + (RITEExecutionPositionComponent.D / SumD) * DeltaD;
                                        R_ = RITEExecutionPositionComponent.S - RITEExecutionPositionComponent.X - D_;
                                    }

                                    TermAllocationPositionVectorComponents.Add(new TermAllocationPositionVector.Component(R_, D_));
                                }

                                ChildRITEAllocationPosition[RITEId].Add(COL, new TermAllocationPositionVector(TermAllocationPositionVectorComponents, RITEExecutionPositionForRITEIdForCOL_Copy.NumBuildings));

                                TermAllocationPositionVector TermAllocationPositionVectorComponentsCoalecsion =
                                    new TermAllocationPositionVector(TermAllocationPositionVectorComponents, RITEExecutionPosition[RITEId][COL].NumBuildings); // TODO : use RITEExecutionPositionForRITEIdForCOL_Copy.NumBuildings instead?
                                TermAllocationPositionVectorComponentsCoalecsion.Coalesce();

                                R_ForRITEChildren[RITEId].Add(COL, TermAllocationPositionVectorComponentsCoalecsion.R);
                            }
                        }

                        #endregion

                        #region Modify R_, optionally, for children

                        double RSum = R_ForRITEChildren.Values.Select(x => x.Values.Sum()).Sum();

                        if ((DeltaD >= 0.0) && (RSum == 0.0))
                        {
                            foreach (long RITEId in SubjectRITEs)
                            {
                                foreach (string COL in SubjectCOLs.Select(x => x.ToString()))
                                {
                                    TermExecutionPosition_ObsoleteV1 RITEExecutionPositionForRITEIdForCOL_Copy
                                        = new TermExecutionPosition_ObsoleteV1(RITEExecutionPosition[RITEId][COL]);

                                    for (int i = 0; i < RITEExecutionPositionForRITEIdForCOL_Copy.components.Count; i++)
                                    {
                                        ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R
                                            = RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).S
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).X
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).D;

                                        //ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).D
                                        //    = RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).S
                                        //        - RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).X
                                        //        - ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R;
                                    }

                                    TermAllocationPositionVector TermAllocationPositionVectorComponentsCoalecsion =
                                        new TermAllocationPositionVector(ChildRITEAllocationPosition[RITEId][COL]);
                                    TermAllocationPositionVectorComponentsCoalecsion.Coalesce();

                                    R_ForRITEChildren[RITEId][COL] = TermAllocationPositionVectorComponentsCoalecsion.R;
                                }
                            }
                        }

                        #endregion

                        RSum = R_ForRITEChildren.Values.Select(x => x.Values.Sum()).Sum();

                        #region Allocate R

                        foreach (long RITEId in SubjectRITEs)
                        {
                            foreach (string COL in SubjectCOLs.Select(x => x.ToString()))
                            {
                                TermExecutionPosition_ObsoleteV1 RITEExecutionPositionForRITEIdForCOL_Copy
                                    = new TermExecutionPosition_ObsoleteV1(RITEExecutionPosition[RITEId][COL]);

                                for (int i = 0; i < RITEExecutionPositionForRITEIdForCOL_Copy.components.Count; i++)
                                {
                                    ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R
                                        = ((RootNodeR == 0.0) ? 0.0 : RootNodeR * ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R / RSum);
                                }
                            }
                        }

                        #endregion

                        #endregion (summed term)
                    }

                    else
                    {
                        #region VECTOR CASE (per risk term)

                        Dictionary<long, Dictionary<string, List<double>>> R_ForRITEChildren
                            = new Dictionary<long, Dictionary<string, List<double>>>();

                        List<double> SumD = Enumerable.Repeat(0d, ANodeAllocationPosition.components.Count).ToList();

                        List<double> SumR = Enumerable.Repeat(0d, ANodeAllocationPosition.components.Count).ToList();

                        foreach (long RITEId in SubjectRITEs)
                        {
                            foreach (string COL in SubjectCOLs.Select(x => x.ToString()))
                            {
                                // TODO : Assert ANodeAllocationPosition.components.Count == RITEAllocationPositionVector[RITEId][COL].components.Count

                                for (int i = 0; i < RITEAllocationPositionVector[RITEId][COL].components.Count; i++ )
                                {
                                    SumD[i] += RITEAllocationPositionVector[RITEId][COL].components[i].D;

                                    SumR[i] += RITEAllocationPositionVector[RITEId][COL].components[i].R;
                                }
                            }
                        }

                        List<double> DeltaD = Enumerable.Repeat(0d, ANodeAllocationPosition.components.Count).ToList();
                        for (int i = 0; i < ANodeAllocationPosition.components.Count; i++)
                            DeltaD[i] = ANodeAllocationPosition.components[i].D - SumD[i];

                        List<double> RootNodeR = Enumerable.Repeat(0d, ANodeAllocationPosition.components.Count).ToList();
                        for (int i = 0; i < ANodeAllocationPosition.components.Count; i++)
                            RootNodeR[i] = ANodeAllocationPosition.components[i].R;

                        #region Compute R_ for RITE children

                        foreach (long RITEId in SubjectRITEs)
                        {
                            if (!ChildRITEAllocationPosition.ContainsKey(RITEId))
                                ChildRITEAllocationPosition.Add(RITEId, new Dictionary<string, TermAllocationPositionVector>());

                            if (!R_ForRITEChildren.ContainsKey(RITEId))
                                R_ForRITEChildren.Add(RITEId, new Dictionary<string, List<double>>());

                            foreach (string COL in SubjectCOLs.Select(x => x.ToString()))
                            {
                                TermExecutionPosition_ObsoleteV1 RITEExecutionPositionForRITEIdForCOL_Copy
                                    = new TermExecutionPosition_ObsoleteV1(RITEExecutionPosition[RITEId][COL]);

                                List<TermAllocationPositionVector.Component> TermAllocationPositionVectorComponents =
                                    new List<TermAllocationPositionVector.Component>(RITEExecutionPositionForRITEIdForCOL_Copy.components.Count);

                                for (int i = 0; i < RITEExecutionPositionForRITEIdForCOL_Copy.components.Count; i++)
                                {
                                    double R = RITEExecutionPositionForRITEIdForCOL_Copy.components[i].S
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.components[i].X 
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.components[i].D;

                                    double R_ = 0.0; double D_ = 0.0;

                                    if (DeltaD[i] >= 0.0 && SumR[i] > 0)
                                    {
                                        R_ = R - DeltaD[i] * (R / SumR[i]);
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.components[i].S 
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.components[i].X 
                                                - R_;
                                    }
                                    else if (DeltaD[i] >= 0.0 && SumR[i] == 0)
                                    {
                                        R_ = 0;
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.components[i].S
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.components[i].X
                                                - R_;
                                    }
                                    else
                                    {
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.components[i].D + (RITEExecutionPositionForRITEIdForCOL_Copy.components[i].D / SumD[i]) * DeltaD[i];
                                        R_ = RITEExecutionPositionForRITEIdForCOL_Copy.components[i].S - RITEExecutionPositionForRITEIdForCOL_Copy.components[i].X - D_;
                                    }

                                    TermAllocationPositionVectorComponents.Add(new TermAllocationPositionVector.Component(R_, D_));
                                }

                                ChildRITEAllocationPosition[RITEId].Add(COL, 
                                    new TermAllocationPositionVector(TermAllocationPositionVectorComponents, ANodeAllocationPosition.NumBuildings));

                                R_ForRITEChildren[RITEId].Add(COL, new List<double>(TermAllocationPositionVectorComponents.Count));
                                for (int i = 0; i < TermAllocationPositionVectorComponents.Count; i++)
                                {
                                    R_ForRITEChildren[RITEId][COL].Add(TermAllocationPositionVectorComponents[i].R);
                                }
                            }
                        }

                        #endregion

                        #region Modify R_, optionally, for children

                        List<double> RSum = Enumerable.Repeat(0d, ANodeAllocationPosition.components.Count).ToList();

                        for (int i = 0; i < R_ForRITEChildren.Keys.Count; i++)
                        {
                            for (int j = 0; j < R_ForRITEChildren.ElementAt(i).Value.Keys.Count; j++)
                            {
                                for (int k = 0; k < ANodeAllocationPosition.components.Count; k++)
                                {
                                    RSum[k] += R_ForRITEChildren.ElementAt(i).Value.ElementAt(j).Value[k];
                                }
                            }
                        }

                        foreach (long RITEId in SubjectRITEs)
                        {
                            foreach (string COL in SubjectCOLs.Select(x => x.ToString()))
                            {
                                TermExecutionPosition_ObsoleteV1 RITEExecutionPositionForRITEIdForCOL_Copy
                                    = new TermExecutionPosition_ObsoleteV1(RITEExecutionPosition[RITEId][COL]);

                                for (int i = 0; i < ANodeAllocationPosition.components.Count; i++)
                                {
                                    if ((DeltaD[i] >= 0.0) && (RSum[i] == 0.0))
                                    {
                                        ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R
                                            = RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).S
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).X
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).D;

                                        //ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).D
                                        //    = RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).S
                                        //        - RITEExecutionPositionForRITEIdForCOL_Copy.components.ElementAt(i).X
                                        //        - ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R;
                                    }
                                }

                                for (int i = 0; i < ChildRITEAllocationPosition[RITEId][COL].components.Count; i++)
                                    R_ForRITEChildren[RITEId][COL][i] = ChildRITEAllocationPosition[RITEId][COL].components[i].R;
                            }
                        }

                        #endregion

                        for (int k = 0; k < ANodeAllocationPosition.components.Count; k++)
                        {
                            RSum[k] = 0.0;

                            for (int i = 0; i < R_ForRITEChildren.Keys.Count; i++)
                            {
                                for (int j = 0; j < R_ForRITEChildren.ElementAt(i).Value.Keys.Count; j++)
                                {
                                    RSum[k] += R_ForRITEChildren.ElementAt(i).Value.ElementAt(j).Value[k];
                                }
                            }
                        }

                        #region Allocate R

                        foreach (long RITEId in SubjectRITEs)
                        {
                            foreach (string COL in SubjectCOLs.Select(x => x.ToString()))
                            {
                                for (int i = 0; i < RITEExecutionPosition[RITEId][COL].components.Count; i++)
                                {
                                    ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R
                                        = ((RootNodeR[i] == 0.0) ? 0.0 : 
                                                    RootNodeR[i] * 
                                                            ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R / RSum[i]);
                                }
                            }
                        }

                        #endregion

                        #endregion VECTOR CASE (per risk term)
                    }
                    
                    #region Update winning allocation position

                    foreach (long RITEId in ChildRITEAllocationPosition.Keys)
                    {
                        if (!RITEAllocationForLevel.ContainsKey(RITEId))
                            RITEAllocationForLevel.Add(RITEId, new Dictionary<string, TermAllocationPositionVector>());
                        foreach (string COL in ChildRITEAllocationPosition[RITEId].Keys)
                        {
                            if (!RITEAllocationForLevel[RITEId].ContainsKey(COL))
                                RITEAllocationForLevel[RITEId].Add(COL, ChildRITEAllocationPosition[RITEId][COL]);
                            else
                            {
                                TermAllocationPositionVector CandidateAllocationPosition
                                    = new TermAllocationPositionVector(ChildRITEAllocationPosition[RITEId][COL]);
                                CandidateAllocationPosition.Coalesce();
                                
                                TermAllocationPositionVector ExistingAllocationPosition
                                    = new TermAllocationPositionVector(RITEAllocationForLevel[RITEId][COL]);
                                ExistingAllocationPosition.Coalesce();

                                if (CandidateAllocationPosition.R > ExistingAllocationPosition.R)
                                {
                                    for (int i = 0; i < ChildRITEAllocationPosition[RITEId][COL].components.Count; i++)
                                    {
                                        RITEAllocationForLevel[RITEId][COL].components.ElementAt(i).R
                                            = ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R;
                                        RITEAllocationForLevel[RITEId][COL].components.ElementAt(i).D
                                            = ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).D;
                                    }
                                }
                                else if (CandidateAllocationPosition.R == ExistingAllocationPosition.R)
                                {
                                    if (CandidateAllocationPosition.D < ExistingAllocationPosition.D)
                                    {
                                        for (int i = 0; i < ChildRITEAllocationPosition[RITEId][COL].components.Count; i++)
                                        {
                                            RITEAllocationForLevel[RITEId][COL].components.ElementAt(i).R
                                                = ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R;
                                            RITEAllocationForLevel[RITEId][COL].components.ElementAt(i).D
                                                = ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).D;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                    #endregion B-3. Allocate to RITEs (for this level)
                }

                #endregion B. Iterate through all nodes in the level

                #region D. Allocate winner of level to global RITE allocation & update RITE execution position
                foreach (long RITEId in RITEAllocationForLevel.Keys)
                {
                    if (!RITEAllocationPositionVector.ContainsKey(RITEId))
                        RITEAllocationPositionVector.TryAdd(RITEId, new Dictionary<string, TermAllocationPositionVector>());

                    foreach (string COL in RITEAllocationForLevel[RITEId].Keys)
                    {
                        if (!RITEAllocationPositionVector[RITEId].ContainsKey(COL))
                            RITEAllocationPositionVector[RITEId].Add(COL, RITEAllocationForLevel[RITEId][COL]);
                        else
                            RITEAllocationPositionVector[RITEId][COL] = RITEAllocationForLevel[RITEId][COL];

                        // update execution position
                        List<TermExecutionPosition_ObsoleteV1.Component> RITExecutionPositionVectorComponents
                            = new List<TermExecutionPosition_ObsoleteV1.Component>();
                        for (int i = 0; i < RITEAllocationPositionVector[RITEId][COL].components.Count; i++)
                        {
                            double S = RITEExecutionPosition[RITEId][COL].components.ElementAt(i).S;
                            double D = RITEAllocationPositionVector[RITEId][COL].components.ElementAt(i).D;
                            double R =RITEAllocationPositionVector[RITEId][COL].components.ElementAt(i).R;
                            RITExecutionPositionVectorComponents.Add(new TermExecutionPosition_ObsoleteV1.Component(S, D, S - D - R));
                        }
                        RITEExecutionPosition[RITEId][COL] = new TermExecutionPosition_ObsoleteV1(RITExecutionPositionVectorComponents, RITEAllocationPositionVector[RITEId][COL].NumBuildings);
                    }
                }
                #endregion

                level--;
            }

            #region Update RITEAllocation (regular, i.e. non-vectorized form)
            RITEAllocation = new ConcurrentDictionary<long, Dictionary<string,TermAllocationPosition>>();
            foreach (long RITEId in RITEAllocationPositionVector.Keys)
            {
                if (!RITEAllocation.ContainsKey(RITEId))
                    RITEAllocation.TryAdd(RITEId, new Dictionary<string, TermAllocationPosition>());
                foreach (string COL in RITEAllocationPositionVector[RITEId].Keys)
                {
                    RITEAllocationPositionVector[RITEId][COL].Coalesce(); // destructive coalescion
                    RITEAllocation[RITEId].Add(COL, new TermAllocationPosition(RITEAllocationPositionVector[RITEId][COL].R, RITEAllocationPositionVector[RITEId][COL].D));
                }
            }
            #endregion
        }
        
        public ConcurrentDictionary<long, Dictionary<string, TermAllocationPosition>> GetAllocation()
        {
            return RITEAllocation;
        }
        #endregion

        #region IIdentifiableINodeCollection Overrides

        public bool Contains(Subject subject)
        {
            return IdentityMap.ContainsKey(subject);
        }

        public INode<TermCollection> this[Subject subject]
        {
            get 
            { 
                return IdentityMap[subject]; 
            }
        }

        public bool TryGetValue(Subject subject, out INode<TermCollection> value)
        {
            TermNode_ObsoleteV1 _value;
            bool result = IdentityMap.TryGetValue(subject, out _value);
            value = _value;
            return result;
        }

        #endregion
    }
}
