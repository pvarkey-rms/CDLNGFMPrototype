﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{

    //1--------------------------------------------
//    class TermFunctionalEngine
//    {
//        private TermNode CurrNode;     //Only for TermNode 
//        private Aggregation AggType;
//        private int[] multiArr;
        

//        public TermFunctionalEngine(TermNode _currNode, Aggregation _aggType)
//        {
//            CurrNode = _currNode;
//            AggType = _aggType;
//            multiArr = CurrNode.PrimarySubject.Schedule.MultiplierArr;
//        }

//        public void TermFunction(InteractionObject[] IntObject)
//        {
//            int NumBldgs = CurrNode.CurrentLossStateCollection.NumBldgs;

//            int intObjCount = IntObject.Count();
//            DeductibleCollection sortedDeductibles = CurrNode.Deductibles.SortByExecOrder(CurrNode.Deductibles);

//            //initialize
//            for (int i = 0; i < NumBldgs; i++)
//            {

//                if (intObjCount > 0)
//                {
//                    CurrNode.CurrentLossStateCollection.collection[i].X = IntObject[i].ExcessFromChildren;
//                    CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].DedFromChildren;
//                }
//                else
//                {
//                    CurrNode.CurrentLossStateCollection.collection[i].X = 0;
//                    CurrNode.CurrentLossStateCollection.collection[i].D = 0;
//                }
//            }

//            //Do Ground-up sub-Limit first
//            foreach (Limit limObj in CurrNode.Limits)
//            {
//                if (limObj.LimitIsNetOfDed == false)
//                {
//                    double limitTot = 0;
//                    if (limObj.LimType == TermValueType.Numeric)
//                        limitTot = limObj.Amount;
//                    else if (limObj.LimType == TermValueType.PercentCovered)
//                        limitTot = limObj.Amount / 100 * CurrNode.GetTIV();
//                    else if (limObj.LimType == TermValueType.PercentAffected)
//                        limitTot = limObj.Amount / 100 * CurrNode.GetAffectedTIV();
//                    else if (limObj.LimType == TermValueType.PercentLoss)
//                        limitTot = limObj.Amount / 100 * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                    else
//                        throw new InvalidOperationException("Lim TermValueType is not Known");

//                    double limit = limitTot;
//                    for (int i = 0; i < NumBldgs; i++)
//                    {
//                        if (AggType == Aggregation.PerBuilding)
//                            limit = limitTot * multiArr[i];

//                        CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X, Math.Max(0.0, CurrNode.CurrentLossStateCollection.collection[i].S - limit));
//                        CurrNode.CurrentLossStateCollection.collection[i].AdjustX();
//                    }
//                }  //end: each Group-up Sub-Limits
//            }

//            //then do all Deductibles one by one, all deductibles are already in the right exec order       
//            foreach (Deductible dedObj in sortedDeductibles)
//            {
//                double dedTot = 0;
//                if (dedObj.DedType == TermValueType.Numeric)
//                    dedTot = dedObj.Amount;
//                else if (dedObj.DedType == TermValueType.PercentCovered)
//                    dedTot = dedObj.Amount * CurrNode.GetTIV();
//                else if (dedObj.DedType == TermValueType.PercentAffected)
//                    dedTot = dedObj.Amount * CurrNode.GetAffectedTIV();
//                else if (dedObj.DedType == TermValueType.PercentLoss)
//                    dedTot = dedObj.Amount * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                else
//                    throw new InvalidOperationException("Ded TermValueType is not Known");

//                double ded = dedTot;
//                for (int i = 0; i < NumBldgs; i++)
//                {
//                    if (AggType == Aggregation.PerBuilding)
//                        ded = dedTot * multiArr[i];

//                    if (dedObj.DedIsFranchise)
//                        ded = (CurrNode.CurrentLossStateCollection.collection[i].S > ded) ? 0.0 : CurrNode.CurrentLossStateCollection.collection[i].S;
//                    else
//                        ded = Math.Min(CurrNode.CurrentLossStateCollection.collection[i].S, ded);

//                    if (dedObj.DedInterType == DedInteractionType.SingleLargest)
//                        CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].LargestDedFromChildren;
//                    else if (dedObj.DedInterType == DedInteractionType.MAX)
//                        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Min(CurrNode.CurrentLossStateCollection.collection[i].D, ded);
//                    else if (dedObj.DedInterType == DedInteractionType.MIN)
//                        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].D, ded);
//                    else if (dedObj.DedInterType == DedInteractionType.Absorbing)
//                        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].D, ded - CurrNode.CurrentLossStateCollection.collection[i].X);
//                    else
//                        throw new InvalidOperationException("Interaction Type is not Known");

//                    CurrNode.CurrentLossStateCollection.collection[i].AdjustD();
//                }
//            } //end: all deds


//            for (int i = 0; i < NumBldgs; i++)
//            {
//                CurrNode.CurrentLossStateCollection.collection[i].AdjustD();
//            }

//            //then do all NetOfDed Sub-Limit
//            foreach (Limit limObj in CurrNode.Limits)
//            {
//                if (limObj.LimitIsNetOfDed)
//                {
//                    double limitTot = 0;
//                    if (limObj.LimType == TermValueType.Numeric)
//                        limitTot = limObj.Amount;
//                    else if (limObj.LimType == TermValueType.PercentCovered)
//                        limitTot = limObj.Amount * CurrNode.GetTIV();
//                    else if (limObj.LimType == TermValueType.PercentAffected)
//                        limitTot = limObj.Amount * CurrNode.GetAffectedTIV();
//                    else if (limObj.LimType == TermValueType.PercentLoss)
//                        limitTot = limObj.Amount * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                    else
//                        throw new InvalidOperationException("Lim TermValueType is not Known");

//                    double limit = limitTot;
//                    for (int i = 0; i < NumBldgs; i++)
//                    {
//                        if (AggType == Aggregation.PerBuilding)
//                            limit = limitTot * multiArr[i];

//                        CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X, CurrNode.CurrentLossStateCollection.collection[i].S - CurrNode.CurrentLossStateCollection.collection[i].D - limit);

//                        CurrNode.CurrentLossStateCollection.collection[i].AdjustX();
//                    }
//                }
//            } //end: NetOfDed sub-limit
//        }
//    } //end: TermFunction 
   

////2-----------------------------------
//    class TermFunctionalEngine_2
//    {
//        private TermNode CurrNode;     //Only for TermNode 
//        private Aggregation AggType;

//        public TermFunctionalEngine_2(TermNode _currNode, Aggregation _aggType)
//        {
//            CurrNode = _currNode;
//            AggType = _aggType;
//        }

//        public void TermFunction(InteractionObject[] IntObject)
//        {
//            int intObjCount = IntObject.Count();
//            DeductibleCollection sortedDeductibles = CurrNode.Deductibles.SortByExecOrder(CurrNode.Deductibles);

//            for (int i = 0; i < CurrNode.CurrentLossStateCollection.NumBldgs; i++)
//            {
//                if (intObjCount > 0)
//                {
//                    CurrNode.CurrentLossStateCollection.collection[i].X = IntObject[i].ExcessFromChildren;
//                    CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].DedFromChildren;
//                }
//                else
//                {
//                    CurrNode.CurrentLossStateCollection.collection[i].X = 0;
//                    CurrNode.CurrentLossStateCollection.collection[i].D = 0;
//                }

//                //Do Ground-up sub-Limit first
//                foreach (Limit limObj in CurrNode.Limits)
//                {
//                    if (limObj.LimitIsNetOfDed == false)
//                    {
//                        double limit = 0;
//                        if (limObj.LimType == TermValueType.Numeric)
//                            limit = limObj.Amount;
//                        else if (limObj.LimType == TermValueType.PercentCovered)
//                            limit = limObj.Amount / 100 * CurrNode.GetTIV();
//                        else if (limObj.LimType == TermValueType.PercentAffected)
//                            limit = limObj.Amount / 100 * CurrNode.GetAffectedTIV();
//                        else if (limObj.LimType == TermValueType.PercentLoss)
//                            limit = limObj.Amount / 100 * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                        else
//                            throw new InvalidOperationException("Lim TermValueType is not Known");

//                        if (AggType == Aggregation.PerBuilding)
//                            limit = limit * CurrNode.PrimarySubject.Schedule.MultiplierArr[i];

//                        CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X , Math.Max(0.0, CurrNode.CurrentLossStateCollection.collection[i].S - limit));                       
//                    }
//                }  //end: each Group-up Sub-Limits

//                //if (intObjCount > 0)
//                //{
//                //    CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X, IntObject[i].ExcessFromChildren);
//                //}
//                CurrNode.CurrentLossStateCollection.collection[i].AdjustX();

//                //then do all Deductibles one by one, all deductibles are already in the right exec order
//                if (CurrNode.Deductibles.Count() > 0)
//                {
//                    foreach (Deductible dedObj in sortedDeductibles)
//                    {
//                        double ded = 0;
//                        if (dedObj.DedType == TermValueType.Numeric)
//                            ded = dedObj.Amount;
//                        else if (dedObj.DedType == TermValueType.PercentCovered)
//                            ded = dedObj.Amount * CurrNode.GetTIV();
//                        else if (dedObj.DedType == TermValueType.PercentAffected)
//                            ded = dedObj.Amount * CurrNode.GetAffectedTIV();
//                        else if (dedObj.DedType == TermValueType.PercentLoss)
//                            ded = dedObj.Amount * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                        else
//                            throw new InvalidOperationException("Ded TermValueType is not Known");

//                        if (AggType == Aggregation.PerBuilding)
//                            ded = ded * CurrNode.PrimarySubject.Schedule.MultiplierArr[i];

//                        if (dedObj.DedIsFranchise)                 
//                            ded = (CurrNode.CurrentLossStateCollection.collection[i].S > ded) ? 0.0 : CurrNode.CurrentLossStateCollection.collection[i].S;                                               
//                        else
//                            ded = Math.Min(CurrNode.CurrentLossStateCollection.collection[i].S, ded);

//                        if (dedObj.DedInterType == DedInteractionType.SingleLargest)                        
//                            CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].LargestDedFromChildren;                        
//                        else if (dedObj.DedInterType == DedInteractionType.MAX)
//                            CurrNode.CurrentLossStateCollection.collection[i].D = Math.Min(CurrNode.CurrentLossStateCollection.collection[i].D, ded);
//                        else if (dedObj.DedInterType == DedInteractionType.MIN)
//                            CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].D, ded);
//                        else if (dedObj.DedInterType == DedInteractionType.Absorbing)
//                            CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].D, ded - CurrNode.CurrentLossStateCollection.collection[i].X);
//                        else
//                            throw new InvalidOperationException("Interaction Type is not Known");

//                        CurrNode.CurrentLossStateCollection.collection[i].AdjustD();

//                        //deductible interaction
//                        //if (intObjCount > 0)
//                        //{
//                        //    if (dedObj.DedInterType == DedInteractionType.SingleLargest)
//                        //    {
//                        //        CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].LargestDedFromChildren;
//                        //    }
//                        //    else if (dedObj.DedInterType == DedInteractionType.MIN)
//                        //    {
//                        //        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D);
//                        //    }
//                        //    else if (dedObj.DedInterType == DedInteractionType.Absorbing)
//                        //    {
//                        //        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D - CurrNode.CurrentLossStateCollection.collection[i].X);
//                        //    }
//                        //    else if (dedObj.DedInterType == DedInteractionType.MAX)
//                        //    {
//                        //        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Min(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D);
//                        //    }
//                        //    else
//                        //        throw new InvalidOperationException("Interaction Type is not Known");
//                        //}                
//                    } //end: all deds
//                }
//                else //no deductibles
//                {
//                    CurrNode.CurrentLossStateCollection.collection[i].D = 0;
//                    if (intObjCount > 0)
//                    {
//                        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D);
//                    }
//                }

//                CurrNode.CurrentLossStateCollection.collection[i].AdjustD();

//                //then do all NetOfDed Sub-Limit
//                foreach (Limit limObj in CurrNode.Limits)
//                {
//                    if (limObj.LimitIsNetOfDed)
//                    {
//                        double limit = 0;
//                        if (limObj.LimType == TermValueType.Numeric)
//                            limit = limObj.Amount;
//                        else if (limObj.LimType == TermValueType.PercentCovered)
//                            limit = limObj.Amount * CurrNode.GetTIV();
//                        else if (limObj.LimType == TermValueType.PercentAffected)
//                            limit = limObj.Amount * CurrNode.GetAffectedTIV();
//                        else if (limObj.LimType == TermValueType.PercentLoss)
//                            limit = limObj.Amount * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                        else
//                            throw new InvalidOperationException("Lim TermValueType is not Known");

//                        if (AggType == Aggregation.PerBuilding)
//                            limit = limit * CurrNode.PrimarySubject.Schedule.MultiplierArr[i];

//                        CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(0.0, CurrNode.CurrentLossStateCollection.collection[i].S - CurrNode.CurrentLossStateCollection.collection[i].D - limit);
//                        if (intObjCount > 0)
//                        {
//                            CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X, IntObject[i].ExcessFromChildren);
//                        }

//                        CurrNode.CurrentLossStateCollection.collection[i].AdjustX();
//                    }
//                } //end: NetOfDed sub-limit
//            } //end: for each building                   
//            CurrNode.CurrentLossStateCollection.CalcTotalSum();
//        } //end: TermFunction       
//    }

////3----------------------------------- vectorized
//    class TermFunctionalEngine_3
//    {
//        private TermNode CurrNode;     //Only for TermNode 
//        private Aggregation AggType;

//        public TermFunctionalEngine_3(TermNode _currNode, Aggregation _aggType)
//        {
//            CurrNode = _currNode;
//            AggType = _aggType;
//        }

//        public void TermFunction(LossStateCollection2 LossFromBelow)
//        {
//            //int intObjCount = IntObject.Count();
//            double [] excessFromBelow = LossFromBelow.Excess;
//            double [] dedFromBelow = LossFromBelow.Deductible;
//            double [] lossFromBelow = LossFromBelow.SubjectLoss;
//            double [] recovFromBelow = LossFromBelow.Recoverable;

//            if (LossFromBelow.NumBldgs != CurrNode.CurrentLossStateCollection.NumBldgs)
//                throw new ArgumentOutOfRangeException("Number of buildings from below must equal number of buildings in current trem node...when performing interaction!");

//            int numbldgs = CurrNode.CurrentLossStateCollection.NumBldgs;

//            DeductibleCollection sortedDeductibles = CurrNode.Deductibles.SortByExecOrder(CurrNode.Deductibles);

//            if (LossFromBelow.NumBldgs > 0)
//            {
//                //CurrNode.CurrentLossStateCollection.collection[i].X = IntObject[i].ExcessFromChildren;                
//                //CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].DedFromChildren;

//                Array.Copy(excessFromBelow, CurrNode.CurrentLossStateCollection.Excess, numbldgs);
//                Array.Copy(dedFromBelow, CurrNode.CurrentLossStateCollection.Deductible, numbldgs);

//            }
//            else
//            {
//                //CurrNode.CurrentLossStateCollection.collection[i].X = 0;
//                //CurrNode.CurrentLossStateCollection.collection[i].D = 0;

//                Array.Clear(CurrNode.CurrentLossStateCollection.Excess, 0, numbldgs);
//                Array.Clear(CurrNode.CurrentLossStateCollection.Deductible, 0, numbldgs);
//            }

//            for (int i = 0; i < CurrNode.CurrentLossStateCollection.NumBldgs; i++)
//            {                
//                //Do Ground-up sub-Limit first
//                foreach (Limit limObj in CurrNode.Limits)
//                {
//                    if (limObj.LimitIsNetOfDed == false)
//                    {
//                        double limit = 0;
//                        if (limObj.LimType == TermValueType.Numeric)
//                            limit = limObj.Amount;
//                        else if (limObj.LimType == TermValueType.PercentCovered)
//                            limit = limObj.Amount / 100 * CurrNode.GetTIV();
//                        else if (limObj.LimType == TermValueType.PercentAffected)
//                            limit = limObj.Amount / 100 * CurrNode.GetAffectedTIV();
//                        else if (limObj.LimType == TermValueType.PercentLoss)
//                            limit = limObj.Amount / 100 * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                        else
//                            throw new InvalidOperationException("Lim TermValueType is not Known");

//                        if (AggType == Aggregation.PerBuilding)
//                            limit = limit * CurrNode.PrimarySubject.Schedule.MultiplierArr[i];

//                        CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X, Math.Max(0.0, CurrNode.CurrentLossStateCollection.collection[i].S - limit));
//                    }
//                }  //end: each Group-up Sub-Limits

//                //if (intObjCount > 0)
//                //{
//                //    CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X, IntObject[i].ExcessFromChildren);
//                //}
//                CurrNode.CurrentLossStateCollection.collection[i].AdjustX();

//                //then do all Deductibles one by one, all deductibles are already in the right exec order
//                if (CurrNode.Deductibles.Count() > 0)
//                {
//                    foreach (Deductible dedObj in sortedDeductibles)
//                    {
//                        double ded = 0;
//                        if (dedObj.DedType == TermValueType.Numeric)
//                            ded = dedObj.Amount;
//                        else if (dedObj.DedType == TermValueType.PercentCovered)
//                            ded = dedObj.Amount * CurrNode.GetTIV();
//                        else if (dedObj.DedType == TermValueType.PercentAffected)
//                            ded = dedObj.Amount * CurrNode.GetAffectedTIV();
//                        else if (dedObj.DedType == TermValueType.PercentLoss)
//                            ded = dedObj.Amount * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                        else
//                            throw new InvalidOperationException("Ded TermValueType is not Known");

//                        if (AggType == Aggregation.PerBuilding)
//                            ded = ded * CurrNode.PrimarySubject.Schedule.MultiplierArr[i];

//                        if (dedObj.DedIsFranchise)
//                            ded = (CurrNode.CurrentLossStateCollection.collection[i].S > ded) ? 0.0 : CurrNode.CurrentLossStateCollection.collection[i].S;
//                        else
//                            ded = Math.Min(CurrNode.CurrentLossStateCollection.collection[i].S, ded);

//                        if (dedObj.DedInterType == DedInteractionType.SingleLargest)
//                            CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].LargestDedFromChildren;
//                        else if (dedObj.DedInterType == DedInteractionType.MAX)
//                            CurrNode.CurrentLossStateCollection.collection[i].D = Math.Min(CurrNode.CurrentLossStateCollection.collection[i].D, ded);
//                        else if (dedObj.DedInterType == DedInteractionType.MIN)
//                            CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].D, ded);
//                        else if (dedObj.DedInterType == DedInteractionType.Absorbing)
//                            CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].D, ded - CurrNode.CurrentLossStateCollection.collection[i].X);
//                        else
//                            throw new InvalidOperationException("Interaction Type is not Known");

//                        CurrNode.CurrentLossStateCollection.collection[i].AdjustD();

//                        //deductible interaction
//                        //if (intObjCount > 0)
//                        //{
//                        //    if (dedObj.DedInterType == DedInteractionType.SingleLargest)
//                        //    {
//                        //        CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].LargestDedFromChildren;
//                        //    }
//                        //    else if (dedObj.DedInterType == DedInteractionType.MIN)
//                        //    {
//                        //        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D);
//                        //    }
//                        //    else if (dedObj.DedInterType == DedInteractionType.Absorbing)
//                        //    {
//                        //        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D - CurrNode.CurrentLossStateCollection.collection[i].X);
//                        //    }
//                        //    else if (dedObj.DedInterType == DedInteractionType.MAX)
//                        //    {
//                        //        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Min(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D);
//                        //    }
//                        //    else
//                        //        throw new InvalidOperationException("Interaction Type is not Known");
//                        //}                
//                    } //end: all deds
//                }
//                else //no deductibles
//                {
//                    CurrNode.CurrentLossStateCollection.collection[i].D = 0;
//                    if (intObjCount > 0)
//                    {
//                        CurrNode.CurrentLossStateCollection.collection[i].D = Math.Max(IntObject[i].DedFromChildren, CurrNode.CurrentLossStateCollection.collection[i].D);
//                    }
//                }

//                CurrNode.CurrentLossStateCollection.collection[i].AdjustD();

//                //then do all NetOfDed Sub-Limit
//                foreach (Limit limObj in CurrNode.Limits)
//                {
//                    if (limObj.LimitIsNetOfDed)
//                    {
//                        double limit = 0;
//                        if (limObj.LimType == TermValueType.Numeric)
//                            limit = limObj.Amount;
//                        else if (limObj.LimType == TermValueType.PercentCovered)
//                            limit = limObj.Amount * CurrNode.GetTIV();
//                        else if (limObj.LimType == TermValueType.PercentAffected)
//                            limit = limObj.Amount * CurrNode.GetAffectedTIV();
//                        else if (limObj.LimType == TermValueType.PercentLoss)
//                            limit = limObj.Amount * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
//                        else
//                            throw new InvalidOperationException("Lim TermValueType is not Known");

//                        if (AggType == Aggregation.PerBuilding)
//                            limit = limit * CurrNode.PrimarySubject.Schedule.MultiplierArr[i];

//                        CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(0.0, CurrNode.CurrentLossStateCollection.collection[i].S - CurrNode.CurrentLossStateCollection.collection[i].D - limit);
//                        if (intObjCount > 0)
//                        {
//                            CurrNode.CurrentLossStateCollection.collection[i].X = Math.Max(CurrNode.CurrentLossStateCollection.collection[i].X, IntObject[i].ExcessFromChildren);
//                        }

//                        CurrNode.CurrentLossStateCollection.collection[i].AdjustX();
//                    }
//                } //end: NetOfDed sub-limit
//            } //end: for each building                   
//            CurrNode.CurrentLossStateCollection.CalcTotalSum();
//        } //end: TermFunction       
//    }


    //4-------------------------------------------- vectorized
    
    class TermFunctionalEngine_4
    {
        private TermNode CurrNode;     //Only for TermNode 
        private Aggregation AggType;
        private double[] multiArr;


        public TermFunctionalEngine_4(TermNode _currNode, Aggregation _aggType)
        {
            CurrNode = _currNode;
            AggType = _aggType;
            multiArr = CurrNode.PrimarySubject.Schedule.MultiplierArr;
        }

        public void TermFunction(LossStateCollection2 LossFromBelow)
        {
            int NumBldgs = CurrNode.CurrentLossStateCollection.NumBldgs;

            //int intObjCount = IntObject.Count();
            double[] excessFromBelow = LossFromBelow.Excess;
            double[] dedFromBelow = LossFromBelow.Deductible;
            double[] lossFromBelow = LossFromBelow.SubjectLoss;
            double[] recovFromBelow = LossFromBelow.Recoverable;

            double[] nodeexcess = CurrNode.CurrentLossStateCollection.Excess;
            double[] nodeded = CurrNode.CurrentLossStateCollection.Deductible;
            double[] nodeloss = CurrNode.CurrentLossStateCollection.SubjectLoss;
            double[] noderecov = CurrNode.CurrentLossStateCollection.Recoverable;

            //if (LossFromBelow.NumBldgs != CurrNode.CurrentLossStateCollection.NumBldgs)
            //    throw new ArgumentOutOfRangeException("Number of buildings from below must equal number of buildings in current trem node...when performing interaction!");
           
            DeductibleCollection sortedDeductibles = CurrNode.Deductibles.SortByExecOrder(CurrNode.Deductibles);


            //Intialize
            if (LossFromBelow.NumBldgs == CurrNode.CurrentLossStateCollection.NumBldgs)
            {
                //CurrNode.CurrentLossStateCollection.collection[i].X = IntObject[i].ExcessFromChildren;                
                //CurrNode.CurrentLossStateCollection.collection[i].D = IntObject[i].DedFromChildren;

                Array.Copy(excessFromBelow, nodeexcess, NumBldgs);
                Array.Copy(dedFromBelow, nodeded, NumBldgs);

            }
            else
            {
                //CurrNode.CurrentLossStateCollection.collection[i].X = 0;
                //CurrNode.CurrentLossStateCollection.collection[i].D = 0;

                Array.Clear(CurrNode.CurrentLossStateCollection.Excess, 0, NumBldgs);
                Array.Clear(CurrNode.CurrentLossStateCollection.Deductible, 0, NumBldgs);
            }

            //Do Ground-up sub-Limit first
            foreach (Limit limObj in CurrNode.Limits)
            {
                if (limObj.LimitIsNetOfDed == false)
                {
                    double limitTot = 0;
                    if (limObj.LimType == TermValueType.Numeric)
                        limitTot = limObj.Amount;
                    else if (limObj.LimType == TermValueType.PercentCovered)
                        limitTot = limObj.Amount / 100 * CurrNode.GetTIV();
                    else if (limObj.LimType == TermValueType.PercentAffected)
                        limitTot = limObj.Amount / 100 * CurrNode.GetAffectedTIV();
                    else if (limObj.LimType == TermValueType.PercentLoss)
                        limitTot = limObj.Amount / 100 * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
                    else
                        throw new InvalidOperationException("Lim TermValueType is not Known");

                    double limit = limitTot;
                    for (int i = 0; i < NumBldgs; i++)
                    {
                        if (AggType == Aggregation.PerBuilding)
                            limit = limitTot * multiArr[i];

                        nodeexcess[i] = Math.Max(nodeexcess[i], Math.Max(0.0, nodeloss[i] - limit));
                    }

                    CurrNode.CurrentLossStateCollection.AdjustX();
                    
                }  //end: each Group-up Sub-Limits
            }

            //then do all Deductibles one by one, all deductibles are already in the right exec order       
            foreach (Deductible dedObj in sortedDeductibles)
            {
                double dedTot = 0;
                if (dedObj.DedType == TermValueType.Numeric)
                    dedTot = dedObj.Amount;
                else if (dedObj.DedType == TermValueType.PercentCovered)
                    dedTot = dedObj.Amount * CurrNode.GetTIV();
                else if (dedObj.DedType == TermValueType.PercentAffected)
                    dedTot = dedObj.Amount * CurrNode.GetAffectedTIV();
                else if (dedObj.DedType == TermValueType.PercentLoss)
                    dedTot = dedObj.Amount * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
                else
                    throw new InvalidOperationException("Ded TermValueType is not Known");

                double ded = dedTot;
                for (int i = 0; i < NumBldgs; i++)
                {
                    if (AggType == Aggregation.PerBuilding)
                        ded = dedTot * multiArr[i];

                    if (dedObj.DedIsFranchise)
                        ded = (nodeloss[i] > ded) ? 0.0 : nodeloss[i];
                    else
                        ded = Math.Min(nodeloss[i], ded);

                    if (dedObj.DedInterType == DedInteractionType.SingleLargest)
                        nodeded[i] = LossFromBelow.GetLargestDed();
                    else if (dedObj.DedInterType == DedInteractionType.MAX)
                        nodeded[i] = Math.Min(nodeded[i], ded);
                    else if (dedObj.DedInterType == DedInteractionType.MIN)
                        nodeded[i] = Math.Max(nodeded[i], ded);
                    else if (dedObj.DedInterType == DedInteractionType.Absorbing)
                        nodeded[i] = Math.Max(nodeded[i], ded - nodeexcess[i]);
                    else
                        throw new InvalidOperationException("Interaction Type is not Known");            
                }

                CurrNode.CurrentLossStateCollection.AdjustD();
            } //end: all deds


            //for (int i = 0; i < NumBldgs; i++)
            //{
            //    CurrNode.CurrentLossStateCollection.AdjustD();
            //}

            CurrNode.CurrentLossStateCollection.AdjustD();

            //then do all NetOfDed Sub-Limit
            foreach (Limit limObj in CurrNode.Limits)
            {
                if (limObj.LimitIsNetOfDed)
                {
                    double limitTot = 0;
                    if (limObj.LimType == TermValueType.Numeric)
                        limitTot = limObj.Amount;
                    else if (limObj.LimType == TermValueType.PercentCovered)
                        limitTot = limObj.Amount * CurrNode.GetTIV();
                    else if (limObj.LimType == TermValueType.PercentAffected)
                        limitTot = limObj.Amount * CurrNode.GetAffectedTIV();
                    else if (limObj.LimType == TermValueType.PercentLoss)
                        limitTot = limObj.Amount * CurrNode.CurrentLossStateCollection.GetTotalSum.S;
                    else
                        throw new InvalidOperationException("Lim TermValueType is not Known");

                    double limit = limitTot;
                    for (int i = 0; i < NumBldgs; i++)
                    {
                        if (AggType == Aggregation.PerBuilding)
                            limit = limitTot * multiArr[i];

                        nodeexcess[i] = Math.Max(nodeexcess[i], nodeloss[i] - nodeded[i] - limit);

                        //CurrNode.CurrentLossStateCollection.collection[i].AdjustX();
                    }

                    CurrNode.CurrentLossStateCollection.AdjustX();
                }
            } //end: NetOfDed sub-limit
        }
    } //end: TermFunction 
}

