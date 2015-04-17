using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public enum AmountType
    {
        PERCENTRCV_COVERED,
        PERCENTRCV_AFFECTED,
        CONSTANT
    }
    public class Amount
    {
        public double value { get; set; }
        public AmountType amountType {get;set;}
    }
}
