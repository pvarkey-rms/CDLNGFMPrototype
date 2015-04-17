using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using Rms.Cdl.Backend.DataObjects;

namespace RMS.ContractObjectModel
{
    [Serializable]
    [ProtoContract]
    public class UniversalSubjectPosition
    {
        [ProtoMember(1)]
        public static HashSet<SymbolicValue> CausesOfLoss;

        [ProtoMember(2)]
        public HashSet<int> ResolvedExposureTypes { get; private set; }

        [ProtoMember(3)]
        public HashSet<long> AllRITEIds { get; private set; }

        public HashSet<long> AllRiskItemIds { get; private set; }

        public UniversalSubjectPosition(HashSet<long> allRITEIds, HashSet<long> allRiskItemIds)
        {
            AllRITEIds = allRITEIds;
            AllRiskItemIds = allRiskItemIds;
            CausesOfLoss = new HashSet<SymbolicValue>() { "EQ", "WS", "CS", "FL", "WT", "FR", "TR", "SH", "FF", "SL", "WI", "WA", "SU", "HA", "TO", "SW", "FZ", "IC", "SN", "WF", "TS" };
            ResolvedExposureTypes = new HashSet<int>(ExposureType.GetIndividualIntExposureTypes(ExposureType.EExposureType.Loss));
        }
    }
}
