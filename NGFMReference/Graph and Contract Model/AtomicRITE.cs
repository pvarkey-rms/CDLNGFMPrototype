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
        protected AllocationStateCollection CurrentAllocationStateCollection;
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
        public abstract void SetAllocState(AllocationStateCollection state);
        public abstract AllocationStateCollection GetAllocState();
        public abstract LossStateCollection GetLossState();
    }

    public class CoverageAtomicRITE : AtomicRITE, IEquatable<CoverageAtomicRITE>
    {
        private LossTimeSeries originalSubjectLoss;
        public RITE RITE { get; private set; }
        public long RITCharacterisiticID { get; private set; }
        public uint TimeStamp { get { return subjectLoss.TimeStamp; } }
        private bool OriginalLossSet = false;

       //private AllocationState CurrentAllocationStateSummed;
        private LossStateCollection CurrentLossStateCollection;

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
            CurrentAllocationStateCollection = new AllocationStateCollection(RITE.ActNumOfBldgs);
            //CurrentAllocationStateSummed = new AllocationState();
            CurrentLossStateCollection = new LossStateCollection(RITE.ActNumOfBldgs);
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

        public override void SetAllocState(AllocationStateCollection allocstate)
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

        //public void SetAllocStateSummed(AllocationState allocstate)
        //{
        //    CurrentAllocationStateSummed = allocstate;
        //}

        public override AllocationStateCollection GetAllocState()
        {
            return CurrentAllocationStateCollection;
        }

        //public AllocationState GetAllocationStateSummed()
        //{
        //    return CurrentAllocationStateSummed;
        //}

        public override LossStateCollection GetLossState()
        {
            return CurrentLossStateCollection;
        }
    }

    public class ContractAtomicRITE : AtomicRITE, IEquatable<ContractAtomicRITE>
    {
        public Graph contractGraph{ get; private set; }

        public ContractAtomicRITE(Graph _contractGraph, string _subPeril, ExposureType _expType)
        {
            contractGraph = _contractGraph;
            SubPeril = _subPeril;
            ExpType = _expType;

            CurrentAllocationStateCollection = new AllocationStateCollection(1);

            if (_contractGraph.IsExecuted)
                subjectLoss = _contractGraph.exResults.GetFilteredTimeSeries(_subPeril, _expType);
        }

        public override LossTimeSeries OriginalSubjectLoss
        {
            get
            {
                return contractGraph.exResults.GetFilteredTimeSeries(SubPeril, ExpType);
            }
        }

        public override void SetSubjectLoss(LossTimeSeries _subjectLoss)
        {
            subjectLoss = _subjectLoss;
        }

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

        //////////Override IAllocatable
        public override bool AllocateByRecoverable { get { return false; } }
        public override bool AllocateRecoverableFirst { get { return false; } }

        public override void SetAllocState(AllocationStateCollection allocState)
        {
            double ratio = allocState.Loss / subjectLoss.TotalLoss;
            AllocatedLossSeries = subjectLoss;
            AllocatedLossSeries.AllocateRatio(ratio);
        }

        public override AllocationStateCollection GetAllocState()
        {
            return CurrentAllocationStateCollection;
        }

        public override LossStateCollection GetLossState()
        {
            LossStateCollection lossCol = new LossStateCollection(1);
            lossCol.SetSubjectLosses(subjectLoss.AllLoss());
            lossCol.SetRecoverableLosses(subjectLoss.AllLoss());

            return lossCol;
        }

    }


    public class LossTimeSeries : IEnumerable<TimeLoss>
    {
        private Dictionary<uint, double[]> timeseries;
        private int NumOfBldgs = 1;

        public double TotalLoss
        {
            get { return timeseries.Values.Select(array => array.Sum()).Sum(); }
        }

        public uint TimeStamp
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
            timeseries = new Dictionary<uint, double[]>();
            NumOfBldgs = _numOfBldgs;
        }
        
        public LossTimeSeries(uint timestamp, double loss)
        {
            timeseries = new Dictionary<uint, double[]>() {{timestamp, new double[] {loss}}};
            NumOfBldgs = 1;
        }

        public LossTimeSeries(uint timestamp, double[] loss)
        {
            timeseries = new Dictionary<uint, double[]>() { { timestamp, loss } };
            NumOfBldgs = loss.Count();
        }

        public void AddLoss(uint timestamp, double loss)
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

        public void AddLoss(uint timestamp, double[] loss, Aggregation aggType = Aggregation.Summed)
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

            foreach (KeyValuePair<uint, double[]> timeloss in otherLoss.timeseries)
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

            foreach (KeyValuePair<uint, double[]> timeloss in this.timeseries)
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
            foreach (KeyValuePair<uint, double[]> pair in timeseries)
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
        public uint Time { get; set; }
        public double Loss { get; set; }

        public TimeLoss(uint _time, double _loss)
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
