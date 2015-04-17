using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public abstract class TermObject : IComparable
    {
        #region Fields
        //priority defines processing order
        protected uint m_priority;
        protected string label;
        #endregion

        #region Constructors
        public TermObject()
        {

        }
        #endregion

        #region Methods
        public abstract void ApplyTermObject(CalcState calcState);

        public int CompareTo(object obj)
        {
            int retVal = 0;

            if (obj is TermObject)
            {
                TermObject toCompare = (TermObject)obj;

                retVal = this.m_priority.CompareTo(toCompare.Priority);
            }

            return retVal;
        }
        #endregion

        #region Properties
        public uint Priority
        {
            get { return this.m_priority; }
            set { this.m_priority = value; }
        }
        #endregion
    }
}
