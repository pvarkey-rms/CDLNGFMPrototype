using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public enum TermNodeType
    {
        OccurrenceDeductible,
        OccurrenceLimit,
        EachRiskLimit,
        Pay,
        Payf
    }

    public class CoverTerm
    {
        public double attachment { get; set; }
        public double limit{get; set;}
        public bool each_occ{get; set;}
        public bool each_risk{get; set;}
        public TermNodeType term_node_type { get; set; }
    }
}