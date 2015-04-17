using System; 
using System.Collections.Generic;
using System.Collections.Concurrent;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractGraphModel
{
    using IGenericCover = ICover<Value, Value, Value>;

    [ProtoContract]
    public interface IContractGraph
    {
        bool AddCovers(IList<IGenericCover> Covers, params object[] AuxilliaryInformation);
        bool RebuildCoverGraph();
        bool AddTerms(IList<ITerm<Value>> Terms, params object[] AuxilliaryInformation);
        bool RebuildTermGraph();
        ResultPosition Execute(params object[] ExecutionParameters);
    }
}
