using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    class DedFunctionalEngine
    {
        /*
        private TermNode CurrNode;     //TODO: only for TermNode 
        private Aggregation AggType;

        public DedFunctionalEngine(TermNode _currNode, Aggregation _aggType)
        {
            CurrNode = _currNode;
            AggType = _aggType;
        }

        public void DedFunction()
        {
            //if (CurrNode.TermIsPerRisk == true && CurrNode.CurrentLossStateCollection.NumBldgs > 1)
            if (AggType == Aggregation.PerBuilding)
            {
                for (int i = 0; i < CurrNode.CurrentLossStateCollection.NumBldgs; i++)
                {
                    Double ded = CurrNode.Deductible * CurrNode.PrimarySubject.Schedule.MultiplierArr[i]; // / CurrNode.Subject.Schedule.NumOfBldgs * CurrNode.Subject.Schedule.MultiplierArr[i];
                    Double limit = CurrNode.Limit * CurrNode.PrimarySubject.Schedule.MultiplierArr[i]; // / CurrNode.Subject.Schedule.NumOfBldgs * CurrNode.Subject.Schedule.MultiplierArr[i];
                    if (CurrNode.DedIsFranchise)
                        CurrNode.CurrentLossStateCollection.collection[i].D = (CurrNode.CurrentLossStateCollection.collection[i].S > ded) ? 0.0 : CurrNode.CurrentLossStateCollection.collection[i].S;
                    else
                        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Min(CurrNode.CurrentLossStateCollection.collection[i].S, ded);
                }
            }
            else
            {
                Double ded = CurrNode.Deductible;
                Double limit = CurrNode.Limit; 
                Double loss = CurrNode.CurrentLossStateCollection.GetTotalSum.S;
                CurrNode.CurrentLossStateCollection.collection[0].S = loss;
                //for (int i = 0; i < CurrNode.Subject.Schedule.ActNumOfBldgs; i++)
                //{
                 //   loss += CurrNode.CurrentLossesArrPerBldg[i].S; // *CurrNode.Subject.Schedule.MultiplierArr[i];
                //}               

                if (CurrNode.DedIsFranchise)
                    CurrNode.CurrentLossStateCollection.collection[0].D = (loss > ded) ? 0.0 : loss;
                else
                    CurrNode.CurrentLossStateCollection.collection[0].D = Math.Min(loss, ded);
            }
        }
         * */
    }
}
