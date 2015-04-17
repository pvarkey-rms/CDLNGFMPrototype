using ExposureType = Rms.Cdl.Backend.DataObjects.ExposureType;
using JsonPrettyPrinterPlus;
using Newtonsoft.Json;

using Rms.Cdl.Backend.DataObjects;
using Rms.Utilities;
using Rms.DataServices.Common;
using Rms.DataServices.DataObjects.CDL;
using Rms.DataServices.DataObjects;
using Rms.DataServices.LogExtension;
using Rms.Platform.Infrastructure.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;

using ProtoBuf;

using ContractObject = RMS.ContractObjectModel.Contract;
using RMS.ContractGraphModel;
using RMS.ContractObjectModel;
using CompiledResult = System.Tuple<string, RMS.ContractObjectModel.Contract, RMS.ContractGraphModel.IContractGraph>;

using NGFMReference;
using NGFM.Reference.MatrixHDFM;

namespace RMS.Prototype.NGFM
{

    #region State Enumerations
    
    public enum ProcessState
    {
        None,/*---->*/CDLParsed,/*------->*/ContractGraphProcessing,/*---->*/ContractGraphBuilt,/*-->*/FMExecuting,/*-->*/FMExecuted,
             /*---->*/
                      CDLParseFailed,                               /*---->*/ContractGraphFailed,                  /*-->*/FMFailed
        //                  |                                                        |                                        |
        //                  V                                                        V                                        V
        //|<----------------<------------------------|<------------------------------<-----------------------|<---------------|
    }

    public enum DRState
    {
        DRNone,/*---->*/DRSimulated,
                     /*---->*/DRUploaded,
                     /*---->*/DRFailed
        //|<------------------------|
    }

    #endregion

    //*****************************************************************
    [Serializable]
    [ProtoContract]
    [ProtoInclude(5, typeof(PrimaryContractExposureData))]
    [ProtoInclude(6, typeof(TreatyContractExposureData))]
    public abstract class ContractExposureData
    {
        #region Fields

        public static TimeSpan totalTime;
        public string filePath { private set; get; }
        //protected static readonly IRmsLogger Logger;
        private StringBuilder BugMessage;

        public ProcessState state { protected set; get; }
        public DRState gustate { protected set; get; }

        public IJavaScriptHarness ParsingHarness;

        //--------------------------------------------------------------------

        private long _Id = 0;

        [ProtoMember(1)]
        public ContractExposure ConExp { protected set; get; }

        public string IR { private set; get; }

        public Dictionary<string, object> JISONJsonParseResult { private set; get; }

        [ProtoMember(2)]
        public ContractObject _ContractObject { protected set; get; }

        //[ProtoMember(3)]
        public IContractGraph _ContractGraph { protected set; get; }

        // map: scheduleName -> subschedule
        public Dictionary<string, Subschedule> SubSchedule { protected set; get; }

        // map: scheduleName -> set of coverageIds
        [ProtoMember(4)]
        public Dictionary<string, HashSet<long>> ResolvedSchedule { protected set; get; }

        // map: RiskItemCharacteristicID -> Parameters
        // RiskItemCharacteristicID should correctly be called ExposureID
        public Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap { protected set; get; }

        // map: RiskItemCharacteristicID -> OriginalAggregateRiskItemCharacteristicID
        public Dictionary<long, long> RiskItemCharacteristicIDOriginalAggregateMap { protected set; get; }

        // map: RiskItemCharacteristicID -> LocationID
        public Dictionary<long, long> RiskItemCharacteristicIDLocationIDMap { protected set; get; }

        // map: LocationId -> set of related RiskItemCharacteristics (i.e. coverages in the same location)
        public Dictionary<long, HashSet<long>> LocationIDToRelatedRiskItemCharacteristicsMap { protected set; get; }

        public GULoss _GULoss;

        public abstract SortedDictionary<DateTime, double> InputTimeSeries { protected set; get; }

        private List<string> AdjustedCOLPrecedences;

        #endregion

        #region Constructors

        //static ContractExposureData()
        //{
        //    string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        //    Directory.SetCurrentDirectory(directoryName);
        //    using (AppConfig.Change(directoryName + "\\RMSLogger.config"))
        //    {
        //        Logger = RmsLoggerFactory.GetLogger(typeof(ContractExposureData));
        //    }
        //}

        public ContractExposureData(long id, string cdl)
        {
            ConExp = new ContractExposure();
            ConExp.Contract = new Rms.DataServices.DataObjects.Contract();
            ConExp.ExposureID = id;
            this._Id = id;
            strCDL = cdl;
            this.state = ProcessState.None;
            this.gustate = DRState.DRNone;
        }

        public ContractExposureData(IJavaScriptHarness parsingHarness, bool isLogging,
            ContractExposure ce = null, string protobufExtractFile = "")
        {
            ParsingHarness = parsingHarness;

            ConExp = ce;
            if (ConExp != null)
            {
                this._Id = ConExp.ExposureID;
            }

            this.filePath = protobufExtractFile;
            state = ProcessState.None;
            gustate = DRState.DRNone;

            BugMessage = (isLogging) ? new StringBuilder() : null;
        }

        public ContractExposureData(ContractExposureData ced, bool deep = false)
        {
            ParsingHarness = ced.ParsingHarness;

            this._Id = ced._Id;

            if (deep) // deep copy
            {
                this.filePath = ced.filePath;

                ced.BugMessage = null;

                this.gustate = DRState.DRNone;
                this.state = ProcessState.None;

                this.ConExp = ProtoBuf.Serializer.DeepClone<ContractExposure>(ced.ConExp);
            }
            else // sufficiently deep copy
            {
                ced.BugMessage = null;

                this.gustate = DRState.DRNone;
                this.state = ced.state;
                
                this._ContractObject = ced._ContractObject;
                this.ResolvedSchedule = (ced.ResolvedSchedule != null) ? new Dictionary<string, HashSet<long>>(ced.ResolvedSchedule) : null;
                this.ExposureIDAttributeMap = 
                    (ced.ExposureIDAttributeMap != null) ? 
                    new Dictionary<long, RiskItemCharacteristicIDAttributes>(ced.ExposureIDAttributeMap) : null;
                this.RiskItemCharacteristicIDOriginalAggregateMap = 
                    (ced.RiskItemCharacteristicIDOriginalAggregateMap != null) ? 
                    new Dictionary<long, long>(ced.RiskItemCharacteristicIDOriginalAggregateMap) : null;
                this.RiskItemCharacteristicIDLocationIDMap =
                    (ced.RiskItemCharacteristicIDLocationIDMap != null) ?
                    new Dictionary<long, long>(ced.RiskItemCharacteristicIDLocationIDMap) : null;
                this.LocationIDToRelatedRiskItemCharacteristicsMap =
                    (ced.LocationIDToRelatedRiskItemCharacteristicsMap != null) ?
                    new Dictionary<long, HashSet<long>>(ced.LocationIDToRelatedRiskItemCharacteristicsMap) : null;

                this.AdjustedCOLPrecedences = ced.AdjustedCOLPrecedences;
            }
        }
        
        #endregion

        #region Diagnostics

        public void ToLog(string msg, params object[] args)
        {
            if (BugMessage != null)
                BugMessage.Append(string.Format("\n\r\t " + msg, args));
        }

        public void ClearBugLog(string msg = "")
        {
            if (BugMessage != null)
            {
                BugMessage.Clear();
                BugMessage.Append(msg);
            }
        }

        public string GetBugLog(){return (null != BugMessage) ? BugMessage.ToString() : "";}

        public bool IsGraphNotYetBuilt()
        {
            return state == ProcessState.None
                || state == ProcessState.CDLParsed
                || state == ProcessState.CDLParseFailed
                || state == ProcessState.ContractGraphFailed;
        }

        public void InterruptOrResetExecutionState()
        {
            if (state == ProcessState.FMExecuting || state == ProcessState.FMExecuted || state == ProcessState.FMFailed)
            {
                state = ProcessState.ContractGraphBuilt;
            }
        }

        public bool CanShowSchedules()
        {
            return (state == ProcessState.ContractGraphBuilt || state == ProcessState.ContractGraphFailed
                || state == ProcessState.FMExecuted || state == ProcessState.FMFailed);
        }

        public abstract Tuple<int, string> StateToString();

        #endregion

        #region ID

        public long Id 
        {
            protected set 
            {
                this._Id = value;
                if (ConExp != null)
                    ConExp.ExposureID = value; 
            }
            get
            {
                if (ConExp != null)
                    return ConExp.ExposureID;
                else return this._Id;
            } 
        }

        public void SetID(long id)
        {
            Id = id;
        }

        #endregion

        #region CDL

        public string strCDL { set { ConExp.Contract.CDLString = value; state = ProcessState.None; } get { return ConExp.Contract.CDLString; } }

        public virtual void ParseCDLUsingJISONJS()
        {
            try
            {
                #region Check
                if (string.IsNullOrEmpty(strCDL))
                    throw new Exception("CDL string is empty.");
                if (null == ParsingHarness)
                    throw new Exception("ParsingHarness is null");
                #endregion

                #region Parsing CDL
                this.JISONJsonParseResult = null; this.IR = "";
                var sw = Stopwatch.StartNew();
                string cdl = strCDL.Replace(System.Environment.NewLine, "     ");//change newlines to spaces
                this.JISONJsonParseResult = ParsingHarness.Parse(cdl);
                this.IR = JsonConvert.SerializeObject(this.JISONJsonParseResult).PrettyPrintJson();
                sw.Stop();
                totalTime += sw.Elapsed;
                string msg = "For contract " + this.Id + ",  parsing CDL took " + sw.PrettyPrintHighRes() + "!";
                //Logger.LogInfoFormatExt(msg);
                ToLog("\r\n " + msg);
                #endregion

                #region Check
                if (null == this.JISONJsonParseResult || this.JISONJsonParseResult.Count() == 0)
                    throw new Exception("JISON Json Parse Result is empty");
                if (string.IsNullOrEmpty(IR))
                    throw new Exception("Intermediate parsing Result is empty");
                #endregion

                state = ProcessState.CDLParsed;
            }
            catch (Exception ex) { state = ProcessState.CDLParseFailed; ToLog("\r\n Error: " + ex.Message); }
        }

        #endregion

        #region Hours Clause

        public bool ContainsHoursClauses()
        {
            if (null != this._ContractObject && this._ContractObject.GetHoursClauses() != null && this._ContractObject.GetHoursClauses().Count > 0)
                return true;
            else
                return false;
        }

        public HoursClause GetHoursClause()
        {
            var hoursClauses = this._ContractObject.GetHoursClauses();
            // TODO: deal with multiple hours clauses (for e.g. by COL) later
            return hoursClauses.First();
        }

        #endregion

        #region COMPILATION ARTIFACTS: Contract Object Model & Contract Graph
        
        /// <summary>
        /// ALERT: This method may change the contents of <code>ResolvedSchedule</code> via explosion of PerRisk schedules
        /// </summary>
        public abstract void BuildContractGraph(List<string[]> COLPrecedence);

        public virtual void BuildContractObjectModel()
        {
            BuildContractObjectModelFromIR();
        }

        protected static ContractObject GetContractObjectModel(Dictionary<string, object> IR,
            Dictionary<string, HashSet<long>> resolvedSchedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap)
        {
            return new ContractObject(IR, resolvedSchedule, CoverageIdAttrMap);
        }

        protected void BuildContractObjectModelFromIR()
        {
            this._ContractObject = GetContractObjectModel(this.JISONJsonParseResult, 
                this.ResolvedSchedule, this.ExposureIDAttributeMap);

            if (null == this._ContractObject)
                throw new Exception("Contract Object wasn't created.");
        }

        protected void PostBuildContractGraph(List<string[]> COLPrecedence)
        {
            if (null == this._ContractGraph)
                throw new Exception("Contract Graph wasn't built.");

            //Updating of COLs
            UpdateCOLs();
            if (COLs == null || COLs.Count() == 0)
                throw new Exception("Contract doesn't contain Cause Of Losses.");

            CheckScheduleSymbols();

            string[] contractCOLs = COLs.Where(kv => kv.Value).Select(kv => kv.Key).ToArray();

            AdjustedCOLPrecedences = GetAdjustedCOLPrecedence(COLPrecedence, contractCOLs);
        }

        private void CheckScheduleSymbols()
        {
            if (this._ContractObject != null && ResolvedSchedule != null)
            {
                var missed = new HashSet<string>();

                foreach (string scheduleSymbol in this._ContractObject.GetScheduleSymbols().Select(elem => elem.ToString()))
                    if (!(ResolvedSchedule.ContainsKey(scheduleSymbol) || ResolvedSchedule.ContainsKey(scheduleSymbol +".#")))
                        missed.Add(scheduleSymbol);

                if (missed.Count() > 0)
                    throw new Exception("Missed schedule symbol(s): \"" + string.Join(", ", missed.ToArray()) + "\""); 
            }
        }

        #endregion

        #region COLs

        protected Dictionary<string, bool> _COLs = null;

        /// <summary>
        /// map: COL -> is selected for FM calculation
        /// </summary>
        public Dictionary<string, bool> COLs
        {
            get
            {
                if (null == _COLs)
                    UpdateCOLs();
                return _COLs;
            }
        }

        protected virtual void UpdateCOLs()
        {
            HashSet<string> AnalysisCOLs = new HashSet<string>();
            if (ConfigurationManager.AppSettings.AllKeys.Contains("AnalysisCausesOfLoss"))
            {
                string[] arr = ConfigurationManager.AppSettings["AnalysisCausesOfLoss"]
                .Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (null != arr && arr.Length > 0)
                {
                    foreach (string COL in arr)
                    {
                        AnalysisCOLs.Add(COL.Trim());
                    }
                }
            }
            else
            {
                AnalysisCOLs.UnionWith(UniversalSubjectPosition.CausesOfLoss.Select(x => x.ToString()));
            }

            var newCOLs = new Dictionary<string, bool>();
            if (null != this._ContractObject)
            {
                var colSet = this._ContractObject.GetCausesOfLoss();
                if (null != colSet && colSet.Count() > 0)
                {
                    foreach (string col in colSet.Select(elem => elem.ToString()))
                    {
                        if (!AnalysisCOLs.Contains(col))
                            continue;
                        if (null != _COLs && _COLs.ContainsKey(col))
                            newCOLs.Add(col, _COLs[col]);
                        else
                            newCOLs.Add(col, true);
                    }
                }
            }
            _COLs = newCOLs;
        }

        #endregion

        #region Schedule

        public HashSet<long> GetRiteIds()
        {
            return ObjectUtilities.MergeValues(ResolvedSchedule);
        }

        #endregion

        #region Maps

        public abstract void ExtractExposureAndScheduleInfo(ConcurrentDictionary<long, ContractExposureData> cacheContractData = null);

        public bool DoneExtracting()
        {
            return (null != ExposureIDAttributeMap && ExposureIDAttributeMap.Count() > 0)
                && (null != ResolvedSchedule && ResolvedSchedule.Count() > 0);
        }

        #endregion

        #region Subject Position & Output (Payout & allocated) Position

        public void TransformDamageRatios(
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            bool simulated = false)
        {
            if (state == ProcessState.FMExecuted || state == ProcessState.FMFailed)
                state = ProcessState.ContractGraphBuilt;

            gustate = DRState.DRNone;

            var COLFilteredEventOccurrenceDRs = FilterDamageRatios(EventOccurrenceDRs);

            if (null != this._ContractObject && null != COLs && null != COLFilteredEventOccurrenceDRs && null != ExposureIDAttributeMap)
            {
                Dictionary<long, Loss>  _FlattenedGULosses = new Dictionary<long, Loss>();
                SortedDictionary<DateTime, double>  _InputTimeSeries = new SortedDictionary<DateTime, double>();
                Dictionary<long, Dictionary<string, double>> _GULossByExposure = new Dictionary<long, Dictionary<string, double>>();
                Dictionary<long, Dictionary<string, double>> _GULossByRiskItem = new Dictionary<long, Dictionary<string, double>>();
                double _TotalGULoss = 0.0;

                #region GULP : DR --> GU Transformation
                try
                {
                    bool flag1 = false, flag2 = false;

                    DateTime inception = this._ContractObject.GetInception();
                    DateTime expiration = this._ContractObject.GetExpiration();
                    DateTime yearStart = new DateTime(inception.Year, 1, 1);
                    List<long> listofRitesToProcess = new List<long>();
                    Dictionary<long, Tuple<int, uint>> LocationMaxLengthTimeStampMap = CreateLocationMaxLengthDictionary(COLFilteredEventOccurrenceDRs, out listofRitesToProcess);

                    foreach (string sCOL in COLFilteredEventOccurrenceDRs.Keys)
                    {
                        flag1 = true;
                        var sample = COLFilteredEventOccurrenceDRs[sCOL].First().Value;

                        foreach (long ExposureId in ExposureIDAttributeMap.Keys)
                        {
                            if (sample.ContainsKey(ExposureId))
                            {
                                flag2 = true;

                                DateTime ts = yearStart.AddDays(sample[ExposureId].Item2);

                                if (ts > expiration)
                                    continue;

                                var riteAttr = ExposureIDAttributeMap[ExposureId];
                                double val = riteAttr.Value / (double)riteAttr.NumBuildings;
                                List<double> GULs = sample[ExposureId].Item3.Select(d => (double)d * val).ToList();

                                // VERIFY IF BELOW IS NECESSARY FOR HI-RES
                                if (GULs.Count == 0)
                                    GULs = new List<double>() { 0.0 };

                                double TotalLoss = GULs.Sum() * sample[ExposureId].Item1;

                                // VERIFY IF BELOW IS POSSIBLE FOR HI-RES
                                //if (TotalLoss <= 0.001)
                                //    continue;

                                if (!_FlattenedGULosses.ContainsKey(ExposureId))
                                {
                                    _FlattenedGULosses.Add(ExposureId, new Loss(ts, sCOL, GULs, sample[ExposureId].Item1));
                                }
                                else
                                {
                                    _FlattenedGULosses[ExposureId].Append(ts, sCOL, GULs, sample[ExposureId].Item1);
                                }

                                if (!_InputTimeSeries.ContainsKey(ts))
                                {
                                    _InputTimeSeries.Add(ts, TotalLoss);
                                }
                                else
                                {
                                    _InputTimeSeries[ts] += TotalLoss;
                                }

                                if (!_GULossByExposure.ContainsKey(ExposureId))
                                    _GULossByExposure.Add(ExposureId, new Dictionary<string, double>());

                                long RiskItemID = ExposureIDAttributeMap[ExposureId].RITExposureId;

                                if (!_GULossByRiskItem.ContainsKey(RiskItemID))
                                    _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

                                if (!_GULossByExposure[ExposureId].ContainsKey(sCOL))
                                    _GULossByExposure[ExposureId].Add(sCOL, TotalLoss);
                                else
                                    _GULossByExposure[ExposureId][sCOL] += TotalLoss;

                                if (!_GULossByRiskItem[RiskItemID].ContainsKey(sCOL))
                                    _GULossByRiskItem[RiskItemID].Add(sCOL, TotalLoss);
                                else
                                    _GULossByRiskItem[RiskItemID][sCOL] += TotalLoss;

                                _TotalGULoss += TotalLoss;
                            }
                        }

                        HashSet<long> AlreadyProcessed = new HashSet<long>();

                        foreach (long ExposureId in RiskItemCharacteristicIDOriginalAggregateMap.Keys)
                        {
                            if (AlreadyProcessed.Contains(ExposureId))
                                continue;

                            if (sample.ContainsKey(ExposureId))
                            {
                                flag2 = true;

                                DateTime ts;

                                //if (sample[ExposureId].Item3.Count == 0)
                                //    continue;

                                ts = yearStart.AddDays(sample[ExposureId].Item2);

                                if (ts > expiration)
                                    continue;

                                foreach (long RelatedExposureId in LocationIDToRelatedRiskItemCharacteristicsMap[RiskItemCharacteristicIDLocationIDMap[ExposureId]])
                                {
                                    AlreadyProcessed.Add(RelatedExposureId);

                                    List<double> GULs;
                                    double factor = 1.0;
                                    long OriginalExposureId = RiskItemCharacteristicIDOriginalAggregateMap[RelatedExposureId];
                                    long LocationId = RiskItemCharacteristicIDLocationIDMap[RelatedExposureId];

                                    if (sample.ContainsKey(RelatedExposureId))
                                    {
                                        ts = yearStart.AddDays(sample[RelatedExposureId].Item2);
                                        var riteAttr = ExposureIDAttributeMap[OriginalExposureId];
                                        double val = riteAttr.Value / (double)riteAttr.NumBuildings;
                                        GULs = sample[RelatedExposureId].Item3.Select(d => (double)d * val).ToList();
                                        if (GULs.Count == 0)
                                        {
                                            var VectorLength = LocationMaxLengthTimeStampMap[LocationId].Item1;
                                            GULs = Enumerable.Repeat(0.0, VectorLength).ToList();
                                        }
                                        factor = sample[RelatedExposureId].Item1;
                                    }
                                    else
                                    {
                                        var VectorLength = LocationMaxLengthTimeStampMap[LocationId].Item1;
                                        GULs = Enumerable.Repeat(0.0, VectorLength).ToList();
                                    }

                                    double TotalLoss = GULs.Sum() * factor;

                                    //if (TotalLoss <= 0.001)
                                    //    continue;

                                    if (!_FlattenedGULosses.ContainsKey(OriginalExposureId))
                                    {
                                        _FlattenedGULosses.Add(OriginalExposureId, new Loss(ts, sCOL, GULs, factor));
                                    }
                                    else
                                    {
                                        _FlattenedGULosses[OriginalExposureId].Append(ts, sCOL, GULs, factor);
                                    }

                                    if (!_InputTimeSeries.ContainsKey(ts))
                                    {
                                        _InputTimeSeries.Add(ts, TotalLoss);
                                    }
                                    else
                                    {
                                        _InputTimeSeries[ts] += TotalLoss;
                                    }

                                    if (!_GULossByExposure.ContainsKey(OriginalExposureId))
                                        _GULossByExposure.Add(OriginalExposureId, new Dictionary<string, double>());

                                    long RiskItemID = ExposureIDAttributeMap[OriginalExposureId].RITExposureId;

                                    if (!_GULossByRiskItem.ContainsKey(RiskItemID))
                                        _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

                                    if (!_GULossByExposure[OriginalExposureId].ContainsKey(sCOL))
                                        _GULossByExposure[OriginalExposureId].Add(sCOL, TotalLoss);
                                    else
                                        _GULossByExposure[OriginalExposureId][sCOL] += TotalLoss;

                                    if (!_GULossByRiskItem[RiskItemID].ContainsKey(sCOL))
                                        _GULossByRiskItem[RiskItemID].Add(sCOL, TotalLoss);
                                    else
                                        _GULossByRiskItem[RiskItemID][sCOL] += TotalLoss;

                                    _TotalGULoss += TotalLoss;
                                }
                            }
                            else //If the sample doesn't contain the Rite, add an empty vector of Location's Max length to avoid unequal size arrays error 
                            {
                                if (!listofRitesToProcess.Contains(ExposureId)) //Process Rites that are only part of atleast one sample.
                                    continue;

                                #region Sunny Fix for handling related exposures

                                //long OriginalExposureId = RiskItemCharacteristicIDOriginalAggregateMap[ExposureId];
                                long LocationId = RiskItemCharacteristicIDLocationIDMap[ExposureId];
                                DateTime ts = yearStart.AddDays(LocationMaxLengthTimeStampMap[LocationId].Item2);
                                //double factor = 1.0;

                                //var VectorLength = LocationMaxLengthTimeStampMap[LocationId].Item1;
                                //List<double>  GULs = Enumerable.Repeat(0.0, VectorLength).ToList();


                                //DateTime ts = new DateTime();

                                //if (ts > expiration)
                                //    continue;

                                foreach (long RelatedExposureId in LocationIDToRelatedRiskItemCharacteristicsMap[RiskItemCharacteristicIDLocationIDMap[ExposureId]])
                                {
                                    AlreadyProcessed.Add(RelatedExposureId);

                                    List<double> GULs;
                                    double factor = 1.0;
                                    long OriginalExposureId = RiskItemCharacteristicIDOriginalAggregateMap[RelatedExposureId];
                                    LocationId = RiskItemCharacteristicIDLocationIDMap[RelatedExposureId];

                                    if (sample.ContainsKey(RelatedExposureId))
                                    {
                                        ts = yearStart.AddDays(sample[RelatedExposureId].Item2);
                                        var riteAttr = ExposureIDAttributeMap[OriginalExposureId];
                                        double val = riteAttr.Value / (double)riteAttr.NumBuildings;
                                        GULs = sample[RelatedExposureId].Item3.Select(d => (double)d * val).ToList();
                                        if (GULs.Count == 0)
                                        {
                                            var VectorLength = LocationMaxLengthTimeStampMap[LocationId].Item1;
                                            GULs = Enumerable.Repeat(0.0, VectorLength).ToList();
                                        }
                                        factor = sample[RelatedExposureId].Item1;
                                    }
                                    else
                                    {
                                        var VectorLength = LocationMaxLengthTimeStampMap[LocationId].Item1;
                                        GULs = Enumerable.Repeat(0.0, VectorLength).ToList();
                                    }

                                    double TotalLoss = GULs.Sum() * factor;

                                    if (!_FlattenedGULosses.ContainsKey(OriginalExposureId))
                                    {
                                        _FlattenedGULosses.Add(OriginalExposureId, new Loss(ts, sCOL, GULs, factor));
                                    }
                                    else
                                    {
                                        _FlattenedGULosses[OriginalExposureId].Append(ts, sCOL, GULs, factor);
                                    }

                                    if (!_InputTimeSeries.ContainsKey(ts))
                                    {
                                        _InputTimeSeries.Add(ts, TotalLoss);
                                    }
                                    else
                                    {
                                        _InputTimeSeries[ts] += TotalLoss;
                                    }

                                    if (!_GULossByExposure.ContainsKey(OriginalExposureId))
                                        _GULossByExposure.Add(OriginalExposureId, new Dictionary<string, double>());

                                    long RiskItemID = ExposureIDAttributeMap[OriginalExposureId].RITExposureId;

                                    if (!_GULossByRiskItem.ContainsKey(RiskItemID))
                                        _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

                                    if (!_GULossByExposure[OriginalExposureId].ContainsKey(sCOL))
                                        _GULossByExposure[OriginalExposureId].Add(sCOL, TotalLoss);
                                    else
                                        _GULossByExposure[OriginalExposureId][sCOL] += TotalLoss;

                                    if (!_GULossByRiskItem[RiskItemID].ContainsKey(sCOL))
                                        _GULossByRiskItem[RiskItemID].Add(sCOL, TotalLoss);
                                    else
                                        _GULossByRiskItem[RiskItemID][sCOL] += TotalLoss;

                                    _TotalGULoss += TotalLoss;
                                }

                                #endregion
                            }
                        }
                    }

                    _GULoss = new GULoss
                    {
                        FlattenedGULosses = _FlattenedGULosses,
                        InputTimeSeries = _InputTimeSeries,
                        GULossByExposure = _GULossByExposure,
                        GULossByRiskItem = _GULossByRiskItem,
                        TotalGULoss = _TotalGULoss
                    };
                    if (_GULoss.FlattenedGULosses.Count() > 0)
                    {
                        gustate = (simulated) ? DRState.DRSimulated : DRState.DRUploaded;
                        if (state == ProcessState.FMFailed || state == ProcessState.FMExecuted)
                            state = ProcessState.ContractGraphBuilt;
                    }
                    else
                    {
                        gustate = DRState.DRNone;

                        if (COLs.Count() == 0)
                            ToLog("\r\n Error: Contract doesn't contain any Cause Of Losses.");
                        else if (!flag1)
                            ToLog("\r\n Error: Cause Of Losses of Contract are not matched to uploaded Ground Up Losses.");
                        else if (!flag2)
                            ToLog("\r\n Error: Coverage IDs from extract are not matched to uploaded Ground Up Losses.");
                    }
                }
                catch (Exception ex) { gustate = DRState.DRFailed; ToLog("\r\n Error: " + ex.Message); }
                #endregion

            }
        }
    
        private Dictionary<long, Tuple<int, uint>> CreateLocationMaxLengthDictionary(Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> COLFilteredEventOccurrenceDRs, out List<long> listofRitesToProcess)
        {
            listofRitesToProcess = new List<long>();
            //LocationID --> MaxLength, TimeStamp
            Dictionary<long, Tuple<int, uint>> LocationMaxLengthTimeStampMap = new Dictionary<long, Tuple<int, uint>>();

            if (RiskItemCharacteristicIDLocationIDMap == null || RiskItemCharacteristicIDLocationIDMap.Count == 0)
                return LocationMaxLengthTimeStampMap;

            foreach (string COL in COLFilteredEventOccurrenceDRs.Keys)
            {
                var sample = COLFilteredEventOccurrenceDRs[COL].First().Value;
                foreach (long RiteID in sample.Keys)
                {
                    if (!RiskItemCharacteristicIDLocationIDMap.Keys.Contains(RiteID))
                        continue;

                    long LocationID = RiskItemCharacteristicIDLocationIDMap[RiteID];
                    if (LocationMaxLengthTimeStampMap.Keys.Contains(LocationID))
                    {
                        Tuple<int, uint> curr = LocationMaxLengthTimeStampMap[LocationID];
                        if (sample[RiteID].Item3.Count > curr.Item1)
                            LocationMaxLengthTimeStampMap[LocationID] = new Tuple<int, uint>(sample[RiteID].Item3.Count, sample[RiteID].Item2);
                    }
                    else
                    {
                        int length = 1;
                        if (sample[RiteID].Item3.Count > 0)
                            length = sample[RiteID].Item3.Count;
                        LocationMaxLengthTimeStampMap.Add(LocationID, new Tuple<int, uint>(length, sample[RiteID].Item2));
                    }
                    if (!listofRitesToProcess.Contains(RiteID))
                        listofRitesToProcess.Add(RiteID);
                }
            }
            return LocationMaxLengthTimeStampMap;
        }
        
        //public void TransformDamageRatios(
        //    Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
        //    bool simulated = false)
        //{
        //    if (state == ProcessState.FMExecuted || state == ProcessState.FMFailed)
        //        state = ProcessState.ContractGraphBuilt;

        //    gustate = DRState.DRNone;

        //    var COLFilteredEventOccurrenceDRs = FilterDamageRatios(EventOccurrenceDRs);

        //    if (null != this._ContractObject && null != COLs && null != COLFilteredEventOccurrenceDRs && null != ExposureIDAttributeMap)
        //    {
        //        Dictionary<long, Loss>  _FlattenedGULosses = new Dictionary<long, Loss>();
        //        SortedDictionary<DateTime, double>  _InputTimeSeries = new SortedDictionary<DateTime, double>();
        //        Dictionary<long, Dictionary<string, double>> _GULossByExposure = new Dictionary<long, Dictionary<string, double>>();
        //        Dictionary<long, Dictionary<string, double>> _GULossByRiskItem = new Dictionary<long, Dictionary<string, double>>();
        //        double _TotalGULoss = 0.0;

        //        #region GULP : DR --> GU Transformation
        //        try
        //        {
        //            bool flag1 = false, flag2 = false;

        //            DateTime inception = this._ContractObject.GetInception();
        //            DateTime expiration = this._ContractObject.GetExpiration();
        //            DateTime yearStart = new DateTime(inception.Year, 1, 1);
        //            foreach (string sCOL in COLFilteredEventOccurrenceDRs.Keys)
        //            {
        //                flag1 = true;
        //                var sample = COLFilteredEventOccurrenceDRs[sCOL].First().Value;

        //                foreach (long ExposureId in ExposureIDAttributeMap.Keys)
        //                {
        //                    if (sample.ContainsKey(ExposureId))
        //                    {
        //                        flag2 = true;

        //                        DateTime ts = yearStart.AddDays(sample[ExposureId].Item2);

        //                        if (ts > expiration)
        //                            continue;

        //                        var riteAttr = ExposureIDAttributeMap[ExposureId];
        //                        double val = riteAttr.Value / (double)riteAttr.NumBuildings;
        //                        List<double> GULs = sample[ExposureId].Item3.Select(d => (double)d * val).ToList();

        //                        // VERIFY IF BELOW IS NECESSARY FOR HI-RES
        //                        if (GULs.Count == 0)
        //                            GULs = new List<double>() { 0.0 };

        //                        double TotalLoss = GULs.Sum() * sample[ExposureId].Item1;

        //                        // VERIFY IF BELOW IS POSSIBLE FOR HI-RES
        //                        //if (TotalLoss <= 0.001)
        //                        //    continue;

        //                        if (!_FlattenedGULosses.ContainsKey(ExposureId))
        //                        {
        //                            _FlattenedGULosses.Add(ExposureId, new Loss(ts, sCOL, GULs, sample[ExposureId].Item1));
        //                        }
        //                        else
        //                        {
        //                            _FlattenedGULosses[ExposureId].Append(ts, sCOL, GULs, sample[ExposureId].Item1);
        //                        }

        //                        if (!_InputTimeSeries.ContainsKey(ts))
        //                        {
        //                            _InputTimeSeries.Add(ts, TotalLoss);
        //                        }
        //                        else
        //                        {
        //                            _InputTimeSeries[ts] += TotalLoss;
        //                        }

        //                        if (!_GULossByExposure.ContainsKey(ExposureId))
        //                            _GULossByExposure.Add(ExposureId, new Dictionary<string, double>());

        //                        long RiskItemID = ExposureIDAttributeMap[ExposureId].RITExposureId;

        //                        if (!_GULossByRiskItem.ContainsKey(RiskItemID))
        //                            _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

        //                        if (!_GULossByExposure[ExposureId].ContainsKey(sCOL))
        //                            _GULossByExposure[ExposureId].Add(sCOL, TotalLoss);
        //                        else
        //                            _GULossByExposure[ExposureId][sCOL] += TotalLoss;

        //                        if (!_GULossByRiskItem[RiskItemID].ContainsKey(sCOL))
        //                            _GULossByRiskItem[RiskItemID].Add(sCOL, TotalLoss);
        //                        else
        //                            _GULossByRiskItem[RiskItemID][sCOL] += TotalLoss;

        //                        _TotalGULoss += TotalLoss;
        //                    }
        //                }

        //                HashSet<long> AlreadyProcessed = new HashSet<long>();

        //                foreach (long ExposureId in RiskItemCharacteristicIDOriginalAggregateMap.Keys)
        //                {
        //                    if (AlreadyProcessed.Contains(ExposureId))
        //                        continue;

        //                    if (sample.ContainsKey(ExposureId))
        //                    {
        //                        flag2 = true;

        //                        DateTime ts;

        //                        if (sample[ExposureId].Item3.Count == 0)
        //                            continue;

        //                        ts = yearStart.AddDays(sample[ExposureId].Item2);

        //                        if (ts > expiration)
        //                            continue;

        //                        foreach (long RelatedExposureId in LocationIDToRelatedRiskItemCharacteristicsMap[RiskItemCharacteristicIDLocationIDMap[ExposureId]])
        //                        {
        //                            AlreadyProcessed.Add(RelatedExposureId);

        //                            List<double> GULs;
        //                            double factor = 1.0;
        //                            long OriginalExposureId = RiskItemCharacteristicIDOriginalAggregateMap[RelatedExposureId];

        //                            if (sample.ContainsKey(RelatedExposureId))
        //                            {
        //                                ts = yearStart.AddDays(sample[RelatedExposureId].Item2);
        //                                var riteAttr = ExposureIDAttributeMap[OriginalExposureId];
        //                                double val = riteAttr.Value / (double)riteAttr.NumBuildings;
        //                                GULs = sample[RelatedExposureId].Item3.Select(d => (double)d * val).ToList();
        //                                if (GULs.Count == 0)
        //                                    GULs = new List<double>() { 0.0 };
        //                                factor = sample[RelatedExposureId].Item1;
        //                            }
        //                            else
        //                            {
        //                                var VectorLength = sample[ExposureId].Item3.Count;
        //                                if (VectorLength == 0)
        //                                    VectorLength = 1;
        //                                GULs = Enumerable.Repeat(0.0, VectorLength).ToList();
        //                            }

        //                            double TotalLoss = GULs.Sum() * factor;

        //                            //if (TotalLoss <= 0.001)
        //                            //    continue;

        //                            if (!_FlattenedGULosses.ContainsKey(OriginalExposureId))
        //                            {
        //                                    _FlattenedGULosses.Add(OriginalExposureId, new Loss(ts, sCOL, GULs, factor));
        //                            }
        //                            else
        //                            {
        //                                    _FlattenedGULosses[OriginalExposureId].Append(ts, sCOL, GULs, factor);
        //                            }

        //                            if (!_InputTimeSeries.ContainsKey(ts))
        //                            {
        //                                _InputTimeSeries.Add(ts, TotalLoss);
        //                            }
        //                            else
        //                            {
        //                                _InputTimeSeries[ts] += TotalLoss;
        //                            }

        //                            if (!_GULossByExposure.ContainsKey(OriginalExposureId))
        //                                _GULossByExposure.Add(OriginalExposureId, new Dictionary<string, double>());

        //                            long RiskItemID = ExposureIDAttributeMap[OriginalExposureId].RITExposureId;

        //                            if (!_GULossByRiskItem.ContainsKey(RiskItemID))
        //                                _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

        //                            if (!_GULossByExposure[OriginalExposureId].ContainsKey(sCOL))
        //                                _GULossByExposure[OriginalExposureId].Add(sCOL, TotalLoss);
        //                            else
        //                                _GULossByExposure[OriginalExposureId][sCOL] += TotalLoss;

        //                            if (!_GULossByRiskItem[RiskItemID].ContainsKey(sCOL))
        //                                _GULossByRiskItem[RiskItemID].Add(sCOL, TotalLoss);
        //                            else
        //                                _GULossByRiskItem[RiskItemID][sCOL] += TotalLoss;

        //                            _TotalGULoss += TotalLoss;
        //                        }
        //                    }
        //                }
        //            }
        //            _GULoss = new GULoss
        //            {
        //                FlattenedGULosses = _FlattenedGULosses,
        //                InputTimeSeries = _InputTimeSeries,
        //                GULossByExposure = _GULossByExposure,
        //                GULossByRiskItem = _GULossByRiskItem,
        //                TotalGULoss = _TotalGULoss
        //            };
        //            if (_GULoss.FlattenedGULosses.Count() > 0)
        //            {
        //                gustate = (simulated) ? DRState.DRSimulated : DRState.DRUploaded;
        //                if (state == ProcessState.FMFailed || state == ProcessState.FMExecuted)
        //                    state = ProcessState.ContractGraphBuilt;
        //            }
        //            else
        //            {
        //                gustate = DRState.DRNone;

        //                if (COLs.Count() == 0)
        //                    ToLog("\r\n Error: Contract doesn't contain any Cause Of Losses.");
        //                else if (!flag1)
        //                    ToLog("\r\n Error: Cause Of Losses of Contract are not matched to uploaded Ground Up Losses.");
        //                else if (!flag2)
        //                    ToLog("\r\n Error: Coverage IDs from extract are not matched to uploaded Ground Up Losses.");
        //            }
        //        }
        //        catch (Exception ex) { gustate = DRState.DRFailed; ToLog("\r\n Error: " + ex.Message); }
        //        #endregion

        //        #region Check
        //        //if (null != GULosses)
        //        //{
        //        //    var hs = new HashSet<long>();
        //        //    foreach (var dict in GULosses.Values)
        //        //    {
        //        //        if (null != dict)
        //        //        {
        //        //            foreach (long id in dict.Keys)
        //        //            {
        //        //                hs.Add(id);
        //        //            }
        //        //        }
        //        //    }
        //        //    int n = hs.Count();
        //        //}
        //        #endregion
        //    }
        //}
        
        private Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> FilterDamageRatios(
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs)
        {
            var dr = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();
            if (null != EventOccurrenceDRs)
            {
                foreach (string AdjustedCOLPrecedence in AdjustedCOLPrecedences)
                {
                    List<string> AllCOLAndAdjustorsWithEquivalents = ProduceAllCOLAndAdjustorsWithEquivalents(AdjustedCOLPrecedence, Loss.COLEquivalencyMap);

                    foreach (string COLAndAdjustorsWithEquivalentsString in AllCOLAndAdjustorsWithEquivalents)
                    {
                        string key = COLAndAdjustorsWithEquivalentsString.Split(':')[0];//"A:B,C"

                        if (!dr.ContainsKey(key) && EventOccurrenceDRs.ContainsKey(COLAndAdjustorsWithEquivalentsString))
                            dr.Add(key, EventOccurrenceDRs[COLAndAdjustorsWithEquivalentsString]);
                    }
                }
            }
            return dr;
        }

        public static List<string> ProduceAllCOLAndAdjustorsWithEquivalents(string AdjustedCOLPrecedence, Dictionary<SymbolicValue, HashSet<SymbolicValue>> COLEquivalencyMap)
        {
            string[] colWithAdjustors = AdjustedCOLPrecedence.Split(':', ',');

            HashSet<SymbolicValue>[] COLAndAdjustorsWithEquivalents = new HashSet<SymbolicValue>[colWithAdjustors.Length];
            for (int i = 0; i < colWithAdjustors.Length; i++)
            {
                HashSet<SymbolicValue> equivalents = new HashSet<SymbolicValue>();
                if (Loss.COLEquivalencyMap.ContainsKey(colWithAdjustors[i]))
                    equivalents = COLEquivalencyMap[colWithAdjustors[i]];
                equivalents.Add(colWithAdjustors[i]);
                COLAndAdjustorsWithEquivalents[i] = equivalents;
            }

            return ProduceAllCOLAndAdjustorsWithEquivalents(COLAndAdjustorsWithEquivalents, true);
        }

        private static List<string> ProduceAllCOLAndAdjustorsWithEquivalents(HashSet<SymbolicValue>[] COLAndAdjustorsWithEquivalents, bool IsFirst = false)
        {
            if (COLAndAdjustorsWithEquivalents.Length == 1)
            {
                return COLAndAdjustorsWithEquivalents[0].Select(x => x.ToString()).ToList();
            }
            List<string> All = new List<string>();
            List<string> RestOfThem = ProduceAllCOLAndAdjustorsWithEquivalents(COLAndAdjustorsWithEquivalents.Skip(1).ToArray());
            foreach (SymbolicValue COLOrAdjustorsWithEquivalent in COLAndAdjustorsWithEquivalents[0])
            {
                foreach (string Rest in RestOfThem)
                {
                    All.Add(COLOrAdjustorsWithEquivalent.ToString() + ((IsFirst)?":":",") + Rest);
                }
            }
            return All;
        }

        public static List<string> GetAdjustedCOLPrecedence(List<string[]> COLsHierarchy, string[] contractCOLs)
        {
            #region Example 1: 
            // 1) COLPrecedence: A>B>D>C
            // 2) Contract contains: A, B, C
            // Filtering: A>B>D>C => A>B>C
            // Returns Adjusted COLs: A>B>C =>
            //  A      or A not adjusted
            //  B:A    or B adjusted for A
            //  B      or B not adjusted
            //  C:A,B  or C adjusted for A and B
            //  C:A    or C adjusted for A
            //  C:B    or C adjusted for B
            //  C      or C not adjusted
            #endregion

            #region Example 2:
            // Adjusted COLs: D>B>A>C =>
            //  ...
            //  C:D,B,A
            //  C:D,B  
            //  C:D,A  
            //  C:D    
            //  C:B,A  
            //  C:B    
            //  C:A    
            //  C      
            #endregion

            if (contractCOLs.Length == 0 || null == COLsHierarchy || COLsHierarchy.Count() == 0)
                return contractCOLs.ToList<string>();

            // Add anarchy COLs :)
            var adjustedCOLs = contractCOLs.Except(COLsHierarchy.Select(e => e.AsEnumerable())
                .Aggregate((a, b) => a.Union(b))).ToList();

            // Add hierarchy COLs
            foreach (var elem in COLsHierarchy)
            {
                var lst = new List<string>();
                foreach (string col in elem.Where(col => contractCOLs.Contains(col)))
                {
                    if (lst.Count() == 0)
                    {
                        if (!adjustedCOLs.Contains(col))
                            adjustedCOLs.Add(col);
                    }
                    else
                    {
                        foreach (string[] set in PowerSet(lst.ToArray()))
                        {
                            string key = (set.Length > 0) ? col + ":" + string.Join(",", set) : col;
                            if (adjustedCOLs.Contains(key))
                                adjustedCOLs.Remove(key);
                            adjustedCOLs.Add(key);
                        }
                    }

                    lst.Add(col);
                }
            }

            return adjustedCOLs;
        }

        private static List<string[]> PowerSet(string[] arr)
        {
            #region Example: 
            // arr[] = {D, B, A}
            //  D,B,A (1 1 1)=7 // binary code
            //  D,B   (1 1 0)=6
            //  D,A   (1 0 1)=5
            //  D     (1 0 0)=4
            //  B,A   (0 1 1)=3
            //  B     (0 1 0)=2
            //  A     (0 0 1)=1
            //        (0 0 0)=0
            #endregion

            var res = new List<string[]>();
            int m = 1 << arr.Length;
            int i = m-1;
            while (i >= 0)
            {
                var subset = new List<string>();
                for (int j = 0; j < arr.Length; j++)
                    if ((int)(i & (1 << j)) != 0)
                        subset.Add(arr[j]);
                subset.Sort();
                res.Add(subset.ToArray());
                i--;
            }
            return res;
        }

        private string AdjustCOL(string col)
        {
            var part = col.Split(new char[] { ':', ';' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

            string key = part[0];

            if (part.Length > 1)
            {
                var lkey = part[1].Split(new char[] { ',','&' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (lkey.Count() > 0)
                {
                    lkey.RemoveAll(elem => elem.Equals(key));
                    lkey.Sort();
                    key += ":" + string.Join(",", lkey.ToArray());
                }
            }
            return key;
        }

        #endregion

        #region FM
        // Event Occurence API
        public abstract ResultPosition ExecuteFM(
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            bool ShouldAllocate,
            Dictionary<long, ResultPosition> cacheContractIdToResult = null);

        // Period of Event Occurences API
        public virtual List<ResultPosition> ExecuteFM(
            List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodOccurrenceDRs,
            bool ShouldAllocate,
            Dictionary<long, List<ResultPosition>> cacheContractIdToResult = null)
        {
            List<ResultPosition> ResultPositionsForPeriod = new List<ResultPosition>(PeriodOccurrenceDRs.Count);

            int i = 0;
            foreach (var EventOccurrenceDR in PeriodOccurrenceDRs)
            {
                ResultPositionsForPeriod.Add(ExecuteFM(EventOccurrenceDR, ShouldAllocate, cacheContractIdToResult.ToDictionary(kv => kv.Key, kv => kv.Value[i])));
                i++;
            }

            return ResultPositionsForPeriod;
        }
        #endregion

    }
    //*****************************************************************
    [Serializable]
    [ProtoContract]
    public class PrimaryContractExposureData : ContractExposureData
    {
        #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION

        bool UseReference;
        ReferencePrototype _ReferencePrototype;

        #endregion

        #region Constructors

        public PrimaryContractExposureData(long id, string cdl)
            : base(id, cdl) 
        {
            ConExp.ContractType = Rms.DataServices.DataObjects.ContractType.Primary;
        }

        public PrimaryContractExposureData(IJavaScriptHarness parsingHarness, bool isLogging,
            ContractExposure ce, string protobufExtractFile = "", ReferencePrototype __ReferencePrototype = null)
            : base(parsingHarness, isLogging, ce, protobufExtractFile)
        {
            #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION
            UseReference = Convert.ToBoolean(ConfigurationManager.AppSettings["UseReference"]);
            if (UseReference)
                _ReferencePrototype = __ReferencePrototype;
            #endregion
        }

        public PrimaryContractExposureData(PrimaryContractExposureData ced, bool deep = false)
            : base(ced, deep)
        {
            if (deep) // deep copy
            { 
            }
            else
            {
                #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION
                if (ced.UseReference)
                {
                    this.UseReference = ced.UseReference;
                    this._ReferencePrototype = ced._ReferencePrototype;
                }
                #endregion

                else if (ced._ContractGraph != null)
                    this._ContractGraph = new PrimaryContractGraph((PrimaryContractGraph)ced._ContractGraph);

                else { }
            }
        }

        #endregion

        #region Diagnostics

        public override Tuple<int, string> StateToString()
        {
            int code = 0;//Done
            if (state == ProcessState.None)
                code = 1;//None
            else if (state == ProcessState.ContractGraphProcessing || state == ProcessState.FMExecuting)
                code = 2;//Processing
            else if (state == ProcessState.CDLParseFailed || state == ProcessState.ContractGraphFailed || state == ProcessState.FMFailed)
                code = -1;//Failed

            string str = state.ToString();
            if (2 == code)//Processing
                str += "...";
            else
            {
                if (-1 == code)//Failed
                    str = "Error: " + str + "!";

                string str2 = gustate.ToString();

                if (gustate == DRState.DRFailed)
                {
                    code = -1;//Failed
                    str2 = "Error: " + str2 + "!";
                }

                str += ", " + str2;
            }
            return new Tuple<int, string>(code, str);
        }

        #endregion

        #region Contract Graph

        public override void BuildContractGraph(List<string[]> COLsHierarchy)
        {
            try
            {
                state = ProcessState.ContractGraphProcessing;

                BuildContractObjectModelFromIR();

                UseReference = UseReference && IsContractObjectModelCompatibleWithVectorizedReference();

                #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION
                if (UseReference)
                {
                    _ReferencePrototype.PrepareContract(Id);
                }
                #endregion

                else
                {
                    this._ContractGraph = ContractGraphBuilder.BuildPrimaryContractGraph(this._ContractObject, this.ExposureIDAttributeMap, this.ResolvedSchedule);

                    PostBuildContractGraph(COLsHierarchy);
                }

                state = ProcessState.ContractGraphBuilt;
            }
            catch (Exception ex) 
            {
                state = ProcessState.ContractGraphFailed; ToLog("\r\n Error: " + ex.Message); 
            }
        }

        private bool IsContractObjectModelCompatibleWithVectorizedReference()
        {
            if (this._ContractObject == null)
                return false;

            if (this._ContractObject.IsThisReinsurance())
                return false;

            //if (this._ContractObject.ContractSubject is RMS.ContractObjectModel.Subject)
            //{
                if (((RMS.ContractObjectModel.Subject)this._ContractObject.ContractSubject).Schedule.ScheduleSymbols.Count > 1)
                    return false;

                foreach (ITerm<Value> Sublimit in this._ContractObject.Sublimits)
                    if (Sublimit.GetSubject().Schedule.ScheduleSymbols.Count > 1)
                        return false;

                foreach (ITerm<Value> Deductible in this._ContractObject.Deductibles)
                    if (Deductible.GetSubject().Schedule.ScheduleSymbols.Count > 1)
                        return false;

                foreach (ICover<Value, Value, Value> Cover in this._ContractObject.Covers)
                {
                    RMS.ContractObjectModel.Subject CoverSubject = (RMS.ContractObjectModel.Subject)Cover.GetSubject();
                    if (!CoverSubject.isDerived && CoverSubject.Schedule.ScheduleSymbols.Count > 1)
                        return false;
                }
            //}

            return true;
        }

        #endregion

        #region Maps

        public override void ExtractExposureAndScheduleInfo(ConcurrentDictionary<long, ContractExposureData> cacheContractData = null)
        {
            #region Check

            if (DoneExtracting())
                return;

            if (ConExp == null)
            {
                ToLog("\r\n Error: Contract Exposure is null");
                return;
            }
            else if (ConExp.Subschedules == null)
            {
                ToLog("\r\n Error: Contract Exposure doesn't contain Schedules.");
                return;
            }

            #endregion

            try
            {
                #region Extraction of Subschedule and preparation of ResolvedSchedule

                var ser = new ProtoCompressedSerializer();
                Dictionary<string, Subschedule> dss =
                    ser.Deserialize<Dictionary<string, Subschedule>>(ConExp.Subschedules, 0, ConExp.Subschedules.Length);

                if (null == dss || dss.Count == 0)
                {
                    throw new Exception("\r\n Error: Contract Exposure Schedules couldn't be deserialized.");
                }

                SubSchedule = (null != dss) ? dss.Where(p => p.Value != null && (p.Value.Ids != null || p.Value.CompressedIds != null))
                    .ToDictionary(p => p.Key, p => p.Value) : null;

                ResolvedSchedule = (null != dss) ? SubSchedule.ToDictionary(p => p.Key,
                    p => ((p.Value.Ids != null) ? p.Value.Ids : new HashSet<long>(p.Value.CompressedIds.Enumerable()))) : new Dictionary<string, HashSet<long>>();

                if (ResolvedSchedule.Count() == 0)
                {
                    throw new Exception("\r\n Error: Contract Exposure Schedules couldn't be deserialized.");
                }

                HashSet<long> AllRITEIdsFromSchedule = ResolvedSchedule.Aggregate(new HashSet<long>(), (a, b) => { a.UnionWith(b.Value); return a; });

                #endregion

                #region Extraction of Rite Attributes

                if (ConExp == null || ConExp.ContractSubjectExposures == null || ConExp.ContractSubjectExposures.Count == 0)
                    return;


                ExposureIDAttributeMap = new Dictionary<long, RiskItemCharacteristicIDAttributes>();
                RiskItemCharacteristicIDOriginalAggregateMap = new Dictionary<long, long>();
                RiskItemCharacteristicIDLocationIDMap = new Dictionary<long, long>();
                LocationIDToRelatedRiskItemCharacteristicsMap = new Dictionary<long, HashSet<long>>();

                foreach (ContractSubjectExposureOfRiteSchedule cseRites in ConExp.ContractSubjectExposures)
                {
                    if (cseRites == null || cseRites.RITECollectionExposure == null || cseRites.RITECollectionExposure.RITExposures == null)
                        continue;

                    foreach (RITExposure ritExposure in cseRites.RITECollectionExposure.RITExposures)
                    {
                        if (ritExposure == null || ritExposure.RiskitemCharacteristicsList == null || ritExposure.RiskitemCharacteristicsList.Items == null)
                            continue;

                        foreach (RiskItemCharacteristicsValuation idxEntry in ritExposure.RiskitemCharacteristicsList.Items)
                        {
                            bool IsDisaggregatedExposure = !AllRITEIdsFromSchedule.Contains(idxEntry.Id) 
                                && AllRITEIdsFromSchedule.Contains(idxEntry.ParentId);

                            bool IsHiResExposure = ((ritExposure.ClonedExposureId == null) || idxEntry.Id == idxEntry.ParentId) 
                                && AllRITEIdsFromSchedule.Contains(idxEntry.Id);

                            bool IsDisaggregatedExposureUsingExplodedSchedule = (idxEntry.Id != idxEntry.ParentId &&
                                AllRITEIdsFromSchedule.Contains(idxEntry.Id));

                            bool ShouldProcess = IsHiResExposure || IsDisaggregatedExposure || IsDisaggregatedExposureUsingExplodedSchedule;

                            if (ShouldProcess)
                            {
                                if ((IsHiResExposure || IsDisaggregatedExposureUsingExplodedSchedule) && !ExposureIDAttributeMap.ContainsKey(idxEntry.Id))
                                {
                                    ExposureIDAttributeMap.Add(idxEntry.Id,
                                        new RiskItemCharacteristicIDAttributes()
                                        {
                                            // from second foreach loop
                                            NumBuildings = ritExposure.CommonCharacteristics.NumBuildings,
                                            RITExposureId = ritExposure.ExposureID,
                                            OriginalRITExposureId = ritExposure.ClonedExposureId,
                                            OriginalRITECharacteristicId = (ritExposure.ClonedExposureId != null) ? idxEntry.ParentId : (long?)null,
                                            ParentRITExposureId = ritExposure.ParentRITExposureId,
                                            IsCampus = (ritExposure.ExposureID != ritExposure.ParentRITExposureId),

                                            // from third foreach loop
                                            ExposureType = (int)RiteType.GetExposureType((RiteType.ERiteType)idxEntry.RiteTypeId),
                                            Value = idxEntry.RITExposureValuationList.First().Value
                                        });
                                }

                                else if (IsDisaggregatedExposure)
                                {
                                    RiskItemCharacteristicIDOriginalAggregateMap.Add(idxEntry.Id, idxEntry.ParentId);
                                    RiskItemCharacteristicIDLocationIDMap.Add(idxEntry.Id, ritExposure.ExposureID);
                                    if (!LocationIDToRelatedRiskItemCharacteristicsMap.ContainsKey(ritExposure.ExposureID))
                                        LocationIDToRelatedRiskItemCharacteristicsMap.Add(ritExposure.ExposureID, new HashSet<long>());
                                    LocationIDToRelatedRiskItemCharacteristicsMap[ritExposure.ExposureID].Add(idxEntry.Id);
                                    if (!ExposureIDAttributeMap.ContainsKey(idxEntry.ParentId))
                                    {
                                        ExposureIDAttributeMap.Add(idxEntry.ParentId,
                                        new RiskItemCharacteristicIDAttributes()
                                        {
                                            // from second foreach loop
                                            NumBuildings = ritExposure.CommonCharacteristics.NumUnits,
                                            RITExposureId = (long)ritExposure.ClonedExposureId,
                                            OriginalRITExposureId = ritExposure.ClonedExposureId,
                                            OriginalRITECharacteristicId = (ritExposure.ClonedExposureId != null) ? idxEntry.ParentId : (long?)null,
                                            ParentRITExposureId = ritExposure.ParentRITExposureId,
                                            IsCampus = (ritExposure.ClonedExposureId != ritExposure.ParentRITExposureId),

                                            // from third foreach loop
                                            ExposureType = (int)RiteType.GetExposureType((RiteType.ERiteType)idxEntry.RiteTypeId),
                                            Value = idxEntry.RITExposureValuationList.First().Value * ritExposure.CommonCharacteristics.NumUnits /
                                                                ritExposure.CommonCharacteristics.NumBuildings
                                        });
                                    }
                                    else
                                    {
                                        //ExposureIDAttributeMap[idxEntry.ParentId].Value +=
                                        //    (idxEntry.RITExposureValuationList.First().Value * ritExposure.CommonCharacteristics.NumUnits /
                                        //                        ritExposure.CommonCharacteristics.NumBuildings);
                                        //ExposureIDAttributeMap[idxEntry.ParentId].NumBuildings +=
                                        //    ritExposure.CommonCharacteristics.NumUnits;
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion

                #region CheckCampus & Explode Rites If Campus

                try
                {
                    if (ExposureIDAttributeMap.Any(kv => kv.Value.IsCampus))
                        ResolvedSchedule = ExplodeRitesForCampus();
                }
                catch (Exception ex) 
                {
                    ToLog("\r\n Error in Extracting of Exposure Info: " + ex.Message);
                }
                
                #endregion
                
            }
            catch (Exception ex)
            {
                ToLog("\r\n Error in Extracting of Exposure Info: " + ex.Message);
                state = ProcessState.None;
            }
        }

        private Dictionary<string, HashSet<long>> ExplodeRitesForCampus()
        {
            //Nina: If Campus, expolode the ResolvedSchedule to extend primary RiteCharacteristics to primary+secondary RiteCharacteristics

            //Group CoverageIdAttr by ParentRitExposureID and by ExposureType
            var GroupedCoverageIdAttr = from CoverageIdAttr in ExposureIDAttributeMap.AsEnumerable()
                                        group CoverageIdAttr by CoverageIdAttr.Value.ParentRITExposureId into AttrByParentRiteId
                                        select
                                            new
                                            {
                                                ParentId = AttrByParentRiteId.Key,
                                                AttrbyExposureType =
                                                    from attr in AttrByParentRiteId
                                                    group attr by attr.Value.ExposureType into AttrByExposureType
                                                    select new { ExposureType = AttrByExposureType.Key, CoverageIdAttr = AttrByExposureType }
                                            };

            //Create ExplodedRitesMap : Map each RiteCharacteristicID(parent) to a Hashset of RiteCHaracteristicIDs (children)
            Dictionary<long, HashSet<long>> ExplodedRitesMap = new Dictionary<long, HashSet<long>>();
            foreach (var ParentIDgroup in GroupedCoverageIdAttr)
            {
                foreach (var ExposureTypegroup in ParentIDgroup.AttrbyExposureType)
                {
                    HashSet<long> ExplodedRites = new HashSet<long>();
                    long CoverageId = 0;
                    foreach (var coverageidAttr in ExposureTypegroup.CoverageIdAttr)
                    {
                        ExplodedRites.Add(coverageidAttr.Key);
                        if (coverageidAttr.Value.ParentRITExposureId == coverageidAttr.Value.RITExposureId)
                            CoverageId = coverageidAttr.Key;
                    }
                    if (!ExplodedRitesMap.ContainsKey(CoverageId))
                        ExplodedRitesMap.Add(CoverageId, ExplodedRites);
                }
            }

            //Loop through ResolvedSchedules, replace CoverageId with Exploded CoverageIds
            Dictionary<string, HashSet<long>> ExplodedSchedule = new Dictionary<string, HashSet<long>>();
            foreach (KeyValuePair<string, HashSet<long>> kvp in ResolvedSchedule)
            {
                HashSet<long> RiteCharacteristicIDs = new HashSet<long>();
                foreach (long RiteId in kvp.Value)
                {
                    KeyValuePair<long, HashSet<long>> explodedrites = ExplodedRitesMap.AsEnumerable()
                                                        .Where(y => y.Key == RiteId)
                                                        .FirstOrDefault();

                    if (!explodedrites.Equals(default(KeyValuePair<long, HashSet<long>>)))
                    {
                        RiteCharacteristicIDs.UnionWith(explodedrites.Value);
                    }
                    else
                        RiteCharacteristicIDs.Add(RiteId);
                }

                ExplodedSchedule.Add(kvp.Key, RiteCharacteristicIDs);
            }
            return ExplodedSchedule;
        }
        
        #endregion

        #region Input TimeSeries

        public override SortedDictionary<DateTime, double> InputTimeSeries
        {
            protected set { }
            get
            {
                //if (null != GULosses)
                //    return new SortedDictionary<DateTime, double>(
                //        GULosses.Where(elem => null != elem.Value).ToDictionary(kvp => kvp.Key, kvp =>
                //        kvp.Value.Values.Where(v => null != v).Select(loss => loss.Amount)
                //        .Aggregate((a, b) => a + b)));
                if (null != _GULoss)
                    return _GULoss.InputTimeSeries;
                return null;
            }
        }

        #endregion

        #region FM

        public override ResultPosition ExecuteFM(
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            bool ShouldAllocate,
            Dictionary<long, ResultPosition> cacheContractIdToResult = null)
        {
            var sw = Stopwatch.StartNew();
            ResultPosition _ResultPosition = null;
            try
            {
                #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION
                if (UseReference)
                {
                     ReferenceResultOutput ResultOutput = _ReferencePrototype.Execute(Id, GraphType.Auto, EventOccurrenceDRs);
                     _ResultPosition = new ResultPosition { PayOut = ResultOutput.TotalPayout, TotalGULoss = ResultOutput.TotalSubjectLoss};
                }
                #endregion

                else
                {
                    TransformDamageRatios(EventOccurrenceDRs);

                    if (gustate == DRState.DRNone)
                    {
                        //Dictionary<long, Dictionary<string, double>> _GULossByExposure;

                        //Dictionary<long, Dictionary<string, double>> _GULossByRiskItem;

                        //AllocateGUByExposureAndRiskItem(ExposureIDAttributeMap, _GULoss.FlattenedGULosses, out _GULossByExposure, out _GULossByRiskItem);

                        return new ResultPosition
                        {
                            TotalGULoss = 0.0,
                            GULossByRITE = _GULoss.GULossByExposure,
                            GULossByRiskItem = _GULoss.GULossByRiskItem,
                            PayOut = 0.0,
                            RITEAllocation = _GULoss.GULossByExposure,
                            RiskItemAllocation = _GULoss.GULossByRiskItem,
                            TimeAllocation = _GULoss.InputTimeSeries.ToDictionary(x => x.Key, x => 0.0).ToSortedDictionary(),
                            InputTimeSeries = _GULoss.InputTimeSeries
                        };
                    }

                    if (gustate == DRState.DRFailed)
                        return null;

                    if (null == this._ContractObject || null == this._ContractGraph)
                        throw new Exception("Contract Graph isn't built or Ground Up Losses are not uploaded");

                    state = ProcessState.FMExecuting;

                    _ResultPosition = this._ContractGraph.Execute(ResolvedSchedule, ExposureIDAttributeMap, _GULoss, ShouldAllocate);
                }

                state = ProcessState.FMExecuted;
            }
            catch (Exception ex)
            {
                state = ProcessState.FMFailed;
                string msg = "Error: " + ex.Message;
                Console.WriteLine(msg);
                ToLog("\r\n " + msg);
                return null;
            }
            sw.Stop();
            totalTime += sw.Elapsed;
            return _ResultPosition;
        }

        private void AllocateGUByExposureAndRiskItem(Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap,
            Dictionary<long, Loss> ExposureIDGULossMap, out Dictionary<long, Dictionary<string, double>> _GULossByExposure,
            out Dictionary<long, Dictionary<string, double>> _GULossByRiskItem)
        {
            _GULossByExposure = new Dictionary<long, Dictionary<string, double>>();
            _GULossByRiskItem = new Dictionary<long, Dictionary<string, double>>();

            foreach (KeyValuePair<long, Loss> LossesPerRITEKVP in ExposureIDGULossMap)
            {
                long RITEId = (ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITECharacteristicId != null) ?
                    (long)ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITECharacteristicId :
                    LossesPerRITEKVP.Key;

                if (!_GULossByExposure.ContainsKey(RITEId))
                    _GULossByExposure.Add(RITEId, new Dictionary<string, double>());

                long RiskItemID = (ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITExposureId != null) ?
                    (long)ExposureIDAttributeMap[LossesPerRITEKVP.Key].OriginalRITExposureId :
                    ExposureIDAttributeMap[LossesPerRITEKVP.Key].RITExposureId;

                if (!_GULossByRiskItem.ContainsKey(RiskItemID))
                    _GULossByRiskItem.Add(RiskItemID, new Dictionary<string, double>());

                foreach (KeyValuePair<SymbolicValue, List<double>> LossPerRITEPerCOLKVP in LossesPerRITEKVP.Value.AmountByCOL)
                {
                    double TotalLossesPerRITEPerCOL
                        = LossesPerRITEKVP.Value[LossPerRITEPerCOLKVP.Key];

                    if (!_GULossByExposure[RITEId].ContainsKey(LossPerRITEPerCOLKVP.Key.ToString()))
                        _GULossByExposure[RITEId].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);
                    else
                        _GULossByExposure[RITEId][LossPerRITEPerCOLKVP.Key.ToString()] += TotalLossesPerRITEPerCOL;


                    if (!_GULossByRiskItem[RiskItemID].ContainsKey(LossPerRITEPerCOLKVP.Key.ToString()))
                        _GULossByRiskItem[RiskItemID].Add(LossPerRITEPerCOLKVP.Key.ToString(), TotalLossesPerRITEPerCOL);
                    else
                        _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()] =
                            _GULossByRiskItem[RiskItemID][LossPerRITEPerCOLKVP.Key.ToString()]
                                +
                            TotalLossesPerRITEPerCOL;
                }
            }
        }

        #endregion

    }
    //*****************************************************************
    [Serializable]
    [ProtoContract]
    public class TreatyContractExposureData : ContractExposureData
    {
        #region Fields

        private Dictionary<string, HashSet<long>> Positions;

        public override SortedDictionary<DateTime, double> InputTimeSeries { protected set; get; }

        #endregion

        #region Constructors

        public TreatyContractExposureData(long id, string cdl)
            : base(id, cdl)
        {
            ConExp.ContractType = Rms.DataServices.DataObjects.ContractType.Treaty;
        }

        public TreatyContractExposureData(IJavaScriptHarness parsingHarness, bool isLogging,
            ContractExposure ce, string protobufExtractFile = "")
            : base(parsingHarness, isLogging, ce, protobufExtractFile)
        {
            if (null == ce)
            {
                ConExp = new ContractExposure();
                ConExp.Contract = new Rms.DataServices.DataObjects.Contract();
                ConExp.ContractType = Rms.DataServices.DataObjects.ContractType.Treaty;
            }
            else 
                PopulatePositionsFromContractExposure();
        }

        public TreatyContractExposureData(IJavaScriptHarness parsingHarness, bool isLogging,
            long id, string strCDL, string file = "")
            : this(parsingHarness, isLogging, null, file)
        {
            if (null != ConExp)
            {
                ConExp.ExposureID = id;
                ConExp.ContractType = Rms.DataServices.DataObjects.ContractType.Treaty;
                ConExp.Contract.CDLString = (null != strCDL) ? strCDL : "";
            }
        }

        public TreatyContractExposureData(TreatyContractExposureData ced, bool deep = false)
            : base(ced, deep)
        {
            if (deep || ced._ContractGraph == null) // deep copy
            {
            }
            else
            {
                this._ContractGraph = new TreatyContractGraph((TreatyContractGraph)ced._ContractGraph);
                this.Positions = new Dictionary<string, HashSet<long>>(ced.Positions);
            }
        }

        #endregion

        #region Diagnostics

        public override Tuple<int, string> StateToString()
        {
            int code = 0;//Done
            if (state == ProcessState.None)
                code = 1;//None
            else if (state == ProcessState.ContractGraphProcessing || state == ProcessState.FMExecuting)
                code = 2;//Processing
            else if (state == ProcessState.CDLParseFailed || state == ProcessState.ContractGraphFailed || state == ProcessState.FMFailed)
                code = -1;//Failed

            string str = state.ToString();
            if (2 == code)//Processing
                str += "...";
            else
            {
                if (-1 == code)//Failed
                    str = "Error: " + str + "!";
            }
            return new Tuple<int, string>(code, str);
        }

        #endregion

        #region COMPILATION ARTIFACTS: Contract Object Model & Contract Graph (override)

        public TreatyContractGraph GetContractGraph() { return (TreatyContractGraph)base._ContractGraph; }

        #endregion

        #region Positions

        public HashSet<long> ChildContractExposureIDs 
        { 
            get 
            {
                if (null == Positions)
                    return new HashSet<long>();

                var positionToContractId = Utilities.AdjustPositions(Positions, PositionToOperation);
                return ObjectUtilities.MergeValues(positionToContractId);
            } 
        }

        public void RemoveFromPositions(params long[] ids)
        {
            if (Positions != null)
            {
                foreach (long id in ids)
                {
                    foreach (var hs in Positions.Values)
                        if (hs.Contains(id))
                            hs.Remove(id);
                }
                SavePositionsInsideContractExposure();
            }
        }

        private Dictionary<string, bool> PositionToOperation { 
            get 
            {
                if (null == this._ContractObject)
                {
                    if (null == this.JISONJsonParseResult)
                        ParseCDLUsingJISONJS();

                    if (null != this.JISONJsonParseResult)
                        this._ContractObject = GetContractObjectModel(this.JISONJsonParseResult, null, null);
                }

                return (null != this._ContractObject) ? this._ContractObject.GetPositionToOperation() : null;
            } 
        }

        public Dictionary<string, HashSet<long>> GetPositions()
        {
            return Positions;
        }

        public HashSet<long> PopulatePosition(string pos, HashSet<long> content)
        {
            if (null != Positions)
            {
                if (Positions.ContainsKey(pos))
                    Positions.Remove(pos);

                Positions.Add(pos, content);

                SavePositionsInsideContractExposure();

                //nulling Rite maps
                ResolvedSchedule = null;
                ExposureIDAttributeMap = null;
                _GULoss = null; //GULosses = null;
                state = ProcessState.None;
                gustate = DRState.DRNone;
            }
            return ObjectUtilities.MergeValues(Positions);
        }

        public void ClearPositions()
        {
            ResolvedSchedule = null;
            Positions = null;
            SavePositionsInsideContractExposure();
        }

        private HashSet<string> GetPositionsFromContract()
        {
            return (null != PositionToOperation) ? new HashSet<string>(PositionToOperation.Select(x => x.Key)) : null;
        }

        private void UpdatePositions()
        {
            var oldPos = Positions;
            Positions = null;
            HashSet<string> pos = GetPositionsFromContract();
            if (null != pos && pos.Count() > 0)
                Positions = pos.ToDictionary(elem => elem, elem => (null != oldPos && oldPos.ContainsKey(elem)) ? oldPos[elem] : new HashSet<long>());
            SavePositionsInsideContractExposure();
        }

        public HashSet<long> GetConExpIds()
        {
            return (null != Positions) ? ObjectUtilities.MergeValues(Positions) : new HashSet<long>();
        }

        public void SavePositionsInsideContractExposure()
        {
            if (ConExp != null)
            {
                if (null != Positions && Positions.Count() > 0)
                {
                    ConExp.Positions = Positions
                        .Select(kv => new Position() { PositionName = kv.Key, LossSourcePositionIDs = (null != kv.Value) ? kv.Value.ToList() : new List<long>() })
                        .ToList();
                }
                else
                    ConExp.Positions = new List<Position>();
            }
        }

        public void PopulatePositionsFromContractExposure()
        {
            if (null != ConExp && null != ConExp.Positions)
            {
                Positions = ConExp.Positions
                    .Where(elem => null != elem)
                    .ToDictionary(elem => elem.PositionName, elem => new HashSet<long>(elem.LossSourcePositionIDs.ToArray()));
            }
        }

        #endregion

        #region COLs

        protected override void UpdateCOLs()
        {
            base.UpdateCOLs();
            if (null == _COLs || _COLs.Count() == 0)
            {
                var allCOLs = UniversalSubjectPosition.CausesOfLoss;
                if (null != allCOLs)
                    _COLs = allCOLs.ToDictionary(col => col.ToString(), col => true);
            }
        }

        #endregion

        #region Maps

        public override void ExtractExposureAndScheduleInfo(ConcurrentDictionary<long, ContractExposureData> cacheContractData = null)
        {
            if (DoneExtracting())
                return;
            try
            {
                ResolvedSchedule = null;
                ExposureIDAttributeMap = null;

                var childrenIDs = ChildContractExposureIDs;
                var children = cacheContractData.Where(elem => null != elem.Value && childrenIDs.Contains(elem.Key))
                                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (null != children && children.Count != 0)
                {
                    #region Union of ResolvedSchedules

                    ResolvedSchedule = children.Where(elem => null != elem.Value.ResolvedSchedule)
                        .Select(elem => elem.Value.ResolvedSchedule)
                        .Aggregate(new Dictionary<string, HashSet<long>>(), (a, b) =>
                        {
                            foreach (var p in b)
                                if (!a.ContainsKey(p.Key))
                                    a.Add(p.Key, p.Value);
                                else
                                    a[p.Key].UnionWith(p.Value);
                            return a;
                        });
                    #endregion

                    #region Union of Rite Attributes
                    ExposureIDAttributeMap = children.Where(elem => null != elem.Value.ExposureIDAttributeMap)
                        .Select(elem => elem.Value.ExposureIDAttributeMap)
                        .Aggregate(new Dictionary<long, RiskItemCharacteristicIDAttributes>(), (a, b) =>
                        {
                            foreach (var p in b)
                                if (!a.ContainsKey(p.Key))
                                    a.Add(p.Key, p.Value);
                            return a;
                        });
                    #endregion

                    if (null == ResolvedSchedule || ResolvedSchedule.Count() == 0 || null == ExposureIDAttributeMap || ExposureIDAttributeMap.Count() == 0)
                        ToLog("\r\n Error: Sub Contracts have empty Schedules.");
                }
                else ToLog("\r\n Error: This Treaty Contract doesn't contain sub Contracts.");
            }
            catch (Exception ex) { ToLog("\r\n Error: " + ex.Message); }

        }
        
        #endregion

        #region Contract Graph

        public override void BuildContractGraph(List<string[]> COLsHierarchy)
        {
            try
            {
                state = ProcessState.ContractGraphProcessing;

                BuildContractObjectModelFromIR();

                #region Build Contract Graph
                var sw = Stopwatch.StartNew();
                this._ContractGraph = ContractGraphBuilder.BuildTreatyContractGraph(this._ContractObject);
                sw.Stop();
                totalTime += sw.Elapsed;
                string msg = "For treaty contract " + this.Id + ",  graph construction took " + sw.PrettyPrintHighRes() + "!";
                //Logger.LogInfoFormatExt(msg);
                ToLog("\r\n " + msg);
                #endregion

                PostBuildContractGraph(COLsHierarchy);

                UpdatePositions();

                state = ProcessState.ContractGraphBuilt;
            }
            catch (Exception ex) { state = ProcessState.ContractGraphFailed; ToLog("\r\n Error: " + ex.Message); }
        }

        #endregion

        #region FM

        public override ResultPosition ExecuteFM(
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            bool ShouldAllocate = true, 
            Dictionary<long, ResultPosition> cacheContractIdToResult = null)
        {
            ResultPosition _ResultPosition = null;
            state = ProcessState.FMExecuting;
            try
            {
                _ResultPosition = new ResultPosition();

                //SortedDictionary<DateTime, double> TimeSeriesPerPosition
                
                InputTimeSeries = null;
                var grossTrueCededFalsePositionMap = this._ContractObject.GetPositionToOperation();
                var childrenIDs = ChildContractExposureIDs;

                if (null != grossTrueCededFalsePositionMap && null != childrenIDs && null != Positions
                    && cacheContractIdToResult != null)
                {
                    var temp = cacheContractIdToResult
                        .Where(kv => (childrenIDs.Contains(kv.Key) && null != kv.Value && null != kv.Value.TimeAllocation && kv.Value.TimeAllocation.Count() > 0))
                        .ToDictionary(kv => kv.Key, kv => kv.Value.TimeAllocation);

                    var PositionToContractId = Utilities.AdjustPositions(Positions, grossTrueCededFalsePositionMap);
                    var TimeSeriesPerPosition = Utilities.CreatePositionToTimeAllocation(PositionToContractId, temp);

                    InputTimeSeries = Utilities.ApplyUnionOrSetDifferenceForPositionTimeSeries(TimeSeriesPerPosition, grossTrueCededFalsePositionMap);
                }
                if (InputTimeSeries == null)
                    throw new Exception("Error! Empty InputTimeSeries for ContractID: " + Id.ToString());
                if (InputTimeSeries.Values.Sum() == 0.0)
                {
                    return new ResultPosition
                    {
                        TotalGULoss = 0.0,
                        PayOut = 0.0,
                        TimeAllocation = InputTimeSeries.ToDictionary(x => x.Key, x => 0.0).ToSortedDictionary(),
                        InputTimeSeries = InputTimeSeries
                    };
                }

                ////Get Event Start Date
                DateTime TreatyEventStartDate = cacheContractIdToResult
                    .Where(x => ChildContractExposureIDs.Contains(x.Key))
                    .Select(x => x.Value)
                    .Select(RP => RP.InputTimeSeries.First().Key)
                    .Min();
                //Verify whether treaty is in force          
                if (TreatyEventStartDate < _ContractObject.GetInception() 
                    || TreatyEventStartDate > _ContractObject.GetExpiration())
                {
                    return new ResultPosition
                    {
                        TotalGULoss = 0.0,
                        PayOut = 0.0,
                        TimeAllocation = InputTimeSeries.ToDictionary(x => x.Key, x => 0.0).ToSortedDictionary(),
                        InputTimeSeries = InputTimeSeries
                    };
                }

                if (ContainsHoursClauses())
                {
                    _ResultPosition.ResultPositionsByHoursClauseOccurrences = new List<Tuple<SortedDictionary<DateTime, double>, ResultPosition>>();

                    HoursClause hc = GetHoursClause();

                    if (hc.GetDuration().Days == 0)
                        return new ResultPosition
                        {
                            TotalGULoss = 0.0,
                            PayOut = 0.0,
                            TimeAllocation = InputTimeSeries.ToDictionary(x => x.Key, x => 0.0).ToSortedDictionary(),
                            InputTimeSeries = InputTimeSeries
                        };

                    if (hc.OnlyOnce)
                    {
                        foreach (SortedDictionary<DateTime, double> InputTimeSeriesSectionedByHoursClause
                            in ApplyHoursClauseOnInputTimeSeries(hc))
                        {
                            _ResultPosition.ResultPositionsByHoursClauseOccurrences.Add(
                                new Tuple<SortedDictionary<DateTime, double>, ResultPosition>(InputTimeSeriesSectionedByHoursClause, 
                                    this._ContractGraph.Execute(InputTimeSeriesSectionedByHoursClause)));
                        }

                        var ResultPositionsByHoursClauseOccurrences =
                            _ResultPosition.ResultPositionsByHoursClauseOccurrences;

                        _ResultPosition = _ResultPosition.ResultPositionsByHoursClauseOccurrences.Select(x => x.Item2)
                                .Aggregate((a, b) => a.UnionWith(b));

                        _ResultPosition.ResultPositionsByHoursClauseOccurrences =
                            ResultPositionsByHoursClauseOccurrences;
                    }
                    else
                    {
                        _ResultPosition = FindMultipleLossOccurencesOnInputTimeSeries(hc);
                    }
                }
                else
                {
                    _ResultPosition = this._ContractGraph.Execute(InputTimeSeries);
                }

                _ResultPosition.InputTimeSeries = InputTimeSeries;

                state = ProcessState.FMExecuted;
            }
            catch (Exception ex) 
            {
                state = ProcessState.FMFailed;
                ToLog("\r\n " + ex.Message);
                return null; 
            }
            return _ResultPosition;
        }


        private ResultPosition FindMultipleLossOccurencesOnInputTimeSeries(HoursClause HoursClause)
        {
            TimeSpan HoursClauseDuration = HoursClause.GetDuration();

            // Make reference copy of graph that contains aggergate state at beginning of event
            TreatyContractGraph ReferenceGraph = new TreatyContractGraph((TreatyContractGraph)this._ContractGraph, false);

            SortedDictionary<DateTime, double> OriginalInputTimeSeries = new SortedDictionary<DateTime, double>(InputTimeSeries);
            Dictionary<DateTime, SortedDictionary<DateTime, double>> ListOfWindows = GenerateLOWindows(OriginalInputTimeSeries, HoursClauseDuration.Days);

            //Define Arrays
            List<DateTime> tsArr = ListOfWindows.Keys.ToList();
            TreatyContractGraph[] ContractGraphState = new TreatyContractGraph[tsArr.Count];
            double[] MaximumPayout = new double[tsArr.Count];
            double[] MaximumPayoutLOSubject = new double[tsArr.Count];
            List<DateTime>[] MaximumPayoutLOStartingDays = new List<DateTime>[tsArr.Count];

            Dictionary<DateTime, ResultPosition> ResultPositionOfMaximumPayoutLO = new Dictionary<DateTime, ResultPosition>();
            SortedDictionary<DateTime, double> TimeAllocation = new SortedDictionary<DateTime, double>();

            for (int i = 0; i < tsArr.Count; i++)
            {
                DateTime StartTime = tsArr[i];
                SortedDictionary<DateTime, double> CurrentLossOccurence = new SortedDictionary<DateTime, double>();
                ListOfWindows.TryGetValue(StartTime, out CurrentLossOccurence);
                int j = PreviousNonOverlapingWindow(i, tsArr, HoursClauseDuration.Days); //Find the starting day of previous non overlapping window

                if (j < 0)
                {
                    MaximumPayoutLOSubject[i] = CurrentLossOccurence.Values.Sum();  //Initialize subject of the current loss occurrence
                    MaximumPayoutLOStartingDays[i] = new List<DateTime> { tsArr[i] };

                    TreatyContractGraph CG = new TreatyContractGraph(ReferenceGraph, false);   //Create a clone and reset aggregate state

                    ResultPosition ResultPositionOfCurrentLO = CG.Execute(CurrentLossOccurence);

                    ContractGraphState[i] = CG;
                    ResultPositionOfMaximumPayoutLO.Add(tsArr[i], ResultPositionOfCurrentLO);

                    if (i == 0)
                    {
                        MaximumPayout[i] = ResultPositionOfCurrentLO.PayOut;
                    }
                    else
                    {
                        MaximumPayout[i] = Math.Max(MaximumPayout[i - 1], ResultPositionOfCurrentLO.PayOut);

                        if (MaximumPayout[i] == MaximumPayout[i - 1] && ((MaximumPayout[i] == 0 && MaximumPayoutLOSubject[i] <= MaximumPayoutLOSubject[i - 1]) || MaximumPayout[i] > 0))
                        {
                            MaximumPayoutLOSubject[i] = MaximumPayoutLOSubject[i - 1];
                            MaximumPayoutLOStartingDays[i] = MaximumPayoutLOStartingDays[i - 1];

                            ContractGraphState[i] = ContractGraphState[i - 1];
                            ResultPositionOfMaximumPayoutLO[tsArr[i]] = ResultPositionOfMaximumPayoutLO[tsArr[i - 1]];
                        }

                    }

                }
                else
                {
                    TreatyContractGraph CG = new TreatyContractGraph((TreatyContractGraph)ContractGraphState[j], false); //Create a clone of the current state of CG at j
                    ResultPosition ResultPositionOfCurrentLO = CG.Execute(CurrentLossOccurence);

                    MaximumPayout[i] = Math.Max(MaximumPayout[i - 1], ResultPositionOfCurrentLO.PayOut + MaximumPayout[j]);

                    if ((MaximumPayout[i] > MaximumPayout[i - 1]) || (MaximumPayout[i] == 0 && MaximumPayoutLOSubject[i] > MaximumPayoutLOSubject[i - 1]))
                    {
                        MaximumPayoutLOSubject[i] = MaximumPayoutLOSubject[j] + CurrentLossOccurence.Values.Sum();
                        List<DateTime> lst_startdates = MaximumPayoutLOStartingDays[j].Union(new List<DateTime> { tsArr[i] }).ToList();
                        MaximumPayoutLOStartingDays[i] = lst_startdates;

                        ContractGraphState[i] = CG;
                        ResultPositionOfMaximumPayoutLO.Add(tsArr[i], ResultPositionOfCurrentLO);
                    }
                    else
                    {
                        MaximumPayoutLOSubject[i] = MaximumPayoutLOSubject[i - 1];
                        MaximumPayoutLOStartingDays[i] = MaximumPayoutLOStartingDays[i - 1];

                        ContractGraphState[i] = ContractGraphState[i - 1];
                        ResultPositionOfMaximumPayoutLO[tsArr[i]] = ResultPositionOfMaximumPayoutLO[tsArr[i - 1]];

                    }
                }
            }

            this._ContractGraph = new TreatyContractGraph((TreatyContractGraph)ContractGraphState.Last(), false);

            foreach (DateTime dt in MaximumPayoutLOStartingDays.Last())
            {
                foreach (KeyValuePair<DateTime, double> item in ResultPositionOfMaximumPayoutLO[dt].TimeAllocation)
                    TimeAllocation.Add(item.Key, item.Value);
            }

            ResultPosition ResultPosition = new ResultPosition();
            ResultPosition.HoursClauseResults = new List<Tuple<HoursClause, HoursClauseOutput>>();
            ResultPosition.HoursClauseResults.Add(new Tuple<HoursClause, HoursClauseOutput>(HoursClause, new HoursClauseOutput(MaximumPayout, MaximumPayoutLOSubject, MaximumPayoutLOStartingDays)));
            ResultPosition.PayOut = MaximumPayout.Last();
            ResultPosition.TimeAllocation = TimeAllocation;
            return ResultPosition;
        }
        private Dictionary<DateTime, SortedDictionary<DateTime, double>> GenerateLOWindows(SortedDictionary<DateTime, double> OriginalInputTimeSeries, int duration)
        {
            Dictionary<DateTime, SortedDictionary<DateTime, double>> lst_LOWindows = new Dictionary<DateTime, SortedDictionary<DateTime, double>>();

            DateTime start = OriginalInputTimeSeries.First().Key;
            DateTime end = OriginalInputTimeSeries.Last().Key;
            List<DateTime> DatesList = Enumerable.Range(0, 1 + end.Subtract(start).Days).Select(offset => start.AddDays(offset)).ToList();
            int count = DatesList.Count();
            SortedDictionary<DateTime, double> Previous_LO = new SortedDictionary<DateTime, double>();
            for (int i = 0; i < count; i++)
            {
                DateTime StartTime = DatesList[i];
                DateTime EndTime = StartTime.AddDays(duration - 1);
                SortedDictionary<DateTime, double> LO = new SortedDictionary<DateTime, double>(
                    OriginalInputTimeSeries.Where(kvp => kvp.Key >= StartTime && kvp.Key <= EndTime).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

                //if (LO != null && LO.Count != 0 && (LO.Keys.Count != Previous_LO.Keys.Count || !LO.Keys.SequenceEqual(Previous_LO.Keys)))
                if (LO != null && LO.Count != 0)
                    lst_LOWindows.Add(StartTime, LO);

                Previous_LO = LO;
            }

            return lst_LOWindows;
        }

        public int PreviousNonOverlapingWindow(int i, List<DateTime> tsArr, int duration)
        {
            DateTime starti = tsArr[i];
            DateTime endi = tsArr[i].AddDays(duration - 1);
            List<DateTime> DatesListi = Enumerable.Range(0, 1 + endi.Subtract(starti).Days).Select(offset => starti.AddDays(offset)).ToList();
            for (int j = i - 1; j >= 0; j--)
            {
                DateTime startj = tsArr[j];
                DateTime endj = tsArr[j].AddDays(duration - 1);
                List<DateTime> DatesListj = Enumerable.Range(0, 1 + endj.Subtract(startj).Days).Select(offset => startj.AddDays(offset)).ToList();

                if (DatesListj.Last() < DatesListi.First())
                    return j;
            }
            return -1;
        }

        private IEnumerable<SortedDictionary<DateTime, double>> ApplyHoursClauseOnInputTimeSeries(HoursClause HoursClause)
        {
            if (null == InputTimeSeries || InputTimeSeries.Count() == 0)
                yield return InputTimeSeries;

            TimeSpan HoursClauseDuration = HoursClause.GetDuration();

            if (HoursClauseDuration.Days == 0)
                yield return InputTimeSeries;

            // TODO: verify if deep copy not needed
            SortedDictionary<DateTime, double> OriginalInputTimeSeries = new SortedDictionary<DateTime, double>(InputTimeSeries);

            do
            {
                double MaxAggLoss = OriginalInputTimeSeries.Values.Sum();
                DateTime StartTime = OriginalInputTimeSeries.First().Key;
                DateTime EndTime = OriginalInputTimeSeries.Last().Key;

                DateTime[] tsArr = OriginalInputTimeSeries.Keys.ToArray();

                int start = 0, end = 0, maxStart = 0, maxEnd = 0;
                double curSum = 0;
                MaxAggLoss = 0;

                while (end < tsArr.Length)
                {
                    if (end > 0)
                        curSum -= OriginalInputTimeSeries[tsArr[start++]];

                    while (end < tsArr.Length && (tsArr[end] - tsArr[start]) < HoursClauseDuration)
                        curSum += OriginalInputTimeSeries[tsArr[end++]];

                    if (curSum > MaxAggLoss)
                    {
                        MaxAggLoss = curSum;
                        maxStart = start;
                        maxEnd = (end > 0) ? end - 1 : end;
                    }
                }

                StartTime = tsArr[maxStart];
                EndTime = tsArr[maxEnd];

                SortedDictionary<DateTime, double> HoursClauseFilteredInputTimeSeries = new SortedDictionary<DateTime, double>(
                    OriginalInputTimeSeries.Where(kvp => kvp.Key >= StartTime && kvp.Key <= EndTime).
                    ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

                OriginalInputTimeSeries = OriginalInputTimeSeries.ExceptKeys<DateTime, double>(new HashSet<DateTime>(HoursClauseFilteredInputTimeSeries.Keys));

                yield return HoursClauseFilteredInputTimeSeries;
            }
            while (OriginalInputTimeSeries.Count > 0 && !HoursClause.OnlyOnce);
        }

        #endregion

    }
    //*****************************************************************

}
