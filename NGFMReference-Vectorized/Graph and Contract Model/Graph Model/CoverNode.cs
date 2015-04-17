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
        public AllocationStateCollection2 CurrentAllocationState;
        public AggState CurrentAggState;
        //public override bool IsPerRisk { get { return false; } }
        //private long FirstRITEID {get; set;}
        long defaultRiteID = -1;  //mean the cover is not PerRisk, not RiteID associated
        private long FirstRITEcharID { get { return defaultRiteID; } set { defaultRiteID = value; } }
        private long defaultFactorIndex = -1;  //corresponding to factor = 1;
        //public float CoverFactor { get { return defaultFactor; } set { defaultFactor = value; } }  //TODO: this is set through PerRisk Expansin because we need to know its location. 
        //public int CoverFactorIndex { get { return defaultFactorIndex; } set { defaultFactorIndex = value; } }  //TODO: this is set through PerRisk Expansin because we need to know its location. 
        //public override float GetFactor()
        //{
        //    return CoverFactor; 
        //}

        //public override int GetFactorIndex()
        //{
        //    return CoverFactorIndex;
        //}

        //public override long GetFirstRITEID()
        //{
        //    if (FirstRITEID != null)
        //        return FirstRITEID;
        //    else
        //        return -1; //means this Summed term, so no need for Factors
        //}

        public override long GetFirstRITEcharID()
        {
            //if (FirstRITEcharID != -1)
                return FirstRITEcharID;
           // else
            //    return -1; //means this Summed term, so no need for Factors

        }
        public void SetFirstRITEcharID(long _rID)
        {
            FirstRITEcharID = _rID;
        }

        public override bool IsPerRisk
        { get 
            {
                if (Subject is PrimarySubject)
                {
                    PrimarySubject pSubject = Subject as PrimarySubject;
                    return pSubject.IsPerRisk;
                }
                else
                    return false;        
            } 
        }

        public bool IsExploded { get; set; }


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
            CurrentAllocationState = new AllocationStateCollection2(1);

            //CurrentAggState = new AggState(Cover.AggDedAmount, Cover.AggLimitAmount, Cover.DedOfAggDedLayer.Amount, Cover.LimitOfAggDedLayer.Amount);
            CurrentAggState = new AggState();
            keystring = keystring + "-Cov: " + _coverName;
        }

        public CoverNode(Subject _subject, string _coverName, CoverNode cNode)
            : base(_subject)
        {
            Payout = 0;
            CoverName = _coverName;
            CurrentAllocationState = new AllocationStateCollection2(1);

            //CurrentAggState = new AggState(Cover.AggDedAmount, Cover.AggLimitAmount, Cover.DedOfAggDedLayer.Amount, Cover.LimitOfAggDedLayer.Amount);
            CurrentAggState = new AggState();
            keystring = keystring + "-Cov: " + _coverName;
            //Cover = cNode.Cover;
            Cover = new Cover(cNode.Cover.CoverName, cNode.Cover.AttIsFranchise, cNode.Cover.AttPoint, cNode.Cover.Limit, cNode.Cover.ProRata, cNode.Cover.Unlimited, cNode.Cover.AttachmentTimeBasis,
                               cNode.Cover.LimitTimeBasis, cNode.Cover.LimitValType, cNode.Cover.NumofReinstatements, cNode.Cover.UnlimitedReinstatements);            
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

        public void SetAllocState(AllocationStateCollection2 allocState)
        {
            CurrentAllocationState = allocState;
        }

        public LossStateCollection2 GetLossState()
        {
            LossStateCollection2 coverNodeLossState = new LossStateCollection2(1);         
            return coverNodeLossState;
        }

        public AllocationStateCollection2 GetAllocState()
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

        public AggState(AggState otherAggState)
        {
            this.AggDedAmount = otherAggState.AggDedAmount;
            this.AggLimitAmount = otherAggState.AggLimitAmount;
            this.AggPayout = otherAggState.AggPayout;
            this.DedFilled = otherAggState.DedFilled;
            this.DedAmountOfAggDedLayer = otherAggState.DedAmountOfAggDedLayer;
            this.IsUnlimited = otherAggState.IsUnlimited;
            this.LimitAmountOfAggDedLayer = otherAggState.LimitAmountOfAggDedLayer;
            this.RemainingLoss = otherAggState.RemainingLoss;
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
                Update(dedToAgg, 0);
                return payout;
            }
            else
                return incomingLoss;        
        }

        public double ApplyAggLimit(double currPayout)
        {
            if (!IsAggLimitFilled && AggPayout < AggLimitAmount)
            {
                double diff = AggLimitAmount - AggPayout;
                currPayout = Math.Min(currPayout, diff);                
                //RemainingLoss += diff;
                //currPayout = currPayout - diff;
                AggPayout += currPayout;
            }
            else if (IsAggLimitFilled)
            {
                RemainingLoss += currPayout;
                currPayout = 0;
            }
            return currPayout;
        }

        public void Update(double _ded, double _aggPayout)
        {
            this.DedFilled += _ded;            
            this.AggPayout += _aggPayout;            
            //this.RemainingLoss = _rLosss;
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
