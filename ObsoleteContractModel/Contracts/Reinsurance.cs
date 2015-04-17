using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel.Contracts
{
    public class Reinsurance : _Contract
    {
        public override void Initialize()
        {
            if (this.m_subject == null)
            {
                throw new Exception("__Subject must be set before Contract.Initialize() is called");
            }

            this.m_trCoverNode = new Tree<CoverNode>();

            //this.m_trCoverNode.Initialize(string.Format("{0}-[CN-ROOT]", this.m_extnId));
            this.m_trCoverNode.Initialize(string.Format("{0}-{1}", this.m_extnId, this.m_subject.ToShortString()));

            base.m_bInit = true;
        }

        public override void Reset()
        {
            base.InitCheck();

            //reset cover-node calculation states
            foreach (CoverNode cn in this.m_trCoverNode.MapIntIdToNode.Values)
            {
                cn.CalcState.Reset();
                cn.Payout = 0.0;
            }
        }

        public override double CalculateLoss(RMS.Utilities.SimulationMsg cdlMsg, bool bDebug)
        {
            InitCheck();

            this.m_trCoverNode.RootNode.CalculateLoss(cdlMsg, bDebug);

            double payout = this.m_trCoverNode.RootNode.Payout;

            return payout;
        }

        public override CoverNode GetCoverNodeByName(string name)
        {
            InitCheck();

            string extnId = string.Format("{0}-{1}", this.m_extnId, name);
            int intId = this.m_trCoverNode.GetNodeIntIdByExtnId(extnId);

            CoverNode cn;
            if (intId == -1)
            {
                cn = new ReinCoverNode();
                cn.ExtnId = extnId;
            }
            else
            {
                cn = base.m_trCoverNode.GetNodeByIntId(intId);
            }

            return cn;
        }

        public override IdentityMap<RiskItem> GetAllRiskItems()
        {
            IdentityMap<RiskItem> idMapRiskItem = new IdentityMap<RiskItem>();

            foreach (CoverNode cn in this.m_trCoverNode.MapIntIdToNode.Values)
            {
                idMapRiskItem.AddObjectToMap(cn);

                if (cn.Subject != null)
                {
                    foreach (RiskItem riskItem in cn.Subject.Schedule.SetSchedule)
                    {
                        if (riskItem is _Contract)
                        {
                            List<RiskItem> vIndirectRiskItem = ((_Contract)riskItem).GetAllRiskItems().GetAllObjects();
                            foreach (RiskItem riskItemNested in vIndirectRiskItem)
                            {
                                idMapRiskItem.AddObjectToMap(riskItemNested);
                            }
                        }
                        else
                        {
                            idMapRiskItem.AddObjectToMap(riskItem);
                        }
                    }
                }
            }

            return idMapRiskItem;
        }

        public IdentityMap<T> GetAllRiskItemsOfType<T>() where T:Identity
        {
            IdentityMap<T> idMapT = new IdentityMap<T>();

            List<RiskItem> vRiskItem = new List<RiskItem>();

            foreach (CoverNode cn in this.m_trCoverNode.MapIntIdToNode.Values)
            {
                //add the covernode
                vRiskItem.Add(cn);

                if (cn.Subject != null)
                {
                    foreach (RiskItem riskItem in cn.Subject.Schedule.SetSchedule)
                    {
                        vRiskItem.Add(riskItem);

                        if (riskItem is _Contract)
                        {
                            //drill into the contract
                            List<RiskItem> vIndirectRiskItem = ((_Contract)riskItem).GetAllRiskItems().GetAllObjects();
                            foreach (RiskItem riskItemNested in vIndirectRiskItem)
                            {
                                vRiskItem.Add(riskItemNested);
                            }
                        }
                    }
                }
            }

            foreach (T riskItem in vRiskItem.OfType<T>())
            {
                idMapT.AddObjectToMap((T)riskItem);
            }

            return idMapT;
        }
    }
}
