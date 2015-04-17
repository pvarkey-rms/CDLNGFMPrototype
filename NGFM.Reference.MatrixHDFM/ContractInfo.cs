using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class ContractInfo
    {
        private bool sublimitIsNetOfDeductible;
        private bool dedIsAbsorbable;

        public bool SublimitIsNetOfDeductible { get { return sublimitIsNetOfDeductible; } }
        public bool DedIsAbsorbable { get { return dedIsAbsorbable; } }

        #region Constructor
        public ContractInfo(bool _sublimitIsNetOfDeductible, bool _dedIsAbsorbable)
        {
            sublimitIsNetOfDeductible = _sublimitIsNetOfDeductible;
            dedIsAbsorbable = _dedIsAbsorbable;
        }
        #endregion
    }
}
