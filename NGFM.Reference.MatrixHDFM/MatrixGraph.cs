using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.DataServices.DataObjects;
using Rms.Analytics.DataService.Zip;
using System.Net;
using HasseManager;
using System.Diagnostics;
using NGFM.Reference.MatrixHDFM;

namespace NGFM.Reference.MatrixHDFM
{
    public abstract class MatrixGraph : IExecutableMatrixGraph
    {
        #region fields
        protected Dictionary<int, TermLevel> allTermLevelInformation;
        protected AtomicRITELevel lowestLevelInfo;
        protected ContractInfo contractInfo;

        //For covers:
        protected Dictionary<int, CoverLevel> allCoverLevelInformation;
        protected LowestCoverLevel lowestcoverlevelInfo;

        #endregion

        public abstract void Initialize();

        public MatrixGraph()
        {
            allTermLevelInformation = new Dictionary<int, TermLevel>();
            allCoverLevelInformation = new Dictionary<int, CoverLevel>();
            //residualARITEs = new Dictionary<int, AtomicRITE>();
        }
        

        #region overrides for IExecutableMatrixGraph

        public ContractInfo ContractInfo { get { return contractInfo; } }

        public ILevelNodeAggInfo GetNodeAggInfo(int level)
        {
            if (level == LowestLevel)
                throw new ArgumentOutOfRangeException("Cannot get Term inforamtion for lowest level, this level will have Atomic Rites only!");
            else
            {
                if (allTermLevelInformation.ContainsKey(level))
                    return allTermLevelInformation[level];
                else
                    throw new ArgumentOutOfRangeException("No aggregation level information for level : " + level);
            }
        }

        public ILevelAtomicRITEInfo GetAtomicRITEInfo(int level)
        {
            if (level == LowestLevel)
                return lowestLevelInfo;
            else
            {
                if (allTermLevelInformation.ContainsKey(level))
                    return allTermLevelInformation[level];
                else
                    throw new ArgumentOutOfRangeException("No aRITE level information for level : " + level);
            }
        }

        public ILevelFinancialInfo GetTermLevelInfo(int level)
        {
            if (level == LowestLevel)
                throw new ArgumentOutOfRangeException("Cannot get Term inforamtion for lowest level, this level will have Atomic Rites only!");
            else
            {
                if (allTermLevelInformation.ContainsKey(level))
                    return allTermLevelInformation[level];
                else
                    throw new ArgumentOutOfRangeException("No term level information for level : " + level);
            }
        }

        public int NumOfLevels
        {
            get
            {
                if (allTermLevelInformation.Count == 0)
                    return 0;
                else
                    return allTermLevelInformation.Count + 1;
            }
        }

        public int LowestLevel
        { 
            get 
            {
                if (NumOfLevels == 0)
                    return 0;
                else
                    return NumOfLevels - 1; 
            } 
        }

        public int NumOfTermLevels 
        {
            get 
            {
                if (NumOfLevels == 0)
                    return 0;
                else
                    return NumOfLevels - 1; 
            }
        }

        //For Covers:
        public int NumOfLevelsForCover { get { return allCoverLevelInformation.Count + 1; } }

        public int NumOfCoverLevels { get { return NumOfLevelsForCover - 1; } }

        public int LowestCoverLevel { get { return LowestLevel + 1; } }

        public ICoverLevelNodeAggInfo GetCoverNodeAggInfo(int level)
        {
            if (level == LowestCoverLevel)
                throw new ArgumentOutOfRangeException("Cannot get Cover information for lowest level, this level will have Atomic Rites only!");
            else
            {
                if (allCoverLevelInformation.ContainsKey(level))
                    return allCoverLevelInformation[level];
                else
                    throw new ArgumentOutOfRangeException("No aggregation level information for level : " + level);
            }
        }

        public ICoverLevelResiduleInfo GetCoverResiduleInfo()
        {
            return lowestcoverlevelInfo;
        }

        public ICoverLevelTermAriteInfo GetCoverAriteInfo()
        {
            return lowestcoverlevelInfo;
        }

        public LowestCoverLevelInfo GetLowestCoverLevelInfo()  //Rain, added to just make sense of the name.
        {
            return lowestcoverlevelInfo.GetLowestCoverLevelInfo();
        }

        public ICoverLevelFinancialInfo GetCoverLevelInfo(int level)
        {
            if (level == LowestCoverLevel)
                throw new ArgumentOutOfRangeException("Cannot get Cover Financial inforamtion for lowest level, this level will not have Financial Info!");
            else
            {
                if (allCoverLevelInformation.ContainsKey(level))
                    return allCoverLevelInformation[level];
                else
                    throw new ArgumentOutOfRangeException("No cover level information for level : " + level);
            }
        }

        public IDerivedCoverLevelNodeAggInfo GetDerivedCoverNodeAggInfo(int level)
        {
            if(level == LowestCoverLevel || level == LowestCoverLevel +1)
                throw new ArgumentOutOfRangeException("Cannot get Cover Derived inforamtion for lowest level or leaflevel, this level will not have Derived Info!");
            else
            {
                if (allCoverLevelInformation.ContainsKey(level))
                    return (DerivedCoverLevel)allCoverLevelInformation[level];
                else
                    throw new ArgumentOutOfRangeException("No cover level information for level : " + level);
            }
        }

        //public float[] LowestlevelARITEindexes { get { return lowestlevelARITEindexes; } }
        //public int TotalNumAtomicRITEs { get { return totalNumARITEs; } }

        #endregion
    }

    public class AutoMatrixGraph : MatrixGraph, IBuildableMatrixGraph
    {
        public AutoMatrixGraph() : base() { }
        public override void Initialize()
        {
        }

        #region overrides for IBuildableMatrixGraph

        public void AddTermLevel(int level, TermLevel termLevel)
        {
            allTermLevelInformation.Add(level, termLevel);
        }
        //void SetLowestLevel(int level);
        public void SetLowestARITELevel(AtomicRITELevel aRITELevel)
        {
            lowestLevelInfo = aRITELevel;
        }

        //Set up for Covers:
        public void AddLeafCoverLevel(int level, CoverLevel coverlevel)
        {
            allCoverLevelInformation.Add(level, coverlevel);
        }

        public void AddDerivedCoverLevel(int level, DerivedCoverLevel derivedCoverLevel)
        {
            allCoverLevelInformation.Add(level, derivedCoverLevel);
        }

        public void SetLowestCoverLevel(LowestCoverLevel lowestCoverLevel)
        {
            lowestcoverlevelInfo = lowestCoverLevel;
        }

        public void SetContractInfo(ContractInfo Contractinfo)
        {
            contractInfo = Contractinfo;
        }

        #endregion
    }

    public interface IExecutableMatrixGraph
    {
        ContractInfo ContractInfo { get; }

        ILevelNodeAggInfo GetNodeAggInfo(int level);
        ILevelAtomicRITEInfo GetAtomicRITEInfo(int level);
        ILevelFinancialInfo GetTermLevelInfo(int level);
        int NumOfLevels { get; }
        int NumOfTermLevels { get; }
        int LowestLevel { get; }

        //For covers:
        ICoverLevelNodeAggInfo GetCoverNodeAggInfo(int level);
        IDerivedCoverLevelNodeAggInfo GetDerivedCoverNodeAggInfo(int level);
        ICoverLevelTermAriteInfo GetCoverAriteInfo();
        ICoverLevelResiduleInfo GetCoverResiduleInfo();
        ICoverLevelFinancialInfo GetCoverLevelInfo(int level);
        LowestCoverLevelInfo GetLowestCoverLevelInfo();

        int NumOfLevelsForCover { get; }
        int NumOfCoverLevels { get; }
        int LowestCoverLevel { get; }
    }

    public interface IBuildableMatrixGraph
    {
        //void SetTermLevelDictionary(Dictionary<int, TermLevel> allTermLevelInformation);
        void AddTermLevel(int level, TermLevel termLevel);
        //void SetLowestLevel(int level);
        void SetLowestARITELevel(AtomicRITELevel aRITELevel);
        void SetContractInfo(ContractInfo Contractinfo);

        //Set up for Covers
        void AddLeafCoverLevel(int level, CoverLevel coverlevel);
        void AddDerivedCoverLevel(int level, DerivedCoverLevel derivedCoverLevel);
        void SetLowestCoverLevel(LowestCoverLevel lowestCoverLevel);
    }

}
