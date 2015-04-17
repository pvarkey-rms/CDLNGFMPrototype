using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.DataServices.DataObjects;
using Rms.Analytics.DataService.Zip;
using RMS.Prototype.NGFM;
using ProtoBuf;
using System.Net;
using HasseManager;
using System.Diagnostics;
using NGFMReference;
using NGFM.Reference.MatrixHDFM;
using System.Text.RegularExpressions;

namespace VectorizedReferenceTest
{
    class Program
    {
        static void Main(string[] args)
        {

            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases\NewRL131_RMS_EDM_FinMod_Automation_RL131_72514_PortID_120_1000002113_1\4248\2050305\635447355784670000\rites_batch0.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases\PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_896_1000002164_1\4438\2142576\635447551688220000\rites_batch0.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases\PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_1013_1000002151_1\4413\2142553\635447537560220000\rites_batch0.dat";   
            // string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases Overlap Max Ded\NewRL131_RMS_EDM_FinMod_Automation_RL131_BT4IT4Aug30thNGtoRLAndInvalidationScriptsRanOnThisEDM_PortID_923_1000001480_1\6611\11352686\635416896292050000\rites_batch0.dat";       
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases Overlap Max Ded\PerRisk_RMS_EDM_FinMod_Automation_RL13_BT4IT4Aug30thNGtoRLAndInvalidationScriptsRanOnThisEDM_PortID_958_1000001574_1\7066\11353699\635417441336090000\rites_batch0.dat"; 
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases Overlap Max Ded\EDM_RLFM_RL13_BT4IT4Aug30thNGtoRLAndInvalidationScriptsRanOnThisEDM_PortID_132_1000001835_1\8170\11366277\635419079253350000\rites_batch0.dat";            
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases Overlap Max Ded\RMS_RLFM_EDM2_ForPlatformQA_PortID_1_1000001611_1\7239\11360603\635417579575500000\rites_batch0.dat";  
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases\PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_1021_1000002108_1\4233\2050284\635447346491010000\rites_batch0.dat";  
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases\MTH_200_rites_batch0.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\MTH Cases\rites_batch0_MTH1.dat"; 
            //string filepath = @" C:\LocalNGFM\EDS Extracts\FromRain\Large Extracts\rites_batch62001_PerRisk.dat";
            //string filepath = @"D:\Nina_Automation_Testing\NTA_EDS_Extract\PerRisk_RMS_EDM_FinMod_Automation_RL13_BT4IT4Aug30thNGtoRLAndInvalidationScriptsRanOnThisEDM_PortID_897_1000001508_1\6752\11353141\635417040157160000\rites_batch0.dat";
            //string filepath = @"D:\Matrix_Prototype_Storage\EDS Extract\JPTY_Multibuilding_PerRisk.dat";
            string filepath = @"D:\Nina_Automation_Testing\Treaties\NoHoursClause\No_Reinstatement_NoAgg\From_Slava\rites_batch0_FromSlava.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\From Swapna\rites_batch260197453_swapna_profile_case_Updated.dat";

            //string filepath1 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch1.dat";
            //string filepath1 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_SingleBuilding_overlap_subperil.dat";
            // string filepath1 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_FixedGraph1.dat";
            //string filepath1 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_FixedGraphOverlap.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0_SingleBuilding_overlap.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0_SingleBuilding.dat";  //step policy
            //string filepath = @"C:\LocalNGFM\EDS Extracts\SunnyCreated\TreatyTestCaseWithPositions.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\Test1EQ_StepPGU_CDL.dat";
            //       string filepath = @"C:\LocalNGFM\EDS Extracts\SunnyCreated\TreatyTestCaseWithPositions.dat";
            PartitionData pd = GetPDFromPath(filepath);
            UpdateCDLFOrGUSublimits(pd, 0);

      
            long conID = 11331;
            //long conID = 42586728;
            //long conID = 2050306;


            //TestMatrixHDFM(pd, conID);
            COLCollection COLSet = new COLCollection(new HashSet<string>() { "EQ" });   //{ "EQ", "WS" });  
            //COLCollection COLSet = new COLCollection(new HashSet<string>() { "CSWT" });   //{ "EQ", "WS" }); 
            //TestRITEMap(pd, conID);
            //TestGULossVector(pd, conID);
            //TestReference(GraphType.FixedGraph1, pd, (int)conID, COLSet);  //CDL: 3
            //TestReference(GraphType.FixedGraphOverlap, pd, (int)conID, COLSet);  //CDL:6 
            //TestReference(GraphType.FixedGraphOverlapSubperil, pd, (int)conID, COLSet); //CDL: 7
            //TestReference(GraphType.FixedGraphOverlapSubperil2, pd, (int)conID, COLSet); //CDL: 8
            //TestReference(GraphType.StepPolicyGraph, pd, (int)conID, COLSet); //CDL: 10
            TestReference(GraphType.Auto, pd, (int)conID, COLSet); //CDL: 10
            //TestReferenceWithPeriods(GraphType.Auto, pd, (int)conID, COLSet); /
            //TestReferenceSpeed(GraphType.Auto, pd, (int)conID, COLSet);
            //testPerformance();
            //TestExposureDataAdaptor(pd, (int)conID);                               
        }

        private static void UpdateCDLFOrGUSublimits(PartitionData pd, int ConIndex)
        {
            Regex regex = new Regex(Regex.Escape("Declarations\r\n"));
            string cdlString = pd.Exposures[ConIndex].Contract.CDLString;
            //string NewCDL = regex.Replace(cdlString, "Declarations\r\n    Claims Adjustment Options Are (SUBLIMITS ARE GROUND UP)\r\n", 1);
            //string NewCDL = regex.Replace(cdlString, "Declarations\r\n    Claims Adjustment Options Are (Deductibles are , Sublimits are Net of Deductible)\r\n", 1);
            string NewCDL = regex.Replace(cdlString, "Declarations\r\n    Claims Adjustment Options Are (Deductibles are absorbable,SUBLIMITS ARE GROUND UP)\r\n", 1);
            //string NewCDL = regex.Replace(cdlString, "Declarations\r\n    Claims Adjustment Options Are (Sublimits are Net of Deductible)\r\n", 1);
            pd.Exposures[ConIndex].Contract.CDLString = NewCDL;
        }

        private static PartitionData GetPDFromPath(string filepath)
        {
            return Deserialize(filepath);
        }

        public static void Serialize(PartitionData pData, string file)
        {
            using (var wc = new WebClient().OpenWrite(file))
            {
                try
                {
                    ProtoBuf.Serializer.Serialize<PartitionData>(wc, pData);
                }
                catch (ProtoBuf.ProtoException p)
                {
                    //Log.Fatal("Error Writing of protobuf to file" + p.Message);
                    Console.WriteLine("Error Writing of protobuf to file \"{0}\": {1}", file, p.Message);
                }
            }
        }

        public static PartitionData Deserialize(string file)
        {
            PartitionData result = null;
            using (var wc = new WebClient().OpenRead(file))
            {
                try
                {
                    result = ProtoBuf.Serializer.Deserialize<PartitionData>(wc);
                }
                catch (ProtoBuf.ProtoException p)
                {
                    //Log.Fatal("Error Reading a protobuf file" + p.Message);
                    Console.WriteLine("Error Reading a protobuf file \"{0}\": {1}", file, p.Message);
                }
            }
            return result;
        }

        private static void UpdateEDSExtractWithCDL(string CDl, string filepath, int ConIndex)
        {
            PartitionData PD = Deserialize(filepath);
            PD.Exposures[ConIndex].Contract.CDLString = CDl;

            string newfilepath = @"C:\LocalNGFM\EDS Extracts\From Swapna\rites_batch260197453_swapna_profile_case_Updated.dat";

            Serialize(PD, newfilepath);
        }

        private static void TestRITEMap(PartitionData pd, long conID)
        {
            //Default SubSampling Settings
            SubSamplingAnalysisSetting subSamplingSettings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(pd, subSamplingSettings);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conID);
            ISubPerilConfig subperilInfo = new RMSSubPerilConfig();

            RITEmapper1 mapper = new RITEmapper1(expData, new RAPSettings(new HashSet<string> {"EQ"}), subperilInfo);

        }

        private static void TestGULossVector(PartitionData pd, long conID)
        {
            //Default SubSampling Settings
            SubSamplingAnalysisSetting subSamplingSettings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(pd, subSamplingSettings);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conID);
            ISubPerilConfig subperilInfo = new RMSSubPerilConfig();

            IRITEindexMapper mapper = new RITEmapper1(expData, new RAPSettings(new HashSet<string> {"EQ"}), subperilInfo);
            HashSet<String> subperils = new HashSet<string>{"EQ", "WS"};


            VectorGUInputGeneratorFactory vectorgeneratorFactory = new VectorGUInputGeneratorFactory(pd, new HashSet<string> { "EQ" }, TimeStyle.ConstantTimeStamps, LossStyle.GroundUp, true, subSamplingSettings);
            VectorGUInputGenerator ReferenceEventGen = vectorgeneratorFactory.GetGeneratorForContract(conID);

            IVectorEvent Event = ReferenceEventGen.GenerateRITELoss(1);

        }

        private static void TestMatrixHDFM(PartitionData pd, long conID)
        {
            Stopwatch sw = new Stopwatch();

            //Default SubSampling Settings
            SubSamplingAnalysisSetting subSamplingSettings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(pd, subSamplingSettings);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conID);
            ISubPerilConfig subperilInfo = new RMSSubPerilConfig();

            HashSet<String> subperils = new HashSet<string> { "WI" };
            IRITEindexMapper mapper = new RITEmapper1(expData, new RAPSettings(subperils), subperilInfo);
            


            VectorGUInputGeneratorFactory vectorgeneratorFactory = new VectorGUInputGeneratorFactory(pd, subperils, TimeStyle.ConstantTimeStamps, LossStyle.GroundUp, true, subSamplingSettings);
            VectorGUInputGenerator vectorGenerator = vectorgeneratorFactory.GetGeneratorForContract(conID);


            FixedMatrixGraphJPTY JPTYGraph = new FixedMatrixGraphJPTY(expData);
            JPTYGraph.Initialize();

            MatrixGraphExecuter executer = new MatrixGraphExecuter(JPTYGraph);

            int NumOfEvents = 100;
            double totalTime = 0;

            for (int eventId = 6; eventId < NumOfEvents; eventId++)
            {

                IVectorEvent Event = vectorGenerator.GenerateRITELoss(eventId);

            sw.Start();
            float payout = (float)(executer.Run(Event).TotalPayOut);
            sw.Stop();
                long test = sw.ElapsedMilliseconds;

                //totalTime += sw.Elapsed.TotalMilliseconds;
            }

            double avgTime = sw.Elapsed.TotalMilliseconds / NumOfEvents;
            double avgGraphState = executer.IniGraphState.Elapsed.TotalMilliseconds / NumOfEvents;
            double avgAggregation = executer.Aggregation.Elapsed.TotalMilliseconds / NumOfEvents;
            double avgInteraction = executer.Interaction.Elapsed.TotalMilliseconds / NumOfEvents;
            double avgFillARite = executer.AssignGUtoARite.Elapsed.TotalMilliseconds / NumOfEvents;
            double avgSumByPattern = executer.Aggregation1.Elapsed.TotalMilliseconds / NumOfEvents;
            double avgAllocation = executer.Allocationtimer.Elapsed.TotalMilliseconds / NumOfEvents;
            

            Console.WriteLine("For " + NumOfEvents + " Events avg execution time is "
                                + avgTime + " Milliseconds");
            Console.WriteLine("For" + NumOfEvents + "avg GraphState Instantiation is "
                                + avgGraphState + "Milliseconds");
            Console.WriteLine("For" + NumOfEvents + "avg Aggregation is "
                              + avgAggregation + "Milliseconds");
            Console.WriteLine("For" + NumOfEvents + "avg Interaction is "
                           + avgInteraction + "Milliseconds");
            Console.WriteLine("For" + NumOfEvents + "avg Assigning GU to Arites is "
                           + avgFillARite + "Milliseconds");
            Console.WriteLine("For" + NumOfEvents + "avg SumArrayByPattern is "
               + avgSumByPattern + "Milliseconds");
            Console.WriteLine("For" + NumOfEvents + "avg allocation time is "
               + avgAllocation + "Milliseconds");

            Console.ReadLine();
        }

        public static void TestReference(GraphType type, PartitionData PD, int conID, COLCollection COLSet)
        {
            RAPSettings settings = new RAPSettings(COLSet.GetSubperils());

            //Default SubSampling Settings
            SubSamplingAnalysisSetting subSamplingSettings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            ReferencePrototype Reference = new ReferencePrototype(PD, settings, subSamplingSettings);

            Reference.ReferencePrepare(GraphType.Auto);

            NGFMPrototype NGFM = new NGFMPrototype();
            NGFM.Prepare(PD);  //NGFM result is cached, so create another object for each event

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(PD, subSamplingSettings);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conID);

            GUInputGeneratorFactory generatorFactory = new GUInputGeneratorFactory(PD, COLSet, subSamplingSettings, TimeStyle.RandomTimeStamps, LossStyle.DamagaeRatio);
            GUInputGenerator NGFMEventGen = generatorFactory.GetGeneratorForContract(conID);

            GUInputGenerator ReferenceEventGen = generatorFactory.GetGeneratorForContract(conID);


            int counter = 0;
            int total = 0;

            Console.WriteLine("State at: " + DateTime.Now.ToString("h:mm:ss tt"));
            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            for (int i = 247; i < 248; i += 1)
            {

                Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double,uint, List<float>>>>> NGFMguLoss;
                Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> RefguLoss;
                if (NGFMEventGen.GenerateRITELoss(i))
                {
                    NGFMguLoss = NGFMEventGen.GULosses;
                }
                else
                    throw new InvalidOperationException("Cannot get ground-up loss for event: " + i);
                if (ReferenceEventGen.GenerateRITELoss(i))
                {
                    RefguLoss = ReferenceEventGen.GULosses;
                }
                else
                    throw new InvalidOperationException("Cannot get ground-up loss for event: " + i);

                //Contract ID 11236672 hard coded.. 11324656 
                double ReferencePayout = Reference.Execute(conID, type, RefguLoss).TotalPayout;
                //double ReferencePayout2 = Reference.Execute(conIndex, GraphType.FixedGraph1, RefguLoss);
                //double NGFMpayout = NGFM.ExecuteFM(NGFMguLoss)[11324656];
                RMS.ContractObjectModel.ResultPosition result = NGFM.ProcessEvent(i, NGFMguLoss, true, 1, new long[] { conID })[conID];
                double NGFMpayout = result.PayOut;
                //double NGFMpayout = 0;
                double diff = NGFMpayout - ReferencePayout;

                total += 1;
                if (Math.Abs(diff) > 0.1)
                {
                    counter += 1;
                    Console.WriteLine("Event ID: " + i + " || " + "NGFM: " + Math.Round(NGFMpayout, 5) + " || " + "Reference: " + Math.Round(ReferencePayout, 5) + " || " + Math.Round(diff, 5));
                }
                Console.WriteLine("Event ID: " + i + " || " + "NGFM: " + Math.Round(NGFMpayout, 2) + " || " + "Reference: " + Math.Round(ReferencePayout, 2) + " || " + Math.Round(diff, 2));
            }

            Console.WriteLine("Number of difference: " + counter);
            Console.WriteLine("total event = " + total);
            Console.WriteLine("End at: " + DateTime.Now.ToString("h:mm:ss tt"));
            Console.ReadLine();

        }

        public static void TestReferenceSpeed(GraphType type, PartitionData PD, int conIndex, COLCollection COLSet)
        {
            RAPSettings settings = new RAPSettings(COLSet.GetSubperils());

            //Default SubSampling Settings
            SubSamplingAnalysisSetting subSamplingSettings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            ReferencePrototype Reference = new ReferencePrototype(PD, settings, subSamplingSettings);

            Reference.ReferencePrepare(GraphType.Auto);

            NGFMPrototype NGFM = new NGFMPrototype();
            NGFM.Prepare(PD);  //NGFM result is cached, so create another object for each event

            double MicroSecondTicks = Stopwatch.Frequency / 1000000.0;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            NGFM.Prepare(PD);  //NGFM result is cached, so create another object for each event
            stopwatch.Stop();
            double NGFMGraphTime = Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(PD, subSamplingSettings);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);

            GUInputGeneratorFactory generatorFactory = new GUInputGeneratorFactory(PD, COLSet, subSamplingSettings, TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio);
            GUInputGenerator NGFMEventGen = generatorFactory.GetGeneratorForContract(conIndex);

            VectorGUInputGeneratorFactory vectorgeneratorFactory = new VectorGUInputGeneratorFactory(PD, COLSet.GetSubperils(), TimeStyle.ConstantTimeStamps, LossStyle.GroundUp, true, subSamplingSettings);
            VectorGUInputGenerator ReferenceEventGen = vectorgeneratorFactory.GetGeneratorForContract(conIndex);


            int counter = 0;
            int total = 0;

            Console.WriteLine("State at: " + DateTime.Now.ToString("h:mm:ss tt"));
            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            for (int i = 1; i < 100; i += 1)
            {

                Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double,uint, List<float>>>>> NGFMguLoss;
                //Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> RefguLoss;
                if (NGFMEventGen.GenerateRITELoss(i))
                {
                    NGFMguLoss = NGFMEventGen.GULosses;
                }
                else
                    throw new InvalidOperationException("Cannot get ground-up loss for event: " + i);

                IVectorEvent RefguLoss = ReferenceEventGen.GenerateRITELoss(i);

                stopwatch = new Stopwatch();

                stopwatch.Start();
                double ReferencePayout = Reference.Execute(conIndex, type, RefguLoss).TotalPayout;
                stopwatch.Stop();
                double ReferenceTime = Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;

                stopwatch.Reset();
                stopwatch.Start();
                double NGFMpayout = 0;
                //double NGFMpayout = NGFM.ExecuteFM(NGFMguLoss)[conIndex];
                RMS.ContractObjectModel.ResultPosition result = NGFM.ProcessEvent(i, NGFMguLoss, true, 1, new long[] { conIndex })[conIndex];
                NGFMpayout = result.PayOut;
                stopwatch.Stop();
                double NGFMTime = Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;

                double diff = NGFMTime - ReferenceTime;

                Console.WriteLine("Event ID: " + i + " || " + "NGFM: " + NGFMTime + " || " + "Reference: " + ReferenceTime + " || " + diff);
            }

            Console.WriteLine("total event = " + total);
            Console.WriteLine("NGFM Graph Building Time = " + NGFMGraphTime);
            Console.WriteLine("End at: " + DateTime.Now.ToString("h:mm:ss tt"));
            Console.ReadLine();
        }

        public static void TestReferenceWithPeriods(GraphType type, PartitionData PD, int conID, COLCollection COLSet)
        {
            RAPSettings settings = new RAPSettings(COLSet.GetSubperils());
            //Default SubSampling Settings
            SubSamplingAnalysisSetting subSamplingSettings = new SubSamplingAnalysisSetting(false, 1, 0, 250, "", "");

            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            ReferencePrototype Reference = new ReferencePrototype(PD, settings, subSamplingSettings);
            //Reference.ReferencePrepare(GraphType.Auto);

            NGFMPrototype NGFM = new NGFMPrototype(1);
            NGFM.Prepare(PD);  //NGFM result is cached, so create another object for each event

            DateTime start = DateTime.Now;
            PLTGenertorFactory generatorFactory = new PLTGenertorFactory(PD, COLSet, subSamplingSettings, start, TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio);
            PLTGenerator NGFMEventGen = generatorFactory.GetGeneratorForContract(conID);

            //GUInputGenerator ReferenceEventGen = generatorFactory.GetGeneratorForContract(conID);

            //VectorGUInputGeneratorFactory vectorgeneratorFactory = new VectorGUInputGeneratorFactory(PD, COLSet.GetSubperils(), TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio, true, subSamplingSettings);
            //VectorGUInputGenerator ReferenceEventGen = vectorgeneratorFactory.GetGeneratorForContract(conID);

            int counter = 0;
            int total = 0;

            Console.WriteLine("State at: " + DateTime.Now.ToString("h:mm:ss tt"));
            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            for (int i = 1; i < 200; i += 1)
            {

                Period NGFMPeriod;
                List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> NGFMguLossList;

                IVectorEvent RefguLoss;

                if (NGFMEventGen.GeneratePeriodLoss(i))
                {
                    NGFMPeriod = NGFMEventGen.PeriodLoss;
                }
                else
                    throw new InvalidOperationException("Cannot get ground-up loss for event: " + i);
                //if (ReferenceEventGen.GenerateRITELoss(i))
                //{
                //    RefguLoss = ReferenceEventGen.GULosses;
                //}
                //else
                //    throw new InvalidOperationException("Cannot get ground-up loss for event: " + i);

                //RefguLoss = ReferenceEventGen.GenerateRITELoss(i);


                //Contract ID 11236672 hard coded.. 11324656 
                //double ReferencePayout = 0;        
                ReferenceResultOutput ReferenceOutput = Reference.ExecutePeriod(conID, type, NGFMPeriod.EventLossList);
                double ReferencePayout = ReferenceOutput.TotalPayout;
                //double ReferencePayout2 = Reference.Execute(conIndex, GraphType.FixedGraph1, RefguLoss);
                //double NGFMpayout = NGFM.ExecuteFM(NGFMguLoss)[11324656];
                List<RMS.ContractObjectModel.ResultPosition> results = NGFM.ProcessPeriod(i, NGFMPeriod.EventLossList, true, 1, conID)[conID];
                double NGFMpayout = results.Select(result => result.PayOut).Sum();
                //double NGFMpayout = 0;
                double diff = NGFMpayout - ReferencePayout;

                total += 1;
                if (Math.Abs(diff) > 0.1)
                {
                    counter += 1;
                    Console.WriteLine("Event ID: " + i + " || " + "NGFM: " + Math.Round(NGFMpayout, 5) + " || " + "Reference: " + Math.Round(ReferencePayout, 5) + " || " + Math.Round(diff, 5));
                }
                Console.WriteLine("Event ID: " + i + " || " + "NGFM: " + Math.Round(NGFMpayout, 2) + " || " + "Reference: " + Math.Round(ReferencePayout, 2) + " || " + Math.Round(diff, 2));
            }

            Console.WriteLine("Number of difference: " + counter);
            Console.WriteLine("total event = " + total);
            Console.WriteLine("End at: " + DateTime.Now.ToString("h:mm:ss tt"));
            Console.ReadLine();

        }

    }
}
