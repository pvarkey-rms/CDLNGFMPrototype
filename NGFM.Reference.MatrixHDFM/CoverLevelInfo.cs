using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{

    public class LowestCoverLevel : ICoverLevelTermAriteInfo, ICoverLevelResiduleInfo
    {
        #region field
        private int levelsize;
        private LowestCoverLevelInfo lowestCoverLevelInfo;
        #endregion

        public int LevelSize { get { return levelsize; } }

        #region Constructor
        public LowestCoverLevel(int _numOfResidule, int _numOfArites, int[] _residuleSubAggPartitions, int[] _ariteAggPartitions,
                                int[] _guLossindices, int[] _recoverableLevelIndex, int[] _recoverableIndices, int[] _ariteGULossIndicies, int[] _leafCoveraRiteFactorsIndex, int[] _leafCoverResidualFactorsIndex)
        {
            levelsize = _numOfArites + _numOfResidule;
            lowestCoverLevelInfo = new LowestCoverLevelInfo(_numOfArites, _numOfResidule, _residuleSubAggPartitions, _ariteAggPartitions,
                                                            _guLossindices, _recoverableLevelIndex, _recoverableIndices, _ariteGULossIndicies, _leafCoveraRiteFactorsIndex, _leafCoverResidualFactorsIndex);

        }

        public LowestCoverLevel(LowestCoverLevelInfo _lowestCoverLevelInfo)
        {
            lowestCoverLevelInfo = _lowestCoverLevelInfo;
            levelsize = _lowestCoverLevelInfo.NumOfArites + _lowestCoverLevelInfo.NumOfResidules;
        }

        public LowestCoverLevelInfo GetLowestCoverLevelInfo() { return lowestCoverLevelInfo; }

        #endregion

        #region Override for ICoverLevelResiduleInfo
        public int NumOfResidules { get { return lowestCoverLevelInfo.NumOfResidules; } }
        public int[] GetResiduleSubAggPartitions() { return lowestCoverLevelInfo.ResiduleSubAggPartitions; }
        public int[] GetGULossIndicies() { return lowestCoverLevelInfo.GULossIndices; }

        #endregion

        #region Override for ICoverLevelAriteInfo
        public int NumOfARITEs { get { return lowestCoverLevelInfo.NumOfArites; } }
        public int[] GetARiteAggregationPartitions() { return lowestCoverLevelInfo.AriteAggPartitions; }
        public int[] GetRecoverableLevelIndex() { return lowestCoverLevelInfo.RecoverableLevelIndex; }
        public int[] GetRecoverableIndicies() { return lowestCoverLevelInfo.RecoverableIndices; }
        public int[] GetAriteGULossIndicies() { return lowestCoverLevelInfo.AriteGULossIndices; }
        #endregion
    }

    public class LowestCoverLevelInfo
    {
        public int[] ResiduleSubAggPartitions { get; private set; }
        public int[] AriteAggPartitions { get; private set; }

        public int[] GULossIndices { get; private set; }
        public int[] RecoverableLevelIndex { get; private set; }
        public int[] RecoverableIndices { get; private set; }
        public int[] AriteGULossIndices {get; private set;} 

        public int NumOfArites { get; private set; }
        public int NumOfResidules { get; private set; }

        public int[] LeafCoveraRiteFactorsIndex { get; private set; }
        public int[] LeafCoverResidualFactorsIndex {get; private set;}

        public FactorPattern ApplyFactorPatternForaRite { get; private set; }
        public FactorPattern ApplyFactorPatternForResidualRite { get; private set; }

        public LowestCoverLevelInfo(int _numOfArites, int _numOfResidules, int[] _residuleSubAggPartitions, int[] _ariteAggPartitions, int[] _gulossIndices,
                                int[] _recoverableLevelIndex, int[] _recoverableIndices, int[] _ariteGULossIndicies, int[] _leafCoveraRiteFactorsIndex, int[] _leafCoverResidualFactorsIndex)
        {
            NumOfArites = _numOfArites;
            NumOfResidules = _numOfResidules;
            ResiduleSubAggPartitions = _residuleSubAggPartitions;
            AriteAggPartitions = _ariteAggPartitions;
            GULossIndices = _gulossIndices;
            RecoverableLevelIndex = _recoverableLevelIndex;
            RecoverableIndices = _recoverableIndices;
            LeafCoveraRiteFactorsIndex = _leafCoveraRiteFactorsIndex;
            LeafCoverResidualFactorsIndex = _leafCoverResidualFactorsIndex;
            PatternInfoGenerator generator = new PatternInfoGenerator();
            ApplyFactorPatternForaRite = generator.MakeFactorPatternInfo(_leafCoveraRiteFactorsIndex);
            ApplyFactorPatternForResidualRite = generator.MakeFactorPatternInfo(_leafCoverResidualFactorsIndex);
            AriteGULossIndices = _ariteGULossIndicies;
        }
    }

    public class CoverLevel : ICoverLevelFinancialInfo, ICoverLevelNodeAggInfo
    {
        #region field
        protected int levelsize;

        protected float[] Share;
        protected float[] CodedLimit;
        protected float[] CodedAttPt;
        protected int[] AggregationPartitions;
        protected int[] AggregationExpansions;
        protected int[] ChildrenMap;
        protected FunctionType payoutFunctionType;
        protected float[] PayOutMultiplier;
        private int[] leafTopCoverList;
        protected int[] FactorsIndex;
        protected FactorPattern ApplyFactorPattern;

        #endregion
        public int LevelSize { get { return levelsize; } }

        #region Constructor

        public CoverLevel(FunctionType _payoutFunctionType, float[] _payoutMultiplier,int _numOfNodes, float[] _share, float[] _codedLim, float[] _codedAttPt,
                          int[] _aggregationPartition, int[] _childrenMap, int[] _factorsIndex, params int[] _leafTopCoverList)
        {
            levelsize = _numOfNodes;
            Share = _share;
            CodedLimit = _codedLim;
            CodedAttPt = _codedAttPt;
            AggregationPartitions = _aggregationPartition;
            ChildrenMap = _childrenMap;
            payoutFunctionType = _payoutFunctionType;
            PayOutMultiplier = _payoutMultiplier;
            leafTopCoverList = _leafTopCoverList;            
            FactorsIndex = _factorsIndex;
            PatternInfoGenerator generator = new PatternInfoGenerator();
            ApplyFactorPattern = generator.MakeFactorPatternInfo(_factorsIndex);
        }

        #endregion

        #region Override for ICoverLevelFinancialInfo
        public FunctionType PayoutFunctionType { get { return payoutFunctionType; } }

        public float[] GetCodedAttachment() { return CodedAttPt; }
        public float[] GetCodedLimit() { return CodedLimit; }
        public float[] GetCodedShare() { return Share; }
        public float[] GetPayoutMultiplier() { return PayOutMultiplier; }
        public int[] GetFactorIndex() { return FactorsIndex; }
        public FactorPattern GetLevelFactorPattern() { return ApplyFactorPattern; }

        #endregion

        #region Override for ICoverLevelNodeAggInfo

        public int NumOfNodes { get { return LevelSize; } }
        public AggregationMode AggMode { get { return AggregationMode.NoPattern; ; } }
        public int[] GetCoverAggregationPartitions() { return AggregationPartitions; }
        public int[] GetChildrenMap() { return ChildrenMap; }
        public int[] GetLeafTopCoverList() { return leafTopCoverList; }
        #endregion

    }

    public class DerivedCoverLevel : CoverLevel, IDerivedCoverLevelNodeAggInfo 
    {
        #region field
        private FunctionType subjectFunctionType;
        private float[] mixFunctionMultiplier;

        #endregion

        #region Constructor
        public DerivedCoverLevel(FunctionType _subjectFunctionType,FunctionType _payoutFunctionType, float[] _mixFunctionMultiplier, float[] _payoutMultiplier, int _numOfNodes, float[] _share,
                                float[] _codedLim, float[] _codedAttPt, int[] _aggregationPartition, int[] _childrenMap, int[] _factorsIndex)
            : base(_payoutFunctionType, _payoutMultiplier,_numOfNodes, _share, 
                                 _codedLim, _codedAttPt,_aggregationPartition,_childrenMap, _factorsIndex)
                          
        {
            subjectFunctionType = _subjectFunctionType;
            payoutFunctionType = _payoutFunctionType;
            mixFunctionMultiplier = _mixFunctionMultiplier;
            PayOutMultiplier = _payoutMultiplier;
            FactorsIndex = _factorsIndex;
            PatternInfoGenerator generator = new PatternInfoGenerator();
            ApplyFactorPattern = generator.MakeFactorPatternInfo(_factorsIndex);
        }

        #endregion

        #region Override for ICoverLevelFunctionInfo
        public FunctionType SubjectFunctionType { get { return subjectFunctionType; } }        
        public float[] GetMixFunctionMultiplier() { return mixFunctionMultiplier; }
        #endregion

    }

    public interface ICoverLevelFinancialInfo
    {
        float[] GetCodedAttachment();
        float[] GetCodedLimit();
        float[] GetCodedShare();
        FunctionType PayoutFunctionType { get; }
        float[] GetPayoutMultiplier();
        int[] GetFactorIndex();
    }

    public interface ICoverLevelNodeAggInfo
    {
        int NumOfNodes { get; }

        //We use expansion for overlap and partitions for non-overlap case
        AggregationMode AggMode { get; }
        int[] GetCoverAggregationPartitions();
        int[] GetChildrenMap();
        int[] GetLeafTopCoverList();
        int[] GetFactorIndex();
        FactorPattern GetLevelFactorPattern();
    }

    public interface ICoverLevelResiduleInfo
    {
        int NumOfResidules { get; }

        int[] GetResiduleSubAggPartitions();        
        int[] GetGULossIndicies();
        LowestCoverLevelInfo GetLowestCoverLevelInfo();
    }

    public interface ICoverLevelTermAriteInfo
    {
        int NumOfARITEs { get; }

        int[] GetARiteAggregationPartitions();
        int[] GetRecoverableIndicies();
        int[] GetRecoverableLevelIndex();
        int[] GetAriteGULossIndicies();
        LowestCoverLevelInfo GetLowestCoverLevelInfo();
    }

    public interface IDerivedCoverLevelNodeAggInfo 
    {
        FunctionType SubjectFunctionType { get; }
       
        float[] GetMixFunctionMultiplier();
    }

    public enum AggregationMode
    {
        NoPattern,
        UseContraction,
        UseExpansion,       
    }

    public enum FunctionType
    {
        RCV,
        Subject,
        Min,
        Max,
        Sum,
        WaitingPeriod,
        Constant,
        Regular,
        Mix
    }
}
