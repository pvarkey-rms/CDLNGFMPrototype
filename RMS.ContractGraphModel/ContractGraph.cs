using System; 
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractGraphModel
{
    using IGenericCover = ICover<Value, Value, Value>;

    [Serializable]
    [ProtoContract]
    public class ContractGraph : IContractGraph
    {
        Contract TheContract;

        [ProtoMember(1)]
        CoverGraph _CoverGraph;

        [ProtoMember(2)]
        TermGraph _TermGraph;

        public ResultPosition ResultPosition { private set; get; }

        public static ContractGraph EMPTYCONTRACTGRAPH = null;

        #region Constructors
        public ContractGraph(Contract _TheContract)
        {
            TheContract = _TheContract;
            _CoverGraph = new CoverGraph();
            _TermGraph = new TermGraph();
        }
        public ContractGraph(ContractGraph CopyFrom, bool ResetAggregateState = true)
        {
            this.TheContract = CopyFrom.TheContract;
            this._CoverGraph = new CoverGraph(CopyFrom._CoverGraph, ResetAggregateState);
            this._TermGraph = new TermGraph(CopyFrom._TermGraph);
        }
        #endregion

        #region API

        public void TryCreateSubjectExposureForCoverLeafsFromTermRoots()
        {
            _CoverGraph.TryCreateSubjectExposureForLeafNodesFromTermRootsMap(_TermGraph);
        }

        public bool AddCovers(IList<IGenericCover> Covers, params object[] AuxilliaryInformation)
        {
            return _AddCovers(Covers, AuxilliaryInformation);
        }

        /// <summary>
        /// Concrete implementations of contract graphs must override this method 
        /// (see for e.g., PrimaryContractGraph & TreatyContractGraph) for RMS implementations
        /// </summary>
        protected virtual bool _AddCovers(IList<IGenericCover> Covers, params object[] AuxilliaryInformation)
        {
            return false;
        }

        protected bool AddTreatyCovers(IList<IGenericCover> Covers)
        {
            bool success = true;

            foreach (IGenericCover Cover in Covers)
                success &= _CoverGraph.Add(Cover, false);

            return success;
        }

        protected bool AddPrimaryCovers(IList<IGenericCover> Covers, Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<string, HashSet<long>> ResolvedSchedule)
        {

            bool success = true;

            //1. Save all the Covers in a warpper which will be used later to store mapping and info

            List<WrappedCover> WrappedCovers = new List<WrappedCover>();

            foreach (IGenericCover Cover in Covers)
            {
                WrappedCover CoverWrapper = new WrappedCover(Cover);
                WrappedCovers.Add(CoverWrapper);
            }

            //2. Explode all the covers in the wrapper and return a list of exploded covers that are ready to be added to the graph.
            
            PerRiskCoverExploder Exploder = new PerRiskCoverExploder(WrappedCovers, CoverageIdAttrMap, ResolvedSchedule);

            HashSet<IGenericCover> ExplodedCovers = Exploder.Explode();

            //3. Add all the prepared Covers to the graph
            
            foreach (IGenericCover Cover in ExplodedCovers)
            {
                success &= _CoverGraph.Add(Cover, CoverageIdAttrMap, ResolvedSchedule, false);
            }

            return success;
        }

        public bool RebuildCoverGraph()
        {
            return _CoverGraph.Rebuild();
        }
     
        public bool AddTerms(IList<ITerm<Value>> Terms, params object[] AuxilliaryInformation)
        {
            return AddTerms(Terms, 
                    (Dictionary<long, RiskItemCharacteristicIDAttributes>)AuxilliaryInformation[0],
                    (Dictionary<string, HashSet<long>>)AuxilliaryInformation[1]);
        }

        private bool AddTerms(IList<ITerm<Value>> Terms, Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<string, HashSet<long>> ResolvedSchedule)
        {
            bool success = true;
            foreach (ITerm<Value> Term in Terms)
                success &= _TermGraph.Add(Term, CoverageIdAttrMap, ResolvedSchedule);

            return success;
        }

        public bool RebuildTermGraph()
        {
            return _TermGraph.Rebuild();
        }

        public ResultPosition Execute(params object[] ExecutionParameters)
        {
            return _Execute(ExecutionParameters);
        }

        /// <summary>
        /// Concrete implementations of contract graphs must override this method 
        /// (see for e.g., PrimaryContractGraph & TreatyContractGraph) for RMS implementations
        /// </summary>
        protected virtual ResultPosition _Execute(params object[] ExecutionParameters)
        {
            return new ResultPosition { PayOut = 0.0 } ;
        }

        /// <summary>
        /// The order of executions and flow of data manifest in this method is fundamental to the correct execution of CDL.
        /// </summary>
        /// <param name="Schedule"></param>
        /// <param name="CoverageIdGULossMap"></param>
        protected ResultPosition ExecutePrimary(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap,
            SortedDictionary<DateTime, Dictionary<long, Loss>> GULosses,//Dictionary<long, Loss> CoverageIdGULossMap,
            bool ShouldAllocate)
        {
            #region 1. Flatten the time series & Compute InputTimeSeries

            var ExposureIDGULossMap = new Dictionary<long, Loss>();

            SortedDictionary<DateTime, double> _InputTimeSeries = new SortedDictionary<DateTime, double>();

            foreach (var KVPByTimestamp in GULosses)
            {
                double TotalLoss = 0.0;
                foreach (var KVPByExposureID in KVPByTimestamp.Value)
                {
                    if (!ExposureIDGULossMap.ContainsKey(KVPByExposureID.Key))
                        ExposureIDGULossMap.Add(KVPByExposureID.Key, KVPByExposureID.Value);
                    else
                        ExposureIDGULossMap[KVPByExposureID.Key].AddOrReplaceAmountByCOL(KVPByExposureID.Value);
                    TotalLoss += KVPByExposureID.Value.Amount;
                }
                _InputTimeSeries.Add(KVPByTimestamp.Key, TotalLoss);
            }

            //foreach (var kvp in GULosses.Values.Select(kv => kv.AsEnumerable()).Aggregate((a, b) => a.Union(b)))
            //{
            //    if (!ExposureIDGULossMap.ContainsKey(kvp.Key))
            //        ExposureIDGULossMap.Add(kvp.Key, kvp.Value);
            //    else
            //        ExposureIDGULossMap[kvp.Key].AddOrReplaceAmountByCOL(kvp.Value);
            //}

            #endregion

            #region 1.5 Compute (true) GU Loss By RITE and RiskItem

            double TotalGULoss = 0.0;

            Dictionary<long, Dictionary<string, double>> _GULossByRITE = new Dictionary<long, Dictionary<string, double>>();

            Dictionary<long, Dictionary<string, double>> _GULossByRiskItem = new Dictionary<long, Dictionary<string, double>>();

            // REAGGREGATION UPDATES
            //foreach (KeyValuePair<long,Loss> LossesPerRITEKVP in ExposureIDGULossMap)
            //{
            //    if (!_GULossByRITE.ContainsKey(LossesPerRITEKVP.Key))
            //        _GULossByRITE.Add(LossesPerRITEKVP.Key, new Dictionary<string, double>());

            //    long RiskItemID = ExposureIDAttributeMap[LossesPerRITEKVP.Key].RITExposureId;

            //    if (!_GULossByRiskItem.ContainsKey(RiskItemID))
            //        _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

            //    foreach (KeyValuePair<SymbolicValue, List<double>> LossPerRITEPerCOLKVP in LossesPerRITEKVP.Value.AmountByCOL)
            //    {
            //        double TotalLossesPerRITEPerCOL
            //            = LossesPerRITEKVP.Value[LossPerRITEPerCOLKVP.Key];
            //        _GULossByRITE[LossesPerRITEKVP.Key].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);

            //        if (!_GULossByRiskItem[RiskItemID].ContainsKey(LossPerRITEPerCOLKVP.Key.ToString()))
            //            _GULossByRiskItem[RiskItemID].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);
            //        else
            //            _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()] =
            //                _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()]
            //                    +
            //                TotalLossesPerRITEPerCOL;

            //        TotalGULoss += TotalLossesPerRITEPerCOL;
            //    }
            //    // @FACTORCHANGES REPLACE
            //    //foreach (KeyValuePair<SymbolicValue, List<double>> LossPerRITEPerCOLKVP in LossesPerRITEKVP.Value.AmountByCOL)
            //    //{
            //    //    double TotalLossesPerRITEPerCOL 
            //    //        = Loss.WeightedSum(LossPerRITEPerCOLKVP.Value, ExposureIDAttributeMap[LossesPerRITEKVP.Key].NumBuildings);
            //    //    _GULossByRITE[LossesPerRITEKVP.Key].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);

            //    //    if (!_GULossByRiskItem[RiskItemID].ContainsKey(LossPerRITEPerCOLKVP.Key.ToString()))
            //    //        _GULossByRiskItem[RiskItemID].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);
            //    //    else
            //    //        _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()] =
            //    //            _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()]
            //    //                +
            //    //            TotalLossesPerRITEPerCOL;

            //    //    TotalGULoss += TotalLossesPerRITEPerCOL;
            //    //}
            //}

            foreach (KeyValuePair<long, Loss> LossesPerRITEKVP in ExposureIDGULossMap)
            {
                long RITEId = (ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITECharacteristicId != null) ?
                    (long)ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITECharacteristicId :
                    LossesPerRITEKVP.Key;

                if (!_GULossByRITE.ContainsKey(RITEId))
                    _GULossByRITE.Add(RITEId, new Dictionary<string, double>());

                long RiskItemID = (ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITExposureId != null) ?
                    (long)ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITExposureId :
                    ExposureIDAttributeMap[LossesPerRITEKVP.Key].RITExposureId;

                if (!_GULossByRiskItem.ContainsKey(RiskItemID))
                    _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

                foreach (KeyValuePair<SymbolicValue, List<double>> LossPerRITEPerCOLKVP in LossesPerRITEKVP.Value.AmountByCOL)
                {
                    double TotalLossesPerRITEPerCOL
                        = LossesPerRITEKVP.Value[LossPerRITEPerCOLKVP.Key];

                    if (!_GULossByRITE[RITEId].ContainsKey(LossPerRITEPerCOLKVP.Key.ToString()))
                        _GULossByRITE[RITEId].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);
                    else
                        _GULossByRITE[RITEId][LossPerRITEPerCOLKVP.Key.ToString()] += TotalLossesPerRITEPerCOL;


                    if (!_GULossByRiskItem[RiskItemID].ContainsKey(LossPerRITEPerCOLKVP.Key.ToString()))
                        _GULossByRiskItem[RiskItemID].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);
                    else
                        _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()] =
                            _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()]
                                +
                            TotalLossesPerRITEPerCOL;

                    TotalGULoss += TotalLossesPerRITEPerCOL;
                }
            }

            #region GU Loss == 0 OPTIMIZATION
            //if (TotalGULoss < 5.0)
            //{
            //    return new ResultPosition
            //    {
            //        TotalGULoss = TotalGULoss,
            //        GULossByRITE = new Dictionary<long,Dictionary<string,double>>(),
            //        GULossByRiskItem = new Dictionary<long,Dictionary<string,double>>(),
            //        PayOut = 0.0,
            //        RITEAllocation = new Dictionary<long,Dictionary<string,double>>(),
            //        RiskItemAllocation = new Dictionary<long, Dictionary<string, double>>(),
            //        TimeAllocation = new SortedDictionary<DateTime,double>(),
            //        InputTimeSeries = _InputTimeSeries
            //    };
            //}
            #endregion

            #endregion

            #region VERIFY UNNECESSARY : 1. Bindings

            var Bindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();

            //// bind subjectvar
            //if (TheContract.ContractSubject is RMS.ContractObjectModel.Subject)
            //{
            //    RMS.ContractObjectModel.Subject contractSubject = TheContract.ContractSubject as RMS.ContractObjectModel.Subject;

            //    Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> ContractSubjectComponents2 =
            //        contractSubject.GetComponents();

            //    double SubjectLoss = 0.0;

            //    foreach (var Component2COL in ContractSubjectComponents2)
            //    {
            //        foreach (var Component2ResolvedExposureType in Component2COL.Value)
            //        {
            //            //HashSet<long> Component2RITEIds = new HashSet<long>();
            //            //foreach (long RITEId in ContractSubjectComponents2[Component2COL][Component2ResolvedExposureType].Item1)
            //            //    Component2RITEIds.Add(RITEId);

            //            foreach (long Component2RITEId in Component2ResolvedExposureType.Value.Item1)
            //            {
            //                if (ExposureIDGULossMap.ContainsKey(Component2RITEId) && ExposureIDGULossMap[Component2RITEId].AmountByCOL.ContainsKey(Component2COL.Key))
            //                    SubjectLoss += ExposureIDGULossMap[Component2RITEId].AmountByCOL[Component2COL.Key].Sum();
            //            }
            //        }
            //    }

            //    Bindings.Add(new SymbolicExpression("Subject"), SubjectLoss);
            //}

            #endregion

            #region 2. Get RCVAffected and RCVCovered for all cover and term subjects

            Dictionary<SubjectPosition, Tuple<double, double>> RCVCoveredAndAffectedBySubjectForCovers
                = new Dictionary<SubjectPosition, Tuple<double, double>>();

            Dictionary<TermNode, Tuple<double, double>> RCVCoveredAndAffectedBySubjectForTerms
                = new Dictionary<TermNode, Tuple<double, double>>();

            foreach (CoverNode _CoverNode in _CoverGraph)
            {
                if (((Subject)_CoverNode.GetContent().GetSubject()).IsDerived())
                    continue;

                if (!RCVCoveredAndAffectedBySubjectForCovers.ContainsKey(_CoverNode.GetSubject()))
                    RCVCoveredAndAffectedBySubjectForCovers.Add(_CoverNode.GetSubject(),
                        GetRCVCoveredAndAffected((Subject)_CoverNode.GetSubject(),
                            Schedule, ExposureIDAttributeMap, ExposureIDGULossMap));
            }

            foreach (TermNode _TermNode in _TermGraph)
            {
                if (!RCVCoveredAndAffectedBySubjectForTerms.ContainsKey(_TermNode))
                    RCVCoveredAndAffectedBySubjectForTerms.Add(_TermNode,
                        GetRCVCoveredAndAffected(_TermNode.GetSubject(),
                            Schedule, ExposureIDAttributeMap, ExposureIDGULossMap));
            }

            #endregion

            #region 3. Filter exposed RITEs (i.e. RITEs relevant to subject)
            
            ExposureIDGULossMap 
                = _CoverGraph.FilterExposedExposureIDs(Schedule, ExposureIDAttributeMap, ExposureIDGULossMap);

            #endregion

            #region 4. GO THROUGH COVER LEAF NODES & MARK AS MULTI-BLDG PER RISK & RESET CanUseTermRootsForCoverLeafSubjectExposures, IF NECESSARY
            _CoverGraph.DetectMultiBldgPerRiskForCoverLeafs(ExposureIDGULossMap, ExposureIDAttributeMap);
            #endregion

            #region 5. ShouldActuallyAllocateAnyway?
            bool ShouldActuallyAllocateAnyway =
                ShouldAllocate || _TermGraph.IsOverlapping || !_CoverGraph.CanUseTermRootsForCoverLeafSubjectExposures;
            #endregion

            #region 6. Execute terms (and, optionally, or, if necessary, allocate)
            _TermGraph.Execute(Schedule, ExposureIDAttributeMap, ExposureIDGULossMap,
                RCVCoveredAndAffectedBySubjectForTerms, Bindings,
                ShouldActuallyAllocateAnyway);
            #endregion

            #region 7. Create subject exposure map for covers
            if (ShouldActuallyAllocateAnyway)
                _CoverGraph.CreateSubjectExposureMapForLeafNodes(Schedule, ExposureIDGULossMap, ExposureIDAttributeMap);
            #endregion

            #region 8. Execute (and, optionally, or, if necessary, allocate)
            double Payout = _CoverGraph.Execute_(_TermGraph, ExposureIDGULossMap, ExposureIDAttributeMap,
                    RCVCoveredAndAffectedBySubjectForCovers, Bindings, ShouldActuallyAllocateAnyway);
            #endregion
            
            return new ResultPosition { 
                                            TotalGULoss = TotalGULoss, 
                                            GULossByRITE = _GULossByRITE,
                                            GULossByRiskItem = _GULossByRiskItem,
                                            PayOut = Payout,
                                            RITEAllocation = GetRITEAllocation(ExposureIDAttributeMap),
                                            RiskItemAllocation = GetRiskItemAllocation(ExposureIDAttributeMap),
                                            TimeAllocation = CalculatePayoutTimeSeries(Payout, GULosses, ExposureIDAttributeMap),
                                            InputTimeSeries = _InputTimeSeries
                                      };
        }

        /// <summary>
        /// The order of executions and flow of data manifest in this method is fundamental to the correct execution of CDL.
        /// </summary>
        /// <param name="Schedule"></param>
        /// <param name="CoverageIdGULossMap"></param>
        protected ResultPosition ExecutePrimary(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap,
            GULoss _GULoss,
            bool ShouldAllocate)
        {
            #region 1. Compute InputTimeSeries & ExposureIDGULossMap

            SortedDictionary<DateTime, double> _InputTimeSeries = _GULoss.InputTimeSeries;

            Dictionary<long, Loss> FlattenedGULosses = _GULoss.FlattenedGULosses;

            //SortedDictionary<DateTime, double> _InputTimeSeries = new SortedDictionary<DateTime, double>();

            //foreach (var KVPByTimestamp in ExposureIDGULossMap)
            //{
            //    foreach (var KVPByExposureID in KVPByTimestamp.Value.GUByCOLByTime.Values)
            //    {
            //        foreach (var KVPByExposureIDByDateTime in KVPByExposureID)
            //        {
            //            if (!_InputTimeSeries.ContainsKey(KVPByExposureIDByDateTime.Key))
            //                _InputTimeSeries.Add(KVPByExposureIDByDateTime.Key, KVPByExposureIDByDateTime.Value);
            //            else
            //                _InputTimeSeries[KVPByExposureIDByDateTime.Key] += KVPByExposureIDByDateTime.Value;
            //        }
            //    }
            //}

            #endregion

            #region 1.5 Compute (true) GU Loss By RITE and RiskItem

            Dictionary<long, Dictionary<string, double>> _GULossByExposure = _GULoss.GULossByExposure;

            Dictionary<long, Dictionary<string, double>> _GULossByRiskItem = _GULoss.GULossByRiskItem;

            double _TotalGULoss = _GULoss.TotalGULoss;
            
            //double TotalGULoss = 0.0;

            //Dictionary<long, Dictionary<string, double>> _GULossByRITE = new Dictionary<long, Dictionary<string, double>>();

            //Dictionary<long, Dictionary<string, double>> _GULossByRiskItem = new Dictionary<long, Dictionary<string, double>>();

            //foreach (KeyValuePair<long, Loss> LossesPerRITEKVP in FlattenedGULosses)
            //{
            //    long RITEId = (ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITECharacteristicId != null) ?
            //        (long)ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITECharacteristicId :
            //        LossesPerRITEKVP.Key;

            //    if (!_GULossByRITE.ContainsKey(RITEId))
            //        _GULossByRITE.Add(RITEId, new Dictionary<string, double>());

            //    long RiskItemID = (ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITExposureId != null) ?
            //        (long)ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITExposureId :
            //        ExposureIDAttributeMap[LossesPerRITEKVP.Key].RITExposureId;

            //    if (!_GULossByRiskItem.ContainsKey(RiskItemID))
            //        _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

            //    foreach (KeyValuePair<SymbolicValue, List<double>> LossPerRITEPerCOLKVP in LossesPerRITEKVP.Value.AmountByCOL)
            //    {
            //        double TotalLossesPerRITEPerCOL
            //            = LossesPerRITEKVP.Value[LossPerRITEPerCOLKVP.Key];

            //        if (!_GULossByRITE[RITEId].ContainsKey(LossPerRITEPerCOLKVP.Key.ToString()))
            //            _GULossByRITE[RITEId].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);
            //        else
            //            _GULossByRITE[RITEId][LossPerRITEPerCOLKVP.Key.ToString()] += TotalLossesPerRITEPerCOL;


            //        if (!_GULossByRiskItem[RiskItemID].ContainsKey(LossPerRITEPerCOLKVP.Key.ToString()))
            //            _GULossByRiskItem[RiskItemID].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);
            //        else
            //            _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()] =
            //                _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()]
            //                    +
            //                TotalLossesPerRITEPerCOL;

            //        TotalGULoss += TotalLossesPerRITEPerCOL;
            //    }
            //}

            #region GU Loss == 0 OPTIMIZATION
            //if (TotalGULoss < 5.0)
            //{
            //    return new ResultPosition
            //    {
            //        TotalGULoss = TotalGULoss,
            //        GULossByRITE = new Dictionary<long,Dictionary<string,double>>(),
            //        GULossByRiskItem = new Dictionary<long,Dictionary<string,double>>(),
            //        PayOut = 0.0,
            //        RITEAllocation = new Dictionary<long,Dictionary<string,double>>(),
            //        RiskItemAllocation = new Dictionary<long, Dictionary<string, double>>(),
            //        TimeAllocation = new SortedDictionary<DateTime,double>(),
            //        InputTimeSeries = _InputTimeSeries
            //    };
            //}
            #endregion

            #endregion

            #region VERIFY UNNECESSARY : 1. Bindings

            var Bindings = new Dictionary<SimpleExpression<SymbolicValue>, double>();

            //// bind subjectvar
            //if (TheContract.ContractSubject is RMS.ContractObjectModel.Subject)
            //{
            //    RMS.ContractObjectModel.Subject contractSubject = TheContract.ContractSubject as RMS.ContractObjectModel.Subject;

            //    Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> ContractSubjectComponents2 =
            //        contractSubject.GetComponents();

            //    double SubjectLoss = 0.0;

            //    foreach (var Component2COL in ContractSubjectComponents2)
            //    {
            //        foreach (var Component2ResolvedExposureType in Component2COL.Value)
            //        {
            //            //HashSet<long> Component2RITEIds = new HashSet<long>();
            //            //foreach (long RITEId in ContractSubjectComponents2[Component2COL][Component2ResolvedExposureType].Item1)
            //            //    Component2RITEIds.Add(RITEId);

            //            foreach (long Component2RITEId in Component2ResolvedExposureType.Value.Item1)
            //            {
            //                if (ExposureIDGULossMap.ContainsKey(Component2RITEId) && ExposureIDGULossMap[Component2RITEId].AmountByCOL.ContainsKey(Component2COL.Key))
            //                    SubjectLoss += ExposureIDGULossMap[Component2RITEId].AmountByCOL[Component2COL.Key].Sum();
            //            }
            //        }
            //    }

            //    Bindings.Add(new SymbolicExpression("Subject"), SubjectLoss);
            //}

            #endregion

            #region 2. Get RCVAffected and RCVCovered for all cover and term subjects

            Dictionary<SubjectPosition, Tuple<double, double>> RCVCoveredAndAffectedBySubjectForCovers
                = new Dictionary<SubjectPosition, Tuple<double, double>>();

            Dictionary<TermNode, Tuple<double, double>> RCVCoveredAndAffectedBySubjectForTerms
                = new Dictionary<TermNode, Tuple<double, double>>();

            foreach (CoverNode _CoverNode in _CoverGraph)
            {
                if (((Subject)_CoverNode.GetContent().GetSubject()).IsDerived())
                    continue;

                if (!RCVCoveredAndAffectedBySubjectForCovers.ContainsKey(_CoverNode.GetSubject()))
                    RCVCoveredAndAffectedBySubjectForCovers.Add(_CoverNode.GetSubject(),
                        GetRCVCoveredAndAffected((Subject)_CoverNode.GetSubject(),
                            Schedule, ExposureIDAttributeMap, FlattenedGULosses));
            }

            foreach (TermNode _TermNode in _TermGraph)
            {
                if (!RCVCoveredAndAffectedBySubjectForTerms.ContainsKey(_TermNode))
                    RCVCoveredAndAffectedBySubjectForTerms.Add(_TermNode,
                        GetRCVCoveredAndAffected(_TermNode.GetSubject(),
                            Schedule, ExposureIDAttributeMap, FlattenedGULosses));
            }

            #endregion

            #region 3. Filter exposed RITEs (i.e. RITEs relevant to subject)

            FlattenedGULosses = _CoverGraph.FilterExposedExposureIDs(Schedule, ExposureIDAttributeMap, FlattenedGULosses);

            #endregion

            #region 4. GO THROUGH COVER LEAF NODES & MARK AS MULTI-BLDG PER RISK & RESET CanUseTermRootsForCoverLeafSubjectExposures, IF NECESSARY
            _CoverGraph.DetectMultiBldgPerRiskForCoverLeafs(FlattenedGULosses, ExposureIDAttributeMap);
            #endregion

            #region 5. ShouldActuallyAllocateAnyway?
            bool ShouldActuallyAllocateAnyway =
                ShouldAllocate || _TermGraph.IsOverlapping || !_CoverGraph.CanUseTermRootsForCoverLeafSubjectExposures;
            #endregion

            #region 6. Execute terms (and, optionally, or, if necessary, allocate)
            _TermGraph.Execute(Schedule, ExposureIDAttributeMap, FlattenedGULosses,
                RCVCoveredAndAffectedBySubjectForTerms, Bindings,
                ShouldActuallyAllocateAnyway);
            #endregion

            #region 7. Create subject exposure map for covers
            if (ShouldActuallyAllocateAnyway)
                _CoverGraph.CreateSubjectExposureMapForLeafNodes(Schedule, FlattenedGULosses, ExposureIDAttributeMap);
            #endregion

            #region 8. Execute (and, optionally, or, if necessary, allocate)
            double Payout = _CoverGraph.Execute_(_TermGraph, FlattenedGULosses, ExposureIDAttributeMap,
                    RCVCoveredAndAffectedBySubjectForCovers, Bindings, ShouldActuallyAllocateAnyway);
            #endregion

            ResultPosition _ResultPosition = new ResultPosition
            {
                TotalGULoss = _TotalGULoss,
                GULossByRITE = _GULossByExposure,
                GULossByRiskItem = _GULossByRiskItem,
                PayOut = Payout,
                RITEAllocation = GetRITEAllocation(ExposureIDAttributeMap),
                RiskItemAllocation = GetRiskItemAllocation(ExposureIDAttributeMap),
                TimeAllocation = CalculatePayoutTimeSeries(Payout, _InputTimeSeries, ExposureIDAttributeMap),
                InputTimeSeries = _InputTimeSeries
            };

            FlattenedGULosses = null;
            _GULoss = null;

            return _ResultPosition;
        }

        private SortedDictionary<DateTime, double> CalculatePayoutTimeSeries(double Payout, SortedDictionary<DateTime, Dictionary<long, Loss>> GULosses,
            Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap)
        {
            var tall = new SortedDictionary<DateTime, double>();

            var rall = _CoverGraph.GetRITEAllocation();

            if (null != rall && rall.Count() > 0)
            {
                foreach (var kvp in GULosses.Where(elem => null != elem.Value))
                {
                    foreach (var kvp2 in kvp.Value.Where(elem => null != elem.Value))
                    {
                        if (rall.ContainsKey(kvp2.Key))
                        {
                            foreach (var kv in rall[kvp2.Key])
                            {
                                if (null != kvp2.Value.AmountByCOL && kvp2.Value.ContainsCOL(kv.Key))
                                // COL EQUIVALENCY CHANGES
                                //if (null != kvp2.Value.AmountByCOL && kvp2.Value.AmountByCOL.ContainsKey(new SymbolicValue(kv.Key)))
                                {
                                    if (!tall.ContainsKey(kvp.Key))
                                        tall.Add(kvp.Key, kv.Value);
                                    else
                                        tall[kvp.Key] += kv.Value;
                                }
                            }
                        }
                    }
                }
            }
            return tall;
        }

        private SortedDictionary<DateTime, double> CalculatePayoutTimeSeries(double Payout, SortedDictionary<DateTime, double> InputTimeSeries,
            Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap)
        {
            var tall = new SortedDictionary<DateTime, double>();

            double GU = InputTimeSeries.Values.Sum();

            foreach (KeyValuePair<DateTime, double> IKVP in InputTimeSeries)
            {
                tall.Add(IKVP.Key, Payout * IKVP.Value / GU);
            }

            return tall;
        }

        protected ResultPosition ExecuteTreaty(SortedDictionary<DateTime, double> InputSubjectPosition, Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, bool Allocate)
        {
            // Execute and return (and, optionally, allocate)
            double Payout = _CoverGraph.Execute(InputSubjectPosition.Values.Sum(), Bindings, Allocate);

            ResultPosition _ResultPosition = new ResultPosition { PayOut = Payout };
            
            _ResultPosition.TimeAllocation = new SortedDictionary<DateTime, double>();

            var TotalInputSubjectLoss = InputSubjectPosition.Values.Sum();

            foreach (DateTime dt in InputSubjectPosition.Keys)
            {
                _ResultPosition.TimeAllocation.Add(dt, _ResultPosition.PayOut * InputSubjectPosition[dt] / TotalInputSubjectLoss);
            }

            _ResultPosition.InputTimeSeries = InputSubjectPosition;

            return _ResultPosition;
        }

        private Tuple<double, double> GetRCVCoveredAndAffected(Subject _Subject,
            Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap,
            Dictionary<long, Loss> ExposureIDGULossMap)
        {
            double RCVCovered = 0.0;

            double RCVAffected = 0.0;

            foreach (SymbolicValue ScheduleSymbol in _Subject.Schedule.ScheduleSymbols)
            {
                if (!Schedule.ContainsKey(ScheduleSymbol.value))
                    continue;

                foreach (long CoverageId in Schedule[ScheduleSymbol.value])
                {
                    if (!ExposureIDAttributeMap.ContainsKey(CoverageId))
                        continue;

                    if (!_Subject.ResolvedExposureTypes.Contains(ExposureIDAttributeMap[CoverageId].ExposureType))
                        continue;

                    if (_Subject.PerRisk)
                        RCVCovered += ExposureIDAttributeMap[CoverageId].Value / ExposureIDAttributeMap[CoverageId].NumBuildings;
                    else
                        RCVCovered += ExposureIDAttributeMap[CoverageId].Value;

                    //RCVCovered += ExposureIDAttributeMap[CoverageId].Value / ExposureIDAttributeMap[CoverageId].NumBuildings;

                    if (ExposureIDGULossMap.ContainsKey(CoverageId))
                    {
                        Loss loss = ExposureIDGULossMap[CoverageId];
                        foreach (SymbolicValue RelevantCauseOfLoss in _Subject.CausesOfLoss)
                        {
                            if (loss.ContainsCOL(RelevantCauseOfLoss) && (loss[RelevantCauseOfLoss] > 0.0))
                            // COL EQUIVALENCY CHANGES
                            //if (loss.AmountByCOL.ContainsKey(RelevantCauseOfLoss) 
                            //    && (loss.AmountByCOL[RelevantCauseOfLoss].Sum() > 0.0))
                            {
                                if (_Subject.PerRisk && ExposureIDGULossMap[CoverageId].FactorArray.Length > 1)
                                    RCVAffected += ExposureIDAttributeMap[CoverageId].Value / ExposureIDAttributeMap[CoverageId].NumBuildings;
                                else
                                    RCVAffected += ExposureIDGULossMap[CoverageId].FactorArray
                                        .Select(x => x * (ExposureIDAttributeMap[CoverageId].Value / ExposureIDAttributeMap[CoverageId].NumBuildings))
                                        .Sum();
                                break;
                            }
                        }
                    }
                }
            }

            return new Tuple<double, double>(RCVCovered, RCVAffected);
        }

        private Dictionary<long, Dictionary<string, double>> GetRITEAllocation(Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap)
        {
            Dictionary<long, Dictionary<string, double>> OriginalRITEAllocation = _CoverGraph.GetRITEAllocation();

            Dictionary<long, Dictionary<string, double>> RITEAllocation = null;

            if (OriginalRITEAllocation != null)
            {
                RITEAllocation = new Dictionary<long, Dictionary<string, double>>();

                foreach (KeyValuePair<long, Dictionary<string, double>> _RITEAllocationKVP in OriginalRITEAllocation)
                {
                    if (ExposureIDAttributeMap[_RITEAllocationKVP.Key].OriginalRITECharacteristicId != null &&
                        ExposureIDAttributeMap[_RITEAllocationKVP.Key].OriginalRITECharacteristicId != _RITEAllocationKVP.Key)
                    {
                        long RITEId = (long)ExposureIDAttributeMap[_RITEAllocationKVP.Key].OriginalRITECharacteristicId;
                        
                        if (!RITEAllocation.ContainsKey(RITEId))
                            RITEAllocation.Add(RITEId, new Dictionary<string, double>());
                        foreach (KeyValuePair<string, double> _RITEAllocationKVPByCOLKVP in _RITEAllocationKVP.Value)
                        {
                            if (!RITEAllocation[RITEId].ContainsKey(_RITEAllocationKVPByCOLKVP.Key))
                                RITEAllocation[RITEId].Add(_RITEAllocationKVPByCOLKVP.Key, _RITEAllocationKVPByCOLKVP.Value);
                            else
                                RITEAllocation[RITEId][_RITEAllocationKVPByCOLKVP.Key] =
                                    RITEAllocation[RITEId][_RITEAllocationKVPByCOLKVP.Key] + _RITEAllocationKVPByCOLKVP.Value;
                        }
                    }
                    else
                    {
                        RITEAllocation.Add(_RITEAllocationKVP.Key, _RITEAllocationKVP.Value);
                    }
                }
            }

            return RITEAllocation;
        }

        private Dictionary<long, Dictionary<string, double>> GetRiskItemAllocation(Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap)
        {
            Dictionary<long, Dictionary<string, double>> RITEAllocation = _CoverGraph.GetRITEAllocation();

            Dictionary<long, Dictionary<string, double>> RiskItemAllocation = null;

            if (RITEAllocation != null)
            {
                RiskItemAllocation = new Dictionary<long, Dictionary<string, double>>();

                foreach (KeyValuePair<long, Dictionary<string, double>> _RITEAllocationKVP in RITEAllocation)
                {
                    long RiskItemID = (ExposureIDAttributeMap[_RITEAllocationKVP.Key].OriginalRITExposureId != null) ?
                    (long)ExposureIDAttributeMap[_RITEAllocationKVP.Key].OriginalRITExposureId :
                    ExposureIDAttributeMap[_RITEAllocationKVP.Key].RITExposureId;

                    if (!RiskItemAllocation.ContainsKey(RiskItemID))
                        RiskItemAllocation.Add(RiskItemID, new Dictionary<string, double>());
                    foreach (KeyValuePair<string, double> _RITEAllocationKVPByCOLKVP in _RITEAllocationKVP.Value)
                    {
                        if (!RiskItemAllocation[RiskItemID].ContainsKey(_RITEAllocationKVPByCOLKVP.Key))
                            RiskItemAllocation[RiskItemID].Add(_RITEAllocationKVPByCOLKVP.Key, _RITEAllocationKVPByCOLKVP.Value);
                        else
                            RiskItemAllocation[RiskItemID][_RITEAllocationKVPByCOLKVP.Key] =
                                RiskItemAllocation[RiskItemID][_RITEAllocationKVPByCOLKVP.Key] + _RITEAllocationKVPByCOLKVP.Value;
                    }
                }
            }

            return RiskItemAllocation;
        }

        #endregion
    }
}
