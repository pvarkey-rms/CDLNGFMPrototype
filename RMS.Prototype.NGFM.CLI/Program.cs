using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Test.CommandLineParsing;
using RMS.ContractObjectModel;
using RMS.Prototype.NGFM;

namespace RMS.Prototype.NGFM.CLI
{
    class Program
    {
        #region ** Input / Output **

        public static string InputFile { get; private set; }
        public static string InputDirectory { get; private set; }
        public static string GUInputSubdirectory = "GU_Input";
        public static string OutputSubdirectory = "Outputs";
        public static string OutputLogSubdirectory = "Log";
        //public string InputDirectory = @"D:\NGFM_work\NGFM_CLI_testdata";
        public static string GUInputFile { get; private set; }
        public static string GUInputDirectory { get; private set; }
        public static string OutputDirectory { get; private set; }
        public static string OutputLogDirectory { get; private set; }

        #endregion

        static bool IsInitialized;

        static NGFMPrototype NGFMPrototypeDLL_Handle;

        static void Main(string[] args)
        {
            ProcessCommandLineArgs(args);

            NGFMPrototypeDLL_Handle = new NGFMPrototype();

            if (InputFile != null)
                NGFMPrototypeDLL_Handle.UploadContractExposures(new string[] { InputFile });
            else
                NGFMPrototypeDLL_Handle.UploadContractExposures(Directory.GetFiles(InputDirectory, "*.dat"));

            Dictionary<long, Task> tasks = null;

            NGFMPrototypeDLL_Handle.BuildParsingContext();

            tasks = NGFMPrototypeDLL_Handle.Prepare_OLDAPI();

            if (tasks != null)
            {
                Task.WaitAll(tasks.Select(kv => kv.Value).ToArray());
            }

            NGFMPrototypeDLL_Handle.DisposeParsingContext();

            // Iterate through all events

            foreach (string _GUInputFile in Directory.GetFiles(GUInputDirectory, GUInputFile))
            {
                Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> dr = null;

                dr = NGFMPrototypeDLL_Handle.ReadDamageRatiosFromFile(_GUInputFile);

                NGFMPrototypeDLL_Handle.TransformDamageRatios(dr);

                Dictionary<long, ResultPosition> Results = NGFMPrototypeDLL_Handle.ProcessEvent((int)_GUInputFile.GetHashCode(),
                    NGFMPrototypeDLL_Handle.DamageRatiosPerSubPeril, true, 1);

                string InputPrefix = (InputFile != null) ? Path.GetFileNameWithoutExtension(InputFile) : "";

                string OutputFileName = "_" + InputPrefix + 
                    Path.GetFileNameWithoutExtension(_GUInputFile) + "_Output.csv";

                Utilities.WriteResultsToCSVFile(OutputDirectory + "\\" + OutputFileName, Results);
            }
        }

        public static void ProcessCommandLineArgs(string[] args)
        {
            var cmds = CommandLineDictionary.FromArguments(args, '-', '=');
            var arg = "";

            if ((cmds.Count == 0) || cmds.TryGetValue("h", out arg) || cmds.TryGetValue("help", out arg))
            {
                Console.WriteLine(@"

    ++++++++++++++++++++++++++++++++++++++
    +++++++ RMS.Prototype.NGFM.CLI +++++++
    ++++++++++++++++++++++++++++++++++++++

    Usage:
        RMS.Prototype.NGFM.CLI.exe
            -d=<string>     - directory or file path with input protobuf-format files
            -gu=<string>    - directory or file path with GU input csv files
            [-s=<string>]   - SQL server name, default '(local)'
            [-log]          - write messages to log-file in 'Log' sub directory
            [-h] or [-help] - show this helper 
            [-singlecore]          - execute in single Thread

    Command line example (single thread):
        RMS.Prototype.NGFM.CLI.exe -d='D:\NGFM_work\NGFM_CLI_testdata' -gu='D:\NGFM_work\NGFM_CLI_testdata'");

                IsInitialized = false;
                return;
            }

            #region Getting command line parameters

            //Input directory or input file name:
            InputFile = null;

            if (cmds.TryGetValue("d", out arg))
            {
                FileAttributes attr = File.GetAttributes(arg);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    InputDirectory = arg;
                }
                else
                {
                    InputDirectory = Path.GetDirectoryName(arg);
                    InputFile = arg;
                }
                OutputDirectory = IOUtilities.CreateSubdirectory(InputDirectory, OutputSubdirectory);
                OutputLogDirectory = IOUtilities.CreateSubdirectory(InputDirectory, OutputLogSubdirectory);
                GUInputDirectory = IOUtilities.CreateSubdirectory(InputDirectory, GUInputSubdirectory);
                GUInputFile = "*.csv";
            }

            if (cmds.TryGetValue("gu", out arg))
            {
                FileAttributes attr = File.GetAttributes(arg);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    GUInputDirectory = arg;
                }
                else
                {
                    GUInputDirectory = Path.GetDirectoryName(arg);
                    GUInputFile = Path.GetFileName(arg);
                }
            }

            //-----------------------------------

            #endregion

            IsInitialized = true;
        }
    }
}
