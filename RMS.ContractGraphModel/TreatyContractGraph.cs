using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractGraphModel
{
    using IGenericCover = ICover<Value, Value, Value>;

    [Serializable]
    [ProtoContract]
    public class TreatyContractGraph : ContractGraph, IContractGraph
    {
        public TreatyContractGraph(Contract TheTreaty) : base(TheTreaty) { }
        public TreatyContractGraph(TreatyContractGraph CopyFrom, bool ResetAggregateState = true) : base(CopyFrom, ResetAggregateState) { }

        protected override bool _AddCovers(IList<IGenericCover> Covers, params object[] AuxilliaryInformation)
        {
            return AddTreatyCovers(Covers);
        }

        protected override ResultPosition _Execute(params object[] ExecutionParameters)
        {
            return ExecuteTreaty(
                (SortedDictionary<DateTime, double>)ExecutionParameters[0], 
                (ExecutionParameters.Length == 2) ? (Dictionary<SimpleExpression<SymbolicValue>, double>)ExecutionParameters[1] : new Dictionary<SimpleExpression<SymbolicValue>, double>(), 
                false);
        }
    }
}
