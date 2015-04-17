using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class Location : RiskItem
    {
        private List<LocationCvg> m_vLocCvg;

        public Location()
        {
        }

        public void AddCoverage(LocationCvg locCvg)
        {
            if (!this.m_vLocCvg.Contains(locCvg))
            {
                this.m_vLocCvg.Add(locCvg);
            }
        }

        public List<LocationCvg> vLocCvg
        {
            get { return this.m_vLocCvg; }
            set { this.m_vLocCvg = value; }
        }

        public override double GetLossState()
        {
            throw new NotImplementedException();
        }
    }
}
