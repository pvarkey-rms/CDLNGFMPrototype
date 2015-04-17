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
    public class FixedMatrixGraphPerRisk : FixedMatrixGraph
    {
        public FixedMatrixGraphPerRisk(ExposureDataAdaptor expData) : base(expData) { }

        //Dictionary<int, LevelInfo> allLevelInformation = new Dictionary<int, LevelInfo>();

        public override void Initialize()
        {
            int n1 = 15535;
            int n2 = 4819;

            //level 0: cover level            
            int n = 1;
            bool _maxded = false;
            bool _percentded = false;
            float[] _minDeds = new float[n];
            float[] _maxDeds = new float[n];
            float[] _limits = new float[n];
            int[] _partitions = new int[1];
            _partitions[0] = n;
            int[] _factor1 = new int[n];
            int[] _factor2 = new int[n];


            int[] _atomicRITEIndexes = new int[0];
            int[] _atomicRITEPartitions = new int[0];

            for (int i = 0; i < n; i++)
            {
                _minDeds[i] = 100000 + (float)(0.1 * i);
                _limits[i] = 100000000000;
            }
            TermLevel L0 = new TermLevel(1, 0, _maxded, _percentded, _minDeds, _maxDeds, _limits, _partitions, _atomicRITEPartitions, _atomicRITEIndexes, _factor1, _factor2);


            //Level 1
            n = n1 + n2;
            _maxded = false;
            _percentded = false;
            _minDeds = new float[n];
            _maxDeds = new float[n];
            _limits = new float[n];
            _partitions = new int[n];

            _atomicRITEIndexes = new int[0];
            _atomicRITEPartitions = new int[0];
          
            for (int i = 0; i < n; i++)
            {
                _minDeds[i] = 100 + (float)(0.1 * i);
                _limits[i] = 1000000000;
                _partitions[i] = i + 1; 
            }

            TermLevel L1 = new TermLevel(n, 0, _maxded, _percentded, _minDeds, _maxDeds, _limits, _partitions, _atomicRITEPartitions, _atomicRITEIndexes, _factor1, _factor2);

            //level 2:
            n = n1 + n2;
            _maxded = false;
            _percentded = false;
            _minDeds = new float[n];
            _maxDeds = new float[n];
            _limits = new float[n];
            _partitions = new int[n];

            _atomicRITEIndexes = new int[0];
            _atomicRITEPartitions = new int[0];

            for (int i = 0; i < n; i++)
            {
                _minDeds[i] = 100 + (float)(0.1 * i);
                _limits[i] = 1000000;
                _partitions[i] = i + 1;
            }

            TermLevel L2 = new TermLevel(n, 0, _maxded, _percentded, _minDeds, _maxDeds, _limits, _partitions, _atomicRITEPartitions, _atomicRITEIndexes, _factor1, _factor2);

            //add all levels to Dictionary
            allTermLevelInformation.Add(0, L0);
            allTermLevelInformation.Add(1, L1);
            allTermLevelInformation.Add(2, L2);
        }
    }
}

