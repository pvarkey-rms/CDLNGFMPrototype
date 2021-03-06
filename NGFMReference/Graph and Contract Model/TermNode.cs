﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class TermNode: GraphNode, IAllocatable
    {
        public AllocationStateCollection CurrentAllocationState { get; set; }

        public DeductibleCollection Deductibles { get; set; }
        public LimitCollection Limits { get; set; }
        private double coveredTIV;
        private double affectedTIV;

        public double CoveredTIV
        {
            get
            {
                if (coveredTIV == null)
                    coveredTIV = GetTIV();

                return coveredTIV;
            }
        }
        public double AffectedTIV
        {
            get
            {
                if (affectedTIV == null)
                    affectedTIV = GetAffectedTIV();

                return affectedTIV;
            }
        }

        //Rain please delete /////////////////////
        //public bool DedIsFranchise { get; private set; }
        //public TermValueType DedType { get; private set; }
        //public TermValueType LimType { get; private set; }
        //public bool LimitIsNetOfDed { get; private set; }
        //public double Deductible { get; private set; }
        //public double Limit { get; private set; }
        //public bool DedIsPerRisk { get; private set; }
        //public bool LimIsPerRisk { get; private set; }
        //public DedInteractionType DedInterType { get; private set; }
        //Rain please delete///////////////////////////////////////////

        //Sunny will delete//////
        //public void SetDedFinTerms(bool _dedIsFranchise, DedInteractionType _dedInterType, double _deductible, bool _dedIsPerRisk, TermValueType _dedBaseType)
        //{
        //    DedIsFranchise = _dedIsFranchise;
        //    Deductible = _deductible;
        //    DedInterType = _dedInterType;
        //    DedIsPerRisk = _dedIsPerRisk;
        //    DedType = _dedBaseType;
        //}

        //public void SetLimFinTerms(bool _limitIsNetOfDed, double _limit, bool _limIsPerRisk, TermValueType _limType)
        //{
        //    LimitIsNetOfDed = _limitIsNetOfDed;
        //    Limit = _limit;
        //    LimIsPerRisk = _limIsPerRisk;
        //    LimType = _limType;
        //}
        //Sunny wil delete ///////////////////


        public override bool IsPerRisk 
        { get 
            {
                if (Deductibles.IsPerRisk || Limits.IsPerRisk)
                    return true;
                else
                    return false;        
            } 
        }

        public PrimarySubject PrimarySubject
        {
            get { return (PrimarySubject)subject; }
        }
        public HashSet<CoverageAtomicRITE> CovARITES
        {
            get
            {
                return new HashSet<CoverageAtomicRITE>(base.AllAtomicRITEs.Cast<CoverageAtomicRITE>());
            }
        }
          
        public LossStateCollection CurrentLossStateCollection {get; set;}  

        public TermNode(PrimarySubject _subject): base(_subject)
        {   
            if(_subject.IsDerived)
                throw new ArgumentOutOfRangeException("Terms Nodes cannot have a derivied subject!");
            CurrentAllocationState = new AllocationStateCollection(_subject.Schedule.ActNumOfBldgs);
            Deductibles = new DeductibleCollection();
            Limits = new LimitCollection();

        }

        public override void Reset()
        {
            base.Reset();
            if (IsPerRisk)
                CurrentLossStateCollection = new LossStateCollection(PrimarySubject.Schedule.ActNumOfBldgs);
            else
                CurrentLossStateCollection = new LossStateCollection(1);
            //CurrentLossState.Reset();
            CurrentAllocationState.Reset();
        }

        public override void PeriodReset()
        {

        }

        public double GetTIV()
        {
            double tiv = 0;
            foreach (CoverageAtomicRITE CovARITE in CovARITES)
            {
                tiv += CovARITE.RITE.GetTIV(CovARITE.RITCharacterisiticID);
            }

            return tiv;
        }

        public double GetAffectedTIV()
        {
            double tiv = 0;
            foreach (CoverageAtomicRITE CovARITE in CovARITES)
            {
                if (CovARITE.OriginalSubjectLoss.TotalLoss > 0)
                    tiv += CovARITE.RITE.GetTIV(CovARITE.RITCharacterisiticID);
            }

            return tiv;
        }

        ///// IAllocatable Override
        public bool AllocateByRecoverable { get { return true; } }
        public bool AllocateRecoverableFirst { get { return true; } }

        public void SetAllocState(AllocationStateCollection allocState)
        {
            CurrentAllocationState = allocState;
        }

        public LossStateCollection GetLossState()
        {
            return CurrentLossStateCollection;        
        }

        public AllocationStateCollection GetAllocState()
        {
            return CurrentAllocationState;
        }
        
        //public HashSet<AtomicRITE> GetAtomicRites()
        //{
        //    HashSet<AtomicRITE> ARITEs = new HashSet<AtomicRITE>();

        //    foreach (RITE rite in subject.Schedule.RITEs)
        //    {
        //        foreach (string subperil in subject.CauseOfLossSet.GetSubperils())
        //        {
        //            foreach (ExposureType expType in subject.ExposureTypes)
        //            {
        //                var RITChars = rite.RiskCharacteristics.Where(RitChar => RitChar.ExpType == expType);
        //                foreach (RITCharacteristic RitChar in RITChars)
        //                {
        //                    ARITEs.Add(new CoverageAtomicRITE(subperil, expType, rite, RitChar.ID));
        //                }
        //            }
        //        }
        //    }

        //    return ARITEs;

        //} 
    }

    public enum DedInteractionType
    {       
        MIN,
        MAX,
        SingleLargest,
        Absorbing
    }

    public enum TermValueType
    {
        Numeric,
        Percent,
        PercentCovered,
        PercentAffected,
        PercentLoss,
        PayFunction           
    }

    public enum DedFunctionName
    {
        RCV,
        Subject
    }

    public class LossState
    {
        public double S { get; set; }
        public double X { get; set; }
        public double R { get; set; }
        public double D { get; set; }

        public LossState()
        {
            S = 0.0;
            X = 0.0;
            R = 0.0;
            D = 0.0;
        }

        public LossState(LossState inputLossesState)
        {
            S = inputLossesState.S;
            X = inputLossesState.X;
            R = inputLossesState.R;
            D = inputLossesState.D;
        }

        public void AdjustD()
        {            
            D = Math.Min(D, S - X);            
        }

        public void AdjustX()
        {
            X = Math.Min(X, S - D);                  
        }

        public void AdjustR()
        {            
            R = S - X - D;
        }

        public void Reset()
        {
            S = 0.0;
            X = 0.0;
            R = 0.0;
            D = 0.0;
        }

        public static LossState operator +(LossState loss1, LossState loss2)
        {
            LossState sum = new LossState();
            sum.S = loss1.S + loss2.S;
            sum.X = loss1.X + loss2.X;
            sum.D = loss1.D + loss2.D;
            sum.R = loss1.R + loss2.R;
            return sum;
        }
    
    }

    public class LossStateCollection : IEnumerable<LossState>
    {
        public LossState[] collection;
        public int NumBldgs
        {
            get { return collection.Count(); }
        }

        public double Loss
        {
            get
            {
                double total = 0;
                foreach (LossState loss in collection)
                {
                    total += loss.R;
                }
                return total;
            }
        }

        private LossState totalSum;
        public LossState GetTotalSum
        {
            get
            {
                return totalSum;
            }
        }

        public LossStateCollection(int NumBldgs)
        {
            collection = new LossState[NumBldgs];
            for (int i = 0; i < NumBldgs; i++)
            {
                collection[i] = new LossState();
            }

            CalcTotalSum();
        }

        public void CalcTotalSum()
        {
            LossState total = new LossState();
            foreach (LossState loss in collection)
            {
                total += loss;
            }
            totalSum = total;
        }

        public void SumLossesFrom(LossStateCollection otherLosses)
        {
            if (this.NumBldgs == otherLosses.NumBldgs)
            {
                collection.Zip(otherLosses.collection, (a, b) => a + b);
            }
            else if (this.NumBldgs == 1)
            {
                collection[0] += otherLosses.GetTotalSum;
            }

            CalcTotalSum();
        }

        public double[] GetSubjectLosses()
        {
            return collection.Select(loss => loss.S).ToArray();
        }

        public void SetSubjectLosses(double[] inputlosses)
        {
            if (NumBldgs > 1 && inputlosses.Length != NumBldgs)  //Rain, at this point, these could be different b/a of summed or per-building
                throw new InvalidOperationException("CurrentLossStateCollection length NOT matching input GU Loss");
            if (NumBldgs == 1)
            {
                collection[0].S = inputlosses.Sum();
            }
            else
            {
                for (int i = 0; i < NumBldgs; i++)
                {
                    collection[i].S = inputlosses[i];       //TODE: test                      
                }
            }

            CalcTotalSum();

        }

        public void SetRecoverableLosses(double[] inputlosses)
        {
            if (inputlosses.Length != NumBldgs)
                throw new InvalidOperationException("CurrentLossStateCollection length NOT matching input GU Loss");
            for (int i = 0; i < NumBldgs; i++)
            {
                collection[i].R = inputlosses[i];
            }

            CalcTotalSum();
        }

        public void Reset()
        {
            for (int i = 0; i < NumBldgs; i++)
            {
                collection[i].Reset();
            }

            CalcTotalSum();
        }

        public IEnumerator<LossState> GetEnumerator()
        {
            foreach (LossState state in collection)
            {
                // Lets check for end of list (its bad code since we used arrays)
                if (state == null)
                {
                    break;
                }

                // Return the current element and then on next function call 
                // resume from next element rather than starting all over again;
                yield return state;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }


    public class LossStateCollection2 : IEnumerable<LossState>
    {
        private double[] subjectloss;
        private double[] excess;
        private double[] recoverable;
        private double[] deductible;

        public double[] SubjectLoss { get; set; }
        public double[] Excess { get; set; }
        public double[] Recoverable { get; set; }
        public double[] Deductible { get; set; }

        private int numBldgs;

        public int NumBldgs
        {
            get { return numBldgs; }
        }

        public double Loss
        {
            get
            {
                double total = 0;
                for (int i = 0; i < NumBldgs; i++)
                {
                    total += recoverable[i];
                }
                return total;
            }
        }

        private LossState totalSum;
        public LossState GetTotalSum
        {
            get
            {
                return totalSum;
            }
        }

        public LossStateCollection2(int NumBldgs)
        {
            numBldgs = NumBldgs;
            subjectloss = new double[NumBldgs];
            excess = new double[NumBldgs];
            recoverable = new double[NumBldgs];
            deductible = new double[NumBldgs];

            CalcTotalSum();
        }

        public void CalcTotalSum()
        {
            LossState total = new LossState();

            double totalsub = 0;
            double totalexcess = 0;
            double totalrecov = 0;
            double totalded = 0;

            for (int i = 0; i < NumBldgs; i++)
            {
                totalsub += subjectloss[i];
                totalexcess += excess[i];
                totalrecov += recoverable[i];
                totalded += deductible[i];
            }

            totalSum.S = totalsub;
            totalSum.X = totalexcess;
            totalSum.R = totalrecov;
            totalSum.D = totalded;
        }

        public void SumLossesFrom(LossStateCollection2 otherLosses)
        {
            if (this.NumBldgs == otherLosses.NumBldgs)
            {
                double[] othersubjectloss = otherLosses.subjectloss;
                double[] otherexcess = otherLosses.excess;
                double[] otherrecoverable = otherLosses.recoverable;
                double[] otherdeductible = otherLosses.deductible;

                for (int i = 0; i < NumBldgs; i++)
                {
                    subjectloss[i] += othersubjectloss[i];
                    excess[i] += otherexcess[i];
                    recoverable[i] += otherrecoverable[i];
                    deductible[i] += otherdeductible[i];
                }
            }
            else if (this.NumBldgs == 1)
            {
                subjectloss[0] += otherLosses.GetTotalSum.S;
                excess[0] += otherLosses.GetTotalSum.X;
                recoverable[0] += otherLosses.GetTotalSum.R;
                deductible[0] += otherLosses.GetTotalSum.D;
            }

            CalcTotalSum();
        }

        public double[] GetSubjectLosses()
        {
            return SubjectLoss;
        }

        public void SetSubjectLosses(double[] inputlosses)
        {
            if (NumBldgs > 1 && inputlosses.Length != NumBldgs)  //Rain, at this point, these could be different b/a of summed or per-building
                throw new InvalidOperationException("CurrentLossStateCollection length NOT matching input GU Loss");
            if (NumBldgs == 1)
            {
                subjectloss[0] = inputlosses.Sum();
                totalSum.S = subjectloss[0];
            }
            else
            {
                double total = 0;
                for (int i = 0; i < NumBldgs; i++)
                {
                    subjectloss[i] = inputlosses[i];  //TODE: test  
                    total += inputlosses[i];
                }
                totalSum.S = total;
            }
        }

        public void SetRecoverableLosses(double[] inputlosses)
        {
            if (inputlosses.Length != NumBldgs)
                throw new InvalidOperationException("CurrentLossStateCollection length NOT matching input GU Loss");

            double total = 0;
            for (int i = 0; i < NumBldgs; i++)
            {
                recoverable[i] = inputlosses[i];
                total += inputlosses[i];
            }

            totalSum.R = total;
        }

        public void Reset()
        {
            Array.Clear(subjectloss, 0, NumBldgs);
            Array.Clear(excess, 0, NumBldgs);
            Array.Clear(recoverable, 0, NumBldgs);
            Array.Clear(deductible, 0, NumBldgs);

            totalSum.D = 0;
            totalSum.R = 0;
            totalSum.S = 0;
            totalSum.X = 0;
        }

        public IEnumerator<LossState> GetEnumerator()
        {
            for (int i = 0; i < NumBldgs; i++)
            {
                LossState state = new LossState();

                state.D = Deductible[i];
                state.R = Recoverable[i];
                state.S = SubjectLoss[i];
                state.X = Excess[i];

                yield return state;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
