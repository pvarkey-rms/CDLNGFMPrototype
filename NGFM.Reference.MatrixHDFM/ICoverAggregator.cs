using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public interface ICoverAggregator
    {
        void AggregateLowestLevel(ICoverLevelResiduleInfo ResiduleInfo, ICoverLevelTermAriteInfo AriteInfo, ICoverState lowestLevelResiduleState,
                                   ICoverState lowestLevelAriteState, ICoverState ParentState, ICoverAllocationState lowestLevelaRiteAllocationRatios, ICoverAllocationState lowestLevelResidualAllocationRatios);

        //void AggregateLeafLevel(int ParentPosition, ICoverState TotalCoverState, ICoverLevelNodeAggInfo LevelAggInfo);

        void AggregateLevel(int ParentPosition, ICoverState TotalCoverState, ICoverLevelNodeAggInfo LevelAggInfo, IDerivedCoverLevelNodeAggInfo DerivedInfo, ICoverAllocationState coverLevelAllocationRatio, float[] FactorVector);

    }

    public class CoverAggregator : ICoverAggregator
    {
        public void AggregateLowestLevel(ICoverLevelResiduleInfo ResiduleInfo, ICoverLevelTermAriteInfo AriteInfo, ICoverState lowestLevelResiduleState,
                                   ICoverState lowestLevelAriteState, ICoverState ParentState, ICoverAllocationState lowestLevelaRiteAllocationRatio, ICoverAllocationState lowestLevelResidualAllocationRatio)
        {
            float[] ResiduleArr = lowestLevelResiduleState.GetSubjectLoss();
            float[] aRiteArr = lowestLevelAriteState.GetSubjectLoss();
            float[] parentArr = ParentState.GetSubjectLoss();
            int[] ResidulePartitions = ResiduleInfo.GetResiduleSubAggPartitions();
            int[] AritePartitions = AriteInfo.GetARiteAggregationPartitions();
            float[] aRiteRArr = lowestLevelAriteState.GetSubjectLoss();  //TODO: this should be recoverables
            float[] residualRatio = lowestLevelResidualAllocationRatio.GetCoverAllocationRatioP();
            float[] aRiteRatio = lowestLevelaRiteAllocationRatio.GetCoverAllocationRatioP();

            //co-op factors
            float[] aRiteFactors = lowestLevelAriteState.GetFactors();  //AriteInfo.GetLowestCoverLevelInfo().LeafCoveraRiteFactors;
            float[] residualRiteFactors = lowestLevelResiduleState.GetFactors(); //ResiduleInfo.GetLowestCoverLevelInfo().LeafCoverResidualFactors;
            FactorPattern aRiteFPattern = AriteInfo.GetLowestCoverLevelInfo().ApplyFactorPatternForaRite;
            FactorPattern residualRiteFPattern = ResiduleInfo.GetLowestCoverLevelInfo().ApplyFactorPatternForResidualRite;

            //pre-process
            float[] newResidualArr;
            float[] newaRiteArr;
            float[] newaRiteRArr;

            //TODO: tricky part, whether apply factor depending on if the allocated Rite recoverable is allocated from
            //summed term or PerRisk term. If allocated from summed term, no need apply factor again; if allocated
            //from PerRisk term, need apply factor
            //right now assuem all the top terms are either summed or PerRisk. This will make this temporarily implementation
            //easier. The Factor List is updated when forming LeafAriteFactor        
            if (aRiteFPattern == FactorPattern.AllOnes)
            {
                newaRiteArr = aRiteArr;
                newaRiteRArr = aRiteRArr;
            }
            else
            {
                newaRiteArr = aRiteArr.Zip(aRiteFactors, (x1, x2) => x1 * x2).ToArray();
                newaRiteRArr = aRiteRArr.Zip(aRiteFactors, (x1, x2) => x1 * x2).ToArray();
            }

            if (residualRiteFPattern == FactorPattern.AllOnes)
                newResidualArr = ResiduleArr;
            else
                newResidualArr = ResiduleArr.Zip(residualRiteFactors, (x1, x2) => x1 * x2).ToArray();


            SumTwoArrayByPartition(newResidualArr, newaRiteArr, parentArr, ResidulePartitions, AritePartitions, newaRiteRArr, residualRatio, aRiteRatio);
        }

        //public void AggregateLeafLevel(int ParentPosition, ICoverState TotalCoverState, ICoverLevelNodeAggInfo LevelAggInfo )
        //{
        //       AggregateSumSubject(ParentPosition,TotalCoverState,LevelAggInfo);
        //}

        public void AggregateLevel(int ParentPosition, ICoverState TotalCoverState, ICoverLevelNodeAggInfo LevelAggInfo, IDerivedCoverLevelNodeAggInfo DerivedInfo, ICoverAllocationState coverLevelAllocationRatio, float[] FactorVector)
        {

            FunctionType functionType = DerivedInfo.SubjectFunctionType;
            float[] coverRatio = coverLevelAllocationRatio.GetCoverAllocationRatioP();

            switch (functionType)
            {
                case FunctionType.Sum:
                    AggregateSumSubject(ParentPosition, TotalCoverState, LevelAggInfo, coverRatio, FactorVector);
                    break;
                case FunctionType.Min:
                    AggregateMinSubject(ParentPosition, TotalCoverState, LevelAggInfo, coverRatio, FactorVector);
                    break;
                case FunctionType.Max:
                    AggregateMaxSubject(ParentPosition, TotalCoverState, LevelAggInfo, coverRatio, FactorVector);
                    break;
                case FunctionType.Mix:
                    AggregateMixSubject(ParentPosition, TotalCoverState, LevelAggInfo, DerivedInfo, coverRatio, FactorVector);
                    break;
            }
        }

        private void SumTwoArrayByPartition(float[] ResiduleArr, float[] aRITEArr, float[] outArr, int[] partition, int[] aRITEpartition, float[] aRITERArr, float[] residualRiteAllocationRatios, float[] aRiteAllocationRatios)
        {
            //if (outArr.Length != partition.Length)
            //    throw new ArgumentException("Output array must have same length as the number of partitions!");

            float[] ResiduletempArr;
            float[] aRITEtempArr;
            float[] aRITEtempRArr;
            float[] allocationArr = new float[partition.Length];

            //allocation ratio numerator: first copy the Residual subject loss to residual ratio, aRite R to aRite ratio
            Array.Copy(ResiduleArr, 0, residualRiteAllocationRatios, 0, ResiduleArr.Length);
            Array.Copy(aRITEArr, 0, aRiteAllocationRatios, 0, aRITEArr.Length);

            int posB;
            int posA = 0;
            int gapBetween = 0;
            int aRITEposB;
            int aRITEposA = 0;
            int aRITEgapBetween = 0;

            for (int i = 0; i < partition.Length; i++)
            {
                posB = partition[i];
                aRITEposB = aRITEpartition[i];
                gapBetween = posB - posA;
                aRITEgapBetween = aRITEposB - aRITEposA;
                ResiduletempArr = new float[gapBetween];
                aRITEtempArr = new float[aRITEgapBetween];
                aRITEtempRArr = new float[aRITEgapBetween];

                Array.Copy(ResiduleArr, posA, ResiduletempArr, 0, gapBetween);
                Array.Copy(aRITEArr, aRITEposA, aRITEtempArr, 0, aRITEgapBetween);
                Array.Copy(aRITERArr, aRITEposA, aRITEtempRArr, 0, aRITEgapBetween);
                outArr[i] = ResiduletempArr.Sum();
                outArr[i] += aRITEtempArr.Sum();

                //for allocation, denominator
                allocationArr[i] = ResiduletempArr.Sum();
                allocationArr[i] += aRITEtempRArr.Sum();

                //ratio.
                //TODO: hopefully we can find a way to do Array wise math operation
                for (int j = posA; j < posA + gapBetween; j++)
                {
                    residualRiteAllocationRatios[j] = (allocationArr[i] == 0) ? 0 : ResiduletempArr[j - posA] / allocationArr[i];
                }

                for (int j = aRITEposA; j < aRITEposA + aRITEgapBetween; j++)
                {
                    aRiteAllocationRatios[j] = (allocationArr[i] == 0) ? 0 : aRITEtempArr[j - aRITEposA] / allocationArr[i];
                }

                posA = posB;
                aRITEposA = aRITEposB;
            }
        }

        private void AggregateSumSubject(int ParentPosition, ICoverState TotalCoverState, ICoverLevelNodeAggInfo LevelAggInfo, float[] coverRatio, float[] FactorVector)
        {
            int[] ChildrenMap = LevelAggInfo.GetChildrenMap();
            int[] MapPartitions = LevelAggInfo.GetCoverAggregationPartitions();
            float[] TotalPayoutState = TotalCoverState.GetPayout();
            float[] TotalSubjectState = TotalCoverState.GetSubjectLoss();
            int MapPosition = 0;
                       
            int[] factorsIndex = LevelAggInfo.GetFactorIndex();
            int counter = factorsIndex.Count();
            float[] factors = new float[counter];
            for (int i = 0; i < counter; i++)
            {

                int uniqueIndex = factorsIndex[i];
                if (uniqueIndex == -1)
                    factors[i] = 1;
                else
                    factors[i] = FactorVector[uniqueIndex];            
            }
                       
            FactorPattern factorPattern = LevelAggInfo.GetLevelFactorPattern();

            if (factorPattern == FactorPattern.AllOnes || factorPattern == FactorPattern.Constant)
            {
                for (int i = 0; i < MapPartitions.Length; i++)
                {
                    float ParentSubject = 0;
                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        ParentSubject += TotalPayoutState[ChildrenMap[j]] * factors[j];  //denominator           
                    }

                    //ratio
                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        coverRatio[j] = (ParentSubject == 0) ? 0 : TotalPayoutState[ChildrenMap[j]] * factors[j] / ParentSubject;
                    }

                    TotalSubjectState[ParentPosition] = ParentSubject;
                    MapPosition = MapPartitions[i] + 1;
                    ParentPosition++;
                }
            }
            else
            {
                for (int i = 0; i < MapPartitions.Length; i++)
                {
                    float ParentSubject = 0;
                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        ParentSubject += TotalPayoutState[ChildrenMap[j]] * factors[j];  //denominator           
                    }

                    //ratio
                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        coverRatio[j] = (ParentSubject == 0) ? 0 : TotalPayoutState[ChildrenMap[j]] * factors[j] / ParentSubject;
                    }

                    TotalSubjectState[ParentPosition] = ParentSubject;
                    MapPosition = MapPartitions[i] + 1;
                    ParentPosition++;
                }
            }
        }

        private void AggregateMinSubject(int ParentPosition, ICoverState TotalCoverState, ICoverLevelNodeAggInfo LevelAggInfo, float[] coverRatio, float[] FactorVector)
        {
            int[] ChildrenMap = LevelAggInfo.GetChildrenMap();
            int[] MapPartitions = LevelAggInfo.GetCoverAggregationPartitions();
            float[] TotalPayoutState = TotalCoverState.GetPayout();
            float[] TotalSubjectState = TotalCoverState.GetSubjectLoss();
            int MapPosition = 1;
            float MinSubject = 0;
            int numOfMin = 0;
            //float[] factors = LevelAggInfo.GetFactor();
            int[] factorsIndex = LevelAggInfo.GetFactorIndex();
            int counter = factorsIndex.Count();
            float[] factors = new float[counter];
            for (int i = 0; i < counter; i++)
            {

                int uniqueIndex = factorsIndex[i];
                if (uniqueIndex == -1)
                    factors[i] = 1;
                else
                    factors[i] = FactorVector[uniqueIndex];
            }
            FactorPattern factorPattern = LevelAggInfo.GetLevelFactorPattern();

            if (factorPattern == FactorPattern.AllOnes || factorPattern == FactorPattern.Constant)
            {
                for (int i = 0; i < MapPartitions.Length; i++)
                {
                    numOfMin = 0;

                    MinSubject = TotalPayoutState[ChildrenMap[MapPosition - 1]];

                    for (int j = MapPosition + 1; j <= MapPartitions[i]; j++)
                    {
                        float temp = TotalPayoutState[ChildrenMap[j - 1]];
                        MinSubject = Math.Min(MinSubject, temp);
                    }

                    //ratio
                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        if (MinSubject == TotalPayoutState[ChildrenMap[j]])
                        {
                            numOfMin++;
                            coverRatio[j - 1] = 1;
                        }
                    }

                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        coverRatio[j - 1] /= numOfMin;
                    }

                    TotalSubjectState[ParentPosition] = MinSubject * factors[MapPosition];
                    MapPosition = MapPartitions[i] + 1;
                    ParentPosition++;
                }
            }
            else
            {
                for (int i = 0; i < MapPartitions.Length; i++)
                {
                    numOfMin = 0;

                    MinSubject = TotalPayoutState[ChildrenMap[MapPosition - 1]] * factors[MapPosition - 1];

                    for (int j = MapPosition + 1; j <= MapPartitions[i]; j++)
                    {
                        float temp = TotalPayoutState[ChildrenMap[j - 1]] * factors[j - 1];
                        MinSubject = Math.Min(MinSubject, temp);
                    }

                    //ratio
                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        if (MinSubject == TotalPayoutState[ChildrenMap[j]] * factors[j])
                        {
                            numOfMin++;
                            coverRatio[j - 1] = 1;
                        }
                    }

                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        coverRatio[j - 1] /= numOfMin;
                    }

                    TotalSubjectState[ParentPosition] = MinSubject;
                    MapPosition = MapPartitions[i] + 1;
                    ParentPosition++;
                }
            }
        }

        private void AggregateMaxSubject(int ParentPosition, ICoverState TotalCoverState, ICoverLevelNodeAggInfo LevelAggInfo, float[] coverRatio, float[] FactorVector)
        {
            int[] ChildrenMap = LevelAggInfo.GetChildrenMap();
            int[] MapPartitions = LevelAggInfo.GetCoverAggregationPartitions();
            float[] TotalPayoutState = TotalCoverState.GetPayout();
            float[] TotalSubjectState = TotalCoverState.GetSubjectLoss();
            //float[] factors = LevelAggInfo.GetFactor();
            int[] factorsIndex = LevelAggInfo.GetFactorIndex();
            int counter = factorsIndex.Count();
            float[] factors = new float[counter];
            for (int i = 0; i < counter; i++)
            {

                int uniqueIndex = factorsIndex[i];
                if (uniqueIndex == -1)
                    factors[i] = 1;
                else
                    factors[i] = FactorVector[uniqueIndex];
            }
            FactorPattern factorPattern = LevelAggInfo.GetLevelFactorPattern();

            int MapPosition = 1;
            float MaxSubject = 0;
            int numOfMax = 0;

            if (factorPattern == FactorPattern.AllOnes || factorPattern == FactorPattern.Constant)
            {
                for (int i = 0; i < MapPartitions.Length; i++) //for each parent
                {
                    //MaxSubject = TotalPayoutState[ChildrenMap[0]];
                    //MaxSubject = 0;
                    MaxSubject = TotalPayoutState[ChildrenMap[MapPosition - 1]] * factors[MapPosition - 1];
                    numOfMax = 0;

                    for (int j = MapPosition + 1; j <= MapPartitions[i]; j++)
                    {
                        float temp = TotalPayoutState[ChildrenMap[j - 1]] * factors[j- 1];
                        MaxSubject = Math.Max(MaxSubject, temp);
                    }

                    //ratio
                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        if (MaxSubject == TotalPayoutState[ChildrenMap[j]] * factors[j])
                        {
                            coverRatio[j - 1] = 1;
                            numOfMax++;
                        }
                    }

                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        coverRatio[j - 1] /= numOfMax;
                    }

                    TotalSubjectState[ParentPosition] = MaxSubject * factors[MapPosition];
                    //MapPosition += MapPartitions[i];
                    MapPosition = MapPartitions[i] + 1;
                    ParentPosition++;
                }
            }
            else
            {
                for (int i = 0; i < MapPartitions.Length; i++) //for each parent
                {
                    MaxSubject = TotalPayoutState[ChildrenMap[MapPosition - 1]] * factors[MapPosition - 1];
                    numOfMax = 0;

                    for (int j = MapPosition + 1; j <= MapPartitions[i]; j++)
                    {
                        float temp = TotalPayoutState[ChildrenMap[j - 1]] * factors[j - 1];
                        MaxSubject = Math.Max(MaxSubject, temp);
                    }

                    //ratio
                    for (int j = MapPosition; j <= MapPartitions[i]; j++)
                    {
                        if (MaxSubject == TotalPayoutState[ChildrenMap[j - 1]] * factors[j - 1])
                        {
                            coverRatio[j - 1] = 1;
                            numOfMax++;
                        }
                    }

                    for (int j = MapPosition; j <= MapPartitions[i]; j++)
                    {
                        coverRatio[j - 1] /= numOfMax;
                    }

                    TotalSubjectState[ParentPosition] = MaxSubject;
                    //MapPosition += MapPartitions[i];
                    MapPosition = MapPartitions[i] + 1;
                    ParentPosition++;
                }
            }

        }

        private void AggregateMixSubject(int ParentPosition, ICoverState TotalCoverState, ICoverLevelNodeAggInfo LevelAggInfo, IDerivedCoverLevelNodeAggInfo DerivedInfo, float[] coverRatio, float[] FactorVector)
        {
            //TODO: for agg is fine, but for allocation we cannot use this method
            throw new NotSupportedException("Mix Derived cover subject is not supported");
            int[] ChildrenMap = LevelAggInfo.GetChildrenMap();
            int[] MapPartitions = LevelAggInfo.GetCoverAggregationPartitions();
            float[] TotalPayoutState = TotalCoverState.GetPayout();
            float[] TotalSubjectState = TotalCoverState.GetSubjectLoss();
            int MapPosition = 1;
            float TempSubject = 0;
            float ChildR = 0;
            float x = 0;
            float[] multiplier = DerivedInfo.GetMixFunctionMultiplier();
            //float[] factors = LevelAggInfo.GetFactor();
            int[] factorsIndex = LevelAggInfo.GetFactorIndex();
            int counter = factorsIndex.Count();
            float[] factors = new float[counter];
            for (int i = 0; i < counter; i++)
            {

                int uniqueIndex = factorsIndex[i];
                if (uniqueIndex == -1)
                    factors[i] = 1;
                else
                    factors[i] = FactorVector[uniqueIndex];
            }
            FactorPattern factorPattern = LevelAggInfo.GetLevelFactorPattern();

            if (factorPattern == FactorPattern.AllOnes || factorPattern == FactorPattern.Constant)
            {
                for (int i = 0; i < MapPartitions.Length; i++)
                {
                    TempSubject = TotalPayoutState[ChildrenMap[0]];
                    x = multiplier[i];

                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        ChildR = TotalPayoutState[ChildrenMap[j]];
                        TempSubject = (TempSubject + ChildR) - x * Math.Abs(TempSubject - ChildR) - Math.Abs(x) * (TempSubject + ChildR);
                    }
                    TotalSubjectState[ParentPosition] = TempSubject * factors[MapPosition];
                    MapPosition += MapPartitions[i];
                    ParentPosition++;
                }
            }
            else
            {
                for (int i = 0; i < MapPartitions.Length; i++)
                {
                    TempSubject = TotalPayoutState[ChildrenMap[0]] * factors[0];
                    x = multiplier[i];

                    for (int j = MapPosition; j < MapPartitions[i]; j++)
                    {
                        ChildR = TotalPayoutState[ChildrenMap[j]] * factors[j];
                        TempSubject = (TempSubject + ChildR) - x * Math.Abs(TempSubject - ChildR) - Math.Abs(x) * (TempSubject + ChildR);
                    }
                    TotalSubjectState[ParentPosition] = TempSubject;
                    MapPosition += MapPartitions[i];
                    ParentPosition++;
                }
            }
        }

    }
}
