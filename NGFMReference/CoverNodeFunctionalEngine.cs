using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            double subLoss = CurrNode.Payout;

            //exec Occurence layer
            if (CurrNode.Cover.AttIsFranchise)
                CurrNode.Payout = (CurrNode.Payout < CurrNode.Cover.AttPoint.Amount) ? 0.0 : CurrNode.Payout;
            else
                CurrNode.Payout = Math.Max(0.0, CurrNode.Payout - CurrNode.Cover.AttPoint.Amount);

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

            //aggregate ded layer
            double currPayout = 0.0;

            if (!CurrNode.CurrentAggState.IsAggDedFilled)
            {
                currPayout = CurrNode.CurrentAggState.ApplyAggDed(subLoss);
                currPayout = CurrNode.CurrentAggState.ApplyAggLimit(currPayout);

                CurrNode.Payout = currPayout;
            }

        }
    }
}
