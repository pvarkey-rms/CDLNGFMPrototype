using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace RMS.ContractObjectModel
{
    [ProtoContract]
    public interface IExpression <out V>
        where V : IValue<AValue>
    {
        V GetValue();

        double GetEvaluatedValue(params object[] bindings);

        void SetValue(IValue<AValue> val);
    }
}
