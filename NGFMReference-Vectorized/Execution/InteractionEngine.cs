using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    class InteractionEngine
    {
        /*
        private TermNode CurrNode;  //assume interaction only happens to TermNode
        private InteractionObject[] IntObject;
        private InteractionObject IntObjectUnited;
        private Aggregation AggType;

        public InteractionEngine(TermNode _currNode, Aggregation _aggType, InteractionObject[] _intObject)
        {
            CurrNode = _currNode;
            IntObject = _intObject;
            AggType = _aggType;
        }

        public void Interaction()
        {
            //if (CurrNode.TermIsPerRisk && CurrNode.CurrentLossStateCollection.NumBldgs > 1)
            if (AggType == Aggregation.PerBuilding)
            {
                for (int i = 0; i < CurrNode.CurrentLossStateCollection.NumBldgs; i++)
                {
                    foreach (Limit limObj in CurrNode.Limits)
                    {
                        foreach (Deductible dedObj in CurrNode.Deductibles)                        
                        {                            
                            double ded = dedObj.Amount * CurrNode.PrimarySubject.Schedule.MultiplierArr[i];
                            double limit = limObj.Amount * CurrNode.PrimarySubject.Schedule.MultiplierArr[i];

                            //Limit
                            CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X, IntObject[i].ExcessFromChildren);

                            //Deductible
                            if (dedObj.DedInterType == DedInteractionType.SingleLargest)
                            {
                                CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].LargestDedFromChildren;
                            }
                            else if (dedObj.DedInterType == DedInteractionType.MIN)
                            {
                                CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D);
                            }
                            else if (dedObj.DedInterType == DedInteractionType.Absorbing)
                            {
                                CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D - CurrNode.CurrentLossStateCollection.collection[i].X);
                            }
                            else if (dedObj.DedInterType == DedInteractionType.MAX)
                            {
                                CurrNode.CurrentLossStateCollection.collection[i].D = Math.Min(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D);
                            }
                            else
                                throw new InvalidOperationException("Interaction Type is not Known");
                        }
                    }
                } //for loop
            }
            else 
            {
                foreach (Limit limObj in CurrNode.Limits)
                {
                    foreach (Deductible dedObj in CurrNode.Deductibles)
                    {
                        double ded = dedObj.Amount;
                        double limit = limObj.Amount;
                        //Limit
                        CurrNode.CurrentLossStateCollection.collection[0].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[0].X, IntObject[0].ExcessFromChildren);

                        //Deductible
                        if (dedObj.DedInterType == DedInteractionType.SingleLargest)
                        {
                            CurrNode.CurrentLossStateCollection.collection[0].D = IntObject[0].LargestDedFromChildren;
                        }
                        else if (dedObj.DedInterType == DedInteractionType.MIN)
                        {
                            CurrNode.CurrentLossStateCollection.collection[0].D = Math.Max(IntObject[0].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[0].D);
                        }
                        else if (dedObj.DedInterType == DedInteractionType.Absorbing)
                        {
                            CurrNode.CurrentLossStateCollection.collection[0].D = Math.Max(IntObject[0].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[0].D - CurrNode.CurrentLossStateCollection.collection[0].X);
                        }
                        else if (dedObj.DedInterType == DedInteractionType.MAX)
                        {
                            CurrNode.CurrentLossStateCollection.collection[0].D = Math.Min(IntObject[0].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[0].D);
                        }
                        else
                            throw new InvalidOperationException("Interaction Type is not Known");
                    }
                }
            }
        }
        */
    }
}
