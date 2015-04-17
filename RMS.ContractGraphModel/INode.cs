using System; 
using System.Collections.Generic;

namespace RMS.ContractGraphModel
{
    public interface INode<T>
    {
        T GetContent();
    }
}
