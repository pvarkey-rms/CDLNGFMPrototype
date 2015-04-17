using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.DataServices.DataObjects;
using System.Diagnostics;
using System.IO;

namespace NGFMReference
{
    public class SubSamplingAnalysisSetting
    {
        public bool UseSubSampling { get; set; }
        public double NmbrSampleBldgScaleFactor { get; set; }
        public int MinSampleBldgs { get; set; }
        public int MaxSampleBldgs { get; set; }
        public string EventWEightsFile { get; set; }
        public string OriginWeightsFile { get; set; }

        public Dictionary<Tuple<int, string, string>, float> OriginWeightsDict { get; set; }  //read weight file to dictionary
        public Dictionary<int, string> ResolutionIntCodeToFieldDict {get; set;}       

        public SubSamplingAnalysisSetting(bool useSubSampling, double nmbrSampleBldgScaleFactor, int minSampleBldgs, int maxSampleBldgs, string eventWEightsFile, string originWeightsFile)
        {
            UseSubSampling = useSubSampling;
            NmbrSampleBldgScaleFactor = nmbrSampleBldgScaleFactor;
            MinSampleBldgs = minSampleBldgs;
            MaxSampleBldgs = maxSampleBldgs;
            EventWEightsFile = eventWEightsFile;           
            OriginWeightsFile = originWeightsFile + "\\OriginWeights_v01.csv";

            SetOriginWeightsDict();
            SetResolutionDict();
        }

        public void SetOriginWeightsDict()
        {
            OriginWeightsDict = new Dictionary<Tuple<int, string, string>, float>();

            if (File.Exists(OriginWeightsFile))
            {
                var strLines = File.ReadLines(OriginWeightsFile);
                foreach (var line in strLines)
                {
                    string[] parts = line.Split(',');
                    Tuple<int, string, string> tempKey = Tuple.Create(int.Parse(parts[0]), parts[1], parts[2]);
                    if (OriginWeightsDict.ContainsKey(tempKey))
                        OriginWeightsDict[tempKey] = float.Parse(parts[3]); //overwrite
                    else
                        OriginWeightsDict.Add(tempKey, float.Parse(parts[3]));
                }
            }           
        }


        public void SetResolutionDict()
        { 
            ResolutionIntCodeToFieldDict = new Dictionary<int, string>();
            AddMapping(1, "CityGeoID");
            AddMapping(2, "PostalCodeGeoID");
            AddMapping(3, "Admin1GeoID");
            AddMapping(4, "Zone1GeoID");
        }

        public void AddMapping(int intCode, string field)
        {
            ResolutionIntCodeToFieldDict.Add(intCode, field);            
        }

    }
}
