using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class NetPosition : SubjectPosition
    {
        public NetPosition(HashSet<SymbolicValue> GrossPosition, HashSet<SymbolicValue> CededPosition)
            : base(GrossPosition, CededPosition, new HashSet<SymbolicValue>(), new HashSet<SymbolicValue>()) { }
        public NetPosition(NetPosition CopyFromNetPosition) :
            this(CopyFromNetPosition.GrossPosition, CopyFromNetPosition.CededPosition) { }
    }
}
