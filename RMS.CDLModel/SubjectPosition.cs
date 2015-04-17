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
    [ProtoInclude(14, typeof(Subject))]
    public class SubjectPosition
    {
        #region Fields

        [ProtoMember(1)]
        public HashSet<SymbolicValue> ExposureTypes;

        [ProtoMember(2)]
        public HashSet<SymbolicValue> CausesOfLoss;

        [ProtoMember(3)]
        public HashSet<SymbolicValue> GrossPosition;

        [ProtoMember(4)]
        public HashSet<SymbolicValue> CededPosition;

        #endregion

        #region IsNotConstrained
        public bool isNotConstrained = false;
        #endregion

        #region Derived Subject
        public bool isDerived = false;
        #endregion

        #region Constructors
        public SubjectPosition() : this(new HashSet<SymbolicValue>()) { }
        public SubjectPosition(HashSet<SymbolicValue> GrossPosition)
            : this(GrossPosition, new HashSet<SymbolicValue>()) { }
        public SubjectPosition(HashSet<SymbolicValue> GrossPosition, HashSet<SymbolicValue> CausesOfLoss)
            : this(GrossPosition, new HashSet<SymbolicValue>(), CausesOfLoss, new HashSet<SymbolicValue>()) { }
        public SubjectPosition(HashSet<SymbolicValue> GrossPosition, HashSet<SymbolicValue> CededPosition, HashSet<SymbolicValue> CausesOfLoss, HashSet<SymbolicValue> ExposuresTypes)
        {
            this.GrossPosition = GrossPosition;
            this.CededPosition = CededPosition;
            this.CausesOfLoss = CausesOfLoss;
            this.ExposureTypes = ExposuresTypes;
        }
        public SubjectPosition(SubjectPosition CopyFromSubjectPosition) :
            this(CopyFromSubjectPosition.GrossPosition, CopyFromSubjectPosition.CededPosition, CopyFromSubjectPosition.CausesOfLoss, CopyFromSubjectPosition.ExposureTypes) { }
        #endregion
    }
}
