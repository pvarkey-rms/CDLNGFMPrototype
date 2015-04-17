using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;
using NGFM.Reference.MatrixHDFM;


namespace NGFMReference
{
    public class VectorGUInputGenerator
    {
        private GUInputGenerator GUgenerator;
        private IRITEindexMapper indexMapper;

        public VectorGUInputGenerator(GUInputGenerator inputGenerator, IRITEindexMapper _indexMapper)
        {
            GUgenerator = inputGenerator;
            indexMapper = _indexMapper;
        }

        public IVectorEvent GenerateRITELoss(int eventID)
        {
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double,uint, List<float>>>>> IntialGULoss;
            if (GUgenerator.GenerateRITELoss(eventID))
            {
                IntialGULoss = GUgenerator.GULosses;
            }
            else
                throw new InvalidOperationException("Cannot get ground-up loss for event: " + eventID);

            GULossTransformer transformer = new GULossTransformer(indexMapper);
            IVectorEvent VectorGU = transformer.DictToVector(IntialGULoss);

            return VectorGU;
        }

        //private IVectorEvent TransformGUDictionary(Dictionary<string, Dictionary<int, Dictionary<long, Tuple<uint, List<float>>>>> GULosses)
        //{
        //    float[] GULossVector = new float[indexMapper.TotalNumIndexes];

        //    foreach (string subperil in GULosses.Keys)
        //    {
        //        Dictionary<long, Tuple<uint, List<float>>> LossesByRITE = GULosses[subperil][0];

        //        foreach (KeyValuePair<long, Tuple<uint, List<float>>> pair in LossesByRITE)
        //        {
        //            int BldgID = 0;
        //            foreach(float loss in pair.Value.Item2)
        //            {
        //                int IndexInGULossVector = indexMapper.GetMappedIndex(pair.Key, BldgID, subperil);
        //                GULossVector[IndexInGULossVector] = loss;
        //                BldgID++;
        //            }
        //        }
        //    }

            
        //}
       
    }

    public class VectorGUInputGeneratorFactory: IDisposable
    {
        private GUInputGeneratorFactory GeneratorFactory;
        private static ISubPerilConfig subperilConfig;
        private PartitionDataAdpator pData;
        private HashSet<string> subperils;

        static VectorGUInputGeneratorFactory()
        {
            subperilConfig = new RMSSubPerilConfig();
        }

        public VectorGUInputGeneratorFactory(PartitionData PD, HashSet<string> _subperils, TimeStyle _timestyle, LossStyle _lossstyle, bool UseAggregatedRITEs, SubSamplingAnalysisSetting subSamplingSettings)
        {
            subperils = _subperils;
            bool subperilsSupported = subperils.All(sp => subperilConfig.GetSubPerilList().Contains(sp));

            if (!subperilsSupported)
                throw new InvalidOperationException("Subperils specified cannot be supported at this time");

            pData = new PartitionDataAdpator(PD, subSamplingSettings);
            GeneratorFactory = new GUInputGeneratorFactory(pData, subperils, _timestyle, _lossstyle);
        }

        public VectorGUInputGenerator GetGeneratorForContract(long conID)
        {
            ExposureDataAdaptor expData = pData.GetExposureAdaptor(conID);
            RITEmapper1 mapper = new RITEmapper1(expData, new RAPSettings(subperils),  subperilConfig);
            GUInputGenerator guGenerator = GeneratorFactory.GetGeneratorForContract(conID);

            return new VectorGUInputGenerator(guGenerator, mapper);
        }

        #region IDisposable Override

        // Flag: Has Dispose already been called? 
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here. 
                GeneratorFactory.Dispose();
                pData.Dispose();           
            }

            // Free any unmanaged objects here. 
            
            disposed = true;
        }

        ~VectorGUInputGeneratorFactory()
        {
            Dispose(false);
        }


        #endregion
    }
    
    public class VectorLossEvent : IVectorEvent
    {
        private float[] lossvector;
        private float[] factors;
        private uint[] timestamps;

        public VectorLossEvent(float[] _lossvector, float[] _factors, uint[] _timestamps)
        {
            lossvector = _lossvector;
            factors = _factors;
            timestamps = _timestamps;
        }

        public float[] LossVector { get { return lossvector; } }
        public DateTime EventDate { get { return DateTime.Now; } }
        public float[] Factors { get { return factors; } }
        public uint[] TimeStamps { get { return timestamps; } }
    }

    public interface IRITEindexMapper
    {
        //Mapping for Factor
        int GetMappedFactorIndex(long riteID, int bldg);

        //Assumes Bldgs are numbered from 0 to NumBldgs-1
        int GetMappedIndex(long riteID, int bldg, string subperil);
        int GetMappedIndex(long riteID, string subperil);
        int GetMappedIndex(long riteID);
        AggregateRITEMapper GetAggregateMapper();

        //Reverse Mapping Methods
        long GetRITEIDFromIndex(int UniqueAriteIndex);
        string GetSubPerilFromIndex(int UniqueAriteIndex);

        bool IndexExists(long riteID, string subperil);

        float[] GetOrderedTIVArray();
        int[] GetOrderedNumBldgs();

        int TotalNumIndexes { get; }
        int TotalNumRITEs { get; }
        int TotalNumBldgs { get;  }
        int TotalNumRITESBySubPeril { get; }

        //Method For Allocation Adaptor
        RITCharacteristic GetRITCharObject(long ID);
    }

    public class RITEmapper1 : IRITEindexMapper
    {
        private ExposureDataAdaptor expData;
        private Dictionary<long, Dictionary<string, int>> preBldgIndex;
        private Dictionary<long, int> CummulativeBuildingMap;
        private Dictionary<long, int> RITEIDsToIndex;
        private float[] orderedTIVArray;
        private int[] orderedNumBldgsList;
        private int[] multiplierArray;

        private AggregateRITEMapper aggregatemapper;

        //Reverse dictionarys
        private Dictionary<int, long> UniqueARTIEIndexToRITEID;
        private Dictionary<int, string> UniqueARTIEIndexToSubPeril;

        private SubPerilMapping SPmapping;
        public int TotalNumIndexes { get; private set; }
        public int TotalNumRITEs { get; private set; }
        public int TotalNumBldgs { get; private set; }
        public int TotalNumRITESBySubPeril { get { return TotalNumRITEs * SPmapping.NumOfSubPerils; } }

        public RITEmapper1(ExposureDataAdaptor _expData, RAPSettings _rapsettings, ISubPerilConfig _subperilConfig)
        {
            expData = _expData;
            HashSet<string> subPerilInputList = new HashSet<string>(_rapsettings.SubPerils.Union(_expData.ContractCOLs.GetSubperils()));
            SPmapping = new SubPerilMapping(_subperilConfig, subPerilInputList);
            preBldgIndex = new Dictionary<long, Dictionary<string, int>>();
            CummulativeBuildingMap = new Dictionary<long, int>();
            UniqueARTIEIndexToRITEID = new Dictionary<int, long>();
            UniqueARTIEIndexToSubPeril = new Dictionary<int, string>();

            aggregatemapper = new AggregateRITEMapper(expData);
            BuildIndexDictionary();
        }

        private void BuildIndexDictionary()
        {        
            long[] orderedRITEIDs = expData.Characteristics.Select(rite => rite.ID).OrderBy(id => id).ToArray();

            orderedNumBldgsList = expData.Characteristics.OrderBy(rite => rite.ID)
                                                                   .Select(rite => rite.ParentRITE.ActNumOfSampleBldgs)
                                                                   .ToArray();
            int[] orderedNumBldgsListForTIV = expData.Characteristics.OrderBy(rite => rite.ID)
                                                                   .Select(rite => rite.ParentRITE.NumOfTrueBldgs)
                                                                   .ToArray();

            //Convert orderedRITEIDs to Dictionary
            RITEIDsToIndex = new Dictionary<long, int>();
            for (int i = 0; i < orderedRITEIDs.Length; i++)
            {
                RITEIDsToIndex.Add(orderedRITEIDs[i], i);
            }

            int NumOfSubperils = SPmapping.NumOfSubPerils;
            TotalNumRITEs = orderedRITEIDs.Count();
            TotalNumBldgs = orderedNumBldgsList.Sum();
            int[] orderedRITEIndexs = new int[orderedRITEIDs.Count()];
            int[]  cummulativeNumBldgsList = new int[TotalNumRITEs];

            int cummulativeIndex = 0;
            int cummulativeBldgs = 0;
            for (int i = 0; i < TotalNumRITEs; i++)
            {
                orderedRITEIndexs[i] = cummulativeIndex;
                cummulativeNumBldgsList[i] = cummulativeBldgs;
                cummulativeIndex = cummulativeIndex + NumOfSubperils * orderedNumBldgsList[i];
                cummulativeBldgs = cummulativeBldgs + orderedNumBldgsList[i];
            }
            TotalNumIndexes = cummulativeIndex;
            orderedTIVArray = new float[TotalNumIndexes];

            for (int i = 0; i < TotalNumRITEs; i++)
            {
                long RITEID = orderedRITEIDs[i];
                int NumOfBldgs = orderedNumBldgsList[i];
                preBldgIndex.Add(RITEID, new Dictionary<string,int>());
                CummulativeBuildingMap.Add(RITEID, cummulativeNumBldgsList[i]);

                foreach (string subperil in SPmapping.GetSubPerilList())
                {
                    int finalIndex = orderedRITEIndexs[i] + SPmapping.GetSubperilCode(subperil) * NumOfBldgs;
                    preBldgIndex[RITEID].Add(subperil, finalIndex);
                    
                    float BldgTIV = (float)expData.CharacteristicsDict[RITEID].TIV / orderedNumBldgsListForTIV[i];
                    //double[] MultArray = expData.CharacteristicsDict[RITEID].ParentRITE.GetMultiplierArr;
                    //loop through all buildings and set TIV
                    for (int bldgID = 0; bldgID < NumOfBldgs; bldgID++)
                    {
                        int uniqueIndex = finalIndex + bldgID;
                        //orderedTIVArray[uniqueIndex] = BldgTIV * (float)MultArray[bldgID];
                        orderedTIVArray[uniqueIndex] = BldgTIV;

                        //Build reverse dictonarys
                        UniqueARTIEIndexToRITEID.Add(uniqueIndex, RITEID);
                        UniqueARTIEIndexToSubPeril.Add(uniqueIndex, subperil);
                    }
                }
            }
        }


        //Override of IRITEindexMapper
        #region Override of IRITEindexMapper

        public int GetMappedIndex(long riteID, int bldg, string subperil)
        {
            return preBldgIndex[riteID][subperil] + bldg;
        }

        public int GetMappedIndex(long riteID, string subperil)
        {
            return SPmapping.NumOfSubPerils * RITEIDsToIndex[riteID] + SPmapping.GetSubperilCode(subperil);
        }

        public float[] GetOrderedTIVArray()
        {
            return orderedTIVArray;
        }

        public int[] GetOrderedNumBldgs()
        {
            return orderedNumBldgsList;
        }

        public int GetMappedIndex(long riteID)
        {
            return RITEIDsToIndex[riteID];
        }

        public bool IndexExists(long riteID, string subperil)
        {
            return preBldgIndex.ContainsKey(riteID) && preBldgIndex[riteID].ContainsKey(subperil);
        }

        public AggregateRITEMapper GetAggregateMapper()
        {
            return aggregatemapper;
        }

        public int GetMappedFactorIndex(long riteID, int bldg)
        {
            if (riteID == -1)  //TODO: mean there is no riteID associted with it
                return -1;
            else
                return CummulativeBuildingMap[riteID] + bldg;
        }

        public long GetRITEIDFromIndex(int UniqueAriteIndex)
        {
            return UniqueARTIEIndexToRITEID[UniqueAriteIndex];
        }

        public string GetSubPerilFromIndex(int UniqueAriteIndex)
        {
            return UniqueARTIEIndexToSubPeril[UniqueAriteIndex];
        }

        public RITCharacteristic GetRITCharObject(long ID)
        {
            return expData.CharacteristicsDict[ID];
        }

        #endregion

    }
    
    public interface ISubPerilConfig
    {
        List<string> GetSubPerilList();
        int NumOfSubPerils { get; }
        int GetSubperilCode(string subperil);
        bool SubPerilSupported(string subperil);
    }

    public class RMSSubPerilConfig : ISubPerilConfig
    {
        public RMSSubPerilConfig()
        {
            Intialize();
        }

        private Dictionary<string, int> subperilCodes;

        private void Intialize()
        {
            subperilCodes = new Dictionary<string, int>();

            subperilCodes.Add("SH", 0);
            subperilCodes.Add("FF", 1);
            subperilCodes.Add("SL", 2);
            subperilCodes.Add("WI", 3);
            subperilCodes.Add("WA", 5);
            subperilCodes.Add("SU", 4);
            subperilCodes.Add("FL", 6);

            subperilCodes.Add("WS", 7);
            subperilCodes.Add("EQ", 8);

            //subperilCodes.Add("EQ", 0);
            //subperilCodes.Add("WS", 1);
            //subperilCodes.Add("WI", 2);
            //subperilCodes.Add("FL", 3);
            //subperilCodes.Add("WA", 4);

            //subperilCodes.Add("WS", 0);
            //subperilCodes.Add("WA", 1);
            //subperilCodes.Add("WI", 2);
            //subperilCodes.Add("EQ", 3);
            //subperilCodes.Add("FL", 4);
        }

        #region Override ISubPerilConfig

        public List<string> GetSubPerilList()
        {
            return subperilCodes.Keys.ToList();
        }
        public int NumOfSubPerils { get {return subperilCodes.Count;}}

        public int GetSubperilCode(string subperil)
        {
            if (!subperilCodes.ContainsKey(subperil))
                throw new InvalidOperationException("Subperil requeted is not supported by RMS or me!!!!");
            else
                return subperilCodes[subperil];
        }
        public bool SubPerilSupported(string subperil)
        {
            if (subperilCodes.ContainsKey(subperil))
                return true;
            else
                return false;
        }
        #endregion

    }

    public class SubPerilMapping
    {
        private Dictionary< string, int> mapping;


        public SubPerilMapping(ISubPerilConfig config, HashSet<string> subperils)
        {
            mapping = new Dictionary<string, int>();
            BuildMapping(subperils.ToList(), config);
        }

        private void BuildMapping(List<string> subperils, ISubPerilConfig config)
        {
            subperils.Sort(new SubPerilComparer(config));

            int i = 0;
            foreach(string s in subperils)
            {
                mapping.Add(s, i);
                i++;
            }
        }

        public int GetSubperilCode(string subperil)
        {
            if (!mapping.ContainsKey(subperil))
                throw new InvalidOperationException("Subperil requeted is not in mapping!!!!");
            else
                return mapping[subperil];
        }

        public List<string> GetSubPerilList()
        {
            return mapping.Keys.ToList();
        }
        public int NumOfSubPerils { get { return mapping.Count; } }


        private class SubPerilComparer: IComparer<string>
        {
            private ISubPerilConfig config;

            public SubPerilComparer(ISubPerilConfig _config)
            {
                config = _config;
            }

            public int Compare(string x, string y)
            {
                return config.GetSubperilCode(x).CompareTo(config.GetSubperilCode(y));
            }
        }

    }
 
}
