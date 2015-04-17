using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractGraphModel
{
    interface IIdentifiableINodeCollection<NODEIDENTITY,NODETYPE>
    {
        bool Contains(NODEIDENTITY ElementIdentity);

        INode<NODETYPE> this[NODEIDENTITY ElementIdentity] { get; }

        bool TryGetValue(NODEIDENTITY ElementIdentity, out INode<NODETYPE> value);
    }
}
