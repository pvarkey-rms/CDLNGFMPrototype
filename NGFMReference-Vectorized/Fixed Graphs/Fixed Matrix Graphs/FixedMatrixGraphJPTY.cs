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

namespace NGFMReference
{
    public class FixedMatrixGraphJPTY : FixedMatrixGraph
    {
        public FixedMatrixGraphJPTY(ExposureDataAdaptor expData)
            : base(expData) 
        {
            
        }
        //Dictionary<int, LevelInfo> allLevelInformation = new Dictionary<int, LevelInfo>();

        public override void Initialize()
        {
            int subPerilIndex = 3; //WI
            ISubPerilConfig subperilInfo = new RMSSubPerilConfig();
            RAPSettings setting = new RAPSettings(new HashSet<string> { "WI", "WA" });
            IRITEindexMapper mapper = new RITEmapper1(expData, setting, subperilInfo);
           
            //int m = 45408;
            int m = 22704;
            //hard-coded the NumOfBuildingArrray
            /////////////////////////////////////////////////////level 0: cover level            
            int n = 1;
            bool _maxded = false;
            bool _percentded = false;
            float[] _minDeds = new float[n];
            float[] _maxDeds = new float[n];
            float[] _limits = new float[n];
            int[] _partitions = new int[1];
            _partitions[0] = n;

            int[] _atomicRITEIndexes = new int[0];
            int[] _atomicRITEPartitions = new int[0];

            int[] _factor1 = new int[n];
            int[] _factor2 = new int[n];


            for (int i = 0; i < n; i++)
            {
                _minDeds[i] = 0;
                _limits[i] = 1000000000000;
            }
            TermLevel L0 = new TermLevel(n, 0, _maxded, _percentded, _minDeds, _maxDeds, _limits, _partitions, _atomicRITEPartitions, _atomicRITEIndexes, _factor1, _factor2);


            ////////////////////////////////////////////////Level 1
            n = m;
            _maxded = false;
            _percentded = false;
            _minDeds = new float[n];
            _maxDeds = new float[n];
            _limits = new float[n];
            _partitions = new int[1];

            _partitions[0] = n;

            _atomicRITEIndexes = new int[0];
            _atomicRITEPartitions = new int[0];

            for (int i = 0; i < n; i++)
            {
                _minDeds[i] = (float)296564.4820;
                _limits[i] = 10000000000;
            }

            TermLevel L1 = new TermLevel(m, 0, _maxded, _percentded, _minDeds, _maxDeds, _limits, _partitions, _atomicRITEPartitions, _atomicRITEIndexes, _factor1, _factor2);

            ///////////////////////////////////level 2:
            n = m;
            _maxded = false;
            _percentded = false;
            _minDeds = new float[n];
            _maxDeds = new float[n];
            _limits = new float[n];
            _partitions = new int[n];

            _atomicRITEIndexes = new int[n];
            _atomicRITEPartitions = new int[n];

            for (int i = 0; i < n; i++)
            {                                       
                _minDeds[i] = 0;
                _limits[i] = (float)444846.7230;
                _partitions[i] = i + 1;
                _atomicRITEPartitions[i] = i + 1;
            }

            List<int> AtomicRITEIndexesList = new List<int>();

            foreach (RITCharacteristic RITE in expData.Characteristics)
            {
                if (RITE.ExpType == ExposureType.CoverageA)
                {
                    int numBldgd = RITE.ParentRITE.NumOfSampleBldgs;
                    for (int i = 0; i < numBldgd; i++)
                    {
                        int uniqueIndex = mapper.GetMappedIndex(RITE.ID, i, "WI");
                        AtomicRITEIndexesList.Add(uniqueIndex);
                    }
                }
            }

            _atomicRITEIndexes = AtomicRITEIndexesList.ToArray();

            TermLevel L2 = new TermLevel(n, n, _maxded, _percentded, _minDeds, _maxDeds, _limits, _partitions, _atomicRITEPartitions, _atomicRITEIndexes, _factor1, _factor2);

            //////////////////////////////////////////////level 3 (AtomicRiteLevel, lowest level):
            n = m;

            _atomicRITEPartitions = new int[n];

            for (int i = 0; i < n; i++)
            {
                _atomicRITEPartitions[i] = i + 1;
            }

            AtomicRITEIndexesList = new List<int>();

            foreach (RITCharacteristic RITE in expData.Characteristics)
            {
                if (RITE.ExpType == ExposureType.CoverageC)
                {
                    int numBldgd = RITE.ParentRITE.NumOfSampleBldgs;
                    for (int i = 0; i < numBldgd; i++)
                    {
                        int uniqueIndex = mapper.GetMappedIndex(RITE.ID, i, "WI");
                        AtomicRITEIndexesList.Add(uniqueIndex);
                    }
                }
            }

            _atomicRITEIndexes = AtomicRITEIndexesList.ToArray();

            //////////////////////////////////////////Finally Set MatrixGraph properties...
            lowestLevelInfo = new AtomicRITELevel(_atomicRITEPartitions, n, _factor1, _atomicRITEIndexes);
            allTermLevelInformation.Add(0, L0);
            allTermLevelInformation.Add(1, L1);
            allTermLevelInformation.Add(2, L2);

            contractInfo = new ContractInfo(false,false);    
        }
    }
}


