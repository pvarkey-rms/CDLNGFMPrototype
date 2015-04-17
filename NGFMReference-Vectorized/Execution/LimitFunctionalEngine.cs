using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    class LimitFunctionalEngine
    {
        /*
        private TermNode CurrNode;     //Only for TermNode 
        private Aggregation AggType;

        public LimitFunctionalEngine(TermNode _currNode, Aggregation _aggType)
        {
            CurrNode = _currNode;
            AggType = _aggType;
        }

        public void LimitFunction()
        {
            //if (CurrNode.TermIsPerRisk == true && CurrNode.CurrentLossStateCollection.NumBldgs > 1)
            if (AggType == Aggregation.PerBuilding)
            {
                for (int i = 0; i < CurrNode.CurrentLossStateCollection.NumBldgs; i++)               
                {
                    Double ded = CurrNode.Deductible * CurrNode.PrimarySubject.Schedule.MultiplierArr[i]; // / CurrNode.Subject.Schedule.NumOfBldgs * CurrNode.Subject.Schedule.MultiplierArr[i];
                    Double limit = CurrNode.Limit * CurrNode.PrimarySubject.Schedule.MultiplierArr[i]; // / CurrNode.Subject.Schedule.NumOfBldgs * CurrNode.Subject.Schedule.MultiplierArr[i];
                    if (limit == -1)  //no limit
                        //CurrNode.CurrentLossesArrPerBldg[i].X = 0;
                        CurrNode.CurrentLossStateCollection.collection[i].X = 0;
                    else
                    {
                        if (CurrNode.LimitIsNetOfDed)
                            CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(0.0, CurrNode.CurrentLossStateCollection.collection[i].S - ded - limit);
                        else
                            CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(0.0, CurrNode.CurrentLossStateCollection.collection[i].S - limit);
                    }                    
                }
            }
            else //do not do per-building
            {
                Double ded = CurrNode.Deductible;
                Double limit = CurrNode.Limit; 
      
                double loss = CurrNode.CurrentLossStateCollection.GetTotalSum.S;
                CurrNode.CurrentLossStateCollection.collection[0].S = loss;               

                if (limit == -1)  //no limit
                    CurrNode.CurrentLossStateCollection.collection[0].X = 0;
                else
                {
                    if (CurrNode.LimitIsNetOfDed)
                        CurrNode.CurrentLossStateCollection.collection[0].X = Math.Max(0.0, loss - ded - limit);
                    else
                        CurrNode.CurrentLossStateCollection.collection[0].X = Math.Max(0.0, loss - limit);
                }
            }            
        }
         * */
    }
}
