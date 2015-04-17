using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel.Sublimits
{
    public class GroundUpSublimit : TermObject //ISublimit
    {
        private Amount m_gsl = new Amount();

        public GroundUpSublimit(double gsl)
        {
            this.m_gsl = new Amount();

            this.m_gsl.value = gsl;

            base.m_priority = 0;
        }



        public GroundUpSublimit(string label, Amount gsl)
        {
            this.m_gsl = gsl;

            base.label = label;
            base.m_priority = 0;
        }

        public override string ToString()
        {
            return string.Format("GroundUpSublimit: gsl={0:f2}", this.m_gsl);
        }

        public void ApplySublimit(CalcState calcState)
        {
            double a = Math.Min(calcState.S, this.m_gsl.value);
            calcState.X = Math.Max(calcState.X, (calcState.S - a));
        }

        public override void ApplyTermObject(CalcState calcState)
        {
            double a = Math.Min(calcState.S, this.m_gsl.value);
            calcState.X = Math.Max(calcState.X, (calcState.S - a));
        }
    }
}
