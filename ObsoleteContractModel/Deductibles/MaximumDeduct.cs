using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel.Deductibles
{
    public class MaximumDeduct : TermObject //IDeductible
    {
        private Amount m_maxd = new Amount();

        public MaximumDeduct(double maxd)
        {
            this.m_maxd = new Amount();
            this.m_maxd.value = maxd;

            base.m_priority = 2;
        }

        public MaximumDeduct(string label, Amount maxd)
        {
            this.m_maxd = maxd;

            base.label = label;
            base.m_priority = 2;
        }

        public override string ToString()
        {
            return string.Format("MaximumDeduct: maxd={0:f2}", this.m_maxd);
        }

        //public void ApplyDeductible(CalcState calcState)
        //{
        //    double a = Math.Min(calcState.S, this.m_maxd);
        //    calcState.D = Math.Min(calcState.D, a);
        //}

        public override void ApplyTermObject(CalcState calcState)
        {
            double a = Math.Min(calcState.S, this.m_maxd.value);
            calcState.D = Math.Min(calcState.D, a);
        }
    }
}