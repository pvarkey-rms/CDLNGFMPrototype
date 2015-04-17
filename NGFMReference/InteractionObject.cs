using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    class InteractionObject
    {
        public double DedFromChildren { get; set; }
        public double LimitFromChildren { get; set; }  //not used
        public double ExcessFromChildren { get; set; }
        public double LargestDedFromChildren { get; set; }

        public InteractionObject()
        {
            DedFromChildren = 0;
            LimitFromChildren = 0;  //not used
            ExcessFromChildren = 0;
            LargestDedFromChildren = 0;
        }

        public void UpdateInterObjState(LossState inputLossesState)
        {
            DedFromChildren += inputLossesState.D;
            ExcessFromChildren += inputLossesState.X;
            LargestDedFromChildren = Math.Max(LargestDedFromChildren, inputLossesState.D);
        }

        public void UpdateInterObjStateForARITE(LossState inputLossState)
        {
            DedFromChildren += inputLossState.D;
            ExcessFromChildren += inputLossState.X;
            LargestDedFromChildren = Math.Max(LargestDedFromChildren, inputLossState.D);
        }
    }
}
