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
    public class NGFMPrototype : IDisposable
    {
        #region Auxiliary fields
        
        // Javascript parsing context: CDL => JSON
        public string underscore_js { private set; get; }
        public string grammar_ast_js { private set; get; }

        private IJavaScriptHarness ParsingHarness = new NoesisJsHarness();

        public void BuildParsingContext()
        {
            ParsingHarness.Construct(underscore_js, grammar_ast_js);
        }

        public void DisposeParsingContext()
        {
            ParsingHarness.Destruct();
        }
        
        private static readonly IRmsLogger Logger;

        private int maxDegreeOfParallelism = TaskManager.NumberThreads;

        #endregion

        #region Fields

        public ConcurrentDictionary<long, ContractExposureData> CacheContractData { private set; get; }
        private Object CacheContractDataLock = new Object();

        private ConcurrentDictionary<long, bool> CacheIndependentContractIDs;

        public ConcurrentDictionary<int, ConcurrentDictionary<long, ResultPosition>>
            CacheEventIdContractIdToResult { private set; get; }
        
        private List<string[]> COLsHierarchy;

        public void Remove(params long[] ids)
        {
            #region Remove from Cache
            foreach (long id in ids)
            {
                ContractExposureData rmv;
                CacheContractData.TryRemove(id, out rmv);
                rmv = null;
            }
            #endregion

            TaskManager.CancelAll(ids);

            #region Remove from Positions
            foreach (var ce in CacheContractData.Values)
            {
                bool flag = (ce is TreatyContractExposureData);
                if (flag)
                {
                   ((TreatyContractExposureData)ce).RemoveFromPositions(ids);
                }
            }
            #endregion

            #region Remove from Ground Up Losses
            if (null != DamageRatiosPerSubPeril && DamageRatiosPerSubPeril.Count() > 0)
            {
                if (CacheContractData.Count() == 0)
                    DamageRatiosPerSubPeril.Clear();
                else
                {
                    var riteIds = GetRiteIds();
                    foreach (var v1 in DamageRatiosPerSubPeril.Values)
                    {
                        if (null != v1)
                        {
                            foreach (var v2 in v1.Values)
                            {
                                if (null != v2)
                                {
                                    var rem = new HashSet<long>();
                                    foreach (long id in v2.Keys)
                                        if (!riteIds.Contains(id))
                                            rem.Add(id);

                                    foreach (long id in rem)
                                        v2.Remove(id);

                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Remove from Results
            if (ids.Length > 0 && CacheEventIdContractIdToResult != null)
            {
                foreach (var res in CacheEventIdContractIdToResult.Values)
                {
                    foreach (long id in ids)
                    {
                        ResultPosition rm;
                        res.TryRemove(id, out rm);
                    }
                }
            }
            #endregion
        }
        private void ClearCache()
        {
            if(CacheContractData != null)
                CacheContractData.Clear();

            if (CacheEventIdContractIdToResult != null)
                CacheEventIdContractIdToResult.Clear();
        }
        private void ClearGULosses()
        {
            if (null != DamageRatiosPerSubPeril && DamageRatiosPerSubPeril.Count() > 0)
                DamageRatiosPerSubPeril.Clear();

            if (CacheEventIdContractIdToResult != null)
                CacheEventIdContractIdToResult.Clear();
        }
        public void ClearCacheAndGULosses()
        {
            ClearCache();
            ClearGULosses();
        }
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

            Dictionary<long, int> RITEIdsToNumBuildings = GetRiteIdsToNumBuildings();
            foreach (var kv in RITEIdsToNumBuildings.Where(elem => elem.Value > 0))
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
            if (null != CacheContractData && CacheContractData.Count() > 0)
            {
                var temp = (conExpIds.Length != 0) ? CacheContractData.Where(kvp => conExpIds.Contains(kvp.Key)) : CacheContractData;
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
            return CacheContractData.Select(elem => elem.Value.COLs).Where(cols => null != cols)
                .Aggregate(new HashSet<string>(), (a, b) => { a.UnionWith(b.Keys); return a; }).ToArray();
        }
        /// <summary>
        /// Get union of Rite IDs from cache of Contyract Exposures
        /// </summary>
        /// <param name="conExpIds">Contract Exposure Ids. If null - return all Rite Ids</param>
        /// <returns></returns>        
        private HashSet<long> GetRiteIds(params long[] conExpIds)
        {
            if (null != CacheContractData && CacheContractData.Count() > 0)
            {
                var temp = (conExpIds.Length != 0) ? CacheContractData.Where(kvp => conExpIds.Contains(kvp.Key)) : CacheContractData;
                if (null != temp && temp.Count() > 0)
                    return temp.Select(kvp => kvp.Value.GetRiteIds()).Where(elem => (null != elem))
                        .Aggregate(new HashSet<long>(), (a, b) => { a.UnionWith(b); return a; });
            }
            return new HashSet<long>();
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
        public void WriteDamageRatiosToFile(string filePath)
        {
            Utilities.WriteDamageRatiosToFile(filePath, DamageRatiosPerSubPeril);
        }
        private void WriteDamageRatiosToFile(string filePath, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> damageRatiosPerSubPeril)
        {
            if (null != damageRatiosPerSubPeril && !string.IsNullOrEmpty(filePath) && Utilities.CreateDirectoryRecursively(Path.GetDirectoryName(filePath)))
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Extension.ToLower().Equals(".txt") || fi.Extension.ToLower().Equals(".csv"))
                    Utilities.WriteDamageRatiosToCSVFile(filePath, damageRatiosPerSubPeril);
                else if (fi.Extension.ToLower().Equals(".dat"))
                    Utilities.WriteDamageRatiosToDATFile(filePath, damageRatiosPerSubPeril);
                else
                    Utilities.WriteDamageRatiosToDATFile(filePath, damageRatiosPerSubPeril);
            }
        }
        public void TransformDamageRatios(
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> lossesPerSubPeril,
            bool isSimulated = false)
        {
            foreach (var ced in CacheContractData.Values)
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

        #region Constructor and Initialization and Destructor

        static NGFMPrototype()
        {
            Logger = RmsLoggerFactory.GetLogger(typeof(NGFMPrototype));
        }

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

        public void Initialize(int maxDegreeOfParallelism = -1)
        {
            if (null == CacheContractData)
                CacheContractData = new ConcurrentDictionary<long, ContractExposureData>();
            else CacheContractData.Clear();

            if (null == CacheEventIdContractIdToResult)
                CacheEventIdContractIdToResult = new ConcurrentDictionary<int, ConcurrentDictionary<long, ResultPosition>>();
            else CacheEventIdContractIdToResult.Clear();

            if (null == CacheIndependentContractIDs)
                CacheIndependentContractIDs = new ConcurrentDictionary<long, bool>();
            else CacheIndependentContractIDs.Clear();

            int numCores = (maxDegreeOfParallelism > 0) ? maxDegreeOfParallelism : Environment.ProcessorCount;

            TaskManager.Initialize(numCores);
        }

        bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                TaskManager.Dispose();
            }

            disposed = true;
        }

        ~NGFMPrototype()
        {
            Dispose(false);
        }

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
            ParsingHarness.Construct(underscore_js, grammar_ast_js);
            // prepare cdl (change newlines to spaces)
            strCDL = strCDL.Replace(System.Environment.NewLine, "     ");
            Dictionary<string, object> JSON_IR = ParsingHarness.Parse(strCDL);
            ParsingHarness.Destruct();
            return JSON_IR;
        }

        #endregion

        #region Common APIs

        /// <summary>
        /// Upload Contract Exposures from files
        /// </summary>
        /// <param name="filePaths"></param>
        /// <param name="append"></param>
        /// <param name="cts">Cancellation Token</param>
        /// <returns>array of new Primary Contract IDs</returns>
        public List<long[]> UploadContractExposures(string[] filePaths, bool append = false, CancellationTokenSource cts = null)
        {
            if (!append)
            {
                Initialize();
            }
            var bagPrimary = new ConcurrentBag<long>();
            var bagTreaty = new ConcurrentBag<long>();
            
            if (cts == null)
                cts = new CancellationTokenSource();

            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount - 1, CancellationToken = cts.Token };
            try
            {
                Parallel.ForEach(Utilities.GetExtantFiles(filePaths), options, file =>
                {
                    try
                    {
                        options.CancellationToken.ThrowIfCancellationRequested();

                        if (file.EndsWith(".dat"))
                        {
                            PartitionData pData = Utilities.DeserializePartitionData(file);

                            #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION
                            _ReferencePrototype = null;
                            UseReference = Convert.ToBoolean(ConfigurationManager.AppSettings["UseReference"]);
                            if (UseReference)
                            {
                                //Sunny hack... need to change to read from Model Setings Provider from the DLM..
                                RAPSettings RapSettings = new RAPSettings(new HashSet<string> { "WS", "WI", "WA" });
                                SubSamplingAnalysisSetting subSamplingSetiings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

                                if (RapSettings != null || subSamplingSetiings != null)
                                    _ReferencePrototype = new ReferencePrototype(pData, RapSettings, subSamplingSetiings);
                                else
                                    throw new InvalidOperationException("Cannot run in Array Mode and have null RapSettings or null SubSampling Settings!");
                            }
                             
                            #endregion

                            foreach (ContractExposure ce in pData.Exposures)
                            {
                                if (null != ce && null != ce.Contract && !string.IsNullOrEmpty(ce.Contract.CDLString)
                                       && !CacheContractData.ContainsKey(ce.ExposureID))
                                {
                                    if ((ce.ContractType != null) && ce.ContractType.IsReinsuranceContract())
                                    { //Treaty Contract
                                        if (CacheContractData.TryAdd(ce.ExposureID, new TreatyContractExposureData(ParsingHarness, true, ce, file)))
                                            bagTreaty.Add(ce.ExposureID);
                                    }
                                    else if (null != ce.ContractSubjectExposures && null != ce.Subschedules)
                                    { // Primary Contract
                                        if (CacheContractData.TryAdd(ce.ExposureID, new PrimaryContractExposureData(ParsingHarness, true, ce, file, _ReferencePrototype)))
                                            bagPrimary.Add(ce.ExposureID);
                                    }
                                }
                            }
                        }
                        else if (file.EndsWith(".dat2"))
                        {
                        }
                    }
                    catch (Exception ex1)
                    {
                        Console.WriteLine("Error: {0}", ex1.Message);
                    }
                });
            }
            catch (OperationCanceledException ex2)
            {
                Console.WriteLine("Error: {0}", ex2.Message);
            }

            var ids = new List<long[]>();
            ids.Add(bagPrimary.ToArray());
            ids.Add(bagTreaty.ToArray());

            return ids;
        }

        public void SaveContractsToFile(string filePath, params long[] ids)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                if (filePath.EndsWith(".dat"))
                {
                    PartitionData pd = new PartitionData();
                    pd.Products = new Product[0];

                    if (ids.Length == 0)//save all
                    {
                        pd.Exposures = CacheContractData.Values.Where(elem => elem != null).Select(elem => elem.ConExp).ToArray();
                    }
                    else //if (ids.Length > 0)
                    {
                        pd.Exposures =
                            ids.Aggregate(new HashSet<long>(), (a, id) =>
                            {
                                a.UnionWith(GetChildContractIDs(GetContractData(id)));
                                return a;
                            })
                            .Select(id => GetContractData(id)).Where(elem => null != elem).Select(elem => elem.ConExp).ToArray();
                    }

                    Utilities.Serialize(pd, filePath);
                }
                else if (filePath.EndsWith(".dat2"))
                {
                    using (var wc = new WebClient().OpenWrite(filePath))
                    {
                        try
                        {
                            ProtoBuf.Serializer.Serialize<ContractExposureData[]>(wc, CacheContractData.Values.ToArray());
                        }
                        catch (ProtoBuf.ProtoException p)
                        {
                            //Log.Fatal("Error Writing of protobuf to file" + p.Message);
                            Console.WriteLine("Error Writing of protobuf to file \"{0}\": {1}", filePath, p.Message);
                        }
                    }
                }
            }
        }

        public HashSet<string> GetContractFileNames()
        {
            return new HashSet<string>(CacheContractData.Where(kvp => null != kvp.Value && !string.IsNullOrEmpty(kvp.Value.filePath))
                .Select(kvp => kvp.Value.filePath).ToArray());
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
            CacheContractData.TryGetValue(id, out data);
            return data;
        }

        public void InterruptOrResetExecutionStates(params long[] ids)
        {
            if (ids.Count() == 0)   // reset all if none specified
            {
                foreach (KeyValuePair<long, ContractExposureData> kvp in CacheContractData)
                {
                    kvp.Value.InterruptOrResetExecutionState();
                }
            }
            else
            {
                foreach (long id in ids)
                {
                    ContractExposureData ced;
                    if (CacheContractData.TryGetValue(id, out ced))
                        ced.InterruptOrResetExecutionState();
                }
            }
        }

        public long CopyContractData(long id)
        {
            ContractExposureData cedCopy = null;
            var ced = GetContractData(id);
            if (null != ced)
            {
                long idCopy = 0;

                if (ced is TreatyContractExposureData)
                {
                    cedCopy = new TreatyContractExposureData(((TreatyContractExposureData)ced), true);
                    if (cedCopy != null)
                    {
                        idCopy = -1;
                        while (CacheContractData.ContainsKey(idCopy))
                            idCopy--;
                    }
                }
                else
                {
                    cedCopy = new PrimaryContractExposureData(((PrimaryContractExposureData)ced), true);
                    if (cedCopy != null)
                    {
                        idCopy = 1;
                        while (CacheContractData.ContainsKey(idCopy))
                            idCopy++;
                    }
                }

                if (idCopy != 0)
                {
                    cedCopy.SetID(idCopy);
                    CacheContractData.TryAdd(idCopy, cedCopy); 
                }
            }
            return (null != cedCopy) ? cedCopy.Id : 0;
        }

        public bool AddNewContractData(long id, string cdl)
        {
            ContractExposureData ced = null;
            if (id != 0 && null == GetContractData(id))
            {
                if (id < 0)//TreatyContractExposureData
                {
                    ced = new TreatyContractExposureData(id, cdl);
                }
                else if(id > 0)
                {
                    ced = new PrimaryContractExposureData(id, cdl);
                }

                if(ced != null)
                    CacheContractData.TryAdd(id, ced);
            }
            return (null != ced);
        }

        public void RemovePayOutAttributes(int eventID, long id)
        {
            if (CacheEventIdContractIdToResult.ContainsKey(eventID))
                if (CacheEventIdContractIdToResult[eventID].ContainsKey(id))
                {
                    ResultPosition rm;
                    CacheEventIdContractIdToResult[eventID].TryRemove(id, out rm);
                }
        }

        private void RemoveEvent(int eventID)
        {
            ConcurrentDictionary<long, ResultPosition> rm;
            if (CacheEventIdContractIdToResult.ContainsKey(eventID))
                CacheEventIdContractIdToResult.TryRemove(eventID, out rm);
        }

        private IEnumerable<ContractExposureData> GetContracts(params long[] ids)
        {
            return (ids.Length != 0) ? CacheContractData.Values.Where(elem => ids.Contains(elem.Id)) : CacheContractData.Values;
        }

        #endregion

        #region API for executing from NGDLM (e.g. for usage with NGDLM integration)

        #region Constructors

        public NGFMPrototype(PartitionData PD) : this()
        {
            Prepare_OLDAPI(PD);
        }

        public NGFMPrototype(int maxDegreeOfParallelism = -1)
        {
            var sw = Stopwatch.StartNew();
            Configure();
            SetCOLsHierarchy();
            Initialize(maxDegreeOfParallelism);
            sw.Stop();
            Logger.LogInfoFormatExt("NGFMPrototype construction took " + sw.PrettyPrintHighRes() + "!");
        }
        
        #endregion

        #region Preparation (Initializing Local Caches & Building Contract Graphs)

        public void Prepare()
        {
            UpdateIndependentContractIDs();

            //ParallelOptions MaxGraphBuildingConcurrency 
            //    = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) };

            //Parallel.ForEach(CacheIndependentContractIDs.Keys, MaxGraphBuildingConcurrency, IndependentContractExposureID =>
            //{
            //    var IndependentContractExposure = GetContractData(IndependentContractExposureID);

            //    BuildContractGraphRecursor((ContractExposureData)IndependentContractExposure);
            //}
            //);

            foreach (long IndependentContractExposureID in CacheIndependentContractIDs.Keys)
            {
                var IndependentContractExposure = GetContractData(IndependentContractExposureID);

                BuildContractGraphRecursor((ContractExposureData)IndependentContractExposure);
            }
        }

        public Dictionary<long, Task> Prepare_OLDAPI()
        {
            UpdateIndependentContractIDs();

            Action<object> action = (obj) =>
                {
                    BuildContractGraphRecursor_OLDAPI((ContractExposureData)obj);
                };


            var tasks = new Dictionary<long, Task>();

            foreach (long id in CacheIndependentContractIDs.Keys)
            {
                var ced = GetContractData(id);

                var task = TaskManager.Start(id, 0, action, ced);

                tasks.Add(id, task);
            }

            return tasks;
        }

        public void Prepare(PartitionData partitionData, int maxDegreeOfParallelism = -1)
        {
            ParsingHarness.Construct(underscore_js, grammar_ast_js);

            Initialize(partitionData, maxDegreeOfParallelism);

            Prepare();

            ParsingHarness.Destruct();
        }

        public void Prepare_OLDAPI(PartitionData partitionData, int maxDegreeOfParallelism = -1)
        {
            ParsingHarness.Construct();

            Initialize(partitionData, maxDegreeOfParallelism);

            var tasks = Prepare_OLDAPI();

            Task.WaitAll(tasks.Values.ToArray());

            ParsingHarness.Destruct();
        }

        #region Preparation (Initializing Local Caches & Building Contract Graphs) Helpers

        private void Initialize(PartitionData partitionData, int maxDegreeOfParallelism = -1)
        {
            Initialize(maxDegreeOfParallelism);

            #region VECTORIZED REFERENCE PROTOTYPE INTEGRATION

            _ReferencePrototype = null;
            UseReference = Convert.ToBoolean(ConfigurationManager.AppSettings["UseReference"]);
            if (UseReference)
            {
                //Sunny hack... need to change to read from Model Setings Provider from the DLM..
                RAPSettings RapSettings = new RAPSettings(new HashSet<string> {"WS", "WI", "WA"});
                SubSamplingAnalysisSetting subSamplingSetiings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

                if (RapSettings != null || subSamplingSetiings !=null)
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
                        CacheContractData.TryAdd(ce.ExposureID, new TreatyContractExposureData(ParsingHarness, false, ce));
                    }
                    else if (null != ce.ContractSubjectExposures && null != ce.Subschedules)
                    { // Primary Contract
                        CacheContractData.TryAdd(ce.ExposureID, new PrimaryContractExposureData(ParsingHarness, false, ce, "", _ReferencePrototype));
                    }
                }
            }
        }

        private HashSet<long> GetDependentContractIDs(params long[] contractIDs)
        {
            if (contractIDs.Length == 0)
                return CacheContractData.Where(kv => null != kv.Value && kv.Value is TreatyContractExposureData)
                .Select(kv => ((TreatyContractExposureData)kv.Value).GetConExpIds())
                .Aggregate(new HashSet<long>(), (a, b) => { if (null != b) a.UnionWith(b); return a; });
            else 
                return CacheContractData.Where(kv => null != kv.Value && contractIDs.Contains(kv.Key) && kv.Value is TreatyContractExposureData)
                .Select(kv => ((TreatyContractExposureData)kv.Value).GetConExpIds())
                .Aggregate(new HashSet<long>(), (a, b) => { if (null != b) a.UnionWith(b); return a; });
        }

        private void UpdateIndependentContractIDs()
        {
            var allIDs = new HashSet<long>(CacheContractData.Keys.ToArray());
            var depIDs = GetDependentContractIDs();
            var indepIDs = new HashSet<long>(allIDs.Where(id => !depIDs.Contains(id)).ToArray());

            foreach (long id in indepIDs)
                if (!CacheIndependentContractIDs.ContainsKey(id))
                    CacheIndependentContractIDs.TryAdd(id, false);
        }

        private void BuildContractGraphRecursor(ContractExposureData ContractExposureNode)
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
                BuildContractGraphCanonicalOrder(ContractExposureNode);
                #endregion
            }
        }

        private void BuildContractGraphCanonicalOrder(ContractExposureData ContractExposureNode)
        {
            ContractExposureNode.ClearBugLog(DateTime.Now.ToString());

            #region *** Parsing CDL string
            if (ContractExposureNode.state == ProcessState.None || ContractExposureNode.state == ProcessState.CDLParseFailed)
            {
                ContractExposureNode.ParseCDLUsingJISONJS();
            }
            #endregion

            if (UseReference && (ContractExposureNode is PrimaryContractExposureData))
            {
                ContractExposureNode.BuildContractGraph(COLsHierarchy);
            }

            else
            {
                #region *** Extracting RITE Attributes ***
                if (!ContractExposureNode.DoneExtracting())
                    ContractExposureNode.ExtractExposureAndScheduleInfo(CacheContractData);
                #endregion

                #region *** Building Contract Graph ***
                if (ContractExposureNode.state == ProcessState.CDLParsed || ContractExposureNode.state == ProcessState.ContractGraphFailed)
                {
                    ContractExposureNode.BuildContractGraph(COLsHierarchy);
                }
                #endregion
            }
        }
        
        private void BuildContractGraphRecursor_OLDAPI(ContractExposureData node)
        {
            if (null != node)
            {
                node.ParsingHarness = ParsingHarness;
                #region Recursion
                if (node is TreatyContractExposureData)
                {
                    Action<object> action = (obj) =>
                    {
                        BuildContractGraphRecursor_OLDAPI((ContractExposureData)obj);
                    };

                    var tasks = new List<Task>();
                    var childrenIDs = ((TreatyContractExposureData)node).ChildContractExposureIDs;
                    foreach (var id in childrenIDs)
                    {
                        var next = GetContractData(id);

                        var task = TaskManager.Start(id, 0, action, next);
                        tasks.Add(task);
                    }

                    Task.WaitAll(tasks.ToArray());
                }
                #endregion

                #region Current Node
                BuildContractGraphCanonicalOrder(node);
                #endregion
            }
        }

        #endregion

        #endregion

        #region FM Execution

        public Dictionary<long, ResultPosition> ProcessEvent(int EventID,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            bool ShouldAllocate,
            int MaxConcurrencyContracts,
            params long[] ContractIDs)
        {
            Dictionary<long, ResultPosition> ContractIdToResultMap = new Dictionary<long, ResultPosition>();

            #region Recursion

            List<long> IndependentContractIDs = GetIndependentContractIDs(ContractIDs);
            //int LevelConcurrency = Environment.ProcessorCount - 1;
            //int BatchSize = 100;
            //int TotalSize = IndependentContractIDs.Count;
            //Parallel.For(0, ((TotalSize / BatchSize) + 1), new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrencyContracts }, i =>
            //Parallel.ForEach(IndependentContractIDs, new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrencyContracts }, id =>//UNCOMMENT FOR CONTRACTS IN PARALLEL
            foreach (long id in IndependentContractIDs) // UNCOMMENT FOR CONTRACTS IN SEQUENCE
            {
                //int BatchUpper = ((i + 1) * BatchSize);
                //for (int j = i * BatchSize; j < BatchUpper && j < TotalSize; j++)
                //{
                //    long id = IndependentContractIDs[j];

                var ced = GetContractData(id);

                    if (!(ced.state == ProcessState.ContractGraphBuilt || ced.state == ProcessState.FMExecuted || ced.state == ProcessState.FMFailed))
                    {
                        Logger.LogInfoFormatExt("Contract with ID : " + id + " is invalid and is not executable!");
                        continue;
                    }

                if (ced is PrimaryContractExposureData)
                    ced = new PrimaryContractExposureData((PrimaryContractExposureData)ced);
                else
                    ced = new TreatyContractExposureData((TreatyContractExposureData)ced);
                ResultPosition Result = ProcessEventRecursor(EventID, ContractIdToResultMap, EventOccurrenceDRs, ced, ShouldAllocate);
                if (null != Result)
                        ContractIdToResultMap.Add(id, Result);
                //}
            }
            //);//UNCOMMENT FOR CONTRACTS IN PARALLEL

            return ContractIdToResultMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            #endregion
        }

        private ResultPosition ProcessEventRecursor(
            int EventID,
            Dictionary<long, ResultPosition> ContractIdToResultMap,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            ContractExposureData node,
            bool ShouldAllocate)
        {
            if (node is TreatyContractExposureData)
            {
                #region Recursion

                var childrenIDs = ((TreatyContractExposureData)node).ChildContractExposureIDs;
                //Dictionary<long, Task<ResultPosition>> tasks = new Dictionary<long, Task<ResultPosition>>();
                foreach (long id in childrenIDs)
                {
                    var ced = GetContractData(id);

                    if (!(ced.state == ProcessState.ContractGraphBuilt || ced.state == ProcessState.FMExecuted || ced.state == ProcessState.FMFailed))
                    {
                        Logger.LogInfoFormatExt("Contract with ID : " + id + " is invalid and is not executable!");
                        continue;
                    }

                    if (ced is PrimaryContractExposureData)
                        ced = new PrimaryContractExposureData((PrimaryContractExposureData)ced);
                    else
                        ced = new TreatyContractExposureData((TreatyContractExposureData)ced);
                    //Task<ResultPosition> cedTask = new Task<ResultPosition>(() =>
                    //    ProcessEventRecursor(eventID, EventOccurrenceDRs, ced));
                    //cedTask.Start();
                    //cedTask.Wait();
                    //tasks.Add(id, cedTask);
                    ResultPosition Result = ProcessEventRecursor(EventID, ContractIdToResultMap, EventOccurrenceDRs, ced, ShouldAllocate);
                    if (null != Result)
                        ContractIdToResultMap.Add(id, Result);
                }

                //foreach (long id in childrenIDs)
                //    if (null != tasks[id].Result)
                //        EventIdToContractIdToResultMap[eventID].TryAdd(id, tasks[id].Result);

                #endregion
            }

            if ((node.state == ProcessState.ContractGraphBuilt || node.state == ProcessState.FMExecuted || node.state == ProcessState.FMFailed))
            {
                return node.ExecuteFM(EventOccurrenceDRs, ShouldAllocate, ContractIdToResultMap);
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
                    Logger.LogInfoFormatExt("Contract with ID : " + id + " is invalid and is not executable!");
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

        /// <summary>
        /// new new API for executing from NGDLM
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="EventOccurrenceDRs">Event Data</param>
        /// <param name="contractIDs">Contract Exposure IDs. If empty - execute for all ContractExposures</param>
        public void ProcessEvent_OLDNEWNEWAPI(int eventId,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            params long[] ContractIDs)
        {
           Dictionary<long, Task> tasks;
           ProcessEvent_OLDAPI(eventId, EventOccurrenceDRs, out tasks, ContractIDs);
        }

        /// <summary>
        /// new new API for executing from UI
        /// </summary>
        /// <param name="eventId">Event ID</param>
        /// <param name="EventOccurrenceDRs">Event Data</param>
        /// <param name="tasks">return Tasks</param>
        /// <param name="ContractIDs">Contract Exposure IDs. If empty - execute for all ContractExposures</param>
        public void ProcessEvent_OLDAPI(int eventId,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            out Dictionary<long, Task> tasks,
            params long[] ContractIDs)
        {
             //Create empty object to caching results for given event
            //var cacheContractIdToResult = AddOrUpdateEvent(eventId);

            RemoveEvent(eventId);
            CacheEventIdContractIdToResult.TryAdd(eventId, new ConcurrentDictionary<long, ResultPosition>());

            Action<object> action = (obj) =>
            {
                ProcessEventRecursor_OLDAPI(eventId, EventOccurrenceDRs, (ContractExposureData)obj, false);
            };

            #region Recursion
            tasks = new Dictionary<long, Task>();
            foreach (long id in GetIndependentContractIDs(ContractIDs))
            {
                var ced = GetCopyOfContractData(id);

                var task = TaskManager.Start(id, eventId, action, ced);
                tasks.Add(id, task);
            }

            #endregion
        }

        public ContractExposureData GetCopyOfContractData(long id)
        {
            if (!CacheContractData.ContainsKey(id))
                return null;

            lock (CacheContractDataLock)
            {
                ContractExposureData original = CacheContractData[id];

                if (original is PrimaryContractExposureData)
                    return new PrimaryContractExposureData((PrimaryContractExposureData)original);
                else
                    return new TreatyContractExposureData((TreatyContractExposureData)original);
            }
        }

        #region Private FM Execution

        private void ProcessEventRecursor_OLDAPI(int eventId,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs, 
            ContractExposureData node,
            bool ShouldAllocate)
        {
            if (null != node)
            {
                if (node is TreatyContractExposureData)
                {
                    #region Recursion

                    Action<object> action = (obj) =>
                        {
                            ProcessEventRecursor_OLDAPI(eventId, EventOccurrenceDRs, (ContractExposureData)obj, true);
                        };

                    var tasks = new List<Task>();
                    var childrenIDs = ((TreatyContractExposureData)node).ChildContractExposureIDs;
                    foreach (long id in childrenIDs)
                    {
                        var next = GetCopyOfContractData(id);

                        var task = TaskManager.Start(id, eventId, action, next);
                        tasks.Add(task);
                    }

                    Task.WaitAll(tasks.ToArray());

                    #endregion
                }

                ConcurrentDictionary<long, ResultPosition> ContractIdToResult;

                if (CacheEventIdContractIdToResult.TryGetValue(eventId, out ContractIdToResult)
                    && !ContractIdToResult.ContainsKey(node.Id) 
                    && (node.state == ProcessState.ContractGraphBuilt 
                    || node.state == ProcessState.FMExecuted || node.state == ProcessState.FMFailed))
                {
                    #region *** Executing Financial Model (current Node) ***

                    Stopwatch sw = Stopwatch.StartNew();
                    var payOutAttr = node.ExecuteFM(EventOccurrenceDRs, ShouldAllocate, 
                        CacheEventIdContractIdToResult[eventId].ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
                    sw.Stop();
                    long t = sw.ElapsedTicks * 1000000000 / Stopwatch.Frequency;

                    ContractIdToResult.TryAdd(node.Id, payOutAttr);

                    #endregion
                }
            }
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

        #endregion

        #region Get Results

        /// <summary>
        /// Return results of FM Execution for given Event
        /// </summary>
        /// <param name="eventID">Event ID</param>
        /// <param name="removeAfterReading">Remove results (for given Event) from Cache after returning</param>
        /// <param name="contractIDs">If empty - return results for independent contractors only, 
        /// otherwise return results for contractIDs even if it dependent contracts</param>
        /// <returns>Results of FM Execution</returns>
        public ConcurrentDictionary<long, ResultPosition> GetResultPositions(int eventID, params long[] contractIDs)
        {
            TaskManager.WaitAll(eventID, contractIDs);

            if (CacheEventIdContractIdToResult.ContainsKey(eventID))
            {
                if (contractIDs.Length == 0)
                    return CacheEventIdContractIdToResult[eventID];
                else
                    return new ConcurrentDictionary<long, ResultPosition>(CacheEventIdContractIdToResult[eventID].Where(kv => contractIDs.Contains(kv.Key)));
            }

            return null;
        }

        public ConcurrentDictionary<int, ConcurrentDictionary<long, ResultPosition>> GetResultPositions()
        {
            TaskManager.WaitAll();
            return CacheEventIdContractIdToResult;
        }

        public void RemoveResultPositions(int eventID)
                {
                    ConcurrentDictionary<long, ResultPosition> rm;
                    CacheEventIdContractIdToResult.TryRemove(eventID, out rm);
                }

        #endregion

        #region Auxilliary APIs

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
            return CacheContractData
                .Where(kv => (null != kv.Value && null != kv.Value.ExposureIDAttributeMap && kv.Value.ExposureIDAttributeMap.Count() > 0))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ExposureIDAttributeMap.Select(kv2 => kv2.Value.Value).Sum());
        }

        public Dictionary<long, double> GetExposureValues()
        {
            Dictionary<long, double> ExposureValuesMap = new Dictionary<long, double>();
            foreach (KeyValuePair<long, ContractExposureData> kv in CacheContractData
                .Where(kv => (null != kv.Value && null != kv.Value.ExposureIDAttributeMap && kv.Value.ExposureIDAttributeMap.Count() > 0)))
            {
                foreach (KeyValuePair<long, RiskItemCharacteristicIDAttributes> kvRITEAttributes in kv.Value.ExposureIDAttributeMap)
                {
                    if (!ExposureValuesMap.ContainsKey(kvRITEAttributes.Key))
                        ExposureValuesMap.Add(kvRITEAttributes.Key,
                            kvRITEAttributes.Value.Value);
                }
            }
            return ExposureValuesMap;
        }

        #endregion

        #endregion
    }
}