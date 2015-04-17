using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    public class Peril : ICauseOfLoss
    {
        #region Fields
        private string m_perilName;

        private int m_intId;
        private string m_extnId;
        private static IdGenerator s_idGen = new IdGenerator();
        #endregion

        #region Constructor
        public Peril()
        {

        }
        public Peril(string name)
        {
            m_perilName = name;
            ExtnId = name;
        }
        #endregion

        #region Methods
        public static int GetIntIdFromExtnId(string extnId)
        {
            return s_idGen.GetIntIdFromExtnId(extnId);
        }

        public override string ToString()
        {
            return string.Format("Peril: intId={0}, extnId={1}, perilName={2}", this.m_intId, this.m_extnId, this.m_perilName);
        }
        #endregion

        public string Name
        {
            get { return this.m_perilName; }
            set { this.m_perilName = value; }
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
        public static void ResetIdentity()
        {
            s_idGen.Reset();
        }


        public bool IsSubsetTo(ICauseOfLoss col)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSubsetTo(ICauseOfLoss col)
        {
            throw new NotImplementedException();
        }
    }
}
