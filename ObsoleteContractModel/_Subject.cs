using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    public class __Subject : Identity
    {
        #region Fields
        //Exposure Type
        private HashSet<string> m_setExpType;
        //Cause of Loss
        private HashSet<ICauseOfLoss> m_setCol;
        //__Subject at Risk _Schedule
        private _Schedule m_sched;
        //private string m_subjectStr;
        //identity interface implementation
        private string m_extnId;
        private int m_intId;
        private static IdGenerator s_idGen = new IdGenerator();
        private bool m_isPerRisk;
        #endregion

        #region Constructors

        public __Subject(_Schedule sched, HashSet<ICauseOfLoss> setCol, HashSet<string> setExpType, bool isPerRisk)
        {
            this.m_sched = sched;
            this.m_setCol = setCol;
            this.m_setExpType = setExpType;
            this.m_isPerRisk = isPerRisk;
        }
        #endregion

        #region ToString()
        public override string ToString()
        {
            StringBuilder subStr = new StringBuilder();
            subStr.Append(string.Format("__Subject: intId={0}, extnId={1}, cdlStr='{2}'", this.m_intId, this.m_extnId,this.ToCDLString()));
            return subStr.ToString();
        }

        public static string ToShortSubjectString(_Schedule sar, HashSet<ICauseOfLoss> setCol, HashSet<string> setExpType)
        {
            StringBuilder subStr = new StringBuilder();

            subStr.Append("{").Append(string.Format("{0}", sar.ExtnId))
                  .Append("}-{")
                  .Append(GetCauseOfLossString(setCol))
                  .Append("}-{")
                  .Append(GetExpTypeString(setExpType))
                  .Append("}");

            return subStr.ToString();
        }

        public string ToShortString()
        {
            StringBuilder subStr = new StringBuilder();

            subStr.Append("{").Append(string.Format("{0}", this.m_sched.ExtnId))
                  .Append("}-{")
                  .Append(GetCauseOfLossString(this.m_setCol))
                  .Append("}-{")
                  .Append(GetExpTypeString(this.m_setExpType))
                  .Append("}");

            return subStr.ToString();
        }

        public static string GetCauseOfLossString(HashSet<ICauseOfLoss> setCol)
        {
            StringBuilder str = new StringBuilder();
            foreach (Peril peril in setCol)
            {
                str.Append(string.Format("{0}, ", peril.Name));
            }
            if (str.Length > 0)
                str.Remove(str.Length - 2, 2);

            return str.ToString();
        }
        
        public static string GetExpTypeString(HashSet<string> setExpType)
        {
            StringBuilder subStr = new StringBuilder();

            HashSet<string> losses = new HashSet<string>();
            losses.Add("Building");
            losses.Add("Contents");
            losses.Add("BI");

            if (losses.IsSubsetOf(setExpType))
            {
                return "Losses";
            }
            else
            {
                //need to output in specific order:
                //Building
                //Contents
                //BI
                if (setExpType.Contains("Building"))
                {
                    subStr.Append("Building, ");
                }
                if (setExpType.Contains("Contents"))
                {
                    subStr.Append("Contents, ");
                }
                if (setExpType.Contains("BI") || setExpType.Contains("Business Interruption"))
                {
                    subStr.Append("BI, ");
                }

                if (subStr.Length > 0)
                    subStr.Remove(subStr.Length - 2, 2);
                return subStr.ToString();
            }
        }

        public string ToCDLString()
        {
            return string.Format("{0} to {1} by {2}", GetExpTypeString(this.m_setExpType), this.m_sched.ExtnId, GetCauseOfLossString(this.m_setCol));
        }
        #endregion

        #region Methods
        public bool IsSubsetTo(__Subject subject)
        {
            return (this.m_sched.IsSubsetTo(subject.Schedule)
                && this.SetExpType.IsSubsetOf(subject.SetExpType)
                && this.SetCauseOfLoss.IsSubsetOf(subject.SetCauseOfLoss));
        }
        public bool Overlaps(__Subject subject)
        {
            return (this.m_sched.IsSubsetTo(subject.Schedule)
                && this.SetExpType.Overlaps(subject.SetExpType)
                && this.SetCauseOfLoss.Overlaps(subject.SetCauseOfLoss));
        }

        public bool IsProperSubsetTo(__Subject subject)
        {
            bool bIsProperSubset = false;
            if (this.IsSubsetTo(subject))
            {
                if (subject.IsSubsetTo(this))
                {
                    bIsProperSubset = false;
                }
                else
                {
                    bIsProperSubset = true;
                }
            }
            else
            {
                bIsProperSubset = false;
            }

            return bIsProperSubset;
        }

        public List<int> GetUniqueLocationExtnId()
        {
            List<int> vLocExtnId = new List<int>();

            if (this.m_sched != null)
            {
                foreach (RiskItem riskItem in this.Schedule.SetSchedule)
                {
                    if (riskItem is LocationCvg)
                    {
                        //direct location coverages
                        LocationCvg locCvg = (LocationCvg)riskItem;
                        if (!vLocExtnId.Contains(locCvg.LocationExtnId))
                        {
                            //add the coverage's location external id
                            vLocExtnId.Add(locCvg.LocationExtnId);
                        }
                    }
                }
            }

            return vLocExtnId;
        }

        public static void ResetIdentity()
        {
            s_idGen.Reset();
        }

        public static int GetIntIdFromExtnId(string extnId)
        {
            return s_idGen.GetIntIdFromExtnId(extnId);
        }
        #endregion

        #region Properties
        public string SubjectStr
        {
            get { return this.ToCDLString(); }
        }
        public HashSet<string> SetExpType
        {
            get { return this.m_setExpType; }
            set { this.m_setExpType = value; }
        }
        public _Schedule Schedule
        {
            get { return this.m_sched; }
            set { this.m_sched = value; }
        }
        public HashSet<ICauseOfLoss> SetCauseOfLoss
        {
            get { return this.m_setCol; }
            set { this.m_setCol = value; }
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

        public bool IsPerRisk
        {
            get { return this.m_isPerRisk; }
            set { this.m_isPerRisk = value; }
        }

        public int IntId
        {
            get { return this.m_intId; }
        }
        #endregion
    }
}
