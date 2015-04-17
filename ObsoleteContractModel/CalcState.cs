using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class CalcState
    {
        #region Field
        private double m_S;
        private double m_D;
        private double m_X;
        #endregion

        #region Constructors
        public CalcState()
        { 
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return string.Format("CalcState: S={0:f2}, D={1:f2}, X={2:f2}", this.m_S, this.m_D, this.m_X);
        }

        public void Reset()
        {
            this.m_D = 0.0;
            this.m_S = 0.0;
            this.m_X = 0.0;
        }
        #endregion

        #region Properties
        public double S
        {
            get { return this.m_S; }
            set { this.m_S = value; }
        }
        public double D
        {
            get { return this.m_D; }
            set { this.m_D = value; }
        }
        public double X
        {
            get { return this.m_X; }
            set { this.m_X = value; }
        }
        #endregion
    }
}
