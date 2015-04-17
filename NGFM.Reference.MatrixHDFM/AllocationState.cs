using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class AllocationState
    {
       
    }

    public class SimpleAllocationState
    {
        protected int size;
        public float[] SubjectLoss { get; private set; }
        public float[] Recoverable { get; private set; }
        public float[] Payout { get; private set; }       

        public SimpleAllocationState(int _size)
        {
            size = _size;
            SubjectLoss = new float[_size];
            Recoverable = new float[_size];
            Payout = new float[_size];
        }

        public void Reset()
        {
            Array.Clear(SubjectLoss, 0, size);
            Array.Clear(Recoverable, 0, size);
            Array.Clear(Payout, 0, size);
        }
    }

    public class LevelAllocationState : SimpleLevelState
    {
        public float[] Excess { get; private set; }
        public float[] Deductible { get; private set; }

        public LevelAllocationState(int _size)
            : base(_size)
        {
            Excess = new float[_size];
            Deductible = new float[_size];
        }

        public void Reset()
        {
            base.Reset();
            Array.Clear(Excess, 0, size);
            Array.Clear(Deductible, 0, size);
        }

    }


    public interface IAllocationState
    {





    }
}