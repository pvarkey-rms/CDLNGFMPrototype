using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class ReferenceResultOutput
    {
        public double TotalPayout { get; protected set; }
        public double TotalSubjectLoss { get; protected set; }
        public DateTime EarliestEventForContract { get; protected set; }

        private QuerryableLossOutput SubjectLosses;
        private QuerryableLossOutput AllocatedPayoutLosses;

        public LossTimeSeries SubjectLossTimeSeries
        { get { return SubjectLosses.TotalTimeSeries; } }
        public LossTimeSeries PayoutTimeSeries
        { get { return AllocatedPayoutLosses.TotalTimeSeries; } }

        public ReferenceResultOutput(double _payout, double _subjectLoss)
        {
            TotalPayout = _payout;
            TotalSubjectLoss = _subjectLoss;

            SubjectLosses = new QuerryableLossOutput();
            AllocatedPayoutLosses = new QuerryableLossOutput();
        }

        public ReferenceResultOutput(double _payout, double _subjectLoss, HashSet<AtomicRITE> rites)
            :this(_payout, _subjectLoss)
        {
            List<AtomicLoss> atomicPayoutLosses  = GetAllocatedPayoutsFromARITEs(rites);
            List<AtomicLoss> atomicSubjectLosses = GetSubjectLossesFromARITEs(rites);

            AllocatedPayoutLosses = new QuerryableLossOutput(atomicPayoutLosses);
            SubjectLosses = new QuerryableLossOutput(atomicSubjectLosses);
        }

        public ReferenceResultOutput(double _payout, double _subjectLoss, List<AtomicLoss> atomicPayoutLosses, DateTime _earliestEventDate)
            : this(_payout, _subjectLoss)
        {
            AllocatedPayoutLosses = new QuerryableLossOutput(atomicPayoutLosses);
            EarliestEventForContract = _earliestEventDate;
        }

        public ReferenceResultOutput(double _payout, double _subjectLoss, QuerryableLossOutput _subjectLosses, QuerryableLossOutput _allocatedPayoutLosses)
            : this(_payout, _subjectLoss)
        {
            SubjectLosses = _subjectLosses;
            AllocatedPayoutLosses = _allocatedPayoutLosses;
        }


        public LossTimeSeries GetFilteredTimeSeries(string subperil, ExposureType exType)
        {
            return AllocatedPayoutLosses.GetFilteredTimeSeries(subperil, exType);
        }

        public static ReferenceResultOutput operator +(ReferenceResultOutput exOutput1, ReferenceResultOutput exOutput2)
        {
            double payout = exOutput1.TotalPayout + exOutput2.TotalPayout;
            double SubjectLoss = exOutput1.TotalSubjectLoss + exOutput2.TotalSubjectLoss;

            QuerryableLossOutput _SubjectLosses = exOutput1.SubjectLosses + exOutput2.SubjectLosses;
            QuerryableLossOutput _AllocatedPayoutLosses = exOutput1.AllocatedPayoutLosses + exOutput2.AllocatedPayoutLosses;

            return new ReferenceResultOutput(payout, SubjectLoss, _SubjectLosses, _AllocatedPayoutLosses);
        }

        private List<AtomicLoss> GetAllocatedPayoutsFromARITEs(HashSet<AtomicRITE> rites)
        {
            List<AtomicLoss> AllocatedPayoutLosses = new List<AtomicLoss>();
            foreach (AtomicRITE aRITE in rites)
            {
                aRITE.SetAllocState(aRITE.GetAllocState());
                foreach (TimeLoss timeloss in aRITE.AllocatedLossSeries)
                    if (timeloss.Loss > 0)
                    {                    
                        AtomicLoss allocLoss = new AtomicLoss();
                        allocLoss.ExpType = aRITE.ExpType;
                        allocLoss.Subperil = aRITE.SubPeril;
                        allocLoss.Loss = timeloss.Loss;
                        allocLoss.timestamp = timeloss.Time;
                        AllocatedPayoutLosses.Add(allocLoss);

                        //Remove when architecture for allcoation finalized !!!!!!!!!
                        if (aRITE is CoverageAtomicRITE)
                            allocLoss.ExposureID = (aRITE as CoverageAtomicRITE).RITCharacterisiticID;
                        else
                            allocLoss.ExposureID = (aRITE as ContractAtomicRITE).contractGraph.Graph.ContractID;
                        //Remove above ////////////////////////////////////////////////////
                    }
            }

            return AllocatedPayoutLosses;
        }

        private List<AtomicLoss> GetSubjectLossesFromARITEs(HashSet<AtomicRITE> rites)
        {
            List<AtomicLoss> SubjectLosses = new List<AtomicLoss>();
            foreach (AtomicRITE aRITE in rites)
            {
                foreach (TimeLoss timeloss in aRITE.SubjectLoss)
                    if (timeloss.Loss > 0)
                    {
                        AtomicLoss subjectLoss = new AtomicLoss();
                        subjectLoss.ExpType = aRITE.ExpType;
                        subjectLoss.Subperil = aRITE.SubPeril;
                        subjectLoss.Loss = timeloss.Loss;
                        subjectLoss.timestamp = timeloss.Time;
                        SubjectLosses.Add(subjectLoss);

                        //Remove when architecture for allcoation finalized !!!!!!!!!
                        if (aRITE is CoverageAtomicRITE)
                            subjectLoss.ExposureID = (aRITE as CoverageAtomicRITE).RITCharacterisiticID;
                        else
                            subjectLoss.ExposureID = (aRITE as ContractAtomicRITE).contractGraph.Graph.ContractID;
                        //Remove above ////////////////////////////////////////////////////
                    }
            }

            return SubjectLosses;
        }

        public void SetSubjectLoss(HashSet<AtomicRITE> rites)
        {
            SubjectLosses = new QuerryableLossOutput(GetSubjectLossesFromARITEs(rites));
        }

        public void SetSubjectLoss(List<AtomicLoss> subjectLosses)
        {
            SubjectLosses = new QuerryableLossOutput(subjectLosses);
        }

        public void SetSubjectLoss(QuerryableLossOutput subjectLosses)
        {
            SubjectLosses = subjectLosses;
        }
    }

    public class AtomicLoss
    {
        public RITE RITE { get; set; }
        public ExposureType ExpType { get; set; }
        public string Subperil { get; set; }
        public long ExposureID { get; set; }
        public DateTime timestamp { get; set; }
        public double Loss { get; set; }
    }

    public class QuerryableLossOutput
    {
        public List<AtomicLoss> AtomicLosses { get; protected set; }
        private LossTimeSeries totalTimeSeries;
        public LossTimeSeries TotalTimeSeries
        {
            get
            {
                return GetTotalTimeSeries();
            }
        }

        public QuerryableLossOutput(List<AtomicLoss> losses)
        {
            AtomicLosses = losses;
        }

        public QuerryableLossOutput()
        {
            AtomicLosses = new List<AtomicLoss>();
        }

        private LossTimeSeries GetTotalTimeSeries()
        {
            var timeGroups =
                from allocLoss in AtomicLosses
                group allocLoss by allocLoss.timestamp into g
                select new { Time = g.Key, Payouts = g };

            LossTimeSeries series = new LossTimeSeries(1);

            foreach (var g in timeGroups)
            {
                double totalpayout = g.Payouts.Select(payout => payout.Loss).Sum();
                series.AddLoss(g.Time, totalpayout);
            }

            return series;
        }

        public LossTimeSeries GetFilteredTimeSeries(string subperil, ExposureType exType)
        {
            var timeGroups =
                from allocLoss in AtomicLosses
                group allocLoss by allocLoss.timestamp into g
                select new { Time = g.Key, Payouts = g };

            LossTimeSeries series = new LossTimeSeries(1);

            foreach (var g in timeGroups)
            {
                double totalpayout = g.Payouts.Where(payout => payout.Subperil == subperil && payout.ExpType == exType)
                                                .Select(payout => payout.Loss)
                                                .Sum();
                series.AddLoss(g.Time, totalpayout);
            }

            return series;
        }

        public static QuerryableLossOutput operator +(QuerryableLossOutput exOutput1, QuerryableLossOutput exOutput2)
        {
            List<AtomicLoss> allocatedLosses = exOutput1.AtomicLosses;

            foreach (AtomicLoss allocLoss in exOutput2.AtomicLosses)
            {
                AtomicLoss sameLoss = allocatedLosses.Where(aLoss => aLoss.ExposureID == allocLoss.ExposureID)
                                                        .FirstOrDefault();

                if (sameLoss != null)
                    sameLoss.Loss += allocLoss.Loss;
                else
                    allocatedLosses.Add(allocLoss);
            }

            return new QuerryableLossOutput(allocatedLosses);
        }

    }
    
}
