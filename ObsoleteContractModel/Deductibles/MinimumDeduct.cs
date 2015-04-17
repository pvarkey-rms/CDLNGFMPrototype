using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel.Deductibles
{
    public class MinimumDeduct : TermObject //IDeductible
    {
        private Amount m_mind = new Amount();

        private ENUM_DEDUCTIBLE_OPTION m_bAbsorbing;

        public MinimumDeduct(double mind, bool bAbsorbing)
        {
            this.m_mind = new Amount();
            this.m_mind.value = mind;

            this.m_bAbsorbing = bAbsorbing == true ? ENUM_DEDUCTIBLE_OPTION.OPTION_DED_ABSORBABLE : ENUM_DEDUCTIBLE_OPTION.OPTION_NONE;

            base.m_priority = 1;
        }



        public MinimumDeduct(string label, Amount mind, ENUM_DEDUCTIBLE_OPTION bAbsorbing)
        {
            this.m_mind = mind;

            this.m_bAbsorbing = bAbsorbing;

            base.label = label;
            base.m_priority = 1;
        }

        public override string ToString()
        {
            return string.Format("MinimumDeduct: mind={0:f2}, bAbsorbable={1}", this.m_mind, this.m_bAbsorbing);
        }


        public override void ApplyTermObject(CalcState calcState)
        {
            double a = 0.0;
            if (this.m_bAbsorbing.Equals(ENUM_DEDUCTIBLE_OPTION.OPTION_DED_ABSORBABLE))
            {
                a = Math.Min(calcState.S, this.m_mind.value) - calcState.X;

            }
            else
            {
                a = Math.Min(calcState.S, this.m_mind.value);
            }

            calcState.D = Math.Max(calcState.D, a);
        }

        public bool bAbsorbing
        {
            get
            {
                if (this.m_bAbsorbing.Equals(ENUM_DEDUCTIBLE_OPTION.OPTION_DED_ABSORBABLE))
                    return true;
                else

                    return false;
            }

            set
            {
                if (value)
                    this.m_bAbsorbing = ENUM_DEDUCTIBLE_OPTION.OPTION_DED_ABSORBABLE;
                else
                    this.m_bAbsorbing = ENUM_DEDUCTIBLE_OPTION.OPTION_NONE;
            }
        }
    }
}
