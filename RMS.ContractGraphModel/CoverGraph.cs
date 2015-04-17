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
    class CoverGraph : 
        DirectedGraph<IGenericCover>, 
        IEnumerable<INode<IGenericCover>>,
        IIdentifiableINodeCollection<SimpleExpression<SymbolicValue>, IGenericCover>
    {
        #region Fields

        [ProtoMember(1)]
        Dictionary<SimpleExpression<SymbolicValue>, CoverNode> IdentityMap;

        [ProtoMember(2)]
        HashSet<CoverNode> RootNodes;

        [ProtoMember(3)]
        HashSet<CoverNode> LeafNodes;

        [ProtoMember(4)]
        Dictionary<CoverNode, HashSet<long>> SubjectExposureMap = null;

        bool _CanUseTermRootsForCoverLeafSubjectExposures = false;
        public bool CanUseTermRootsForCoverLeafSubjectExposures
        {
            get { return _CanUseTermRootsForCoverLeafSubjectExposures; }
            private set { _CanUseTermRootsForCoverLeafSubjectExposures = value; }
        }
        Dictionary<CoverNode, HashSet<TermNode>> SubjectExposureToTermRootsMap = null;
        Dictionary<CoverNode, Subject> SubjectExposureToResidualSubjectMap = null;

        [ProtoMember(5)]
        ConcurrentDictionary<CoverNode, CoverExecutionPosition> ExecutionRegister = null;

        Dictionary<CoverNode, Dictionary<SimpleExpression<SymbolicValue>, double>> AggregateState = null;

        [ProtoMember(6)]
        ConcurrentDictionary<CoverNode, double> Allocation = null;

        [ProtoMember(7)]
        Dictionary<long, Dictionary<string, double>> RITEAllocation = null;

        [ProtoMember(8)]
        GraphOperationState ExecutionState;

        [ProtoMember(9)]
        GraphOperationState SubjectExposureMapConstructionState;

        #endregion

        #region Constructors
        public CoverGraph() : base() 
        {
            IdentityMap
                = new Dictionary<SimpleExpression<SymbolicValue>, CoverNode>();
            RootNodes = new HashSet<CoverNode>();
            LeafNodes = new HashSet<CoverNode>();
            ExecutionState = new GraphOperationState();
            SubjectExposureMapConstructionState = new GraphOperationState();
        }

        public CoverGraph(CoverGraph CopyFromThisCoverGraph, bool ResetAggregateState = true)
            : base(CopyFromThisCoverGraph)
        {
            this.IdentityMap = CopyFromThisCoverGraph.IdentityMap;
            this.RootNodes = CopyFromThisCoverGraph.RootNodes;
            this.LeafNodes = CopyFromThisCoverGraph.LeafNodes;
            this.ExecutionState = new GraphOperationState();
            this._CanUseTermRootsForCoverLeafSubjectExposures = CopyFromThisCoverGraph._CanUseTermRootsForCoverLeafSubjectExposures;
            this.SubjectExposureToTermRootsMap = CopyFromThisCoverGraph.SubjectExposureToTermRootsMap;
            this.SubjectExposureToResidualSubjectMap = CopyFromThisCoverGraph.SubjectExposureToResidualSubjectMap;
            this.SubjectExposureMapConstructionState = CopyFromThisCoverGraph.SubjectExposureMapConstructionState;

            if (ResetAggregateState)
                AggregateState =
                    new Dictionary<CoverNode, Dictionary<SimpleExpression<SymbolicValue>, double>>();
            else
            {
                AggregateState =
                    new Dictionary<CoverNode, Dictionary<SimpleExpression<SymbolicValue>, double>>();
                foreach (KeyValuePair<CoverNode, Dictionary<SimpleExpression<SymbolicValue>, double>> AggregateStateKVP
                    in CopyFromThisCoverGraph.AggregateState)
                {
                    var ValueDeepCopy = new Dictionary<SimpleExpression<SymbolicValue>, double>();
                    foreach (KeyValuePair<SimpleExpression<SymbolicValue>, double> ValueKVP in AggregateStateKVP.Value)
                    {
                        ValueDeepCopy.Add(ValueKVP.Key, ValueKVP.Value);
                    }
                    AggregateState.Add(AggregateStateKVP.Key, ValueDeepCopy);
                }
            }
        }
        #endregion Constructors

        #region Graph Methods

        private SymbolicExpression GetCoverNodeIdentity(IGenericCover Cover)
        {
            SymbolicExpression NodeIdentity = Cover.GetLabel();

            if (NodeIdentity == null)
            {
                string CoverHashCodeString = Cover.GetHashCode().ToString().Trim();
                string DateTimeNowString = DateTime.Now.ToString();
                string CoverNodeAutoGenLabel = "Automatically Generated CoverLabel ["
                    + CoverHashCodeString + " @ " + DateTimeNowString + "]";
                NodeIdentity = new SymbolicExpression(CoverNodeAutoGenLabel);
            }

            return NodeIdentity;
        }

        public bool Add(IGenericCover Cover, bool RebuildGraphAfterAdd = true)
        {
            SymbolicExpression NodeIdentity = GetCoverNodeIdentity(Cover);

            if (Contains(NodeIdentity))
                throw new ArgumentException("A cover node with the same identity (i.e. label) already exists in this cover graph!");

            CoverNode _CoverNode = new CoverNode(Cover);

            bool IsAddSuccessful = AddCoverNode(NodeIdentity, _CoverNode);

            if (IsAddSuccessful && RebuildGraphAfterAdd)
                IsAddSuccessful &= Rebuild();

            return IsAddSuccessful;
        }

        public bool Add(IGenericCover Cover, Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<string, HashSet<long>> ResolvedSchedule, bool RebuildGraphAfterAdd = true)
        {
            SymbolicExpression NodeIdentity = GetCoverNodeIdentity(Cover);

            if (Contains(NodeIdentity))
                throw new ArgumentException("A cover node with the same identity (i.e. label) already exists in this cover graph!");

            CoverNode _CoverNode = new CoverNode(Cover);

            Subject CoverSubject = (Subject)Cover.GetSubject();

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

            bool IsAddSuccessful = AddCoverNode(NodeIdentity, _CoverNode);

            if (IsAddSuccessful && RebuildGraphAfterAdd)
                IsAddSuccessful &= Rebuild();

            return IsAddSuccessful;
        }

        private bool AddCoverNode(SymbolicExpression NodeIdentity, CoverNode _CoverNode)
        {
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

            return IsAddSuccessful;
        }

        public bool Rebuild()
        {
            bool IsRebuildSuccessful = true;

            foreach (CoverNode CoverNode in this)
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
                            CoverNode ChildCoverNode = IdentityMap[ChildCoverLabel];
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

            AddAggregateDerivedCoversForReinstatements();

            ExecutionState.RegisterModificationInGraphTopology();
            SubjectExposureMapConstructionState.RegisterModificationInGraphTopology();

            return IsRebuildSuccessful;
        }

        private void AddAggregateDerivedCoversForReinstatements()
        {
            ICollection<INode<IGenericCover>> OriginalCoverNodes = 
                new HashSet<INode<IGenericCover>>(this);

            foreach (CoverNode CoverNode in OriginalCoverNodes)
            {
                ILimit<Value> Limit = CoverNode.GetContent().GetLimit();

                if (Limit == null)
                    continue;

                TimeBasis CoverTimeBasis = CoverNode.GetContent().GetLimit().GetTimeBasis();
                
                if (CoverTimeBasis != TimeBasis.Occurrence)
                    continue;

                int NumberReinstatements = CoverNode.GetContent().GetLimit().GetNumberReinstatements();

                if (NumberReinstatements == -1) // at this point, NumberReinstatements == -1 => Primary
                    continue;

                bool ShouldAddEffectiveAggregateCover = false;

                if ((GetParentsOfNode(CoverNode) == null) || ((GetParentsOfNode(CoverNode)).Count() == 0))
                {
                    ShouldAddEffectiveAggregateCover = true;
                }
                else
                {
                    ShouldAddEffectiveAggregateCover = true;

                    foreach (CoverNode ParentCoverNode in GetParentsOfNode(CoverNode))
                    {
                        TimeBasis ParentCoverTimeBasis = ParentCoverNode.GetContent().GetLimit().GetTimeBasis();

                        if (ParentCoverTimeBasis == TimeBasis.Aggregate)
                        {
                            ShouldAddEffectiveAggregateCover = false;
                            break;
                        }
                    }
                }

                if (ShouldAddEffectiveAggregateCover)
                {
                    SymbolicExpression CoverLabelExpression = CoverNode.GetContent().GetLabel();

                    if (CoverLabelExpression == null || CoverLabelExpression.ToString().Trim().Equals(""))
                    {
                        string CoverHashCodeString = CoverNode.GetContent().GetHashCode().ToString().Trim();
                        string DateTimeNowString = DateTime.Now.ToString();
                        string CoverNodeAutoGenLabel = "Automatically Generated CoverLabel ["
                            + CoverHashCodeString + " @ " + DateTimeNowString + "]";
                        CoverNode.GetContent().SetLabel(CoverNodeAutoGenLabel);
                        CoverLabelExpression = CoverNode.GetContent().GetLabel();
                    }
                    
                    string CoverLabel = CoverLabelExpression.ToString();

                    FunctionInvocation<IValue<AValue>> EffectiveAggregateDerivedSubject =
                        new FunctionInvocation<IValue<AValue>>("SUM", new SymbolicExpression(CoverLabel));

                    ArithmeticExpression EffectiveAggregateLimit =
                        new ArithmeticExpression(
                            new ArithmeticTerm(
                                    new ArithmeticTerm(NumberReinstatements + 1),
                                    ArithmeticOperator.MULTIPLY,
                                    CoverNode.GetContent().GetLimit().GetLimitSpecification().GetExpression()));

                    //double EffectiveAggregateLimit = (NumberReinstatements + 1) 
                    //    * CoverNode.GetContent().GetLimit().GetLimitSpecification().GetExpression();

                    Cover<NumericValue, Value, NumericValue> EffectiveAggregateLimitCover 
                        = new Cover<NumericValue,Value,NumericValue>(
                            new Participation<NumericValue>(100),
                            new Limit<Value>(EffectiveAggregateLimit, TimeBasis.Aggregate, 0),
                            EffectiveAggregateDerivedSubject);

                    // Create cover node for effective aggregate limit cover

                    SymbolicExpression EffectiveAggregateLimitCoverNodeIdentity = GetCoverNodeIdentity(EffectiveAggregateLimitCover);

                    CoverNode EffectiveAggregateLimitCoverNode = new CoverNode(EffectiveAggregateLimitCover);

                    Subject EffectiveAggregateLimitCoverSubject = (Subject)EffectiveAggregateLimitCover.GetSubject();

                    // Set per-risk with appropriate number of risks (i.e. buildings), as necessary

                    Subject CoverNodeSubject = (Subject)CoverNode.GetSubject();

                    if (CoverNodeSubject.PerRisk)
                    {
                        EffectiveAggregateLimitCoverSubject.PerRisk = CoverNodeSubject.PerRisk;
                        EffectiveAggregateLimitCoverSubject.NumBuildings = CoverNodeSubject.NumBuildings;
                    }

                    // Add node to graph

                    bool IsAddSuccessful = AddCoverNode(EffectiveAggregateLimitCoverNodeIdentity, EffectiveAggregateLimitCoverNode);

                    if (IsAddSuccessful)
                    {
                        // Get Original Parents of CoverNode
                        ICollection<INode<IGenericCover>> ParentsOfCoverNode = 
                            new HashSet<INode<IGenericCover>>(GetParentsOfNode(CoverNode));

                        // Attach as parent to original cover node

                        MakeParentChildEdge(EffectiveAggregateLimitCoverNode, CoverNode);
                        RootNodes.Remove(CoverNode);
                        LeafNodes.Remove(EffectiveAggregateLimitCoverNode);

                        // Attach as child to parents of original cover node 
                        // and, delete corresponding parent-child links from those parents to original cover node

                        
                        foreach (CoverNode ParentCoverNode in ParentsOfCoverNode)
                        {
                            MakeParentChildEdge(ParentCoverNode, EffectiveAggregateLimitCoverNode);
                            RootNodes.Remove(EffectiveAggregateLimitCoverNode);
                            DeleteParentChildEdge(ParentCoverNode, CoverNode);
                            ParentCoverNode.GetContent().GetDerivedSubject().SetParameters(EffectiveAggregateLimitCoverNodeIdentity.ToString());
                        }
                    }
                }
            }
        }

        #endregion

        #region API

        public Dictionary<long, Dictionary<string, double>> GetRITEAllocation()
        {
            return RITEAllocation;
        }
        
        public Dictionary<long, Loss> FilterExposedExposureIDs(Dictionary<string, HashSet<long>> Schedule,
           Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap,
           Dictionary<long, Loss> ExposureIDGULossMap)
        {
            Dictionary<long, Loss> ExposedExposureIDGULossMap = new Dictionary<long, Loss>();

            foreach (CoverNode LeafNode in LeafNodes)
            {
                Subject RootNodeSubject = (Subject)LeafNode.GetContent().GetSubject();

                // If any cover subject is unconstrained (i.e. entire contract subject is applicable), return without filtering
                if (RootNodeSubject.IsNotConstrained())
                    return ExposureIDGULossMap;

                foreach (string ScheduleSymbol in RootNodeSubject.Schedule.ScheduleSymbols.Select(ss => ss.ToString()).ToList())
                {
                    foreach (long RITECoverageId in Schedule[ScheduleSymbol])
                    {
                        if (ExposedExposureIDGULossMap.ContainsKey(RITECoverageId))
                            continue;
                        if ((ExposureIDGULossMap.ContainsKey(RITECoverageId))
                                && (ExposureIDAttributeMap.ContainsKey(RITECoverageId))
                                && RootNodeSubject.ResolvedExposureTypes.Contains(ExposureIDAttributeMap[RITECoverageId].ExposureType))
                        {
                            foreach (SymbolicValue COL in RootNodeSubject.CausesOfLoss)
                            {
                                if (ExposureIDGULossMap[RITECoverageId].CausesOfLoss.Contains(COL) ||
                                    (Loss.COLEquivalencyMap.ContainsKey(COL) && Loss.COLEquivalencyMap[COL].ContainsAny(ExposureIDGULossMap[RITECoverageId].CausesOfLoss)))
                                {
                                    ExposedExposureIDGULossMap.Add(RITECoverageId, ExposureIDGULossMap[RITECoverageId]);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return ExposedExposureIDGULossMap;
        }

        public void DetectMultiBldgPerRiskForCoverLeafs(Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap)
        {
            foreach (CoverNode _CoverNode in LeafNodes)
            {
                Subject CoverNodeSubject = (Subject)_CoverNode.GetSubject();

                if (CoverNodeSubject.PerRisk
                    && (CoverNodeSubject.Schedule.ScheduleSymbols.Count == 1)
                    && (CoverNodeSubject.RITEIds.Any(x => CoverageIdAttrMap.ContainsKey(x)
                        && CoverageIdGULossMap.ContainsKey(x)
                        && (CoverageIdAttrMap[x].NumBuildings > 1
                            || CoverageIdGULossMap[x].Factor != 1.0))))
                {
                    _CoverNode.MarkAsMultiBuildingPerRisk();
                    CanUseTermRootsForCoverLeafSubjectExposures = false;
                }
            }
        }

        public bool TryCreateSubjectExposureForLeafNodesFromTermRootsMap(TermGraph TermGraph)
        {
            SubjectExposureToTermRootsMap = new Dictionary<CoverNode, HashSet<TermNode>>();
            SubjectExposureToResidualSubjectMap = new Dictionary<CoverNode, Subject>();
            HashSet<TermNode> TermRootsAccountedFor = new HashSet<TermNode>();

            foreach (CoverNode LeafCoverNode in LeafNodes)
            {
                if (LeafCoverNode.IsMultiBuildingPerRisk())
                {
                    SubjectExposureToTermRootsMap = null;
                    SubjectExposureToResidualSubjectMap = null;
                    return false;
                }
                SubjectExposureToTermRootsMap.Add(LeafCoverNode, new HashSet<TermNode>());
                Subject CoverLeafResidualSubject = (Subject)LeafCoverNode.GetSubject();
                foreach (TermNode RootTermNode in TermGraph.RootNodes)
                {
                    if (RootTermNode.GetSubject().IsSubsetOf((Subject)LeafCoverNode.GetSubject()))
                    {
                        SubjectExposureToTermRootsMap[LeafCoverNode].Add(RootTermNode);
                        TermRootsAccountedFor.Add(RootTermNode);
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

            // Check if all Term Roots are accounted for by some Cover Leaf
            foreach (TermNode RootTermNode in TermGraph.RootNodes)
            {
                if (!TermRootsAccountedFor.Contains(RootTermNode))
                {
                    _CanUseTermRootsForCoverLeafSubjectExposures = false;
                    SubjectExposureToTermRootsMap = null;
                    SubjectExposureToResidualSubjectMap = null;
                    return false;
                }
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

            SubjectExposureMap = new Dictionary<CoverNode, HashSet<long>>();

            foreach (CoverNode LeafNode in LeafNodes)
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

            ExecutionRegister = new ConcurrentDictionary<CoverNode, CoverExecutionPosition>();
            Allocation = new ConcurrentDictionary<CoverNode, double>();

            foreach (CoverNode RootNode in RootNodes)
            {
                ExecutionRegister.GetOrAdd(RootNode,
                    ExecuteCoverNode(RootNode, _TermGraph, CoverageIdGULossMap, 
                        CoverageIdAttrMap, RCVCoveredAndAffectedBySubject, Bindings, ShouldAllocate));

                ExecutionRegister[RootNode].Coalesce();
                // @FACTORCHANGES REPLACE
                // ExecutionRegister[RootNode].Coalesce(ExecutionRegister[RootNode].NumBuildings);

                ContractPayout += ExecutionRegister[RootNode].P;
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

        private CoverExecutionPosition ExecuteCoverNode(CoverNode _CoverNode,
            TermGraph _TermGraph,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<SubjectPosition, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool ShouldAllocate)
            {
                // Base case : _CoverNode is leaf

                CoverExecutionPosition SubjectPosition;

                if ((GetChildrenOfNode(_CoverNode) == null) || (GetChildrenOfNode(_CoverNode).Count == 0))
                {
                    Subject CoverNodeSubject = (Subject)_CoverNode.GetSubject();

                    //if (CoverNodeSubject.PerRisk
                    //   && (CoverNodeSubject.Schedule.ScheduleSymbols.Count == 1)
                    //   && (CoverNodeSubject.RITEIds.Any(x => CoverageIdAttrMap.ContainsKey(x) 
                    //        && CoverageIdGULossMap.ContainsKey(x) 
                    //        && (CoverageIdAttrMap[x].NumBuildings > 1 
                    //            || CoverageIdGULossMap[x].Factor != 1.0))))
                    //{
                    //    _CoverNode.MarkAsMultiBuildingPerRisk();
                    //}

                    if (_CoverNode.IsMultiBuildingPerRisk())
                    {
                        if (_CanUseTermRootsForCoverLeafSubjectExposures)
                        {
                            throw new InvalidOperationException("Presence of multi-building per risk cover leafs should have trigerred allocation");
                        }
                        else
                        {
                            Dictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocationAfterTermGraphExecution = _TermGraph.GetAllocation();

                            double[] _CoverNodeSubjectLoss = null;
                            double Factor = 1.0;
                            double[] FactorArray = null;
                            // COL EQUIVALENCY CHANGES
                            //double[] _CoverNodeSubjectLoss = new double[CoverNodeSubject.NumBuildings];

                            #region OLDER INCORRECT APPROACH
                            //foreach (long RITEId in RITEAllocationAfterTermGraphExecution.Keys.Where(it => SubjectExposureMap[_CoverNode].Contains(it)))
                            //{
                            //    foreach (string COL in RITEAllocationAfterTermGraphExecution[RITEId].Keys)
                            //    {
                            //        if (CoverNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                            //            _CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(
                            //                Loss.WeightedSplit(RITEAllocationAfterTermGraphExecution[RITEId][COL].R,
                            //                    CoverageIdGULossMap[RITEId].AmountByCOL[COL],
                            //                    CoverNodeSubject.NumBuildings),
                            //                (a, b) => a + b).ToArray<double>();
                            //    }
                            //}
                            #endregion

                            #region OLD INCORRECT APPROACH
                            //foreach (long RITEId in SubjectExposureMap[_CoverNode])
                            //{
                            //    if (RITEAllocationAfterTermGraphExecution.ContainsKey(RITEId))
                            //    {
                            //        foreach (string COL in RITEAllocationAfterTermGraphExecution[RITEId].Keys)
                            //        {
                            //            if (CoverNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                            //                _CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(
                            //                Loss.WeightedSplit(RITEAllocationAfterTermGraphExecution[RITEId][COL].R,
                            //                    CoverageIdGULossMap[RITEId].AmountByCOL[COL],
                            //                    CoverNodeSubject.NumBuildings),
                            //                (a, b) => a + b).ToArray<double>();
                            //        }
                            //    }
                            //    else
                            //    {
                            //        foreach (SymbolicValue COL in CoverNodeSubject.GetComponents().Keys)
                            //        {
                            //            if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(COL))
                            //                _CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(
                            //                    CoverageIdGULossMap[RITEId].AmountByCOL[COL],
                            //                (a, b) => a + b).ToArray<double>();
                            //        }
                            //    }
                            //}
                            #endregion


                            // NEW APPROACH : GO THROUGH COLs FIRST
                            foreach (SymbolicValue COL in CoverNodeSubject.GetComponents().Keys)
                            {
                                foreach (long RITEId in SubjectExposureMap[_CoverNode])
                                {
                                    if (RITEAllocationAfterTermGraphExecution.ContainsKey(RITEId)
                                        && RITEAllocationAfterTermGraphExecution[RITEId].ContainsKey(COL.ToString()))
                                    {
                                        if (_CoverNodeSubjectLoss == null)
                                            _CoverNodeSubjectLoss = Enumerable.Repeat(0.0, CoverageIdGULossMap[RITEId].GetSamplesForCOL(COL).Count).ToArray();
                                        // COL EQUIVALENCY CHANGES
                                        //_CoverNodeSubjectLoss = new double[CoverageIdGULossMap[RITEId].AmountByCOL[COL].Count];
                                        double FactorSum = CoverageIdGULossMap[RITEId].FactorArray.Sum();
                                        for (int i = 0; i < _CoverNodeSubjectLoss.Length; i++)
                                            _CoverNodeSubjectLoss[i] += (RITEAllocationAfterTermGraphExecution[RITEId][COL.ToString()].R) / 
                                                (FactorSum);
                                        // @FACTORARRAY REPLACE
                                        //for (int i = 0; i < _CoverNodeSubjectLoss.Length; i++)
                                        //    _CoverNodeSubjectLoss[i] += RITEAllocationAfterTermGraphExecution[RITEId][COL.ToString()].R /
                                        //        (_CoverNodeSubjectLoss.Length * CoverageIdGULossMap[RITEId].Factor);
                                        // @FACTORCHANGES REPLACE
                                        //_CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(
                                        //    Loss.WeightedSplit(RITEAllocationAfterTermGraphExecution[RITEId][COL.ToString()].R,
                                        //        CoverageIdGULossMap[RITEId].AmountByCOL[COL],
                                        //        CoverNodeSubject.NumBuildings),
                                        //    (a, b) => a + b).ToArray<double>();
                                    }
                                    else // if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(COL)) // ASSERT : SHOULD ALWAYS BE TRUE!
                                    {
                                        if (_CoverNodeSubjectLoss == null)
                                            _CoverNodeSubjectLoss = Enumerable.Repeat(0.0, CoverageIdGULossMap[RITEId].GetSamplesForCOL(COL).Count).ToArray();
                                        _CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(
                                                CoverageIdGULossMap[RITEId].GetSamplesForCOL(COL),
                                            (a, b) => a + b).ToArray<double>();
                                        // COL EQUIVALENCY CHANGES
                                        //_CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(
                                        //        CoverageIdGULossMap[RITEId].AmountByCOL[COL],
                                        //    (a, b) => a + b).ToArray<double>();
                                    }
                                    Factor = CoverageIdGULossMap[RITEId].Factor;
                                    FactorArray = CoverageIdGULossMap[RITEId].FactorArray;
                                }
                            }

                            if (FactorArray == null || _CoverNodeSubjectLoss == null) // no exposures!
                            {
                                _CoverNodeSubjectLoss = new double[] { 0.0 };
                                FactorArray = new double[] { 1.0 };
                            }

                            SubjectPosition = new CoverExecutionPosition(_CoverNodeSubjectLoss, Enumerable.Repeat(0.0, FactorArray.Length).ToArray(), 
                                FactorArray, CoverNodeSubject.NumBuildings);
                            // @FACTORARRAY REPLACE
                            //SubjectPosition = new CoverExecutionPosition(_CoverNodeSubjectLoss, Factor, CoverNodeSubject.NumBuildings);
                        }
                    }

                    else
                    {
                        double _CoverNodeSubjectLoss = 0.0;
                        double Factor = 1.0;

                        if (_CanUseTermRootsForCoverLeafSubjectExposures && !ShouldAllocate && !_TermGraph.IsOverlapping)
                        {
                            foreach (TermNode RootTermNode in SubjectExposureToTermRootsMap[_CoverNode])
                            {
                                TermExecutionPosition RootTermNodeExecutionPosition = _TermGraph.ExecutionRegister[RootTermNode];
                                RootTermNodeExecutionPosition.Coalesce();
                                // @FACTORCHANGES REPLACE
                                // RootTermNodeExecutionPosition.Coalesce(RootTermNode.GetSubject().NumBuildings);
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
                                        if (CoverageIdGULossMap[RITEId].ContainsCOL(ComponentsKVP.Key))
                                        {
                                            _CoverNodeSubjectLoss += CoverageIdGULossMap[RITEId][ComponentsKVP.Key];
                                            // @FACTORCHANGES REPLACE
                                            //_CoverNodeSubjectLoss += Loss.WeightedSum(CoverageIdGULossMap[RITEId].AmountByCOL[ComponentsKVP.Key],
                                            //    CoverageIdAttrMap[RITEId].NumBuildings);
                                        }
                                        // COL EQUIVALENCY CHANGES
                                        //if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(ComponentsKVP.Key))
                                        //{
                                        //    _CoverNodeSubjectLoss += CoverageIdGULossMap[RITEId][ComponentsKVP.Key];
                                        //    // @FACTORCHANGES REPLACE
                                        //    //_CoverNodeSubjectLoss += Loss.WeightedSum(CoverageIdGULossMap[RITEId].AmountByCOL[ComponentsKVP.Key],
                                        //    //    CoverageIdAttrMap[RITEId].NumBuildings);
                                        //}
                                    }
                                }
                            }

                            SubjectPosition = new CoverExecutionPosition(_CoverNodeSubjectLoss, Factor);
                        }

                        else
                        {
                            Dictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocationAfterTermGraphExecution = _TermGraph.GetAllocation();

                            #region OLDER INCORRECT APPROACH
                            //foreach (long RITEId in RITEAllocationAfterTermGraphExecution.Keys.Where(it => SubjectExposureMap[_CoverNode].Contains(it)))
                            //{
                            //    foreach (string COL in RITEAllocationAfterTermGraphExecution[RITEId].Keys)
                            //    {
                            //        if (CoverNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                            //            _CoverNodeSubjectLoss += RITEAllocationAfterTermGraphExecution[RITEId][COL].R;
                            //    }
                            //}
                            #endregion

                            #region OLD INCORRECT APPROACH
                            //foreach (long RITEId in SubjectExposureMap[_CoverNode])
                            //{
                            //    if (RITEAllocationAfterTermGraphExecution.ContainsKey(RITEId))
                            //    {
                            //        foreach (string COL in RITEAllocationAfterTermGraphExecution[RITEId].Keys)
                            //        {
                            //            if (CoverNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                            //                _CoverNodeSubjectLoss += RITEAllocationAfterTermGraphExecution[RITEId][COL].R;
                            //        }
                            //    }
                            //    else
                            //    {
                            //        foreach (SymbolicValue COL in CoverNodeSubject.GetComponents().Keys)
                            //        {
                            //            if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(COL.ToString()))
                            //                _CoverNodeSubjectLoss += Loss.WeightedSum(CoverageIdGULossMap[RITEId].AmountByCOL[COL.ToString()],
                            //                    CoverageIdAttrMap[RITEId].NumBuildings);
                            //        }
                            //    }
                            //}
                            #endregion

                            // NEW APPROACH : GO THROUGH COLs FIRST
                            foreach (SymbolicValue COL in CoverNodeSubject.GetComponents().Keys)
                            {
                                foreach (long RITEId in SubjectExposureMap[_CoverNode])
                                {
                                    if (RITEAllocationAfterTermGraphExecution.ContainsKey(RITEId) && RITEAllocationAfterTermGraphExecution[RITEId].ContainsKey(COL.ToString()))
                                    {
                                        _CoverNodeSubjectLoss += RITEAllocationAfterTermGraphExecution[RITEId][COL.ToString()].R;
                                    }
                                    else // if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(COL)) // SHOULD ALWAYS BE TRUE!
                                    {
                                        _CoverNodeSubjectLoss += CoverageIdGULossMap[RITEId][COL];
                                        // COL EQUIVALENCY CHANGES
                                        //_CoverNodeSubjectLoss += CoverageIdGULossMap[RITEId][COL];
                                        // @FACTORCHANGES REPLACE
                                        //_CoverNodeSubjectLoss += Loss.WeightedSum(CoverageIdGULossMap[RITEId].AmountByCOL[COL.ToString()],
                                        //        CoverageIdAttrMap[RITEId].NumBuildings);
                                    }
                                }
                            }

                            SubjectPosition = new CoverExecutionPosition(new double[]{_CoverNodeSubjectLoss}, Factor);
                        }
                    }

                    CoverNodeSubject = (Subject)_CoverNode.GetSubject();
                    Bindings.Remove(new SymbolicExpression("RCVCovered"));
                    Bindings.Add(new SymbolicExpression("RCVCovered"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item1);
                    Bindings.Remove(new SymbolicExpression("RCVAffected"));
                    Bindings.Add(new SymbolicExpression("RCVAffected"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item2);

                    if (!AggregateState.ContainsKey(_CoverNode))
                    {
                        AggregateState.Add(_CoverNode, new Dictionary<SimpleExpression<SymbolicValue>, double>());
                        AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateLimitState"), 0.0);
                        AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateAttachmentState"), 0.0);
                    }

                    return _CoverNode.Execute(SubjectPosition, Bindings, AggregateState[_CoverNode]);
                }

                // Recursive case

                Subject _CoverNodeSubject = (Subject)_CoverNode.GetSubject();

                // A. Go through each child node: (a) try to register for execution; (b) if successful, execute

                int NumBuildings = _CoverNodeSubject.NumBuildings;
                double ChildFactor = 1.0;

                HashSet<Task<double>> ChildCoverNodeTasks = new HashSet<Task<double>>();
                foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
                {
                    ExecutionRegister.GetOrAdd(_ChildCoverNode,
                        ExecuteCoverNode(_ChildCoverNode, _TermGraph, CoverageIdGULossMap, 
                            CoverageIdAttrMap, RCVCoveredAndAffectedBySubject, Bindings, ShouldAllocate));
                    NumBuildings = Math.Max(NumBuildings, ((Subject)_ChildCoverNode.GetSubject()).NumBuildings);
                    double _ChildCoverNodeFactor = ExecutionRegister[_ChildCoverNode].Factor;
                    if (_ChildCoverNodeFactor != 1.0)
                        ChildFactor = _ChildCoverNodeFactor;
                }

                // B. Form subject

                if ((_CoverNode.GetSubject() as Subject).PerRisk 
                    && ((NumBuildings > 1) || (ChildFactor != 1.0)))
                    _CoverNode.MarkAsMultiBuildingPerRisk();

                if (_CoverNode.IsMultiBuildingPerRisk())
                {
                    List<double> S_vector = new List<double>();
                    double Factor = 1.0;
                    double[] FactorArray = null;
                    for (int i = 0; ; i++)
                    {
                        Dictionary<SimpleExpression<SymbolicValue>, double> ChildCoverPayoutBindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();
                        foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
                        {
                            if (i <= ExecutionRegister[_ChildCoverNode].P_vector.Length - 1)
                            {
                                ChildCoverPayoutBindings.Add(_ChildCoverNode.GetContent().GetLabel(), ExecutionRegister[_ChildCoverNode].P_vector[i]);
                                Factor = ExecutionRegister[_ChildCoverNode].Factor;
                                FactorArray = ExecutionRegister[_ChildCoverNode].FactorArray;
                            }
                        }
                        FunctionInvocation<IValue<AValue>> DerivedSubject
                            = _CoverNode.GetContent().GetDerivedSubject();
                        if (ChildCoverPayoutBindings.Count > 0)
                            S_vector.Add(DerivedSubject.GetEvaluatedValue(ChildCoverPayoutBindings));
                        else
                            break;
                    }

                    SubjectPosition = new CoverExecutionPosition(S_vector.ToArray<double>(),
                        Enumerable.Repeat(0.0, S_vector.Count).ToArray(), FactorArray, NumBuildings);
                    // @FACTORARRAY REPLACE
                    //SubjectPosition = new CoverExecutionPosition(S_vector.ToArray<double>(), Factor, NumBuildings);
                }
                else
                {
                    List<double> SubjectPositions = new List<double>();
                    double Factor = 1.0;
                    double[] FactorArray = null;
                    FunctionInvocation<IValue<AValue>> DerivedSubject
                        = _CoverNode.GetContent().GetDerivedSubject();
                    foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
                    {
                        SubjectPositions.Add((DerivedSubject.GetFunctionDelegate()(ExecutionRegister[_ChildCoverNode].P_vector.Cast<object>().ToArray()).GetEvaluatedValue()));
                        Factor = ExecutionRegister[_ChildCoverNode].Factor;
                        FactorArray = ExecutionRegister[_ChildCoverNode].FactorArray;
                    }
                    SubjectPosition
                       = new CoverExecutionPosition(new double[] { DerivedSubject.GetFunctionDelegate()(SubjectPositions.Cast<object>().ToArray()).GetEvaluatedValue() },
                           new double[] { 0.0 }, FactorArray);
                    // @FACTORARRAY REPLACE
                    //SubjectPosition
                    //   = new CoverExecutionPosition(new double[] { DerivedSubject.GetFunctionDelegate()(SubjectPositions.Cast<object>().ToArray()).GetEvaluatedValue() },
                    //   Factor);
                }

                Bindings.Remove(new SymbolicExpression("RCVCovered"));
                Bindings.Remove(new SymbolicExpression("RCVAffected"));
                Bindings.Remove(new SymbolicExpression("Subject"));

                if (!AggregateState.ContainsKey(_CoverNode))
                {
                    AggregateState.Add(_CoverNode, new Dictionary<SimpleExpression<SymbolicValue>, double>());
                    AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateLimitState"), 0.0);
                    AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateAttachmentState"), 0.0);
                }

                return _CoverNode.Execute(SubjectPosition, Bindings, AggregateState[_CoverNode]);
            }

        public double Execute_(TermGraph _TermGraph,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<SubjectPosition, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool ShouldAllocate = false)
        {
            Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>> RITEAllocationAfterTermGraphExecution 
                = _TermGraph.GetAllocation_();

            double ContractPayout = 0.0;

            if (IdentityMap.Count == 0)
            {
                // Update execution state
                ExecutionState = new GraphOperationState(false);
                return 0.0;
            }

            ExecutionRegister = new ConcurrentDictionary<CoverNode, CoverExecutionPosition>();
            Allocation = new ConcurrentDictionary<CoverNode, double>();

            foreach (CoverNode RootNode in RootNodes)
            {
                ExecutionRegister.GetOrAdd(RootNode,
                    ExecuteCoverNode_(RootNode, _TermGraph, CoverageIdGULossMap,
                        CoverageIdAttrMap, RCVCoveredAndAffectedBySubject, Bindings, ShouldAllocate));

                ExecutionRegister[RootNode].Coalesce();
                // @FACTORCHANGES REPLACE
                // ExecutionRegister[RootNode].Coalesce(ExecutionRegister[RootNode].NumBuildings);

                ContractPayout += ExecutionRegister[RootNode].P;
            }

            // Update execution state
            ExecutionState = new GraphOperationState(false);

            // Allocate?

            if (ShouldAllocate)
            {
                this.Allocate_(RITEAllocationAfterTermGraphExecution);
                // The line below is a dummy; intended to breakpoint a debugging session so that one can view RITEAllocation (Hi Slava! :)
                //Console.WriteLine(RITEAllocation);
            }

            return ContractPayout;
        }

        private CoverExecutionPosition ExecuteCoverNode_(CoverNode _CoverNode,
            TermGraph _TermGraph,
            Dictionary<long, Loss> CoverageIdGULossMap,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<SubjectPosition, Tuple<double, double>> RCVCoveredAndAffectedBySubject,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool ShouldAllocate)
        {
            // Base case : _CoverNode is leaf

            CoverExecutionPosition SubjectPosition;

            if ((GetChildrenOfNode(_CoverNode) == null) || (GetChildrenOfNode(_CoverNode).Count == 0))
            {
                Subject CoverNodeSubject = (Subject)_CoverNode.GetSubject();

                //if (CoverNodeSubject.PerRisk
                //   && (CoverNodeSubject.Schedule.ScheduleSymbols.Count == 1)
                //   && (CoverNodeSubject.RITEIds.Any(x => CoverageIdAttrMap.ContainsKey(x) 
                //        && CoverageIdGULossMap.ContainsKey(x) 
                //        && (CoverageIdAttrMap[x].NumBuildings > 1 
                //            || CoverageIdGULossMap[x].Factor != 1.0))))
                //{
                //    _CoverNode.MarkAsMultiBuildingPerRisk();
                //}

                if (_CoverNode.IsMultiBuildingPerRisk()) // multi-building per-risk leaf cover
                {
                    if (_CanUseTermRootsForCoverLeafSubjectExposures)
                    {
                        throw new InvalidOperationException("Presence of multi-building per risk cover leafs should have trigerred allocation!");
                    }
                    else
                    {
                        Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>> RITEAllocationAfterTermGraphExecution 
                            = _TermGraph.GetAllocation_();

                        double[] _CoverNodeSubjectLoss = null;
                        double Factor = 1.0;
                        double[] FactorArray = null;

                        // NEW APPROACH : GO THROUGH COLs FIRST
                        foreach (SymbolicValue COL in CoverNodeSubject.GetComponents().Keys)
                        {
                            foreach (long RITEId in SubjectExposureMap[_CoverNode])
                            {
                                if (RITEAllocationAfterTermGraphExecution.ContainsKey(RITEId)
                                    && RITEAllocationAfterTermGraphExecution[RITEId].ContainsKey(COL.ToString()))
                                {
                                    if (_CoverNodeSubjectLoss == null)
                                        _CoverNodeSubjectLoss = Enumerable.Repeat(0.0, CoverageIdGULossMap[RITEId].GetSamplesForCOL(COL).Count).ToArray();
                                    for (int i = 0; i < _CoverNodeSubjectLoss.Length; i++)
                                        _CoverNodeSubjectLoss[i] += RITEAllocationAfterTermGraphExecution[RITEId][COL.ToString()].R_vector[i];
                                }
                                else // if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(COL)) // ASSERT : SHOULD ALWAYS BE TRUE!
                                {
                                    if (_CoverNodeSubjectLoss == null)
                                        _CoverNodeSubjectLoss = Enumerable.Repeat(0.0, CoverageIdGULossMap[RITEId].GetSamplesForCOL(COL).Count).ToArray();
                                    _CoverNodeSubjectLoss = _CoverNodeSubjectLoss.Zip(
                                            CoverageIdGULossMap[RITEId].GetSamplesForCOL(COL),
                                        (a, b) => a + b).ToArray<double>();
                                }
                                Factor = CoverageIdGULossMap[RITEId].Factor;
                                FactorArray = CoverageIdGULossMap[RITEId].FactorArray;
                            }
                        }

                        if (FactorArray == null || _CoverNodeSubjectLoss == null) // no exposures!
                        {
                            _CoverNodeSubjectLoss = new double[] { 0.0 };
                            FactorArray = new double[] { 1.0 };
                        }

                        SubjectPosition = new CoverExecutionPosition(_CoverNodeSubjectLoss, Enumerable.Repeat(0.0, FactorArray.Length).ToArray(),
                            FactorArray, CoverNodeSubject.NumBuildings);
                    }
                }

                else // non-multi-bldg per-risk leaf cover
                {
                    double _CoverNodeSubjectLoss = 0.0;
                    double Factor = 1.0;

                    if (_CanUseTermRootsForCoverLeafSubjectExposures && !_TermGraph.IsOverlapping)
                    {
                        foreach (TermNode RootTermNode in SubjectExposureToTermRootsMap[_CoverNode])
                        {
                            TermExecutionPosition RootTermNodeExecutionPosition = _TermGraph.ExecutionRegister[RootTermNode];
                            RootTermNodeExecutionPosition.Coalesce();
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
                                    if (CoverageIdGULossMap[RITEId].ContainsCOL(ComponentsKVP.Key))
                                    {
                                        _CoverNodeSubjectLoss += CoverageIdGULossMap[RITEId][ComponentsKVP.Key];
                                    }
                                }
                            }
                        }

                        SubjectPosition = new CoverExecutionPosition(_CoverNodeSubjectLoss, 0.0, Factor);
                    }

                    else // cannot or should not directly connect Term roots to Cover Leafs
                    {
                        Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>> RITEAllocationAfterTermGraphExecution 
                            = _TermGraph.GetAllocation_();

                        // NEW APPROACH : GO THROUGH COLs FIRST
                        foreach (SymbolicValue COL in CoverNodeSubject.GetComponents().Keys)
                        {
                            foreach (long RITEId in SubjectExposureMap[_CoverNode])
                            {
                                if (RITEAllocationAfterTermGraphExecution.ContainsKey(RITEId) 
                                    && RITEAllocationAfterTermGraphExecution[RITEId].ContainsKey(COL.ToString()))
                                {
                                    _CoverNodeSubjectLoss += 
                                        RITEAllocationAfterTermGraphExecution[RITEId][COL.ToString()].R_vector
                                        .Zip(CoverageIdGULossMap[RITEId].FactorArray, (a, b) => a * b).Sum();
                                }
                                else // if (CoverageIdGULossMap[RITEId].AmountByCOL.ContainsKey(COL)) // SHOULD ALWAYS BE TRUE!
                                {
                                    _CoverNodeSubjectLoss += CoverageIdGULossMap[RITEId][COL];
                                }
                            }
                        }

                        SubjectPosition = new CoverExecutionPosition(new double[] { _CoverNodeSubjectLoss }, Factor);
                    }
                }

                CoverNodeSubject = (Subject)_CoverNode.GetSubject();
                Bindings.Remove(new SymbolicExpression("RCVCovered"));
                Bindings.Add(new SymbolicExpression("RCVCovered"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item1);
                Bindings.Remove(new SymbolicExpression("RCVAffected"));
                Bindings.Add(new SymbolicExpression("RCVAffected"), RCVCoveredAndAffectedBySubject[CoverNodeSubject].Item2);

                if (!AggregateState.ContainsKey(_CoverNode))
                {
                    AggregateState.Add(_CoverNode, new Dictionary<SimpleExpression<SymbolicValue>, double>());
                    AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateLimitState"), 0.0);
                    AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateAttachmentState"), 0.0);
                }

                return _CoverNode.Execute(SubjectPosition, Bindings, AggregateState[_CoverNode]);
            }

            // Recursive case

            Subject _CoverNodeSubject = (Subject)_CoverNode.GetSubject();

            // A. Go through each child node: (a) try to register for execution; (b) if successful, execute

            int NumBuildings = _CoverNodeSubject.NumBuildings;
            double ChildFactor = 1.0;

            foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
            {
                ExecutionRegister.GetOrAdd(_ChildCoverNode,
                    ExecuteCoverNode_(_ChildCoverNode, _TermGraph, CoverageIdGULossMap,
                        CoverageIdAttrMap, RCVCoveredAndAffectedBySubject, Bindings, ShouldAllocate));
                NumBuildings = Math.Max(NumBuildings, ((Subject)_ChildCoverNode.GetSubject()).NumBuildings);
                double _ChildCoverNodeFactor = ExecutionRegister[_ChildCoverNode].Factor;
                if (_ChildCoverNodeFactor != 1.0)
                    ChildFactor = _ChildCoverNodeFactor;
            }

            // B. Form subject

            if ((_CoverNode.GetSubject() as Subject).PerRisk
                && ((NumBuildings > 1) || (ChildFactor != 1.0)))
                _CoverNode.MarkAsMultiBuildingPerRisk();

            if (_CoverNode.IsMultiBuildingPerRisk())
            {
                List<double> S_vector = new List<double>();
                double Factor = 1.0;
                double[] FactorArray = null;
                for (int i = 0; ; i++)
                {
                    Dictionary<SimpleExpression<SymbolicValue>, double> ChildCoverPayoutBindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();
                    foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
                    {
                        if (i <= ExecutionRegister[_ChildCoverNode].P_vector.Length - 1)
                        {
                            ChildCoverPayoutBindings.Add(_ChildCoverNode.GetContent().GetLabel(), ExecutionRegister[_ChildCoverNode].P_vector[i]);
                            Factor = ExecutionRegister[_ChildCoverNode].Factor;
                            FactorArray = ExecutionRegister[_ChildCoverNode].FactorArray;
                        }
                    }
                    FunctionInvocation<IValue<AValue>> DerivedSubject
                        = _CoverNode.GetContent().GetDerivedSubject();
                    if (ChildCoverPayoutBindings.Count > 0)
                        S_vector.Add(DerivedSubject.GetEvaluatedValue(ChildCoverPayoutBindings));
                    else
                        break;
                }

                SubjectPosition = new CoverExecutionPosition(S_vector.ToArray<double>(),
                    Enumerable.Repeat(0.0, S_vector.Count).ToArray(), FactorArray, NumBuildings);
            }

            else
            {
                List<double> SubjectPositions = new List<double>();
                FunctionInvocation<IValue<AValue>> DerivedSubject
                    = _CoverNode.GetContent().GetDerivedSubject();
                foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
                {
                    SubjectPositions.Add((DerivedSubject.GetFunctionDelegate()(ExecutionRegister[_ChildCoverNode].P_vector.Zip(ExecutionRegister[_ChildCoverNode].FactorArray, (a, b) => a * b).Cast<object>().ToArray()).GetEvaluatedValue()));
                }
                SubjectPosition
                   = new CoverExecutionPosition(new double[] { DerivedSubject.GetFunctionDelegate()(SubjectPositions.Cast<object>().ToArray()).GetEvaluatedValue() },
                       new double[] { 0.0 }, new double[] { 1.0 });
            }

            Bindings.Remove(new SymbolicExpression("RCVCovered"));
            Bindings.Remove(new SymbolicExpression("RCVAffected"));
            Bindings.Remove(new SymbolicExpression("Subject"));

            if (!AggregateState.ContainsKey(_CoverNode))
            {
                AggregateState.Add(_CoverNode, new Dictionary<SimpleExpression<SymbolicValue>, double>());
                AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateLimitState"), 0.0);
                AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateAttachmentState"), 0.0);
            }

            return _CoverNode.Execute(SubjectPosition, Bindings, AggregateState[_CoverNode]);
        }

        public double Execute(double SubjectLoss, 
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool Allocate = true)
        {
            double ContractPayout = 0.0;

            if (!ExecutionState.HasOperationStateChanged(SubjectLoss))
            {
                foreach (CoverNode RootNode in RootNodes)
                {
                    ExecutionRegister[RootNode].Coalesce();
                    ContractPayout += ExecutionRegister[RootNode].P;
                }

                return ContractPayout;
            }

            if (IdentityMap.Count == 0)
            {
                // Update execution state
                ExecutionState = new GraphOperationState(false, SubjectLoss);
                return 0.0;
            }

            ExecutionRegister = new ConcurrentDictionary<CoverNode, CoverExecutionPosition>();
            Allocation = new ConcurrentDictionary<CoverNode, double>();

            foreach (CoverNode RootNode in RootNodes)
            {
                ExecutionRegister.GetOrAdd(RootNode,
                    ExecuteCoverNode(RootNode, Bindings, SubjectLoss));

                ExecutionRegister[RootNode].Coalesce();

                ContractPayout += ExecutionRegister[RootNode].P;
            }

            // Update execution state
            ExecutionState = new GraphOperationState(false, SubjectLoss);

            //// Prepare a set of every termnode
            //HashSet<CoverNode> CoverNodes = new HashSet<CoverNode>(IdentityMap.Values);

            //// Go through each node: (a) try to register for execution; (b) if successful, execute
            //HashSet<Task> CoverNodeTasks = new HashSet<Task>();
            //foreach (CoverNode _CoverNode in CoverNodes)
            //{
            //    ExecutionRegister.GetOrAdd(_CoverNode, ExecuteCoverNode(_CoverNode, Bindings, SubjectLoss));
            //}

            //// Update execution state
            //ExecutionState = new GraphOperationState(false, SubjectLoss);

            //ContractPayout = 0.0;

            //foreach (CoverNode RootNode in RootNodes)
            //{
            //    ExecutionRegister[RootNode].Coalesce();
            //    ContractPayout += ExecutionRegister[RootNode].P;
            //}

            //// Allocate?

            //if (Allocate)
            //{
            //    //TODO: this.Allocate(SubjectLoss);
            //    // The line below is a dummy; intended to breakpoint a debugging session so that one can view RITEAllocation (Hi Slava! :)
            //    //Console.WriteLine(RITEAllocation);
            //}

            return ContractPayout;
        }

        private CoverExecutionPosition ExecuteCoverNode(CoverNode _CoverNode, 
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, double SubjectLoss)
        {
            // Base case : _CoverNode is leaf

            if ((GetChildrenOfNode(_CoverNode) == null) || (GetChildrenOfNode(_CoverNode).Count == 0))
            {
                Bindings.Remove(new SymbolicExpression("Subject"));
                Bindings.Add(new SymbolicExpression("Subject"), SubjectLoss);

                if (!AggregateState.ContainsKey(_CoverNode))
                {
                    AggregateState.Add(_CoverNode, new Dictionary<SimpleExpression<SymbolicValue>, double>());
                    AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateLimitState"), 0.0);
                    AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateAttachmentState"), 0.0);
                }

                return new CoverExecutionPosition(SubjectLoss, _CoverNode.Execute(Bindings, AggregateState[_CoverNode]));
            }

            // Recursive case

            // A. Go through each child node: (a) try to register for execution; (b) if successful, execute

            double SubjectPosition = 0.0;

            Dictionary<SimpleExpression<SymbolicValue>, double> ChildCoverPayoutBindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();
            
            foreach (CoverNode _ChildCoverNode in GetChildrenOfNode(_CoverNode))
            {
                ExecutionRegister.GetOrAdd(_ChildCoverNode, ExecuteCoverNode(_ChildCoverNode, Bindings, SubjectLoss));
                ExecutionRegister[_ChildCoverNode].Coalesce();
                ChildCoverPayoutBindings.Add(_ChildCoverNode.GetContent().GetLabel(), ExecutionRegister[_ChildCoverNode].P);
            }

            FunctionInvocation<IValue<AValue>> DerivedSubject
                    = _CoverNode.GetContent().GetDerivedSubject();

            SubjectPosition = DerivedSubject.GetEvaluatedValue(ChildCoverPayoutBindings);

            Bindings.Remove(new SymbolicExpression("Subject"));
            Bindings.Add(new SymbolicExpression("Subject"), SubjectPosition);

            if (!AggregateState.ContainsKey(_CoverNode))
            {
                AggregateState.Add(_CoverNode, new Dictionary<SimpleExpression<SymbolicValue>, double>());
                AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateLimitState"), 0.0);
                AggregateState[_CoverNode].Add(new SymbolicExpression("AggregateAttachmentState"), 0.0);
            }

            return new CoverExecutionPosition(SubjectLoss, _CoverNode.Execute(Bindings, AggregateState[_CoverNode]));
        }

        public void Allocate(Dictionary<long, Dictionary<string, TermAllocationPosition>> TermRITEAllocation)
        {
            if (ExecutionState.HasOperationStateChanged())
                throw new Exception("Cover Graph not yet executed or has changed since last execution!");

            RITEAllocation = new Dictionary<long, Dictionary<string, double>>();

            // Allocate all root nodes
            foreach (CoverNode RootNode in RootNodes)
            {
                // Allocate root nodes trivially
                if (!Allocation.ContainsKey(RootNode))
                    Allocation.TryAdd(RootNode, ExecutionRegister[RootNode].P);

                AllocateSubtree(RootNode, ExecutionRegister[RootNode].P, TermRITEAllocation);
            }
        }

        private void AllocateSubtree(CoverNode RootNode, double AllocatedAtRoot,
            Dictionary<long, Dictionary<string, TermAllocationPosition>> TermRITEAllocation)
        {
            ICollection<INode<IGenericCover>> Children = GetChildrenOfNode(RootNode);

            if ((Children != null) && (Children.Count > 0))
            {
                if (RootNode.GetContent().GetDerivedSubject().GetFunctionName().ToUpper().Equals("SUM"))
                {
                    double TotalChildrenPayout = Children.Aggregate(0.0, (accumulator, it) => accumulator 
                                                                                + ExecutionRegister[(CoverNode)it].P);
                    foreach (CoverNode ChildCoverNode in Children)
                    {
                        double ChildCoverNodeAllocation = AllocatedAtRoot * (ExecutionRegister[ChildCoverNode].P / TotalChildrenPayout);
                        if (!Allocation.ContainsKey(ChildCoverNode))
                            Allocation.TryAdd(ChildCoverNode, ChildCoverNodeAllocation);

                        AllocateSubtree(ChildCoverNode, ChildCoverNodeAllocation, TermRITEAllocation);
                    }
                }
                else if (RootNode.GetContent().GetDerivedSubject().GetFunctionName().ToUpper().Equals("MAX"))
                {
                    HashSet<CoverNode> MaxChildCovers = new HashSet<CoverNode>();

                    // Find MAX contributors
                    double MaxPayout = double.MinValue;
                    foreach (CoverNode ChildCoverNode in Children)
                    {
                        if (ExecutionRegister[ChildCoverNode].P > MaxPayout)
                        {
                            MaxPayout = ExecutionRegister[ChildCoverNode].P;
                            MaxChildCovers.Clear();
                            MaxChildCovers.Add(ChildCoverNode);
                        }
                        else if (ExecutionRegister[ChildCoverNode].P == MaxPayout)
                        {
                            MaxChildCovers.Add(ChildCoverNode);
                        }
                    }

                    // Allocate to MAX contributors
                    foreach (CoverNode ChildCoverNode in Children)
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
                    HashSet<CoverNode> MinChildCovers = new HashSet<CoverNode>();

                    // Find MIN contributors
                    double MinPayout = double.MaxValue;
                    foreach (CoverNode ChildCoverNode in Children)
                    {
                        if (ExecutionRegister[ChildCoverNode].P < MinPayout)
                        {
                            MinPayout = ExecutionRegister[ChildCoverNode].P;
                            MinChildCovers.Clear();
                            MinChildCovers.Add(ChildCoverNode);
                        }
                        else if (ExecutionRegister[ChildCoverNode].P == MinPayout)
                        {
                            MinChildCovers.Add(ChildCoverNode);
                        }
                    }

                    // Allocate to MIN contributors
                    foreach (CoverNode ChildCoverNode in Children)
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
                    if (!TermRITEAllocation.ContainsKey(RITEId))
                        continue;

                    if (!RITEAllocation.ContainsKey(RITEId))
                        RITEAllocation.Add(RITEId, new Dictionary<string, double>());

                    foreach (string COL in TermRITEAllocation[RITEId].Keys)
                    {
                        if (RootNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                        {
                            double AllocationContribution = ((TotalExposedRITEAllocation == 0.0) ? 0.0 : AllocatedAtRoot * TermRITEAllocation[RITEId][COL].R / TotalExposedRITEAllocation);

                            if (!RITEAllocation[RITEId].ContainsKey(COL))
                                RITEAllocation[RITEId].Add(COL, AllocationContribution);
                            else
                                RITEAllocation[RITEId][COL] = RITEAllocation[RITEId][COL] + AllocationContribution;
                        }
                    }
                }
            }
        }

        public void Allocate_(Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>> TermRITEAllocation)
        {
            if (ExecutionState.HasOperationStateChanged())
                throw new Exception("Cover Graph not yet executed or has changed since last execution!");

            RITEAllocation = new Dictionary<long, Dictionary<string, double>>();

            // Allocate all root nodes
            foreach (CoverNode RootNode in RootNodes)
            {
                // Allocate root nodes trivially
                if (!Allocation.ContainsKey(RootNode))
                    Allocation.TryAdd(RootNode, ExecutionRegister[RootNode].P);

                AllocateSubtree_(RootNode, ExecutionRegister[RootNode].P, TermRITEAllocation);
            }
        }

        private void AllocateSubtree_(CoverNode RootNode, double AllocatedAtRoot,
            Dictionary<long, Dictionary<string, TermAllocationPositionVectorized>> TermRITEAllocation)
        {
            ICollection<INode<IGenericCover>> Children = GetChildrenOfNode(RootNode);

            if ((Children != null) && (Children.Count > 0))
            {
                if (RootNode.GetContent().GetDerivedSubject().GetFunctionName().ToUpper().Equals("SUM"))
                {
                    double TotalChildrenPayout = Children.Aggregate(0.0, (accumulator, it) => accumulator
                                                                                + ExecutionRegister[(CoverNode)it].P);
                    foreach (CoverNode ChildCoverNode in Children)
                    {
                        double ChildCoverNodeAllocation = AllocatedAtRoot * (ExecutionRegister[ChildCoverNode].P / TotalChildrenPayout);
                        if (!Allocation.ContainsKey(ChildCoverNode))
                            Allocation.TryAdd(ChildCoverNode, ChildCoverNodeAllocation);

                        AllocateSubtree_(ChildCoverNode, ChildCoverNodeAllocation, TermRITEAllocation);
                    }
                }
                else if (RootNode.GetContent().GetDerivedSubject().GetFunctionName().ToUpper().Equals("MAX"))
                {
                    HashSet<CoverNode> MaxChildCovers = new HashSet<CoverNode>();

                    // Find MAX contributors
                    double MaxPayout = double.MinValue;
                    foreach (CoverNode ChildCoverNode in Children)
                    {
                        if (ExecutionRegister[ChildCoverNode].P > MaxPayout)
                        {
                            MaxPayout = ExecutionRegister[ChildCoverNode].P;
                            MaxChildCovers.Clear();
                            MaxChildCovers.Add(ChildCoverNode);
                        }
                        else if (ExecutionRegister[ChildCoverNode].P == MaxPayout)
                        {
                            MaxChildCovers.Add(ChildCoverNode);
                        }
                    }

                    // Allocate to MAX contributors
                    foreach (CoverNode ChildCoverNode in Children)
                    {
                        if (MaxChildCovers.Contains(ChildCoverNode))
                        {
                            double ChildCoverNodeAllocation = AllocatedAtRoot / MaxChildCovers.Count;
                            if (!Allocation.ContainsKey(ChildCoverNode))
                                Allocation.TryAdd(ChildCoverNode, ChildCoverNodeAllocation);
                            AllocateSubtree_(ChildCoverNode, ChildCoverNodeAllocation, TermRITEAllocation);
                        }
                        else
                        {
                            if (!Allocation.ContainsKey(ChildCoverNode))
                                Allocation.TryAdd(ChildCoverNode, 0.0);
                            AllocateSubtree_(ChildCoverNode, 0.0, TermRITEAllocation);
                        }
                    }
                }
                else if (RootNode.GetContent().GetDerivedSubject().GetFunctionName().ToUpper().Equals("MIN"))
                {
                    HashSet<CoverNode> MinChildCovers = new HashSet<CoverNode>();

                    // Find MIN contributors
                    double MinPayout = double.MaxValue;
                    foreach (CoverNode ChildCoverNode in Children)
                    {
                        if (ExecutionRegister[ChildCoverNode].P < MinPayout)
                        {
                            MinPayout = ExecutionRegister[ChildCoverNode].P;
                            MinChildCovers.Clear();
                            MinChildCovers.Add(ChildCoverNode);
                        }
                        else if (ExecutionRegister[ChildCoverNode].P == MinPayout)
                        {
                            MinChildCovers.Add(ChildCoverNode);
                        }
                    }

                    // Allocate to MIN contributors
                    foreach (CoverNode ChildCoverNode in Children)
                    {
                        if (MinChildCovers.Contains(ChildCoverNode))
                        {
                            double ChildCoverNodeAllocation = AllocatedAtRoot / MinChildCovers.Count;
                            if (!Allocation.ContainsKey(ChildCoverNode))
                                Allocation.TryAdd(ChildCoverNode, ChildCoverNodeAllocation);
                            AllocateSubtree_(ChildCoverNode, ChildCoverNodeAllocation, TermRITEAllocation);
                        }
                        else
                        {
                            if (!Allocation.ContainsKey(ChildCoverNode))
                                Allocation.TryAdd(ChildCoverNode, 0.0);
                            AllocateSubtree_(ChildCoverNode, 0.0, TermRITEAllocation);
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
                            TotalExposedRITEAllocation += (TermRITEAllocation[RITEId][COL].R_vector.Zip(TermRITEAllocation[RITEId][COL].FactorArray, (a, b) => a * b).Sum());
                        }
                    }
                }

                foreach (long RITEId in SubjectExposureMap[RootNode])
                {
                    if (!TermRITEAllocation.ContainsKey(RITEId))
                        continue;

                    if (!RITEAllocation.ContainsKey(RITEId))
                        RITEAllocation.Add(RITEId, new Dictionary<string, double>());

                    foreach (string COL in TermRITEAllocation[RITEId].Keys)
                    {
                        if (RootNodeSubject.CausesOfLoss.Contains(new SymbolicValue(COL)))
                        {
                            double AllocationContribution = ((TotalExposedRITEAllocation == 0.0) ? 0.0 : AllocatedAtRoot * (TermRITEAllocation[RITEId][COL].R_vector.Zip(TermRITEAllocation[RITEId][COL].FactorArray, (a, b) => a * b).Sum()) / TotalExposedRITEAllocation);

                            if (!RITEAllocation[RITEId].ContainsKey(COL))
                                RITEAllocation[RITEId].Add(COL, AllocationContribution);
                            else
                                RITEAllocation[RITEId][COL] = RITEAllocation[RITEId][COL] + AllocationContribution;
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
            CoverNode _value;
            bool result = IdentityMap.TryGetValue(Label, out _value);
            value = _value;
            return result;
        }

        #endregion
    }

    class PerRiskCoverExploder
    {
        List<WrappedCover> CoverList;
        Dictionary<string, HashSet<long>> ResolvedSchedule;
        HashSet<IGenericCover> ExplodedCoversCollection;
        Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap;

        #region Constructor
        public PerRiskCoverExploder(List<WrappedCover> _CoverList, Dictionary<long, RiskItemCharacteristicIDAttributes> _CoverageIdAttrMap,
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
            #region If Cover is Derived
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
            #endregion

            #region Else (cover is leaf)
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
            #endregion

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


