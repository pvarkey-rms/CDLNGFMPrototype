using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFMReference;
using Rms.DataServices.DataObjects;
using Rms.Analytics.DataService.Zip;
using RMS.Prototype.NGFM;
using ProtoBuf;
using System.Net;
using HasseManager;
using System.Configuration;
using System.IO;
using System.Diagnostics;

namespace NGFMReferenceTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\TreatyTestCaseWithPositions.dat";
           
            //PartitionData pdTreaty = GetPDFromPath(filepath);
            //UpdateParDataWithCDL(pdTreaty, 0, 9);
            //UpdateParDataWithCDL(pdTreaty, 1, 3);
            //UpdateParDataWithCDL(pdTreaty, 2, 6);
            //TestTreaty(pdTreaty, 36030);

            //return;


            //step policy
            //COLCollection COLSet2 = new COLCollection(new HashSet<string>() { "EQ", "WS" });
            //string filepath2 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0_SingleBuilding.dat";
            //PartitionData pd2 = GetPDFromPath(filepath2);
            //UpdateParDataWithCDL(pd2, 0, 10);
            //TestReference(GraphType.Auto, pd2, 11331, COLSet2);
            //return;

            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0.dat";
            //string filepath1 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch1.dat";
            //string filepath1 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_SingleBuilding_overlap_subperil.dat";
            // string filepath1 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_FixedGraph1.dat";
            //string filepath1 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_FixedGraphOverlap.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0.dat";           
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0_SingleBuilding_overlap.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0_SingleBuilding.dat";  //step policy
            //string filepath = @"C:\LocalNGFM\EDS Extracts\SunnyCreated\TreatyTestCaseWithPositions.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\Test1EQ_StepPGU_CDL.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\SunnyCreated\TreatyTestCaseWithPositions.dat";
            //PartitionData pd = GetPDFromPath(filepath);
            //UpdateParDataWithCDL(pd, 0, 3);
            //UpdateParDataWithNumBldgs(pd, 0, 1, 0);
            //UpdateParDataWithNumBldgs(pd, 0, 1, 1);
            //UpdateParDataWithNumBldgs(pd, 0, 1, 2);

            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\MTH_200_rites_batch0.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\MTH_218_rites_batch0.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch1001.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch87.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch260197453_swapna_profile_case.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch62001_PerRisk.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch77003.dat";
            string filepath = @"C:\HDFM\Testing\rites_batch0.dat";
            Console.WriteLine(" start at: " + DateTime.Now.ToString("h:mm:ss tt"));

            //testPerformance();
            //return;

            //test schedule tree            
            //PartitionData pd1 = Deserialize(filepath);
            //foreach (ContractExposure exp in pd1.Exposures.ToArray())
            //{
            //    long conIndex = exp.ExposureID;
            //    testScheduleTreeBuilder(pd1, conIndex);
            //}

            //Console.WriteLine(" end at: " + DateTime.Now.ToString("h:mm:ss tt"));
            //return;


            ////test subject tree            
            //PartitionData pd1 = Deserialize(filepath);
            //long conIndex = pd1.Exposures.First().ExposureID;
            //testSubjectTreeBuilder(pd1, conIndex);

            //Console.WriteLine(" end at: " + DateTime.Now.ToString("h:mm:ss tt"));
            //return;

            //MTH cases in Network
            //NGDLM: @"rites_batch62001_PerRisk";
            //test 2:  @"PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_1203_1000002102_1\"
            //test 68: @"PerRisk_RMS_EDM_FinMod_Automation_RL13_BT4IT4Aug30thNGtoRLAndInvalidationScriptsRanOnThisEDM_PortID_1079_1000001848_1\";
            //test 69: @"PerRisk_RMS_EDM_FinMod_Automation_RL13_BT4IT4Aug30thNGtoRLAndInvalidationScriptsRanOnThisEDM_PortID_348_1000001849_1\";
            //test 136: @"RMS_RLFM_EDM1_ForPlatformQA_PortID_1_1000002189_1\";
            //test 137: @"RMS_RLFM_EDM2_ForPlatformQA_PortID_1_1000001611_1\";
            //test 200: @"EDM_RLFM_RL131_72514_PortID_110_1000002298_1\";
            //test 218" @"PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_975_1000002301_1\";
            //test 219: @"PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_976_1000002302_1\";
            //test 220: @"PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_977_1000002303_1\";
            //test 221: @"PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_978_1000002304_1\";
            //test 222: @"PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_988_1000002308_1\";
            //test 224: @"PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_990_1000002310_1\";
            //test 246: @"\EDM_RLFM_RL131_72514_PortID_4_1000002332_1\";
            //test 247: @"EDM_RLFM_RL131_72514_PortID_2_1000002330_1\";
            //test 248: @"EDM_RLFM_RL131_72514_PortID_3_1000002331_1\";
            //test 250: @"\EDM_RLFM_RL131_72514_PortID_5_1000002333_1\";
            //test 251: @"EDM_RLFM_RL131_72514_PortID_117_1000002334_1\";
            //test 255: @"\EDM_RLFM_RL131_72514_PortID_35_1000002337_1";
            //test 359: @"EDM_RLFM_RL131_72514_PortID_163_1000002361_1\";
            //test 382: @"EDM_RLFM_RL131_72514_PortID_155_1000002362_1\";


            //test single EDS file
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_SingleBuilding_overlap_subperil.dat";
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch260197453_swapna_profile_case.dat";

            TestContract(filepath);
            //TestSpeed(filepath);D:\main\NGDLM\Sandbox\CDLNGFMPrototype\NGFMReferenceTest\Program.cs
            return;
            
            //test MTH   
            //string filepath = @"\\ca1ntap01\MPM\MPM\ModelCertification\NextGen\NGFM\NGFM Test Automation\NTA_EDS_Extract\";
            //filepath = filepath + @"PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_975_1000002301_1\";
            //string[] filepaths = null;
            //filepaths = Directory.GetFiles(filepath, "*", SearchOption.AllDirectories).Where(f => f.Contains("rites_batch0")).ToArray();
            //TestContract(filepaths[0]);
            
            //return;


            //test Repositery
            //string filepath = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\";
            var testCases = new Dictionary<string, Dictionary<string, string>>();
            testCases = GetTestCases("TestCaseRepository.txt");
            int testCaseCounter = 0;
            foreach (var kv in testCases)
            {
                testCaseCounter++;

                if (testCaseCounter != 9)
                    continue;

                Console.WriteLine(kv.Key + " start at: " + DateTime.Now.ToString("h:mm:ss tt"));                
                long conID = Convert.ToInt32(kv.Value["ContractID"]);

                PartitionData pd = Deserialize(kv.Value["RITE"]);                

                if (kv.Value.ContainsKey("CDL"))
                {
                    string cdl = kv.Value["CDL"];
                    pd.Exposures[0].Contract.CDLString = cdl;
                }

                //update number of buildings for each location
                if (kv.Value.ContainsKey("Building"))
                {
                    List<String> multiBuilding = kv.Value["Building"].Split(';').ToList<string>();
                    foreach (string str in multiBuilding)
                    {
                        string[] pair = str.Split(new char[] { ',' }, 2);
                        UpdateParDataWithNumBldgs(pd, 0, Convert.ToInt32(pair[1]), Convert.ToInt32(pair[0]));
                    }
                }
                //UpdateParDataWithNumBldgs(pd, 0, 1, 0);
                //UpdateParDataWithNumBldgs(pd, 0, 1, 1);
                //UpdateParDataWithNumBldgs(pd, 0, 1, 2);

            //long conID = 11331;
            //long conID = 11324656;
            //ExposureDataAdaptor ExpData;                
            //ExpData = new ExposureDataAdaptor(pd, conID);

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(pd);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conID);

            //COLCollection COLSet = new COLCollection(new HashSet<string>() { "EQWS" });   //should be { "EQ", "WS" });
                COLCollection COLSet = new COLCollection(new HashSet<string>() { "EQ", "WS", "WI"});

                //GraphType
                string graphTypeString = kv.Value["GraphType"].ToString();
                GraphType gt = (GraphType) Enum.Parse(typeof(GraphType), graphTypeString);

                TestReference(gt, pd, (int)conID, COLSet);
                //TestReferenceSpeed(gt, pd, (int)conID, COLSet);
             //TestReference(GraphType.FixedGraph1, pd, (int)conID, COLSet);  //CDL: 3
            //TestReference(GraphType.FixedGraphOverlap, pd, (int)conID, COLSet);  //CDL:6 
            //TestReference(GraphType.FixedGraphOverlapSubperil, pd, (int)conID, COLSet); //CDL: 7
            //TestReference(GraphType.FixedGraphOverlapSubperil2, pd, (int)conID, COLSet); //CDL: 8
            //TestReference(GraphType.StepPolicyGraph, pd, (int)conID, COLSet); //CDL: 10
                Console.WriteLine(kv.Key + " end at: " + DateTime.Now.ToString("h:mm:ss tt") + "\n\n");
                //Console.ReadLine();  
            }
        }

        static Dictionary<string, Dictionary<string, string>> GetTestCases(string fileName)
        {
            var testCases = new Dictionary<string, Dictionary<string, string>>();

            #region Get Testing Configurations
            using (StreamReader sr = new StreamReader("TestCaseRepository.txt"))
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
                        string[] pair = str.Split(new char[] { ':' }, 2);

                        if (pair[0] == "CDL")
                        {
                            var sb = new StringBuilder();
                            sb.Append(pair[1].Replace("\"", ""));
                            while ((str = sr.ReadLine()) != null)
                            {
                                sb.Append("\r\n" + str.Replace("\"", ""));
                                if (str.EndsWith("\""))
                                    break;
                            }
                            pair[1] = sb.ToString();
                        }                       
                        testContent.Add(pair[0], pair[1]);                        
                    }
                }
            }
            #endregion

            return testCases;
        }

        static void UpdateParDataWithCDL(PartitionData pd, int conIndex, int CDLType)
        {
            pd.Exposures[conIndex].Contract.CDLString = GetCDL(CDLType);        
        }

        static void UpdateParDataWithNumBldgs(PartitionData pd, int conIndex, int Numbldgs, int LocNum)
        {
            ContractSubjectExposureOfRiteSchedule RITEs = pd.Exposures[0].ContractSubjectExposures[0] as ContractSubjectExposureOfRiteSchedule;
            RITEs.RITECollectionExposure.RITExposures.ToArray()[LocNum].CommonCharacteristics.NumBuildings = Numbldgs;    //raintest       
        }

        static string GetCDL(int CDLnumber)
        {
            string cdl;
            switch (CDLnumber)
            {
                case 1:
                    {
                        cdl = @"Contract Declarations
                                Name is {NGFM EQ102}
                                Currency is USD
                                Using RMS {22 Jul 2014}
                                Inception is 01 Jan 2000
                                Expiration is 31 Dec 2003
                                Type is {Primary Policy}
                                Subject is Loss to S2729 by EQ, WS
                                Covers 
                                L102_3222: 100% Share of 1M xs 500 For Loss to S2729.EQ by EQ, WS 
                                L102_3223: 100% Share of 1M xs 500 on L102_3222, L102_3223
                                Sections
                                Section Sec1:
                                        Declarations
                                Premium is 10
                                        CoverNames are (L102_3222)
                                SubLimits 
                                30K For Loss to S2729.EQ.59491 by EQ, WS Per Risk
                                55K For Loss to S2729.EQ.59492 by EQ, WS 
                                10M For Building to S2729.EQ.59491 by EQ, WS 
                                100K For Contents to S2729.EQ.59491 by EQ, WS 
                                Deductibles 
                                1K For Loss to S2729.EQ.59491 by EQ, WS Per Risk
                                200 For Loss to S2729.EQ.59492 by EQ, WS 
                                30K For Building to S2729.EQ.59491 by EQ, WS";
                        break;
                    }
                case 2:
                    {
                        cdl = @"Contract Declarations
                        Name is {NGFM EQ102}
                        Currency is USD
                        Using RMS {22 Jul 2014}
                        Inception is 01 Jan 2000
                        Expiration is 31 Dec 2003
                        Type is {Primary Policy}
                        Subject is Loss to S2729 by EQ,WS 
                        Covers 
                        L102_3222: 100% Share of 1M xs 500 For Loss to S2729.EQ by EQ,WS
                        Sections
                        Section Sec1:
                                Declarations
                        Premium is 10
                                CoverNames are (L102_3222)
                        SubLimits 
                        30K For Loss to S2729.EQ.59491 by EQ
                        55K For Loss to S2729.EQ.59492 by EQ,WS
                        10M For Building to S2729.EQ.59491 by EQ 
                        100K For Contents to S2729.EQ.59491 by EQ 
                        Deductibles 
                        1K For Loss to S2729.EQ.59491 by EQ
                        200 For Loss to S2729.EQ.59492 by EQ,WS
                        5K For Building to S2729.EQ.59491 by EQ";
                        break;
                    }
                case 3:
                    {
                        cdl = @"Contract Declarations
                            Name is {NGFM EQ102}
                            Currency is USD
                            Using RMS {1 Jan 2014}
                            Inception is 1 Jan 2014
                            Expiration is 31 Dec 2014
                            Type is {Primary Policy}
                            Subject is Loss to S2729 by EQ
                            Covers 
                            L102_3222: 100% Share of 1M xs 500 For Loss to S2729.EQ by EQ
                            Sections
                            Section Sec1:
                                    Declarations
                            Premium is 10
                                    CoverNames are (L102_3222)
                            SubLimits 
                            30K For Loss to S2729.EQ.59491 by EQ Per Risk
                            55K For Loss to S2729.EQ.59492 by EQ Per Risk
                            10M For Building to S2729.EQ.59491 by EQ Per Risk
                            100K For Contents to S2729.EQ.59491 by EQ Per Risk
                            Deductibles 
                            1K For Loss to S2729.EQ.59491 by EQ Per Risk
                            200 For Loss to S2729.EQ.59492 by EQ Per Risk
                            30K For Building to S2729.EQ.59491 by EQ Per Risk";
                        break;
                    }
                case 4:
                    {
                        cdl = @"Contract Declarations
                            Name is {NGFM EQ102}
                            Currency is USD
                            Using RMS {22 Jul 2014}
                            Inception is 01 Jan 2000
                            Expiration is 31 Dec 2003
                            Type is {Primary Policy}
                            Subject is Loss to S2729 by EQSH, EQFF 
                            Covers 
                            L102_3222: 100% Share of 1M xs 500 For Loss to S2729.EQ by EQSH, EQFF
                            Sections
                            Section Sec1:
                                    Declarations
                            Premium is 10
                                    CoverNames are (L102_3222)
                            SubLimits 
                            30K For Loss to S2729.EQ by EQSH, EQFF
                            Deductibles 
                            1K For Loss to S2729.EQ by EQSH, EQFF";
                        break;
                    }
                case 5:
                    {
                        cdl = @"Contract Declarations
                            Name is {NGFM EQ102}
                            Currency is USD
                            Using RMS {22 Jul 2014}
                            Inception is 01 Jan 2000
                            Expiration is 31 Dec 2003
                            Type is {Primary Policy}
                            Subject is Loss to S2729 by EQ, WS 
                            Covers 
                            L102_3222: 100% Share of 1M xs 500 For Loss to S2729.EQ by EQ, WS
                            L102_3223: 50% Share of 1M xs 500 on L102_3222
                            Sections
                            Section Sec1:
                                    Declarations
                            Premium is 10
                                    CoverNames are (L102_3222)
                            SubLimits 
                            30K For Loss to S2729.EQ.59491 by EQ, WS Per Risk
                            55K For Loss to S2729.EQ.59492 by EQ, WS Per Risk
                            10M For Building to S2729.EQ.59491 by EQ, WS Per Risk
                            100K For Contents to S2729.EQ.59491 by EQ, WS Per Risk
                            Deductibles 
                            1K For Loss to S2729.EQ.59491 by EQ, WS Per Risk
                            200 For Loss to S2729.EQ.59492 by EQ, WS Per Risk
                            30K For Building to S2729.EQ.59491 by EQ, WS Per Risk";
                        break;
                    }
                case 6:  
                    {
                        cdl = @"Contract Declarations
                                Name is {Test 1 EQ}
                                Currency is USD
                                Using RMS {19 Jun 2014}
                                Inception is 1 Jan 2014
                                Expiration is 31 Dec 2014
                                Type is {Primary Policy}
                                Subject is Loss to S16 by EQ 
                                Covers 
                                L938_365: 100% Share of 500M xs 0.01 For Loss to S16.EQ by EQ 
                                SubLimits 
                                1M For Building to S16.EQ by EQ 
                                500K For Contents to S16.EQ by EQ 
                                150K For BI to S16.EQ by EQ 
                                300K For Loss to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                1M For Loss to S16.EQ.SubPolicy5229.47 by EQ 
                                200K For Loss to S16.EQ.48 by EQ 
                                500K For Building to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                200K For Contents to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                50K For BI to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                1M For Building to S16.EQ.SubPolicy5229.47 by EQ 
                                400K For Contents to S16.EQ.SubPolicy5229.47 by EQ 
                                200K For BI to S16.EQ.SubPolicy5229.47 by EQ 
                                300K For Building to S16.EQ.48 by EQ 
                                200K For Contents to S16.EQ.48 by EQ 
                                40K For BI to S16.EQ.48 by EQ 
                                Deductibles 
                                40K For Loss to S16.EQ by EQ 
                                50K For Building to S16.EQ by EQ 
                                50K For Contents to S16.EQ by EQ 
                                10K For BI to S16.EQ by EQ 
                                32K For Loss to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                40K For Loss to S16.EQ.SubPolicy5229.47 by EQ 
                                5K For Loss to S16.EQ.48 by EQ 
                                20K For Building to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                25K For Contents to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                2K  For BI to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                20K For Building to S16.EQ.SubPolicy5229.47 by EQ 
                                50K For Contents to S16.EQ.SubPolicy5229.47 by EQ";
                        break;
                    }
                case 7:
                    {
                        cdl = @"Contract Declarations
                                Name is {Test 1 EQ}
                                Currency is USD
                                Using RMS {19 Jun 2014}
                                Inception is 29 Mar 2005
                                Expiration is 29 Mar 2007
                                Type is {Primary Policy}
                                Subject is Loss to S16 by EQ,WS
                                Covers 
                                L937_16: 100% Share of 500M xs 0.01 For Loss to S16.EQ by EQ,WS
                                SubLimits 
                                1M For Building to S16.EQ by EQ,WS
                                500K For Contents to S16.EQ by EQ,WS
                                150K For BI to S16.EQ by EQ,WS
                                300K For Loss to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                1M For Loss to S16.EQ.SubPolicy5229.47 by EQ,WS Per Risk
                                200K For Loss to S16.EQ.48 by EQ,WS Per Risk
                                500K For Building to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                200K For Contents to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                50K For BI to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                1M For Building to S16.EQ.SubPolicy5229.47 by EQ,WS Per Risk
                                400K For Contents to S16.EQ.SubPolicy5229.47 by EQ,WS Per Risk
                                200K For BI to S16.EQ.SubPolicy5229.47 by EQ,WS Per Risk
                                300K For Building to S16.EQ.48 by EQ,WS Per Risk
                                200K For Contents to S16.EQ.48 by EQ,WS Per Risk
                                40K For BI to S16.EQ.48 by EQ,WS Per Risk
                                Deductibles 
                                40K For Loss to S16.EQ by EQ,WS
                                50K For Building to S16.EQ by EQ,WS
                                50K For Contents to S16.EQ by EQ,WS
                                10K For BI to S16.EQ by EQ,WS
                                32K For Loss to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                40K For Loss to S16.EQ.SubPolicy5229.47 by EQ,WS Per Risk
                                5K For Loss to S16.EQ.48 by EQ,WS Per Risk
                                20K For Building to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                25K For Contents to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                2K  For BI to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                20K For Building to S16.EQ.SubPolicy5229.47 by EQ,WS Per Risk
                                50K For Contents to S16.EQ.SubPolicy5229.47 by EQ,WS Per Risk";
                        break;
                    }
                case 8:
                    {
                        cdl = @"Contract Declarations
                                Name is {Test 1 EQ}
                                Currency is USD
                                Using RMS {19 Jun 2014}
                                Inception is 29 Mar 2005
                                Expiration is 29 Mar 2007
                                Type is {Primary Policy}
                                Subject is Loss to S16 by EQ,WS
                                Covers 
                                L938_365: 100% Share of 500M xs 0.01 For Loss to S16.EQ by EQ,WS
                                SubLimits 
                                1M For Building to S16.EQ by EQ
                                500K For Contents to S16.EQ by WS
                                150K For BI to S16.EQ by EQ,WS
                                300K For Loss to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                1M For Loss to S16.EQ.SubPolicy5229.47 by WS Per Risk
                                200K For Loss to S16.EQ.48 by EQ,WS Per Risk
                                500K For Building to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                200K For Contents to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                50K For BI to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                1M For Building to S16.EQ.SubPolicy5229.47 by WS Per Risk
                                400K For Contents to S16.EQ.SubPolicy5229.47 by WS Per Risk
                                200K For BI to S16.EQ.SubPolicy5229.47 by WS Per Risk
                                300K For Building to S16.EQ.48 by EQ,WS Per Risk
                                200K For Contents to S16.EQ.48 by EQ,WS Per Risk
                                40K For BI to S16.EQ.48 by EQ,WS Per Risk
                                Deductibles 
                                40K For Loss to S16.EQ by EQ,WS
                                50K For Building to S16.EQ by EQ
                                50K For Contents to S16.EQ by WS
                                10K For BI to S16.EQ by EQ,WS
                                32K For Loss to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                40K For Loss to S16.EQ.SubPolicy5229.47 by WS Per Risk
                                5K For Loss to S16.EQ.48 by EQ,WS Per Risk
                                20K For Building to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                25K For Contents to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                2K  For BI to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ Per Risk
                                20K For Building to S16.EQ.SubPolicy5229.47 by WS Per Risk
                                50K For Contents to S16.EQ.SubPolicy5229.47 by WS Per Risk";
                        break;
                    }
                //OCCURRENCES ARE (720 Hours by EQ)
                case 9:
                    {
                        cdl = @"Contract Declarations
                                        Name is CAT_14c
                                        Inception is 01 Jan 2014
                                        Expiration is 31 Dec 2014
                                        Attachment Basis is Loss Occurring 
                                        OCCURRENCES ARE (10 DAYS by EQ)                                                                         
                                        Type is {Catastrophe Treaty}
                                      Subject is PositionA_Gross by EQ 
                                      Covers 
                                        OccLim:
                                             100% Share of 10M xs 3000 by EQ";                                
                        break;
                    }
                case 10:
                    {
                        cdl = @"Contract Declarations
                                Name is {Test 1 EQ}
                                Currency is USD
                                Using RMS {19 Jun 2014}
                                Inception is 29 Mar 2005
                                Expiration is 29 Mar 2007
                                Type is {Primary Policy}
                                Claims Adjustment Options Are (Deductibles are absorbable)
                                Subject is Loss to S16 by EQ 
                                Covers 
                                Blanket_EQ: 100% Share on Max(L937_25,L938_26,L939_27)
                                L937_25: 100% Share of Pay 150000 xs 100K For Building, Contents to S16.EQ by EQ 
                                L938_26: 100% Share of Pay Min(Subject,400000) xs 300K For Contents, BI to S16.EQ by EQ 
                                L939_27: 100% Share on Max(L939_27_Building,L939_27_Contents,L939_27_BI)
                                L939_27_Building: 100% Share of Pay Min(Subject,1000000) xs 800K For Building to S16.EQ by EQ 
                                L939_27_Contents: 100% Share of Pay Min(Subject,1000000) xs 800K For Contents to S16.EQ by EQ 
                                L939_27_BI: 100% Share of Pay Min(Subject,1000000) xs 800K For BI to S16.EQ by EQ";
                        break;
                    }
                case 11:
                    {
                        cdl = @"Contract 
                        Declarations
                        Name is {Test 1 EQ}
                        Currency is USD
                        Using RMS {19 Jun 2014}
                        Inception is 29 Mar 2005
                        Expiration is 29 Mar 2007
                        Type is {Primary Policy}
                        Claims Adjustment Options Are (Sublimits are Net of Deductible)
                        Subject is Loss to S16 by EQ 
                        Covers 
                        L938_365: 100% Share of 500M For Loss to S16.EQ by EQ 
                        SubLimits 
                        RL_Policy: 1M For Building to S16.EQ by EQ 
                        RL_Policy: 500K For Contents to S16.EQ by EQ 
                        RL_Policy: 150K For BI to S16.EQ by EQ 
                        300K For Loss to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ 
                        1M For Loss to S16.EQ.SubPolicy5229.47 by EQ 
                        200K For Loss to S16.EQ.48 by EQ 
                        500K For Building to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ 
                        200K For Contents to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ 
                        50K For BI to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ 
                        1M For Building to S16.EQ.SubPolicy5229.47 by EQ 
                        400K For Contents to S16.EQ.SubPolicy5229.47 by EQ 
                        200K For BI to S16.EQ.SubPolicy5229.47 by EQ 
                        300K For Building to S16.EQ.48 by EQ 
                        200K For Contents to S16.EQ.48 by EQ 
                        40K For BI to S16.EQ.48 by EQ 
                        Deductibles 
                        RL_Policy: 50K Max For Loss to S16.EQ by EQ 
                        RL_Policy: 40K For Loss to S16.EQ by EQ 
                        RL_Policy: 50K For Building to S16.EQ by EQ 
                        RL_Policy: 50K For Contents to S16.EQ by EQ 
                        RL_Policy: 10K For BI to S16.EQ by EQ 
                        2% RCV Covered For Loss to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ 
                        40K For Loss to S16.EQ.SubPolicy5229.47 by EQ 
                        5K For Loss to S16.EQ.48 by EQ 
                        2% RCV Covered For Building to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ 
                        5% RCV Covered For Contents to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ 
                        2% RCV Covered For BI to S16.EQ.SubPolicy5229.SubPolicy5228.46 by EQ 
                        20K For Building to S16.EQ.SubPolicy5229.47 by EQ 
                        50K For Contents to S16.EQ.SubPolicy5229.47 by EQ";
                        break;
                    }

                default:
                    throw new NotSupportedException("No CDL for testcase: " + CDLnumber);
            }
            return cdl;
        }

        //static void TestExtractor(Graph GraphOne, ExposureDataAdaptor ExpData)
        //{
        //    NGFMReference.FinancialTermExtractor finExtractor = new NGFMReference.PrimaryTermExtractor(ExpData);

        //    string error;
        //    if (!finExtractor.GetTermsForGraph(GraphOne, out error))
        //        throw new Exception(error);
        //}

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

        public static void TestReference(GraphType type, PartitionData PD, long conIndex, COLCollection COLSet)
        {

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(PD);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);

            //int numB = 0;
            //for (int i = 0; i < 368; i++)
            //{
            //    ContractSubjectExposureOfRiteSchedule RITEs = PD.Exposures[0].ContractSubjectExposures[0] as ContractSubjectExposureOfRiteSchedule;
            //    numB += RITEs.RITECollectionExposure.RITExposures.ToArray()[i].CommonCharacteristics.NumBuildings;
            //    UpdateParDataWithNumBldgs(PD, 0, 1, i);
            //}

           // GUInputGenerator NGFMEventGen = new GUInputGenerator(PD, conIndex, COLSet, TimeStyle.RandomTimeStamps, LossStyle.DamagaeRatio);
           // GUInputGenerator ReferenceEventGen = new GUInputGenerator(PD, conIndex, COLSet, TimeStyle.RandomTimeStamps, LossStyle.DamagaeRatio);

           // int counter = 0;
           // int total = 0;

           // //NGFMPrototype NGFM = new NGFMPrototype();
           // //NGFM.Prepare(PD);
           // //ExposureDataAdaptor expData = new ExposureDataAdaptor(PD, NGFM);

           // //PartitionDataAdpator PDataAdap = new PartitionDataAdpator(PD);
           //// ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);

   

           // NGFMPrototype NGFM = new NGFMPrototype();
           // NGFM.Prepare(PD);
           // ReferencePrototype Reference = new ReferencePrototype(PD);    

           // for (int i = 1; i < 100; i += 1)
           // {               
           //     //Generate GU Loss         
           //     Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>> NGFMguLoss;
           //     Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>> RefguLoss;
           //    // if (NGFMEventGen.GenerateRITELoss(i))
           //   //  {
           //   //      NGFMguLoss = NGFMEventGen.GULosses;
           //    // }
           //    // else
           //     //    throw new InvalidOperationException("Cannot get ground-up loss for event: " + i);
           //     //if (ReferenceEventGen.GenerateRITELoss(i))
           //     //{
           //     //    RefguLoss = ReferenceEventGen.GULosses;
           //     //}
           //     //else
           //     //    throw new InvalidOperationException("Cannot get ground-up loss for event: " + i);

           //     //Contract ID 11236672 hard coded.. 11324656
           //     //double ReferencePayout = 0;
           //     //double NGFMpayout = 0;
           //    double ReferencePayout = Reference.Execute(conIndex, type, RefguLoss);
           //     //double NGFMpayout = NGFM.ExecuteFM(NGFMguLoss)[11324656];

           //     //NGFM.ExecuteFM(i, NGFMguLoss, conIndex);

           //     //double NGFMpayout = NGFM.GetPayoutsByContractId(i, true, conIndex)[conIndex];
                
           //     //public Dictionary<long, double> GetPayoutsByContractId(int eventID, params long[] ids)

           //     double diff = NGFMpayout - ReferencePayout;

           //     total += 1;
           //     if (Math.Abs(diff) > 0.2)
           //     {
           //         counter += 1;
           //         //Console.WriteLine("Event ID: " + i + " || " + "NGFM: " + Math.Round(NGFMpayout, 5) + " || " + "Reference: " + Math.Round(ReferencePayout, 5) + " || " + Math.Round(diff, 5));
           //     }
           //     Console.WriteLine("Event ID: " + i + " || " + "NGFM: " + Math.Round(NGFMpayout, 2) + " || " + "Reference: " + Math.Round(ReferencePayout, 2) + " || " + Math.Round(diff, 2));                
           //     //NGFM = null;
           //     GC.Collect();
           // }

            
            PerformanceMetrics metrics = TestReferenceSpeed2(type, PD, (int)conIndex, COLSet);
            //Console.WriteLine("Number of difference: " + counter + " out of " + total + " events.");
            Console.WriteLine("ExecutionRatio: " + metrics.NGFM2RefRatioExecute + " Graph Ratio: " + metrics.NGFM2RefRatioGraph);
            Console.WriteLine("Reference Absolute Execution Time: " + metrics.ReferenceTime);
            Console.WriteLine("NGFM Absolute Execution Time: " + metrics.NGFMTime);

            //Console.WriteLine("total event = " + total);
        }

        public static void testPerformance()
        {
            Dictionary<int, HashSet<int>> LookUpChild = new Dictionary<int, HashSet<int>>();


            HashSet<int> parent = new HashSet<int>();
            HashSet<int> child = new HashSet<int>() { 345 };
            int m = 1000000;

            int[] big1 = new int[m];
            int[] big2 = new int[m];
            int[] big3 = new int[m];

            int i;
            for (i = 1; i < m; i++)
            {
                parent.Add(i);
            }

            for (i = 1; i < m; i++)
            {
                big1[i] = i;
                big2[i] = 2 * i;
            }

            Stopwatch stopwatch = new Stopwatch();
            Console.WriteLine(" end at: " + DateTime.Now.ToString("h:mm:ss tt"));
            stopwatch.Start();
            bool tempCompare = false;
            for (i = 1; i < m; i++)
            {
                //tempCompare = child.IsSubsetOf(parent);            
                big3[i] = big1[i] * big2[i];
            }
            stopwatch.Stop();
            Console.WriteLine("Number of Ticks TestOne: " + stopwatch.ElapsedTicks);


            stopwatch.Reset();
            stopwatch.Start();
            big3 = big1.Zip(big2, (d1, d2) => d1 * d2).ToArray();
            //for (i = 1; i <= m; i++)
            //{
            //    for (int j = 1; j <= m; j++)
            //    {
            //        tempCompare = child.IsSubsetOf(child);                    
            //    }            
            //}


            stopwatch.Stop();

            Console.WriteLine("Number of Ticks TestTwo: " + stopwatch.ElapsedTicks);
            Console.ReadLine();
        }

        public static void testScheduleTreeBuilder(PartitionData pd, long conIndex)
        {
            List<ScheduleOfRITEs> sorList = new List<ScheduleOfRITEs>();

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(pd);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);
            Console.WriteLine(" expData end at: " + DateTime.Now.ToString("h:mm:ss tt"));  
            HashSet<ScheduleOfRITEs> schedules = expData.Schedules;
            List<IScheduleInput> sInputList = new List<IScheduleInput>();

            int i = 0;
            foreach (ScheduleOfRITEs aRite in schedules)
            {
                i++;
                bool _isPR = false;
                //if (i % 2 == 1)
                if (i == 1)
                   _isPR = true;

                ScheduleInput aInput = new ScheduleInput(aRite, _isPR);          
                sInputList.Add(aInput);
            }

            ScheduleTreeBuilder buildTree = new ScheduleTreeBuilder(sInputList);
            buildTree.Run();
        }



        //public static void testSubjectTreeBuilder(PartitionData pd, long conIndex)
        //{           
        //    PartitionDataAdpator PDataAdap = new PartitionDataAdpator(pd);
        //    ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);
        //    Console.WriteLine(" expData end at: " + DateTime.Now.ToString("h:mm:ss tt"));
        //    HashSet<ScheduleOfRITEs> schedules = expData.Schedules;
            
        //    List<ISubjectInput> sInputList = new List<ISubjectInput>();

        //    int i = 0;
        //    foreach (ScheduleOfRITEs sch in schedules)
        //    {
        //        i++;
        //        bool _isPR = false;
        //        string col = "EQ";
        //        string exp = "B";

        //        //if (i % 2 == 1)
        //        if (i == 1)               
        //            _isPR = true;

        //        if (i % 2 == 0)
        //        {
        //            col = "EQWS";
        //            exp = "BC";
        //        }
    
        //        SubjectInput aInput = new SubjectInput(col, exp, sch, _isPR);
        //        sInputList.Add(aInput);
        //    }
      
        //    SubjectTreeBuilder buildTree = new SubjectTreeBuilder(sInputList);
        //    buildTree.Run();
        //}

        private static void TestExposureDataAdaptor(PartitionData pd, int conIndex)
        {
            //ExposureDataAdaptor expAdaptor = new ExposureDataAdaptor(pd, conIndex);
            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(pd);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);
        }

        private static void TestTreaty(PartitionData treatypd, int treatyID)
        {

            ReferencePrototype Reference = new ReferencePrototype(treatypd);

            Reference.AddBuildSettings(11331, GraphType.FixedGraphOverlap);
            Reference.AddBuildSettings(11324656, GraphType.FixedGraph1);

            COLCollection COLSet = new COLCollection(new HashSet<string>() { "EQ" });   //{ "EQ", "WS" });
            //ExposureDataAdaptor treatyexpData = new ExposureDataAdaptor(treatypd, treatyID);

            GUInputGenerator NGFMEventGen = new GUInputGenerator(treatypd, 36030, COLSet, TimeStyle.RandomTimeStamps, LossStyle.DamagaeRatio);
            GUInputGenerator ReferenceEventGen = new GUInputGenerator(treatypd, 36030, COLSet, TimeStyle.RandomTimeStamps, LossStyle.GroundUp);

            int counter = 0;
            int total = 0;

            Console.WriteLine("State at: " + DateTime.Now.ToString("h:mm:ss tt"));
            NGFMPrototype NGFM = new NGFMPrototype();
            NGFM.Prepare(treatypd);
            for (int i = 6; i < 12; i += 2)
            {
                //NGFMPrototype NGFM = new NGFMPrototype(treatypd);  //NGFM result is cached, so create another object for each event
                //Generate GU Loss         
                Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> NGFMguLoss;
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
                //Dictionary<long, SortedDictionary<DateTime, double>> NGFMoutput = NGFM.ExecuteFM_GetTimeSeries(NGFMguLoss, new long[] { 11331, 11324656, 36030 });

                //double overlapPayout = NGFM.GetTimeSeriesByContractId()[11331].Values.Sum();
                //double normalPayout = NGFM.GetTimeSeriesByContractId()[11324656].Values.Sum();
                //double NGFMpayout = NGFM.ExecuteFM(NGFMguLoss, new long[] { 36030 })[36030];
                double NGFMpayout = 0;

                //Execute Reference
                double RefnormalPayout = Reference.Execute(11324656, GraphType.FixedGraph1, RefguLoss);
                double RefoverlapPayout = Reference.Execute(11331, GraphType.FixedGraphOverlap, RefguLoss);

                //LossTimeSeries RefoverlapSeries = GetLossTimeSeries(NGFM.GetTimeSeriesByContractId()[11331]);
                //LossTimeSeries RefnormalSeries = GetLossTimeSeries(NGFM.GetTimeSeriesByContractId()[11324656]);
                //RefoverlapSeries.AllocateRatio(.3333);
                //RefnormalSeries.AllocateRatio(.3333);

                //Reference.InputLossForGraph(11331, RefoverlapSeries);
                //Reference.InputLossForGraph(11324656, RefnormalSeries);

                double ReferencePayout = Reference.Execute(36030, GraphType.FixedTreaty1, RefguLoss);
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

        private static PartitionData BuildPDWithTreaty()
        {
            string treatyfilepath = @"C:\LocalNGFM\EDS Extracts\Cat-Treaty-Protobuffs\36030\635393344682880000\rites_batchTreatyExposures.dat";
            string primaryFilepath = @"C:\LocalNGFM\EDS Extracts\FromRain\OverlapCase\rites_batch0_SingleBuilding_overlap.dat";
            string primaryFilepath2 = @"C:\LocalNGFM\EDS Extracts\FromRain\Version2\rites_batch0.dat";

            PartitionData treatypd = GetPDFromPath(treatyfilepath);
            UpdateParDataWithCDL(treatypd, 0, 9);

            PartitionData primarypd = GetPDFromPath(primaryFilepath);
            Serialize(primarypd, primaryFilepath);
            primarypd = GetPDFromPath(primaryFilepath);

            int size = primarypd.Exposures.Count();

            PartitionData primarypd2 = GetPDFromPath(primaryFilepath2);

            Array.Resize(ref primarypd.Exposures, size + 2);
            primarypd.Exposures[size] = treatypd.Exposures[0];
            primarypd.Exposures[size + 1] = primarypd2.Exposures[0];

            string savePath = @"C:\LocalNGFM\EDS Extracts\SunnyCreated\TreatyTestCase.DAT";
            Serialize(primarypd, savePath);
            PartitionData TestSerialize = GetPDFromPath(primaryFilepath);


            return primarypd;
        }

        public static void TestReferenceSpeed(GraphType type, PartitionData PD, int conIndex, COLCollection COLSet)
        {
            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            ReferencePrototype Reference = new ReferencePrototype(PD);
            NGFMPrototype NGFM = new NGFMPrototype();

            double MicroSecondTicks = Stopwatch.Frequency / 1000000.0;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            NGFM.Prepare(PD);  //NGFM result is cached, so create another object for each event
            stopwatch.Stop();
            double NGFMGraphTime = Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(PD);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);

            GUInputGenerator NGFMEventGen = new GUInputGenerator(PD, conIndex, COLSet, TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio);
            GUInputGenerator ReferenceEventGen = new GUInputGenerator(PD, conIndex, COLSet, TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio);

            int counter = 0;
            int total = 0;

            Console.WriteLine("State at: " + DateTime.Now.ToString("h:mm:ss tt"));
            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            for (int i = 1; i < 100; i += 1)
            {

                Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> NGFMguLoss;
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

                stopwatch = new Stopwatch();

                stopwatch.Start();
                double ReferencePayout = Reference.Execute(conIndex, type, RefguLoss);
                stopwatch.Stop();
                double ReferenceTime = Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;

                stopwatch.Reset();
                stopwatch.Start();
                double NGFMpayout = 0;
                //double NGFMpayout = NGFM.ExecuteFM(NGFMguLoss)[conIndex];
                NGFM.ProcessEvent(i, NGFMguLoss, true, 1, conIndex);                
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

        public static PerformanceMetrics TestReferenceSpeed2(GraphType type, PartitionData PD, int conIndex, COLCollection COLSet)
        {
            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            ReferencePrototype Reference = new ReferencePrototype(PD);
            NGFMPrototype NGFM = new NGFMPrototype();

            double MicroSecondTicks = Stopwatch.Frequency / 1000000.0;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            NGFM.Prepare(PD);  //NGFM result is cached, so create another object for each event
            stopwatch.Stop();
            double NGFMGraphTime = Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;
            double RefGraphTime = 0;

            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(PD);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);

            GUInputGenerator NGFMEventGen = new GUInputGenerator(PD, conIndex, COLSet, TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio);
            GUInputGenerator ReferenceEventGen = new GUInputGenerator(PD, conIndex, COLSet, TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio);

            int counter = 0;
            int total = 0;

            double ReferenceTimeTotal = 0;
            double NGFMTimeTotal = 0;
            int NumOfEvents = 100;


            Console.WriteLine("State at: " + DateTime.Now.ToString("h:mm:ss tt"));
            //NGFMPrototype NGFM = new NGFMPrototype(PD);
            for (int i = NumOfEvents + 1; i < 2*NumOfEvents; i += 1)
            {

                Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> NGFMguLoss;
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

                stopwatch = new Stopwatch();

                stopwatch.Start();
                double ReferencePayout = Reference.Execute(conIndex, type, RefguLoss);
                stopwatch.Stop();
                if (i > 5)
                    ReferenceTimeTotal += stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency; //Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;
                   
                else if (i == 1)
                    RefGraphTime = Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;


                stopwatch.Reset();
                stopwatch.Start();
                //double NGFMpayout = NGFM.ExecuteFM(NGFMguLoss)[conIndex];
                double NGFMpayout = 0;
                NGFM.ProcessEvent(i, NGFMguLoss, true, 1, conIndex);   
                stopwatch.Stop();
                if (i > 5)
                    NGFMTimeTotal += stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency;//Convert.ToDouble(stopwatch.ElapsedTicks) / MicroSecondTicks;

            }

            double execRatio = NGFMTimeTotal/ReferenceTimeTotal;
            double graphRatio = NGFMGraphTime / RefGraphTime;

            return new PerformanceMetrics(graphRatio, execRatio, ReferenceTimeTotal / (NumOfEvents - 5), NGFMTimeTotal / (NumOfEvents - 5));
        }

        //private static void UpdateWithLossSeries(PositionData positions, SortedDictionary<DateTime, double> overlapseries, SortedDictionary<DateTime, double> normalseries)
        //{
        //    HashSet<Graph> contracts = positions.posDict["POSITIONA_GROSS"];

        //    Graph OverlapContract = contracts.Where(contract => contract.ContractID == 11331).FirstOrDefault();
        //    Graph FixedGraph1Contract = contracts.Where(contract => contract.ContractID == 11324656).FirstOrDefault();

        //    OverlapContract.PayoutTimeSeries = GetLossTimeSeries(overlapseries);
        //    FixedGraph1Contract.PayoutTimeSeries = GetLossTimeSeries(normalseries);
        //}

        private static LossTimeSeries GetLossTimeSeries(SortedDictionary<DateTime, double> series)
        {
            LossTimeSeries output = new LossTimeSeries(1);
            foreach (KeyValuePair<DateTime, double> pair in series)
            {
                output.AddLoss((uint)pair.Key.DayOfYear, pair.Value);
            }

            return output;
        }

        private static void TestWindowGeneration(PartitionData pd, int conIndex)
        {
            //ExposureDataAdaptor expAdaptor = new ExposureDataAdaptor(pd, conIndex);
            PartitionDataAdpator PDataAdap = new PartitionDataAdpator(pd);
            ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(conIndex);
            FixedGraph1 graph = new FixedGraph1(expData);

            COLCollection COLSet = new COLCollection(new HashSet<string>() { "EQ", "WS" });
            GUInputGenerator ReferenceEventGen = new GUInputGenerator(pd, conIndex, COLSet, TimeStyle.ConstantTimeStamps, LossStyle.GroundUp);

            for (int i = 3; i < 6; i += 11)
            {
                //Generate GU Loss         
                Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> RefguLoss;

                if (ReferenceEventGen.GenerateRITELoss(i))
                {
                    RefguLoss = ReferenceEventGen.GULosses;
                }
                else
                    throw new InvalidOperationException("Cannot get ground-up loss for event: " + i);

                GUInputEngine guinputengine = new GUInputEngine(RefguLoss, graph);
                List<TimeWindow> timewindows = guinputengine.GenerateWindows(2);

                Console.WriteLine("Window Generation Tested");
            }
        }

        private void testPLTGen(PartitionData pd, int conIndex)
        {
            
            return;
        }


        private static void TestSpeed(string filepath, long conID = -1)
        {
            PartitionData pd = Deserialize(filepath);
            if (conID == -1)
                conID = pd.Exposures.First().ExposureID;

            int numB = 0;

            //for (int i = 0; i < 368; i++)
            //{
            //    ContractSubjectExposureOfRiteSchedule RITEs = pd.Exposures[0].ContractSubjectExposures[0] as ContractSubjectExposureOfRiteSchedule;
            //    numB += RITEs.RITECollectionExposure.RITExposures.ToArray()[i].CommonCharacteristics.NumBuildings;
            //    UpdateParDataWithNumBldgs(pd, 0, 1, i);
            //}

            Console.WriteLine("num of b = " + numB);


            COLCollection COLSet = new COLCollection(new HashSet<string>() { "EQ", "WS", "WI" });
            TestReferenceSpeed(GraphType.Auto, pd, (int)conID, COLSet);
        }

        private static void TestContract(string filepath, long conID = -1)
        {
            PartitionData pd = Deserialize(filepath);
            if (conID == -1)         
                conID = pd.Exposures.First().ExposureID;
            
            COLCollection COLSet = new COLCollection(new HashSet<string>() {"EQ", "WS", "WI"});
            TestReference(GraphType.Auto, pd, conID, COLSet);

        }
    }

    //NGFMguLoss = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>()
    //            {
    //                {"EQ", new Dictionary<int,Dictionary<long,Tuple<uint,List<float>>>>()
    //                         {
    //                            {1, new Dictionary<long,Tuple<uint,List<float>>>()
    //                                {
    //                                    {3253, new Tuple<uint,List<float>>(5, new List<float>(){.1f})},
    //                                    {3254, new Tuple<uint,List<float>>(15, new List<float>(){.2f})},
    //                                    {3255, new Tuple<uint,List<float>>(23, new List<float>(){.3f})},
    //                                    {3256, new Tuple<uint,List<float>>(16, new List<float>(){.4f})},
    //                                    {3257, new Tuple<uint,List<float>>(9, new List<float>(){.5f})},
    //                                    {3258, new Tuple<uint,List<float>>(1, new List<float>(){.6f})},
    //                                    {3259, new Tuple<uint,List<float>>(4, new List<float>(){.7f})},
    //                                    {3260, new Tuple<uint,List<float>>(5, new List<float>(){.8f})},
    //                                    {3261, new Tuple<uint,List<float>>(14, new List<float>(){.9f})},
    //                                    {19419518, new Tuple<uint,List<float>>(14, new List<float>(){.1f})},
    //                                    {19419519, new Tuple<uint,List<float>>(7, new List<float>(){.2f})},
    //                                    {19419520, new Tuple<uint,List<float>>(12, new List<float>(){.3f})},
    //                                    {19419521, new Tuple<uint,List<float>>(9, new List<float>(){.4f})},
    //                                }

    //                            }
    //                        }
    //                 } 
    //             };

    //NGFMguLoss = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>>()
    //            {
    //                {"EQ", new Dictionary<int,Dictionary<long,Tuple<uint,List<float>>>>()
    //                         {
    //                            {1, new Dictionary<long,Tuple<uint,List<float>>>()
    //                                {
    //                                    {3253, new Tuple<uint,List<float>>(5, new List<float>(){.02f})},
    //                                    {3254, new Tuple<uint,List<float>>(15, new List<float>(){.01f})},
    //                                    {3255, new Tuple<uint,List<float>>(23, new List<float>(){.03f})},
    //                                    {3256, new Tuple<uint,List<float>>(16, new List<float>(){.01f})},
    //                                    {3257, new Tuple<uint,List<float>>(9, new List<float>(){.05f})},
    //                                    {3258, new Tuple<uint,List<float>>(1, new List<float>(){.05f})},
    //                                    {3259, new Tuple<uint,List<float>>(4, new List<float>(){.06f})},
    //                                    {3260, new Tuple<uint,List<float>>(5, new List<float>(){.01f})},
    //                                    {3261, new Tuple<uint,List<float>>(14, new List<float>(){.05f})},
    //                                    {19419518, new Tuple<uint,List<float>>(14, new List<float>(){.1f})},
    //                                    {19419519, new Tuple<uint,List<float>>(7, new List<float>(){0})},
    //                                    {19419520, new Tuple<uint,List<float>>(12, new List<float>(){0})},
    //                                    {19419521, new Tuple<uint,List<float>>(9, new List<float>(){.4f})},
    //                                }

    //                            }
    //                        }
    //                 } 
    //             };

    public class PerformanceMetrics
    {
        public double NGFM2RefRatioExecute { get; set; }
        public double NGFM2RefRatioGraph { get; set; }
        public double ReferenceTime { get; set; }
        public double NGFMTime { get; set; }

        public PerformanceMetrics(double graphRatio, double execRatio, double refTime, double ngfmTime)
        {
            NGFM2RefRatioExecute = execRatio;
            NGFM2RefRatioGraph = graphRatio;
            ReferenceTime = refTime;
            NGFMTime = ngfmTime;
        }
    }

}



