using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    public class ReinCoverNode : CoverNode
    {
        //determines how losses are handled (i.e. reference calcState or allocState)
        //public enum ENUM_LOSS_BASIS
        //{
        //    LOSS_BASIS_CALC,
        //    LOSS_BASIS_ALLOC
        //};

        //protected ENUM_LOSS_BASIS m_lossBasis;
        //protected ENUM_ALLOCATION_TYPE m_allocType;

        public ReinCoverNode()
        {
            //this.m_lossBasis = ENUM_LOSS_BASIS.LOSS_BASIS_CALC;
        }

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
                if (riskItem is ReinCoverNode)
                {
                    this.m_calcState.S += riskItem.GetLossState();
                }
                else
                {
                    if (this.m_resolution != ENUM_RESOLUTION_TYPE.NONE)
                    {
                        //each resolved risk item is effectively treated as a subcover to the current cover
                        List<RiskItem> vResolvedRiskItem = ApplyResolution(riskItem, this.m_resolution);

                        #region debug output
                        if (bDebug)
                        {
                            cdlMsg.SendMessage(string.Format("RiskItems resolved for ReinCoverNode: \r\n{0}", riskItem));
                            for (int rIdx = 0; rIdx < vResolvedRiskItem.Count; ++rIdx)
                            {
                                cdlMsg.SendMessage(string.Format("\t{0}", vResolvedRiskItem[rIdx]));
                            }
                        }
                        #endregion

                        //temporary calculation state for the resolved risk items.
                        //RMS.ContractObjectModel.CalcState resCalcState = new CalcState();

                        for (int rIdx = 0; rIdx < vResolvedRiskItem.Count; ++rIdx)
                        {
                            RiskItem resRiskItem = vResolvedRiskItem[rIdx];

                            RMS.ContractObjectModel.CalcState calcState = new CalcState();

                            //reinsurance cover operating on allocated loss
                            switch (base.m_contract.AllocType)
                            {
                                case ENUM_ALLOCATION_TYPE.ALLOC_TYPE_PROPORTIONAL:
                                    calcState.S = Math.Min(resRiskItem.GetLossState(), resRiskItem.GetAllocLossState());
                                    break;
                                case ENUM_ALLOCATION_TYPE.ALLOC_TYPE_PERRISK:
                                    calcState.S = resRiskItem.GetAllocLossState();
                                    break;
                                case ENUM_ALLOCATION_TYPE.ALLOC_TYPE_NONE:
                                    calcState.S = resRiskItem.GetLossState();
                                    break;
                            }

                            double prelimReSar = DetermineCoverPayout(calcState);
                            if (resRiskItem is TermNode)
                            {
                                TermNode resTermNode = (TermNode)resRiskItem;
                                resTermNode.PrelimReSar = prelimReSar;
                            }

                            //this.m_calcState.S += prelimReSar;

                            this.m_payout += prelimReSar;

                            //resCalcState.S += prelimReSar;

                            //resCalcState.S += calcState.S;
                            //resCalcState.D += calcState.D;
                            //resCalcState.X += calcState.X;
                        }

                        //this.m_calcState.S += resCalcState.S;
                        //this.m_calcState.D += resCalcState.D;
                        //this.m_calcState.X += resCalcState.X;
                    }
                    else
                    {
                        switch (base.m_contract.AllocType)
                        {
                            case ENUM_ALLOCATION_TYPE.ALLOC_TYPE_PROPORTIONAL:
                                this.m_calcState.S = Math.Min(riskItem.GetLossState(), riskItem.GetAllocLossState());
                                break;
                            case ENUM_ALLOCATION_TYPE.ALLOC_TYPE_PERRISK:
                                this.m_calcState.S = riskItem.GetAllocLossState();
                                break;
                            case ENUM_ALLOCATION_TYPE.ALLOC_TYPE_NONE:
                                this.m_calcState.S = riskItem.GetLossState();
                                break;
                        };
                    }
                }
            }

            if (this.m_resolution == ENUM_RESOLUTION_TYPE.NONE)
            {
                this.m_payout += DetermineCoverPayout(this.m_calcState);
            }
        }

        //public ENUM_LOSS_BASIS LossBasis
        //{
        //    get { return this.m_lossBasis; }
        //    set { this.m_lossBasis = value; }
        //}
    }
}
