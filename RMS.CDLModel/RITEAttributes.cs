using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace RMS.ContractObjectModel
{
    [Serializable]
    [ProtoContract]
    public class RiskItemCharacteristicIDAttributes
    {
        [ProtoMember(1)]
        public int ExposureType { set; get; }

        [ProtoMember(2)]
        public int NumBuildings { set; get; }

        [ProtoMember(3)]
        // RiskItemID
        public long RITExposureId { set; get; }

        public long? OriginalRITExposureId { set; get; }

        public long? OriginalRITECharacteristicId { set; get; }

        [ProtoMember(4)]
        // RiskItemID of campus principal
        public long ParentRITExposureId { set; get; }

        [ProtoMember(5)]
        public double Value { set; get; }

        public bool IsCampus { set; get; }

        public List<string> ScheduleSymbols { set; get; }

        public RiskItemCharacteristicIDAttributes()
        {
            ScheduleSymbols = new List<string>();
        }
        public RiskItemCharacteristicIDAttributes(RiskItemCharacteristicIDAttributes ra)
            :this()
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
