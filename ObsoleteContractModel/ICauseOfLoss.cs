using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public interface ICauseOfLoss
    {
        string ExtnId { get; set; }
        string Name { get; set; }

        bool IsSubsetTo(ICauseOfLoss col);
        bool IsProperSubsetTo(ICauseOfLoss col);
    }
}
