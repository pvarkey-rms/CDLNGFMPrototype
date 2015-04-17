using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    public abstract class RiskItem : Identity
    {
        //this indicates whether to operate on GetLossState() or GetAllocLossState()
        //may be cleaner to make this a delegate..
        public enum ENUM_CALC_LOSS_BASIS
        {
            LOSS_BASIS_CALC,
            LOSS_BASIS_ALLOC,
            //take the minimum of allocated loss and calculated loss
            LOSS_BASIS_MIN_CALC_ALLOC
        };

        #region Fields
        protected int m_intId;
        protected string m_extnId;
        protected string m_description;
        protected static IdGenerator s_idGen = new IdGenerator();

        protected AllocState m_allocState;
        protected CalcState m_calcState;
        #endregion

        public RiskItem()
        {
            this.m_calcState = new CalcState();
            this.m_allocState = new AllocState();
        }

        public abstract double GetLossState();

        public virtual double GetAllocLossState()
        {
            return this.m_allocState.AllocPayout;
        }

        #region Properties
        public string Description
        {
            get { return this.m_description; }
            set { this.m_description = value; }
        }
        public int IntId
        {
            get { return this.m_intId; }
        }
        public string ExtnId
        {
            get { return this.m_extnId; }
            set
            {
                this.m_extnId = value;
                this.m_intId = s_idGen.RegisterExtnId(value);
            }
        }
        public AllocState AllocState
        {
            get { return this.m_allocState; }
            set { this.m_allocState = value; }
        }
        public CalcState CalcState
        {
            get { return this.m_calcState; }
            set { this.m_calcState = value; }
        }
        #endregion
    }
}
