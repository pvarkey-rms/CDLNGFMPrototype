using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Noesis.Javascript;

namespace RMS.Prototype.NGFM
{
    class NoesisJsHarness : IJavaScriptHarness
    {
        private JavascriptContext JsContext;

        public void Construct(params string[] JavascriptSourceFiles)
        {
            JsContext = new JavascriptContext();
            foreach (string JavascriptSourceFile in JavascriptSourceFiles)
            {
                JsContext.Run(System.IO.File.ReadAllText(JavascriptSourceFile));
            }
        }

        public void Destruct()
        {
            JsContext.TerminateExecution();
            JsContext.Dispose();
        }

        public object Execute(string FunctionInvocation)
        {
            return JsContext.Run(FunctionInvocation);
        }

        public Dictionary<string, object> Parse(string strCDL)
        {
            return (Dictionary<string, object>)Execute("grammarAst.parse('" + strCDL + "')");
        }
    }
}
