using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.DataServices.DataObjects;
using System.Reflection;

namespace NGFMReference
{
    public class AggNumSamplesGenerator
    {
        private SubSamplingAnalysisSetting DLMSetting;

        public AggNumSamplesGenerator(SubSamplingAnalysisSetting _DLMSetting)
        {
            //string eventWeightFile = @"D:\NGFM";
            //string originWeightFile = @"D:\NGFM";
            DLMSetting = _DLMSetting;
            //DLMSetting = new DisaggregationAnalysisSetting(true, (float)0.5, 2, eventWeightFile, originWeightFile);
        }

        public int GetNumOfSamples(ExtractRITCharInfo rite)
        {
            if (DLMSetting.UseSubSampling == true)
            {
                float originWeight = GetOriginWeight(rite, DLMSetting);
                return (int)Math.Max(DLMSetting.MinSampleBldgs,
                                 Math.Floor(rite.NumUnits * DLMSetting.NmbrSampleBldgScaleFactor * originWeight));
            }
            else
                return rite.NumBuildings;
        }

        public string GetGEOId(ExtractRITCharInfo rite, SubSamplingAnalysisSetting DLMSetting)
        {
            if (DLMSetting.ResolutionIntCodeToFieldDict.ContainsKey(rite.OriginWeightsGeoIDLookupIntCode))
            {
                string field = DLMSetting.ResolutionIntCodeToFieldDict[rite.OriginWeightsGeoIDLookupIntCode];
                Type type = rite.Address.GetType();
                PropertyInfo info = type.GetProperty(field);
                if (info == null)
                    return null;

                object value = info.GetValue(rite.Address, null);

                return value.ToString();
            }
            else
                throw new InvalidOperationException("Resolution Int code " + rite.OriginWeightsGeoIDLookupIntCode + " not found");
        }

        public float GetOriginWeight(ExtractRITCharInfo rite, SubSamplingAnalysisSetting DLMSetting)
        {
            string geoId = GetGEOId(rite, DLMSetting);
            string lob = rite.OriginWEightsLOBLookupCode;
            int disAggResolution = rite.DisAggResolutionIntCode;

            if (DLMSetting.OriginWeightsDict.ContainsKey(Tuple.Create(disAggResolution, lob, geoId)))
                return DLMSetting.OriginWeightsDict[Tuple.Create(disAggResolution, lob, geoId)];
            else
                return 1;
        }

    }
}

