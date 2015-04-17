using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class CauseOfLossConstraint
    {
        public HashSet<ICauseOfLoss> cause_of_loss_list { get; set; }
    }
}
