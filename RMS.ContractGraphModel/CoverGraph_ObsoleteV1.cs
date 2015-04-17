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
    using IGenericCover = ICover<Value, Value, Value>;

    [Serializable]
    [ProtoContract]
    class CoverGraph_ObsoleteV1 : 
        DirectedGraph<IGenericCover>, 
        IEnumerable<INode<IGenericCover>>,
        IIdentifiableINodeCollection<SimpleExpression<SymbolicValue>, IGenericCover>
    {
        #region Fields

        [ProtoMember(1)]
        Dictionary<SimpleExpression<SymbolicValue>, CoverNode_ObsoleteV1> IdentityMap;

        [ProtoMember(2)]
        HashSet<CoverNode_ObsoleteV1> RootNodes;

        [ProtoMember(3)]
        HashSet<CoverNode_ObsoleteV1> LeafNodes;

        [ProtoMember(4)]
        Dictionary<CoverNode_ObsoleteV1, HashSet<long>> SubjectExposureMap = null;

        bool _CanUseTermRootsForCoverLeafSubjectExposures = false;
        public bool CanUseTermRootsForCoverLeafSubjectExposures
        {
            get { return _CanUseTermRootsForCoverLeafSubjectExposures; }
            private set { _CanUseTermRootsForCoverLeafSubjectExposures = value; }
        }
        Dictionary<CoverNode_ObsoleteV1, HashSet<TermNode>> SubjectExposureToTermRootsMap = null;
        Dictionary<CoverNode_ObsoleteV1, Subject> SubjectExposureToResidualSubjectMap = null;

        [ProtoMember(5)]
        public ConcurrentDictionary<CoverNode_ObsoleteV1, double> ExecutionRegister = null;

        [ProtoMember(6)]
        public ConcurrentDictionary<CoverNode_ObsoleteV1, double> Allocation = null;

        [ProtoMember(7)]
        public Dictionary<long, Dictionary<string, double>> RITEAllocation = null;

        [ProtoMember(8)]
        GraphOperationState ExecutionState;

        [ProtoMember(9)]
        GraphOperationState SubjectExposureMapConstructionState;

        #endregion

        #region Constructors
        public CoverGraph_ObsoleteV1() : base() 
        {
            IdentityMap
                = new Dictionary<SimpleExpression<SymbolicValue>, CoverNode_ObsoleteV1>();
            RootNodes = new HashSet<CoverNode_ObsoleteV1>();
            LeafNodes = new HashSet<CoverNode_ObsoleteV1>();
            ExecutionState = new GraphOperationState();
            SubjectExposureMapConstructionState = new GraphOperationState();
        }

        public CoverGraph_ObsoleteV1(CoverGraph_ObsoleteV1 CopyFromThisCoverGraph)
            : base(CopyFromThisCoverGraph)
        {
            this.IdentityMap = CopyFromThisCoverGraph.IdentityMap;
            this.RootNodes = CopyFromThisCoverGraph.RootNodes;
            this.LeafNodes = CopyFromThisCoverGraph.LeafNodes;
            this.ExecutionState = CopyFromThisCoverGraph.ExecutionState;
            this._CanUseTermRootsForCoverLeafSubjectExposures = CopyFromThisCoverGraph._CanUseTermRootsForCoverLeafSubjectExposures;
            this.SubjectExposureToTermRootsMap = CopyFromThisCoverGraph.SubjectExposureToTermRootsMap;
            this.SubjectExposureToResidualSubjectMap = CopyFromThisCoverGraph.SubjectExposureToResidualSubjectMap;
            this.SubjectExposureMapConstructionState = CopyFromThisCoverGraph.SubjectExposureMapConstructionState;
        }
        #endregion Constructors

        #region Graph Methods

        public bool Add(IGenericCover Cover, bool RebuildGraphAfterAdd = true)
        {
            SymbolicExpression NodeIdentity = Cover.GetLabel();

            if (NodeIdentity == null)
                NodeIdentity = new SymbolicExpression(Cover.GetHashCode().ToString());

            CoverNode_ObsoleteV1 _CoverNode = new CoverNode_ObsoleteV1(Cover);

            if (Contains(NodeIdentity))
                throw new ArgumentException("A cover node with the same identity (i.e. label) already exists in this cover graph!");

            bool IsAddSuccessful = base.Add(_CoverNode);

            if (IsAddSuccessful)
            {
                IdentityMap.Add(NodeIdentity, _CoverNode);
                // A newly added cover node (i.e. with no parent or child links) is both a root and a leaf, trivially.
                RootNodes.Add(_CoverNode);
                LeafNodes.Add(_CoverNode);
                ExecutionState.RegisterModificationInGraphTopology();
                SubjectExposureMapConstructionState.RegisterModificationInGraphTopology();
            }

            if (IsAddSuccessful && RebuildGraphAfterAdd)
                IsAddSuccessful &= Rebuild();

            return IsAddSuccessful;
        }

        public bool Add(IGenericCover Cover, Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<string, HashSet<long>> ResolvedSchedule, bool RebuildGraphAfterAdd = true)
        {
            SymbolicExpression NodeIdentity = Cover.GetLabel();

            if (NodeIdentity == null)
                NodeIdentity = new SymbolicExpression(Cover.GetHashCode().ToString());

            if (Contains(NodeIdentity))
                throw new ArgumentException("A cover node with the same identity (i.e. label) already exists in this cover graph!");

            Subject CoverSubject = (Subject)Cover.GetSubject();

            CoverNode_ObsoleteV1 _CoverNode = new CoverNode_ObsoleteV1(Cover);

            //Mark the Node for MultiBuilding Per Risk
            if (CoverSubject.PerRisk
                       && (CoverSubject.Schedule.ScheduleSymbols.Count == 1)
                       && (ResolvedSchedule.ContainsKey(CoverSubject.Schedule.ScheduleSymbols.First().ToString()))
                       && (ResolvedSchedule[CoverSubject.Schedule.ScheduleSymbols.First().ToString()]
                           .Any(x => CoverageIdAttrMap.ContainsKey(x) && CoverageIdAttrMap[x].NumBuildings > 1))
                  )
            {
                _CoverNode.MarkAsMultiBuildingPerRisk();
                CoverSubject.NumBuildings = CoverageIdAttrMap[ResolvedSchedule[CoverSubject.Schedule.ScheduleSymbols.First().ToString()].First()].NumBuildings;
                //foreach (string ScheduleSymbol in CoverSubject.Schedule.ScheduleSymbols.Select(x => x.ToString()))
                //{
                //    if (ResolvedSchedule.ContainsKey(ScheduleSymbol))
                //    {
                //        if (CoverageIdAttrMap.ContainsKey(ResolvedSchedule[ScheduleSymbol].First()))
                //            CoverSubject.NumBuildings += CoverageIdAttrMap[ResolvedSchedule[ScheduleSymbol].First()].NumBuildings;
                //    }
                //}
            }

            bool IsAddSuccessful = base.Add(_CoverNode);

            if (IsAddSuccessful)
            {
                IdentityMap.Add(NodeIdentity, _CoverNode);
                // A newly added cover node (i.e. with no parent or child links) is both a root and a leaf, trivially.
                RootNodes.Add(_CoverNode);
                LeafNodes.Add(_CoverNode);
                ExecutionState.RegisterModificationInGraphTopology();
                SubjectExposureMapConstructionState.RegisterModificationInGraphTopology();
            }

            if (IsAddSuccessful && RebuildGraphAfterAdd)
                IsAddSuccessful &= Rebuild();

            return IsAddSuccessful;
        }

        public bool Remove(IGenericCover Cover)
        {
            SymbolicExpression NodeIdentity = Cover.GetLabel();

            if (NodeIdentity == null)
                NodeIdentity = new SymbolicExpression(Cover.GetHashCode().ToString());

            CoverNode_ObsoleteV1 _CoverNode = new CoverNode_ObsoleteV1(Cover);

            if (Contains(NodeIdentity))
            {
                if (IdentityMap[NodeIdentity].Equals(_CoverNode))
                {
                    bool IsRemoveSuccessful = base.Remove(_CoverNode);
                    if (IsRemoveSuccessful)
                    {
                        IsRemoveSuccessful &= IdentityMap.Remove(NodeIdentity);
                        ExecutionState.RegisterModificationInGraphTopology();
                        SubjectExposureMapConstructionState.RegisterModificationInGraphTopology();
                    }
                    return IsRemoveSuccessful;
                }
                else
                {
                    throw new ArgumentException("Another cover with the same identity (i.e. label) is present in the cover graph!");
                }
            }

            return false;
        }

        public bool Rebuild()
        {
            bool IsRebuildSuccessful = true;

            foreach (CoverNode_ObsoleteV1 CoverNode in this)
            {
                FunctionInvocation<IValue<AValue>> DerivedSubject
                    = CoverNode.GetContent().GetDerivedSubject();

                if ((DerivedSubject != null) && (DerivedSubject.GetParameters() != null) && (DerivedSubject.GetParameters().Length != 0))
                {
                    object[] ChildCoverLabels = DerivedSubject.GetParameters();
                    foreach (SimpleExpression<SymbolicValue> ChildCoverLabel in ChildCoverLabels)
                    {
                        //SymbolicExpression ChildCoverLabel = (SymbolicExpression)__ChildCoverLabel;
                        if (Contains(ChildCoverLabel))
                        {
                            CoverNode_ObsoleteV1 ChildCoverNode = IdentityMap[ChildCoverLabel];
                            IsRebuildSuccessful &= MakeParentChildEdge(CoverNode, ChildCoverNode);
                            // RootNodes & LeafNodes update
                            if (IsRebuildSuccessful)
                            {
                                if ((GetParentsOfNode(CoverNode) == null) || ((GetParentsOfNode(CoverNode)).Count() == 0))
                                    RootNodes.Add(CoverNode);
                                RootNodes.Remove(ChildCoverNode);
                                if ((GetChildrenOfNode(ChildCoverNode) == null) || ((GetChildrenOfNode(ChildCoverNode)).Count() == 0))
                                    LeafNodes.Add(ChildCoverNode);
                                LeafNodes.Remove(CoverNode);
                            }
                        }
                    }
                }
            }

            ExecutionState.RegisterModificationInGraphTopology();
            SubjectExposureMapConstructionState.RegisterModificationInGraphTopology();

            return IsRebuildSuccessful;
        }

        #endregion

        #region API

        public Dictionary<long, Dictionary<string, double>> GetRITEAllocation()
        {
            return RITEAllocation;
        }
        
        public Dictionary<long, Loss> FilterExposedRITEs(Dictionary<string, HashSet<long>> Schedule,
           Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
           Dictionary<long, Loss> CoverageIdGULossMap)
        {
            Dictionary<long, Loss> ExposedRITECoverageIdLossMap = new Dictionary<long, Loss>();

            foreach (CoverNode_ObsoleteV1 LeafNode in LeafNodes)
            {
                Subject RootNodeSubject = (Subject)LeafNode.GetContent().GetSubject();

                // If any cover subject is unconstrained (i.e. entire contract subject is applicable), return without filtering
                if (RootNodeSubject.IsNotConstrained())
                    return CoverageIdGULossMap;

                foreach (string ScheduleSymbol in RootNodeSubject.Schedule.ScheduleSymbols.Select(ss => ss.ToString()).ToList())
                {
                    foreach (long RITECoverageId in Schedule[ScheduleSymbol])
                    {
                        if (ExposedRITECoverageIdLossMap.ContainsKey(RITECoverageId))
                            continue;
                        if ((CoverageIdGULossMap.ContainsKey(RITECoverageId))
                                && (CoverageIdAttrMap.ContainsKey(RITECoverageId))
                                && RootNodeSubject.ResolvedExposureTypes.Contains(CoverageIdAttrMap[RITECoverageId].ExposureType)
                                && RootNodeSubject.CausesOfLoss.ContainsAny(CoverageIdGULossMap[RITECoverageId].CausesOfLoss))
                            ExposedRITECoverageIdLossMap.Add(RITECoverageId, CoverageIdGULossMap[RITECoverageId]);
                    }
                }
            }

            return ExposedRITECoverageIdLossMap;
        }

        public bool TryCreateSubjectExposureForLeafNodesFromTermRootsMap(TermGraph TermGraph)
        {
            SubjectExposureToTermRootsMap = new Dictionary<CoverNode_ObsoleteV1, HashSet<TermNode>>();
            SubjectExposureToResidualSubjectMap = new Dictionary<CoverNode_ObsoleteV1, Subject>();

            foreach (CoverNode_ObsoleteV1 LeafCoverNode in LeafNodes)
            {
                SubjectExposureToTermRootsMap.Add(LeafCoverNode, new HashSet<TermNode>());
                Subject CoverLeafResidualSubject = (Subject)LeafCoverNode.GetSubject();
                foreach (TermNode RootTermNode in TermGraph.RootNodes)
                {
                    if (RootTermNode.GetSubject().IsSubsetOf((Subject)LeafCoverNode.GetSubject()))
                    {
                        SubjectExposureToTermRootsMap[LeafCoverNode].Add(RootTermNode);
                        CoverLeafResidualSubject -= RootTermNode.GetSubject();
                    }
                    else if (((Subject)LeafCoverNode.GetSubject()).IsSubsetOf(RootTermNode.GetSubject())
                                            ||
                        ((Subject)LeafCoverNode.GetSubject()).OverlapsWithoutInclusion((Subject)LeafCoverNode.GetSubject()))
                    {
                        SubjectExposureToTermRootsMap = null;
                        SubjectExposureToResidualSubjectMap = null;
                        return false;
                    }
                }
                SubjectExposureToResidualSubjectMap.Add(LeafCoverNode, CoverLeafResidualSubject);
            }

            _CanUseTermRootsForCoverLeafSubjectExposures = true;
            return true;
        }
        
        public void CreateSubjectExposureMapForLeafNodes(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap)
        {
            if (!SubjectExposureMapConstructionState.HasOperationStateChanged(Schedule, CoverageIdGULossMap))
                return;

            SubjectExposureMap = new Dictionary<CoverNode_ObsoleteV1, HashSet<long>>();

            foreach (CoverNode_ObsoleteV1 LeafNode in LeafNodes)
            {
                HashSet<long> RITEs = new HashSet<long>();
                Subject _Subject = (Subject)LeafNode.GetContent().GetSubject();

                foreach (SymbolicValue ScheduleSymbol in _Subject.Schedule.ScheduleSymbols)
                {
                    if (!Schedule.ContainsKey(ScheduleSymbol.ToString()))
                        continue;

                    RITEs.UnionWith(Schedule[ScheduleSymbol.ToString()].Where(RiteId => CoverageIdGULossMap.ContainsKey(RiteId)
                        && _Subject.CausesOfLoss.ContainsAny(CoverageIdGULossMap[RiteId].CausesOfLoss)
                        && _Subject.ResolvedExposureTypes.Contains(CoverageIdAttrMap[RiteId].ExposureType)));
                }

                SubjectExposureMap.Add(LeafNode, RITEs);
            }

            // Update subject exposure map construction state
            SubjectExposureMapConstructionState = 
                new GraphOperationState(false, new Dictionary<string, HashSet<long>>(Schedule), new Dictionary<long, Loss>(CoverageIdGULossMap));
        }

        public double Execute(TermGraph _TermGraph,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<SubjectPosition, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool ShouldAllocate = false)
        {
            Dictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocationAfterTermGraphExecution = _TermGraph.GetAllocation();

            double ContractPayout = 0.0;

            if (IdentityMap.Count == 0)
            {
                // Update execution state
                ExecutionState = new GraphOperationState(false);
                return 0.0;
            }

            ExecutionRegister = new ConcurrentDictionary<CoverNode_ObsoleteV1, double>();
            Allocation = new ConcurrentDictionary<CoverNode_ObsoleteV1, double>();

            foreach (CoverNode_ObsoleteV1 RootNode in RootNodes)
            {
                ContractPayout += ExecutionRegister.GetOrAdd(RootNode,
                    ExecuteCoverNode(RootNode, _TermGraph, CoverageIdGULossMap, 
                        CoverageIdAttrMap, RCVCoveredAndAffectedBySubject, Bindings, ShouldAllocate));
            }

            // Update execution state
            ExecutionState = new GraphOperationState(false);

            // Allocate?

            if (ShouldAllocate)
            {
                this.Allocate(RITEAllocationAfterTermGraphExecution);
                // The line below is a dummy; intended to breakpoint a debugging session so that one can view RITEAllocation (Hi Slava! :)
                //Console.WriteLine(RITEAllocation);
            }

            return ContractPayout;
        }

        private double ExecuteCoverNode(CoverNode_ObsoleteV1 _CoverNode,
            TermGraph _TermGraph,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<SubjectPosition, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool ShouldAllocate)
        {
            // Base case : _CoverNode is leaf

            if ((GetChildrenOfNode(_CoverNode) == null) || (GetChildrenOfNode(_CoverNode).Count == 0))
            {
                double _CoverNodeSubjectLoss = 0.0;
                Subject CoverNodeSubject = (Subject)_CoverNode.GetSubject();

                if (_CanUseTermRootsForCoverLeafSubjectExposures && !ShouldAllocate && !_TermGraph.IsOverlapping)
                {
                    foreach (TermNode RootTermNode in SubjectExposureToTermRootsMap[_CoverNode])
                    {
                        TermExecutionPosition RootTermNodeExecutionPosition = _TermGraph.ExecutionRegister[RootTermNode];
                        RootTermNodeExecutionPosition.Coalesce(RootTermNode.GetSubject().NumBuildings);
                        _CoverNodeSubjectLoss += (RootTermNodeExecutionPosition.S - RootTermNodeExecutionPosition.D - RootTermNodeExecutionPosition.X);
                    }

                    Subject CoverNodeResidualSubject = SubjectExposureToResidualSubjectMap[_CoverNode];
                    foreach (KeyValuePair<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> ComponentsKVP
                        in CoverNodeResidualSubject.GetComponents())
                    {
                        foreach (KeyValuePair<int, Tuple<HashSet<long>, HashSet<long>>> ComponentsByExposureTypeKVP
                            in ComponentsKVP.Value)
                        {
                            foreach (long RITEId in ComponentsByExposureTypeKVP.Value.Item1)
                            {
                                if (!CoverageIdGULossMap.ContainsKey(RITEId))
                                    continue;
                                if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(ComponentsKVP.Key))
                                    _CoverNodeSubjectLoss += Loss.WeightedSum(CoverageIdGULossMap[RITEId].AmountByCOL[ComponentsKVP.Key],
                                        CoverageIdAttrMap[RITEId].NumBuildings);
                            }
                        }
                    }
                }
                else
                {
                    Dictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocationAfterTermGraphExecution = _TermGraph.GetAllocation();

                    foreach (long RITEId in RITEAllocationAfterTermGraphExecution.Keys.Where(it => SubjectExposureMap[_CoverNode].Contains(it)))
                    {
                        foreach (string COL in RITEAllocationAfterTermGraphExecution[RITEId].Keys)
                        {
                            if (CoverNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                                _CoverNodeSubjectLoss += RITEAllocationAfterTermGraphExecution[RITEId][COL].R;
                        }
                    }
                }

                CoverNodeSubject = (Subject)_CoverNode.GetSubject();
                Bindings.Remove(new SymbolicExpression("RCVCovered"));
                Bindings.Add(new SymbolicExpression("RCVCovered"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item1);
                Bindings.Remove(new SymbolicExpression("RCVAffected"));
                Bindings.Add(new SymbolicExpression("RCVAffected"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item2);
                Bindings.Remove(new SymbolicExpression("Subject"));
                Bindings.Add(new SymbolicExpression("Subject"), _CoverNodeSubjectLoss);

                return _CoverNode.Execute(Bindings);
            }

            // Recursive case

            // A. Go through each child node: (a) try to register for execution; (b) if successful, execute

            HashSet<Task<double>> ChildCoverNodeTasks = new HashSet<Task<double>>();
            foreach (CoverNode_ObsoleteV1 _ChildCoverNode in GetChildrenOfNode(_CoverNode))
            {
                ExecutionRegister.GetOrAdd(_ChildCoverNode,
                    ExecuteCoverNode(_ChildCoverNode, _TermGraph, CoverageIdGULossMap, 
                        CoverageIdAttrMap, RCVCoveredAndAffectedBySubject, Bindings, ShouldAllocate));
            }

            // B. Form subject

            double SubjectPosition = 0.0;

            Dictionary<SimpleExpression<SymbolicValue>, double> ChildCoverPayoutBindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();
            foreach (CoverNode_ObsoleteV1 _ChildCoverNode in GetChildrenOfNode(_CoverNode))
            {
                ChildCoverPayoutBindings.Add(_ChildCoverNode.GetContent().GetLabel(), ExecutionRegister[_ChildCoverNode]);
            }

            FunctionInvocation<IValue<AValue>> DerivedSubject
                    = _CoverNode.GetContent().GetDerivedSubject();

            SubjectPosition = DerivedSubject.GetEvaluatedValue(ChildCoverPayoutBindings);

            Bindings.Remove(new SymbolicExpression("RCVCovered"));
            Bindings.Remove(new SymbolicExpression("RCVAffected"));
            Bindings.Remove(new SymbolicExpression("Subject"));
            Bindings.Add(new SymbolicExpression("Subject"), SubjectPosition);

            return _CoverNode.Execute(Bindings);
        }

        //#region Nina
        //private double ExecuteCoverNode(CoverNode _CoverNode,
        //    TermGraphVectorized _TermGraph,
        //    Dictionary<long, Loss> CoverageIdGULossMap,
        //    Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
        //    Dictionary<SubjectPosition, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
        //    Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool ShouldAllocate)
        //{
        //    // Base case : _CoverNode is leaf
        //    #region MuiltiBuilding Per Risk
        //    if(_CoverNode.IsMultiBuildingPerRisk())
        //    {
        //        if ((GetChildrenOfNode(_CoverNode) == null) || (GetChildrenOfNode(_CoverNode).Count == 0))
        //        {
        //            //double[] _CoverNodeSubjectLoss = ;
        //            Subject CoverNodeSubject = (Subject)_CoverNode.GetSubject();
        //             double[] _CoverNodeSubjectLoss; 

        //            if (_CanUseTermRootsForCoverLeafSubjectExposures && !ShouldAllocate && !_TermGraph.IsOverlapping)
        //            {
        //                foreach (TermNodeVectorized RootTermNode in SubjectExposureToTermRootsMap[_CoverNode])
        //                {
        //                    TermExecutionPositionVectorized RootTermNodeExecutionPosition = _TermGraph.ExecutionRegister[RootTermNode];
        //                    _CoverNodeSubjectLoss = (RootTermNodeExecutionPosition.S_vector).Zip(RootTermNodeExecutionPosition.D_vector, (a, b) => (a - b)).ToArray();
        //                    _CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(RootTermNodeExecutionPosition.X_vector, (a, b) => (a - b)).ToArray();
        //                }

        //                Subject CoverNodeResidualSubject = SubjectExposureToResidualSubjectMap[_CoverNode];
        //                foreach (KeyValuePair<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> ComponentsKVP
        //                    in CoverNodeResidualSubject.GetComponents())
        //                {
        //                    foreach (KeyValuePair<int, Tuple<HashSet<long>, HashSet<long>>> ComponentsByExposureTypeKVP
        //                        in ComponentsKVP.Value)
        //                    {
        //                        foreach (long RITEId in ComponentsByExposureTypeKVP.Value.Item1)
        //                        {
        //                            if (!CoverageIdGULossMap.ContainsKey(RITEId))
        //                                continue;
        //                            if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(ComponentsKVP.Key))
        //                                (CoverageIdGULossMap[RITEId].AmountByCOL[ComponentsKVP.Key]).ToArray();
        //                                //_CoverNodeSubjectLoss += Loss.WeightedSum(CoverageIdGULossMap[RITEId].AmountByCOL[ComponentsKVP.Key],
        //                                //    CoverageIdAttrMap[RITEId].NumBuildings);
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                Dictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocationAfterTermGraphExecution = _TermGraph.GetAllocation();

        //                foreach (long RITEId in RITEAllocationAfterTermGraphExecution.Keys.Where(it => SubjectExposureMap[_CoverNode].Contains(it)))
        //                {
        //                    foreach (string COL in RITEAllocationAfterTermGraphExecution[RITEId].Keys)
        //                    {
        //                        if (CoverNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
        //                            _CoverNodeSubjectLoss = RITEAllocationAfterTermGraphExecution[RITEId][COL].R;
        //                            //_CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(  , (a,b) => (a+b));

        //                    }
        //                }
        //            }

        //            CoverNodeSubject = (Subject)_CoverNode.GetSubject();
        //            Bindings.Remove(new SymbolicExpression("RCVCovered"));
        //            Bindings.Add(new SymbolicExpression("RCVCovered"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item1);
        //            Bindings.Remove(new SymbolicExpression("RCVAffected"));
        //            Bindings.Add(new SymbolicExpression("RCVAffected"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item2);
        //            Bindings.Remove(new SymbolicExpression("Subject"));
        //            Bindings.Add(new SymbolicExpression("Subject"), _CoverNodeSubjectLoss);

        //            return _CoverNode.Execute(Bindings);
        //        }

        //            //Recursive case

        //        // A. Go through each child node: (a) try to register for execution; (b) if successful, execute

        //        HashSet<Task<double>> ChildCoverNodeTasks = new HashSet<Task<double>>();
        //        foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
        //        {
        //            ExecutionRegister.GetOrAdd(_ChildCoverNode,
        //                ExecuteCoverNode(_ChildCoverNode, _TermGraph, CoverageIdGULossMap,
        //                    CoverageIdAttrMap, RCVCoveredAndAffectedBySubject, Bindings, ShouldAllocate));
        //        }

        //        // B. Form subject

        //        double SubjectPosition = 0.0;

        //        Dictionary<SimpleExpression<SymbolicValue>, double> ChildCoverPayoutBindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();
        //        foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
        //        {
        //            ChildCoverPayoutBindings.Add(_ChildCoverNode.GetContent().GetLabel(), ExecutionRegister[_ChildCoverNode]);
        //        }

        //        FunctionInvocation<IValue<AValue>> DerivedSubject
        //                = _CoverNode.GetContent().GetDerivedSubject();

        //        SubjectPosition = DerivedSubject.GetEvaluatedValue(ChildCoverPayoutBindings);

        //        Bindings.Remove(new SymbolicExpression("RCVCovered"));
        //        Bindings.Remove(new SymbolicExpression("RCVAffected"));
        //        Bindings.Remove(new SymbolicExpression("Subject"));
        //        Bindings.Add(new SymbolicExpression("Subject"), SubjectPosition);

        //        return _CoverNode.Execute(Bindings);

        //    }
        //    #endregion 

        //    #region Single or Summed 
        //    else 
        //    {
        //        if ((GetChildrenOfNode(_CoverNode) == null) || (GetChildrenOfNode(_CoverNode).Count == 0))
        //        {
        //            double _CoverNodeSubjectLoss = 0.0;
        //            Subject CoverNodeSubject = (Subject)_CoverNode.GetSubject();

        //            if (_CanUseTermRootsForCoverLeafSubjectExposures && !ShouldAllocate && !_TermGraph.IsOverlapping)
        //            {
        //                foreach (TermNodeVectorized RootTermNode in SubjectExposureToTermRootsMap[_CoverNode])
        //                {
        //                    TermExecutionPositionVectorized RootTermNodeExecutionPosition = _TermGraph.ExecutionRegister[RootTermNode];
        //                    RootTermNodeExecutionPosition.Coalesce(RootTermNode.GetSubject().NumBuildings);
        //                    _CoverNodeSubjectLoss += (RootTermNodeExecutionPosition.S - RootTermNodeExecutionPosition.D - RootTermNodeExecutionPosition.X);
        //                }

        //                Subject CoverNodeResidualSubject = SubjectExposureToResidualSubjectMap[_CoverNode];
        //                foreach (KeyValuePair<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> ComponentsKVP
        //                    in CoverNodeResidualSubject.GetComponents())
        //                {
        //                    foreach (KeyValuePair<int, Tuple<HashSet<long>, HashSet<long>>> ComponentsByExposureTypeKVP
        //                        in ComponentsKVP.Value)
        //                    {
        //                        foreach (long RITEId in ComponentsByExposureTypeKVP.Value.Item1)
        //                        {
        //                            if (!CoverageIdGULossMap.ContainsKey(RITEId))
        //                                continue;
        //                            if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(ComponentsKVP.Key))
        //                                _CoverNodeSubjectLoss += Loss.WeightedSum(CoverageIdGULossMap[RITEId].AmountByCOL[ComponentsKVP.Key],
        //                                    CoverageIdAttrMap[RITEId].NumBuildings);
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                Dictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocationAfterTermGraphExecution = _TermGraph.GetAllocation();

        //                foreach (long RITEId in RITEAllocationAfterTermGraphExecution.Keys.Where(it => SubjectExposureMap[_CoverNode].Contains(it)))
        //                {
        //                    foreach (string COL in RITEAllocationAfterTermGraphExecution[RITEId].Keys)
        //                    {
        //                        if (CoverNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
        //                            _CoverNodeSubjectLoss += RITEAllocationAfterTermGraphExecution[RITEId][COL].R;
        //                    }
        //                }
        //            }

        //            CoverNodeSubject = (Subject)_CoverNode.GetSubject();
        //            Bindings.Remove(new SymbolicExpression("RCVCovered"));
        //            Bindings.Add(new SymbolicExpression("RCVCovered"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item1);
        //            Bindings.Remove(new SymbolicExpression("RCVAffected"));
        //            Bindings.Add(new SymbolicExpression("RCVAffected"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item2);
        //            Bindings.Remove(new SymbolicExpression("Subject"));
        //            Bindings.Add(new SymbolicExpression("Subject"), _CoverNodeSubjectLoss);

        //            return _CoverNode.Execute(Bindings);
        //        }

        //        // Recursive case

        //        // A. Go through each child node: (a) try to register for execution; (b) if successful, execute

        //        HashSet<Task<double>> ChildCoverNodeTasks = new HashSet<Task<double>>();
        //        foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
        //        {
        //            ExecutionRegister.GetOrAdd(_ChildCoverNode,
        //                ExecuteCoverNode(_ChildCoverNode, _TermGraph, CoverageIdGULossMap,
        //                    CoverageIdAttrMap, RCVCoveredAndAffectedBySubject, Bindings, ShouldAllocate));
        //        }

        //        // B. Form subject

        //        double SubjectPosition = 0.0;

        //        Dictionary<SimpleExpression<SymbolicValue>, double> ChildCoverPayoutBindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();
        //        foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
        //        {
        //            ChildCoverPayoutBindings.Add(_ChildCoverNode.GetContent().GetLabel(), ExecutionRegister[_ChildCoverNode]);
        //        }

        //        FunctionInvocation<IValue<AValue>> DerivedSubject
        //                = _CoverNode.GetContent().GetDerivedSubject();

        //        SubjectPosition = DerivedSubject.GetEvaluatedValue(ChildCoverPayoutBindings);

        //        Bindings.Remove(new SymbolicExpression("RCVCovered"));
        //        Bindings.Remove(new SymbolicExpression("RCVAffected"));
        //        Bindings.Remove(new SymbolicExpression("Subject"));
        //        Bindings.Add(new SymbolicExpression("Subject"), SubjectPosition);

        //        return _CoverNode.Execute(Bindings);
        //    }
        //    #endregion
        //}
        //#endregion

        public double Execute(double SubjectLoss, 
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool Allocate = true)
        {
            double ContractPayout = 0.0;

            if (!ExecutionState.HasOperationStateChanged(SubjectLoss))
            {
                foreach (CoverNode_ObsoleteV1 RootNode in RootNodes)
                {
                    ContractPayout += ExecutionRegister[RootNode];
                }

                return ContractPayout;
            }

            if (IdentityMap.Count == 0)
            {
                // Update execution state
                ExecutionState = new GraphOperationState(false, SubjectLoss);
                return 0.0;
            }

            ExecutionRegister = new ConcurrentDictionary<CoverNode_ObsoleteV1, double>();
            Allocation = new ConcurrentDictionary<CoverNode_ObsoleteV1, double>();

            // Prepare a set of every termnode
            HashSet<CoverNode_ObsoleteV1> CoverNodes = new HashSet<CoverNode_ObsoleteV1>(IdentityMap.Values);

            // Go through each node: (a) try to register for execution; (b) if successful, execute
            HashSet<Task> CoverNodeTasks = new HashSet<Task>();
            foreach (CoverNode_ObsoleteV1 _CoverNode in CoverNodes)
            {
                ExecutionRegister.GetOrAdd(_CoverNode, ExecuteCoverNode(_CoverNode, Bindings, SubjectLoss));
            }

            // Update execution state
            ExecutionState = new GraphOperationState(false, SubjectLoss);

            ContractPayout = 0.0;

            foreach (CoverNode_ObsoleteV1 RootNode in RootNodes)
            {
                ContractPayout += ExecutionRegister[RootNode];
            }

            // Allocate?

            if (Allocate)
            {
                //TODO: this.Allocate(SubjectLoss);
                // The line below is a dummy; intended to breakpoint a debugging session so that one can view RITEAllocation (Hi Slava! :)
                //Console.WriteLine(RITEAllocation);
            }

            return ContractPayout;
        }

        private double ExecuteCoverNode(CoverNode_ObsoleteV1 _CoverNode, 
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, double SubjectLoss)
        {
            // Base case : _CoverNode is leaf

            if ((GetChildrenOfNode(_CoverNode) == null) || (GetChildrenOfNode(_CoverNode).Count == 0))
            {
                Bindings.Remove(new SymbolicExpression("Subject"));
                Bindings.Add(new SymbolicExpression("Subject"), SubjectLoss);

                return _CoverNode.Execute(Bindings);
            }

            // Recursive case

            // A. Go through each child node: (a) try to register for execution; (b) if successful, execute

            HashSet<Task<double>> ChildCoverNodeTasks = new HashSet<Task<double>>();
            foreach (CoverNode_ObsoleteV1 _ChildCoverNode in GetChildrenOfNode(_CoverNode))
            {
                ExecutionRegister.GetOrAdd(_ChildCoverNode, ExecuteCoverNode(_ChildCoverNode, Bindings, SubjectLoss));
            }

            double SubjectPosition = 0.0;

            Dictionary<SimpleExpression<SymbolicValue>, double> ChildCoverPayoutBindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();
            foreach (CoverNode_ObsoleteV1 _ChildCoverNode in GetChildrenOfNode(_CoverNode))
            {
                ChildCoverPayoutBindings.Add(_ChildCoverNode.GetContent().GetLabel(), ExecutionRegister[_ChildCoverNode]);
            }

            FunctionInvocation<IValue<AValue>> DerivedSubject
                    = _CoverNode.GetContent().GetDerivedSubject();

            SubjectPosition = DerivedSubject.GetEvaluatedValue(ChildCoverPayoutBindings);

            Bindings.Remove(new SymbolicExpression("Subject"));
            Bindings.Add(new SymbolicExpression("Subject"), SubjectPosition);

            return _CoverNode.Execute(Bindings);
        }

        public void Allocate(Dictionary<long, Dictionary<string, TermAllocationPosition>> TermRITEAllocation)
        {
            if (ExecutionState.HasOperationStateChanged())
                throw new Exception("Cover Graph not yet executed or has changed since last execution!");

            RITEAllocation = new Dictionary<long, Dictionary<string, double>>();

            // Allocate all root nodes
            foreach (CoverNode_ObsoleteV1 RootNode in RootNodes)
            {
                // Allocate root nodes trivially
                if (!Allocation.ContainsKey(RootNode))
                    Allocation.TryAdd(RootNode, ExecutionRegister[RootNode]);

                AllocateSubtree(RootNode, ExecutionRegister[RootNode], TermRITEAllocation);
            }
        }

        private void AllocateSubtree(CoverNode_ObsoleteV1 RootNode, double AllocatedAtRoot,
            Dictionary<long, Dictionary<string, TermAllocationPosition>> TermRITEAllocation)
        {
            ICollection<INode<IGenericCover>> Children = GetChildrenOfNode(RootNode);

            if ((Children != null) && (Children.Count > 0))
            {
                if (RootNode.GetContent().GetDerivedSubject().GetFunctionName().ToUpper().Equals("SUM"))
                {
                    double TotalChildrenPayout = Children.Aggregate(0.0, (accumulator, it) => accumulator + ExecutionRegister[(CoverNode_ObsoleteV1)it]);
                    foreach (CoverNode_ObsoleteV1 ChildCoverNode in Children)
                    {
                        double ChildCoverNodeAllocation = AllocatedAtRoot * (ExecutionRegister[ChildCoverNode] / TotalChildrenPayout);
                        if (!Allocation.ContainsKey(ChildCoverNode))
                            Allocation.TryAdd(ChildCoverNode, ChildCoverNodeAllocation);

                        AllocateSubtree(ChildCoverNode, ChildCoverNodeAllocation, TermRITEAllocation);
                    }
                }
                else if (RootNode.GetContent().GetDerivedSubject().GetFunctionName().ToUpper().Equals("MAX"))
                {
                    HashSet<CoverNode_ObsoleteV1> MaxChildCovers = new HashSet<CoverNode_ObsoleteV1>();

                    // Find MAX contributors
                    double MaxPayout = double.MinValue;
                    foreach (CoverNode_ObsoleteV1 ChildCoverNode in Children)
                    {
                        if (ExecutionRegister[ChildCoverNode] > MaxPayout)
                        {
                            MaxPayout = ExecutionRegister[ChildCoverNode];
                            MaxChildCovers.Clear();
                            MaxChildCovers.Add(ChildCoverNode);
                        }
                        else if (ExecutionRegister[ChildCoverNode] == MaxPayout)
                        {
                            MaxChildCovers.Add(ChildCoverNode);
                        }
                    }

                    // Allocate to MAX contributors
                    foreach (CoverNode_ObsoleteV1 ChildCoverNode in Children)
                    {
                        if (MaxChildCovers.Contains(ChildCoverNode))
                        {
                            double ChildCoverNodeAllocation = AllocatedAtRoot / MaxChildCovers.Count;
                            if (!Allocation.ContainsKey(ChildCoverNode))
                                Allocation.TryAdd(ChildCoverNode, ChildCoverNodeAllocation);
                            AllocateSubtree(ChildCoverNode, ChildCoverNodeAllocation, TermRITEAllocation);
                        }
                        else
                        {
                            if (!Allocation.ContainsKey(ChildCoverNode))
                                Allocation.TryAdd(ChildCoverNode, 0.0);
                            AllocateSubtree(ChildCoverNode, 0.0, TermRITEAllocation);
                        }
                    }
                }
                else if (RootNode.GetContent().GetDerivedSubject().GetFunctionName().ToUpper().Equals("MIN"))
                {
                    HashSet<CoverNode_ObsoleteV1> MinChildCovers = new HashSet<CoverNode_ObsoleteV1>();

                    // Find MIN contributors
                    double MinPayout = double.MaxValue;
                    foreach (CoverNode_ObsoleteV1 ChildCoverNode in Children)
                    {
                        if (ExecutionRegister[ChildCoverNode] < MinPayout)
                        {
                            MinPayout = ExecutionRegister[ChildCoverNode];
                            MinChildCovers.Clear();
                            MinChildCovers.Add(ChildCoverNode);
                        }
                        else if (ExecutionRegister[ChildCoverNode] == MinPayout)
                        {
                            MinChildCovers.Add(ChildCoverNode);
                        }
                    }

                    // Allocate to MIN contributors
                    foreach (CoverNode_ObsoleteV1 ChildCoverNode in Children)
                    {
                        if (MinChildCovers.Contains(ChildCoverNode))
                        {
                            double ChildCoverNodeAllocation = AllocatedAtRoot / MinChildCovers.Count;
                            if (!Allocation.ContainsKey(ChildCoverNode))
                                Allocation.TryAdd(ChildCoverNode, ChildCoverNodeAllocation);
                            AllocateSubtree(ChildCoverNode, ChildCoverNodeAllocation, TermRITEAllocation);
                        }
                        else
                        {
                            if (!Allocation.ContainsKey(ChildCoverNode))
                                Allocation.TryAdd(ChildCoverNode, 0.0);
                            AllocateSubtree(ChildCoverNode, 0.0, TermRITEAllocation);
                        }
                    }
                }
            }
            else
            {
                // compute sum of term allocations to subject (exposed) RITEs

                SubjectPosition RootNodeSubject = RootNode.GetSubject();

                double TotalExposedRITEAllocation = 0.0;

                foreach (long RITEId in TermRITEAllocation.Keys.Where(x => SubjectExposureMap[RootNode].Contains(x)))
                {
                    foreach (string COL in TermRITEAllocation[RITEId].Keys)
                    {
                        if (RootNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                        {
                            TotalExposedRITEAllocation += TermRITEAllocation[RITEId][COL].R;
                        }
                    }
                }

                foreach (long RITEId in SubjectExposureMap[RootNode])
                {
                    if (!RITEAllocation.ContainsKey(RITEId))
                        RITEAllocation.Add(RITEId, new Dictionary<string, double>());

                    foreach (string COL in TermRITEAllocation[RITEId].Keys)
                    {
                        if (RootNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                        {
                            if (!RITEAllocation[RITEId].ContainsKey(COL))
                                RITEAllocation[RITEId].Add(COL, AllocatedAtRoot * TermRITEAllocation[RITEId][COL].R / TotalExposedRITEAllocation);
                            else
                                RITEAllocation[RITEId][COL] = RITEAllocation[RITEId][COL] + (AllocatedAtRoot * TermRITEAllocation[RITEId][COL].R / TotalExposedRITEAllocation);
                        }
                    }
                }
            }
        }

        #endregion

        #region IIdentifiableINodeCollection Overrides

        public bool Contains(SimpleExpression<SymbolicValue> Label)
        {
            return IdentityMap.ContainsKey(Label);
        }

        public INode<IGenericCover> this[SimpleExpression<SymbolicValue> Label]
        {
            get { return IdentityMap[Label]; }
        }

        public bool TryGetValue(SimpleExpression<SymbolicValue> Label, out INode<IGenericCover> value)
        {
            CoverNode_ObsoleteV1 _value;
            bool result = IdentityMap.TryGetValue(Label, out _value);
            value = _value;
            return result;
        }

        #endregion
    }

    class PerRiskCoverExploder_ObsoleteV1
    {
        List<WrappedCover> CoverList;
        Dictionary<string, HashSet<long>> ResolvedSchedule;
        HashSet<IGenericCover> ExplodedCoversCollection;
        Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap;

        #region Constructor
        public PerRiskCoverExploder_ObsoleteV1(List<WrappedCover> _CoverList, Dictionary<long, RiskItemCharacteristicIDAttributes> _CoverageIdAttrMap,
                        Dictionary<string, HashSet<long>> _ResolvedSchedule)
           
        {
            CoverList = _CoverList;
            ResolvedSchedule = _ResolvedSchedule;
            CoverageIdAttrMap = _CoverageIdAttrMap;
        }
        #endregion

        #region API

        public HashSet<IGenericCover> Explode()
        {
            ExplodedCoversCollection = new HashSet<IGenericCover>();
            foreach (WrappedCover WrappedCover in CoverList)
            {
                if(!WrappedCover.IsExploded)
                    ExplodedCoversCollection.UnionWith(Process(WrappedCover));
            }
            return ExplodedCoversCollection;
        }

        private HashSet<IGenericCover> Process(WrappedCover Cover)
        {
            if (Cover.IsDerived)
            {
                //First Check if all the children has been added or exploded
                foreach (SymbolicExpression ChildCoverLabel in Cover.ChildCoverLabels)
                {
                    WrappedCover ChildCover = CoverList.Find(cover => cover.CoverLabel.Equals(ChildCoverLabel));
                    if (!ChildCover.IsExploded)
                    {
                        ExplodedCoversCollection.UnionWith(Process(ChildCover));
                    }
                }
                if (Cover.IsPerRisk)
                {
                    ExplodeDerivedCover(Cover, CoverList);
                    Cover.IsExploded = true;
                    return Cover.ExplodedCovers;
                }
                else
                {
                    HashSet<SymbolicExpression> ExplodedChildCoverLabels = new HashSet<SymbolicExpression>();
                    foreach (SymbolicExpression ChildCoverLabel in Cover.ChildCoverLabels)
                    {
                        WrappedCover ChildCover = CoverList.Find(cover => cover.CoverLabel.Equals(ChildCoverLabel));
                        if (ChildCover.IsPerRisk)
                        {
                            foreach (SymbolicExpression ExplodedChildLabel in ChildCover.ExplodedCoverLabels)
                            {
                                ExplodedChildCoverLabels.Add(ExplodedChildLabel);
                            }
                        }
                        else
                        {
                            ExplodedChildCoverLabels.Add(ChildCover.CoverLabel);
                        }
                    }
                    object[] NewParameters = BuildNewParameters(ExplodedChildCoverLabels);

                    FunctionInvocation<IValue<AValue>> NewDerivedSubject = (FunctionInvocation<IValue<AValue>>)Activator.CreateInstance
                    (Cover.DerivedSubject.GetType(), Cover.DerivedSubject, NewParameters);

                    IGenericCover ClonedCover = (IGenericCover)Activator.CreateInstance(Cover.GetOriginalCover().GetType(), Cover.GetOriginalCover(), Cover.CoverLabel, NewDerivedSubject);
                    Cover.ExplodedCovers.Add(ClonedCover);
                }
            }
            else
            {
                if (Cover.IsPerRisk)
                {
                    ExplodeLeafCover(Cover, CoverageIdAttrMap, ResolvedSchedule);
                    Cover.IsExploded = true;
                    return Cover.ExplodedCovers;
                }
                else
                {
                    Cover.ExplodedCovers.Add(Cover.GetOriginalCover());
                    Cover.IsExploded = true;
                    return Cover.ExplodedCovers;
                }
            }
        
            return Cover.ExplodedCovers;
        }

        private void ExplodeLeafCover(WrappedCover PerRiskCover, Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
           Dictionary<string, HashSet<long>> ResolvedSchedule)
        {

            Dictionary<string, HashSet<long>> ExplodedSchedule
                = new Dictionary<string, HashSet<long>>();

            Dictionary<string, SymbolicExpression> ExplodedScheduleLabelMap
                = new Dictionary<string, SymbolicExpression>();

            //Explode Schedule & Lable for Cover
            foreach (SymbolicValue ScheduleSymbol in PerRiskCover.schecule.ScheduleSymbols)
            {
                SymbolicExpression OriginalLabel = PerRiskCover.CoverLabel;
                string PerRiskifiedScheduleSymbol = (ResolvedSchedule.ContainsKey(ScheduleSymbol.ToString())) ? ScheduleSymbol.ToString() : ScheduleSymbol.ToString() + ".#";
                if (ResolvedSchedule.ContainsKey(PerRiskifiedScheduleSymbol))
                {
                    foreach (long RiskID in ResolvedSchedule[PerRiskifiedScheduleSymbol])
                    {
                        if (CoverageIdAttrMap.ContainsKey(RiskID))
                        {
                            string TrimmedPerRiskifiedScheduleSymbol = PerRiskifiedScheduleSymbol.Replace(".#", "");
                            string NewScheduleSymbol = TrimmedPerRiskifiedScheduleSymbol + "." + CoverageIdAttrMap[RiskID].RITExposureId.ToString();
                            SymbolicExpression NewCoverLabel = (SymbolicValue)(OriginalLabel.ToString() + "." + CoverageIdAttrMap[RiskID].RITExposureId.ToString());
                            if (!ExplodedSchedule.ContainsKey(NewScheduleSymbol))
                            {
                                ExplodedSchedule.Add(NewScheduleSymbol, new HashSet<long>());
                                PerRiskCover.ExplodedCoverLabels.Add(NewCoverLabel);
                                ExplodedScheduleLabelMap.Add(NewScheduleSymbol, NewCoverLabel);
                            }
                            ExplodedSchedule[NewScheduleSymbol].Add(RiskID);
                        }
                    }
                }
            }

            foreach (string ExplodedScheduleSymbol in ExplodedSchedule.Keys)
            {
                //Copy Cover with new label               
                IGenericCover ClonedCover = (IGenericCover)Activator.CreateInstance(PerRiskCover.GetOriginalCover().GetType(), PerRiskCover.GetOriginalCover(), ExplodedScheduleLabelMap[ExplodedScheduleSymbol]);


                // NOTE: MUTATION OF INPUT DATA 'ResolvedSchedule'
                // add to schedule dictionary
                if (!ResolvedSchedule.ContainsKey(ExplodedScheduleSymbol))
                    ResolvedSchedule.Add(ExplodedScheduleSymbol, new HashSet<long>(ExplodedSchedule[ExplodedScheduleSymbol]));

                ((Subject)ClonedCover.GetSubject()).HardResetSchedule(new HashSet<SymbolicValue>() { new SymbolicValue(ExplodedScheduleSymbol) }, ResolvedSchedule);

                // set PerRisk (i.e. resolution) flag to false
                // perhaps, use line below only for single building RITEs
                // ClonedTerm.HardResetResolution(false);

                // add cloned term to list
                PerRiskCover.ExplodedCovers.Add(ClonedCover);
            }
        }

        private void ExplodeDerivedCover(WrappedCover PerRiskCover, List<WrappedCover> WrappedCovers)
        {
            //Group the Exploded Children Cover => Then Clone the cover with new ChildLabelCovers
            Dictionary<SymbolicExpression, HashSet<SymbolicExpression>> GroupedExplodedChildLabels = new Dictionary<SymbolicExpression, HashSet<SymbolicExpression>>();
            foreach (SymbolicExpression ChildCoverLabel in PerRiskCover.ChildCoverLabels)
            {
                WrappedCover ChildCover = WrappedCovers.Find(cover => cover.CoverLabel.Equals(ChildCoverLabel));
                foreach (SymbolicExpression ExplodedChildCoverLabel in ChildCover.ExplodedCoverLabels)
                {
                    SymbolicExpression NewCoverLabel = (SymbolicValue)(PerRiskCover.CoverLabel.ToString() + "." + ExplodedChildCoverLabel.ToString().Split('.').Last());
                    if (!GroupedExplodedChildLabels.ContainsKey(NewCoverLabel))
                    {
                        GroupedExplodedChildLabels.Add(NewCoverLabel, new HashSet<SymbolicExpression>());
                        PerRiskCover.ExplodedCoverLabels.Add(NewCoverLabel);
                    }
                    GroupedExplodedChildLabels[NewCoverLabel].Add(ExplodedChildCoverLabel);
                }
            }
            
            //Create new Child Labels & Clone Cover with new Child Labels
            foreach (KeyValuePair<SymbolicExpression, HashSet<SymbolicExpression>> ExplodedLabelMap in GroupedExplodedChildLabels)
            {
                //build new parameters
                SymbolicExpression Label = ExplodedLabelMap.Key;
                object[] NewParameters = BuildNewParameters(ExplodedLabelMap.Value);

                FunctionInvocation<IValue<AValue>> NewDerivedSubject = (FunctionInvocation<IValue<AValue>>)Activator.CreateInstance
                    (PerRiskCover.DerivedSubject.GetType(), PerRiskCover.DerivedSubject, NewParameters);

                IGenericCover ClonedCover = (IGenericCover)Activator.CreateInstance(PerRiskCover.GetOriginalCover().GetType(), PerRiskCover.GetOriginalCover(), Label, NewDerivedSubject);
                PerRiskCover.ExplodedCovers.Add(ClonedCover);
            }
        }

        private object[] BuildNewParameters(HashSet<SymbolicExpression> ExplodedChildLabels)
        {
            //build new parameters
            object[] NewParameters = new Object[ExplodedChildLabels.Count];
            int i = 0;
            foreach (SymbolicExpression ExplodedChildCover in ExplodedChildLabels)
            {
                SimpleExpression<SymbolicValue> explodedChildCover = new SimpleExpression<SymbolicValue>((SymbolicValue)ExplodedChildCover.ToString());
                NewParameters[i] = explodedChildCover;
                i++;
            }

            return NewParameters;
        }

        #endregion
    }
    
}


