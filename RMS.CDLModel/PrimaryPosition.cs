using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class PrimaryPosition : SubjectPosition
    {
        public PrimaryPosition(HashSet<SymbolicValue> GrossPosition, HashSet<SymbolicValue> ExposureTypes)
            : this(GrossPosition, new HashSet<SymbolicValue>(), ExposureTypes) { }
        public PrimaryPosition(HashSet<SymbolicValue> GrossPosition, HashSet<SymbolicValue> CausesOfLoss, HashSet<SymbolicValue> ExposureTypes)
            : base(GrossPosition, new HashSet<SymbolicValue>(), CausesOfLoss, ExposureTypes) { }
    }
}
