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
    class TermGraph : 
        DirectedGraph<TermCollection>,
        IEnumerable<INode<TermCollection>>,
        IIdentifiableINodeCollection<Subject, TermCollection>
    {
        #region Fields

        [ProtoMember(1)]
        protected Dictionary<Subject, TermNode> IdentityMap;

        [ProtoMember(2)]
        public HashSet<TermNode> RootNodes { get; private set; }

        [ProtoMember(4)]
        public Dictionary<TermNode, TermExecutionPosition> ExecutionRegister { get; private set; }
        Dictionary<TermNode, Subject> ResidualSubjectCache = null;
        Dictionary<TermNode, TermAllocationPosition> Allocation = null;
        Dictionary<TermNode, TermAllocationPositionVectorized> Allocation_ = null;

        [ProtoMember(6)]
        Dictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocation = null;

        Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>> RITEAllocation_ = null;

        [ProtoMember(7)]
        protected GraphOperationState ExecutionState;

        protected static readonly Subject EMPTYSUBJECTCONSTRAINT = new Subject();

        #endregion
        
        #region Overlap Fields
        public bool IsOverlapping { private set; get; }
        private Dictionary<INode<TermCollection>, int> TopoSort = null;
        #endregion

        #region Constructors
        public TermGraph() : base()
        {
            IdentityMap
                = new Dictionary<Subject, TermNode>();
            RootNodes = new HashSet<TermNode>();
            ExecutionState = new GraphOperationState();
            ResidualSubjectCache = new Dictionary<TermNode, Subject>();
            IsOverlapping = false;
        }

        public TermGraph(TermGraph CopyFromThisTermGraph)
            : base(CopyFromThisTermGraph)
        {
            this.IdentityMap = CopyFromThisTermGraph.IdentityMap;
            this.RootNodes = CopyFromThisTermGraph.RootNodes;
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

            if (NodeIdentity == null)
                NodeIdentity = EMPTYSUBJECTCONSTRAINT;

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

            TermCollection TermCollection = (IdentityMap.ContainsKey(NodeIdentity) ? 
                IdentityMap[NodeIdentity].GetContent() : new TermCollection(NodeIdentity));

            // Switching off the following check intentionally, in order to allow redundant terms
            //if (TermCollection.Contains(Term))
            //    throw new ArgumentException("A term node with the same identity (i.e. subject) already contains this term in its collection!");

            IsAddSuccessful = TermCollection.Add(Term);

            if (IsAddSuccessful && !IdentityMap.ContainsKey(NodeIdentity))
            {
                TermNode _TermNode = new TermNode(TermCollection);

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
                    foreach (long RITECharacteristicID in RITEIDs_ResolvedSchedulePerRiskifiedScheduleSymbol)
                    {
                        if (CoverageIdAttrMap.ContainsKey(RITECharacteristicID))
                        {
                            string TrimmedPerRiskifiedScheduleSymbol = PerRiskifiedScheduleSymbol.Replace(".#", "");
                            string NewScheduleSymbol = TrimmedPerRiskifiedScheduleSymbol + "." + CoverageIdAttrMap[RITECharacteristicID].RITExposureId.ToString();
                            if (!ExplodedSchedule.ContainsKey(NewScheduleSymbol))
                                ExplodedSchedule.Add(NewScheduleSymbol, new HashSet<long>());
                            ExplodedSchedule[NewScheduleSymbol].Add(RITECharacteristicID);
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
        public bool Rebuild()
        {
            IEnumerable<INode<TermCollection>> TermNodes = this;

            bool IsRebuildSuccessful = true;

            #region O(m * N^2) algorithm to produce all ancestors for all nodes

            Dictionary<Subject, TermNodeWithGuid> IdentityMapWithGuid
                = new Dictionary<Subject, TermNodeWithGuid>();

            Dictionary<TermNodeWithGuid, HashSet<TermNodeWithGuid>> AllAncestors
                = new Dictionary<TermNodeWithGuid, HashSet<TermNodeWithGuid>>();
            
            // O(N) to construct TermNodeWithGuid versions for each node
            foreach (TermNode TermNode in this)
            {
                IdentityMapWithGuid.Add(TermNode.GetSubject(), new TermNodeWithGuid(TermNode));
            }
            
            // O(m * N^2) to produce all ancestors for all nodes
            foreach (TermNode TermNode in TermNodes)
            {
                TermNodeWithGuid TermNodeWithGUID = IdentityMapWithGuid[TermNode.GetSubject()];
                foreach (TermNode OtherTermNode in TermNodes.Where(TN => !TN.Equals(TermNode)))
                {
                    if (TermNode.IsChildOf(OtherTermNode))
                    {
                        TermNodeWithGUID.AddParent(IdentityMapWithGuid[OtherTermNode.GetSubject()]);

                        if (!AllAncestors.ContainsKey((TermNodeWithGuid)TermNodeWithGUID))
                            AllAncestors.Add((TermNodeWithGuid)TermNodeWithGUID, new HashSet<TermNodeWithGuid>());
                        AllAncestors[(TermNodeWithGuid)TermNodeWithGUID].Add((TermNodeWithGuid)IdentityMapWithGuid[OtherTermNode.GetSubject()]);
                    }
                }
            }

            // O(m * N) to replace IdentityMap with TermNodeWithGuid versions
            foreach (Subject Subject in IdentityMapWithGuid.Keys)
            {
                TermNode TermNode = IdentityMap[Subject];
                TermNodeWithGuid TermNodeWithGUID = IdentityMapWithGuid[Subject];

                IdentityMap.Remove(Subject);
                Remove(TermNode);

                Add(TermNodeWithGUID);
                IdentityMap.Add(Subject, TermNodeWithGUID);

                if (RootNodes.Contains(TermNode))
                {
                    RootNodes.Remove(TermNode);
                    RootNodes.Add(TermNodeWithGUID);
                }
            }

            #endregion

            foreach (TermNode TermNode in TermNodes)
            {
                HashSet<INode<TermCollection>> NoNeedToConsiderForTermNode = new HashSet<INode<TermCollection>>();

                NoNeedToConsiderForTermNode.Add(TermNode);

                foreach (TermNode OtherTermNode in TermNodes)
                {
                    if (NoNeedToConsiderForTermNode.Contains(OtherTermNode))
                        continue;

                    #region if (TermNode.IsChildOf(OtherTermNode))
                    if (TermNode.IsChildOf(OtherTermNode))
                    {
                        // find lowest-level parent
                        TermNode CurrentParentNode = OtherTermNode;
                        do
                        {
                            bool keepGoing = false;
                            foreach (TermNode ChildNodeOfCurrentParent in GetChildrenOfNode(CurrentParentNode).Where(TN => (TN != TermNode)))
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
                        while (TermNode.IsChildOf(CurrentParentNode));

                        #region search for and delete non-direct ancestral links
                        //foreach (TermNode ParentTermNode in GetParentsOfNode(CurrentParentNode))
                        //{
                        //    if (AllAncestors.ContainsKey(ParentTermNode)
                        //        && AllAncestors[ParentTermNode].Contains(TermNode)) // if (IsReachable(ParentTermNode, TermNode))
                        //    {
                        //        DeleteParentChildEdge(ParentTermNode, TermNode);
                        //        // RootNodes & LeafNodes update
                        //        if ((GetParentsOfNode(TermNode) == null)
                        //            || ((GetParentsOfNode(TermNode)).Count() == 0))
                        //            RootNodes.Add(TermNode);
                        //    }
                        //}
                        #endregion

                        #region search for and delete non-direct ancestral links to ancestors of CurrentParentNode
                        Queue<INode<TermCollection>> Ancestors = new Queue<INode<TermCollection>>(GetParentsOfNode(CurrentParentNode));

                        if (AllAncestors.ContainsKey((TermNodeWithGuid)TermNode))
                        {
                            while (Ancestors.Count != 0)
                            {
                                TermNodeWithGuid AncestorNode = (TermNodeWithGuid)Ancestors.Dequeue();
                                if (AllAncestors[(TermNodeWithGuid)TermNode].Contains(AncestorNode))
                                {
                                    DeleteParentChildEdge(AncestorNode, TermNode);
                                    // RootNodes & LeafNodes update
                                    if ((GetParentsOfNode(TermNode) == null)
                                        || ((GetParentsOfNode(TermNode)).Count() == 0))
                                        RootNodes.Add(TermNode);
                                }
                                foreach (INode<TermCollection> ParentOfAncestor in GetParentsOfNode(AncestorNode))
                                    Ancestors.Enqueue(ParentOfAncestor);
                            }
                        }
                        #endregion

                        #region search for and delete non-direct descendant links
                        //foreach (TermNode ChildTermNode in GetChildrenOfNode(TermNode))
                        //{
                        //    //if (AllAncestors.ContainsKey(CurrentParentNode)
                        //    //    && AllAncestors[CurrentParentNode].Contains(ChildTermNode)) // if (IsReachable(CurrentParentNode, ChildTermNode))
                        //    if (IsReachable(CurrentParentNode, ChildTermNode))
                        //    {
                        //        DeleteParentChildEdge(CurrentParentNode, ChildTermNode);
                        //        // RootNodes & LeafNodes update
                        //        if ((GetParentsOfNode(ChildTermNode) == null)
                        //            || ((GetParentsOfNode(ChildTermNode)).Count() == 0))
                        //            RootNodes.Add(ChildTermNode);
                        //    }
                        //}
                        #endregion

                        IsRebuildSuccessful &= MakeParentChildEdge(CurrentParentNode, TermNode);

                        // RootNodes & LeafNodes update
                        if ((GetParentsOfNode(CurrentParentNode) == null) || ((GetParentsOfNode(CurrentParentNode)).Count() == 0))
                            RootNodes.Add(CurrentParentNode);
                        RootNodes.Remove(TermNode);
                    }
                    #endregion if (TermNode.IsChildOf(OtherTermNode))

                    #region else if (OtherTermNode.IsChildOf(TermNode))
                    else if (OtherTermNode.IsChildOf(TermNode))
                    {
                        // find lowest-level parent
                        TermNode CurrentParentNode = TermNode;
                        do
                        {
                            bool keepGoing = false;
                            foreach (TermNode ChildNodeOfCurrentParent in GetChildrenOfNode(CurrentParentNode).Where(TN => (TN != TermNode)))
                            {
                                if (OtherTermNode.IsChildOf(ChildNodeOfCurrentParent))
                                {
                                    CurrentParentNode = ChildNodeOfCurrentParent;
                                    keepGoing = true;
                                    break;
                                }
                            }
                            if (!keepGoing)
                                break;
                        }
                        while (OtherTermNode.IsChildOf(CurrentParentNode));

                        #region search for and delete ancestral links
                        //foreach (TermNode ParentOtherTermNode in GetParentsOfNode(CurrentParentNode))
                        //{
                        //    if (IsReachable(ParentOtherTermNode, OtherTermNode))
                        //    {
                        //        DeleteParentChildEdge(ParentOtherTermNode, OtherTermNode);
                        //        // RootNodes & LeafNodes update
                        //        if ((GetParentsOfNode(OtherTermNode) == null) || ((GetParentsOfNode(OtherTermNode)).Count() == 0))
                        //            RootNodes.Add(OtherTermNode);
                        //        if ((GetChildrenOfNode(ParentOtherTermNode) == null) || ((GetChildrenOfNode(ParentOtherTermNode)).Count() == 0))
                        //            LeafNodes.Add(ParentOtherTermNode);
                        //    }
                        //}
                        #endregion

                        #region search for and delete non-direct descendant links of OtherTermNode
                        Queue<INode<TermCollection>> Descendants = new Queue<INode<TermCollection>>(GetChildrenOfNode(OtherTermNode));

                        while (Descendants.Count != 0)
                        {
                            TermNodeWithGuid DescendantNode = (TermNodeWithGuid)Descendants.Dequeue();
                            if (AllAncestors[DescendantNode].Contains((TermNodeWithGuid)CurrentParentNode))
                            {
                                DeleteParentChildEdge(CurrentParentNode, DescendantNode);
                                // RootNodes & LeafNodes update
                                if ((GetParentsOfNode(DescendantNode) == null)
                                    || ((GetParentsOfNode(DescendantNode)).Count() == 0))
                                    RootNodes.Add(DescendantNode);
                            }
                            foreach (INode<TermCollection> ChildOfDescendant in GetChildrenOfNode(DescendantNode))
                                Descendants.Enqueue(ChildOfDescendant);
                        }
                        #endregion

                        IsRebuildSuccessful &= MakeParentChildEdge(CurrentParentNode, OtherTermNode);

                        // RootNodes & LeafNodes update
                        if ((GetParentsOfNode(CurrentParentNode) == null) || (GetParentsOfNode(CurrentParentNode).Count() == 0))
                            RootNodes.Add(CurrentParentNode);
                        RootNodes.Remove(OtherTermNode);
                    }
                    #endregion else if (OtherTermNode.IsChildOf(TermNode))
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

        public bool MakeParentChildEdge(TermNode parent, TermNode child, bool AddAnyNodesNotPresent = false)
        {
            child.AddParent(parent);
            return base.MakeParentChildEdge(parent, child, AddAnyNodesNotPresent);
        }

        public bool DeleteParentChildEdge(TermNode parent, TermNode child)
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

            foreach (TermNode ANode in this)
            {
                foreach (TermNode AnotherNode in this.Where(N => (N != ANode)))
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
            foreach (TermNode ANode in this)
            {
                TopoSort.Add(ANode, -1);
            }
            // Set RootNodes to 0 and recurse
            foreach (TermNode RootNode in RootNodes)
            {
                TopoSort.Remove(RootNode);
                TopoSort.Add(RootNode, 0);
                RecursiveTopologicalSort(RootNode, TopoSort[RootNode]);
            }
        }

        private void RecursiveTopologicalSort(TermNode ANode, int ANodeTopoVal)
        {
            foreach (TermNode AChildNode in GetChildrenOfNode(ANode))
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
            Dictionary<TermNode, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings,
            bool ShouldAllocate = true,
            bool RedoEvenIfCached = true)
        {
            if (IdentityMap.Count == 0)
            {
                // Update execution state
                ExecutionState = new GraphOperationState(false,
                    new Dictionary<string, HashSet<long>>(Schedule),
                    new Dictionary<long, Loss>(CoverageIdGULossMap));
                // ALLOCATE
                if (ShouldAllocate)
                    this.Allocate_(Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
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

            ExecutionRegister = new Dictionary<TermNode, TermExecutionPosition>();
            Allocation = new Dictionary<TermNode, TermAllocationPosition>();
            Allocation_ = new Dictionary<TermNode, TermAllocationPositionVectorized>();

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
            foreach (TermNode RootNode in RootNodes)
            {
                ExecutionRegister.Add(RootNode,
                                                ExecuteTermNode(RootNode, Schedule, CoverageIdAttrMap,
                                                    CoverageIdGULossMap, RCVCoveredAndAffectedBySubject,
                                                    Bindings)
                                                );
            }
            #endregion

            // Update execution state
            ExecutionState = new GraphOperationState(false, new Dictionary<string, HashSet<long>>(Schedule), new Dictionary<long, Loss>(CoverageIdGULossMap));

            // ALLOCATE
            if (ShouldAllocate)
            {
                //this.Allocate(Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
                this.Allocate_(Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
            }
        }

        /// <summary>
        /// Execute node; recurse over children, if any.
        /// </summary>
        /// <param name="_TermNode"></param>
        /// <param name="Schedule"></param>
        /// <param name="CoverageIdGULossMap"></param>
        /// <returns></returns>
        private TermExecutionPosition ExecuteTermNode(TermNode _TermNode,
            Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<TermNode, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
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
            foreach (TermNode _ChildTermNode in ChildTermNodes)
            {
                ExecutionRegister.Add(_ChildTermNode,
                                                ExecuteTermNode(_ChildTermNode, Schedule, CoverageIdAttrMap,
                                                    CoverageIdGULossMap, RCVCoveredAndAffectedBySubject,
                                                    Bindings)
                                                );
            }

            // B. Construct SubjectPosition for this _TermNode from ResultPosition(s) of child nodes and residual subject

            List<TermExecutionPosition> SubjectPosition
                = new List<TermExecutionPosition>(ChildTermNodes.Count + 1);

            // C.1 Get (or, construct, once) residual subject position & add

            Subject ResidualSubject = ResidualSubjectCache[_TermNode];

            if (ResidualSubject.PerRisk
                       && (ResidualSubject.Schedule.ScheduleSymbols.Count == 1)
                       && (ResidualSubject.RITEIds.Any(x => 
                           (CoverageIdAttrMap.ContainsKey(x) && CoverageIdGULossMap.ContainsKey(x) &&
                            (CoverageIdGULossMap[x].FactorArray.Length > 1)))
                          )
                )
            {
                _TermNode.MarkAsMultiBuildingPerRisk();
            }

            TermExecutionPosition ResidualSubjectPosition = 
                GetResidualSubjectPosition(ResidualSubject,
                    CoverageIdAttrMap,
                    CoverageIdGULossMap,
                    _TermNode.IsMultiBuildingPerRisk());

            SubjectPosition.Add(ResidualSubjectPosition);

            // D.1 Prepare bindings

            Dictionary<SimpleExpression<SymbolicValue>, double> BindingsForNode =
                        GetBindingsForNode(_TermNode, RCVCoveredAndAffectedBySubject, Bindings);

            // D.2 Wait for all child tasks to complete; then add to SubjectPosition

            Subject _TermNodeSubject = (_TermNode.GetSubject() as Subject);

            bool AreChildrenPerRisk = false;

            foreach (TermNode _ChildTermNode in ChildTermNodes)
            {
                SubjectPosition.Add(ExecutionRegister[_ChildTermNode]);
                if (ExecutionRegister[_ChildTermNode].FactorArray.Length > 1)
                    AreChildrenPerRisk = true;
            }

            if (_TermNodeSubject.PerRisk && AreChildrenPerRisk)
                _TermNode.MarkAsMultiBuildingPerRisk();

            // E. Execute this node

            return _TermNode.Execute(SubjectPosition, BindingsForNode);
        }

        private void Allocate(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap)
        {
            if (ExecutionState.HasOperationStateChanged())
                throw new Exception("Term Graph not yet executed or has changed since last execution!");

            RITEAllocation = new Dictionary<long, Dictionary<string,TermAllocationPosition>>();

            // Allocate all root nodes
            foreach (TermNode RootNode in RootNodes)
            {
                // Coalesce (applicable to per-risk multi-buidling roots)
                ExecutionRegister[RootNode].Coalesce();

                // Allocate root nodes trivially
                TermAllocationPosition AllocationPosition =
                    new TermAllocationPosition((ExecutionRegister[RootNode].S - ExecutionRegister[RootNode].D - ExecutionRegister[RootNode].X),
                        ExecutionRegister[RootNode].D);

                if (!Allocation.ContainsKey(RootNode))
                    Allocation.Add(RootNode, AllocationPosition);

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
                                RITEAllocation.Add(RITEId, new Dictionary<string, TermAllocationPosition>());
                            RITEAllocation[RITEId].Add(COL.ToString(),
                                new TermAllocationPosition(GULossForRITEID[COL], 0.0));
                            // @FACTORCHANGES REPLACE
                            //RITEAllocation[RITEId].Add(COL.ToString(), 
                            //    new TermAllocationPosition(Loss.WeightedSum(GULossForRITEID.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings), 0.0));
                        }
                    }
                }
            }
        }

        private void AllocateSubtree(TermNode RootNode, TermAllocationPosition AllocationPosition,
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
            foreach (TermNode Child in Children)
                ExecutionRegister[Child].Coalesce();

            double SumD =
                Children.Aggregate(0.0, (accumulator, it) => accumulator + ExecutionRegister[(TermNode)it].D);

            double DeltaD =
                AllocationPosition.D - SumD;

            double SumR =
                Children.Aggregate(0.0, (accumulator, it) => accumulator +
                                (ExecutionRegister[(TermNode)it].S - ExecutionRegister[(TermNode)it].D - ExecutionRegister[(TermNode)it].X))
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

            Dictionary<TermNode, double> R_ForTermChildren = new Dictionary<TermNode, double>(Children.Count);

            foreach (TermNode ChildNode in Children)
            {
                //TermAllocationPosition AllocationPosition = new TermAllocationPosition();
                if (!Allocation.ContainsKey(ChildNode))
                    Allocation.Add(ChildNode, new TermAllocationPosition());

                TermAllocationPosition ChildNodeAllocationPosition = Allocation[ChildNode];

                TermExecutionPosition ChildNodeExecutionPosition = ExecutionRegister[ChildNode];

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
                    // COL EQUIVALENCY CHANGES
                    if (!GULossForRITEId.ContainsCOL(COL))
                        continue;
                    if (!R_ForRITEChildren.ContainsKey(RITEId))
                        R_ForRITEChildren.Add(RITEId, new Dictionary<string, double>());
                    R_ForRITEChildren[RITEId].Add(COL.ToString(), GULossForRITEId[COL]);
                    // @FACTORCHANGES REPLACE
                    //R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                }
            }

            // D. Modify R_, optionally for children

            double RSum = R_ForTermChildren.Values.Sum() + R_ForRITEChildren.Values.Select(x => x.Values.Sum()).Sum();

            if ((DeltaD >= 0.0) && (RSum == 0.0))
            {
                foreach (TermNode ChildNode in Children)
                {
                    TermExecutionPosition ChildNodeExecutionPosition = ExecutionRegister[ChildNode];

                    double R = ChildNodeExecutionPosition.S - ChildNodeExecutionPosition.X - ChildNodeExecutionPosition.D;

                    R_ForTermChildren[ChildNode] = R;
                }
                foreach (long RITEId in RITEs)
                {
                    Loss GULossForRITEId = CoverageIdGULossMap[RITEId];

                    foreach (SymbolicValue COL in GULossForRITEId.AmountByCOL.Keys)
                    {
                        if (!R_ForRITEChildren[RITEId].ContainsKey(COL.ToString()))
                            R_ForRITEChildren[RITEId].Add(COL.ToString(), GULossForRITEId[COL]);
                        else
                            R_ForRITEChildren[RITEId][COL.ToString()] = GULossForRITEId[COL];
                        // @FACTORCHANGES REPLACE
                        //if (!R_ForRITEChildren[RITEId].ContainsKey(COL.ToString()))
                        //    R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                        //else
                        //    R_ForRITEChildren[RITEId][COL.ToString()] = Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings);
                    }
                }
            }

            RSum = R_ForTermChildren.Values.Sum() + R_ForRITEChildren.Values.Select(x => x.Values.Sum()).Sum();

            // E. Allocate R

            foreach (TermNode ChildNode in Children)
            {
                TermAllocationPosition ChildNodeAllocationPosition = Allocation[ChildNode];

                ChildNodeAllocationPosition.R = (RootNodeR == 0.0) ? 0.0 : RootNodeR * (R_ForTermChildren[ChildNode] / RSum);
            }

            foreach (long RITEId in RITEs)
            {
                if (!RITEAllocation.ContainsKey(RITEId))
                    RITEAllocation.Add(RITEId, new Dictionary<string, TermAllocationPosition>());
                Dictionary<string, double> R_ForRITEChildren_RITEId = R_ForRITEChildren[RITEId];
                foreach (string COL in R_ForRITEChildren_RITEId.Keys)
                {
                    if (!RITEAllocation[RITEId].ContainsKey(COL))
                        RITEAllocation[RITEId].Add(COL, new TermAllocationPosition(((RootNodeR == 0.0) ? 0.0 : RootNodeR * R_ForRITEChildren_RITEId[COL] / RSum), 0.0));
                }
            }

            // F. Recursively, allocate children

            foreach (TermNode ChildNode in Children)
            {
                AllocateSubtree((TermNode)ChildNode, Allocation[(TermNode)ChildNode], Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
            }
        }

        private void Allocate_(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap)
        {
            if (ExecutionState.HasOperationStateChanged())
                throw new Exception("Term Graph not yet executed or has changed since last execution!");

            RITEAllocation_ = new Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>>();

            // Allocate all root nodes
            foreach (TermNode RootNode in RootNodes)
            {
                // Allocate root nodes trivially
                TermAllocationPositionVectorized AllocationPosition = null;

                if (RootNode.IsMultiBuildingPerRisk())
                    AllocationPosition = new TermAllocationPositionVectorized((ExecutionRegister[RootNode].S_vector.Zip(ExecutionRegister[RootNode].D_vector, (a, b) => a - b).Zip(ExecutionRegister[RootNode].X_vector, (a, b) => a - b)).ToArray<double>(),
                        ExecutionRegister[RootNode].D_vector);
                else
                {
                    ExecutionRegister[RootNode].Coalesce();
                    AllocationPosition = new TermAllocationPositionVectorized((ExecutionRegister[RootNode].S - ExecutionRegister[RootNode].D - ExecutionRegister[RootNode].X),
                        ExecutionRegister[RootNode].D);
                }

                if (!Allocation_.ContainsKey(RootNode))
                    Allocation_.Add(RootNode, AllocationPosition);

                if (RootNode.IsMultiBuildingPerRisk())
                    AllocateSubtree_PerRisk(RootNode, AllocationPosition, Schedule, 
                    CoverageIdAttrMap, CoverageIdGULossMap);
                else
                    AllocateSubtree__(RootNode, AllocationPosition, Schedule,
                    CoverageIdAttrMap, CoverageIdGULossMap);
            }

            // Allocate residual subject (i.e. all subject loss position RITEs not yet allocated) trivially
            // TODO : BUGFIX : distinguish per risk multi building vs. not
            foreach (string ScheduleSymbol in Schedule.Keys)
            {
                foreach (long RITEId in Schedule[ScheduleSymbol])
                {
                    if (!CoverageIdGULossMap.ContainsKey(RITEId))
                        continue;
                    if (!RITEAllocation_.ContainsKey(RITEId))
                    {
                        Loss GULossForRITEID = CoverageIdGULossMap[RITEId];
                        GULossForRITEID.NumBuildings = CoverageIdAttrMap[RITEId].NumBuildings;
                        foreach (SymbolicValue COL in GULossForRITEID.AmountByCOL.Keys)
                        {
                            if (!RITEAllocation_.ContainsKey(RITEId))
                                RITEAllocation_.Add(RITEId, new Dictionary<string, TermAllocationPositionVectorized>());
                            RITEAllocation_[RITEId].Add(COL.ToString(),
                                new TermAllocationPositionVectorized(GULossForRITEID.AmountByCOL[COL].ToArray(),
                                    Enumerable.Repeat(0.0d, GULossForRITEID.AmountByCOL[COL].Count).ToArray(), GULossForRITEID.FactorArray));
                            // @FACTORCHANGES REPLACE
                            //RITEAllocation[RITEId].Add(COL.ToString(), 
                            //    new TermAllocationPosition(Loss.WeightedSum(GULossForRITEID.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings), 0.0));
                        }
                    }
                }
            }
        }

        private void AllocateSubtree__(TermNode RootNode, TermAllocationPositionVectorized AllocationPosition,
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
            
            double SumD =
                Children.Aggregate(0.0, (accumulator, it) => accumulator +
                    (((it as TermNode).IsMultiBuildingPerRisk()) ? ExecutionRegister[(TermNode)it].D_vector.Zip(ExecutionRegister[(TermNode)it].FactorArray, (a, b) => a * b).Sum()
                                            : ExecutionRegister[(TermNode)it].D));

            double DeltaD = AllocationPosition.D_vector.Sum() - SumD;

            double SumR = 0.0;

            foreach (TermNode ChildNode in Children)
            {
                if (ChildNode.IsMultiBuildingPerRisk())
                {
                    SumR +=
                        ExecutionRegister[ChildNode].S_vector.Zip(ExecutionRegister[ChildNode].FactorArray, (a, b) => a * b).Sum()
                        - ExecutionRegister[ChildNode].D_vector.Zip(ExecutionRegister[ChildNode].FactorArray, (a, b) => a * b).Sum()
                        - ExecutionRegister[ChildNode].X_vector.Zip(ExecutionRegister[ChildNode].FactorArray, (a, b) => a * b).Sum();

                }
                else
                {
                    SumR +=
                        ExecutionRegister[ChildNode].S
                        - ExecutionRegister[ChildNode].D
                        - ExecutionRegister[ChildNode].X;
                }
            }

            foreach (long RITEId in RITEs)
            {
                SumR +=
                            CoverageIdGULossMap[RITEId].AmountByCOL
                                .Where(x => ResidualSubject.CausesOfLoss.Contains(x.Key))
                                .Select(x => x.Value.Zip(CoverageIdGULossMap[RITEId].FactorArray, (a, b) => a * b).Sum())
                                .Sum();
            }

            double RootNodeR = AllocationPosition.R_vector.Sum(); // TODO : verify same as Allocation[RootNode].R;

            // C.1 Compute R_ for term children & allocate D

            Dictionary<TermNode, double[]> R_ForTermChildren
                = new Dictionary<TermNode, double[]>(Children.Count);

            foreach (TermNode ChildNode in Children)
            {
                TermExecutionPosition ChildNodeExecutionPosition
                    = ExecutionRegister[ChildNode];

                double[] R = ChildNodeExecutionPosition.S_vector.Zip(ChildNodeExecutionPosition.X_vector, (a, b) => a - b)
                    .Zip(ChildNodeExecutionPosition.D_vector, (a, b) => a - b).ToArray();

                if (ChildNode.IsMultiBuildingPerRisk())
                {
                    R = R.Zip(ChildNodeExecutionPosition.FactorArray, (a, b) => a * b).ToArray();
                }

                double[] R_ = Enumerable.Repeat(0.0d, R.Length).ToArray();

                if (!Allocation_.ContainsKey(ChildNode))
                    Allocation_.Add(ChildNode, new TermAllocationPositionVectorized(R_));

                TermAllocationPositionVectorized ChildNodeAllocationPosition
                    = Allocation_[ChildNode];

                for (int i = 0; i < R_.Length; i++)
                {
                    if (DeltaD >= 0.0)
                    {
                        R_[i] = R[i] - DeltaD * (R[i] / SumR);
                        ChildNodeAllocationPosition.D_vector[i] =
                            (ChildNodeExecutionPosition.S_vector[i] * ChildNodeExecutionPosition.FactorArray[i])
                            - (ChildNodeExecutionPosition.X_vector[i] * ChildNodeExecutionPosition.FactorArray[i])
                            - R_[i];
                    }
                    else
                    {
                        ChildNodeAllocationPosition.D_vector[i] =
                            (ChildNodeExecutionPosition.D_vector[i] * ChildNodeExecutionPosition.FactorArray[i])
                            + (ChildNodeExecutionPosition.D_vector[i]  * ChildNodeExecutionPosition.FactorArray[i] / SumD) * DeltaD;
                        R_[i] = (ChildNodeExecutionPosition.S_vector[i] * ChildNodeExecutionPosition.FactorArray[i])
                            - (ChildNodeExecutionPosition.X_vector[i] * ChildNodeExecutionPosition.FactorArray[i])
                            - ChildNodeAllocationPosition.D_vector[i];
                    }
                }

                R_ForTermChildren.Add(ChildNode, R_);
            }

            // C.2 Compute R_ for RITE children

            Dictionary<long, Dictionary<string, double[]>> R_ForRITEChildren
                = new Dictionary<long, Dictionary<string, double[]>>(RITEs.Count);

            foreach (long RITEId in RITEs)
            {
                Loss GULossForRITEId = CoverageIdGULossMap[RITEId];

                foreach (SymbolicValue COL in ResidualSubject.CausesOfLoss)
                {
                    // COL EQUIVALENCY CHANGES
                    if (!GULossForRITEId.ContainsCOL(COL))
                        continue;
                    if (!R_ForRITEChildren.ContainsKey(RITEId))
                        R_ForRITEChildren.Add(RITEId, new Dictionary<string, double[]>());
                    R_ForRITEChildren[RITEId].Add(COL.ToString(),
                        GULossForRITEId.AmountByCOL[COL].Zip(GULossForRITEId.FactorArray, (a, b) => a * b).ToArray());
                    // @FACTORCHANGES REPLACE
                    //R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                }
            }

            // D. Modify R_, optionally for children

            double RSum = R_ForTermChildren.Values.Select(x => x.Sum()).Sum()
                                +
            R_ForRITEChildren.Values.SelectMany(x => x.Values).Select(x => x.Sum()).Sum();

            if ((DeltaD >= 0.0) && (RSum == 0.0))
            {
                foreach (TermNode ChildNode in Children)
                {
                    TermExecutionPosition ChildNodeExecutionPosition
                        = ExecutionRegister[ChildNode];

                    double[] R = ChildNodeExecutionPosition.S_vector.Zip(ChildNodeExecutionPosition.X_vector, (a, b) => a - b)
                        .Zip(ChildNodeExecutionPosition.D_vector, (a, b) => a - b).ToArray(); ;

                    if (ChildNode.IsMultiBuildingPerRisk())
                    {
                        R = R.Zip(ChildNodeExecutionPosition.FactorArray, (a, b) => a * b).ToArray();
                    }

                    R_ForTermChildren[ChildNode] = R;
                }
                foreach (long RITEId in RITEs)
                {
                    Loss GULossForRITEId = CoverageIdGULossMap[RITEId];

                    foreach (SymbolicValue COL in GULossForRITEId.AmountByCOL.Keys)
                    {
                        if (!R_ForRITEChildren[RITEId].ContainsKey(COL.ToString()))
                            R_ForRITEChildren[RITEId].Add(COL.ToString(),
                                GULossForRITEId.AmountByCOL[COL].ToArray());
                        else
                            R_ForRITEChildren[RITEId][COL.ToString()] =
                                GULossForRITEId.AmountByCOL[COL].Zip(GULossForRITEId.FactorArray, (a, b) => a * b).ToArray();
                        // @FACTORCHANGES REPLACE
                        //if (!R_ForRITEChildren[RITEId].ContainsKey(COL.ToString()))
                        //    R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                        //else
                        //    R_ForRITEChildren[RITEId][COL.ToString()] = Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings);
                    }
                }
            }

            RSum = R_ForTermChildren.Values.Select(x => x.Sum()).Sum()
                                +
            R_ForRITEChildren.Values.SelectMany(x => x.Values).Select(x => x.Sum()).Sum();

            // E. Allocate R

            foreach (TermNode ChildNode in Children)
            {
                TermAllocationPositionVectorized ChildNodeAllocationPosition
                    = Allocation_[ChildNode];

                if (ChildNode.IsMultiBuildingPerRisk())
                {
                    for (int i = 0; i < R_ForTermChildren[ChildNode].Length; i++)
                    {
                        ChildNodeAllocationPosition.R_vector[i] =
                            (RootNodeR == 0.0) ? 0.0 :
                            RootNodeR * (R_ForTermChildren[ChildNode][i] / RSum) / ExecutionRegister[ChildNode].FactorArray[i];
                    }
                }
                else
                {
                    for (int i = 0; i < R_ForTermChildren[ChildNode].Length; i++)
                    {
                        ChildNodeAllocationPosition.R_vector[i] =
                            (RootNodeR == 0.0) ? 0.0 :
                            RootNodeR * (R_ForTermChildren[ChildNode][i] / RSum);
                    }
                }
            }

            foreach (long RITEId in RITEs)
            {
                if (!RITEAllocation_.ContainsKey(RITEId))
                    RITEAllocation_.Add(RITEId, 
                        new Dictionary<string, TermAllocationPositionVectorized>());
                Dictionary<string, double[]> R_ForRITEChildren_RITEId
                    = R_ForRITEChildren[RITEId];
                foreach (string COL in R_ForRITEChildren_RITEId.Keys)
                {
                    if (!RITEAllocation_[RITEId].ContainsKey(COL))
                    {
                        double[] R_ForCOL_ForRITEId 
                            = new double[R_ForRITEChildren_RITEId[COL].Length];
                        for (int i = 0; i < R_ForRITEChildren_RITEId[COL].Length; i++)
                        {
                            R_ForCOL_ForRITEId[i] =
                                (RootNodeR == 0.0) ? 0.0 :
                                RootNodeR * (R_ForRITEChildren_RITEId[COL][i] / RSum) / CoverageIdGULossMap[RITEId].FactorArray[i];
                        }
                        RITEAllocation_[RITEId].Add(COL,
                                new TermAllocationPositionVectorized(R_ForCOL_ForRITEId,
                                    Enumerable.Repeat(0.0d, R_ForCOL_ForRITEId.Length).ToArray(), CoverageIdGULossMap[RITEId].FactorArray));
                    }
                }
            }

            // F. Recursively, allocate children

            foreach (TermNode ChildNode in Children)
            {
                if (ChildNode.IsMultiBuildingPerRisk())
                    AllocateSubtree_PerRisk((TermNode)ChildNode, Allocation_[(TermNode)ChildNode],
                        Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
                else
                {
                    // TODO : verify below redundant
                    // ExecutionRegister[(TermNode)ChildNode].Coalesce();
                    AllocateSubtree__((TermNode)ChildNode, Allocation_[(TermNode)ChildNode],
                        Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
                }
            }
        }

        private void AllocateSubtree_PerRisk(TermNode RootNode, TermAllocationPositionVectorized AllocationPosition,
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

            double[] SumD = null;

            if (Children != null && Children.Count != 0)
            {
                SumD = Enumerable.Repeat(0.0d, ExecutionRegister[(TermNode)Children.First()].D_vector.Length).ToArray();
            }
            else // if (RITEs.Count != 0) // ASSERT : always true!
            {
                SumD = Enumerable.Repeat(0.0d, CoverageIdGULossMap[RITEs.First()].AmountByCOL
                                                                   .Where(x => ResidualSubject.CausesOfLoss.Contains(x.Key)).First().Value.Count).ToArray();
            }

            SumD =
                Children.Aggregate(SumD, (accumulator, it) => 
                    accumulator.Zip(ExecutionRegister[(TermNode)it].D_vector, (a, b) => a + b).ToArray());

            double[] DeltaD =
                AllocationPosition.D_vector.Zip(SumD, (a, b) => a - b).ToArray();

            double[] SumR = Enumerable.Repeat(0.0d, SumD.Length).ToArray();

            SumR =
                Children.Aggregate(SumR, (accumulator, it) => accumulator.Zip(
                                (ExecutionRegister[(TermNode)it].S_vector.Zip(ExecutionRegister[(TermNode)it].D_vector, (a, b) => a - b).Zip(ExecutionRegister[(TermNode)it].X_vector, (a, b) => a - b)), 
                                (a, b) => a + b).ToArray());

            // check this!!!
            SumR =
                RITEs.Aggregate(SumR, (accumulator, it) =>
                                            accumulator.Zip(
                                            CoverageIdGULossMap[it].AmountByCOL
                                                                   .Where(x => ResidualSubject.CausesOfLoss.Contains(x.Key))
                                                                   .Select(x => x.Value)
                                                                   .Aggregate(accumulator, (accumulatorCOL, itCOL) => accumulatorCOL.Zip(itCOL, (a, b) => a + b).ToArray()),
                                                            (a, b) => a + b).ToArray()
                               );


            double[] RootNodeR = Allocation_[RootNode].R_vector;

            // C.1 Compute R_ for term children & allocate D

            Dictionary<TermNode, double[]> R_ForTermChildren = new Dictionary<TermNode, double[]>(Children.Count);

            foreach (TermNode ChildNode in Children)
            {
                TermExecutionPosition ChildNodeExecutionPosition = ExecutionRegister[ChildNode];

                double[] R = ChildNodeExecutionPosition.S_vector.Zip(ChildNodeExecutionPosition.X_vector, (a, b) => a - b)
                    .Zip(ChildNodeExecutionPosition.D_vector, (a, b) => a - b).ToArray();

                double[] R_ = Enumerable.Repeat(0.0d, R.Length).ToArray();

                if (!Allocation_.ContainsKey(ChildNode))
                    Allocation_.Add(ChildNode, new TermAllocationPositionVectorized(R_));

                TermAllocationPositionVectorized ChildNodeAllocationPosition = Allocation_[ChildNode];
                
                for (int i = 0; i < DeltaD.Length; i++)
                {
                    if (DeltaD[i] >= 0.0)
                    {
                        R_[i] = R[i] - DeltaD[i] * (R[i] / SumR[i]);
                        ChildNodeAllocationPosition.D_vector[i] = ChildNodeExecutionPosition.S_vector[i] 
                            - ChildNodeExecutionPosition.X_vector[i] - R_[i];
                    }
                    else
                    {
                        ChildNodeAllocationPosition.D_vector[i] = ChildNodeExecutionPosition.D_vector[i] 
                            + (ChildNodeExecutionPosition.D_vector[i] / SumD[i]) * DeltaD[i];
                        R_[i] = ChildNodeExecutionPosition.S_vector[i] - ChildNodeExecutionPosition.X_vector[i] 
                            - ChildNodeAllocationPosition.D_vector[i];
                    }
                }

                R_ForTermChildren.Add(ChildNode, R_);
            }

            // C.2 Compute R_ for RITE children

            Dictionary<long, Dictionary<string, double[]>> R_ForRITEChildren 
                = new Dictionary<long, Dictionary<string, double[]>>(RITEs.Count);

            foreach (long RITEId in RITEs)
            {
                Loss GULossForRITEId = CoverageIdGULossMap[RITEId];

                foreach (SymbolicValue COL in ResidualSubject.CausesOfLoss)
                {
                    // COL EQUIVALENCY CHANGES
                    if (!GULossForRITEId.ContainsCOL(COL))
                        continue;
                    if (!R_ForRITEChildren.ContainsKey(RITEId))
                        R_ForRITEChildren.Add(RITEId, new Dictionary<string, double[]>());
                    R_ForRITEChildren[RITEId].Add(COL.ToString(), 
                        GULossForRITEId.AmountByCOL[COL].ToArray());
                    // @FACTORCHANGES REPLACE
                    //R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                }
            }

            // D. Modify R_, optionally for children

            double[] RSum = Enumerable.Repeat(0.0d, DeltaD.Length).ToArray();

            RSum = R_ForTermChildren.Values.Aggregate(RSum, (accumulator, it) => accumulator.Zip(it, (a, b) => a + b).ToArray());

            RSum = R_ForRITEChildren.Values.SelectMany(x => x.Values)
                .Aggregate(RSum, (accumulator, it) => 
                    accumulator.Zip(it, (a, b) => a + b).ToArray());

            foreach (TermNode ChildNode in Children)
            {
                TermExecutionPosition ChildNodeExecutionPosition
                    = ExecutionRegister[ChildNode];

                double[] R = ChildNodeExecutionPosition.S_vector.Zip(ChildNodeExecutionPosition.X_vector, (a, b) => a - b)
                    .Zip(ChildNodeExecutionPosition.D_vector, (a, b) => a - b).ToArray(); ;

                for (int i = 0; i < DeltaD.Length; i++)
                {
                    if ((DeltaD[i] >= 0.0) && (RSum[i] == 0.0))
                    {
                        R_ForTermChildren[ChildNode][i] = R[i];
                    }
                }
            }

            RSum = Enumerable.Repeat(0.0d, DeltaD.Length).ToArray();

            RSum = R_ForTermChildren.Values.Aggregate(RSum, (accumulator, it) => accumulator.Zip(it, (a, b) => a + b).ToArray());

            RSum = R_ForRITEChildren.Values.SelectMany(x => x.Values)
                .Aggregate(RSum, (accumulator, it) => 
                    accumulator.Zip(it, (a, b) => a + b).ToArray());

            // E. Allocate R

            foreach (TermNode ChildNode in Children)
            {
                TermAllocationPositionVectorized ChildNodeAllocationPosition 
                    = Allocation_[ChildNode];

                for (int i = 0; i < RootNodeR.Length; i++)
                {
                    ChildNodeAllocationPosition.R_vector[i] =
                        (RootNodeR[i] == 0.0) ? 0.0 :
                        RootNodeR[i] * (R_ForTermChildren[ChildNode][i] / RSum[i]);
                }
            }

            foreach (long RITEId in RITEs)
            {
                if (!RITEAllocation_.ContainsKey(RITEId))
                    RITEAllocation_.Add(RITEId, new Dictionary<string, TermAllocationPositionVectorized>());
                Dictionary<string, double[]> R_ForRITEChildren_RITEId 
                    = R_ForRITEChildren[RITEId];
                foreach (string COL in R_ForRITEChildren_RITEId.Keys)
                {
                    if (!RITEAllocation_[RITEId].ContainsKey(COL))
                    {
                        double[] R_ForCOL_ForRITEId = new double[RootNodeR.Length];
                        for (int i = 0; i < RootNodeR.Length; i++)
                        {
                            R_ForCOL_ForRITEId[i] =
                                ((RootNodeR[i] == 0.0) ? 0.0 : 
                                RootNodeR[i] * R_ForRITEChildren_RITEId[COL][i] / RSum[i]);
                        }
                        RITEAllocation_[RITEId].Add(COL,
                                new TermAllocationPositionVectorized(R_ForCOL_ForRITEId,
                                    Enumerable.Repeat(0.0d, R_ForCOL_ForRITEId.Length).ToArray(), CoverageIdGULossMap[RITEId].FactorArray));
                    }
                }
            }

            // F. Recursively, allocate children

            foreach (TermNode ChildNode in Children)
            {
                AllocateSubtree_PerRisk((TermNode)ChildNode, Allocation_[(TermNode)ChildNode], 
                    Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
            }
        }

        private void AllocateSubtree_(TermNode RootNode, TermAllocationPositionVectorized AllocationPosition,
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
                        
            double SumD =
                Children.Aggregate(0.0, (accumulator, it) => accumulator + 
                    ExecutionRegister[(TermNode)it].D_vector.Sum());

            double DeltaD = AllocationPosition.D_vector.Sum() - SumD;

            double SumR = 
                Children.Aggregate(0.0, (accumulator, it)
                =>  accumulator + 
                    ExecutionRegister[(TermNode)it].S_vector.Sum()
                    - ExecutionRegister[(TermNode)it].D_vector.Sum()
                    - ExecutionRegister[(TermNode)it].X_vector.Sum())
                                    +
                   RITEs.Aggregate(0.0, (accumulator, it) =>
                                            accumulator +
                                            CoverageIdGULossMap[it].AmountByCOL
                                                                   .Where(x => ResidualSubject.CausesOfLoss.Contains(x.Key))
                                                                   .Select(x => x.Value.Sum())
                                                                   .Sum()
                               );

            double RootNodeR = Allocation_[RootNode].R_vector.Sum();

            // C.1 Compute R_ for term children & allocate D

            Dictionary<TermNode, double[]> R_ForTermChildren 
                = new Dictionary<TermNode, double[]>(Children.Count);

            foreach (TermNode ChildNode in Children)
            {
                TermExecutionPosition ChildNodeExecutionPosition 
                    = ExecutionRegister[ChildNode];

                double[] R = ChildNodeExecutionPosition.S_vector.Zip(ChildNodeExecutionPosition.X_vector, (a, b) => a - b)
                    .Zip(ChildNodeExecutionPosition.D_vector, (a, b) => a - b).ToArray();

                double[] R_ = Enumerable.Repeat(0.0d, R.Length).ToArray();

                if (!Allocation_.ContainsKey(ChildNode))
                    Allocation_.Add(ChildNode, new TermAllocationPositionVectorized(R_));

                TermAllocationPositionVectorized ChildNodeAllocationPosition 
                    = Allocation_[ChildNode];

                for (int i = 0; i < R_.Length; i++)
                {
                    if (DeltaD >= 0.0)
                    {
                        R_[i] = R[i] - DeltaD * (R[i] / SumR);
                        ChildNodeAllocationPosition.D_vector[i] = 
                            ChildNodeExecutionPosition.S_vector[i]
                            - ChildNodeExecutionPosition.X_vector[i] - R_[i];
                    }
                    else
                    {
                        ChildNodeAllocationPosition.D_vector[i] = 
                            ChildNodeExecutionPosition.D_vector[i]
                            + (ChildNodeExecutionPosition.D_vector[i] / SumD) * DeltaD;
                        R_[i] = ChildNodeExecutionPosition.S_vector[i] 
                            - ChildNodeExecutionPosition.X_vector[i]
                            - ChildNodeAllocationPosition.D_vector[i];
                    }
                }

                R_ForTermChildren.Add(ChildNode, R_);
            }

            // C.2 Compute R_ for RITE children

            Dictionary<long, Dictionary<string, double[]>> R_ForRITEChildren
                = new Dictionary<long, Dictionary<string, double[]>>(RITEs.Count);

            foreach (long RITEId in RITEs)
            {
                Loss GULossForRITEId = CoverageIdGULossMap[RITEId];

                foreach (SymbolicValue COL in ResidualSubject.CausesOfLoss)
                {
                    // COL EQUIVALENCY CHANGES
                    if (!GULossForRITEId.ContainsCOL(COL))
                        continue;
                    if (!R_ForRITEChildren.ContainsKey(RITEId))
                        R_ForRITEChildren.Add(RITEId, new Dictionary<string, double[]>());
                    R_ForRITEChildren[RITEId].Add(COL.ToString(), 
                        GULossForRITEId.AmountByCOL[COL].ToArray());
                    // @FACTORCHANGES REPLACE
                    //R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                }
            }

            // D. Modify R_, optionally for children

            double RSum = R_ForTermChildren.Values.Select(x => x.Sum()).Sum()
                                +
            R_ForRITEChildren.Values.SelectMany(x => x.Values).Select(x => x.Sum()).Sum();

            if ((DeltaD >= 0.0) && (RSum == 0.0))
            {
                foreach (TermNode ChildNode in Children)
                {
                    TermExecutionPosition ChildNodeExecutionPosition 
                        = ExecutionRegister[ChildNode];

                    double[] R = ChildNodeExecutionPosition.S_vector.Zip(ChildNodeExecutionPosition.X_vector, (a, b) => a - b)
                        .Zip(ChildNodeExecutionPosition.D_vector, (a, b) => a - b).ToArray(); ;

                    R_ForTermChildren[ChildNode] = R;
                }
                foreach (long RITEId in RITEs)
                {
                    Loss GULossForRITEId = CoverageIdGULossMap[RITEId];

                    foreach (SymbolicValue COL in GULossForRITEId.AmountByCOL.Keys)
                    {
                        if (!R_ForRITEChildren[RITEId].ContainsKey(COL.ToString()))
                            R_ForRITEChildren[RITEId].Add(COL.ToString(), 
                                GULossForRITEId.AmountByCOL[COL].ToArray());
                        else
                            R_ForRITEChildren[RITEId][COL.ToString()] = 
                                GULossForRITEId.AmountByCOL[COL].ToArray();
                        // @FACTORCHANGES REPLACE
                        //if (!R_ForRITEChildren[RITEId].ContainsKey(COL.ToString()))
                        //    R_ForRITEChildren[RITEId].Add(COL.ToString(), Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings));
                        //else
                        //    R_ForRITEChildren[RITEId][COL.ToString()] = Loss.WeightedSum(GULossForRITEId.AmountByCOL[COL], CoverageIdAttrMap[RITEId].NumBuildings);
                    }
                }
            }

            RSum = R_ForTermChildren.Values.Select(x => x.Sum()).Sum()
                                +
            R_ForRITEChildren.Values.SelectMany(x => x.Values).Select(x => x.Sum()).Sum();

            // E. Allocate R

            foreach (TermNode ChildNode in Children)
            {
                TermAllocationPositionVectorized ChildNodeAllocationPosition
                    = Allocation_[ChildNode];

                for (int i = 0; i < R_ForTermChildren[ChildNode].Length; i++)
                {
                    ChildNodeAllocationPosition.R_vector[i] =
                        (RootNodeR == 0.0) ? 0.0 :
                        RootNodeR * (R_ForTermChildren[ChildNode][i] / RSum);
                }
            }

            foreach (long RITEId in RITEs)
            {
                if (!RITEAllocation_.ContainsKey(RITEId))
                    RITEAllocation_.Add(RITEId, 
                        new Dictionary<string, TermAllocationPositionVectorized>());
                Dictionary<string, double[]> R_ForRITEChildren_RITEId
                    = R_ForRITEChildren[RITEId];
                foreach (string COL in R_ForRITEChildren_RITEId.Keys)
                {
                    if (!RITEAllocation_[RITEId].ContainsKey(COL))
                    {
                        double[] R_ForCOL_ForRITEId 
                            = new double[R_ForRITEChildren_RITEId[COL].Length];
                        for (int i = 0; i < R_ForRITEChildren_RITEId[COL].Length; i++)
                        {
                            R_ForCOL_ForRITEId[i] =
                                ((RootNodeR == 0.0) ? 0.0 :
                                RootNodeR * R_ForRITEChildren_RITEId[COL][i] / RSum);
                        }
                        RITEAllocation_[RITEId].Add(COL,
                                new TermAllocationPositionVectorized(R_ForCOL_ForRITEId,
                                    Enumerable.Repeat(0.0d, R_ForCOL_ForRITEId.Length).ToArray(), CoverageIdGULossMap[RITEId].FactorArray));
                    }
                }
            }

            // F. Recursively, allocate children

            foreach (TermNode ChildNode in Children)
            {
                if (ChildNode.IsMultiBuildingPerRisk())
                    AllocateSubtree_PerRisk((TermNode)ChildNode, Allocation_[(TermNode)ChildNode], 
                        Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
                else
                    AllocateSubtree_((TermNode)ChildNode, Allocation_[(TermNode)ChildNode],
                        Schedule, CoverageIdAttrMap, CoverageIdGULossMap);
            }
        }

        public void PopulateResidualSubjects()
        {
            foreach (TermNode _TermNode in this)
            {
                ResidualSubjectCache.Add((TermNode)_TermNode, GetResidualSubject((TermNode)_TermNode));
            }
        }

        private Subject GetResidualSubject(TermNode TermNode)
        {
            Subject ResidualSubject = TermNode.GetSubject();

            foreach (TermNode ChildTermNode in GetChildrenOfNode(TermNode))
            {
                ResidualSubject -= ChildTermNode.GetSubject();
            }

            return ResidualSubject;
        }

        private TermExecutionPosition GetResidualSubjectPosition(Subject ResidualSubject,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<long, Loss> CoverageIdGULossMap,
            bool IsMultiBuildingPerRisk)
        {
            TermExecutionPosition ResidualSubjectPosition = new TermExecutionPosition();

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

                        if (loss.ContainsCOL(Component2COL))
                        {
                            if (IsMultiBuildingPerRisk)
                            {
                                double[] _S = loss.GetSamplesForCOL(Component2COL).ToArray<double>();
                                ResidualSubjectPosition +=
                                    new TermExecutionPosition(_S,
                                        Enumerable.Repeat(0.0, _S.Length).ToArray(),
                                        Enumerable.Repeat(0.0, _S.Length).ToArray(),
                                        loss.FactorArray);
                                // @FACTORARRAY REPLACE
                                //ResidualSubjectPosition +=
                                //    new TermExecutionPosition(loss.GetSamplesForCOL(Component2COL).ToArray<double>(),
                                //        loss.Factor);
                            }
                            else
                            {
                                ResidualSubjectPosition.Coalesce();

                                ResidualSubjectPosition += new TermExecutionPosition(loss[Component2COL]);
                                // @FACTORCHANGES REPLACE
                                //ResidualSubjectPosition += new TermExecutionPosition(
                                //    Loss.WeightedSum(loss.AmountByCOL[Component2COL], CoverageIdAttrMap[Component2RITEId].NumBuildings));

                            }
                        }
                        // COL EQUIVALENCY CHANGES
                        //if (loss.AmountByCOL.ContainsKey(Component2COL))
                        //{
                        //    if (IsMultiBuildingPerRisk)
                        //    {
                        //        ResidualSubjectPosition += 
                        //            new TermExecutionPosition(loss.AmountByCOL[Component2COL].ToArray<double>(), 
                        //                loss.Factor);
                        //    }
                        //    else
                        //    {
                        //        ResidualSubjectPosition.Coalesce();
                            
                        //        ResidualSubjectPosition += new TermExecutionPosition(loss[Component2COL]);
                        //        // @FACTORCHANGES REPLACE
                        //        //ResidualSubjectPosition += new TermExecutionPosition(
                        //        //    Loss.WeightedSum(loss.AmountByCOL[Component2COL], CoverageIdAttrMap[Component2RITEId].NumBuildings));
                            
                        //    }
                        //}
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

        private Dictionary<SimpleExpression<SymbolicValue>, double> GetBindingsForNode(TermNode _TermNode,
            Dictionary<TermNode, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
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
            Dictionary<TermNode, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings)
        {
            #region Initialize execution & allocation positions for RITEs
            ConcurrentDictionary<long, Dictionary<string, TermExecutionPosition>> RITEExecutionPosition
                = new ConcurrentDictionary<long, Dictionary<string, TermExecutionPosition>>();
            ConcurrentDictionary<long, Dictionary<string, TermAllocationPositionVector>> RITEAllocationPositionVector
                = new ConcurrentDictionary<long, Dictionary<string, TermAllocationPositionVector>>();

            foreach (long RITEId in CoverageIdGULossMap.Keys)
            {
                if (!RITEExecutionPosition.ContainsKey(RITEId))
                    RITEExecutionPosition.TryAdd(RITEId, new Dictionary<string, TermExecutionPosition>());
                foreach (SymbolicValue COL in CoverageIdGULossMap[RITEId].AmountByCOL.Keys)
                {
                    RITEExecutionPosition[RITEId].Add(COL.ToString(), 
                        new TermExecutionPosition(CoverageIdGULossMap[RITEId].AmountByCOL[COL].ToArray<double>(), 
                            CoverageIdGULossMap[RITEId].Factor,
                            CoverageIdAttrMap[RITEId].NumBuildings));
                }
            }

            foreach (long RITEId in RITEExecutionPosition.Keys)
            {
                if (!RITEAllocationPositionVector.ContainsKey(RITEId))
                    RITEAllocationPositionVector.TryAdd(RITEId, new Dictionary<string, TermAllocationPositionVector>());
                foreach (string COL in RITEExecutionPosition[RITEId].Keys)
                {
                    int Count = RITEExecutionPosition[RITEId][COL].S_vector.Length;
                    List<TermAllocationPositionVector.Component> Components
                        = new List<TermAllocationPositionVector.Component>(Count);
                    for (int i = 0; i < Count; i++)
                    {
                        Components.Add(new TermAllocationPositionVector.Component(RITEExecutionPosition[RITEId][COL].S_vector[i], 0.0));
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

                foreach (TermNode ANode in this.Where(t => (TopoSort[t] == level)))
                {
                    #region B-1. Compute Subject Position for term from RITE Execution Positions of subject RITEs

                    TermExecutionPosition SubjectPosition = new TermExecutionPosition();

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
                                    SubjectPosition += new TermExecutionPosition(RITEExecutionPosition[Component2RITEId][Component2COL.ToString()]);
                                else
                                {
                                    SubjectPosition.Coalesce(); // @FACTORCHANGES : SubjectPosition.Coalesce(1); // TODO: is this unnecessary?
                                    TermExecutionPosition CoalescedSubjectRITEExecutionPosition =
                                        new TermExecutionPosition(RITEExecutionPosition[Component2RITEId][Component2COL.ToString()]);
                                    CoalescedSubjectRITEExecutionPosition.Coalesce();
                                    SubjectPosition += CoalescedSubjectRITEExecutionPosition;
                                    SubjectPosition.Coalesce(); // @FACTORCHANGES : SubjectPosition.Coalesce(1);
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

                    TermExecutionPosition ANodeResultPosition =
                        ANode.Execute(new List<TermExecutionPosition> { SubjectPosition }, BindingsForNode);
                    #endregion

                    #region B-3. Allocate to RITEs (for this level)

                    List<TermAllocationPositionVector.Component> ANodeAllocationPositionComponents =
                        new List<TermAllocationPositionVector.Component>(ANodeResultPosition.S_vector.Length);

                    for (int i = 0; i < ANodeResultPosition.S_vector.Length; i++)
                    {
                        ANodeAllocationPositionComponents.Add(new TermAllocationPositionVector.Component((ANodeResultPosition.S_vector[i]
                            - ANodeResultPosition.D_vector[i] - ANodeResultPosition.X_vector[i]),
                            ANodeResultPosition.D_vector[i]));
                    }

                    //foreach (TermExecutionPosition.Component TermExecutionPositionComponent in ANodeResultPosition.components)
                    //{
                    //    ANodeAllocationPositionComponents.Add(new TermAllocationPositionVector.Component((TermExecutionPositionComponent.S - TermExecutionPositionComponent.D - TermExecutionPositionComponent.X), 
                    //        TermExecutionPositionComponent.D));
                    //}

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
                                TermExecutionPosition RITEExecutionPositionForRITEIdForCOL_Copy
                                    = new TermExecutionPosition(RITEExecutionPosition[RITEId][COL]);

                                List<TermAllocationPositionVector.Component> TermAllocationPositionVectorComponents =
                                    new List<TermAllocationPositionVector.Component>(RITEExecutionPositionForRITEIdForCOL_Copy.S_vector.Length);

                                for (int i = 0; i < RITEExecutionPositionForRITEIdForCOL_Copy.S_vector.Length; i++)
                                {
                                    double R = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i]
                                        - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i] - RITEExecutionPositionForRITEIdForCOL_Copy.D_vector[i];

                                    double R_ = 0.0; double D_ = 0.0;

                                    if (DeltaD >= 0.0 && SumR > 0)
                                    {
                                        R_ = R - DeltaD * (R / SumR);
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i]
                                            - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i] - R_;
                                    }
                                    else if (DeltaD >= 0 && SumR == 0)
                                    {
                                        R_ = 0;
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i] 
                                            - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i] - R_;
                                    }
                                    else
                                    {
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.D_vector[i]
                                            + (RITEExecutionPositionForRITEIdForCOL_Copy.D_vector[i] / SumD) * DeltaD;
                                        R_ = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i]
                                            - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i] - D_;
                                    }

                                    TermAllocationPositionVectorComponents.Add(new TermAllocationPositionVector.Component(R_, D_));
                                }

                                ChildRITEAllocationPosition[RITEId].Add(COL, 
                                    new TermAllocationPositionVector(TermAllocationPositionVectorComponents, 
                                        RITEExecutionPositionForRITEIdForCOL_Copy.NumBuildings));

                                TermAllocationPositionVector TermAllocationPositionVectorComponentsCoalecsion =
                                    new TermAllocationPositionVector(TermAllocationPositionVectorComponents, 
                                        RITEExecutionPosition[RITEId][COL].NumBuildings);
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
                                    TermExecutionPosition RITEExecutionPositionForRITEIdForCOL_Copy
                                        = new TermExecutionPosition(RITEExecutionPosition[RITEId][COL]);

                                    for (int i = 0; i < RITEExecutionPositionForRITEIdForCOL_Copy.S_vector.Length; i++)
                                    {
                                        ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R
                                            = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i]
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i]
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.D_vector[i];
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
                                for (int i = 0; i < RITEExecutionPosition[RITEId][COL].S_vector.Length; i++)
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
                                TermExecutionPosition RITEExecutionPositionForRITEIdForCOL_Copy
                                    = new TermExecutionPosition(RITEExecutionPosition[RITEId][COL]);

                                List<TermAllocationPositionVector.Component> TermAllocationPositionVectorComponents =
                                    new List<TermAllocationPositionVector.Component>(RITEExecutionPositionForRITEIdForCOL_Copy.S_vector.Length);

                                for (int i = 0; i < RITEExecutionPositionForRITEIdForCOL_Copy.S_vector.Length; i++)
                                {
                                    double R = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i]
                                        - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i]
                                        - RITEExecutionPositionForRITEIdForCOL_Copy.D_vector[i];

                                    double R_ = 0.0; double D_ = 0.0;

                                    if (DeltaD[i] >= 0.0 && SumR[i] > 0)
                                    {
                                        R_ = R - DeltaD[i] * (R / SumR[i]);
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i]
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i]
                                                - R_;
                                    }
                                    else if (DeltaD[i] >= 0.0 && SumR[i] == 0)
                                    {
                                        R_ = 0;
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i]
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i]
                                                - R_;
                                    }
                                    else
                                    {
                                        D_ = RITEExecutionPositionForRITEIdForCOL_Copy.D_vector[i] + (RITEExecutionPositionForRITEIdForCOL_Copy.D_vector[i] / SumD[i]) * DeltaD[i];
                                        R_ = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i] - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i] - D_;
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
                                TermExecutionPosition RITEExecutionPositionForRITEIdForCOL_Copy
                                    = new TermExecutionPosition(RITEExecutionPosition[RITEId][COL]);

                                for (int i = 0; i < ANodeAllocationPosition.components.Count; i++)
                                {
                                    if ((DeltaD[i] >= 0.0) && (RSum[i] == 0.0))
                                    {
                                        ChildRITEAllocationPosition[RITEId][COL].components.ElementAt(i).R
                                            = RITEExecutionPositionForRITEIdForCOL_Copy.S_vector[i]
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.X_vector[i]
                                                - RITEExecutionPositionForRITEIdForCOL_Copy.D_vector[i];
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
                                for (int i = 0; i < RITEExecutionPosition[RITEId][COL].S_vector.Length; i++)
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
                        int Count = RITEAllocationPositionVector[RITEId][COL].components.Count;
                        double[] S = new double[Count];
                        double[] D = new double[Count];
                        double[] X = new double[Count];
                        for (int i = 0; i < Count; i++)
                        {
                            S[i] = RITEExecutionPosition[RITEId][COL].S_vector[i];
                            D[i] = RITEAllocationPositionVector[RITEId][COL].components.ElementAt(i).D;
                            X[i] = S[i] - D[i] - RITEAllocationPositionVector[RITEId][COL].components.ElementAt(i).R;
                        }
                        RITEExecutionPosition[RITEId][COL] = 
                            new TermExecutionPosition(S, D, X, 1.0, RITEAllocationPositionVector[RITEId][COL].NumBuildings);
                    }
                }
                #endregion

                level--;
            }

            #region Update RITEAllocation (regular, i.e. non-vectorized form)
            RITEAllocation = new Dictionary<long, Dictionary<string,TermAllocationPosition>>();
            RITEAllocation_ = new Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>>();
            foreach (long RITEId in RITEAllocationPositionVector.Keys)
            {
                if (!RITEAllocation.ContainsKey(RITEId))
                {
                    RITEAllocation.Add(RITEId, new Dictionary<string, TermAllocationPosition>());
                    RITEAllocation_.Add(RITEId, new Dictionary<string, TermAllocationPositionVectorized>());
                }
                foreach (string COL in RITEAllocationPositionVector[RITEId].Keys)
                {
                    RITEAllocationPositionVector[RITEId][COL].Coalesce(); // destructive coalescion
                    RITEAllocation[RITEId].Add(COL, new TermAllocationPosition(RITEAllocationPositionVector[RITEId][COL].R, RITEAllocationPositionVector[RITEId][COL].D));
                    RITEAllocation_[RITEId].Add(COL, 
                        new TermAllocationPositionVectorized(RITEAllocationPositionVector[RITEId][COL].R, RITEAllocationPositionVector[RITEId][COL].D));
                }
            }
            #endregion
        }
        
        public Dictionary<long, Dictionary<string, TermAllocationPosition>> GetAllocation()
        {
            return RITEAllocation;
        }

        public Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>> GetAllocation_()
        {
            return RITEAllocation_;
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
            TermNode _value;
            bool result = IdentityMap.TryGetValue(subject, out _value);
            value = _value;
            return result;
        }

        #endregion
    }
}
