using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFMReference.ContractModel;

namespace NGFMReference
{
    class CoverNodeFunctionalEngine
    {
        private CoverNode CurrNode;     //Attachment only for Cover Node

        public CoverNodeFunctionalEngine(CoverNode _currNode)
        {
            CurrNode = _currNode;
        }

        public void CoverNodeFunction()
        {
            double subLoss = CurrNode.Payout; //Payout aggregated from below, as Subject loss for this coverNode

            //exec AggDed Layer first, first fill AggDed
            double currPayout = 0.0;

            if (CurrNode.Cover.AttachmentTimeBasis == TimeBasis.Aggregate)
            {
                if (!CurrNode.CurrentAggState.IsAggDedFilled)
                {
                    currPayout = CurrNode.CurrentAggState.ApplyAggDed(subLoss);
                    CurrNode.Payout = currPayout;
                }
            }

            //exec Occurence layer
            if (CurrNode.Cover.AttachmentTimeBasis != TimeBasis.Aggregate)
            {
                if (CurrNode.Cover.AttIsFranchise)
                    CurrNode.Payout = (CurrNode.Payout < CurrNode.Cover.AttPoint.Amount) ? 0.0 : CurrNode.Payout;
                else
                    CurrNode.Payout = Math.Max(0.0, CurrNode.Payout - CurrNode.Cover.AttPoint.Amount);
            }

            if (CurrNode.Cover.LimitTimeBasis != TimeBasis.Aggregate)
            {
                if (!CurrNode.Cover.Unlimited)
                {
                    double limit = 0.0;

                    if (CurrNode.Cover.LimitValType == TermValueType.Numeric)
                        limit = CurrNode.Cover.Limit.Amount;
                    else if (CurrNode.Cover.LimitValType == TermValueType.PayFunction)
                    {
                        FunctionValue LimitFunc = CurrNode.Cover.Limit as FunctionValue;
                        foreach (Value arg in LimitFunc.Arguments)
                        {

                        }
                    }
                    CurrNode.Payout = Math.Min(CurrNode.Payout, CurrNode.Cover.Limit.Amount) * CurrNode.Cover.ProRata.Amount;
                }
                else
                {
                    CurrNode.Payout = CurrNode.Payout * CurrNode.Cover.ProRata.Amount;
                }
            }

            if (CurrNode.Cover.LimitTimeBasis == TimeBasis.Aggregate)
            {
                //apply Agg limit layer if needed
                currPayout = CurrNode.Payout;
                if (!CurrNode.CurrentAggState.IsAggLimitFilled)
                    currPayout = CurrNode.CurrentAggState.ApplyAggLimit(CurrNode.Payout);
                else
                    currPayout = 0;

                CurrNode.Payout = currPayout * CurrNode.Cover.ProRata.Amount;
            }            
        }
    }
}
