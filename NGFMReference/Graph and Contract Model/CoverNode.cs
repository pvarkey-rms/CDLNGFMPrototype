using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class CoverNode : GraphNode, IAllocatable
    {
        private Cover cover;
        public AllocationStateCollection CurrentAllocationState;
        public AggState CurrentAggState;
        public override bool IsPerRisk { get { return false; } }


        public Cover Cover
        {
            get { {return cover; }}
            set
            {
                cover = value;
                CurrentAggState.SetFinTerms(value);
            }
        }

        public String CoverName { get; private set; }
        public double Payout { get; set; }
        public double AllocationPayout { get { return CurrentAllocationState.GetPayout(); } }

        public CoverNode(Subject _subject, string _coverName)
            : base(_subject)
        {
            Payout = 0;
            CoverName = _coverName;
            CurrentAllocationState = new AllocationStateCollection(1);
            //CurrentAggState = new AggState(Cover.AggDedAmount, Cover.AggLimitAmount, Cover.DedOfAggDedLayer.Amount, Cover.LimitOfAggDedLayer.Amount);
            CurrentAggState = new AggState();
            keystring = keystring + "-Cov: " + _coverName;
        }

        public override void Reset()
        {
            base.Reset();
            Payout = 0;
            CurrentAllocationState.Reset();
        }

        public override void PeriodReset()
        {
            CurrentAggState.Reset();
        }
        //////// IAllocatable
        public bool AllocateByRecoverable
        { 
            get 
            {
                if (Subject.IsDerived)
                    return false;
                else
                    return true;
            }   
        }
        public bool AllocateRecoverableFirst { get { return false; } }

        public void SetAllocState(AllocationStateCollection allocState)
        {
            CurrentAllocationState = allocState;
        }

        public LossStateCollection GetLossState()
        {
            LossStateCollection coverNodeLossState = new LossStateCollection(1);         
            return coverNodeLossState;
        }

        public AllocationStateCollection GetAllocState()
        {            
            return CurrentAllocationState;
        }

        public Double GetPayout()
        {
            return Payout;
        }
    }

    public class AggState
    {
        public double AggLimitAmount { get; private set; }
        public bool IsUnlimited { get; private set; }
        public double AggDedAmount { get; private set; }
        public double DedAmountOfAggDedLayer { get; private set; }
        public double LimitAmountOfAggDedLayer { get; private set; }
        public double DedFilled { get; set; }       
        public double AggPayout { get; set; }
        //public double CurrPayout { get; set; }
        public double RemainingLoss { get; set; }
        public bool IsAggDedFilled
        {
            get
            {
                if (DedFilled >= AggDedAmount)
                    return true;
                else
                    return false;
            }
        }
        public bool IsAggLimitFilled
        {
            get
            {
                if (AggPayout >= AggLimitAmount)
                    return true;
                else
                    return false;
            }
        }

        public AggState()
        {
            Reset();
        }

        public void SetFinTerms(double _aggDedAmount, double _aggLimitAmount, double _dedOfAggDedLayer, double _limitOfAggDedLayer)
        {
            AggDedAmount = _aggDedAmount;
            AggLimitAmount = _aggLimitAmount;
            DedAmountOfAggDedLayer = _dedOfAggDedLayer;
            LimitAmountOfAggDedLayer = _limitOfAggDedLayer;
        }

        public void SetFinTerms(Cover cover)
        {
            if (cover.AttachmentTimeBasis == TimeBasis.Aggregate)
                AggDedAmount = cover.AttPointAmount;
            else
                AggDedAmount = 0;

            if (cover.LimitTimeBasis == TimeBasis.Aggregate)
                AggLimitAmount = cover.LimitAmount;
            else
                IsUnlimited = true; 

            DedAmountOfAggDedLayer = cover.DedOfAggDedLayer.Amount;
            LimitAmountOfAggDedLayer = cover.LimitOfAggDedLayer.Amount;
        }

        public void Reset()
        {
            DedFilled = 0.0;
            //CurrPayout = 0.0;
            AggPayout = 0.0;
            RemainingLoss = 0.0;
        }

        public double ApplyAggDed(double incomingLoss)
        {
            double dedToAgg = 0.0;
            double payout = incomingLoss;
            double remainingLoss = 0.0;

            if (!IsAggDedFilled)
            {
                dedToAgg = Math.Min(Math.Max(0, incomingLoss - DedAmountOfAggDedLayer), AggDedAmount - DedFilled);
                //dedToAgg = (LimitAmountOfAggDedLayer > 0 ? Math.Min(dedToAgg, LimitAmountOfAggDedLayer) : dedToAgg);
                //payout = Math.Max(0, incomingLoss - dedToAgg);
                if (LimitAmountOfAggDedLayer > 0)
                {
                    dedToAgg = Math.Min(dedToAgg, LimitAmountOfAggDedLayer);
                    //payout = Math.Min(LimitAmountOfAggDedLayer, Math.Max(0, incomingLoss - dedToAgg));
                    payout = Math.Max(0, incomingLoss - dedToAgg);
                }
                else
                {
                    payout = incomingLoss - dedToAgg;
                }
                remainingLoss = incomingLoss - payout - dedToAgg;
                update(dedToAgg, payout, remainingLoss);
                return payout;
            }
            else
                return incomingLoss;        
        }

        public double ApplyAggLimit(double currPayout)
        {
            if (!IsAggLimitFilled && AggPayout > AggLimitAmount)
            {
                double diff = AggPayout - AggLimitAmount;
                RemainingLoss += diff;
                currPayout = currPayout - diff;
                AggPayout = AggLimitAmount;                 
            }
            else if (IsAggLimitFilled)
            {
                RemainingLoss += currPayout;
                currPayout = 0;
            }
            return currPayout;
        }

        public void update(double _ded, double _payout, double _rLosss)
        {
            this.DedFilled += _ded;            
            this.AggPayout += _payout;
            this.RemainingLoss = _rLosss;
            //this.CurrPayout = _currPayout;
        }

        //public AggState CopyToAggState()
        //{
        //    AggState copiedState = new AggState();
        //    copiedState.DedFilled = DedFilled;
        //    copiedState.LimitFilled = LimitFilled;
        //    copiedState.Payout = Payout;
        //    copiedState.RemainingLoss = RemainingLoss;

        //    return copiedState;
        //}
    }  //AggState

}
