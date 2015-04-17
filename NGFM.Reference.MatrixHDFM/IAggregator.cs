using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace NGFM.Reference.MatrixHDFM
{

    public class Aggregator1 : IAggregator
    {
        public void AggregateLevel(ILevelState childLevel, ISimpleLevelState childARITELevel, ILevelState parentLevel, ILevelNodeAggInfo childAggInfo, ILevelAtomicRITEInfo aRITEINfo, Stopwatch aggregation1)
        {
            int childlevelsize = childAggInfo.NumOfNodes;
            IAggPatternInfo patterinInfo = childAggInfo.GetNodePatternInfo();
            int[] partitions = childAggInfo.GetNodeAggregationPartitions();

            IAggPatternInfo aRITEpatterinInfo = aRITEINfo.GetARitePatternInfo();
            int[] aRITEpartitions = aRITEINfo.GetARiteAggregationPartitions();

            float[] nodeFactors = childLevel.GetFactors();  //childAggInfo.GetSimpleLevelInfo().Factors;
            float[] riteFactor = childARITELevel.GetFactors(); //aRITEINfo.GetaRiteInfo().Factors;
            FactorPattern nodeFPattern = childAggInfo.GetSimpleLevelInfo().ApplyFactorPattern;
            FactorPattern riteFPattern = aRITEINfo.GetaRiteInfo().ApplyFactorPattern;

            float[] childrenDed;
            float[] childrenExcess;

            //pre-process to multiply the Factor array
            if (nodeFPattern == FactorPattern.AllOnes)
            {
                childrenDed = childLevel.GetDeductible();
                childrenExcess = childLevel.GetExcess();
            }
            else
            {
                childrenDed = childLevel.GetDeductible().Zip(nodeFactors, (x1, x2) => x1 * x2).ToArray();
                childrenExcess = childLevel.GetExcess().Zip(nodeFactors, (x1, x2) => x1 * x2).ToArray();
            }

            //Aggregate D and X (ARITEs do not have D, X)
            switch (patterinInfo.GetLevelRelation)
            {
                case AggRelationship.OneToOne:
                    Array.Copy(childrenDed, parentLevel.GetDeductible(), childlevelsize);
                    Array.Copy(childrenExcess, parentLevel.GetExcess(), childlevelsize);
                    break;
                case AggRelationship.RepeatedPattern:
                    int groupSize = patterinInfo.RepeatedPartitionSize;
                    SumArrayByPattern(childrenDed, parentLevel.GetDeductible(), groupSize);
                    SumArrayByPattern(childrenExcess, parentLevel.GetExcess(), groupSize);
                    break;
                case AggRelationship.NoPattern:
                    SumArrayByPartition(childrenDed, parentLevel.GetDeductible(), partitions);

                    aggregation1.Start();
                    SumArrayByPartition(childrenExcess, parentLevel.GetExcess(), partitions);
                    aggregation1.Stop();
                    break;
            }

            // Aggregate S in case with Atomic Rites & Aggregate R for allocation
            float[] childrenNodeSubLoss;
            float[] childrenRiteSubLoss;
            float[] childrenRecov;

            if (nodeFPattern == FactorPattern.AllOnes)
            {
                childrenNodeSubLoss = childLevel.GetSubjectLoss();
                childrenRecov = childLevel.GetRecoverable();
            }
            else
            {
                childrenNodeSubLoss = childLevel.GetSubjectLoss().Zip(nodeFactors, (x1, x2) => x1 * x2).ToArray();
                childrenRecov = childLevel.GetRecoverable().Zip(nodeFactors, (x1, x2) => x1 * x2).ToArray();
            }

            if (aRITEINfo.HasAtomicRITEs)
            {
                //pre-process
                if (riteFPattern == FactorPattern.AllOnes)
                {
                    childrenRiteSubLoss = childARITELevel.GetSubjectLoss();
                }
                else
                {
                    childrenRiteSubLoss = childARITELevel.GetSubjectLoss().Zip(riteFactor, (x1, x2) => x1 * x2).ToArray();
                }

                if (aRITEpatterinInfo.GetLevelRelation == AggRelationship.NoPattern ||
                    patterinInfo.GetLevelRelation == AggRelationship.NoPattern)
                {
                    SumTwoArrayByPartition(childrenNodeSubLoss, childrenRiteSubLoss, parentLevel.GetSubjectLoss(), partitions, aRITEpartitions);
                    SumTwoArrayByPartition(childrenRecov, childrenRiteSubLoss, parentLevel.GetRecoverable(), partitions, aRITEpartitions);
                }
                else
                {
                    int termgroupsize;
                    int aRITEgroupsize;

                    switch (patterinInfo.GetLevelRelation)
                    {
                        case AggRelationship.OneToOne:
                            termgroupsize = 1;
                            break;
                        case AggRelationship.RepeatedPattern:
                            termgroupsize = aRITEpatterinInfo.RepeatedPartitionSize;
                            break;
                        default:
                            throw new NotSupportedException("The Level realtion " + patterinInfo.GetLevelRelation + " is currently not supported");
                    }

                    switch (aRITEpatterinInfo.GetLevelRelation)
                    {
                        case AggRelationship.OneToOne:
                            aRITEgroupsize = 1;
                            break;
                        case AggRelationship.RepeatedPattern:
                            aRITEgroupsize = aRITEpatterinInfo.RepeatedPartitionSize;
                            break;
                        default:
                            throw new NotSupportedException("The Level realtion " + patterinInfo.GetLevelRelation + " is currently not supported");
                    }

                    SumTwoArrayByPatternNew(childrenNodeSubLoss, childrenRiteSubLoss, parentLevel.GetSubjectLoss(), termgroupsize, aRITEgroupsize);
                    SumTwoArrayByPatternNew(childrenRecov, childrenRiteSubLoss, parentLevel.GetRecoverable(), termgroupsize, aRITEgroupsize);
                }
            }
            // Aggregate S in case with no Atomic Rites
            else
            {
                switch (patterinInfo.GetLevelRelation)
                {
                    case AggRelationship.OneToOne:
                        Array.Copy(childrenNodeSubLoss, parentLevel.GetSubjectLoss(), childlevelsize);
                        Array.Copy(childrenRecov, parentLevel.GetRecoverable(), childlevelsize);
                        break;
                    case AggRelationship.RepeatedPattern:
                        int groupSize = patterinInfo.RepeatedPartitionSize;
                        SumArrayByPattern(childrenNodeSubLoss, parentLevel.GetSubjectLoss(), groupSize);
                        SumArrayByPattern(childrenRecov, parentLevel.GetRecoverable(), groupSize);
                        break;
                    case AggRelationship.NoPattern:
                        SumArrayByPartition(childrenNodeSubLoss, parentLevel.GetSubjectLoss(), partitions);
                        SumArrayByPartition(childrenRecov, parentLevel.GetRecoverable(), partitions);
                        break;
                }
            }
        }

        public void AggregateLevel(ISimpleLevelState childLevel, ILevelState parentLevel, ILevelAtomicRITEInfo aRITEINfo)
        {
            int childlevelsize = aRITEINfo.NumOfARITEs;
            IAggPatternInfo patterinInfo = aRITEINfo.GetARitePatternInfo();
            int[] partitions = aRITEINfo.GetARiteAggregationPartitions();
            float[] factors = childLevel.GetFactors(); //aRITEINfo.GetaRiteInfo().Factors;
            FactorPattern fPattern = aRITEINfo.GetaRiteInfo().ApplyFactorPattern;

            //pre-process for factor
            float[] childrenLoss;
            if (fPattern == FactorPattern.AllOnes)
                childrenLoss = childLevel.GetSubjectLoss();
            else
                childrenLoss = childLevel.GetSubjectLoss().Zip(factors, (x1, x2) => x1 * x2).ToArray();

            switch (patterinInfo.GetLevelRelation)
            {
                case AggRelationship.OneToOne:
                    Array.Copy(childrenLoss, parentLevel.GetSubjectLoss(), childlevelsize);
                    break;
                case AggRelationship.RepeatedPattern:
                    int groupSize = patterinInfo.RepeatedPartitionSize;
                    SumArrayByPattern(childrenLoss, parentLevel.GetSubjectLoss(), groupSize);
                    break;
                case AggRelationship.NoPattern:
                    SumArrayByPartition(childrenLoss, parentLevel.GetSubjectLoss(), partitions);
                    break;
            }
        }

        private void SumArrayByPartition(float[] myArr, float[] outArr, int[] partition)
        {
            if (outArr.Length != partition.Length)
                throw new ArgumentException("Output array must have same length as the number of partitions!");

            float[] tempArr;


            int posA = 0;
            int gapBetween = 0;

            for (int i = 0; i < partition.Length; i++)
            {
                int posB = partition[i];
                gapBetween = posB - posA;
                tempArr = new float[gapBetween];
                Array.Copy(myArr, posA, tempArr, 0, gapBetween);
                outArr[i] = tempArr.Sum();
                posA = posB;
            }
        }

        private void SumArrayByPattern(float[] myArr, float[] outArr, int groupSize)
        {
            if (myArr.Length % groupSize != 0)
                throw new ArgumentException("Input array length must be multiple of groupSize!");

            int outindex = 0;
            for (int i = 0; i < myArr.Length; i += groupSize)
            {
                float partitionSum = 0;
                for (int j = i; j < i + groupSize; j++)
                {
                    partitionSum += myArr[j];
                }
                outArr[outindex] = partitionSum;
                outindex += 1;
            }
        }

        private void SumTwoArrayByPartition(float[] myArr, float[] aRITEArr, float[] outArr, int[] partition, int[] aRITEpartition)
        {
            if (outArr.Length != partition.Length)
                throw new ArgumentException("Output array must have same length as the number of partitions!");

            float[] tempArr;
            float[] aRITEtempArr;

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
                tempArr = new float[gapBetween];
                aRITEtempArr = new float[aRITEgapBetween];

                Array.Copy(myArr, posA, tempArr, 0, gapBetween);
                Array.Copy(aRITEArr, aRITEposA, aRITEtempArr, 0, aRITEgapBetween);
                outArr[i] = tempArr.Sum();
                outArr[i] += aRITEtempArr.Sum();
                posA = posB;
                aRITEposA = aRITEposB;
            }
        }

        private void SumTwoArrayByPattern(float[] myArr, float[] aRITEArr, float[] outArr, int groupSize, int ARITEgroupSize)
        {
            if (myArr.Length % groupSize != 0)
                throw new ArgumentException("Input array length must be multiple of groupSize!");

            int NumOfGroups = myArr.Length / groupSize;
            int termArrayIndex = 0;
            int aRITEarrayINdex = 0;

            for (int i = 0; i < NumOfGroups; i++)
            {
                float partitionSum = 0;

                //Sum all Term Children              
                for (int j = termArrayIndex; j < termArrayIndex + groupSize; j++)
                {
                    partitionSum += myArr[j];
                }

                //Sum all ARite Children
                for (int j = aRITEarrayINdex; j < aRITEarrayINdex + ARITEgroupSize; j++)
                {
                    partitionSum += aRITEArr[j];
                }

                outArr[i] = partitionSum;

                termArrayIndex += groupSize;
                aRITEarrayINdex += ARITEgroupSize;
            }
        }

        private void SumTwoArrayByPatternNew(float[] myArr, float[] aRITEArr, float[] outArr, int groupSize, int ARITEgroupSize)
        {
            if (myArr.Length % groupSize != 0)
                throw new ArgumentException("Input array length must be multiple of groupSize!");

            int termArrayIndex = 0;
            int aRITEarrayINdex = 0;
            int NumOfGroups = myArr.Length / groupSize;

            for (int i = 0; i < NumOfGroups; i++)
            {
                float partitionSum = 0;

                //Sum all Term Children      
                //TODO: need put check here if the group size > 4.
                switch (groupSize)
                {
                    case 4:
                        partitionSum += myArr[termArrayIndex + 3];
                        goto case 3;
                    case 3:
                        partitionSum += myArr[termArrayIndex + 2];
                        goto case 2;
                    case 2:
                        partitionSum += myArr[termArrayIndex + 1];
                        goto case 1;
                    case 1:
                        partitionSum += myArr[termArrayIndex];
                        break;
                }

                switch (ARITEgroupSize)
                {
                    case 4:
                        partitionSum += aRITEArr[aRITEarrayINdex + 3];
                        goto case 3;
                    case 3:
                        partitionSum += aRITEArr[aRITEarrayINdex + 2];
                        goto case 2;
                    case 2:
                        partitionSum += aRITEArr[aRITEarrayINdex + 1];
                        goto case 1;
                    case 1:
                        partitionSum += aRITEArr[aRITEarrayINdex];
                        break;
                }

                outArr[i] = partitionSum;
                termArrayIndex += groupSize;
                aRITEarrayINdex += ARITEgroupSize;

            }
        }
    }

    public interface IAggregator
    {
        //Aggregate all other level except the lowest level
        void AggregateLevel(ILevelState childLevel, ISimpleLevelState childARITELevel, ILevelState parentLevel, ILevelNodeAggInfo childAggInfo, ILevelAtomicRITEInfo aRITEINfo, Stopwatch aggregation1);

        //Aggregate the lowest level (All Arites) to immediate parent level (Here we don't have the the termLevelState)
        void AggregateLevel(ISimpleLevelState childLevel, ILevelState parentLevel, ILevelAtomicRITEInfo aRITEINfo);
    }
}
