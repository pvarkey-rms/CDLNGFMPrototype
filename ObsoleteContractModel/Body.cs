using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class Body
    {
        public __Subject contract_subject { get; set; }
        public Covers covers { get; set; }
        public SubLimits sublimits { get; set; }
        public DeductibleSpecs deductibles { get; set; }

    }
}
