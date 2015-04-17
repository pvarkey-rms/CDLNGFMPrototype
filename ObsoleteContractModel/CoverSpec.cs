using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class CoverSpec
    {
        public string cover_id { get; set; }
        public double share { get; set; }
        public CoverSubject cover_subject { get; set; }
        public List<CoverTerm> cover_term_list { get; set; }
    }
}
