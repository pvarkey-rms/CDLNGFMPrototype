using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public interface AValue
    {
    }

    public interface IValue<out AValue>
    {
    }
}
