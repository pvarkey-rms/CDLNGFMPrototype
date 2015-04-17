using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NGFM.Reference.MatrixHDFM
{
    //public class MatrixGraphAllocation2
    //{
    //    private IExecutableMatrixGraph Graph;
    //    private GraphState graphState;

    //    public MatrixGraphAllocation(IExecutableMatrixGraph _graph, GraphState _graphState)
    //    {
    //        Graph = _graph;
    //        graphState = _graphState;
    //    }

    //    public void Allocate_UsingFor()
    //    {
    //        LevelState[] allLevelState = Graph.AllLevelState;

    //        LevelAllocationState[] allLevelAllocationState = new LevelAllocationState[Graph.NumOfLevels];

    //        for (int i = 0; i < Graph.NumOfLevels - 1; i++)
    //        {
    //            //start from highest level, allocate down                
    //            //need to get children infor
    //            ILevelInfo cLevelInfo = Graph.GetTermLevelInfo(i + 1);
    //            int[] cPartition = cLevelInfo.GetAggregationPartitions();
    //            int[] cPartitionCount = cLevelInfo.GetAggregationCount();
    //            float[] cSubjectLoss = allLevelState[i + 1].SubjectLoss;
    //            float[] cExcess = allLevelState[i + 1].Excess;
    //            float[] cDed = allLevelState[i + 1].Deductible;
    //            float[] cR = allLevelState[i + 1].Recoverable;

    //            allLevelAllocationState[i + 1] = new LevelAllocationState(cLevelInfo.LevelSize); //allocation is for child

    //            ILevelInfo pLevelInfo = Graph.GetTermLevelInfo(i);
    //            float[] pPayout = allLevelState[i].Recoverable;

    //            float[] allocatedD = new float[cDed.Length];
    //            float[] allocatedRPreAdj = new float[cDed.Length];
    //            float[] allocatedRFinal = new float[cDed.Length];

    //            int kk = 0;
    //            for (int p = 0; p < pLevelInfo.LevelSize; p++)   //for each parent node
    //            {
    //                if (pPayout[p] == 0)
    //                {
    //                    for (int c = kk; c < cPartition[p]; c++)  //for each child for this parent Node p
    //                    {
    //                        allocatedRFinal[c] = 0;
    //                    }
    //                    kk += cPartition[p];
    //                    continue;
    //                }

    //                float allocatedRSummed = 0;
    //                float originalRSummed = 0;

    //                float rRatio = allLevelState[i].AllocateRatioR[p];
    //                float dRatio = allLevelState[i].AllocateRatioD[p];

    //                for (int c = kk; c < cPartition[p]; c++)
    //                {
    //                    allocatedD[c] = dRatio * cDed[c];
    //                    allocatedRPreAdj[c] = cSubjectLoss[c] - cExcess[c] - allocatedD[c];
    //                    allocatedRPreAdj[c] *= rRatio;
    //                    allocatedRSummed += allocatedRPreAdj[c];
    //                    originalRSummed += cR[c];
    //                }

    //                //when this parent j is done
    //                if (allocatedRSummed == 0)
    //                {
    //                    for (int c = kk; c < cPartition[p]; c++)
    //                    {
    //                        allocatedRFinal[c] = pPayout[p] * cR[c] / originalRSummed;
    //                    }

    //                }
    //                else
    //                {
    //                    for (int c = kk; c < cPartition[p]; c++)
    //                    {
    //                        allocatedRFinal[c] = pPayout[p] * allocatedRPreAdj[c] / allocatedRSummed;
    //                    }
    //                }
    //                kk += cPartition[p];
    //            }
    //        }
    //    }

    //    public void Allocate_UsingArray()
    //    {
    //        LevelState[] allLevelState = Graph.AllLevelState;

    //        LevelAllocationState[] allLevelAllocationState = new LevelAllocationState[Graph.NumOfLevels];

    //        for (int i = 0; i < Graph.NumOfLevels - 1; i++)
    //        {
    //            //start from highest level, allocate down
    //            //need to get children infor
    //            ILevelInfo cLevelInfo = Graph.GetTermLevelInfo(i + 1);
    //            int[] cPartition = cLevelInfo.GetAggregationPartitions();
    //            int[] cPartitionCount = cLevelInfo.GetAggregationCount();
    //            float[] cSubjectLoss = allLevelState[i + 1].SubjectLoss;
    //            float[] cExcess = allLevelState[i + 1].Excess;
    //            float[] cDed = allLevelState[i + 1].Deductible;
    //            float[] cR = allLevelState[i + 1].Recoverable;

    //            allLevelAllocationState[i + 1] = new LevelAllocationState(cLevelInfo.LevelSize); //allocation is for child

    //            //propagte Parent Level infor, so it will have the same length as Children Level
    //            float[] RRatio = Utilities.ArrayPropagate(allLevelState[i].AllocateRatioR, cPartitionCount);
    //            float[] DRatio = Utilities.ArrayPropagate(allLevelState[i].AllocateRatioD, cPartitionCount);
    //            float[] pPayout = Utilities.ArrayPropagate(allLevelState[i].Recoverable, cPartitionCount);

    //            float[] allocatedD = Utilities.ArrayMultiplication(DRatio, cDed);
    //            float[] allocatedRPreAdj = Utilities.ThreeArraySubtraction(cSubjectLoss, cExcess, allocatedD);
    //            allocatedRPreAdj = Utilities.ArrayMultiplication(RRatio, allocatedRPreAdj);

    //            //final adjustment
    //            float[] allocatedRSummed = Utilities.SumArrayByPartition(allocatedRPreAdj, cPartition);
    //            float[] allocatedRSummedSignal = new float[allocatedRSummed.Length];

    //            for (int j = 0; j < allocatedRSummed.Length; j++)
    //            {
    //                allocatedRSummedSignal[j] = allocatedRSummed[j];

    //                if (allocatedRSummed[j] > 0)
    //                    allocatedRSummedSignal[j] = 1;
    //            }

    //            float[] m1 = Utilities.ArrayPropagate(allocatedRSummedSignal, cPartitionCount);
    //            float[] m2 = Utilities.ConstantSubtractArray(1, m1);
    //            float[] temp = Utilities.ArrayAddition(Utilities.ArrayMultiplication(m1, cR), Utilities.ArrayMultiplication(m2, allocatedRPreAdj));

    //            float[] temp2 = Utilities.SumArrayByPartition(temp, cPartition);
    //            temp2 = Utilities.ArrayDivision(allLevelState[i].Recoverable, temp2);
    //            float[] adjRRatio = Utilities.ArrayPropagate(temp2, cPartitionCount);
    //            float[] allocatedR = Utilities.ArrayMultiplication(adjRRatio, temp);
    //        }
    //    }
    //}

    public class MatrixGraphAllocation
    {
        private IExecutableMatrixGraph Graph;
        private IGraphState graphState;

        ILevelState ParentTermLevelState {get;set;}
        ILevelState ChildrenTermLevelState {get;set;}
        ISimpleLevelState ChildrenLevelAriteState { get; set;}
        ILevelNodeAggInfo ParentLevelAggInfo {get;set;}
        ILevelNodeAggInfo ChildrenLevelAggInfo {get;set;}
        ILevelAtomicRITEInfo ChildrenLevelAriteInfo {get;set;}

        #region Constructor
        public MatrixGraphAllocation(IExecutableMatrixGraph _graph, IGraphState _graphState)
        {
            Graph = _graph;
            graphState = _graphState;
        }
        #endregion

        public void Run()
        {
            //1.Loop through the levels and allocate till the last Term Level
            int LastLevelinTermLevels = Graph.NumOfTermLevels-1;
            for (int i =0; i < LastLevelinTermLevels; i ++)
            {
                ParentTermLevelState = graphState.GetTermLevelState(i);
                ChildrenTermLevelState = graphState.GetTermLevelState(i + 1);
                ChildrenLevelAriteState = graphState.GetARITELevelState(i+1);
                ParentLevelAggInfo = Graph.GetNodeAggInfo(i);
                ChildrenLevelAggInfo = Graph.GetNodeAggInfo(i+1);
                ChildrenLevelAriteInfo = Graph.GetAtomicRITEInfo(i+1);

                AllocateToOneTermLevel();
            }

            //2.Allocate to the lowest level (All Atomic Rites)
            ParentTermLevelState = graphState.GetTermLevelState(LastLevelinTermLevels);
            ChildrenLevelAriteState = graphState.GetARITELevelState(LastLevelinTermLevels + 1);
            ChildrenLevelAriteInfo = Graph.GetAtomicRITEInfo(LastLevelinTermLevels + 1);

            AllocateToLowestLevel();            
        }

        private void AllocateToOneTermLevel()
        {
            int NumOfArites = ChildrenLevelAriteState.GetSubjectLoss().Length;
            if (NumOfArites != 0)
            {
                AllocateToTermAndArite();
            }
            else
            {
                AllocateToTermOnly();
            }
        }

        private void AllocateToTermOnly()
        {
            float[] ParentR = ParentTermLevelState.GetRecoverable();
            float[] ChildrenR = ChildrenTermLevelState.GetRecoverable();
            float[] ChildrenD = ChildrenTermLevelState.GetDeductible();
            float[] ChildrenS = ChildrenTermLevelState.GetSubjectLoss();
            float[] ChildrenX = ChildrenTermLevelState.GetExcess();
            int[] ChildrenPartition = ChildrenLevelAggInfo.GetNodeAggregationPartitions();
            int NumOfParents = ParentR.Length;        

            float[] rRatio = ParentTermLevelState.GetAllocateRatioR();
            float[] dRatio = ParentTermLevelState.GetAllocateRatioD();

            float allocatedRSummed;
            float originalRSummed;
            float temp;
            float DRatio;
            float RRatio;

            //Allocation one parent level to its children level
            int StartPosition = 0;
            for (int i = 0; i<NumOfParents; i ++)
            {
                int EndPosition = ChildrenPartition[i];
                if(ParentR[i]==0)
                {
                    Array.Clear(ChildrenR, StartPosition, EndPosition - StartPosition);
                    StartPosition ++;
                    continue;
                }

                allocatedRSummed = 0;
                originalRSummed = 0;
                temp = 0;
                DRatio = dRatio[i];
                RRatio = rRatio[i];

                for (int c = StartPosition; c < EndPosition; c++)
                {                  
                    //ChildrenD[c] = dRatio[i] * ChildrenD[c];
                    //originalRSummed += ChildrenR[c];
                    //ChildrenR[c] = (ChildrenS[c] - ChildrenX[c] - ChildrenD[c]) * rRatio[i];
                    //ChildrenD[c] = ChildrenS[c] - ChildrenX[c] - ChildrenR[c];
                    //allocatedRSummed += ChildrenR[c];
                    
                    temp = ChildrenS[c] - ChildrenX[c];
                    ChildrenD[c] = DRatio * ChildrenD[c];
                    originalRSummed += ChildrenR[c];
                    ChildrenR[c] = (temp - ChildrenD[c]) * RRatio;
                    ChildrenD[c] = temp - ChildrenR[c];
                    allocatedRSummed += ChildrenR[c];
                }               

                //when this parent j is done                
                if (allocatedRSummed == 0)
                {
                    for (int c = StartPosition; c < EndPosition; c++)
                    {
                        ChildrenR[c] = ParentR[i] * ChildrenR[c] / originalRSummed;
                    }
                }
                else
                {
                    //Nina: Create temp to improve performance: so we won't be calling and calculating
                    //ParentR[i] / allocated many many time in for loop;
                    temp = ParentR[i] / allocatedRSummed;
                    for (int c = StartPosition; c < EndPosition; c++)
                    {
                        ChildrenR[c] = temp * ChildrenR[c];
                    }
                }
                StartPosition = EndPosition; 
            }
        }

        private void AllocateToTermAndArite()
        {
            float[] ParentR = ParentTermLevelState.GetRecoverable();
            float[] ChildrenR = ChildrenTermLevelState.GetRecoverable();
            float[] ChildrenD = ChildrenTermLevelState.GetDeductible();
            float[] ChildrenS = ChildrenTermLevelState.GetSubjectLoss();
            float[] ChildrenX = ChildrenTermLevelState.GetExcess();
            int[] ChildrenPartition = ChildrenLevelAggInfo.GetNodeAggregationPartitions();
            int NumOfParents = ParentR.Length;

            float[] ARiteR = ChildrenLevelAriteState.GetRecoverable();
            float[] ARiteS = ChildrenLevelAriteState.GetSubjectLoss();
            int[] AritePartition = ChildrenLevelAriteInfo.GetARiteAggregationPartitions();

            float[] rRatio = ParentTermLevelState.GetAllocateRatioR();
            float[] dRatio = ParentTermLevelState.GetAllocateRatioD();
            float allocatedRSummed;
            float originalRSummed;
            float temp;
            float DRatio;
            float RRatio;

            //Allocation one parent level to its children level
            int StartPosition = 0;
            int AriteStartPosition = 0;
            for (int i = 0; i < NumOfParents; i++)
            {
                int EndPosition = ChildrenPartition[i];
                int AriteEndPosition = AritePartition[i];
                if (ParentR[i] == 0)
                {
                    Array.Clear(ChildrenR, StartPosition, EndPosition - StartPosition);
                    Array.Clear(ARiteR, AriteStartPosition, AriteEndPosition - AriteStartPosition);
                    StartPosition = EndPosition;
                    AriteStartPosition = AriteEndPosition;
                    continue;
                }

                allocatedRSummed = 0;
                originalRSummed = 0;
                temp = 0;
                DRatio = dRatio[i];
                RRatio = rRatio[i];

                for (int c = StartPosition; c <EndPosition ; c++)
                {
                    //ChildrenD[c] = dRatio[i] * ChildrenD[c];
                    //originalRSummed += ChildrenR[c];
                    //ChildrenR[c] = (ChildrenS[c] - ChildrenX[c] - ChildrenD[c]) * rRatio[i];
                    //ChildrenD[c] = ChildrenS[c] - ChildrenX[c] - ChildrenR[c];
                    //allocatedRSummed += ChildrenR[c];
                    temp = ChildrenS[c] - ChildrenX[c];
                    ChildrenD[c] = DRatio * ChildrenD[c];
                    originalRSummed += ChildrenR[c];
                    ChildrenR[c] = (temp - ChildrenD[c]) * RRatio;
                    ChildrenD[c] = temp - ChildrenR[c];
                    allocatedRSummed += ChildrenR[c];
                }

                for (int j = AriteStartPosition; j < AriteEndPosition; j++)
                {
                    //originalRSummed += ARiteR[j];
                    originalRSummed += ARiteS[j];
                    ARiteR[j] = ARiteS[j] * RRatio;
                    allocatedRSummed += ARiteR[j];
                }


                //when this parent j is done
                if (allocatedRSummed == 0)
                {
                    for (int c = StartPosition; c < EndPosition; c++)
                    {
                        ChildrenR[c] = ParentR[i] * ChildrenR[c] / originalRSummed;

                    }
                    for (int j = AriteStartPosition; j < AriteEndPosition; j++)
                    {
                        ARiteR[j] = ParentR[i] * ARiteR[j] / originalRSummed;
                    }
                }
                else
                {
                    //Nina: create temp to improve performance so that ParentR[i] / allocatedRSummed won't 
                    //be calculated many times in for loop below;
                    temp = ParentR[i] / allocatedRSummed;
                    for (int c = StartPosition; c < EndPosition; c++)
                    {
                        //ChildrenR[c] = ParentR[i] * ChildrenR[c] / allocatedRSummed;
                        ChildrenR[c] = temp * ChildrenR[c];
                    }
                    for (int j = AriteStartPosition; j < AriteEndPosition; j++)
                    {
                        //ARiteR[j] = ParentR[i] * ARiteR[j] / allocatedRSummed;
                        ARiteR[j] = temp * ARiteR[j];
                    }
                }
                StartPosition = EndPosition;
                AriteStartPosition = AriteEndPosition;
            }
        }

        private void AllocateToLowestLevel()
        {
            float[] ParentR = ParentTermLevelState.GetRecoverable();
            float[] ParentS = ParentTermLevelState.GetSubjectLoss();
            float[] ChildrenS = ChildrenLevelAriteState.GetSubjectLoss();
            float[] ChildrenR = ChildrenLevelAriteState.GetRecoverable();
            int[] ChildrenPartition = ChildrenLevelAriteInfo.GetARiteAggregationPartitions();
            int NumOfParents = ParentR.Length;
            float AllocationRatio;
            int EndPosition;

            int StartPosition = 0;
            for (int i = 0; i < NumOfParents; i++)
            {
                EndPosition = ChildrenPartition[i];
                if (ParentR[i] == 0)
                {
                    Array.Clear(ChildrenR, StartPosition, EndPosition - StartPosition);
                    StartPosition = EndPosition;
                    continue;
                }

                //ParentS[i] is equivalent to the sum of the Rs from children below;
                //AllocationRatio = ParentR[i]/ ParentS[i]; //this parent Subject has already co-op with factors, but the Children are not
                float lossTotal = 0;

                for (int j = StartPosition; j < EndPosition; j++)
                {
                    //ChildrenR[j] = ParentR[i] * (ChildrenS[j] / ParentS[i]);
                    //ChildrenR[j] = AllocationRatio * ChildrenS[j];
                    lossTotal += ChildrenS[j];
                }
                for (int j = StartPosition; j < EndPosition; j++)
                {                    
                    ChildrenR[j] = ParentR[i]*ChildrenS[j]/lossTotal;                    
                }
                //TODO: to merge the two for loops to one

                StartPosition = EndPosition;
            }
        }
    }
}
