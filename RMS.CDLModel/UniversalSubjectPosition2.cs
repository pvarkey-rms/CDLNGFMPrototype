using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using Ewah;

using Rms.Cdl.Backend.DataObjects;

namespace RMS.ContractObjectModel
{
    [Serializable]
    [ProtoContract]
    public class UniversalSubjectPosition2
    {
        [ProtoMember(1)]
        HashSet<SymbolicValue> CausesOfLoss;

        [ProtoMember(2)]
        HashSet<int> ResolvedExposureTypes;

        [ProtoMember(3)]
        public long OffsetForAllRITEIds { private set; get; }

        [ProtoMember(4)]
        EwahCompressedBitArray AllRITEIds;

        public UniversalSubjectPosition2(SortedSet<long> allRITEIds)
        {
            CausesOfLoss = new HashSet<SymbolicValue>() { "EQ", "WS", "CS", "FL", "WT", "FR", "TR", "SH", "FF", "SL", "WI", "SU", "HA", "TO", "SW", "FZ", "IC", "SN", "WF", "TS" };
            ResolvedExposureTypes = new HashSet<int>(ExposureType.GetIndividualIntExposureTypes(ExposureType.EExposureType.Loss));

            AllRITEIds = new EwahCompressedBitArray();
            OffsetForAllRITEIds = allRITEIds.First();
            foreach (long RITEId in allRITEIds)
            {
                AllRITEIds.Set((int)(RITEId - OffsetForAllRITEIds));
            }
        }

        #region API
        public HashSet<int> GetResolvedExposureTypes()
        {
            return ResolvedExposureTypes;
        }

        public EwahCompressedBitArray GetAllRITEIds()
        {
            return AllRITEIds;
        }

        public HashSet<SymbolicValue> GetCausesOfLoss()
        {
            return CausesOfLoss;
        }
        #endregion
    }
}
