using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{

    public class TermLevel : ILevelAtomicRITEInfo, ILevelNodeAggInfo, ILevelFinancialInfo
    {
        #region fields

        private int levelsize;
        private bool hasMaxDed;
        private bool hasPercentDed;
        private bool hasFranchiseDed;
        private float[] minDeds;
        private float[] maxDeds;
        private float[] limits;
        private int[] franchiseMinDedFlags;
        private int[] franchiseMaxDedFlags;
        //private bool[] maxDedFlags;

        private bool isAllAtomicRITEs;
        private bool hasAtomicRITEs;
        private SimpleLevelInfo nodeInfo;
        private AtomicRITELevelInfo aRITEInfo;
        #endregion

        public int LevelSize { get { return levelsize; } }

        public AtomicRITELevelInfo GetaRiteInfo()
        {
            return aRITEInfo;
        }

        public SimpleLevelInfo GetSimpleLevelInfo()
        {
            return nodeInfo;
        }

        #region Constructors

        public TermLevel(int _numOfNodes, int _numOfARites, bool _maxded, bool _percentded, float[] _minDeds, float[] _maxDeds, float[] _limits, 
                                int[] _nodePartitions, int[] _aRITEPartitions, int[] _atomicRITEIndicies,
                                 bool _franchiseded, int[] _franchiseMinDedFlags, int[] _franchiseMaxDedFlags, int[] _nodeFactorsIndex, int[] _aRiteFactorsIndex)
        {
            levelsize = _numOfNodes + _numOfARites;
            hasMaxDed = _maxded;
            hasPercentDed = _percentded;
            hasFranchiseDed = _franchiseded;
            minDeds = _minDeds;
            maxDeds = _maxDeds;
            limits = _limits;
            franchiseMinDedFlags = _franchiseMinDedFlags;
            franchiseMaxDedFlags = _franchiseMaxDedFlags;
            //maxDedFlags = null;
            nodeInfo = new SimpleLevelInfo(_nodePartitions, _numOfNodes, _nodeFactorsIndex);
            aRITEInfo = new AtomicRITELevelInfo(_aRITEPartitions, _numOfARites, _aRiteFactorsIndex, _atomicRITEIndicies);
            hasAtomicRITEs = _numOfARites > 0 ? true : false;
            isAllAtomicRITEs = hasAtomicRITEs && _numOfNodes == 0 ? true : false;                        
        }


        public TermLevel(bool _maxded, bool _percentded, float[] _minDeds, float[] _maxDeds, float[] _limits,
                                SimpleLevelInfo _levelNodeInfo, AtomicRITELevelInfo _levelARTIEInfo
                                , bool _franchiseded, int[] _franchiseMinDedFlags, int[] _franchiseMaxDedFlags, bool[] _maxDedFlags)
        {
            hasMaxDed = _maxded;
            hasPercentDed = _percentded;
            hasFranchiseDed = false;
            minDeds = _minDeds;
            maxDeds = _maxDeds;
            limits = _limits;
            franchiseMinDedFlags = _franchiseMinDedFlags;
            franchiseMaxDedFlags = _franchiseMaxDedFlags;
            //maxDedFlags = _maxDedFlags;
            nodeInfo = _levelNodeInfo;
            aRITEInfo = _levelARTIEInfo;
            hasAtomicRITEs = _levelARTIEInfo.Size > 0 ? true : false;
            isAllAtomicRITEs = hasAtomicRITEs && _levelNodeInfo.Size == 0 ? true : false;
        }

        public TermLevel(int _numOfNodes, int _numOfARites, bool _maxded, bool _percentded, float[] _minDeds, float[] _maxDeds, float[] _limits,
                               int[] _nodePartitions, int[] _aRITEPartitions, int[] _atomicRITEIndicies, int[] _nodeFactorsIndex, int[] _aRiteFactorsIndex)
        {
            levelsize = _numOfNodes + _numOfARites;
            hasMaxDed = _maxded;
            hasPercentDed = _percentded;
            hasFranchiseDed = false;
            minDeds = _minDeds;
            maxDeds = _maxDeds;
            limits = _limits;
            nodeInfo = new SimpleLevelInfo(_nodePartitions, _numOfNodes, _nodeFactorsIndex);
            aRITEInfo = new AtomicRITELevelInfo(_aRITEPartitions, _numOfARites, _aRiteFactorsIndex, _atomicRITEIndicies);
            hasAtomicRITEs = _numOfARites > 0 ? true : false;
            isAllAtomicRITEs = hasAtomicRITEs && _numOfNodes == 0 ? true : false;
        }


        public TermLevel(bool _maxded, bool _percentded, float[] _minDeds, float[] _maxDeds, float[] _limits,
                                SimpleLevelInfo _levelNodeInfo, AtomicRITELevelInfo _levelARTIEInfo)
        {
            hasMaxDed = _maxded;
            hasPercentDed = _percentded;
            minDeds = _minDeds;
            maxDeds = _maxDeds;
            limits = _limits;
            nodeInfo = _levelNodeInfo;
            aRITEInfo = _levelARTIEInfo;
            hasAtomicRITEs = _levelARTIEInfo.Size > 0 ? true : false;
            isAllAtomicRITEs = hasAtomicRITEs && _levelNodeInfo.Size == 0 ? true : false;
        }


        //Construct without financial terms
        public TermLevel(int _numOfNodes, int _numOfARites, int[] _nodePartitions, int[] _aRITEPartitions, int[] _atomicRITEIndicies, int[] _nodeFactorsIndex, int[] _aRiteFactorsIndex)
        {
            levelsize = _numOfNodes + _numOfARites;
            nodeInfo = new SimpleLevelInfo(_nodePartitions, _numOfNodes, _nodeFactorsIndex);
            aRITEInfo = new AtomicRITELevelInfo(_aRITEPartitions, NumOfARITEs, _aRiteFactorsIndex, _atomicRITEIndicies);
            hasAtomicRITEs = _numOfARites > 0 ? true : false;
            isAllAtomicRITEs = hasAtomicRITEs && _numOfNodes == 0 ? true : false;
        }

        //Construct without financial terms
        public TermLevel(SimpleLevelInfo _levelNodeInfo, AtomicRITELevelInfo _levelARTIEInfo)
        {
            nodeInfo = _levelNodeInfo;
            aRITEInfo = _levelARTIEInfo;
            hasAtomicRITEs = _levelARTIEInfo.Size > 0 ? true : false;
            isAllAtomicRITEs = hasAtomicRITEs && _levelNodeInfo.Size == 0 ? true : false;
        }

        #endregion

        public void SetFinancialTerms(bool _maxded, bool _percentded, bool _franchiseded, int[] _franchiseMinDedFlags, int[] _franchiseMaxDedFlags, bool[] _maxDedFlags, float[] _minDeds, float[] _maxDeds, float[] _limits)
        {
            hasMaxDed = _maxded;
            hasPercentDed = _percentded;
            hasFranchiseDed = _franchiseded;
            minDeds = _minDeds;
            maxDeds = _maxDeds;
            limits = _limits;
            franchiseMinDedFlags = _franchiseMinDedFlags;
            franchiseMaxDedFlags = _franchiseMaxDedFlags;
            //maxDedFlags = _maxDedFlags;
            
        }

        #region Overrides for ILevelFinancialInfo
        public bool HasMaxDed { get { return hasMaxDed; } }
        public bool HasPercentDed { get { return hasPercentDed; } }
        public bool HasFranchiseDed { get { return hasFranchiseDed; } }

        
        public float[] GetCodedMinDeds() { return minDeds; }      
        public float[] GetCodedMaxDeds() { return maxDeds; }
        public float[] GetCodedLimits() { return limits; }
        public int[] GetFranchiseMinDedFlags() { return franchiseMinDedFlags; }
        public int[] GetFranchiseMaxDedFlags() { return franchiseMaxDedFlags; }        

        #endregion

        #region Overrides for ILevelNodeAggInfo

        public int NumOfNodes { get { return nodeInfo.Size; } }
        public int[] GetNodeAggregationPartitions() { return nodeInfo.Partitions; }
        public IAggPatternInfo GetNodePatternInfo()
        {
            return nodeInfo.PatternInfo;
        }
        #endregion

        #region Overrides for ILevelAtomicRITEInfo
        public int NumOfARITEs { get { return aRITEInfo.Size; } }
        public bool IsAllAtomicRITEs { get { return isAllAtomicRITEs; } }
        public bool HasAtomicRITEs { get { return hasAtomicRITEs; } }

        public int[] GetGULossIndicies() { return aRITEInfo.AtomicRITEIndicies; }
        public int[] GetARiteAggregationPartitions() { return aRITEInfo.Partitions; }
        public IAggPatternInfo GetARitePatternInfo() { return aRITEInfo.PatternInfo; }

        #endregion
    }

    public class AtomicRITELevel : ILevelAtomicRITEInfo
    {
        #region fields

        private int levelsize;
        private AtomicRITELevelInfo aRITEInfo;
        #endregion

        public AtomicRITELevelInfo GetaRiteInfo()
        {
            return aRITEInfo;
        }
        public int LevelSize { get { return levelsize; } }

        public AtomicRITELevel(int[] _partitions, int size, int[] _factorsIndex, int[] _atomicRITEIndicies)
        {
            levelsize = size;
            aRITEInfo = new AtomicRITELevelInfo(_partitions, size, _factorsIndex, _atomicRITEIndicies);
        }

        public AtomicRITELevel(AtomicRITELevelInfo aRITElevelInfo)
        {
            levelsize = aRITElevelInfo.Size;
            aRITEInfo = aRITElevelInfo;
        }

        #region Overrides for ILevelAtomicRITEInfo
        public int NumOfARITEs { get { return aRITEInfo.Size; } }
        public bool IsAllAtomicRITEs { get { return true; } }
        public bool HasAtomicRITEs { get { return true; } }

        public int[] GetGULossIndicies() { return aRITEInfo.AtomicRITEIndicies; }
        public int[] GetARiteAggregationPartitions() { return aRITEInfo.Partitions; }
        public IAggPatternInfo GetARitePatternInfo() { return aRITEInfo.PatternInfo; }

        #endregion

    }

    public interface ILevelFinancialInfo
    {

        bool HasMaxDed { get; }
        bool HasPercentDed { get; }
        bool HasFranchiseDed { get; }

        float[] GetCodedMinDeds();
        float[] GetCodedMaxDeds();
        float[] GetCodedLimits();
        int[] GetFranchiseMinDedFlags();
        int[] GetFranchiseMaxDedFlags();
       // bool[] GetMaxDedFlags();
    }

    public interface ILevelNodeAggInfo
    {
        int NumOfNodes { get; }
        int[] GetNodeAggregationPartitions();
        IAggPatternInfo GetNodePatternInfo();
        AtomicRITELevelInfo GetaRiteInfo();
        SimpleLevelInfo GetSimpleLevelInfo();        
    }

    public interface ILevelAtomicRITEInfo
    {
        bool IsAllAtomicRITEs { get; }
        bool HasAtomicRITEs { get; }
        int NumOfARITEs { get; }

        int[] GetARiteAggregationPartitions();
        IAggPatternInfo GetARitePatternInfo();
        int[] GetGULossIndicies();
        AtomicRITELevelInfo GetaRiteInfo();
    }

    public class AtomicRITELevelInfo : SimpleLevelInfo
    {
        public int[] AtomicRITEIndicies { get; private set; }

        public AtomicRITELevelInfo(int[] _partitions, int size, int[] _factorsIndex, int[] _atomicRITEIndicies):
            base(_partitions, size, _factorsIndex)
        {
            AtomicRITEIndicies = _atomicRITEIndicies;
        }
    }

    public class SimpleLevelInfo 
    {
        #region Properties
        public int Size { get; protected set; }
        public int[] Partitions { get; protected set; }
        public IAggPatternInfo PatternInfo { get; private set; }

        //co-op factors
        public int[] FactorsIndex { get; private set; }
        public FactorPattern ApplyFactorPattern { get; private set; }
        #endregion

        public SimpleLevelInfo(int[] _partitions, int size, int[] _factorsIndex)
        {
            Size = size;
            Partitions = _partitions;
            FactorsIndex = _factorsIndex;
            PatternInfoGenerator generator = new PatternInfoGenerator();
            PatternInfo = generator.MakePatternInfo(PatternInfoType.Basic, _partitions, size);
            ApplyFactorPattern = generator.MakeFactorPatternInfo(_factorsIndex);
        }



    }

    public class LevelFactory
    {
        private string leveltype = "normal";

    }
    
}
