using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    public class Portfolio<T> where T : _Contract
    {
        #region Fields
        IdentityMap<T> m_idMapContract;
        #endregion

        #region Constructors
        public Portfolio()
        {
            this.m_idMapContract = new IdentityMap<T>();
        }
        #endregion

        #region Methods
        public void AddContract(T con)
        {
            this.m_idMapContract.AddObjectToMap(con);
        }

        public _Contract GetContractByExtnId(string extnId)
        {
            return this.m_idMapContract.GetObjectByExtnId(extnId);
        }

        public List<T> GetAllContracts()
        {
            return this.m_idMapContract.GetAllObjects();
        }

        #region ToString()
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            str.Append("Portfolio:\r\n");
            foreach (_Contract con in this.GetAllContracts())
            {
                str.Append(string.Format("{0}\r\n", con.ToString()));
            }

            return str.ToString();
        }
        #endregion

        #endregion

        #region Properties
        #endregion
    }
}
