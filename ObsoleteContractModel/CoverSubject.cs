using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class CoverSubject
    {
        public __Subject subject { get; set; }
        public List<string> subcover_list { get; set; }
    }
}
