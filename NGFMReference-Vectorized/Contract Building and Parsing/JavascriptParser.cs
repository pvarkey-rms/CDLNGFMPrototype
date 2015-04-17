using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;
using Noesis.Javascript;

namespace NGFMReference
{
    public class JavascriptParser : IDisposable
    {
        public string underscore_js { private set; get; }
        public string grammar_ast_js { private set; get; }
        private JavascriptContext JsContext;
        private bool contextNeedsDisposale;

        public JavascriptParser(JavascriptParserSettings settings)
        {
            Configure(settings);
            BuildJSContext();
            contextNeedsDisposale = true;
        }

        public JavascriptParser(JavascriptContext _JsContext)
        {
            JsContext = _JsContext;
            contextNeedsDisposale = false;
        }

        private void Configure(JavascriptParserSettings settings)
        {
            underscore_js = settings.UnderscoreJSFilename;

            grammar_ast_js = settings.GrammerASTFilename;
        }

        private void BuildJSContext()
        {
            JsContext = new JavascriptContext();
            JsContext.Run(System.IO.File.ReadAllText(underscore_js));
            JsContext.Run(System.IO.File.ReadAllText(grammar_ast_js));
        }

        private void DisposeJSContext()
        {
            if (contextNeedsDisposale)
            {
                JsContext.TerminateExecution();
                JsContext.Dispose();
            }
        }

        public Dictionary<string, object> ParseCDL(string strCDL)
        {
            //BuildJSContext();
            // prepare cdl (change newlines to spaces)
            strCDL = strCDL.Replace(System.Environment.NewLine, "     ");
            Dictionary<string, object> JSON_IR =
                (Dictionary<string, object>)(JsContext.Run("grammarAst.parse('" + strCDL + "')"));
            //DisposeJSContext();
            return JSON_IR;
        }

        #region IDisposable Override

        // Flag: Has Dispose already been called? 
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here. 
                DisposeJSContext();
            }

            // Free any unmanaged objects here. 
            
            disposed = true;
        }

        ~JavascriptParser()
        {
            Dispose(false);
        }


        #endregion

    }


    public class JavascriptParserSettings
    {
        public string UnderscoreJSFilename { get; set; }
        public string GrammerASTFilename { get; set; }

        public JavascriptParserSettings(string _underscoreJSFilename, string _grammerASTFilename)
        {
            UnderscoreJSFilename = _underscoreJSFilename;
            GrammerASTFilename = _grammerASTFilename;
        }
    }
}
