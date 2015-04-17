using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class GraphState : IGraphState
    {
        private Dictionary<int, LevelState> termlevelstates;
        private Dictionary<int, SimpleLevelState> ARITElevelstates;
        private CoverState TotalCoverState;
        private CoverState lowestCoverLevelResiduleState;
        private CoverState lowestCoverLevelAriteState;
 
        //for Cover Graph allocation
        private Dictionary<int, CoverAllocationRatioState> coverLevelAllocationRatioStates;
        private CoverAllocationRatioState lowestCoverLevelAriteAllocationRatio;
        private CoverAllocationRatioState lowestCoverLevelResidualAllocationRatio;
        
        public GraphState(IExecutableMatrixGraph graph)
        {
            if (graph.NumOfLevels != 0)
            {
                termlevelstates = new Dictionary<int, LevelState>();
                ARITElevelstates = new Dictionary<int, SimpleLevelState>();

                int NumOfLevels = graph.NumOfLevels;

                //Add both ARITE level state and Term/Node Level State for Upper levels
                for (int level = 0; level < NumOfLevels - 1; level++)
                {
                    int NumOfLevelNodes = graph.GetNodeAggInfo(level).NumOfNodes; //actually this is the number of terms                            
                    int NumOfARites = graph.GetAtomicRITEInfo(level).NumOfARITEs;
                    termlevelstates.Add(level, new LevelState(NumOfLevelNodes));
                    ARITElevelstates.Add(level, new SimpleLevelState(NumOfARites));
                }

                //Add ARITE level state for lowest level, don't add Term/Node Level State
                int lowestlevel = graph.LowestLevel;
                int NumOfLowestLevelARites = graph.GetAtomicRITEInfo(lowestlevel).NumOfARITEs;
                ARITElevelstates.Add(graph.LowestLevel, new SimpleLevelState(NumOfLowestLevelARites));
            }

            #region delete later: old version: create one loss state per level
            //For Covers:

            //int NumOfCoverLevels = graph.NumOfLevelsForCover;
            //int LowestCoverLevel = graph.LowestCoverLevel;

            //for (int coverlevel = LowestCoverLevel; coverlevel <= NumOfCoverLevels; coverlevel++)
            //{
            //    int NumOfCoverLevelNodes = graph.GetCoverNodeAggInfo(coverlevel).NumOfNodes;
            //    coverlevelstate.Add(coverlevel, new CoverLevelState(NumOfCoverLevelNodes));
            //}

            ////Add level state for lowest cover level.
            //int NumOfResidules = graph.GetCoverResiduleInfo(LowestCoverLevel).NumOfResidules;
            //int NumOfArites = graph.GetCoverAriteInfo(LowestCoverLevel).NumOfARITEs;
            //lowestCoverLevelResiduleState = new CoverLevelState(NumOfResidules);
            //lowestCoverLevelAriteState = new CoverLevelState(NumOfArites);
            #endregion

            //For Covers:
            int leafCoverLevel = graph.LowestCoverLevel + 1;
            int TotalNumOfCovers = 0;
            int TopLevel = graph.NumOfCoverLevels + leafCoverLevel-1;
            //graph.levelLenghtArray = new int[NumofLevels];
            coverLevelAllocationRatioStates = new Dictionary<int, CoverAllocationRatioState>();

            for (int i = leafCoverLevel; i <= TopLevel; i++)
            {
                int NumOfNodes = graph.GetCoverNodeAggInfo(i).NumOfNodes;
                TotalNumOfCovers += NumOfNodes;
                //int j = 0;
                //graph.levelLenghtArray[j] = NumOfNodes;
                //j++;   
 
                //Allocation state is Per Level, due to the overlap that
                //the same cover will have to keep two ratios 
                //coverLevelAllocationRatioStates.Add(i, new CoverAllocationRatioState(NumOfNodes));  //this will not work, ratio should be same length as the ChildMap
                coverLevelAllocationRatioStates.Add(i, new CoverAllocationRatioState(graph.GetCoverNodeAggInfo(i).GetChildrenMap().Count()));                
            }
            TotalCoverState = new CoverState(TotalNumOfCovers);

            lowestCoverLevelResiduleState = new CoverState(graph.GetCoverResiduleInfo().NumOfResidules);
            lowestCoverLevelAriteState = new CoverState(graph.GetCoverAriteInfo().NumOfARITEs);
            lowestCoverLevelAriteAllocationRatio = new CoverAllocationRatioState(graph.GetCoverAriteInfo().NumOfARITEs);
            lowestCoverLevelResidualAllocationRatio = new CoverAllocationRatioState(graph.GetCoverResiduleInfo().NumOfResidules);
        }

        #region ILossState override
        public ISimpleLevelState GetARITELevelState(int level)
        {
            if (ARITElevelstates.ContainsKey(level))
                return ARITElevelstates[level];
            else
                throw new ArgumentOutOfRangeException("This Graph State does not have an aRITE level at level: " + level);
        }

        public ILevelState GetTermLevelState(int level)
        {
            if (termlevelstates.ContainsKey(level))
                return termlevelstates[level];
            else
                throw new ArgumentOutOfRangeException("This Graph State does not have an term level at level: " + level);
        }

        public ICoverState GetLowestCoverLevelResiduleState() { return lowestCoverLevelResiduleState; }

        public ICoverState GetLowestCoverLevelAriteState() { return lowestCoverLevelAriteState; }

        public ICoverState GetTotalCoverState() { return TotalCoverState; }

        public ICoverAllocationState GetCoverLevelAllocationRatioState(int level)
        {
            if (coverLevelAllocationRatioStates.ContainsKey(level))
                return coverLevelAllocationRatioStates[level];
            else
                throw new ArgumentOutOfRangeException("This Graph State does not have an cover level at level: " + level);
        }

        public ICoverAllocationState GetLowestCoverLevelAriteAllocationRatio() {return lowestCoverLevelAriteAllocationRatio;}

        public ICoverAllocationState GetLowestCoverLevelResidualAllocationRatio() {return lowestCoverLevelResidualAllocationRatio;}

        #endregion
    }

    public class SimpleLevelState : ISimpleLevelState
    {
        protected int size;
        protected float[] SubjectLoss;
        protected float[] Recoverable;
        protected float[] AllocateRatioR;
        protected float[] AllocateRatioD;
        protected float[] Factors;

        public SimpleLevelState(int _size)
        {
            size = _size;
            SubjectLoss = new float[_size];
            Recoverable = new float[_size];
            AllocateRatioR = new float[_size];
            AllocateRatioD = new float[_size];
            Factors = new float[_size];
        }

        public virtual void Reset()
        {
            Array.Clear(SubjectLoss, 0, size);
            Array.Clear(Recoverable, 0, size);
            Array.Clear(AllocateRatioR, 0, size);
            Array.Clear(AllocateRatioD, 0, size);
        }

        #region override ISimpleLevelState

        public float[] GetSubjectLoss() { return SubjectLoss; }
        public float[] GetRecoverable() { return Recoverable; }
        public float[] GetAllocateRatioR() { return AllocateRatioR; }
        public float[] GetAllocateRatioD() { return AllocateRatioD; }
        public float[] GetFactors() { return Factors; }

        #endregion 
    }

    public class LevelState : SimpleLevelState, ILevelState
    {
        protected float[] Excess;
        protected float[] Deductible;


        public LevelState(int _size):base(_size)
        {
            Excess = new float[_size];
            Deductible = new float[_size];
        }

        public override void Reset()
        {
            base.Reset();
            Array.Clear(Excess, 0, size);
            Array.Clear(Deductible, 0, size);
        }

        #region override ILevelState

        public float[] GetExcess() { return Excess; }
        public float[] GetDeductible() { return Deductible; }

        #endregion

    }

    public interface ISimpleLevelState
    {
        float[] GetSubjectLoss();
        float[] GetRecoverable();
        float[] GetAllocateRatioR();
        float[] GetAllocateRatioD();
        float[] GetFactors();
    }

    public interface ILevelState : ISimpleLevelState
    {
        float[] GetExcess();
        float[] GetDeductible();
    }

    public interface IGraphState
    {
        ISimpleLevelState GetARITELevelState(int level);

        ILevelState GetTermLevelState(int level);

        ICoverState GetLowestCoverLevelResiduleState();

        ICoverState GetLowestCoverLevelAriteState();

        ICoverState GetTotalCoverState();

        ICoverAllocationState GetCoverLevelAllocationRatioState(int level);
        ICoverAllocationState GetLowestCoverLevelAriteAllocationRatio();
        ICoverAllocationState GetLowestCoverLevelResidualAllocationRatio();
    }

    //For Covers:
    public interface ICoverState : ISimpleCoverState
    {
        float[] GetPayout();
        float[] GetAllocatedPayout();
        int GetSize();
    }

    public interface ISimpleCoverState
    {
        float[] GetSubjectLoss();
        int GetSize();
        float[] GetFactors();
    }

    public interface ICoverAllocationState
    {
        float[] GetCoverAllocationRatioP();
    }

    public class SimpleCoverState : ISimpleCoverState
    {
        protected int size;
        protected float[] SubjectLoss;
        protected float[] Factors;

        public SimpleCoverState (int _size)
        {
            size =_size;
            SubjectLoss = new float[_size];
            Factors = new float[_size];
        }

        public virtual void Reset()
        {
            Array.Clear(SubjectLoss, 0, size);
        }

        public int GetSize() { return size;}
        public float[] GetFactors()
        {
            return Factors;
        }

        #region Override for ISimpleCoverLevelState
        public float[] GetSubjectLoss() { return SubjectLoss; }
        #endregion

    }

    public class CoverState : SimpleCoverState, ICoverState
    {
        protected float[] Payout;
        protected float[] AllocatedPayout;        
        
        public CoverState(int _size) :base(_size)
        {
            Payout = new float[_size];
            AllocatedPayout = new float[_size];
        }

        public override void Reset()
        {
            base.Reset();
            Array.Clear(Payout, 0, size);
        }

        public float[] GetPayout() { return Payout; }
        public float[] GetAllocatedPayout() { return AllocatedPayout; }
        public float[] GetFactors()
        {
            return Factors;
        }
    }

    public class CoverAllocationRatioState : ICoverAllocationState
    {
        protected float[] AllocationRatioP;
        protected int Size;

        public CoverAllocationRatioState(int _size)
        {
            Size = _size;
            AllocationRatioP = new float[Size];
        }

        public void Reset()
        {   
            Array.Clear(AllocationRatioP, 0, Size);
        }

        public float[] GetCoverAllocationRatioP() { return AllocationRatioP; }
    }
}
