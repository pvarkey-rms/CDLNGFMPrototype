using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMS.ContractObjectModel;
using RMS.ContractGraphModel;
using RMS.Prototype.NGFM;

using Newtonsoft.Json;
//using Noesis.Javascript;
using Jint;
using JsonPrettyPrinterPlus;


namespace RMS.Prototype.NGFMUnitTest
{
    [TestClass]
    public class JISON_V8_Parser_UnitTests
    {
        /// <summary>
        /// This test method checks if files "grammar-ast.js" and "underscore.js" exist
        /// in output directory of RMS.Prototype.NGFM project.
        /// </summary>
        [TestMethod]
        public void Test_ConfigurationJISONJS()
        {
            var ngfmPrototype = new NGFMPrototype();

            CheckIfFileExists(ngfmPrototype.grammar_ast_js, "grammar-ast.js");
            CheckIfFileExists(ngfmPrototype.underscore_js, "underscore.js");

            ngfmPrototype.Dispose();
        }
        private void CheckIfFileExists(string fileName, string fileAlias = "")
        {
            string RMSPrototypeNGFM_configFile = @"RMS.Prototype.NGFM\App.config";
            Assert.IsNotNull(fileName, "You need to add existing item: \"{0}\" as a link to this test project.", RMSPrototypeNGFM_configFile);
            Assert.IsTrue(File.Exists(fileName), "File \"{0}\" which defined in \"{1}\" doesn't exist.", fileAlias, RMSPrototypeNGFM_configFile);
        }

        [TestMethod]
        public void TestJint()
        {
            var ngfmPrototype = new NGFMPrototype();

            Jint.Engine JintEngine = new Jint.Engine();
            JintEngine.Execute(System.IO.File.ReadAllText(ngfmPrototype.underscore_js));
            JintEngine.Execute(System.IO.File.ReadAllText(ngfmPrototype.grammar_ast_js));

            string strCDL = "Contract Declarations Subject is Loss to Acme by HU Inception is 5 Jun 2014 Expiration is 4 Jun 2015 PolicyNum is A5059-3 Covers 100% share of 10M Sublimits 10000 by Wind";

            strCDL = strCDL.Replace(System.Environment.NewLine, "     ");

            Dictionary<string, object> IR =
                (Dictionary<string, object>)(AsPOJO(JintEngine.Execute("grammarAst.parse('" + strCDL + "')").GetCompletionValue()));

            
        }

        private object AsPOJO(Jint.Native.JsValue JsValue)
        {
            if (JsValue.Type == Jint.Runtime.Types.Boolean)
                return Jint.Runtime.TypeConverter.ToBoolean(JsValue);

            else if (JsValue.Type == Jint.Runtime.Types.Number)
                return Jint.Runtime.TypeConverter.ToNumber(JsValue);

            else if (JsValue.Type == Jint.Runtime.Types.String)
                return Jint.Runtime.TypeConverter.ToString(JsValue);

            else // if (JsValue.Type == Jint.Runtime.Types.Object)
            {
                Jint.Native.Object.ObjectInstance AsObject =
                    JsValue.TryCast<Jint.Native.Object.ObjectInstance>();

                if (AsObject is Jint.Native.Array.ArrayInstance)
                {
                    int length = Jint.Runtime.TypeConverter.ToInt32(AsObject.Get("length"));
                    object[] array = new object[length];
                    for (int i = 0; i < length; i++)
                    {
                        array[i] = AsPOJO(AsObject.Get(i.ToString()));
                    }
                    return array;
                }

                else
                {
                    Dictionary<string, object> AsDictionary = new Dictionary<string, object>();

                    foreach (string Property in AsObject.Properties.Keys)
                    {
                        AsDictionary.Add(Property, AsPOJO(AsObject.Get(Property)));
                    }

                    return AsDictionary;
                }
            }
        }

        /// <summary>
        /// This test method checks correctness of the transformation from CDL-format (contract definition language) to JSON-format
        /// by function ParseCDLUsingJISONJS_GetIR(...) of class ContractsAndExposuresHarnessUnitTest from RMS.Prototype.NGFM project.
        /// </summary>
        [TestMethod]
        public void Test_ParseCDLUsingJISONJS()
        {
            string fileWithTestCases = "TestCases_ParseCDLtoJSON.txt";
            string testName = "";

            var ngfmPrototype = new NGFMPrototype();

            using (StreamReader sr = new StreamReader(fileWithTestCases))
            {
                string str, actualOut, expectedOut;
                var testIn = new StringBuilder();
                var testOut = new StringBuilder();

                bool flagIn = false;
                bool flagOut = false;

                while((str = sr.ReadLine()) != null)
                {
                    if (str.StartsWith("[Test"))
                    {
                        if (testName != "")
                        {
                            actualOut = ngfmPrototype.ParseCDLUsingJISONJS_GetIR(testIn.ToString());
                            expectedOut = testOut.ToString().Trim();
                            Assert.AreEqual(expectedOut, actualOut, "Test \"{0}\" is failed.", testName);
                            testIn.Clear();
                            testOut.Clear();
                            flagIn = flagOut = false;
                        }
                        testName = str;
                    }
                    else if (str.StartsWith("[CDL]"))
                    {
                        flagIn = true;
                        flagOut = false;
                    }
                    else if (str.StartsWith("[JSON]"))
                    {
                        flagIn = false;
                        flagOut = true;
                    }
                    else if (flagIn)
                    {
                        if (testIn.Length > 0)
                            testIn.Append("\r\n");
                        testIn.Append(str);
                    }
                    else if (flagOut)
                    {
                        if(testOut.Length > 0)
                            testOut.Append("\r\n");
                        testOut.Append(str);
                    }
                }

                if (flagOut)
                {
                    actualOut = ngfmPrototype.ParseCDLUsingJISONJS_GetIR(testIn.ToString());
                    expectedOut = testOut.ToString().Trim();
                    Assert.AreEqual(expectedOut, actualOut, "Test \"{0}\" is failed.", testName);
                }
            }

            ngfmPrototype.Dispose();
 
        }
    }
}
