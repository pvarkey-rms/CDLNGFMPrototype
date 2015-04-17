using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    public class _Schedule
    {
        #region Fields
        protected int m_intId;
        protected string m_extnId;
        protected string m_description;
        protected static IdGenerator s_idGen = new IdGenerator();
        protected HashSet<RiskItem> m_setSchedule;
        #endregion

        #region Constructors
        public _Schedule()
        {
            this.m_setSchedule = new HashSet<RiskItem>();
        }
        public _Schedule(string name)
        {
            this.m_setSchedule = new HashSet<RiskItem>();
            ExtnId = name;
        }
        #endregion

        #region Methods


        public void AddRiskItemToSchedule(RiskItem riskItem)
        {
            this.m_setSchedule.Add(riskItem);
        }

        public void RemoveRiskItemFromSchedule(RiskItem riskItem)
        {
            if (this.m_setSchedule.Contains(riskItem))
            {
                this.m_setSchedule.Remove(riskItem);
            }
        }

        public string ToSarString()
        {
            return StringUtil.BuildStringFromList(this.m_setSchedule.Select(riskItem => riskItem.IntId).ToList());
        }
        #endregion

        #region Properties

        public HashSet<RiskItem> SetSchedule
        {
            get { return this.m_setSchedule; }
            set { this.m_setSchedule = value; }
        }

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
        #endregion

        public bool IsSubsetTo(_Schedule sar)
        {
            bool bIsSubset = true;

            string[] vLocCompare = sar.ExtnId.Split('.');
            string[] vLocThis = this.ExtnId.Split('.');

            //assumed that structure is A.B.C
            if (vLocCompare.Length > vLocThis.Length)
            {
                bIsSubset = false;
            }
            else
            {
                for (int vIdx = 0; vIdx < vLocCompare.Length && bIsSubset; ++vIdx)
                {
                    if (vLocThis[vIdx] != vLocCompare[vIdx])
                    {
                        bIsSubset = false;
                    }
                }
            }

            return bIsSubset;
        }

        public bool IsProperSubsetTo(_Schedule sar)
        {
            bool bIsProperSubset = true;

            string[] vLocCompare = sar.ExtnId.Split('.');
            string[] vLocThis = this.ExtnId.Split('.');

            //assumed that structure is A.B.C
            if (vLocCompare.Length >= vLocThis.Length)
            {
                bIsProperSubset = false;
            }
            else
            {
                for (int vIdx = 0; vIdx < vLocCompare.Length && bIsProperSubset; ++vIdx)
                {
                    if (vLocThis[vIdx] != vLocCompare[vIdx])
                    {
                        bIsProperSubset = false;
                    }
                }
            }

            return bIsProperSubset;

        }
    }
}
