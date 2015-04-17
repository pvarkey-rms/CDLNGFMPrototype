using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public abstract class AtomicRITE : IAllocatable
    {
        protected static int count = 0;
        public abstract LossTimeSeries OriginalSubjectLoss { get; }
        protected AllocationStateCollection2 CurrentAllocationStateCollection;
        public string SubPeril { get; protected set; }
        public ExposureType ExpType { get; protected set; }
        protected LossTimeSeries subjectLoss;
        public LossTimeSeries SubjectLoss { get { return subjectLoss; } }
        public LossTimeSeries AllocatedLossSeries { get; protected set; }    

        public abstract void SetSubjectLoss(LossTimeSeries _subjectLoss);

        public virtual void Reset()
        {
            CurrentAllocationStateCollection.Reset();
        }

        //override IAllocatable
        public abstract bool AllocateByRecoverable {get;}
        public abstract bool AllocateRecoverableFirst { get; }
        public void SetAllocState(AllocationStateCollection2 allocstate)
        {
            CurrentAllocationStateCollection = allocstate;
            AllocatedLossSeries = subjectLoss.Clone();
            double ratio;
            if (subjectLoss.TotalLoss > 0)
            {
                ratio = allocstate.GetPayout() / subjectLoss.TotalLoss;
                AllocatedLossSeries.AllocateRatio(ratio);
            }
        }
        public abstract AllocationStateCollection2 GetAllocState();
        public abstract LossStateCollection2 GetLossState();
        //public abstract float GetFactor();
        //public abstract int GetFactorIndex();
    }

    public class CoverageAtomicRITE : AtomicRITE, IEquatable<CoverageAtomicRITE>
    {
        private LossTimeSeries originalSubjectLoss;
        public RITE RITE { get; private set; }
        public long RITCharacterisiticID { get; private set; }
        public DateTime TimeStamp { get { return subjectLoss.TimeStamp; } }
        private bool OriginalLossSet = false;

       //private AllocationState CurrentAllocationStateSummed;
        private LossStateCollection2 CurrentLossStateCollection;
        //public override float GetFactor()
        //{
        //    return RITE.Factor; //TODO: based on algorithm
        //}

        //public override int GetFactorIndex()
        //{
        //    return RITE.FactorIndex; //TODO: based on algorithm
        //}

        public override LossTimeSeries OriginalSubjectLoss
        {
            get 
            { 
                return originalSubjectLoss; 
            }
        }

        public CoverageAtomicRITE(string _subperil, ExposureType _expType, RITE _rite, long ID)
        {
            SubPeril = _subperil;
            ExpType= _expType;
            RITE = _rite;
            RITCharacterisiticID = ID;
            CurrentAllocationStateCollection = new AllocationStateCollection2(RITE.ActNumOfSampleBldgs);
            //CurrentAllocationStateSummed = new AllocationState();
            CurrentLossStateCollection = new LossStateCollection2(RITE.ActNumOfSampleBldgs);
        }

        public override void SetSubjectLoss(LossTimeSeries _subjectLoss)
        {
            if (OriginalLossSet == false)
            {
                originalSubjectLoss = _subjectLoss;
                OriginalLossSet = true;
            }
            else
            {
                subjectLoss = _subjectLoss;
                CurrentLossStateCollection.SetSubjectLosses(subjectLoss.AllLoss());
                CurrentLossStateCollection.SetRecoverableLosses(subjectLoss.AllLoss());
            }
        }

        public override void Reset()
        {
            base.Reset();
            CurrentLossStateCollection.Reset();
            OriginalLossSet = false;
        }
  
        //////////Override IEquatable
        public bool Equals(CoverageAtomicRITE other)
        {
            if (other == null)
                return false;

            if (this.SubPeril == other.SubPeril &
                this.ExpType.Equals(other.ExpType) &
                this.RITCharacterisiticID == other.RITCharacterisiticID &
                this.RITE.Equals(other.RITE))
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            CoverageAtomicRITE riteObj = obj as CoverageAtomicRITE;
            if (riteObj == null)
                return false;
            else
                return Equals(riteObj);
        }

        public override int GetHashCode()
        {
            return SubPeril.GetHashCode() ^ ExpType.GetHashCode() ^ (int)RITCharacterisiticID ^ RITE.GetHashCode();
        }

        //////////Override IAllocatable
        public override bool AllocateByRecoverable { get { return true; } }
        public override bool AllocateRecoverableFirst { get { return true; } }

        //public override void SetAllocState(AllocationStateCollection2 allocstate)
        //{
        //    CurrentAllocationStateCollection = allocstate;
        //    AllocatedLossSeries = subjectLoss.Clone();
        //    double ratio;
        //    if (subjectLoss.TotalLoss > 0)
        //    {
        //        ratio = allocstate.GetPayout() / subjectLoss.TotalLoss;
        //        AllocatedLossSeries.AllocateRatio(ratio);
        //    }          
        //}

        //public void SetAllocStateSummed(AllocationState allocstate)
        //{
        //    CurrentAllocationStateSummed = allocstate;
        //}

        public override AllocationStateCollection2 GetAllocState()
        {
            return CurrentAllocationStateCollection;
        }

        //public AllocationState GetAllocationStateSummed()
        //{
        //    return CurrentAllocationStateSummed;
        //}

        public override LossStateCollection2 GetLossState()
        {
            return CurrentLossStateCollection;
        }        
    }

    public class ContractAtomicRITE : AtomicRITE, IEquatable<ContractAtomicRITE>
    {
        public GraphInfo contractGraph { get; private set; }
        public PositionType positionType { get; private set; }

        public ContractAtomicRITE(GraphInfo _contractGraph, string _subPeril, ExposureType _expType, PositionType _positionType)
        {
            contractGraph = _contractGraph;
            SubPeril = _subPeril;
            ExpType = _expType;
            positionType = _positionType;

            CurrentAllocationStateCollection = new AllocationStateCollection2(1);

            if (_contractGraph.Graph.IsExecuted)
                subjectLoss = _contractGraph.Graph.exResults.GetFilteredTimeSeries(_subPeril, _expType);
        }

        public override LossTimeSeries OriginalSubjectLoss
        {
            get
            {
                return contractGraph.Graph.exResults.GetFilteredTimeSeries(SubPeril, ExpType);
            }
        }

        public override void SetSubjectLoss(LossTimeSeries _subjectLoss)
        {
            subjectLoss = _subjectLoss;
        }

        //public override float GetFactor()
        //{
        //    return 3; //TODO: no need for now
        //}

        //public override int GetFactorIndex()
        //{
        //    return 0; //TODO: not used now
        //}
        //public override LossTimeSeries OriginalSubjectLoss
        //{
        //    get
        //    {
        //        if (subjectLoss != null)
        //            return subjectLoss;
        //        else
        //            return subjectLoss = contractGraph.exResults.GetFilteredTimeSeries(SubPeril, ExpType);
        //    }
        //}

        //////////Override IEquatable
        public bool Equals(ContractAtomicRITE other)
        {
            if (other == null)
                return false;

            if (this.SubPeril == other.SubPeril &
                this.ExpType.Equals(other.ExpType) & 
                this.contractGraph == other.contractGraph)
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            ContractAtomicRITE riteObj = obj as ContractAtomicRITE;
            if (riteObj == null)
                return false;
            else
                return Equals(riteObj);
        }

        public override int GetHashCode()
        {
            return SubPeril.GetHashCode() ^ ExpType.GetHashCode() ^ contractGraph.GetHashCode();
        }

        public override void Reset()
        {
            base.Reset();
            contractGraph.Graph.EventReset();
        }

        //////////Override IAllocatable
        public override bool AllocateByRecoverable { get { return false; } }
        public override bool AllocateRecoverableFirst { get { return false; } }

        //public override void SetAllocState(AllocationStateCollection2 allocState)
        //{
        //    CurrentAllocationStateCollection = allocState;
        //    AllocatedLossSeries = subjectLoss.Clone();
        //    double ratio;
        //    if (subjectLoss.TotalLoss > 0)
        //    {
        //        ratio = allocState.GetPayout() / subjectLoss.TotalLoss;
        //        AllocatedLossSeries.AllocateRatio(ratio);
        //    } 
        //}

        public override AllocationStateCollection2 GetAllocState()
        {
            return CurrentAllocationStateCollection;
        }

        public override LossStateCollection2 GetLossState()
        {
            LossStateCollection2 lossCol = new LossStateCollection2(1);
            double[] allSubjectLosses;
            
            //Handle Net or Gross postions
            if (positionType == PositionType.Gross)
                allSubjectLosses = subjectLoss.AllLoss();
            else if (positionType == PositionType.Ceded)
                allSubjectLosses = subjectLoss.AllLoss().Select(loss => loss * -1).ToArray();
            else
                throw new NotSupportedException("Cannot handle positionType: " + positionType.ToString());

            lossCol.SetSubjectLosses(subjectLoss.AllLoss());
            lossCol.SetRecoverableLosses(subjectLoss.AllLoss());

            return lossCol;
        }

    }

    public enum PositionType
    {
        Gross,
        Ceded,
        Net
    }

    public class LossTimeSeries : IEnumerable<TimeLoss>
    {
        private Dictionary<DateTime, double[]> timeseries;
        private int NumOfBldgs = 1;

        public double TotalLoss
        {
            get { return timeseries.Values.Select(array => array.Sum()).Sum(); }
        }

        public DateTime TimeStamp
        {
            get
            {
                if (timeseries.Count == 1)
                    return timeseries.Keys.First();
                else
                    throw new InvalidOperationException("Cannot get timestamp for Loss Series with more than one loss");
            }
        }

        public LossTimeSeries(int _numOfBldgs)
        {
            timeseries = new Dictionary<DateTime, double[]>();
            NumOfBldgs = _numOfBldgs;
        }

        public LossTimeSeries(DateTime timestamp, double loss)
        {
            timeseries = new Dictionary<DateTime, double[]>() { { timestamp, new double[] { loss } } };
            NumOfBldgs = 1;
        }

        public LossTimeSeries(DateTime timestamp, double[] loss)
        {
            timeseries = new Dictionary<DateTime, double[]>() { { timestamp, loss } };
            NumOfBldgs = loss.Count();
        }

        public void AddLoss(DateTime timestamp, double loss)
        {
            if (NumOfBldgs != 1)
                throw new InvalidOperationException("Cannot losses to array with Num of buildings greater than 1");
            if (timeseries.ContainsKey(timestamp))
            {
                timeseries[timestamp] = new double[] { timeseries[timestamp][0] + loss };
            }
            else
                timeseries.Add(timestamp, new double[] {loss});
        }

        public void AddLoss(DateTime timestamp, double[] loss, Aggregation aggType = Aggregation.Summed)
        {         
            if (timeseries.ContainsKey(timestamp))
            {
                if (aggType == Aggregation.Summed)
                    timeseries[timestamp] = new double[] { timeseries[timestamp].Sum() + loss.Sum() } ;
                else
                {
                    if (loss.Count() == timeseries[timestamp].Count())
                        timeseries[timestamp] = timeseries[timestamp].Zip(loss, (x, y) => x + y).ToArray();
                    else
                        throw new ArgumentOutOfRangeException("Input loss array is of wrong size for aggregation per building");
                }
            }
            else
            {
                if (aggType == Aggregation.Summed)
                    timeseries.Add(timestamp, new double [] {loss.Sum()}); 
                else 
                    timeseries.Add(timestamp, loss);
            }
        }
     
        public void MergeTimeSeries(LossTimeSeries otherLoss, Aggregation aggType = Aggregation.Summed)
        {
            if (NumOfBldgs != otherLoss.NumOfBldgs && aggType == Aggregation.PerBuilding)
                throw new InvalidOperationException("Cannot merge time series with losses of different number of buildings");

            foreach (KeyValuePair<DateTime, double[]> timeloss in otherLoss.timeseries)
            {
                this.AddLoss(timeloss.Key, timeloss.Value, aggType);
            }
        }

        public void AllocateRatio(double ratio)
        {
            timeseries = timeseries.ToDictionary(elem => elem.Key,
                                                elem => elem.Value.Select(loss => loss * ratio).ToArray());
        }

        //public static LossTimeSeries operator +(LossTimeSeries loss1, LossTimeSeries loss2)
        //{
        //    LossTimeSeries outputseries = new LossTimeSeries();
        //    Dictionary<int, double> loss2timeseries = new Dictionary<int, double>(loss2.timeseries);

        //    foreach (KeyValuePair<int, double> timeloss1 in loss1.timeseries)
        //    {
        //        if (loss2timeseries.ContainsKey(timeloss1.Key))
        //        {
        //            outputseries.AddLoss(timeloss1.Key, timeloss1.Value + loss2timeseries[timeloss1.Key]);
        //            loss2timeseries.Remove(timeloss1.Key);
        //        }
        //        else
        //            outputseries.AddLoss(timeloss1.Key, timeloss1.Value);
        //    }

        //    foreach (KeyValuePair<int, double> timeloss2 in loss2timeseries)
        //    {
        //        outputseries.AddLoss(timeloss2.Key, timeloss2.Value);
        //    }

        //    return outputseries;
        //}

        public double[] AllLoss()
        {
            double[] buildingLoss = new double[NumOfBldgs];
            
            foreach (double[] array in timeseries.Values)
            {
                buildingLoss = buildingLoss.Zip(array, (x, y) => x + y).ToArray();
            }
            return buildingLoss;
        }

        public LossTimeSeries ApplyWindow(TimeWindow window)
        {
            LossTimeSeries series = new LossTimeSeries(this.NumOfBldgs);

            foreach (KeyValuePair<DateTime, double[]> timeloss in this.timeseries)
            {
                if ((timeloss.Key >= window.start && timeloss.Key <= window.end) || window.IsUnrestricted)
                {
                    series.AddLoss(timeloss.Key, timeloss.Value, Aggregation.PerBuilding);
                }               
            }

            return series;
        }

        public IEnumerator<TimeLoss> GetEnumerator()
        {
            foreach (KeyValuePair<DateTime, double[]> pair in timeseries)
            {
                yield return new TimeLoss(pair.Key, pair.Value.Sum());
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public LossTimeSeries Clone()
        {
            LossTimeSeries series = new LossTimeSeries(this.NumOfBldgs);
            series.timeseries = this.timeseries;

            return series;
        }
    }

    public class TimeLoss
    {
        public DateTime Time { get; set; }
        public double Loss { get; set; }

        public TimeLoss(DateTime _time, double _loss)
        {
            Time = _time;
            Loss = _loss;
        }
    }

    public enum Aggregation
    {
        Summed,
        PerBuilding
    }
}
