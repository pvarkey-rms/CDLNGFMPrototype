using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMS.ContractObjectModel;
using RMS.ContractGraphModel;
using RMS.Prototype.NGFM;

using Newtonsoft.Json;
using JsonPrettyPrinterPlus;

using Rms.Analytics.DataService.Zip;
using Rms.DataServices.DataObjects;
using Rms.Platform.Infrastructure.Diagnostics;

using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RMS.Prototype.NGFMUnitTest
{
    [TestClass]
    public class NGDLM_API_UnitTests
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            var applicationName = ConfigurationManager.AppSettings["ApplicationName"];
            var configFilePath = ConfigurationManager.AppSettings["LoggerPath"];

            RmsLoggerFactory.Initialize(applicationName, configFilePath);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            GC.Collect(0);
        }

        [TestMethod]
        public void HDFM()
        {
            var testCases = new Dictionary<string, Dictionary<string, string>>();

            #region Get Testing Configurations
            using (StreamReader sr = new StreamReader("TestCases_FMExecution.txt"))
            {
                string str;
                Dictionary<string, string> testContent = null;

                while ((str = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(str) || str.StartsWith("//"))//Commenting
                        continue;
                    else if (str.StartsWith("[Test"))
                    {
                        testContent = new Dictionary<string, string>();
                        testCases.Add(str, testContent);
                    }
                    else if (null != testContent)
                    {
                        string[] pair = str.Replace(" ", string.Empty)
                            .Split(new char[] { ':' }, 2);

                        testContent.Add(pair[0], pair[1]);
                    }
                }
            }
            #endregion

            #region === Construct NGFMPrototype & ProcessEvent() ===

            HDFM HDFM = new HDFM();

            #endregion

            int j = 1;

            foreach (var kv in testCases)
            {
                #region Get Expected Results

                var lossesPerSubPeril = ReadGULossFromFile(kv.Value["DR"]);

                PartitionData pData = RMS.Prototype.NGFM.Utilities.DeserializePartitionData(kv.Value["RITE"]);

                if (kv.Value.ContainsKey("CDL"))
                {
                    string cdl = ReadCDL(kv.Value["CDL"]);
                    foreach (var ce in pData.Exposures)
                        ce.Contract.CDLString = cdl;
                }

                string[] res = kv.Value["RESULT"].Split(new char[] { ';', ',', ' ' });
                var expectedResult = new Dictionary<long, double>();
                for (int i = 0; i < res.Length - 1; i += 2)
                {
                    long id = long.Parse(res[i]);
                    if (!expectedResult.ContainsKey(id))
                        expectedResult.Add(id, double.Parse(res[i + 1]));
                }

                #endregion

                #region === ProcessEvent() ===

                Stopwatch sw = Stopwatch.StartNew();

                HDFM.Prepare(pData);

                sw.Stop();

                //Debug.WriteLine("Prepare(" + kv.Key + ") took " + ElapsedNanoSeconds(sw) + " ns!");

                sw.Restart();
                
                var payOutDict = HDFM.ProcessEvent(0, lossesPerSubPeril, false, 1);

                sw.Stop();

                //Debug.WriteLine("Process(" + kv.Key + ") took " + ElapsedNanoSeconds(sw) + " ns!");

                Debug.WriteLine(j + "," + ElapsedNanoSeconds(sw));

                j++;

                #endregion

                #region Comparison

                foreach (long id in expectedResult.Keys)
                {
                    double actualResult = (payOutDict.ContainsKey(id)) ? payOutDict[id].PayOut : -1;
                    Assert.AreEqual(expectedResult[id], actualResult, 0.01, "Test \"{0}\" (ID={1}) is failed.", kv.Key, id);

                    //string r = expectedResult[id];
                    //int n = r.Length - 1 - r.IndexOf('.');

                    //double payOut = (payOutDict.ContainsKey(id)) ? payOutDict[id].PayOut : -1;

                    //double d = Math.Round(payOut, n);
                    //string actualResult = d.ToString();
                    //Assert.AreEqual(expectedResult[id], actualResult, "Test \"{0}\" (ID={1}) is failed.", kv.Key, id);
                }

                #endregion
            }
        }

        public static long ElapsedNanoSeconds(Stopwatch watch)
        {
            return watch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;
        }

        //[TestMethod]
        //public void EventConcurrency_MultipleEvents_SameDR_API_ExecuteFM()
        //{
        //    string ExtractFile = "001_extract.dat";
        //    string DRFile = "001_damage_ratios.txt";
        //    int NumberEvents = 3;
        //    int LevelOfConcurrency = 3;
        //    int EventBatchSize = 1;

        //    #region Get Expected Results

        //    string[] res = "1,673542.72;2,742042.72;3,673542.72;4,623115.81;5,668602.02;6,565000;7,678602.02;8,600941.15;9,606844.78;10,400000;11,328947.78;12,530000;13,328947.78;14,623117.61;15,623115.71;16,623115.71;17,636844.78;18,597917.86;19,553602.02"
        //        .Split(new char[] { ';', ',', ' ' });
        //    var ExpectedResult = new Dictionary<long, string>();
        //    for (int i = 0; i < res.Length - 1; i += 2)
        //    {
        //        long id = long.Parse(res[i]);
        //        if (!ExpectedResult.ContainsKey(id))
        //            ExpectedResult.Add(id, res[i + 1]);
        //    }

        //    #endregion

        //    #region === Construct & Initialize NGFMPrototype

        //    PartitionData pData = RMS.Prototype.NGFM.Utilities.DeserializePartitionData(ExtractFile);

        //    HDFM hdfmPrototype = new HDFM();

        //    hdfmPrototype.Prepare_OLDAPI(pData);

        //    #endregion

        //    #region Clone DRs

        //    Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>> DRs = ReadGULossFromFile(DRFile);

        //    List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>>
        //        DRClones = new List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>>(NumberEvents);

        //    for (int i = 0; i < NumberEvents; i++)
        //    {
        //        Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>> ClonedDRs
        //            = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>(DRs);
        //        DRClones.Add(ClonedDRs);
        //    }

        //    #endregion

        //    #region & ProcessEvent() ===

        //    // old API ----------------
        //    //var payOutDict = HDFM.ProcessEvent(EventOccurrenceDRs);
        //    //-------------------------

        //    ConcurrentDictionary<int, ConcurrentDictionary<long, ResultPosition>> Results =
        //        new ConcurrentDictionary<int, ConcurrentDictionary<long, ResultPosition>>();

        //    Stopwatch sw = Stopwatch.StartNew();

        //    Parallel.For(0, (NumberEvents / EventBatchSize), new ParallelOptions { MaxDegreeOfParallelism = LevelOfConcurrency }, EventBatchId =>
        //    {
        //        int EventStartId = EventBatchId * EventBatchSize;
        //        for (int EventId = EventStartId; EventId < (EventStartId + EventBatchSize); EventId++)
        //            Results.TryAdd(EventId, hdfmPrototype.ProcessEvent(EventId, DRClones[EventId]));
        //    }
        //    );

        //    sw.Stop();

        //    Console.WriteLine();

        //    #endregion

        //    #region Comparison

        //    foreach (long id in ExpectedResult.Keys)
        //    {
        //        string r = ExpectedResult[id];
        //        int n = r.Length - 1 - r.IndexOf('.');

        //        foreach (ConcurrentDictionary<long, ResultPosition> Result in Results.Values)
        //        {
        //            double payOut = (Result.ContainsKey(id)) ? Result[id].PayOut : -1;
        //            double d = Math.Round(payOut, n);
        //            string actualResult = d.ToString();
        //            Assert.AreEqual(ExpectedResult[id], actualResult, "Fail!");
        //        }
        //    }

        //    #endregion
        //}

        //[TestMethod]
        //public void EventConcurrency_MultipleEvents_SameDR_DifferentObjects_API_ExecuteFM()
        //{
        //    string ExtractFile = "002_extract.dat";
        //    string DRFile = "002_damage_ratios.txt";
        //    int NumberEvents = 3;
        //    int LevelOfConcurrency = 3;
        //    int EventBatchSize = 1;

        //    #region Get Expected Results

        //    string[] res = "1,365602.55;2,444780.32;3,434780.32;4,326888.53;5,306888.53;6,341888.53;7,150000;8,150000;9,165000;10,165000;11,365602.55;12,365602.55;13,306888.53"
        //        .Split(new char[] { ';', ',', ' ' });
        //    var ExpectedResult = new Dictionary<long, string>();
        //    for (int i = 0; i < res.Length - 1; i += 2)
        //    {
        //        long id = long.Parse(res[i]);
        //        if (!ExpectedResult.ContainsKey(id))
        //            ExpectedResult.Add(id, res[i + 1]);
        //    }

        //    #endregion

        //    #region === Construct & Initialize NGFMPrototype

        //    List<NGFMPrototype> NGFMPrototypes = new List<NGFMPrototype>(NumberEvents);

        //    for (int i = 0; i < NumberEvents; i++)
        //    {
        //        PartitionData pData = RMS.Prototype.NGFM.Utilities.DeserializePartitionData(ExtractFile);

        //        NGFMPrototype ngfmPrototype = new NGFMPrototype();

        //        ngfmPrototype.Prepare(pData);

        //        NGFMPrototypes.Add(ngfmPrototype);
        //    }

        //    #endregion

        //    #region Clone DRs

        //    Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>> DRs = ReadGULossFromFile(DRFile);

        //    List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>>
        //        DRClones = new List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>>(NumberEvents);

        //    for (int i = NumberEvents - 1; i >= 0; i--)
        //    {
        //        Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>> ClonedDRs
        //            = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>();
        //        foreach (string COL in DRs.Keys)
        //        {
        //            ClonedDRs.Add(COL, new Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>());
        //            foreach (int SampleID in DRs[COL].Keys)
        //            {
        //                ClonedDRs[COL].Add(SampleID, new Dictionary<long, Tuple<uint, List<float>>>());
        //                foreach (long RITEId in DRs[COL][SampleID].Keys)
        //                {
        //                    List<float> drs = new List<float>(DRs[COL][SampleID][RITEId].Item2.Count);
        //                    foreach (float dr in DRs[COL][SampleID][RITEId].Item2)
        //                        drs.Add(dr);
        //                    ClonedDRs[COL][SampleID].Add(RITEId, new Tuple<uint, List<float>>(DRs[COL][SampleID][RITEId].Item1, drs));
        //                }
        //            }
        //        }
        //        DRClones.Add(ClonedDRs);
        //    }

        //    #endregion

        //    #region & ProcessEvent() ===

        //    // old API ----------------
        //    //var payOutDict = HDFM.ProcessEvent(EventOccurrenceDRs);
        //    //-------------------------

        //    ConcurrentDictionary<int, ConcurrentDictionary<long, ResultPosition>> Results =
        //        new ConcurrentDictionary<int, ConcurrentDictionary<long, ResultPosition>>();

        //    Stopwatch sw = Stopwatch.StartNew();

        //    Parallel.For(0, (NumberEvents / EventBatchSize), new ParallelOptions { MaxDegreeOfParallelism = LevelOfConcurrency }, EventBatchId =>
        //    {
        //        int EventStartId = EventBatchId * EventBatchSize;
        //        for (int EventId = EventStartId; EventId < (EventStartId + EventBatchSize); EventId++)
        //            Results.TryAdd(EventId, NGFMPrototypes[EventBatchId].ProcessEvent(EventId, DRClones[EventId]));
        //    }
        //    );

        //    sw.Stop();

        //    Console.WriteLine();

        //    #endregion

        //    #region Comparison

        //    foreach (long id in ExpectedResult.Keys)
        //    {
        //        string r = ExpectedResult[id];
        //        int n = r.Length - 1 - r.IndexOf('.');

        //        foreach (ConcurrentDictionary<long, ResultPosition> Result in Results.Values)
        //        {
        //            double payOut = (Result.ContainsKey(id)) ? Result[id].PayOut : -1;
        //            double d = Math.Round(payOut, n);
        //            string actualResult = d.ToString();
        //            Assert.AreEqual(ExpectedResult[id], actualResult, "Fail!");
        //        }
        //    }

        //    #endregion
        //}

        [TestMethod]
        public void API_ExecuteFM()
        {
            var testCases = new Dictionary<string, Dictionary<string, string>>();

            #region Get Testing Configurations
            using (StreamReader sr = new StreamReader("TestCases_FMExecution.txt"))
            {
                string str;
                Dictionary<string, string> testContent = null;

                while ((str = sr.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(str) || str.StartsWith("//"))//Commenting
                        continue;
                    else if (str.StartsWith("[Test"))
                    {
                        testContent = new Dictionary<string, string>();
                        testCases.Add(str, testContent);
                    }
                    else if (null != testContent)
                    {
                        string[] pair = str.Replace(" ", string.Empty)
                            .Split(new char[] { ':' }, 2);

                        testContent.Add(pair[0], pair[1]);
                    }
                }
            }
            #endregion

            #region === Construct NGFMPrototype ===

            NGFMPrototype ngfmPrototype = new NGFMPrototype();

            #endregion

            foreach (var kv in testCases)
            {
                #region Get Expected Results

                var lossesPerSubPeril = ReadGULossFromFile(kv.Value["DR"]);

                PartitionData pData = RMS.Prototype.NGFM.Utilities.DeserializePartitionData(kv.Value["RITE"]);

                if (kv.Value.ContainsKey("CDL"))
                {
                    string cdl = ReadCDL(kv.Value["CDL"]);
                    foreach (var ce in pData.Exposures)
                        ce.Contract.CDLString = cdl;
                }

                string[] res = kv.Value["RESULT"].Split(new char[] { ';', ',', ' ' });
                var expectedResult = new Dictionary<long, double>();
                for (int i = 0; i < res.Length - 1; i += 2)
                {
                    long id = long.Parse(res[i]);
                    if (!expectedResult.ContainsKey(id))
                        expectedResult.Add(id, double.Parse(res[i + 1]));
                }

                #endregion

                #region === ProcessEvent() ===

                ngfmPrototype.Prepare(pData);

                // old API ----------------
                //var payOutDict = HDFM.ProcessEvent(EventOccurrenceDRs);
                //-------------------------

                // new new API ----------------
                int eventID = 0;
                ngfmPrototype.ProcessEvent_OLDNEWNEWAPI(eventID, lossesPerSubPeril);
                var payOutDict = ngfmPrototype.GetResultPositions(eventID, expectedResult.Keys.ToArray())
                    .ToDictionary(p => p.Key, p => p.Value.PayOut);
                //-------------------------

                #endregion

                #region Comparison

                foreach (long id in expectedResult.Keys)
                {
                    double actualResult = (payOutDict.ContainsKey(id)) ? payOutDict[id] : -1;
                    Assert.AreEqual(expectedResult[id], actualResult, 0.01, "Test \"{0}\" (ID={1}) is failed.", kv.Key, id);
                    //string r = expectedResult[id];
                    //int n = r.Length - 1 - r.IndexOf('.');

                    //double payOut = (payOutDict.ContainsKey(id)) ? payOutDict[id] : -1;

                    //double d = Math.Round(payOut, n);
                    //string actualResult = d.ToString();
                    //Assert.AreEqual(expectedResult[id], actualResult, "Test \"{0}\" (ID={1}) is failed.", kv.Key, id);
                }

                #endregion
            }

            ngfmPrototype.Dispose();
        }

        //[TestMethod]
        //public void EventConcurrency_NewNewAPI_MultipleEvents_SameDR_API_ExecuteFM()
        //{
        //    string ExtractFile = "001_extract.dat";
        //    string DRFile = "001_damage_ratios.txt";
        //    int NumberEvents = 3;
        //    int LevelOfConcurrency = 3;

        //    #region Get Expected Results

        //    string[] res = "1,673542.72;2,742042.72;3,673542.72;4,623115.81;5,668602.02;6,565000;7,678602.02;8,600941.15;9,606844.78;10,400000;11,328947.78;12,530000;13,328947.78;14,623117.61;15,623115.71;16,623115.71;17,636844.78;18,597917.86;19,553602.02"
        //        .Split(new char[] { ';', ',', ' ' });
        //    var ExpectedResult = new Dictionary<long, string>();
        //    for (int i = 0; i < res.Length - 1; i += 2)
        //    {
        //        long id = long.Parse(res[i]);
        //        if (!ExpectedResult.ContainsKey(id))
        //            ExpectedResult.Add(id, res[i + 1]);
        //    }

        //    #endregion

        //    #region === Construct & Initialize NGFMPrototype

        //    PartitionData pData = RMS.Prototype.NGFM.Utilities.DeserializePartitionData(ExtractFile);

        //    NGFMPrototype ngfmPrototype = new NGFMPrototype(LevelOfConcurrency);

        //    ngfmPrototype.Prepare_OLDAPI(pData);

        //    #endregion

        //    #region Clone DRs

        //    var DRClones = new List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>>(NumberEvents);

        //    for (int i = 0; i < NumberEvents; i++)
        //    {
        //        DRClones.Add(ReadGULossFromFile(DRFile));
        //    }

        //    #endregion

        //    #region & ProcessEvent() ===

        //    Stopwatch sw = Stopwatch.StartNew();

        //    for (int eventId = 0; eventId < DRClones.Count; eventId++)
        //    {
        //        ngfmPrototype.ProcessEvent(eventId, DRClones[eventId]);
        //    }

        //    var Results = ngfmPrototype.GetResultPositions();

        //    sw.Stop();

        //    Console.WriteLine();

        //    #endregion

        //    #region Comparison

        //    Assert.AreEqual(Results.Count, NumberEvents);

        //    foreach (long id in ExpectedResult.Keys)
        //    {
        //        string r = ExpectedResult[id];
        //        int n = r.Length - 1 - r.IndexOf('.');

        //        foreach (ConcurrentDictionary<long, ResultPosition> Result in Results.Values)
        //        {
        //            double payOut = (Result.ContainsKey(id)) ? Result[id].PayOut : -1;
        //            double d = Math.Round(payOut, n);
        //            string actualResult = d.ToString();
        //            Assert.AreEqual(ExpectedResult[id], actualResult, "Fail!");
        //        }
        //    }

        //    #endregion
        //}

        private string ReadCDL(string fileName)
        {
            var sb = new StringBuilder();
            using (StreamReader sr = new StreamReader(fileName))
            {
                string str;
                while ((str = sr.ReadLine()) != null)
                {
                    sb.Append(str);
                }
            }
            return sb.ToString();
        }
        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> ReadGULossFromFile(string filePath)
        {
            FileInfo fi = new FileInfo(filePath);
            if (fi.Extension.ToLower().Equals(".dat"))
                return RMS.Prototype.NGFM.Utilities.ReadDamageRatiosFromDATFile(filePath);
            else if (fi.Extension.ToLower().Equals(".dat1"))
                return RMS.Prototype.NGFM.Utilities.ReadUncertOutput(filePath);
            else //if (fi.Extension.ToLower().Equals(".txt") || fi.Extension.ToLower().Equals(".csv"))
                return RMS.Prototype.NGFM.Utilities.ReadDamageRatiosFromCSVFile(filePath);
        }

        [TestMethod]
        public void TestCOLPrecedence()
        {
            List<string[]> COLsHierarchy = new List<string[]>();
            string[] contractCOLs;
            List<string> AdjustedCOLs;

            // SH and SL:SH
            COLsHierarchy.Clear();
            COLsHierarchy.Add(new string[] { "SH", "FF", "SL" });
            contractCOLs = new string[] { "SH", "SL" };
            AdjustedCOLs =
                ContractExposureData.GetAdjustedCOLPrecedence(COLsHierarchy, contractCOLs);
            Assert.IsTrue(AdjustedCOLs.Count == 3);
            Assert.IsTrue(AdjustedCOLs[0].Equals("SH"));
            Assert.IsTrue(AdjustedCOLs[1].Equals("SL:SH"));
            Assert.IsTrue(AdjustedCOLs[2].Equals("SL"));

            // WA:WI and WI:WA
            COLsHierarchy.Clear();
            COLsHierarchy.Add(new string[] { "WI", "WA"});
            COLsHierarchy.Add(new string[] { "WA", "WI" });
            contractCOLs = new string[] { "WA", "WI" };
            AdjustedCOLs = 
                ContractExposureData.GetAdjustedCOLPrecedence(COLsHierarchy, contractCOLs);
            Assert.IsTrue(AdjustedCOLs.Count == 4);
            Assert.IsTrue(AdjustedCOLs[0].Equals("WA:WI"));
            Assert.IsTrue(AdjustedCOLs[1].Equals("WA"));
            Assert.IsTrue(AdjustedCOLs[2].Equals("WI:WA"));
            Assert.IsTrue(AdjustedCOLs[3].Equals("WI"));
        }

        [TestMethod]
        public void TestProduceAllCOLAndAdjustorsWithEquivalents()
        {
            Dictionary<SymbolicValue, HashSet<SymbolicValue>> COLEquivalencyMap = new Dictionary<SymbolicValue, HashSet<SymbolicValue>>()
            {
                { "WA", new HashSet<SymbolicValue>() { "FL" }},
            };


            string AdjustedCOLPrecedence = "WA:WI";
            List<string> AllCOLAndAdjustorsWithEquivalents = 
                ContractExposureData.ProduceAllCOLAndAdjustorsWithEquivalents(AdjustedCOLPrecedence, COLEquivalencyMap);
            Assert.IsTrue(AllCOLAndAdjustorsWithEquivalents.Count == 2);
            Assert.IsTrue(AllCOLAndAdjustorsWithEquivalents[0].Equals("FL:WI"));
            Assert.IsTrue(AllCOLAndAdjustorsWithEquivalents[1].Equals("WA:WI"));

            AdjustedCOLPrecedence = "WI:WA";
            AllCOLAndAdjustorsWithEquivalents =
                ContractExposureData.ProduceAllCOLAndAdjustorsWithEquivalents(AdjustedCOLPrecedence, COLEquivalencyMap);
            Assert.IsTrue(AllCOLAndAdjustorsWithEquivalents.Count == 2);
            Assert.IsTrue(AllCOLAndAdjustorsWithEquivalents[0].Equals("WI:FL"));
            Assert.IsTrue(AllCOLAndAdjustorsWithEquivalents[1].Equals("WI:WA"));
        }
    }
}
