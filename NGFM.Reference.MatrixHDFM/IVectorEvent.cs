using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public interface IVectorEvent
    {
        float[] LossVector { get; }
        float[] Factors { get; }
        uint[] TimeStamps { get; }
        DateTime EventDate { get; }
    }
    
}
