using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class LocationCvg : RiskItem
    {
        #region Fields
        private int m_locExtnId;
        private int m_perilExtnId;

        private string m_expType;
        private double m_exposure;

        private double m_lossState;
        #endregion

        #region Constructors
        public LocationCvg(string extnId)
        {
            base.ExtnId = extnId;
        }
        public LocationCvg()
        {

        }
        #endregion

        #region Methods

        public static void ResetIdentity()
        {
            s_idGen.Reset();
        }
        #endregion

        #region ToString()
        public override string ToString()
        {
            return string.Format("LocationCvg: intId={0}, extnId={1}, lossType={2}, perilIntId={3}, locExtnId={4}, exposure={5}",
                this.m_intId, this.m_extnId, this.m_expType, this.m_perilExtnId, this.m_locExtnId, this.m_exposure);
        }
        #endregion

        #region Properties

        #endregion

        public void SetLossState(double lossState)
        {
            this.m_calcState.S = lossState;
            //this.m_lossState = lossState;
        }

        public override double GetLossState()
        {
            return this.m_calcState.S;
            //return this.m_lossState;
        }

        public double Exposure
        {
            get { return this.m_exposure; }
            set { this.m_exposure = value; }
        }

        public int PerilExtnId
        {
            get { return this.m_perilExtnId; }
            set { this.m_perilExtnId = value; }
        }
        public string ExposureType
        {
            get { return this.m_expType; }
            set { this.m_expType = value; }
        }
        public int LocationExtnId
        {
            get { return this.m_locExtnId; }
            set { this.m_locExtnId = value; }
        }
    }
}
