using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public abstract class Subject : IEquatable<Subject>
    {
        protected static int TotalNumSubjects = 0;

        protected ExposureTypeCollection exposureTypes;
        protected COLCollection causeOfLossSet;

        public ExposureTypeCollection ExposureTypes
        { get { return exposureTypes; } }
        public COLCollection CauseOfLossSet
        { get { return causeOfLossSet; } }
        public abstract bool IsLocCvg {get; }
        public int ID { protected set; get; }

        protected void SetID()
        {
            TotalNumSubjects++;
            ID = TotalNumSubjects;      
        }

        public Subject()
        {
            SetID();
        }

        public Boolean IsDerived { get; set; }
        public List<String> ChildrenCoverNodeList { get; set; }

        public abstract HashSet<AtomicRITE> GetAtomicRites();

        public virtual bool IsLargerThan(Subject other)
        {
            if (IsDerived || other.IsDerived)
                throw new InvalidOperationException("This method cannot compare derivied subjects");
            
            if (this.CauseOfLossSet.LargerThan(other.CauseOfLossSet) &&
                    this.ExposureTypes.LargerThan(other.ExposureTypes, IsLocCvg))
                return true;
            else
                return false;
        }

        public virtual bool IsLargerThanOrEquals(Subject other)
        {
            if (IsDerived || other.IsDerived)
                throw new InvalidOperationException("This method cannot compare derivied subjects");

            if (this.CauseOfLossSet.LargerOrEqualThan(other.CauseOfLossSet) &&
                    this.ExposureTypes.LargerOrEqualThan(other.ExposureTypes, false))
                return true;
            else
                return false;
        }

        public virtual bool Equals(Subject other)
        {
            if (other == null)
                return false;

            if (IsDerived)
            {
                return Enumerable.SequenceEqual(ChildrenCoverNodeList.OrderBy(t => t), other.ChildrenCoverNodeList.OrderBy(t => t));
            }
            else
            {
                if (this.exposureTypes.Equals(other.exposureTypes) &
                            this.CauseOfLossSet.Equals(other.CauseOfLossSet))
                    return true;
                else
                    return false;
            }

        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            Subject subjectObj = obj as Subject;
            if (subjectObj == null)
                return false;
            else
                return Equals(subjectObj);
        }

        public override int GetHashCode()
        {
            if (IsDerived)
            {
                int code = 0;
                foreach (string s in ChildrenCoverNodeList)
                {
                    code = code + s.GetHashCode() * 31;
                }
                return code;
            }
            else
                return CauseOfLossSet.GetHashCode() ^ exposureTypes.GetHashCode();
        }

        public override string ToString()
        {
            if (IsDerived)
                return String.Join(",", ChildrenCoverNodeList);
            else
                return CauseOfLossSet.ToString() + "; " + exposureTypes.ToString();
        }

    }

    public class PrimarySubject: Subject, IEquatable<PrimarySubject>
    {
        private ScheduleOfRITEs schedule;
        private bool isLocCvg;
        public ScheduleOfRITEs Schedule
        {get{ return schedule;}}
        public bool IsPerRisk {get; set;}

        public int Size
        {
            get { return exposureTypes.Count + causeOfLossSet.Count + schedule.ScheduleList.Count; }
        }
        public override bool IsLocCvg { get { return isLocCvg; } }

        public PrimarySubject(List<string> _childrenCoverNodeList):base()
        {
            IsDerived = true;
            isLocCvg = false;
            ChildrenCoverNodeList = _childrenCoverNodeList;
        }

        public PrimarySubject(ScheduleOfRITEs _schedule, ExposureTypeCollection _exposureTypes, COLCollection _causeOfLossSet, bool _isPerRisk = false):base()
        {
            schedule = _schedule;
            exposureTypes = _exposureTypes;
            causeOfLossSet = _causeOfLossSet;
            if (_schedule != null && _schedule.IsLocation && _exposureTypes.Count == 1)
                isLocCvg = true;
            else
                isLocCvg = false;

            IsPerRisk = _isPerRisk;

        }

        public bool Equals(PrimarySubject other)
        {
            if (IsDerived)
            {
                return Enumerable.SequenceEqual(ChildrenCoverNodeList.OrderBy(t => t), other.ChildrenCoverNodeList.OrderBy(t => t));
            }
            else
            {
                if (this.schedule.ScheduleListEquals((other as PrimarySubject).schedule) &
                    base.Equals(other))
                    return true;
                else
                    return false;
            }
        }

        public override bool Equals(Subject other)
        {
            if (other == null)
                return false;

            if (other is PrimarySubject)
            {
                return Equals(other as PrimarySubject);
            }
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            PrimarySubject subjectObj = obj as PrimarySubject;
            if (subjectObj == null)
                return false;
            else
                return Equals(subjectObj);
        }

        public override int GetHashCode()
        {
            if (IsDerived)
            {
                int code = 0;
                foreach (string s in ChildrenCoverNodeList)
                {
                    code = code + s.GetHashCode() * 31;
                }
                return code;
            }
            else
            {
                return schedule.GetScheduleListHashCode() ^ base.GetHashCode();
            }
        }

        public override string ToString()
        {
            if(IsDerived)
                return base.ToString();
            else
                return schedule.Name + "; " + base.ToString();          
        }

        public override HashSet<AtomicRITE> GetAtomicRites()
        {
            if (!IsDerived)
            {
                HashSet<CoverageAtomicRITE> ARITEs = new HashSet<CoverageAtomicRITE>();

                foreach (ExposureType expType in ExposureTypes)
                {
                    var RITChars = Schedule.RITChars.Where(RitChar => RitChar.ExpType == expType || ExposureTypeCollection.GetMappedType(RitChar.ExpType) == expType);
                    foreach (RITCharacteristic RITChar in RITChars)
                    {
                        foreach (string subperil in CauseOfLossSet.GetSubperils())
                        {
                            ARITEs.Add(new CoverageAtomicRITE(subperil, ExposureTypeCollection.GetMappedType(RITChar.ExpType), RITChar.ParentRITE, RITChar.ID));
                            //IEnumerable<RITCharacteristic> RITChars;
                            //RITChars = rite.RiskCharacteristics.Where(RitChar => RitChar.ExpType == expType || ExposureTypeCollection.GetMappedType(RitChar.ExpType) == expType);
                            //foreach (RITCharacteristic RitChar in RITChars)
                            //{
                            //    ARITEs.Add(new CoverageAtomicRITE(subperil, ExposureTypeCollection.GetMappedType(RitChar.ExpType), rite, RitChar.ID));
                            //}
                        }
                    }
                }

                return new HashSet<AtomicRITE>(ARITEs.Cast<AtomicRITE>());
            }
            else
                throw new InvalidOperationException("Cannot get Atomic Rites for derived subjects!");           
        }

        public bool IsLargerThan(PrimarySubject other)
        {
            if (base.Equals(other) &&
                this.Schedule.ScheduleList.SetEquals(other.Schedule.ScheduleList))
                return false;
            else if (this.Schedule.IsLargerOrEqualThan(other.Schedule) &&
                base.IsLargerThanOrEquals(other))
                return true;
            else
                return false;
        }

        public override bool IsLargerThan(Subject other)
        {
            if (other is PrimarySubject)
                return this.IsLargerThan(other as PrimarySubject);
            else
                return false;
        }

    }

    public class ReinsuranceSubject : Subject, IEquatable<ReinsuranceSubject>
    {
        private ScheduleOfContracts schedule;
        public FunctionType SubjectFunction { get; set; } //default is "Sum" function.
        public ScheduleOfContracts Schedule
        {get{ return schedule;}}

        public int Size
        {
            get { return exposureTypes.Count + causeOfLossSet.Count + schedule.ScheduleList.Count; }
        }
        public override bool IsLocCvg { get { return false; } }

        public ReinsuranceSubject(List<string> _childrenCoverNodeList):base()
        {
            IsDerived = true;
            ChildrenCoverNodeList = _childrenCoverNodeList;
            SubjectFunction = FunctionType.Sum;
            TotalNumSubjects++;
            ID = TotalNumSubjects;
        }

        public ReinsuranceSubject(ScheduleOfContracts _schedule, ExposureTypeCollection _exposureTypes, COLCollection _causeOfLossSet):base()
        {
            schedule = _schedule;
            exposureTypes = _exposureTypes;
            causeOfLossSet = _causeOfLossSet;
            SubjectFunction = FunctionType.Sum;
            TotalNumSubjects++;
            ID = TotalNumSubjects;
        }

        public bool Equals(ReinsuranceSubject other)
        {
            if (other == null)
                return false;

            if (IsDerived)
            {
                return Enumerable.SequenceEqual(ChildrenCoverNodeList.OrderBy(t => t), other.ChildrenCoverNodeList.OrderBy(t => t));
            }
            else
            {
                if (other is ReinsuranceSubject)
                {
                    if (this.schedule.ScheduleListEquals((other as ReinsuranceSubject).schedule) &
                        base.Equals(other))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }

        }

        public override bool Equals(Subject other)
        {
            if (other == null)
                return false;

            if (other is ReinsuranceSubject)
            {
                return Equals(other as ReinsuranceSubject);
            }
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            ReinsuranceSubject subjectObj = obj as ReinsuranceSubject;
            if (subjectObj == null)
                return false;
            else
                return Equals(subjectObj);
        }

        public override int GetHashCode()
        {
            if (IsDerived)
            {
                int code = 0;
                foreach (string s in ChildrenCoverNodeList)
                {
                    code = code + s.GetHashCode() * 31;
                }
                return code;
            }
            else
            {
                return schedule.GetScheduleListHashCode() ^ base.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (IsDerived)
                return base.ToString();
            else
                return schedule.Name + "; " + base.ToString();
        }

        public override HashSet<AtomicRITE> GetAtomicRites()
        {
            if (!IsDerived)
            {
                HashSet<ContractAtomicRITE> ARITEs = new HashSet<ContractAtomicRITE>();

                foreach (Graph contractGraph in Schedule.ScheduleList)
                {
                    foreach (string subperil in CauseOfLossSet.GetSubperils())
                    {
                        foreach (ExposureType expType in ExposureTypes)
                        {                    
                             ARITEs.Add(new ContractAtomicRITE(contractGraph, subperil, expType));
                        }
                    }
                }

                return new HashSet<AtomicRITE>(ARITEs.Cast<AtomicRITE>()); ;
            }
            else
                throw new InvalidOperationException("Cannot get Atomic Rites for derived subjects!");           
        }

        public bool IsLargerThan(ReinsuranceSubject other)
        {
            if (base.Equals(other) &&
                this.Schedule.Equals(other.Schedule))
                return false;
            else if (this.Schedule.IsLargerOrEqualThan(other.Schedule) &&
                base.IsLargerThanOrEquals(other))
                return true;
            else
                return false;
        }

        public override bool IsLargerThan(Subject other)
        {
            if (other is ReinsuranceSubject)
                return this.IsLargerThan(other as ReinsuranceSubject);
            else
                return false;
        }
    }
}
          