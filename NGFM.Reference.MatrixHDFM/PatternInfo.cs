using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class PatternInfo : IAggPatternInfo
    {
        private int[] partitionSizes;

        public PatternInfo(AggRelationship _getLevelRelation, int _repeatedPartitionSize, int[] partitionSizes)
        {
            GetLevelRelation = _getLevelRelation;
            RepeatedPartitionSize = _repeatedPartitionSize;
        }

        #region IAggPatternInfo override
        public AggRelationship GetLevelRelation { get; private set; }
        public int RepeatedPartitionSize { get; private set; }
        public int[] GetPartitionSizes()
        {
            return partitionSizes;

        }
        #endregion

    }

    public class PatternInfoGenerator
    {
        public IAggPatternInfo MakePatternInfo(PatternInfoType type, int[] partitions, int levelsize)
        {
            if (type == PatternInfoType.Basic)
            {
                return BuildBasicPatternInfo(partitions, levelsize);
            }
            else
                throw new ArgumentOutOfRangeException("Cannot currently generate patterns of type: " + type.ToString());
        }

        public FactorPattern MakeFactorPatternInfo(int[] factorsIndex)
        {      
            if (factorsIndex == null)
                return FactorPattern.NoFactor;

            int counter = factorsIndex.Distinct().ToList().Count;
            if (counter == 0)
                return FactorPattern.NoFactor;
            else if (counter > 1)
                return FactorPattern.Mixed;
            else if (factorsIndex[0] == -1)  //means the factor = 1
                return FactorPattern.AllOnes;
            else
                return FactorPattern.Mixed;
            //else
              //  return FactorPattern.Constant;  //TODO: no constant pattern can be detected at graph building stage as we move toward to per event factor
        }

        private PatternInfo BuildBasicPatternInfo(int[] partitions, int levelsize)
        {
            if (partitions.Length == 0)
                return new PatternInfo(AggRelationship.NoPattern, levelsize, new int[0]);

            AggRelationship relationship = AggRelationship.NoPattern;
            int repetitionSize = 0;
            
            int[] partitionSizes = CalcPartitionSizes(partitions);

            if (partitionSizes.Length == 1)
                return new PatternInfo(AggRelationship.NoPattern, levelsize, partitionSizes);

            int firstsize = partitionSizes[0] = partitions[0];

            if (partitionSizes.All(size => size == firstsize))
            {
                if (firstsize == 1)
                {
                    relationship = AggRelationship.OneToOne;
                    repetitionSize = 1;
                }
                else
                {
                    relationship = AggRelationship.RepeatedPattern;
                    repetitionSize = firstsize;
                }
            }

            return new PatternInfo(relationship, repetitionSize, partitionSizes);
        }

        private int[] CalcPartitionSizes(int[] partitions)
        {
            int[] partitionSizes = new int[partitions.Length];
            for (int i = 1; i < partitions.Length; i++)
            {
                partitionSizes[i] = partitions[i] - partitions[i - 1];
            }

            return partitionSizes;
        }
    }

    public interface IAggPatternInfo
    {
        AggRelationship GetLevelRelation { get; }
        int RepeatedPartitionSize { get; }

        int[] GetPartitionSizes();
    }

    public enum AggRelationship
    {
        OneToOne,
        RepeatedPattern,
        NoPattern
    }

    public enum PatternInfoType
    {
        Basic
    }

    public enum FactorPattern
    {
        AllOnes,
        Constant,
        Mixed,
        NoFactor
    }

}
