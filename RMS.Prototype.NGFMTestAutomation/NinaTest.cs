using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RMS.Prototype.NGFM;
using Rms.Analytics.DataService.Zip;
using RMS.ContractObjectModel;
using RMS.ContractGraphModel;
using PrototypeContract = RMS.ContractObjectModel.Contract;
using System.Net;
using ProtoBuf;
using System.Windows.Forms;
using Rms.DataServices.DataObjects;
using System.Data.SqlClient;
using System.Xml;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using System.Threading;
using WorkFlowAutomationForProtobuffs;
using RMS.NGFMAutomation.LoopEvents;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using NGFMReference;
using System.Diagnostics;
using System.Collections.Concurrent;
using NGFM.Reference.MatrixHDFM;

namespace RMS.NGFMAutomation.LoopEvents
{

    public class NGFMAutomation
    {
        public string xlfilepath { get; set; }
        public string outputfilepath { get; set; }
        public TextBox outputTextbox { get; set; }
        public OutputObject outputObject { get; set; }

        //1. Generate EDS Extract for all test cases
        public void GenerateAllEDSExtract(bool RunAllCases)
        {
            MessageLog Extractlog = new MessageLog();
            ExcelInputEngine ExcelInput = new ExcelInputEngine(xlfilepath);
            List<TestCase> TestCasesList = ExcelInput.getxlinfo(Extractlog, RunAllCases);

            for (int i = 0; i < TestCasesList.Count(); i++)
            {
                if (TestCasesList[i].ExtractFolderPath == "")
                {
                    //1. Create EDS Generator Adaptor
                    DownloadedEDSInfo extractsinfo = new DownloadedEDSInfo();
                    EDSGeneratorAdaptor edsGenerator = new EDSGeneratorAdaptor(TestCasesList[i], extractsinfo);

                    //2. Convert EDM to Extract and update the test case object with folder path
                    string ExtractFolderPath = edsGenerator.ConvertEDMtoExtract();

                    string ReturnMessage = edsGenerator.GetErrorMessage();

                    if (ReturnMessage.StartsWith("EDS Extracts copied"))
                    {

                        //3. Update Excel Input xl Package with Extract Folder Path
                        ExcelInput.UpdateInputxlPackage(i, ExtractFolderPath);
                        outputTextbox.AppendText("Completed EDS Generation for testcase" + i + "\n");
                    }
                    else
                    {
                        ExcelInput.UpdatXlwithErrorMessge(i, ReturnMessage);
                        ExcelInput.xlpackage.Save();
                        ExcelInput.LoadExcel(xlfilepath);
                        outputTextbox.AppendText("Stopped EDS Generation at testcase" + i + " _" + ReturnMessage + "\n");

                        //ErrorMessage = "Stopped EDS Generation at testcase" + i + " _" + ReturnMessage;
                        //textbox2.Text = "EDS Generator stopped working at testcase" + i + " _" + ReturnMessage;
                    }
                }
            }
            ExcelInput.xlpackage.Save();
        }

        //2. Run Automation to get comparison between FM & Baseline
        public void Run(bool RunAllCases)
        {

            MessageLog log = new MessageLog();

            ExcelInputEngine ExcelInput = new ExcelInputEngine(xlfilepath);

            List<TestCase> TestCasesList = ExcelInput.getxlinfo(log, RunAllCases);

            ExcelOutPutEngine outputEngine = new ExcelOutPutEngine(outputObject);
            outputEngine.LoadExcel();
            outputEngine.CreateSQLTable(log);
            //Delete later:
            //int[] NumOfBldgInfo = new int[] { 5, 10, 20, 30, 40, 50, 100, 200, 300, 400, 500, 1000, 2000, 3000, 4000, 5000 };
            //int k = 0;
            foreach (TestCase testcase in TestCasesList)
            {
                Stopwatch FMGraphSW = new Stopwatch();
                MessageLog TestCaseLog = new MessageLog();
                outputObject.performancestats = true;

                //1. Generate Partition Data
                PartitionDataEngine pdatEngine = new PartitionDataEngine(testcase);
                PartitionData pdat = new PartitionData();
                if (pdatEngine.DeserializePartitionData(TestCaseLog, out pdat) == false)
                {
                    outputObject.contractstats = false;
                    outputObject.performancestats = false;
                    int outputrow = 1 + outputEngine.xlpackage.Workbook.Worksheets[1].Dimension.End.Row;
                    outputEngine.OutputToExcel(testcase, outputrow, TestCaseLog);
                    continue;
                }

                #region Modify number of bldg
             
                //pdatEngine.ModifyNumOfBldgs(pdat, NumOfBldgInfo[k]);
                //k++;
                //pdatEngine.ScaleTIV(pdat);
                //pdatEngine.SerializeCreateNew(pdat, @"D:\Nina_Automation_Testing\MultiBldg_Performance_Test\NTA_EDS_Extract\");
                //continue;


                ////Modify Number of building to multi-building
                //pdatEngine.RandomModifyNumOfBldgs(pdat);

                ////test serializaiton:
                //pdatEngine.SerializeCreateNew(pdat, @"D:\Nina_Automation_Testing\MultiBldg_MTH_Extracts\");
                //continue;

                //Delete Later: for modifying number of buildings
                //int NumberOfBldgs = 100000;
                //pdatEngine.ModifyNumOfBldgs(pdat, NumberOfBldgs);
                //pdatEngine.Serialize(pdat, @"D:\Nina_Automation_Testing\MultiBldg_Performance_Test\NTA_EDS_Extract\PerRisk_RMS_EDM_FinMod_Automation_RL131_72514_PortID_925_1000002173_1\4483\2142615\635447577274700000\rites_batch0" + "_" + NumberOfBldgs + ".dat");

                //Delete Later: To change TIV (Original TIV * NumOfBldgs)
                
                #endregion

                #region Split and Create New Contracts
                //pdatEngine.SplitExtract(pdat, 100);
                //continue;
                #endregion

                //2.Create NGFM Object
                NGFMPrototype ngfmPrototype = new NGFMPrototype(1);
                FMGraphSW.Start();
                ngfmPrototype.Prepare(pdat);
                FMGraphSW.Stop();
                outputObject.PerformanceOutput = new PerformanceOutPut();
                outputObject.PerformanceOutput.FMGraphBuildingTime = FMGraphSW.Elapsed.TotalMilliseconds;

                //3. Create Baseline Object
                IBaseline iBaseline;
                if (testcase.TestCaseType == "RL")
                {
                    RLBaseline rlBaseline = new RLBaseline(pdat, testcase, outputObject, outputEngine);
                    iBaseline = rlBaseline;
                }
                else if (testcase.TestCaseType == "REF")
                {
                    SpecialFeatureBaseline sfBaseline = new SpecialFeatureBaseline(pdat, testcase,outputObject);
                    iBaseline = sfBaseline;
                }
                else
                    throw new NotSupportedException("Does not currently support Test Case type: " + testcase.TestCaseType);

                //4. Prepare Testcase: In RL :get AcctToContract Mapping & LoccvgToRite Mapping
                                     //In Sunny's : get AcctContract Mapping & Convert the GraphType to GraphType Object
                if (iBaseline.PrepareTestCase(TestCaseLog) == false)
                {
                    outputObject.contractstats = false;
                    int outputrow = 1 + outputEngine.xlpackage.Workbook.Worksheets[1].Dimension.End.Row;
                    outputEngine.OutputToExcel(testcase, outputrow, TestCaseLog);
                    continue;
                }

                //5. Execute TestCase & save per event results to DataTable
                TestCaseExecutor testcaseExecutor = new TestCaseExecutor(iBaseline, ngfmPrototype, outputEngine, outputObject);
                
                testcaseExecutor.Run(testcase);
            }

            //7. Save Results to Excel Output Sheet
            string outputpath = outputfilepath + @"\NGFM_RL_Results_Comparison_" + DateTime.Now.ToString("MM-dd-yyyy_HH-mm") + ".xlsx";
            FileStream stream = File.Create(outputpath);
            outputEngine.xlpackage.SaveAs(stream);
            stream.Close();

        }

        public NGFMAutomation(string _inputfilepath, string _outputfilepath, OutputObject _outputobject)
        {
            xlfilepath = _inputfilepath;
            outputfilepath = _outputfilepath;
            outputObject = _outputobject;
        }

        public NGFMAutomation(string _inputfilepath, TextBox _ExtractProgress)
        {
            xlfilepath = _inputfilepath;
            outputTextbox = _ExtractProgress;
        }
    }

    public class TestCaseExecutor
    {
        public Dictionary<long, int> ContrAcctMap { get; set; }
        public IBaseline iBaseline { get; set; }
        public NGFMPrototype ngfmPrototype { get; set; }
        public ExcelOutPutEngine outputEngine { get; set; }
        public OutputObject outputobject { get; set; }
        public PartitionData pdat { get; set; }

        
        public void Run(TestCase testcase)
        {
            
            int outputrow = 0;
            ContrAcctMap = iBaseline.ContrAcctMap;

            //Loop through each Contract/account
            foreach (KeyValuePair<long, int> ContrAcctKvp in ContrAcctMap)
            {

                //StopWatch to measure FM vs.Reference Time
                Stopwatch FMExeSW = new Stopwatch();
                Stopwatch MatrixExeSW = new Stopwatch();

                outputEngine.CreateDataTable();
                MessageLog Accountlog = new MessageLog();

                // For one account all events: Get Event list table, RL GR table, LoccvgDr table
                if (iBaseline.PrepareContract(ContrAcctKvp.Key, Accountlog) == false)
                {
                    outputEngine.PrepareOutputObject(ContrAcctKvp);
                    outputrow = 1 + outputEngine.xlpackage.Workbook.Worksheets[1].Dimension.End.Row;
                    outputEngine.OutputToExcel(testcase, outputrow, Accountlog);
                    continue;
                }
                int NumOfEvent = iBaseline.EventListTable.AsEnumerable().Count();
                foreach (DataRow EventInfo in iBaseline.EventListTable.AsEnumerable())
                {
                    //1. Get gulosses (input for executing FM)
                    int Event = Convert.ToInt32(EventInfo["EventID"]);
                    MessageLog Eventlog = new MessageLog();
                    Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double,uint, List<float>>>>> gulosses = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();
                    List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodGUlosses = new List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>>();

                    if (testcase.InputType == GUInputType.Period)
                    {
                        if (iBaseline.GetPeriodGUlosses(ContrAcctKvp.Key, Event, Eventlog, out PeriodGUlosses) == false)
                        {
                            outputEngine.PrepareOutputObject(ContrAcctKvp);
                            outputobject.FMContractPayout = -999;
                            outputEngine.OutputToDataTable(EventInfo, testcase, Eventlog);
                            continue;
                        }
                    }
                    else
                    {
                        if (iBaseline.GetGUlosses(ContrAcctKvp.Key, Event, Eventlog, out gulosses) == false)
                        {
                            outputEngine.PrepareOutputObject(ContrAcctKvp);
                            outputobject.FMContractPayout = -999;
                            outputEngine.OutputToDataTable(EventInfo, testcase, Eventlog);
                            continue;
                        }
                    }


                    //2. Get NGFM Contract Payout & save it to OutputEngine Object

                    FMContractExecutor fmContractExecutor = new FMContractExecutor(ngfmPrototype);
                    FMExeSW.Start();
                    fmContractExecutor.GetContractPayout(Event, ContrAcctKvp.Key, gulosses, PeriodGUlosses,Eventlog, outputobject, testcase.InputType);
                    FMExeSW.Stop();

                    //3. Get Baseline Payout Per Event & save it to OutputEngine Object
                    if (iBaseline.GetBaselinePayout(Event, Eventlog, MatrixExeSW) == false)
                    {
                        outputobject.baselinePayout = -999;
                    }

                    //4. Prepare output object (get account & contract id )
                    
                    outputEngine.PrepareOutputObject(ContrAcctKvp);
                    outputobject.PerformanceOutput.FMExecutionTime = FMExeSW.Elapsed.TotalMilliseconds/NumOfEvent;
                    outputobject.PerformanceOutput.MatrixExecutionTime = MatrixExeSW.Elapsed.TotalMilliseconds/NumOfEvent;

                    //5. Save Results to DataTable
                    outputEngine.OutputToDataTable(EventInfo, testcase, Eventlog);
                }
                //6. Bulk Copy DataTable into SQL Database
                if (outputEngine.CopyDataTableToSQL(Accountlog) == false)
                {
                    outputobject.contractstats = false;
                    outputrow = 1 + outputEngine.xlpackage.Workbook.Worksheets[1].Dimension.End.Row;
                    outputEngine.OutputToExcel(testcase, outputrow, Accountlog);
                    continue;
                }

                //7. Get Account level AAL and Mismatched event counts, etc.
                outputEngine.PrepareXloutputObject(testcase, Accountlog);

                //8. Save Results to Excel Output Package
                outputrow = 1 + outputEngine.xlpackage.Workbook.Worksheets[1].Dimension.End.Row;
                outputEngine.OutputToExcel(testcase, outputrow, Accountlog);
            }

        }


        public TestCaseExecutor(IBaseline _iBaseline, NGFMPrototype _ngfmPrototype, ExcelOutPutEngine _outputengine, OutputObject _outputobject)
        {
            iBaseline = _iBaseline;
            ngfmPrototype = _ngfmPrototype;
            outputEngine = _outputengine;
            outputobject = _outputobject;
        }
    }

    public class FMContractExecutor
    {
        public NGFMPrototype ngfmPrototype { get; set; }

        public bool GetContractPayout(int EventID, long ContractId, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> gulosses,
                                      List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodGulosses, MessageLog Eventlog, OutputObject outputobject, GUInputType InputType)
        {
            bool Status = true;
            Dictionary<long, ResultPosition> contractpayoutDict = new Dictionary<long, ResultPosition>();
            Dictionary<long, List<ResultPosition>> PeriodPayoutDict = new Dictionary<long, List<ResultPosition>>();

            try
            {
                if (InputType == GUInputType.Period)
                {
                    //Call FM API to Execute the contract
                    PeriodPayoutDict = ngfmPrototype.ProcessPeriod(EventID, PeriodGulosses,true,1,ContractId);

                    if (PeriodPayoutDict.Count == 0)
                    {
                        Status = false;
                        Eventlog.Log("Error while executing NGFM : No contract payout produced by NGFM");
                        outputobject.FMContractPayout = -999;
                    }

                    else
                    {
                        foreach (KeyValuePair<long, List<ResultPosition>> payoutkvp in PeriodPayoutDict)
                        {
                            if (double.IsNaN(payoutkvp.Value[0].PayOut) == true)
                            {
                                Status = false;
                                Eventlog.Log("Error while executing NGFM : Contract Payout is NaN");
                                outputobject.FMContractPayout = -999;
                            }
                            else
                            {
                                double FMpayout = PeriodPayoutDict[ContractId].AsEnumerable().Select(x => x.PayOut).Sum();
                                outputobject.FMContractPayout = FMpayout;
                            }
                        }
                    }
                }
                else
                {
                    contractpayoutDict = ngfmPrototype.ProcessEvent(EventID, gulosses, true, 1, ContractId);

                    if (contractpayoutDict.Count == 0)
                    {
                        Status = false;
                        Eventlog.Log("Error while executing NGFM : No contract payout produced by NGFM");
                        outputobject.FMContractPayout = -999;
                    }
                    else
                    {
                        foreach (KeyValuePair<long, ResultPosition> payoutkvp in contractpayoutDict)
                        {
                            if (double.IsNaN(payoutkvp.Value.PayOut) == true)
                            {
                                Status = false;
                                Eventlog.Log("Error while executing NGFM : Contract Payout is NaN");
                                outputobject.FMContractPayout = -999;
                            }
                            else
                            {
                                outputobject.FMContractPayout = contractpayoutDict[ContractId].PayOut;
                            }
                        }
                    }
   
                }
                

            }
            catch (Exception e)
            {
                Status = false;
                Eventlog.Log("Error while executing NGFM :" + e.Message + ";");
                outputobject.FMContractPayout = -999;
            }

            return Status;

        }
        public FMContractExecutor(NGFMPrototype _ngfmPrototype)
        {
            ngfmPrototype = _ngfmPrototype;
        }
    }

    public class ExcelInputEngine
    {
        public string xlfilepath { get; set; }
        public ExcelPackage xlpackage { get; set; }
        public ExcelWorksheet Inputsheet { get; set; }

        public void LoadExcel(string xlfilepath)
        {
            FileInfo file = new FileInfo(xlfilepath);
            xlpackage = new ExcelPackage(file);
        }

        public List<TestCase> getxlinfo(MessageLog log, bool RunAllCases)
        {
            LoadExcel(xlfilepath);
            Inputsheet = xlpackage.Workbook.Worksheets[1];
            List<TestCase> TestCaseList = new List<TestCase>();

            for (int i = 1; i < xlpackage.Workbook.Worksheets[1].Dimension.End.Row; i++)
            {
                int portindex = i + 1;
                TestCase xlinfo = new TestCase();
                xlinfo.RDM = new RDMInfo();
                xlinfo.EDM = new EDMInfo();
                xlinfo.Description = new Description();
                string tempSubPerilsString;
                try
                {
                    xlinfo.RunStatus = (NTARunStatus)Enum.Parse(typeof(NTARunStatus), (Inputsheet.Cells[portindex, 25].Value ?? NTARunStatus.NotRun).ToString());
                }
                catch (ArgumentException)
                {
                    throw (new ArgumentException("Error: The NTARunStatus type is not handled in input excel, test case:" + xlinfo.TestCaseID));
                }


                if (xlinfo.RunStatus == NTARunStatus.Run|| RunAllCases==true)
                {
                    tempSubPerilsString = (Inputsheet.Cells[portindex, 3].Value ?? String.Empty).ToString();
                    xlinfo.Description.SubPeril = tempSubPerilsString.Split(',');
                    xlinfo.Description.SubRegion = (Inputsheet.Cells[portindex, 4].Value ?? String.Empty).ToString();
                    xlinfo.Description.RapProfile = (Inputsheet.Cells[portindex, 5].Value ?? String.Empty).ToString();
                    xlinfo.Description.Feature = (Inputsheet.Cells[portindex, 6].Value ?? String.Empty).ToString();
                    xlinfo.Description.SubFeature = (Inputsheet.Cells[portindex, 7].Value ?? String.Empty).ToString();
                    xlinfo.ExtractFolderPath = (Inputsheet.Cells[portindex, 21].Value ?? String.Empty).ToString();
                    if ((Inputsheet.Cells[portindex, 26].Value ?? String.Empty).ToString() == "Yes")
                    {
                        xlinfo.EDM.MultiBldg = true;
                    }
                    else
                    {
                        xlinfo.EDM.MultiBldg = false;
                    }
                    if ((Inputsheet.Cells[portindex, 24].Value ?? String.Empty).ToString() == "Period")
                    {
                        xlinfo.InputType = GUInputType.Period;
                    }
                    else
                    {
                        xlinfo.InputType = GUInputType.Event;
                    }
                    if (Inputsheet.Cells[portindex, 1].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing TestCaseID in excel input sheet row " + portindex));
                    }
                    else
                    {
                        xlinfo.TestCaseID = Convert.ToInt32(Inputsheet.Cells[portindex, 1].Value);
                    }
                    xlinfo.Description.ExposureDetails = (Inputsheet.Cells[portindex, 8].Value ?? String.Empty).ToString();
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 9].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing EDM Database Name in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.EDM.DBName = (Inputsheet.Cells[portindex, 9].Value ?? String.Empty).ToString();;
                    }
                    if (Inputsheet.Cells[portindex, 10].Value == "22" && Inputsheet.Cells[portindex, 10].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing Portinfoid in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.EDM.Portinfoid = (Inputsheet.Cells[portindex, 10].Value?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 11].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing PortNum in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.EDM.PortNum = (Inputsheet.Cells[portindex, 11].Value?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 12].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing PortName in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.EDM.PortName = (Inputsheet.Cells[portindex, 12].Value?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 13].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing EDM SQL Server in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.EDM.SQLServer = (Inputsheet.Cells[portindex, 13].Value?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 14].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing EDM SQLlogin in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.EDM.SQLlogin = (Inputsheet.Cells[portindex, 14].Value ?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value =="RL" && Inputsheet.Cells[portindex, 15].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing EDM SQL Password in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.EDM.SQLpass = (Inputsheet.Cells[portindex, 15].Value?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 16].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing RDM Name in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.RDM.DBName = (Inputsheet.Cells[portindex, 16].Value ?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 17].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing RDM SQL Server in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.RDM.SQLserver = (Inputsheet.Cells[portindex, 17].Value ?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 18].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing RDM SQL Login in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.RDM.SQLlogin = (Inputsheet.Cells[portindex, 18].Value ?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 19].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing RDM SQL Password in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.RDM.SQLpass = (Inputsheet.Cells[portindex, 19].Value ?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "RL" && Inputsheet.Cells[portindex, 20].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing RDM Anlsid in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.RDM.Anlsid = (Inputsheet.Cells[portindex, 20].Value ?? String.Empty).ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing TestCaseType in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {

                        xlinfo.TestCaseType = Inputsheet.Cells[portindex, 22].Value.ToString();
                    }
                    if (Inputsheet.Cells[portindex, 22].Value == "REF" && Inputsheet.Cells[portindex, 23].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing GraphType in excel input sheet test case: " + xlinfo.TestCaseID));
                    }
                    else
                    {
                        xlinfo.GraphType = (Inputsheet.Cells[portindex, 23].Value ?? String.Empty).ToString();
                    }
                    //Perils that NGFM support now
                    List<string> SupportedPeril = new List<string>();
                    SupportedPeril.Add("EQ");
                    SupportedPeril.Add("WS");
                    SupportedPeril.Add("FL");
                    SupportedPeril.Add("CS");
                    SupportedPeril.Add("WT");
                    SupportedPeril.Add("TY");
                    SupportedPeril.Add("HU");

                    if (Inputsheet.Cells[portindex, 2].Value == null)
                    {
                        throw (new InvalidDataException("Error: Missing Peril in excel input sheet row " + portindex));
                    }
                    else if (SupportedPeril.Contains(Inputsheet.Cells[portindex, 2].Value.ToString().Substring(2, 2)))
                    {
                        if (Inputsheet.Cells[portindex, 2].Value.ToString().Substring(2, 2) == "TY" || Inputsheet.Cells[portindex, 2].Value.ToString().Substring(2, 2) == "HU")
                        {
                            xlinfo.Description.Peril = "WS";
                        }
                        else if (Inputsheet.Cells[portindex, 2].Value.ToString().Substring(2, 2) == "WT" || Inputsheet.Cells[portindex, 2].Value.ToString().Substring(2, 2) == "CS")
                        {
                            xlinfo.Description.Peril = "CSWT";
                        }
                        else
                        {
                            xlinfo.Description.Peril = Inputsheet.Cells[portindex, 2].Value.ToString().Substring(2, 2);
                        }
                    }
                    else
                    {
                        throw (new InvalidDataException("Error: Peril not supported by NGFM in excel input sheet row " + portindex + "Please select Peril in: EQ, WS, FL, CS, WT, TY"));
                    }

                    TestCaseList.Add(xlinfo);
                }
            }
            return TestCaseList;
        }

        public void UpdateInputxlPackage(int caseIndex, string extractPath)
        {
            Inputsheet = xlpackage.Workbook.Worksheets[1];
            Inputsheet.Cells[caseIndex + 2, 21].Value = extractPath;
        }

        public void UpdatXlwithErrorMessge(int caseIndex, string ErrorMessage)
        {
            Inputsheet.Cells[caseIndex + 2, 21].Value = ErrorMessage;
            Inputsheet.Cells[caseIndex + 2, 21].Style.Fill.PatternType = ExcelFillStyle.Solid;
            Inputsheet.Cells[caseIndex + 2, 21].Style.Fill.BackgroundColor.SetColor(Color.Red);
        }

        public ExcelInputEngine(string inputfilepath)
        {
            xlfilepath = inputfilepath;
        }

    }

    public class TestCase
    {
        public RDMInfo RDM { get; set; }
        public EDMInfo EDM { get; set; }
        public Description Description { get; set; }
        public NTARunStatus RunStatus { get; set; }
        public GUInputType InputType { get; set; }

        public string ExtractFolderPath { get; set; }

        public string TestCaseType { get; set; }

        public int TestCaseID { get; set; }

        public string GraphType { get; set; }

    }

    public class RDMInfo
    {
        public string DBName { get; set; }
        public string SQLserver { get; set; }
        public string SQLlogin { get; set; }
        public string SQLpass { get; set; }
        public string Anlsid { get; set; }
    }

    public class EDMInfo
    {
        public string DBName { get; set; }
        public string Portinfoid { get; set; }
        public string PortName { get; set; }
        public string PortNum { get; set; }
        public string SQLServer { get; set; }
        public string SQLlogin { get; set; }
        public string SQLpass { get; set; }
        public bool MultiBldg { get; set; }
    }

    public class Description
    {
        public string Peril { get; set; }
        public string[] SubPeril { get; set; }
        public string SubRegion { get; set; }
        public string RapProfile { get; set; }
        public string Feature { get; set; }
        public string SubFeature { get; set; }
        public string ExposureDetails { get; set; }

    }

    public enum NTARunStatus
    {
        Run,
        NotRun
    }

    public enum GUInputType
    {
        Period,
        Event
    }

    public class OutputObject
    {
        public double baselinePayout { get; set; }
        public double FMContractPayout { get; set; }
        public int AccountId { get; set; }
        public long ContractId { get; set; }
        public string OutPutServerName { get; set; }
        public string OutPutUserName { get; set; }
        public string OutPutPass { get; set; }
        public string OutPutDataBase { get; set; }
        public double BaselineAcctAAL { get; set; }
        public double FMContractAAL { get; set; }
        //Number of mis matched events per contract/account
        public long MisMatchEventCount { get; set; }
        public long TotalEventCount { get; set; }
        public long FailedEventCount { get; set; }
        public bool contractstats { get; set; }
        public bool performancestats { get; set; }

        //Performance Output information
        public PerformanceOutPut PerformanceOutput { get; set; }
    }

    public class PerformanceOutPut
    {
        public double FMGraphBuildingTime { get; set; }
        public double MatrixGraphBuildingTime { get; set; }
        public double FMExecutionTime { get; set; }
        public double MatrixExecutionTime { get; set; }
        public double GraphBuildingRatio { get; set; }
        public double ExecutionRatio { get; set; }
    }

    public class ExcelOutPutEngine
    {
        public ExcelPackage xlpackage { get; set; }
        public ExcelWorksheet OutputSheet { get; set; }
        public OutputObject outputobject { get; set; }
        public string OPSQLtablename { get; set; }
        public DataTable outputDataTable { get; set; }
        public ExcelWorksheet PerformanceSheet { get; set; }

        public void LoadExcel()
        {
            FileInfo file = new FileInfo(@".\Template\NTA Results Output Format.xlsx");
            xlpackage = new ExcelPackage(file);
        }

        public void PrepareOutputObject(KeyValuePair<long, int> ContrAcctKvp)
        {
            int AccountID = ContrAcctKvp.Value;
            long ContractID = ContrAcctKvp.Key;
            outputobject.AccountId = AccountID;
            outputobject.ContractId = ContractID;
        }

        public void OutputToExcel(TestCase testcase, int OutputRow, MessageLog log)
        {
            OutputSheet = xlpackage.Workbook.Worksheets[1];

            OutputSheet.Cells[OutputRow, 1].Value = testcase.TestCaseID;
            OutputSheet.Cells[OutputRow, 4].Value = testcase.EDM.PortName;
            OutputSheet.Cells[OutputRow, 5].Value = testcase.EDM.Portinfoid;
            OutputSheet.Cells[OutputRow, 6].Value = testcase.EDM.DBName;
            OutputSheet.Cells[OutputRow, 7].Value = testcase.EDM.SQLServer;
            OutputSheet.Cells[OutputRow, 8].Value = testcase.EDM.SQLlogin;
            OutputSheet.Cells[OutputRow, 9].Value = testcase.EDM.SQLpass;
            OutputSheet.Cells[OutputRow, 10].Value = testcase.RDM.DBName;
            OutputSheet.Cells[OutputRow, 11].Value = testcase.RDM.SQLserver;
            OutputSheet.Cells[OutputRow, 12].Value = testcase.RDM.SQLlogin;
            OutputSheet.Cells[OutputRow, 13].Value = testcase.RDM.SQLpass;
            OutputSheet.Cells[OutputRow, 14].Value = testcase.RDM.Anlsid;
            OutputSheet.Cells[OutputRow, 15].Value = testcase.ExtractFolderPath;
            OutputSheet.Cells[OutputRow, 16].Value = testcase.Description.Peril;

            //Handle outputing subperil. Subperil is stored in a string array. We need to output for ex: (WI,WA)
            string[] subperils = testcase.Description.SubPeril;
            if (subperils.Length == 1)
            {
                OutputSheet.Cells[OutputRow, 17].Value = subperils[0];
            }
            else
            {
                string outputsubperils =""; 
                for (int i = 0; i < subperils.Length; i++)
                {
                    if (i != subperils.Length - 1)
                        outputsubperils += subperils[i] + ",";
                    else
                        outputsubperils += subperils[i];                   
                }
                OutputSheet.Cells[OutputRow, 17].Value = outputsubperils;
            }

            OutputSheet.Cells[OutputRow, 18].Value = testcase.Description.SubRegion;
            OutputSheet.Cells[OutputRow, 19].Value = testcase.Description.RapProfile;
            OutputSheet.Cells[OutputRow, 20].Value = testcase.Description.Feature;
            OutputSheet.Cells[OutputRow, 21].Value = testcase.Description.SubFeature;
            OutputSheet.Cells[OutputRow, 22].Value = testcase.Description.ExposureDetails;
            if (outputobject.contractstats == true)
            {
                OutputSheet.Cells[OutputRow, 2].Value = outputobject.AccountId;
                OutputSheet.Cells[OutputRow, 3].Value = outputobject.ContractId;
                OutputSheet.Cells[OutputRow, 23].Value = outputobject.BaselineAcctAAL;
                OutputSheet.Cells[OutputRow, 24].Value = outputobject.FMContractAAL;
                if (outputobject.BaselineAcctAAL == 0 && outputobject.FMContractAAL == 0)
                {
                    OutputSheet.Cells[OutputRow, 25].Value = 0;
                }
                else if (outputobject.BaselineAcctAAL == 0 && outputobject.FMContractAAL != 0)
                {
                    OutputSheet.Cells[OutputRow, 25].FormulaR1C1 = "(RC[-1]-RC[-2])/RC[-1]";
                }
                else
                {
                    OutputSheet.Cells[OutputRow, 25].FormulaR1C1 = "(RC[-1]-RC[-2])/RC[-2]";
                }
                OutputSheet.Cells[OutputRow, 26].FormulaR1C1 = "RC[-2] - RC[-3]";
                OutputSheet.Cells[OutputRow, 27].Value = outputobject.MisMatchEventCount;
                OutputSheet.Cells[OutputRow, 28].Value = outputobject.TotalEventCount;
                OutputSheet.Cells[OutputRow, 29].Value = outputobject.FailedEventCount;
                if (outputobject.MisMatchEventCount == 0 && outputobject.FailedEventCount == 0 && outputobject.BaselineAcctAAL != -999)
                {
                    OutputSheet.Cells[OutputRow, 30].Value = "Pass";
                }
                else
                {
                    OutputSheet.Cells[OutputRow, 30].Value = "Fail";
                }
            }
            else
            {
                OutputSheet.Cells[OutputRow, 2].Value = outputobject.AccountId;
                OutputSheet.Cells[OutputRow, 3].Value = outputobject.ContractId;
                OutputSheet.Cells[OutputRow, 30].Value = "Fail";
            }

            OutputSheet.Cells[OutputRow, 31].Value = log.MsgLogSb.ToString();

            //Add Reference vs.FM performance comparison
            PerformanceSheet = xlpackage.Workbook.Worksheets[2];

            if (outputobject.performancestats == true)
            {
                PerformanceSheet.Cells[OutputRow + 2, 1].Value = testcase.TestCaseID;
                PerformanceSheet.Cells[OutputRow + 2, 2].Value = outputobject.ContractId;
                PerformanceSheet.Cells[OutputRow + 2, 3].Value = outputobject.PerformanceOutput.FMGraphBuildingTime;
                PerformanceSheet.Cells[OutputRow + 2, 4].Value = outputobject.PerformanceOutput.MatrixGraphBuildingTime;
                PerformanceSheet.Cells[OutputRow + 2, 5].Value = Math.Round(outputobject.PerformanceOutput.GraphBuildingRatio, 2);
                PerformanceSheet.Cells[OutputRow + 2, 6].Value = outputobject.PerformanceOutput.FMExecutionTime;
                PerformanceSheet.Cells[OutputRow + 2, 7].Value = outputobject.PerformanceOutput.MatrixExecutionTime;
                PerformanceSheet.Cells[OutputRow + 2, 8].Value = Math.Round(outputobject.PerformanceOutput.ExecutionRatio, 2);
            }
            else
            {
                PerformanceSheet.Cells[OutputRow + 2, 1].Value = testcase.TestCaseID;
                PerformanceSheet.Cells[OutputRow + 2, 2].Value = outputobject.ContractId;
            }

        }

        public DataTable CreateDataTable()
        {
            outputDataTable = new DataTable();

            DataColumn column9 = new DataColumn("TestCaseID");
            outputDataTable.Columns.Add(column9);
            DataColumn column1 = new DataColumn("ContractID");
            outputDataTable.Columns.Add(column1);
            DataColumn column2 = new DataColumn("AccountID");
            outputDataTable.Columns.Add(column2);
            DataColumn column3 = new DataColumn("EventID");
            outputDataTable.Columns.Add(column3);
            DataColumn column10 = new DataColumn("EventRate");
            outputDataTable.Columns.Add(column10);
            DataColumn column4 = new DataColumn("BaselinePayOut");
            outputDataTable.Columns.Add(column4);
            DataColumn column5 = new DataColumn("FMcontractPayOut");
            outputDataTable.Columns.Add(column5);
            DataColumn column6 = new DataColumn("Abs_Difference");
            outputDataTable.Columns.Add(column6);
            DataColumn column7 = new DataColumn("Relavent_Differnce");
            outputDataTable.Columns.Add(column7);
            DataColumn column8 = new DataColumn("Status");
            outputDataTable.Columns.Add(column8);
            DataColumn column11 = new DataColumn("Log");
            outputDataTable.Columns.Add(column11);

            return outputDataTable;
        }

        public void OutputToDataTable(DataRow EventInfo, TestCase testcase, MessageLog Eventlog)
        {
            double abs_difference = (outputobject.FMContractPayout - outputobject.baselinePayout);
            double relative_difference;

            if (outputobject.baselinePayout == 0 && outputobject.FMContractPayout != 0)
            {
                relative_difference = Convert.ToDouble((abs_difference / outputobject.FMContractPayout));
            }
            else if (outputobject.baselinePayout == 0 && outputobject.FMContractPayout == 0)
            {
                relative_difference = 0;
            }
            else
            {
                relative_difference = Convert.ToDouble((abs_difference / outputobject.baselinePayout));
            }

            DataRow row = outputDataTable.NewRow();
            row["ContractID"] = (int)outputobject.ContractId;
            row["AccountID"] = (int)outputobject.AccountId;
            row["EventID"] = Convert.ToInt32(EventInfo["EventID"]);
            row["EventRate"] = (float)Convert.ToDouble(EventInfo["RATE"]);
            row["BaselinePayOut"] = (float)outputobject.baselinePayout;
            row["FMcontractPayOut"] = (float)Convert.ToDouble(outputobject.FMContractPayout);
            row["Abs_Difference"] = (float)abs_difference;
            row["Relavent_Differnce"] = (float)relative_difference;
            row["TestCaseID"] = (int)testcase.TestCaseID;
            row["Log"] = Eventlog.MsgLogSb.ToString();

            if (outputobject.baselinePayout == -999 || outputobject.FMContractPayout == -999)
            {
                row["Status"] = "Fail";
            }
            else if (relative_difference >= 0.005 || relative_difference <= -0.005)
            {
                row["Status"] = "Mismatch";
            }
            else
            {
                row["Status"] = "Pass";
            }

            outputDataTable.Rows.Add(row);
        }

        public void CreateSQLTable(MessageLog log)
        {

            SqlConnection conn = new SqlConnection();
            SqlCommand command = new SqlCommand();
            string query = null;

            conn.ConnectionString = "Server=" + outputobject.OutPutServerName + ";Database=" + outputobject.OutPutDataBase + ";User Id=" + outputobject.OutPutUserName + ";Password=" + outputobject.OutPutPass;
            conn.Open();

            OPSQLtablename = "NTA_Comparison_" + DateTime.Now.ToString("MM_dd_yyyy_HH_mm_ss");
            query = @"CREATE TABLE [" + outputobject.OutPutDataBase + "].dbo.[" + OPSQLtablename +
                        "](TestCaseID int not null," +
                        "ContractID int not null," +
                        "AccountID int not null," +
                        "EventID int not null," +
                        "EventRate float not null," +
                        "BaselinePayout float not null," +
                        "FMContractPayout float not null," +
                        "Abs_Difference float not null," +
                        "Relative_Difference float not null," +
                        "Status varchar(8)," +
                        "Log varchar(MAX));";

            command.Parameters.Clear();

            command.Connection = conn;
            command.CommandText = query;
            command.CommandTimeout = 600;
            SqlDataReader reader = command.ExecuteReader();


            command.Dispose();
            conn.Close();

        }

        public bool CopyDataTableToSQL(MessageLog Accountlog)
        {
            bool Status = true;
            SqlConnection conn = new SqlConnection();

            conn.ConnectionString = "Server=" + outputobject.OutPutServerName + ";Database=" + outputobject.OutPutDataBase + ";User Id=" + outputobject.OutPutUserName + ";Password=" + outputobject.OutPutPass;

            SqlBulkCopy bulkcopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.FireTriggers, null);
            bulkcopy.BatchSize = 10000;
            bulkcopy.BulkCopyTimeout = 400;
            bulkcopy.DestinationTableName = OPSQLtablename;

            conn.Open();

            try
            {
                bulkcopy.WriteToServer(outputDataTable);
            }
            catch (Exception e)
            {
                Accountlog.Log("Error while writing events to SQL :" + e.Message);
                Status = false;
            }
            finally
            {
                conn.Close();
            }
            return Status;
        }

        private double GetRLAccountAAL(TestCase testcase)
        {

            SqlConnection conn = new SqlConnection();
            SqlCommand command = new SqlCommand();
            string query = null;
            double AccountAAL = 0;


            conn.ConnectionString = "Server=" + testcase.RDM.SQLserver + ";Database=" + testcase.RDM.DBName + ";User Id=" + testcase.RDM.SQLlogin + ";Password=" + testcase.RDM.SQLpass;
            try
            {
                conn.Open();

                query = @"SELECT PUREPREMIUM
                          FROM[" + testcase.RDM.DBName + "].[dbo].[rdm_accountstats]" +
                          "where ANLSID =" + testcase.RDM.Anlsid + " and ID =" + outputobject.AccountId + " and PERSPCODE = 'GR'";

                command.Connection = conn;
                command.CommandText = query;
                command.CommandTimeout = 600;
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    AccountAAL = (double)reader["PUREPREMIUM"];
                }

                command.Dispose();
                conn.Close();
                return AccountAAL;
            }

            catch (SqlException e)
            {
                throw e;
            }

        }

        public void PrepareXloutputObject(TestCase testcase, MessageLog Accountlog)
        {
            //try
            //{
            //    outputobject.BaselineAcctAAL = GetRLAccountAAL(testcase);
            //}
            //catch (SqlException e)
            //{
            //    Accountlog.Log("Error getting BaselineAAL from RL :" + e.Message);
            //    outputobject.BaselineAcctAAL = -999;
            //}

            //LINQ query on DataTable to get Contract Level AAL: the sum of (Per event contract payout * event rate)
            outputobject.FMContractAAL = outputDataTable.AsEnumerable()
                                                        .Where(row => Convert.ToInt32(row["AccountID"]) == Convert.ToInt32(outputobject.AccountId))
                                                        .Select(row => Convert.ToDouble(row["FMcontractPayOut"]) * Convert.ToDouble(row["EventRate"]))
                                                        .Sum();
            outputobject.BaselineAcctAAL = outputDataTable.AsEnumerable()
                                                        .Where(row => Convert.ToInt32(row["AccountID"]) == Convert.ToInt32(outputobject.AccountId))
                                                        .Select(row => Convert.ToDouble(row["BaselinePayOut"]) * Convert.ToDouble(row["EventRate"]))
                                                        .Sum();
            outputobject.TotalEventCount = outputDataTable.AsEnumerable()
                                                          .Where(row => Convert.ToInt32(row["AccountID"]) == Convert.ToInt32(outputobject.AccountId))
                                                          .Select(row => Convert.ToInt32(row["EventID"]))
                                                          .Count();
            outputobject.MisMatchEventCount = outputDataTable.AsEnumerable()
                                                             .Where(row => Convert.ToInt32(row["AccountID"]) == Convert.ToInt32(outputobject.AccountId) && Convert.ToString(row["Status"]) == "Mismatch")
                                                             .Select(row => Convert.ToInt32(row["EventID"]))
                                                             .Count();
            outputobject.FailedEventCount = outputDataTable.AsEnumerable()
                                                           .Where(row => Convert.ToInt32(row["AccountID"]) == Convert.ToInt32(outputobject.AccountId) && Convert.ToString(row["Status"]) == "Fail")
                                                           .Select(row => Convert.ToInt32(row["EventID"]))
                                                           .Count();

            //Prepare Performance OutPut Ratio between FM & Reference
            PerformanceOutPut PerformOutPut = outputobject.PerformanceOutput;


            if (PerformOutPut.FMGraphBuildingTime > PerformOutPut.MatrixGraphBuildingTime)
            {
                if (PerformOutPut.MatrixGraphBuildingTime != 0)
                    PerformOutPut.GraphBuildingRatio = PerformOutPut.FMGraphBuildingTime / PerformOutPut.MatrixGraphBuildingTime;
            }
            else
            {
                if(PerformOutPut.FMGraphBuildingTime !=0)
                    PerformOutPut.GraphBuildingRatio = -(PerformOutPut.MatrixGraphBuildingTime / PerformOutPut.FMGraphBuildingTime);
            }


            if (PerformOutPut.FMExecutionTime > PerformOutPut.MatrixExecutionTime)
            {
                if(PerformOutPut.MatrixExecutionTime!=0)
                    PerformOutPut.ExecutionRatio = PerformOutPut.FMExecutionTime / PerformOutPut.MatrixExecutionTime;
            }
            else
            {
                if(PerformOutPut.FMExecutionTime!=0)
                    PerformOutPut.ExecutionRatio = -(PerformOutPut.MatrixExecutionTime) / PerformOutPut.FMExecutionTime;
            }

            outputobject.contractstats = true;

        }

        public ExcelOutPutEngine(OutputObject _outputobject)
        {
            outputobject = _outputobject;
        }
    }

    public class EDSGeneratorAdaptor
    {
        public TestCase testcase { get; set; }
        DownloadedEDSInfo extractsinfo { get; set; }

        //Convert EDM to Extract & update the testcase object with folder path
        public string ConvertEDMtoExtract()
        {
            EDMtoEDSConvertor eds_extractor = new EDMtoEDSConvertor();
            //, @"D:\main\NGDLM\Sandbox\CDLNGFMPrototype\RMS.Prototype.NGFMTestAutomation\EDS_Extract_API"
            extractsinfo = eds_extractor.ConvertEDMToEDS(testcase.EDM.DBName, testcase.EDM.SQLServer, testcase.EDM.SQLlogin, testcase.EDM.SQLpass, testcase.EDM.Portinfoid, testcase.EDM.PortName, "HWD03", @"D:\main\NGDLM\Sandbox\CDLNGFMPrototype\RMS.Prototype.NGFMTestAutomation\EDS_Extract_API\");

            string ExtractPath = GetExtractPath();
            testcase.ExtractFolderPath = ExtractPath;

            //string ExtractPath = @"D:\Nina_Automation_Testing\TempEDSFolder4\";
            //testcase.ExtractFolderPath = ExtractPath;
            return ExtractPath;
        }

        //Get destination folder path and update excel input sheet
        //private string GetExtractPath(TestCase testcase)
        //{
        //    string xmlFilePath = @"D:\main\NGModelCertificationTools\CDLNGFMPrototype\RMS.Prototype.NGFMTestAutomation\bin\Debug\Config.xml";
        //    XmlDocument xmldoc = new XmlDocument();
        //    xmldoc.Load(xmlFilePath);
        //    string ExtractOutputPath;

        //    foreach (XmlNode childnode in xmldoc.ChildNodes)
        //    {
        //        if (childnode.Name.Equals("Configuration"))
        //        {
        //            foreach (XmlNode child in childnode.ChildNodes)
        //            {
        //                if (child.Name.Equals("EDSExtractTargetFolder"))
        //                {
        //                    string ExtractFolderName = testcase.EDM.DBName + "_" + "PortID" + "_" + testcase.EDM.Portinfoid + "*";
        //                    ExtractOutputPath = Directory.GetDirectories(child.InnerText, ExtractFolderName)[0];
        //                }
        //            }
        //        }
        //    }
        //    return ExtractOutputPath;
        //}

        private string GetExtractPath()
        {
            string ExtractOutputPath = null;
            Dictionary<string, string> folderInfo = extractsinfo.FOLDERNAMES;
            foreach (KeyValuePair<string, string> folder in folderInfo)
            {
                ExtractOutputPath = folder.Key;
            }

            return ExtractOutputPath;
        }

        public string GetErrorMessage()
        {
            string ErrorMessage = null;
            Dictionary<string, string> folderInfo = extractsinfo.FOLDERNAMES;
            foreach (KeyValuePair<string, string> folder in folderInfo)
            {
                ErrorMessage = folder.Value;
            }

            return ErrorMessage;
        }

        public EDSGeneratorAdaptor(TestCase _testcase, DownloadedEDSInfo _extractsinfo)
        {
            testcase = _testcase;
            extractsinfo = _extractsinfo;
        }
    }

    public class PartitionDataEngine
    {
        public TestCase testcase { get; set; }

        private string[] ExistentFiles(string[] files)
        {
            var fs = new List<string>();
            if (files != null)
                foreach (string f in files)
                    if (File.Exists(f))
                        fs.Add(f);
            return fs.ToArray();
        }

        private PartitionData Deserialize(string file)
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

        public bool DeserializePartitionData(MessageLog TestCaselog, out PartitionData Pdat)
        {
            bool Status = true;
            string extractfolderpath = testcase.ExtractFolderPath;
            string[] PortExtractFolder = null;
            CancellationTokenSource ct = new CancellationTokenSource();
            try
            {
                PortExtractFolder = Directory.GetFiles(extractfolderpath, "*", SearchOption.AllDirectories).Where(x => Char.IsDigit(Path.GetFileName(x)[11])).ToArray();
            }
            catch (ArgumentException argex)
            {
                TestCaselog.Log("Error while DeserializePartitionData PartitionData : ExtractFolderPath is null or contain invalid characters :" + " " + argex.Message);
                Pdat = null;
                return false;
            }
            catch (DirectoryNotFoundException direx)
            {
                TestCaselog.Log("Error while DeserializePartitionData PartitionData : ExtractFolderPath can not be found" + direx.Message);
                Pdat = null;
                return false;
            }
            catch (Exception ex)
            {
                TestCaselog.Log("Error while DeserializePartitionData PartitionData :" + ex.Message);
                Pdat = null;
                return false;
            }

            List<PartitionData> PDs = new List<PartitionData>();
            foreach (var f in ExistentFiles(PortExtractFolder))
            {
                if (ct.IsCancellationRequested)
                {
                    Pdat = null;
                    return false;
                }
                var pd = Deserialize(f);
                //if (testcase.TestCaseType=="RL")
                //{
                    foreach (ContractExposure ContrExpo in pd.Exposures)
                    {
                        Regex regex = new Regex(Regex.Escape("Declarations\r\n"));
                        string NewCDL = regex.Replace(ContrExpo.Contract.CDLString, "Declarations\r\n    Claims Adjustment Options Are (Deductibles are absorbable, Sublimits are Net of Deductible)\r\n", 1);

                        ContrExpo.Contract.CDLString = NewCDL;
                    }
                //}
                if (pd != null)
                    PDs.Add(pd);
            }
            Pdat = (PDs.Count > 0) ? PartitionData.Merge(PDs) : null;

            return Status;

        }

        //PartitionDataEngine Constructor
        public PartitionDataEngine(TestCase _testCase)
        {
            testcase = _testCase;
        }

        //Modifying MultiBuilding in Pdat and serialize it back to the Extract
        public void ModifyNumOfBldgs(PartitionData partitiondata, int NumOfBldgs)
        {
            foreach (ContractExposure ContractExpo in partitiondata.Exposures)
            {
                ContractSubjectExposureOfRiteSchedule riteschedule = (ContractSubjectExposureOfRiteSchedule)ContractExpo.ContractSubjectExposures[0];

                List<RITExposure> list = new List<RITExposure>();

                if (riteschedule.RITECollectionExposure.RITExposures != null)
                {
                    list = riteschedule.RITECollectionExposure.RITExposures.ToList();

                    foreach (RITExposure riteExposure in list)
                    {
                        riteExposure.CommonCharacteristics.NumBuildings = NumOfBldgs;
                    }
                }
            }
        }

        public void RandomModifyNumOfBldgs(PartitionData partitiondata)
        {
            foreach (ContractExposure ContractExpo in partitiondata.Exposures)
            {
                ContractSubjectExposureOfRiteSchedule riteschedule = (ContractSubjectExposureOfRiteSchedule)ContractExpo.ContractSubjectExposures[0];

                List<RITExposure> list = new List<RITExposure>();

                Random random = new Random();
                int NumOfBldg;

                if (riteschedule.RITECollectionExposure.RITExposures != null)
                {
                    list = riteschedule.RITECollectionExposure.RITExposures.ToList();

                    foreach (RITExposure riteExposure in list)
                    {
                        NumOfBldg = random.Next(2, 500);
                        riteExposure.CommonCharacteristics.NumBuildings = NumOfBldg;
                    }
                }
            }
        }

        //Serialize partitionData
        public void Serialize(PartitionData pData, string file)
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

        //Given a folder path, to generate new extract and save to that folder
        public void SerializeCreateNew(PartitionData pData, string file)
        {
            //using (var wc = new WebClient().OpenWrite(file))
            //{
                try
                {
                    //string folderpath = file + "MTH_testcase_"+testcase.TestCaseID.ToString();
                    string folderpath = file + "MultiBldg_Performance_" + testcase.Description.SubFeature.ToString()+"_Bldgs";
                    Directory.CreateDirectory(folderpath);
                    Stream stream = File.Create(folderpath + "\\rites_batch0.dat");
                    ProtoBuf.Serializer.Serialize<PartitionData>(stream, pData);
                    stream.Close();
                }
                catch (ProtoBuf.ProtoException p)
                {
                    //Log.Fatal("Error Writing of protobuf to file" + p.Message);
                    Console.WriteLine("Error Writing of protobuf to file \"{0}\": {1}", file, p.Message);
                }
            //}

        }

        //Given a folder path, to generate new extract and save to that folder: new file name include number of contracts in extract
        public void SerializeSubContracts(PartitionData pData, string file, int NumOfContracts)
        {
            //using (var wc = new WebClient().OpenWrite(file))
            //{
            try
            {
                //string folderpath = file + "MTH_testcase_"+testcase.TestCaseID.ToString();
                string path = file + "\\rites_batch" + NumOfContracts + "_Contracts.dat";
                Stream stream = File.Create(path);
                ProtoBuf.Serializer.Serialize<PartitionData>(stream, pData);
                stream.Close();
            }
            catch (ProtoBuf.ProtoException p)
            {
                //Log.Fatal("Error Writing of protobuf to file" + p.Message);
                Console.WriteLine("Error Writing of protobuf to file \"{0}\": {1}", file, p.Message);
            }
            //}

        }

        //create sub extracts from original extracts
        public void SplitExtract(PartitionData pData,int increment)
        {

            int NewExtractNumOfContracts = 0;

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < 55; i++)
            {
                NewExtractNumOfContracts = NewExtractNumOfContracts + increment;
                PartitionData newPD = new PartitionData();

                newPD.Exposures = new ContractExposure[NewExtractNumOfContracts];

                //only extract few contract
                for (int j = 0; j < NewExtractNumOfContracts; j++)
                {
                    newPD.Exposures[j] = pData.Exposures[j];
                }

                SerializeSubContracts(newPD, @"C:\Nina_Automation_Testing\ForYoungsuk\SplitExtracts", NewExtractNumOfContracts);
                string settingBatchLine = "<Value>" + NewExtractNumOfContracts + "</Value>";
                sb.AppendLine(settingBatchLine);
            }

            StreamWriter file = new StreamWriter(@"C:\Nina_Automation_Testing\ForYoungsuk\AnalysisSettingBatch.txt");
            file.WriteLine(sb.ToString());
            file.Close();
            file.Dispose();
        }

        //Change TIV to Original TIV * NumOfBldg (Since NumOfBldg was increased earlier)
        public void ScaleTIV(PartitionData partitiondata)
        {
            foreach (ContractExposure ContractExpo in partitiondata.Exposures)
            {
                ContractSubjectExposureOfRiteSchedule riteschedule = (ContractSubjectExposureOfRiteSchedule)ContractExpo.ContractSubjectExposures[0];

                List<RITExposure> list = new List<RITExposure>();

                if (riteschedule.RITECollectionExposure.RITExposures != null)
                {
                    list = riteschedule.RITECollectionExposure.RITExposures.ToList();

                    foreach (RITExposure riteExposure in list)
                    {
                        foreach (RiskItemCharacteristicsValuation CharacteristicItem in riteExposure.RiskitemCharacteristicsList.Items)
                        {
                            CharacteristicItem.RITExposureValuationList[0].Value *= riteExposure.CommonCharacteristics.NumBuildings;
                        }
                    }
                }
            }
        }

    }

    public class MessageLog
    {
        public StringBuilder MsgLogSb { get; set; }

        public void Log(string ProcessMsg)
        {
            MsgLogSb.AppendLine(ProcessMsg);
        }

        public MessageLog()
        {
            MsgLogSb = new StringBuilder();
        }

    }

    public class ContractAcctMap
    {

        public PartitionData Pdata { get; set; }

        public Dictionary<long, int> MapContractIdToAccgrpid(MessageLog Msglogsb, out bool PrepStatus)
        {
            Dictionary<long, int> ContractIdAcctId = new Dictionary<long, int>();
            PrepStatus = true;

            try
            {

                foreach (ContractExposure ContractExpo in Pdata.Exposures)
                {
                    int acctid;
                    bool Status = Int32.TryParse(ContractExpo.ExternalSourceId.Split(':')[2], out acctid);

                    if (Status == false)
                    {
                        Msglogsb.Log("Error while mapping ContractID to AccgrpID in partition data: Invalid AccountID;");
                        break;
                    }
                    if(!ContractIdAcctId.ContainsKey(ContractExpo.ExposureID))
                    ContractIdAcctId.Add(ContractExpo.ExposureID, acctid);
                }
            }
            catch (ArgumentException e)
            {
                Msglogsb.Log("Error while mapping ContractID to AccgrpID in partition data, please check partition data:" + " " + e.Message);
                PrepStatus = false;
            }

            return ContractIdAcctId;
        }

        public ContractAcctMap(PartitionData pdat)
        {
            Pdata = pdat;
        }

    }

    public class LoccvgRiteMap
    {
        public Dictionary<int, Tuple<long, int>> MapLoccvgIdToRiteId(PartitionData partitiondata, MessageLog Msglogsb)
        {
            //Loop through EDS ContractExposuresFromFile to find mapping between RiteId and Loccvgid and store mapping in Dictionary
            Dictionary<int, Tuple<long, int>> LoccvgidMapping = new Dictionary<int, Tuple<long, int>>();

            foreach (ContractExposure ContractExpo in partitiondata.Exposures)
            {
                ContractSubjectExposureOfRiteSchedule riteschedule = (ContractSubjectExposureOfRiteSchedule)ContractExpo.ContractSubjectExposures[0];
                int loccvgid;
                long RiteId = 0;
                bool ParseStatus;
                int NumOfBldgs = 1;

                List<RITExposure> list = new List<RITExposure>();

                if (riteschedule.RITECollectionExposure.RITExposures != null)
                {
                    list = riteschedule.RITECollectionExposure.RITExposures.ToList();

                    foreach (RITExposure riteExposure in list)
                    {
                        NumOfBldgs = riteExposure.CommonCharacteristics.NumBuildings;
                        foreach (RiskItemCharacteristicsValuation CharacteristicItem in riteExposure.RiskitemCharacteristicsList.Items)
                        {
                            ParseStatus = Int32.TryParse(CharacteristicItem.ExternalSourceId, out loccvgid);
                            if (ParseStatus == false)
                            {
                                Msglogsb.Log("Error while mapping LoccvgID to RiteID in partition data: Invalid Loccvgid;");
                                break;
                            }

                            RiteId = CharacteristicItem.Id;
                            if (!LoccvgidMapping.ContainsKey(loccvgid))
                                LoccvgidMapping.Add(loccvgid, Tuple.Create(RiteId,NumOfBldgs)); // store mapping in dictionary
                        }
                    }
                }
            }
            return LoccvgidMapping;
        }
    }

    public class AccountLevelPrep
    {
        public TestCase testcase { get; set; }
        public Dictionary<long, int> ContrAcctMap { get; set; }
        public MessageLog MsgLogSb { get; set; }

        //1. Get EventId and Event Rate for all events in one account
        public DataTable GetEventList(long ContractId)
        {
            DataTable EventsTable = new DataTable();
            int Accountid = ContrAcctMap[ContractId];
            SqlConnection conn = new SqlConnection();
            SqlCommand command = new SqlCommand();
            string query = null;

            //Create DataTable for eventid and event rate to calculate Contract Level AAL later for NGFM
            //Table includes all events and rates affect this account at GU level.
            EventsTable = new DataTable();

            conn.ConnectionString = "Server=" + testcase.RDM.SQLserver + ";Database=" + testcase.RDM.DBName + ";User Id=" + testcase.RDM.SQLlogin + ";Password=" + testcase.RDM.SQLpass;
            try
            {
                conn.Open();

                query = @"if not exists(select * from sys.servers where name = @EDMServer)
                Begin
                EXEC sp_addlinkedserver @EDMServer;
                Exec sp_addlinkedsrvlogin @EDMServer, 'false' , null,  @EDMUser, @EDMPass;
                end
                
                DECLARE @query AS NVARCHAR(MAX) 
                set @query= 'Select d.EVENTID, d.RATE  From [' + @RDMDB + '].[dbo].[rdm_anlsevent]as d
                where d.ANLSID=' + @Anlsid+' and d.EVENTID in(Select distinct c.EVENTID
                From [' + @RDMDB + '].[dbo].[rdm_loccvg]as c
                where c.PERSPCODE =''GU'' and c.PERSPCODE=''GU'' and c.ANLSID=' 
                + @Anlsid+' and c.LOSSTYPE in (1,2,3) and c.ID in (select b.loccvgid 
                FROM ['+@EDMServer+'].['+@EDMDB+'].[dbo].[Loc] as a
                inner join ['+@EDMServer+'].['+@EDMDB+'].[dbo].[loccvg] as b
                on a.LOCID=b.LOCID
                where a.ACCGRPID='+@Accgrpid+' and b.LOSSTYPE in (1,2,3)))' 
                                     
                Exec sp_executesql @query 
                
                if exists(select * from sys.servers where name = @EDMServer)
                Begin
                	Exec sp_dropserver @EDMServer, 'droplogins';
                end";


                command.Parameters.Clear();

                command.Parameters.AddWithValue("@EDMServer", testcase.EDM.SQLServer);
                command.Parameters.AddWithValue("@EDMUser", testcase.EDM.SQLlogin);
                command.Parameters.AddWithValue("@EDMPass", testcase.EDM.SQLpass);
                command.Parameters.AddWithValue("@EDMDB", testcase.EDM.DBName);
                command.Parameters.AddWithValue("@RDMServer", testcase.RDM.SQLserver);
                command.Parameters.AddWithValue("@RDMUser", testcase.RDM.SQLlogin);
                command.Parameters.AddWithValue("@RDMPass", testcase.RDM.SQLpass);
                command.Parameters.AddWithValue("@RDMDB", testcase.RDM.DBName);
                command.Parameters.AddWithValue("@Anlsid", testcase.RDM.Anlsid);
                command.Parameters.AddWithValue("@Accgrpid", Accountid.ToString());

                command.Connection = conn;
                command.CommandText = query;
                command.CommandTimeout = 600;

                SqlDataAdapter DataAdapter = new SqlDataAdapter(command);
                DataAdapter.Fill(EventsTable);
            }

            catch (SqlException e)
            {
                MsgLogSb.Log("Error when PrepareContract.GetEventList from RL: " + e.Message);
            }
            finally
            {
                command.Dispose();
                conn.Close();
            }

            return EventsTable;
        }

        //2. Get account level baseline payout for all events in one account
        public DataTable GetAcctBaseline(long ContractId)
        {
            DataTable BaselinePayoutTable = new DataTable();
            int AccountId = ContrAcctMap[ContractId];
            SqlConnection conn = new SqlConnection();
            SqlCommand command = new SqlCommand();
            string query = null;
            conn.ConnectionString = "Server=" + testcase.RDM.SQLserver + ";Database=" + testcase.RDM.DBName + ";User Id=" + testcase.RDM.SQLlogin + ";Password=" + testcase.RDM.SQLpass;

            try
            {
                conn.Open();

                query = "SELECT PERSPVALUE, EVENTID FROM [" + testcase.RDM.DBName + "].[dbo].[rdm_account]" +
                        "where ANLSID =" + testcase.RDM.Anlsid + " and ID=" + AccountId + "and PERSPCODE='GR'";

                command.Parameters.Clear();
                command.Connection = conn;
                command.CommandText = query;
                command.CommandTimeout = 600;

                SqlDataAdapter DataAdapter = new SqlDataAdapter(command);
                DataAdapter.Fill(BaselinePayoutTable);
            }
            catch (SqlException e)
            {
                MsgLogSb.Log("Error while PrepareContract.GetAcctBaseline from RL: " + e.Message);
            }
            finally
            {
                command.Dispose();
                conn.Close();
            }
            return BaselinePayoutTable;
        }

        //3. Get Loccvgid and damage ratio for all events in one account
        public DataTable GetDRfromRLRDM(TestCase testcase, long ContractId, int Accountid, CampusIdentifier campusidf)
        {
            DataTable LoccvgDrAllEvents = new DataTable();
            SqlConnection conn = new SqlConnection();
            SqlCommand command = new SqlCommand();

            //If Campus, get the loccvgids and DRs from bldg table in RL. Else, get it from Loccvg table

            if (campusidf.IsCampus == true && campusidf.ContractIsCampusList.Contains(ContractId))
            {
                string query = null;

                conn.ConnectionString = "Server=" + testcase.RDM.SQLserver + ";Database=" + testcase.RDM.DBName + ";User Id=" + testcase.RDM.SQLlogin + ";Password=" + testcase.RDM.SQLpass;
                try
                {
                    conn.Open();

                    query = @"if not exists(select * from sys.servers where name = @EDMServer)
                        Begin
                        EXEC sp_addlinkedserver @EDMServer;
                        Exec sp_addlinkedsrvlogin @EDMServer, 'false' , null,  @EDMUser, @EDMPass;
                        end

                        DECLARE @query AS NVARCHAR(MAX) 
                        set @query= 'Select c.ID, c.EVENTID, c.PERSPVALUE/c.EXPVALUE as DamageRatio
                        FROM [' + @RDMDB + '].[dbo].[rdm_bldgcvg]as c
                        where c.PERSPCODE =''GU'' and c.ANLSID=' 
                        + @Anlsid+' and c.LOSSTYPE in (1,2,3) and 
                        c.ID in(Select b.LOCCVGID FROM ['+@EDMServer+'].['+@EDMDB+'].[dbo].[loccvg] as b
                                where b.LOSSTYPE in (1,2,3) and b.LOCID
                             in(Select a.LOCID FROM ['+@EDMServer+'].['+@EDMDB+'].[dbo].[Loc] as a
                                where a.ACCGRPID='+@Accgrpid+'))'
                     
                        Exec sp_executesql @query 

                        if exists(select * from sys.servers where name = @EDMServer)
                        Begin
	                        Exec sp_dropserver @EDMServer, 'droplogins';
                        end";

                    command.Parameters.Clear();

                    command.Parameters.AddWithValue("@EDMServer", testcase.EDM.SQLServer);
                    command.Parameters.AddWithValue("@EDMUser", testcase.EDM.SQLlogin);
                    command.Parameters.AddWithValue("@EDMPass", testcase.EDM.SQLpass);
                    command.Parameters.AddWithValue("@EDMDB", testcase.EDM.DBName);
                    command.Parameters.AddWithValue("@RDMServer", testcase.RDM.SQLserver);
                    command.Parameters.AddWithValue("@RDMUser", testcase.RDM.SQLlogin);
                    command.Parameters.AddWithValue("@RDMPass", testcase.RDM.SQLpass);
                    command.Parameters.AddWithValue("@RDMDB", testcase.RDM.DBName);
                    command.Parameters.AddWithValue("@Anlsid", testcase.RDM.Anlsid);
                    command.Parameters.AddWithValue("@Accgrpid", Accountid.ToString());

                    command.Connection = conn;
                    command.CommandText = query;
                    command.CommandTimeout = 600;

                    SqlDataAdapter DataAdapter = new SqlDataAdapter(command);
                    DataAdapter.Fill(LoccvgDrAllEvents);
                }

                catch (SqlException e)
                {
                    MsgLogSb.Log("Error when PrepareContract.GetDRfromRLRDM: " + e.Message);
                }
                finally
                {
                    command.Dispose();
                    conn.Close();
                }
            

            }
            else
            {

                string query = null;

                conn.ConnectionString = "Server=" + testcase.RDM.SQLserver + ";Database=" + testcase.RDM.DBName + ";User Id=" + testcase.RDM.SQLlogin + ";Password=" + testcase.RDM.SQLpass;
                try
                {
                    conn.Open();

                    query = @"if not exists(select * from sys.servers where name = @EDMServer)
                        Begin
                        EXEC sp_addlinkedserver @EDMServer;
                        Exec sp_addlinkedsrvlogin @EDMServer, 'false' , null,  @EDMUser, @EDMPass;
                        end

                        DECLARE @query AS NVARCHAR(MAX) 
                        set @query= 'Select c.ID, c.EVENTID, c.PERSPVALUE/d.EXPVALUE as DamageRatio
                        FROM [' + @RDMDB + '].[dbo].[rdm_loccvg]as c
                        inner join [' + @RDMDB + '].[dbo].[rdm_loccvgstd]as d
                        on c.ID=d.ID and c.EVENTID = d.EVENTID and c.PERSPCODE=d.PERSPCODE and c.ANLSID=d.ANLSID
                        where c.PERSPCODE =''GU'' and c.ANLSID=' 
                        + @Anlsid+' and c.LOSSTYPE in (1,2,3) and 
                        c.ID in(Select b.LOCCVGID FROM ['+@EDMServer+'].['+@EDMDB+'].[dbo].[loccvg] as b
                                where b.LOSSTYPE in (1,2,3) and b.LOCID
                             in(Select a.LOCID FROM ['+@EDMServer+'].['+@EDMDB+'].[dbo].[Loc] as a
                                where a.ACCGRPID='+@Accgrpid+'))'
                     
                        Exec sp_executesql @query 

                        if exists(select * from sys.servers where name = @EDMServer)
                        Begin
	                        Exec sp_dropserver @EDMServer, 'droplogins';
                        end";

                    command.Parameters.Clear();

                    command.Parameters.AddWithValue("@EDMServer", testcase.EDM.SQLServer);
                    command.Parameters.AddWithValue("@EDMUser", testcase.EDM.SQLlogin);
                    command.Parameters.AddWithValue("@EDMPass", testcase.EDM.SQLpass);
                    command.Parameters.AddWithValue("@EDMDB", testcase.EDM.DBName);
                    command.Parameters.AddWithValue("@RDMServer", testcase.RDM.SQLserver);
                    command.Parameters.AddWithValue("@RDMUser", testcase.RDM.SQLlogin);
                    command.Parameters.AddWithValue("@RDMPass", testcase.RDM.SQLpass);
                    command.Parameters.AddWithValue("@RDMDB", testcase.RDM.DBName);
                    command.Parameters.AddWithValue("@Anlsid", testcase.RDM.Anlsid);
                    command.Parameters.AddWithValue("@Accgrpid", Accountid.ToString());

                    command.Connection = conn;
                    command.CommandText = query;
                    command.CommandTimeout = 600;

                    SqlDataAdapter DataAdapter = new SqlDataAdapter(command);
                    DataAdapter.Fill(LoccvgDrAllEvents);
                }

                catch (SqlException e)
                {
                    MsgLogSb.Log("Error when PrepareContract.GetDRfromRLRDM: " + e.Message);
                }
                finally
                {
                    command.Dispose();
                    conn.Close();
                }
            }
            return LoccvgDrAllEvents;
        }
        

        public AccountLevelPrep(TestCase _testcase, Dictionary<long, int> _ContrAcctMap, MessageLog _MsgLogSb)
        {
            testcase = _testcase;
            ContrAcctMap = _ContrAcctMap;
            MsgLogSb = _MsgLogSb;
        }
    }

    public class CampusIdentifier
    {
        public bool IsCampus { get; set; }
        public HashSet<long> ContractIsCampusList { get; set; }

        public void CheckCampus(PartitionData pdat)
        {
            // Nina: Check if it has Campus in the Subschedule
            IsCampus = false;
            List<RITExposure> RitExposureList = new List<RITExposure>();
            ContractIsCampusList = new HashSet<long>();
            foreach (ContractExposure ConExp in pdat.Exposures)
            {
                if (((ContractSubjectExposureOfRiteSchedule)ConExp.ContractSubjectExposures[0]).RITECollectionExposure.RITExposures != null)
                {
                    RitExposureList = ((ContractSubjectExposureOfRiteSchedule)ConExp.ContractSubjectExposures[0]).RITECollectionExposure.RITExposures.ToList();
                    foreach (RITExposure Ritexposure in RitExposureList)
                    {
                        long ParentRitExposureID = Ritexposure.ParentRITExposureId;
                        long RitExposureID = Ritexposure.ExposureID;
                        if (RitExposureID != ParentRitExposureID || IsCampus)
                        {
                            IsCampus = true;
                            ContractIsCampusList.Add(ConExp.ExposureID);
                        }
                    }
                }
            }
        }
        
    }

    public interface IBaseline
    {

        DataTable EventListTable { get; set; }

        Dictionary<long, int> ContrAcctMap { get; set; }

        bool PrepareTestCase(MessageLog TestCaseLog);

        //Prepare EventList per Account/Contract
        bool PrepareContract(long ContractId, MessageLog MsglogSb);

        //Get GUlosses
        bool GetGUlosses(long ContractId, int Event, MessageLog MsglogSb, out Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> GulossDictionary);

        //Get Reference GUlosses & with Period
        bool GetPeriodGUlosses(long ContractId, int Event, MessageLog MsglogSb, out List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodGulossDictionary);

        //Get Baseline Payout
        bool GetBaselinePayout(int EventId, MessageLog MsglogSb, Stopwatch ExecutionSW);

    }

    public class RLBaseline : IBaseline
    {
        public PartitionData Pdata { get; set; }
        public TestCase testcase { get; set; }
        public Dictionary<long, int> ContrAcctMap { get; set; }
        public Dictionary<int, Tuple<long, int>> LoccvgRiteMap { get; set; }
        public DataTable EventListTable { get; set; }
        public DataTable RLAcctGRTable { get; set; }
        public DataTable AcctDrTable { get; set; }
        public OutputObject outputobject { get; set; }
        public ExcelOutPutEngine outputengine { get; set; }
        public CampusIdentifier campusIdf { get; set; }

        //1. Get AccountContractId Mapping & LoccvgRiteId mapping for one testcase & CheckCampus
        public bool PrepareTestCase(MessageLog TestCaseLog)
        {
            bool PrepStatus;
            ContractAcctMap CAmap = new ContractAcctMap(Pdata);
            ContrAcctMap = CAmap.MapContractIdToAccgrpid(TestCaseLog, out PrepStatus);
            if (PrepStatus == false)
            {
                return PrepStatus;
            }
            else
            {
                if (ContrAcctMap.Count == 0)
                {
                    TestCaseLog.Log("Error while Preparing TestCase: Can't generate ContractID-AccountID Map(Dictionary);Please check PartitionData");
                    PrepStatus = false;
                }

                LoccvgRiteMap LRmap = new LoccvgRiteMap();
                LoccvgRiteMap = LRmap.MapLoccvgIdToRiteId(Pdata, TestCaseLog);

                if (LoccvgRiteMap.Count == 0)
                {
                    TestCaseLog.Log("Error while Preparing TestCase: Can't generate LoccvgID-RiteID Map(Dictionary); Please check PartitionData");
                    PrepStatus = false;
                }

                //Check if the testcase has Campus Accounts
                campusIdf = new CampusIdentifier();
                campusIdf.CheckCampus(Pdata);
            }

            return PrepStatus;
        }

        //2.Get Contract Level Info Ready
        public bool PrepareContract(long ContractId, MessageLog MsglogSb)
        {
            AccountLevelPrep AccountPrep = new AccountLevelPrep(testcase, ContrAcctMap, MsglogSb);
            bool PrepStatus = true;

            //1. Prepare Event List
            EventListTable = AccountPrep.GetEventList(ContractId);
            if (EventListTable.Rows.Count == 0)
            {
                MsglogSb.Log("Error Preparing Contract in Account Loop : No events found in RL RDM");
                outputobject.contractstats = false;
                PrepStatus = false;
            }

            //2. Get RL Baseline Payout for one account including all events
            RLAcctGRTable = AccountPrep.GetAcctBaseline(ContractId);

            //3. Get Loccvgid and Damage Ratio for one account including all events
            AcctDrTable = AccountPrep.GetDRfromRLRDM(testcase, ContractId, ContrAcctMap[ContractId],campusIdf);
            if (AcctDrTable.Rows.Count == 0)
            {
                MsglogSb.Log("Error Preparing Contract in Account Loop: No Damage Ratio found in RL RDM");
                PrepStatus = false;
            }

            return PrepStatus;
        }

        //3. Get GUloss ready for one Event
        public bool GetGUlosses(long ContractId, int Event, MessageLog MsglogSb, out Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> GulossDictionary)
        {
            bool CompleteStatus = true;
            GulossDictionary = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();
            Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>> DictBySampleId = new Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>();
            Dictionary<long, Tuple<double, uint, List<float>>> DictByRiteId = new Dictionary<long, Tuple<double,uint, List<float>>>();
            uint time = 0;
            int sampleid = 0;
            string Peril = testcase.Description.Peril;
            int AccountId = ContrAcctMap[ContractId];

            if (testcase.EDM.MultiBldg != true)
            {
                double factor = 1.0;

                Dictionary<int, double> DamageRatioDict = AcctDrTable.AsEnumerable().Where(row => Convert.ToInt32(row["EVENTID"]) == Event)
                                                                .ToDictionary(row => Convert.ToInt32(row["ID"]), row => Convert.ToDouble(row["DamageRatio"]));

                foreach (KeyValuePair<int, double> kvpDRdict in DamageRatioDict)
                {
                    List<float> MultiBldgDr = new List<float>();
                    MultiBldgDr.Add((float)kvpDRdict.Value);
                    var DamageRatio = Tuple.Create(factor, time, MultiBldgDr);
                    long Riteid = (long)LoccvgRiteMap[kvpDRdict.Key].Item1;
                    DictByRiteId.Add(Riteid, DamageRatio);
                }
            }
            else
            {
                double factor = 1;
                double MaxNumOfBldgs = 5.0;
                int NumOfBldgs = 1;

                Dictionary<int, double> DamageRatioDict = AcctDrTable.AsEnumerable().Where(row => Convert.ToInt32(row["EVENTID"]) == Event)
                                                                .ToDictionary(row => Convert.ToInt32(row["ID"]), row => Convert.ToDouble(row["DamageRatio"]));

                foreach (KeyValuePair<int, double> kvpDRdict in DamageRatioDict)
                {
                    NumOfBldgs =LoccvgRiteMap[kvpDRdict.Key].Item2;
                    factor = (NumOfBldgs/ MaxNumOfBldgs);
                    List<float> MultiBldgDr = new List<float>();
                    //if (NumOfBldgs <= MaxNumOfBldgs)
                    //{
                    //    for (int i = 0; i < NumOfBldgs; i++)
                    //    {
                    //        MultiBldgDr.Add((float)kvpDRdict.Value);
                    //    }
                    //}
                    //else
                    //{
                        for (int i = 0; i < MaxNumOfBldgs; i++)
                        {
                            MultiBldgDr.Add((float)kvpDRdict.Value);
                        }
                    //}
                    var DamageRatio = Tuple.Create(factor, time, MultiBldgDr);
                    long Riteid = (long)LoccvgRiteMap[kvpDRdict.Key].Item1;
                    DictByRiteId.Add(Riteid, DamageRatio);
                }

            }

            DictBySampleId.Add(sampleid, DictByRiteId);
            GulossDictionary.Add(Peril, DictBySampleId);
                  
            return CompleteStatus;
        }

        public bool GetPeriodGUlosses(long ContractId, int Event, MessageLog MsglogSb, out List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodGulossDictionary)
        {
            throw new Exception("Comparison with RL only support GU Losses for events at this moment");
        }

        //4. Get RL Account GR for one event and assign Payout to OutPutObject
        public bool GetBaselinePayout(int EventId, MessageLog MsglogSb, Stopwatch ExecutionSW)
        {
            bool CompleteStatus = true;
            var BaselinePayout = RLAcctGRTable.AsEnumerable()
                                           .Where(row => Convert.ToInt32(row["EventID"]) == Convert.ToInt32(EventId))
                                           .Select(row => Convert.ToDouble(row["PERSPVALUE"]));
            if (BaselinePayout.Any())
            {
                outputobject.baselinePayout = BaselinePayout.Single();
            }
            else
            {
                outputobject.baselinePayout = 0;
            }
            return CompleteStatus;
        }

        //Constructor
        public RLBaseline(PartitionData _pdat, TestCase _testcase, OutputObject _outputobject, ExcelOutPutEngine _outputengine)
        {
            Pdata = _pdat;
            testcase = _testcase;
            outputobject = _outputobject;
            outputengine = _outputengine;
        }

    }

    public class SpecialFeatureBaseline : IBaseline
    {
        public DataTable EventListTable { get; set; }
        public Dictionary<long, int> ContrAcctMap { get; set; }
        public PartitionData Pdat { get; set; }
        public TestCase TestCase { get; set; }
        public long ContractID { get; set; }
        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> GuLossesDictionary { get; set; }
        public GraphType graphtype { get; set; }
        public OutputObject outputobject { get; set; }
        private ReferencePrototype RP { get; set; }
        private VectorGUInputGeneratorFactory VectorGUInputFactory { get; set; }
        private GUInputGeneratorFactory GUgeneratorFactory { get; set; }
        private PLTGenertorFactory PeriodgeneratorFactory { get; set; }   
        private GUInputGenerator GUgenerator { get; set; }
        private VectorGUInputGenerator VectorGuGenerator { get; set; }        
        private Period HDFMPeriod { get; set; }
        private PLTGenerator HDFMEventGen { get; set; }
        private List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodGULossDictionary { get; set; }

        //private IVectorEvent VectorGULosses { get; set; }

        //1.Prepare ContractAcctMapping & Get Graph Type
        public bool PrepareTestCase(MessageLog TestCaseLog)
        {
            // Preparation for Reference  
            HashSet<string> Subperils = new HashSet<string>(TestCase.Description.SubPeril);
            //Default SubSampling Settings
            SubSamplingAnalysisSetting subSamplingSettings = new SubSamplingAnalysisSetting(false, 1, 0, 200000, "", "");

            RP = new ReferencePrototype(Pdat, new RAPSettings(Subperils), subSamplingSettings);

            //VectorGUInputFactory = new VectorGUInputGeneratorFactory(Pdat, Subperils, TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio, false, subSamplingSettings);
            
            //Make GU Factory for HDFM Prototype GU Loss generator
            if (TestCase.InputType == GUInputType.Event)
            {
                GUgeneratorFactory = new GUInputGeneratorFactory(Pdat, Subperils, subSamplingSettings, TimeStyle.RandomTimeStamps, LossStyle.DamagaeRatio);
            }
            else
            {
                DateTime start = DateTime.Now;
                PeriodgeneratorFactory = new PLTGenertorFactory(Pdat, Subperils, subSamplingSettings, start, TimeStyle.ConstantTimeStamps, LossStyle.DamagaeRatio);
            }
            ContrAcctMap = new Dictionary<long, int>();
            foreach (ContractExposure ContractExpo in Pdat.Exposures)
            {
                if(!ContrAcctMap.ContainsKey(ContractExpo.ExposureID))
                ContrAcctMap.Add(ContractExpo.ExposureID, Convert.ToInt32(ContractExpo.ExposureID));
            }

            graphtype = new GraphType();
            switch (TestCase.GraphType)
            {
                case "Auto":
                    graphtype = GraphType.Auto;
                    break;
                case "FixedGraph1":
                    graphtype = GraphType.FixedGraph1;
                    break;
                case "FixedGraph2":
                    graphtype = GraphType.FixedGraph2;
                    break;
                case "FixedGraphOverlap":
                    graphtype = GraphType.FixedGraphOverlap;
                    break;
                case "FixedGraphOverlapSubperil":
                    graphtype = GraphType.FixedGraphOverlapSubperil;
                    break;
                case "FixedTreaty1":
                    graphtype = GraphType.FixedTreaty1;
                    break;
                case "Treaty":
                    graphtype = GraphType.Treaty;
                    break;
            }

            //Prepare EventListTable (EventId & EventRate): EventIds are a list of numbers from 1 to 10,000 & EventRate = 1/10,000)
            Stopwatch MatrixGraphSW = new Stopwatch();
            MatrixGraphSW.Start();
            RP.ReferencePrepare();
            MatrixGraphSW.Stop();
            outputobject.PerformanceOutput.MatrixGraphBuildingTime = MatrixGraphSW.Elapsed.TotalMilliseconds;

            bool TestCaseStatus = true;
            return TestCaseStatus;
        }

        //2. Create a EventListTable that has eventid from 1 - 10,000. Since Sunny's Prototype doesn't get the event list from RL.
        public bool PrepareContract(long ContractId, MessageLog MsglogSb)
        {
            //Vector GU Generator for Reference
            HashSet<string> COL = new HashSet<string>(TestCase.Description.SubPeril);     
            //ExposureDataAdaptor expData = PDataAdap.GetExposureAdaptor(ContractId);
            //ISubPerilConfig subperilInfo = new RMSSubPerilConfig();
            //IRITEindexMapper mapper = new RITEmapper1(expData, subperilInfo);
            //IRITEindexMapper RiteIndexMapper = new RITEmapper1(expData, subperilInfo);
            //VectorGuGenerator = VectorGUInputFactory.GetGeneratorForContract(ContractId);  

            //GU Generator for FM
            COLCollection ColCollection = new COLCollection(COL);
            if (TestCase.InputType == GUInputType.Event)
            {
                GUgenerator = GUgeneratorFactory.GetGeneratorForContract(ContractId);
            }
            else
            {
                HDFMEventGen = PeriodgeneratorFactory.GetGeneratorForContract(ContractId);
            }
            
            ContractID = ContractId;
            EventListTable = new DataTable();
            DataColumn EventID = new DataColumn("EventID");
            EventListTable.Columns.Add(EventID);
            DataColumn EventRate = new DataColumn("RATE", typeof(double));
            EventListTable.Columns.Add(EventRate);
            int N = 99;

            for (int i = 0; i <= N; i++)
            {
                DataRow row = EventListTable.NewRow();
                row["EventID"] = i;
                row["RATE"] = 1.0 / N;
                EventListTable.Rows.Add(row);
            }

            bool PrepareContractStatus = true;
            return PrepareContractStatus;
        }

        //3. Simulate Random GUlosses for each event
        public bool GetGUlosses(long ContractId, int Event, MessageLog MsglogSb, out Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> GulossDictionary)
        {
            bool status = true;
            GulossDictionary = null;
            GuLossesDictionary = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();
            try
            {
                //Form GU Loss Dictionary for FM
                status = GUgenerator.GenerateRITELoss(Event);
                
                GulossDictionary = GUgenerator.GULosses;
                GuLossesDictionary = GUgenerator.GULosses;
                //Form GU Loss Vector for Reference
                //VectorGULosses = VectorGuGenerator.GenerateRITELoss(Event);

            }
            catch (Exception e)
            {
                MsglogSb.Log(e.ToString());
                status = false;
            }

            return status;
        }

        //3. Simulate Random GUlosses for each period
        public bool GetPeriodGUlosses(long ContractId, int Period, MessageLog MsglogSb, out List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PeriodGulossDictionary)
        {
            bool status = true;
            PeriodGULossDictionary = new List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>>();
            PeriodGulossDictionary = null;

            try
            {
                status = HDFMEventGen.GeneratePeriodLoss(Period);
                PeriodGULossDictionary = HDFMEventGen.PeriodLoss.EventLossList;
                PeriodGulossDictionary = PeriodGULossDictionary;
            }
            catch (Exception e)
            {
                MsglogSb.Log(e.ToString());
                status = false;
            }

            return status;
        }

        //4. Execute Sunny's Prototype to get the baseline payout
        public bool GetBaselinePayout(int EventId, MessageLog Msglogsb, Stopwatch MatrixExeSW)
        {
            bool status = true;
            try
            {
                MatrixExeSW.Start();
                if (TestCase.InputType == GUInputType.Period)
                {
                    outputobject.baselinePayout = RP.ExecutePeriod(Convert.ToInt32(ContractID), graphtype, PeriodGULossDictionary).TotalPayout;
                }
                else
                {
                    outputobject.baselinePayout = RP.Execute(Convert.ToInt32(ContractID), graphtype, GuLossesDictionary).TotalPayout;
                }
                MatrixExeSW.Stop();
            }
            catch (Exception ex)
            {
                Msglogsb.Log(ex.ToString());
                status = false;
            }
            return status;
        }

        public SpecialFeatureBaseline(PartitionData _pdat, TestCase _testcase,OutputObject _outputobject)
        {
            Pdat = _pdat;
            TestCase = _testcase;
            outputobject = _outputobject;
        }
    }
}




