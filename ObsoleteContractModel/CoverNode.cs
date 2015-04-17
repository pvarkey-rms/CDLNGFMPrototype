using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    public class CoverNode : Node
    {
        #region Enums
        public enum ENUM_RESOLUTION_TYPE
        {
            LOCATION,
            POLICY,
            NONE
        }

        #endregion

        #region Fields
        protected double m_share;
        protected double m_limit;
        protected double m_agglimit;
        public double m_limitPC;
        public double m_agglimitPC;
        public double m_limitPA;
        public double m_agglimitPA;

        protected double m_attach;
        public double m_aggach;
        public double m_attachPC;
        public double m_aggachPC;
        public double m_attachPA;
        public double m_aggachPA;

        public int _setsType;
        public int _payfType;
        public double _pay;

        //cover recovery
        protected double m_payout;
        protected ENUM_RESOLUTION_TYPE m_resolution;
        
        //this is a temporary list used to identify the cover's children (temp hack)
        protected List<string> m_vSubCover;
        public string parentId;
        public int nChild; // number of node trees

        protected _Contract m_contract;
        #endregion

        #region Constructors
        public CoverNode()
        {
            this.m_share = -1;
            this.m_limit = -1;
            this.m_agglimit = -1;
            this.m_attach = -1;

            this.m_resolution = ENUM_RESOLUTION_TYPE.NONE;

            this.m_vSubCover = new List<string>();
        }
        #endregion

        #region Methods
        public override void CalculateLoss(SimulationMsg cdlMsg, bool bDebug)
        {
            //process children node payouts.
            for (int cIdx = 0; cIdx < base.m_vChildNode.Count; ++cIdx)
            {
                CoverNode cn = (CoverNode)base.m_vChildNode[cIdx];
                cn.CalculateLoss(cdlMsg, bDebug);
            }

            List<CalcState> vResolvedCalcState = new List<CalcState>();
            _Schedule sar = this.m_subject.Schedule;

            foreach (RiskItem riskItem in sar.SetSchedule)
            {
                if (this.m_resolution != ENUM_RESOLUTION_TYPE.NONE)
                {
                    List<RiskItem> vResolvedRiskItem = ApplyResolution(riskItem, this.m_resolution);

                    #region debug output
                    if (bDebug)
                    {
                        cdlMsg.SendMessage(string.Format("RiskItems resolved for RiskItem: \r\n{0}", riskItem));
                        for (int rIdx = 0; rIdx < vResolvedRiskItem.Count; ++rIdx)
                        {
                            cdlMsg.SendMessage(string.Format("\t{0}", vResolvedRiskItem[rIdx]));
                        }
                    }
                    #endregion

                    //each resolved risk item is effectively treated as a subcover
                    for (int rIdx = 0; rIdx < vResolvedRiskItem.Count; ++rIdx)
                    {
                        RMS.ContractObjectModel.CalcState calcState = new CalcState();

                        calcState.S = vResolvedRiskItem[rIdx].GetLossState();

                        this.m_calcState.S += DetermineCoverPayout(calcState);
           
                    }

                    this.m_payout += this.m_calcState.S;
                }
                else
                {
                    this.m_calcState.S += riskItem.GetLossState();
                }
            }

            if (this.m_resolution == ENUM_RESOLUTION_TYPE.NONE)
            {
                this.m_payout += DetermineCoverPayout(this.m_calcState);
            }
        }

        protected double DetermineCoverPayout(CalcState calcState)
        {
            if (this.m_attach != -1)
            {
                calcState.D = Math.Min(calcState.S, this.m_attach);
            }
            if (this.m_limit != -1)
            {
                double n = calcState.S - calcState.D;
                double a = Math.Min(this.m_limit, n);
                calcState.X = n - a;
            }

            double payout = (calcState.S - calcState.X - calcState.D);

            if (this.m_share != -1)
            {
                payout = this.m_share * payout;
            }

            return payout;
        }

        public static List<RiskItem> ApplyResolution(RiskItem riskItem, ENUM_RESOLUTION_TYPE resType)
        {
            List<RiskItem> vResolvedRiskItems = new List<RiskItem>();
            if (riskItem is _Contract)
            {
                _Contract con = (_Contract)riskItem;

                switch (resType)
                {
                    case ENUM_RESOLUTION_TYPE.LOCATION:
                        vResolvedRiskItems = con.GetAllRiskItems().GetAllObjects().OfType<TermNode>().Where(termNode => termNode.TermNodeType == TermNode.ENUM_TERM_NODE_TYPE.LOCATION).ToList<RiskItem>();
                        break;
                }
            }
            else if (riskItem is Node)
            {
                throw new NotImplementedException();
            }
            return vResolvedRiskItems;
        }

        public override double GetLossState()
        {
            return this.m_payout;
        }

        public override string ToString()
        {
            return string.Format("CoverNode: intId={0}, extnId={1}, _Participation={2:f2}, occlimit={3:f2}, attach={4:f2}",
                base.m_intId, base.m_extnId, this.m_share, this.m_limit, this.m_attach);
        }
        public override bool IsSubsetTo(Node n)
        {
            throw new NotImplementedException();
        }
        public override bool IsProperSubsetTo(Node n)
        {
            throw new NotImplementedException();
        }
        public override bool Overlaps(Node n)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Properties
        public ENUM_RESOLUTION_TYPE Resolution
        {
            get { return this.m_resolution; }
            set { this.m_resolution = value; }
        }
        public List<string> vSubCover
        {
            get { return this.m_vSubCover; }
            set { this.m_vSubCover = value; }
        }
        public double Payout
        {
            get { return this.m_payout; }
            set { this.m_payout = value; }
        }
        public double Share
        {
            get { return this.m_share; }
            set { this.m_share = value; }
        }
        public double Limit
        {
            get { return this.m_limit; }
            set { this.m_limit = value; }
        }
        public double AggLimit
        {
            get { return this.m_agglimit; }
            set { this.m_agglimit = value; }
        }
        public double Attach
        {
            get { return this.m_attach; }
            set { this.m_attach = value; }
        }
        public _Contract Contract
        {
            get { return this.m_contract; }
            set { this.m_contract = value; }
        }
        #endregion
    }
}
