using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    /// <summary>
    ///  Encapsulates allocation deductible, recoverable and _Limit
    /// </summary>
    public class AllocState
    {
        private double m_allocDeduct;
        private double m_allocRecov;
        private double m_allocPayout;

        public AllocState()
        { 
        
        }

        public void Reset()
        {
            this.m_allocDeduct = 0.0;
            this.m_allocRecov = 0.0;
            this.m_allocPayout = 0.0;
        }

        public override string ToString()
        {
            return string.Format("AllocState: D={0:f2}, R={1:f2}, P={2:f2}", this.m_allocDeduct,this.m_allocRecov,this.m_allocPayout);
        }

        public double AllocPayout
        {
            get { return this.m_allocPayout; }
            set { this.m_allocPayout = value; }
        }

        public double AllocDeduct
        {
            get { return this.m_allocDeduct; }
            set { this.m_allocDeduct = value; }
        }
        public double AllocRecov
        {
            get { return this.m_allocRecov; }
            set { this.m_allocRecov = value; }
        }
    }
}
