using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel.Sublimits
{
    public class NetOfDeductibleSublimit : TermObject //ISublimit
    {
        private Amount m_nsl = new Amount();

        public NetOfDeductibleSublimit(double nsl)
        {
            this.m_nsl = new Amount();

            this.m_nsl.value = nsl;

            base.m_priority = 3;
        }


        public NetOfDeductibleSublimit(string label, Amount nsl)
        {
            this.m_nsl = nsl;

            base.label = label;
            base.m_priority = 3;
        }

        public override string ToString()
        {
            return string.Format("NetOfDeductible: nsl={0:f2}", this.m_nsl);
        }

        public void ApplySublimit(CalcState calcState)
        {
            double n = calcState.S - calcState.D;
            double a = Math.Min(this.m_nsl.value, n);
            calcState.X = Math.Max(calcState.X, (n - a));
        }

        public override void ApplyTermObject(CalcState calcState)
        {
            double n = calcState.S - calcState.D;
            double a = Math.Min(this.m_nsl.value, n);
            calcState.X = Math.Max(calcState.X, (n - a));
        }
    }
}
