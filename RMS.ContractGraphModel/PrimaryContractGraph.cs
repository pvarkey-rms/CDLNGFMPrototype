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
    public class PrimaryContractGraph : ContractGraph, IContractGraph
    {
        public PrimaryContractGraph(Contract ThePrimaryContract) : base(ThePrimaryContract) { }
        public PrimaryContractGraph(PrimaryContractGraph CopyFrom) : base(CopyFrom) { }

        protected override bool _AddCovers(IList<IGenericCover> Covers, params object[] AuxilliaryInformation)
        {
            return AddPrimaryCovers(Covers,
                    (Dictionary<long, RiskItemCharacteristicIDAttributes>)AuxilliaryInformation[0],
                    (Dictionary<string, HashSet<long>>)AuxilliaryInformation[1]);
        }

        protected override ResultPosition _Execute(params object[] ExecutionParameters)
        {
            if (ExecutionParameters[2] is SortedDictionary<DateTime, Dictionary<long, Loss>>)
            {
                return ExecutePrimary(
                                   (Dictionary<string, HashSet<long>>)ExecutionParameters[0],
                                   (Dictionary<long, RiskItemCharacteristicIDAttributes>)ExecutionParameters[1],
                                   (SortedDictionary<DateTime, Dictionary<long, Loss>>)ExecutionParameters[2],
                                   (ExecutionParameters.Length == 4) ? (bool)ExecutionParameters[3] : true
                              );
            }
            else if (ExecutionParameters[2] is GULoss)
            {
                return ExecutePrimary(
                               (Dictionary<string, HashSet<long>>)ExecutionParameters[0],
                               (Dictionary<long, RiskItemCharacteristicIDAttributes>)ExecutionParameters[1],
                               (GULoss)ExecutionParameters[2],
                               (ExecutionParameters.Length == 4) ? (bool)ExecutionParameters[3] : true
                          );
            }
            else
                throw new NotImplementedException();
        }
    }
}
