using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public interface IEvaluatableValue : IValue<AValue>
    {
        double GetEvaluatedValue(params object[] bindings);
    }
}