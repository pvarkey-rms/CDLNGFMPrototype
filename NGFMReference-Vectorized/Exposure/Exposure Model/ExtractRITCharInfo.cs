using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class ExtractRITCharInfo
    {
        
        public int ExposureType { set; get; }

        public int NumBuildings { set; get; }

        public int NumSamples { set; get; }

        public double Factor { set; get; }

        //fields needed for compute numSamples for Agg location
        public int NumUnits { get; set; }
        public float HazardScaleFactor { get; set; } //TaxValue in CommonCharacteristics, not used explicitly in the algorithm
        public int OriginWeightsGeoIDLookupIntCode { get; set; } //FireAlarm in CommonCharacteristics
        public string OriginWEightsLOBLookupCode { get; set; }  //FirePlace in CommonCharacteristics
        public int DisAggResolutionIntCode { get; set; }  //NumberOfEscalators in CommonCharacteristics
        public object Address { get; set; }
        //

        // RiskItemID
        public long RITExposureId { set; get; }

        public long OriginalRITExposureId { set; get; }

        public long OriginalRITECharacteristicId { set; get; }

        // RiskItemID of campus principal
        public long ParentRITExposureId { set; get; }

        public double Value { set; get; }

        public bool IsCampus { set; get; }

        public bool IsAggregateLowRes { set; get; }

        public List<string> ScheduleSymbols { set; get; }

        public ExtractRITCharInfo()
        {
            ScheduleSymbols = new List<string>();
            IsAggregateLowRes = false;
        }
        public ExtractRITCharInfo(ExtractRITCharInfo ra)
            : this()
        {
            if (ra != null)
            {
                this.ExposureType = ra.ExposureType;
                this.NumBuildings = ra.NumBuildings;
                this.RITExposureId = ra.RITExposureId;
                this.ParentRITExposureId = ra.ParentRITExposureId;
                this.Value = ra.Value;
                this.IsCampus = ra.IsCampus;
                this.OriginalRITExposureId = ra.OriginalRITExposureId;
                this.OriginalRITECharacteristicId = ra.OriginalRITECharacteristicId;
                this.IsAggregateLowRes = ra.IsAggregateLowRes;

                this.NumUnits = ra.NumUnits;
                this.HazardScaleFactor = ra.HazardScaleFactor;
                this.OriginWeightsGeoIDLookupIntCode = ra.OriginWeightsGeoIDLookupIntCode;
                this.OriginWEightsLOBLookupCode = ra.OriginWEightsLOBLookupCode;
                this.DisAggResolutionIntCode = ra.DisAggResolutionIntCode;
                this.Address = ra.Address;
            }
        }

        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3},{4},{5},\"{6}\"",
                ExposureType, NumBuildings, RITExposureId, ParentRITExposureId, Value, IsCampus, string.Join(";", ScheduleSymbols.ToArray()));
        }

        public static string GetHeader()
        {
            return "ExposureType,NumBuildings,RITExposureId,ParentRITExposureId,Value,IsCampus,ScheduleSymbols";
        }

    }

    
}
