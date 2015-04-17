using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

using Newtonsoft.Json;
using JsonPrettyPrinterPlus;

using Rms.Analytics.DataService.Zip;
using ExposureType = Rms.Cdl.Backend.DataObjects.ExposureType;
using Rms.DataServices.Common;
using Rms.DataServices.DataObjects;
using Rms.DataServices.DataObjects.CDL;
using Rms.DataServices.LogExtension;
using Rms.Platform.Infrastructure.Diagnostics;
using Rms.Utilities;
using ProtoBuf;
using System.Net;

using RMS.ContractObjectModel;
using RMS.ContractGraphModel;
using PrototypeContract = RMS.ContractObjectModel.Contract;
using CompiledResult = System.Tuple<string, RMS.ContractObjectModel.Contract, RMS.ContractGraphModel.IContractGraph>;

using NGFMReference;

namespace RMS.Prototype.NGFM
{
    //**************************************************
    public class HDFM
    {
        #region Auxiliary fields
        
        // Javascript parsing context: CDL => JSON
        public string underscore_js { private set; get; }
        public string grammar_ast_js { private set; get; }

        public IJavaScriptHarness JavaScriptHarness = new NoesisJsHarness();

        public void BuildParsingContext()
        {
            JavaScriptHarness.Construct(underscore_js, grammar_ast_js);
        }

        public void DisposeParsingContext()
        {
            JavaScriptHarness.Destruct();
        }
        
        //private static readonly IRmsLogger Logger;

        #endregion

        #region Fields

        public ConcurrentDictionary<long, ContractExposureData> ContractExposureDataIndex { private set; get; }

        private Object ContractExposureDataIndexLock = new Object();

        private ConcurrentDictionary<long, bool> CacheIndependentContractIDs;

        private List<string[]> COLsHierarchy;

        #endregion

        #region Ground Up Damage Ratios

        // map: COL -> [0] -> ExposureId -> (timeStamp, (GroundUpLoss per building))
        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> 
            DamageRatiosPerSubPeril;

        public bool simulate = false;
        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> SimulateDamageRatios(int seed = 17317)
        {
            ClearGULosses();

            // map: COL -> [0] -> ExposureId -> (timeStamp, (GroundUpLoss per building))
            var dr = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();

            #region Simulation
            var rand = new Random(seed);
            var cols = GetCOLs(); int m = cols.Length - 1;

            Dictionary<long, int> riteIds = GetRiteIdsToNumBuildings();
            foreach (var kv in riteIds.Where(elem => elem.Value > 0))
            {
                foreach (string col in cols)
                {
                    //string col = cols[rand.Next(0, m)];
                    uint numDays = (uint)rand.Next(0, 365);

                    var drs = Enumerable.Range(1, kv.Value).Select(elem => (float)rand.NextDouble()).ToList();

                    if (!dr.ContainsKey(col))
                        dr.Add(col, new Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>());

                    if (!dr[col].ContainsKey(0))
                        dr[col].Add(0, new Dictionary<long, Tuple<double, uint, List<float>>>());

                    dr[col][0].Add(kv.Key, new Tuple<double, uint, List<float>>(1.0, numDays, drs));
                }
            }
            simulate = true;
            #endregion

            #region Check
            //if(null != dr)
            //{
            //    var hs = new HashSet<long>();
            //    foreach (string sCOL in dr.Keys)
            //    {
            //        var sample = dr[sCOL].First().Value;
            //        foreach (long id in sample.Keys)
            //        {
            //            hs.Add(id);
            //        }
            //        int n = hs.Count();
            //    }
            //}
            #endregion

            DamageRatiosPerSubPeril = dr;

            return dr;
        }

        private Dictionary<long,int> GetRiteIdsToNumBuildings(params long[] conExpIds)
        {
            if (null != ContractExposureDataIndex && ContractExposureDataIndex.Count() > 0)
            {
                var temp = (conExpIds.Length != 0) ? ContractExposureDataIndex.Where(kvp => conExpIds.Contains(kvp.Key)) : ContractExposureDataIndex;
                if (null != temp && temp.Count() > 0)
                {
                    return temp.Select(kvp => kvp.Value.ExposureIDAttributeMap).Where(elem => (null != elem) && elem.Count() > 0)
                        .Aggregate(new Dictionary<long, int>(), (a, b) =>
                        {
                            foreach (var kv in b)
                                if (!a.ContainsKey(kv.Key) && null != kv.Value)
                                    a.Add(kv.Key, kv.Value.NumBuildings);
                            return a; 
                        });
                }
            }
            return new Dictionary<long, int>();
        }
        
        private string[] GetCOLs()
        {
            return ContractExposureDataIndex.Select(elem => elem.Value.COLs).Where(cols => null != cols)
                .Aggregate(new HashSet<string>(), (a, b) => { a.UnionWith(b.Keys); return a; }).ToArray();
        }
        
        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> ReadDamageRatiosFromFile(string filePath)
        {
            // map: COL -> [0] -> ExposureId -> (timeStamp, (GroundUpLoss per building))
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> dr = null;

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Extension.ToLower().Equals(".txt") || fi.Extension.ToLower().Equals(".csv"))
                    dr = Utilities.ReadDamageRatiosFromCSVFile(filePath);
                else if (fi.Extension.ToLower().Equals(".dat"))
                    dr = Utilities.ReadDamageRatiosFromDATFile(filePath);
                else if (fi.Extension.ToLower().Equals(".dat1"))
                    dr = Utilities.ReadUncertOutput(filePath);
            }
            simulate = false;

            DamageRatiosPerSubPeril = dr;
            return dr;
        }

        public void TransformDamageRatios(
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> lossesPerSubPeril,
            bool isSimulated = false)
        {
            foreach (var ced in ContractExposureDataIndex.Values)
            {
                if (ced is PrimaryContractExposureData)
                    ced.TransformDamageRatios(lossesPerSubPeril, isSimulated);
                ced.InterruptOrResetExecutionState();
            }
        }

        #endregion

        #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION

        bool UseReference;
        ReferencePrototype _ReferencePrototype;

        #endregion

        #region Parse & Compile API

        // This function is used only for unit tests in project: "RMS.Prototype.NGFMUnitTest"
        public string ParseCDLUsingJISONJS_GetIR(string strCDL)
        {
            string IR; Dictionary<string, object> JISONJsonParseResult;
            JISONJsonParseResult = ParseCDLUsingJISONJS(strCDL);
            IR = JsonConvert.SerializeObject(JISONJsonParseResult).PrettyPrintJson();
            return IR;
        }

        public Dictionary<string, object> ParseCDLUsingJISONJS(string strCDL)
        {
            // prepare cdl (change newlines to spaces)
            strCDL = strCDL.Replace(System.Environment.NewLine, "     ");
            return JavaScriptHarness.Parse(strCDL);
        }

        #endregion

        #region Common APIs

        private void ClearContractExposureDataIndex()
        {
            if (ContractExposureDataIndex != null)
                ContractExposureDataIndex.Clear();
        }

        private void ClearGULosses()
        {
            if (null != DamageRatiosPerSubPeril && DamageRatiosPerSubPeril.Count() > 0)
                DamageRatiosPerSubPeril.Clear();
        }

        public void ClearContractExposureDataIndexAndGULosses()
        {
            ClearContractExposureDataIndex();
            ClearGULosses();
        }

        private HashSet<long> GetChildContractIDs(ContractExposureData root)
        {
            var ids = new HashSet<long>();

            if (root != null)
            {
                var q = new Queue<ContractExposureData>();
                q.Enqueue(root);

                while (q.Count() > 0)
                {
                    var node = q.Dequeue();
                    ids.Add(node.Id);

                    if (node is TreatyContractExposureData)
                    {
                        var hs = ((TreatyContractExposureData)node).ChildContractExposureIDs;
                        if (null != hs)
                            foreach (long id in hs)
                                q.Enqueue(GetContractData(id));
                    }
                }
            }

            return ids;
        }

        public ContractExposureData GetContractData(long id)
        {
            ContractExposureData data = null;
            ContractExposureDataIndex.TryGetValue(id, out data);
            return data;
        }

        public void InterruptOrResetExecutionStates(params long[] ids)
        {
            if (ids.Count() == 0)   // reset all if none specified
            {
                foreach (KeyValuePair<long, ContractExposureData> kvp in ContractExposureDataIndex)
                {
                    kvp.Value.InterruptOrResetExecutionState();
                }
            }
            else
            {
                foreach (long id in ids)
                {
                    ContractExposureData ced;
                    if (ContractExposureDataIndex.TryGetValue(id, out ced))
                        ced.InterruptOrResetExecutionState();
                }
            }
        }

        #endregion

        #region Constructors & Destructor

        //static HDFM()
        //{
        //    Logger = RmsLoggerFactory.GetLogger(typeof(NGFMPrototype));
        //}

        private void Configure()
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(directoryName);

            using (AppConfig.Change(directoryName + "\\App.config"))
            {
                underscore_js = ConfigurationManager.AppSettings["underscore_js"];
                if (File.Exists(underscore_js))
                {
                    FileInfo fi = new FileInfo(underscore_js);
                    underscore_js = fi.FullName;
                }
                else
                    throw new Exception(underscore_js + " : File Not Found!");

                grammar_ast_js = ConfigurationManager.AppSettings["cdl2js_ir_script"];
                if (File.Exists(grammar_ast_js))
                {
                    FileInfo fi = new FileInfo(grammar_ast_js);
                    grammar_ast_js = fi.FullName;
                }
                else
                    throw new Exception(grammar_ast_js + " : File Not Found!");
            }
        }

        private void SetCOLsHierarchy()
        {
            COLsHierarchy = new List<string[]>();

            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(directoryName);

            using (AppConfig.Change(directoryName + "\\App.config"))
            {
                string[] arr = ConfigurationManager.AppSettings["CauseOfLossHierarchy"]
                .Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (null != arr && arr.Length > 0)
                {
                    foreach (string str in arr)
                    {
                        string[] elem = str.Split(new char[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
                        if (null != elem && elem.Length > 0)
                            COLsHierarchy.Add(elem);
                    }
                }
            }
        }

        private void Initialize(bool append = false)
        {
            if (null == ContractExposureDataIndex)
                ContractExposureDataIndex = new ConcurrentDictionary<long, ContractExposureData>();
            else if (!append) ContractExposureDataIndex.Clear();

            if (null == CacheIndependentContractIDs)
                CacheIndependentContractIDs = new ConcurrentDictionary<long, bool>();
            else if (!append) CacheIndependentContractIDs.Clear();
        }

        public void Clear()
        {
            ContractExposureDataIndex.Clear();
            CacheIndependentContractIDs.Clear();
        }

        public HDFM(PartitionData PD) : this()
        {
            Prepare(PD);
        }

        public HDFM()
        {
            Configure();
            SetCOLsHierarchy();
            Initialize();
        }

        #endregion

        #region Preparation (Initializing Local Caches & Building Contract Graphs)

        public void Append(PartitionData partitionData, bool OnlyBuildCOM = false)
        {
            Prepare(partitionData, true, OnlyBuildCOM);
        }

        public void Prepare(PartitionData partitionData, bool append = false, bool OnlyBuildCOM = false)
        {
            JavaScriptHarness.Construct(underscore_js, grammar_ast_js);

            Initialize(partitionData, append);

            Prepare(OnlyBuildCOM);

            JavaScriptHarness.Destruct();
        }

        private void Prepare(bool OnlyBuildCOM = false)
        {
            UpdateIndependentContractIDs();

            ParallelOptions MaxGraphBuildingConcurrency
                = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) };

            Parallel.ForEach(CacheIndependentContractIDs.Keys, MaxGraphBuildingConcurrency, IndependentContractExposureID =>
            {
                var IndependentContractExposure = GetContractData(IndependentContractExposureID);

                BuildContractGraphRecursor((ContractExposureData)IndependentContractExposure);
            }
            );

            //foreach (long IndependentContractExposureID in CacheIndependentContractIDs.Keys)
            //{
            //    var IndependentContractExposure = GetContractData(IndependentContractExposureID);

            //    BuildContractGraphRecursor((ContractExposureData)IndependentContractExposure);
            //}
        }

        private void Initialize(PartitionData partitionData, bool append = false)
        {
            Initialize(append);

            #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION

            _ReferencePrototype = null;
            UseReference = Convert.ToBoolean(ConfigurationManager.AppSettings["UseReference"]);
            if (UseReference)
            {
                //Sunny hack... need to change to read from Model Setings Provider from the DLM..
                RAPSettings RapSettings = new RAPSettings(new HashSet<string> { "WS", "WI", "WA" });
                SubSamplingAnalysisSetting subSamplingSetiings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

                if (RapSettings != null || subSamplingSetiings != null)
                    _ReferencePrototype = new ReferencePrototype(partitionData, RapSettings, subSamplingSetiings);
                else
                    throw new InvalidOperationException("Cannot run in Array Mode and have null RapSettings or null SubSampling Settings!");
            }

            #endregion

            if (null != partitionData && null != partitionData.Exposures)
            {
                foreach (ContractExposure ce in partitionData.Exposures)
                {
                    if ((ce.ContractType != null) && ce.ContractType.IsReinsuranceContract())
                    { //Treaty Contract
                        ContractExposureDataIndex.TryAdd(ce.ExposureID, new TreatyContractExposureData(JavaScriptHarness, false, ce));
                    }
                    else if (null != ce.ContractSubjectExposures && null != ce.Subschedules)
                    { // Primary Contract
                        ContractExposureDataIndex.TryAdd(ce.ExposureID, new PrimaryContractExposureData(JavaScriptHarness, false, ce, "", _ReferencePrototype));
                    }
                }
            }
        }

        private HashSet<long> GetDependentContractIDs(params long[] contractIDs)
        {
            if (contractIDs.Length == 0)
                return ContractExposureDataIndex.Where(kv => null != kv.Value && kv.Value is TreatyContractExposureData)
                .Select(kv => ((TreatyContractExposureData)kv.Value).GetConExpIds())
                .Aggregate(new HashSet<long>(), (a, b) => { if (null != b) a.UnionWith(b); return a; });
            else 
                return ContractExposureDataIndex.Where(kv => null != kv.Value && contractIDs.Contains(kv.Key) && kv.Value is TreatyContractExposureData)
                .Select(kv => ((TreatyContractExposureData)kv.Value).GetConExpIds())
                .Aggregate(new HashSet<long>(), (a, b) => { if (null != b) a.UnionWith(b); return a; });
        }

        private void UpdateIndependentContractIDs()
        {
            var allIDs = new HashSet<long>(ContractExposureDataIndex.Keys.ToArray());
            var depIDs = GetDependentContractIDs();
            var indepIDs = new HashSet<long>(allIDs.Where(id => !depIDs.Contains(id)).ToArray());

            foreach (long id in indepIDs)
                if (!CacheIndependentContractIDs.ContainsKey(id))
                    CacheIndependentContractIDs.TryAdd(id, false);
        }

        private void BuildContractGraphRecursor(ContractExposureData ContractExposureNode, bool OnlyBuildCOM = false)
        {
            if (null != ContractExposureNode)
            {
                #region Recursion
                if (ContractExposureNode is TreatyContractExposureData)
                {
                    var Children = ((TreatyContractExposureData)ContractExposureNode).ChildContractExposureIDs;
                    foreach (var Child in Children)
                    {
                        var ChildContractData = GetContractData(Child);
                        BuildContractGraphRecursor((ContractExposureData)ChildContractData);
                    }
                }
                #endregion

                #region Current Node
                BuildContractGraphCanonicalOrder(ContractExposureNode, OnlyBuildCOM);
                #endregion
            }
        }

        private void BuildContractGraphCanonicalOrder(ContractExposureData ContractExposureNode, bool OnlyBuildCOM = false)
        {
            ContractExposureNode.ClearBugLog(DateTime.Now.ToString());

            #region *** Parsing CDL sring
            if (ContractExposureNode.state == ProcessState.None || ContractExposureNode.state == ProcessState.CDLParseFailed)
            {
                ContractExposureNode.ParseCDLUsingJISONJS();
            }
            #endregion

            #region *** Extracting Exposure Attributes ***
            if (!ContractExposureNode.DoneExtracting())
                ContractExposureNode.ExtractExposureAndScheduleInfo(ContractExposureDataIndex);
            #endregion

            #region *** Building Contract Graph ***
            if (ContractExposureNode.state == ProcessState.CDLParsed || ContractExposureNode.state == ProcessState.ContractGraphFailed)
            {
                if (OnlyBuildCOM)
                    ContractExposureNode.BuildContractObjectModel();
                else
                    ContractExposureNode.BuildContractGraph(COLsHierarchy);
            }
            #endregion
        }

        #endregion

        #region FM Execution

        public Dictionary<long, ResultPosition> ProcessEventFile(
            int EventID,
            string filePath,
            bool ShouldAllocate,
            int MaxConcurrencyContracts,
            params long[] ContractIDs)
        {
            var DRs = ReadDamageRatiosFromFile(filePath);
            return ProcessEvent(EventID, DRs, ShouldAllocate, MaxConcurrencyContracts, ContractIDs);
        }

        public Dictionary<long, ResultPosition> ProcessEvent(
            int EventID,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            bool ShouldAllocate,
            int MaxConcurrencyContracts,
            params long[] ContractIDs)
        {
            ConcurrentDictionary<long, ResultPosition> ContractIdToResultMap = new ConcurrentDictionary<long, ResultPosition>();

            #region Recursion

            List<long> IndependentContractIDs = GetIndependentContractIDs(ContractIDs);
            //int LevelConcurrency = Environment.ProcessorCount - 1;
            //int BatchSize = 100;
            //int TotalSize = IndependentContractIDs.Count;
            //Parallel.For(0, ((TotalSize / BatchSize) + 1), new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrencyContracts }, i =>
            Parallel.ForEach(IndependentContractIDs, new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrencyContracts }, id =>//UNCOMMENT FOR CONTRACTS IN PARALLEL
            //foreach (long id in IndependentContractIDs) // UNCOMMENT FOR CONTRACTS IN SEQUENCE
            {
                //int BatchUpper = ((i + 1) * BatchSize);
                //for (int j = i * BatchSize; j < BatchUpper && j < TotalSize; j++)
                //{
                    //long id = IndependentContractIDs[j];

                var ced = GetContractData(id);
                    if (!(ced.state == ProcessState.ContractGraphBuilt || ced.state == ProcessState.FMExecuted || ced.state == ProcessState.FMFailed))
                    {
                        Console.WriteLine("Contract with ID : " + id + " is invalid and is not executable!");
                        //Logger.LogInfoFormatExt("Contract with ID : " + id + " is invalid and is not executable!");
                        return;
                    }
                if (ced is PrimaryContractExposureData)
                    ced = new PrimaryContractExposureData((PrimaryContractExposureData)ced);
                else
                    ced = new TreatyContractExposureData((TreatyContractExposureData)ced);
                ResultPosition Result = ProcessEventRecursor(EventID, ContractIdToResultMap, EventOccurrenceDRs, ced, ShouldAllocate);
                if (null != Result)
                        ContractIdToResultMap.TryAdd(id, Result);
                //}
            }
            );//UNCOMMENT FOR CONTRACTS IN PARALLEL

            return ContractIdToResultMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            #endregion
        }

        private ResultPosition ProcessEventRecursor(
            int EventID,
            ConcurrentDictionary<long, ResultPosition> ContractIdToResultMap,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            ContractExposureData node,
            bool ShouldAllocate)
        {
            if (node is TreatyContractExposureData)
            {
                #region Recursion

                var childrenIDs = ((TreatyContractExposureData)node).ChildContractExposureIDs;
                foreach (long id in childrenIDs)
                {
                    var ced = GetContractData(id);
                    if (ced is PrimaryContractExposureData)
                        ced = new PrimaryContractExposureData((PrimaryContractExposureData)ced);
                    else
                        ced = new TreatyContractExposureData((TreatyContractExposureData)ced);
                    ResultPosition Result = ProcessEventRecursor(EventID, ContractIdToResultMap, EventOccurrenceDRs, ced, true);
                    if (null != Result)
                        ContractIdToResultMap.TryAdd(id, Result);
                }

                #endregion
            }

            if ((node.state == ProcessState.ContractGraphBuilt || node.state == ProcessState.FMExecuted || node.state == ProcessState.FMFailed))
            {
                return node.ExecuteFM(EventOccurrenceDRs, ShouldAllocate, ContractIdToResultMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }

            return null;
        }

        public Dictionary<long, List<ResultPosition>> ProcessPeriod(
            int PeriodID,
            List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodOfEventOccurrenceDRs,
            bool ShouldAllocate,
            int MaxConcurrencyContracts,
            params long[] ContractIDs)
        {
            ConcurrentDictionary<long, List<ResultPosition>> ContractIdToResultMap = new ConcurrentDictionary<long, List<ResultPosition>>();

            #region Recursion

            List<long> IndependentContractIDs = GetIndependentContractIDs(ContractIDs);
            //int LevelConcurrency = Environment.ProcessorCount - 1;
            //int BatchSize = 100;
            //int TotalSize = IndependentContractIDs.Count;
            //Parallel.For(0, ((TotalSize / BatchSize) + 1), new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrencyContracts }, i =>
            Parallel.ForEach(IndependentContractIDs, new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrencyContracts }, id =>//UNCOMMENT FOR CONTRACTS IN PARALLEL
            //foreach (long id in IndependentContractIDs) // UNCOMMENT FOR CONTRACTS IN SEQUENCE
            {
                //int BatchUpper = ((i + 1) * BatchSize);
                //for (int j = i * BatchSize; j < BatchUpper && j < TotalSize; j++)
                //{
                //long id = IndependentContractIDs[j];

                var ced = GetContractData(id);
                if (!(ced.state == ProcessState.ContractGraphBuilt || ced.state == ProcessState.FMExecuted || ced.state == ProcessState.FMFailed))
                {
                    Console.WriteLine("Contract with ID : " + id + " is invalid and is not executable!");
                    //Logger.LogInfoFormatExt("Contract with ID : " + id + " is invalid and is not executable!");
                    return;
                }
                if (ced is PrimaryContractExposureData)
                    ced = new PrimaryContractExposureData((PrimaryContractExposureData)ced);
                else
                    ced = new TreatyContractExposureData((TreatyContractExposureData)ced);
                List<ResultPosition> Result = ProcessPeriodRecursor(PeriodID, ContractIdToResultMap, PeriodOfEventOccurrenceDRs, ced, ShouldAllocate);
                if (null != Result)
                    ContractIdToResultMap.TryAdd(id, Result);
                //}
            }
            );//UNCOMMENT FOR CONTRACTS IN PARALLEL

            return ContractIdToResultMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            #endregion

        }

        private List<ResultPosition> ProcessPeriodRecursor(
            int PeriodID,
            ConcurrentDictionary<long, List<ResultPosition>> ContractIdToResultMap,
            List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodOfEventOccurrenceDRs,
            ContractExposureData node,
            bool ShouldAllocate)
        {
            if (node is TreatyContractExposureData)
            {
                #region Recursion

                var childrenIDs = ((TreatyContractExposureData)node).ChildContractExposureIDs;
                foreach (long id in childrenIDs)
                {
                    var ced = GetContractData(id);
                    if (ced is PrimaryContractExposureData)
                        ced = new PrimaryContractExposureData((PrimaryContractExposureData)ced);
                    else
                        ced = new TreatyContractExposureData((TreatyContractExposureData)ced);
                    List<ResultPosition> Result = ProcessPeriodRecursor(PeriodID, ContractIdToResultMap, PeriodOfEventOccurrenceDRs, ced, true);
                    if (null != Result)
                        ContractIdToResultMap.TryAdd(id, Result);
                }

                #endregion
            }

            if ((node.state == ProcessState.ContractGraphBuilt || node.state == ProcessState.FMExecuted || node.state == ProcessState.FMFailed))
            {
                return node.ExecuteFM(PeriodOfEventOccurrenceDRs, ShouldAllocate, ContractIdToResultMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            }

            return null;
        }

        private List<long> GetIndependentContractIDs(params long[] contractIDs)
        {
            if (contractIDs.Length == 0)
                return CacheIndependentContractIDs.Keys.ToList();
            else
            {
                var depIDs = GetDependentContractIDs(contractIDs);
                return contractIDs.Where(id => !depIDs.Contains(id)).ToList();
            }
        }

        #endregion

        #region Exposed Limit API

        // returns 0 if the contract doesn't exist
        public double GetContractExposureAmount(long conExpId)
        {
            Dictionary<long, double> ContractExposureAmount = GetContractExposureAmount();
            if (ContractExposureAmount.ContainsKey(conExpId))
                return ContractExposureAmount[conExpId];
            else 
                return 0.0;
        }

        public Dictionary<long, double> GetContractExposureAmount()
        {
            return ContractExposureDataIndex
                .Where(kv => (null != kv.Value && null != kv.Value.ExposureIDAttributeMap && kv.Value.ExposureIDAttributeMap.Count() > 0))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ExposureIDAttributeMap.Select(kv2 => kv2.Value.Value).Sum());
        }

        #endregion
    }
}