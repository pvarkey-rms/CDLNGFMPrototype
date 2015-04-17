using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class SDSpec
    {
        public int tid {get;set;}
        public __Subject subject {get;set;}
        public List<TermObject> term_list {get;set;}
    }
}
