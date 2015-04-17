using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractGraphModel
{
    interface IDirectedGraph<T> : ICollection<INode<T>>
    {
        ICollection<INode<T>> GetChildrenOfNode(INode<T> node);
        ICollection<INode<T>> GetParentsOfNode(INode<T> node);

        bool MakeParentChildEdge(INode<T> parent, INode<T> child, bool AddAnyNodesNotPresent);
    }
}
