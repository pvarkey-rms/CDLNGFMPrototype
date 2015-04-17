using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Prototype.NGFM
{
    public interface IJavaScriptHarness
    {        
        void Construct(params string[] JavascriptSourceFiles);

        void Destruct();

        object Execute(string functionInvocation);

        Dictionary<string, object> Parse(string strCDL);
    }
}
