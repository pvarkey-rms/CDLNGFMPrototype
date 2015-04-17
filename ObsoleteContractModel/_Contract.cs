using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    //TODO: option model needs to be addressed
    public enum ENUM_DEDUCTIBLE_OPTION
    {
        OPTION_DED_ABSORBABLE,
        OPTION_NONE
    };
    public enum ENUM_ALLOCATION_TYPE
    {
        ALLOC_TYPE_NONE,
        ALLOC_TYPE_PROPORTIONAL,
        ALLOC_TYPE_PERRISK,
    };

    public enum ContractEntry
    {
        CONTRACT_NAME_LENGTH,
        PARENT_INDEX,
        NODE_NUMBER,
        CONTRACT_OVERLAP = 9,
        COVER_SHARE = 11,
        COVER_OCCATT = 12,
        COVER_OCCATTPC = 112,
        COVER_OCCATTPA = 212,
        COVER_OCCLIM = 13,
        COVER_OCCLIMPC = 113,
        COVER_OCCLIMPA = 213,
        COVER_AGGATT = 32,
        COVER_AGGATTPC = 142,
        COVER_AGGATTPA = 242,
        COVER_AGGLIM = 33,
        COVER_AGGLIMPC = 143,
        COVER_AGGLIMPA = 243,
        COVER_SETS_MAX = 14,
        COVER_SETS_MIN = 114,
        COVER_SETS_SUM = 214,
        COVER_PAY = 15,
        COVER_PAYP = 115,
        COVER_PAYF_MAX = 16,
        COVER_PAYFP_MAX = 116,
        COVER_PAYF_MIN = 17,
        COVER_PAYFP_MIN = 117,
        COVER_PER_CONTRACT = 18,
        COVER_NON_PER_CONTRACT = 118,
        NODE_ISPERRISK = 20,
        NODE_GSL = 21,
        NODE_GSLPC = 121,
        NODE_GSLPA = 221,
        NODE_MIND = 22,
        NODE_MINDPC = 122,
        NODE_MINDPA = 222,
        NODE_MINDA = 23,
        NODE_MINDAPC = 123,
        NODE_MINDAPA = 223,
        NODE_MAXD = 24,
        NODE_MAXDPC = 124,
        NODE_MAXDPA = 224,
        NODE_NSL = 25,
        NODE_NSLPC = 125,
        NODE_NSLPA = 225,
        NODE_BUCKET = 301,
        NODE_CHILDNUM = 302,
        NODE_ROOTNUM = 303
    };

    public abstract class _Contract : RiskItem
    {
        #region Fields
        //header information
        protected string m_version;
        protected string m_currency;
        protected ENUM_DEDUCTIBLE_OPTION m_deductOpt;
        protected ENUM_ALLOCATION_TYPE m_allocType;
        protected DateTime m_incepDt;
        protected DateTime m_expiryDt;
        protected bool m_bIsActive;
        protected double m_subjPremium;
        protected string m_insuredStr;
        protected string m_insurerStr;

        //initialization flag
        protected bool m_bInit;
        //contract's subject
        protected __Subject m_subject;
        //cover node tree
        protected Tree<CoverNode> m_trCoverNode;

        public int nNodes;

        #endregion

        #region Constructors
        public _Contract()
        {
            this.m_bInit = false;
            this.m_allocType = ENUM_ALLOCATION_TYPE.ALLOC_TYPE_NONE;
        }
        public _Contract(string name) : this()
        {
            ExtnId = name;
            this.m_bInit = false;
            this.m_allocType = ENUM_ALLOCATION_TYPE.ALLOC_TYPE_NONE;
        }
        #endregion

        #region Abstract Methods
        public abstract void Reset();
        public abstract void Initialize();
        public abstract double CalculateLoss(SimulationMsg cdlMsg, bool bDebug);
        #endregion

        #region Methods
        protected virtual void InitCheck()
        {
            if (!this.m_bInit)
            {
                throw new Exception("Call to Contract.Initialize() must be made.");
            }
        }

        public virtual void AddCoverNode(CoverNode coverNode)
        {
            this.InitCheck();
            this.m_trCoverNode.AddNodeToTree(coverNode, false);
        }

        //TODO
        public virtual CoverNode GetCoverNodeBySubject(__Subject subject)
        {
            InitCheck();

            string extnId = string.Format("{0}-{1}", this.m_extnId, subject.ToShortString());

            int intId = this.m_trCoverNode.GetNodeIntIdByExtnId(extnId);
            CoverNode cn;
            if (intId == -1)
            {
                cn = new CoverNode();
                cn.Subject = subject;
                cn.ExtnId = extnId;
            }
            else
            {
                cn = this.m_trCoverNode.GetNodeByIntId(intId);
            }

            return cn;

        }

        //TODO
        public virtual CoverNode GetCoverNodeByName(string name)
        {
            InitCheck();

            string extnId = string.Format("{0}-{1}",this.m_extnId,name);
            int intId = this.m_trCoverNode.GetNodeIntIdByExtnId(extnId);

            CoverNode cn;
            if (intId == -1)
            {
                cn = new CoverNode();
                cn.ExtnId = extnId;
            }
            else
            {
                cn = this.m_trCoverNode.GetNodeByIntId(intId);
            }

            return cn;
        }

        public virtual IdentityMap<RiskItem> GetAllRiskItems()
        {
            IdentityMap<RiskItem> idMapRiskItem = new IdentityMap<RiskItem>();

            foreach (CoverNode cn in this.m_trCoverNode.MapIntIdToNode.Values)
            {
                idMapRiskItem.AddObjectToMap(cn);

                if (cn.Subject != null)
                {
                    foreach (RiskItem riskItem in cn.Subject.Schedule.SetSchedule)
                    {
                        idMapRiskItem.AddObjectToMap(riskItem);
                    }
                }
            }

            return idMapRiskItem;
        }

        public override string ToString()
        {
            return String.Format("Contract: intId={0}, extnId={1}, subject={2}", this.m_intId, this.m_extnId, this.m_subject);
        }

        public static void ResetIdentity()
        {
            s_idGen.Reset();
        }

        public override double GetLossState()
        {
            return this.m_trCoverNode.RootNode.Payout;
        }
        #endregion

        #region Properties
        public __Subject Subject
        {
            get { return this.m_subject; }
            set { this.m_subject = value; }
        }
        public Tree<CoverNode> TrCoverNode
        {
            get { return this.m_trCoverNode; }
        }
        public ENUM_DEDUCTIBLE_OPTION DeductOption
        {
            get { return this.m_deductOpt; }
            set { this.m_deductOpt = value; }
        }
        public ENUM_ALLOCATION_TYPE AllocType
        {
            get { return this.m_allocType; }
            set { this.m_allocType = value; }
        }
        public string Version
        {
            get { return this.m_version; }
            set { this.m_version = value; }
        }
        public string Currency
        {
            get { return this.m_currency; }
            set { this.m_currency = value; }
        }
        #endregion
    }
}
