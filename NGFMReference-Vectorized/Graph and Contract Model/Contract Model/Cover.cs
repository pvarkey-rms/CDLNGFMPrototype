using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFMReference.ContractModel;

namespace NGFMReference
{
    public class Cover
    {
        //Might be deleted sometime in future, works for now //////////////////////////////////
        public bool AttIsFranchise { get; private set; }
        public bool Unlimited { get; private set; }
        public Value AttPoint { get; private set; }
        public Value Limit { get; set; }
        public TimeBasis LimitTimeBasis { get; private set; }
        public TimeBasis AttachmentTimeBasis { get; private set; }
        public double LimitAmount { get { return Limit.Amount; } }
        public double AttPointAmount { get { return AttPoint.Amount; } }
        public Value DedOfAggDedLayer { get { return new MonetaryValue(0.0); } }
        public Value LimitOfAggDedLayer { get { return new MonetaryValue(0.0); } }
        // Might be deleted sometime in future, works for now /////////////////////////////////////

        //Maybe will utilize this in future, not helpful for parsing right now...
        //public AttachmentPoint AttPoint { get; private set; }
        //public CoverLimit Limit { get; private set; }

        //public bool LimitIsPay { get; private set; }
        public TermValueType LimitValType { get; private set;}
        public uint NumofReinstatements { get; private set; }
        public bool UnlimitedReinstatements { get; private set; }
  
        public PercentValue ProRata { get; private set; }
        public String CoverName { get; private set; }


        public Cover(string name, bool _attIsFranchise, Value _attPoint, Value _limit, PercentValue _proRata, 
                     bool _unlimited, TimeBasis _attachmentTimeBasis, TimeBasis _limitTimeBasis, TermValueType _valType,
                        uint _numofReinstatements, bool _unlimitedReinstatements)
        {
            CoverName = name;
            AttIsFranchise = _attIsFranchise;
            AttPoint = _attPoint;
            Limit = _limit;
            ProRata = _proRata;
            Unlimited = _unlimited;
            LimitTimeBasis = _limitTimeBasis;
            AttachmentTimeBasis = _attachmentTimeBasis;
            LimitValType = _valType;
            NumofReinstatements = _numofReinstatements;
            UnlimitedReinstatements = _unlimitedReinstatements;
        }

        public void SetFinTerms(string name, bool _attIsFranchise, Value _attPoint, 
                                Value _limit, PercentValue _proRata, bool _unlimited, TermValueType _valType,
                                uint _numofReinstatements, bool _unlimitedReinstatements)
        {
            CoverName = name;
            AttIsFranchise = _attIsFranchise;
            AttPoint = _attPoint;
            Limit = _limit;
            ProRata = _proRata;
            Unlimited = _unlimited;
            LimitValType = _valType;
            NumofReinstatements = _numofReinstatements;
            UnlimitedReinstatements = _unlimitedReinstatements;
        }
    }

    public class AttachmentPoint
    {
        public bool AttIsFranchise { get; private set; }
        public Value Value { get; private set; }
        public double Amount { get { return Value.Amount; } }
        public TimeBasis TimeBasis { get; private set; } 
    }

    public class CoverLimit
    {
        public Value Value { get; private set; }
        public bool Unlimited { get; private set; }
        public TimeBasis TimeBasis { get; private set; } 
    }

    public enum TimeBasis
    {
        Aggregate,
        Occurrence
    }

}
