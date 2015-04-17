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
using RMS.NGFMAutomation.LoopEvents;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using WorkFlowAutomationForProtobuffs;



namespace RMS.NGFMAutomation.LoopEvents
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RMS.NGFMAutomation.LoopEvents.Form1());
            
            
            //string InputXlPath = @"D:\Nina_Automation_Testing\InputSheet\NTA Test Case Input Nina Testing.xlsx";
            //string OutputXlPath = @"D:\Nina_Automation_Testing\OutputSheet";

            //RMS.NGFMAutomation.NGFMAutomation automation = new RMS.NGFMAutomation.NGFMAutomation(InputXlPath,OutputXlPath);
            //automation.GenerateAllEDSExtract();
            //automation.Run();

            //string InputXlPath = @"D:\Nina_Automation_Testing\InputSheet\NTA Test Case Input Nina Testing_LoopThroughEvents.xlsx";
            //string OutputXlPath = @"D:\Nina_Automation_Testing\OutputSheet";
            //string InputXlPath = @"D:\Nina_Automation_Testing\InputSheet\NGFM_InputSheetLatest.xlsx";

            //RMS.NGFMAutomation.LoopEvents.NGFMAutomation automation = new RMS.NGFMAutomation.LoopEvents.NGFMAutomation(InputXlPath, OutputXlPath);
            //automation.GenerateAllEDSExtract();
            //automation.Run();
            
        }

    }
}

