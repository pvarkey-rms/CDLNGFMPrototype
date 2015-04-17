using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jurassic;

namespace RMS.Prototype.NGFM
{
    class JurassicHarness : IJavaScriptHarness
    {
        private Jurassic.ScriptEngine JurassicEngine;

        public void Construct(params string[] JavascriptSourceFiles)
        {
            JurassicEngine = new Jurassic.ScriptEngine();
            foreach (string JavascriptSourceFile in JavascriptSourceFiles)
            {
                JurassicEngine.ExecuteFile(JavascriptSourceFile);
            }
            JurassicEngine.Execute("var Parse = function (strCDL) " +
                    "{ return grammarAst.parse(strCDL); };");
        }

        public void Destruct()
        {
        }

        public object Execute(string FunctionInvocation)
        {
            return JurassicEngine.CallGlobalFunction(FunctionInvocation);
        }

        public Dictionary<string, object> Parse(string strCDL)
        {
            return (Dictionary<string, object>)
                JurassicEngine.CallGlobalFunction("Parse", strCDL);
        }
    }
}
