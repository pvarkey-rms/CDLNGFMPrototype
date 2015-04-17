using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class Header
    {
        public string contract_name { get; set; }
        public string currency { get; set; }
        public DeductibleOption options { get; set; }

    }
}
