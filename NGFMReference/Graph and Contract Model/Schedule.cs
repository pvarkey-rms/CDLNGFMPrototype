using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public abstract class Schedule<T> : IEquatable<Schedule<T>> 
    {      
        public string Name { get; private set; }
        public HashSet<T> ScheduleList { get; private set; }
        
        public Schedule(string _name)
        {
            ScheduleList = new HashSet<T>();
            Name = _name;
        }

        public Schedule(string _name, HashSet<T> listItems)
        {
            ScheduleList = listItems;
            Name = _name;
        }

        public abstract void AddItem(T Item);

        public virtual bool IsLargerOrEqualThan(Schedule<T> other)
        {
            if (other == null)
                return false;

            if (this.ScheduleList.IsSupersetOf(other.ScheduleList))
                return true;
            else
                return false;
        }

        #region Schedule Equality overrides
            public bool Equals(Schedule<T> other)
            {
                if (other == null)
                    return false;

                if (this.ScheduleList.SetEquals(other.ScheduleList)
                    & this.Name == other.Name)
                    return true;
                else
                    return false;
            }

            public override bool Equals(Object obj)
            {
                if (obj == null)
                    return false;

                Schedule<T> subjectObj = obj as Schedule<T>;
                if (subjectObj == null)
                    return false;
                else
                    return Equals(subjectObj);
            }

            public override int GetHashCode()
            {
                int code = 0;
            
                foreach (T item in ScheduleList)
                {
                    code = code + 31 * item.GetHashCode();
                }

                return this.Name.GetHashCode() + code;
                //return code;
            }

        #endregion

        public override string ToString()
        {
            string[] ItemIds = this.ScheduleList.Select(item => item.ToString()).ToArray();
            return string.Join(",", ItemIds);
        }

        #region Equality Used For Subjects
        public virtual bool ScheduleListEquals(Schedule<T> other)
        {
            if (other == null)
                return false;

            if (this.ScheduleList.SetEquals(other.ScheduleList))
                return true;
            else
                return false;
        }

        public virtual int GetScheduleListHashCode()
        {
            int code = 0;

            foreach (T item in ScheduleList)
            {
                code = code + 31 * item.GetHashCode();
            }

            return code;
        }

        #endregion

        public string GetDifferenceString(Schedule<T> largerSchedule)
        {
            string[] ItemIds = largerSchedule.ScheduleList.Except(this.ScheduleList).Select(Item => Item.ToString()).ToArray();
            return string.Join(",", ItemIds);
        }
    }

    public class ScheduleOfRITEs: Schedule<RITE>
    {       
        public bool IsLocation { get; private set; }
        public int ActNumOfBldgs
        {
            get
            {
                if (IsLocation)
                    return ScheduleList.First().ActNumOfBldgs;
                else
                    return 1;
            }
        }
        public int NumOfBldgs
        {
            get
            {
                if (IsLocation)
                    return ScheduleList.First().NumOfBldgs;
                else
                    return 1;
            }
        }

        public int[] MultiplierArr
        {
            get
            {
                if (IsLocation)
                {
                    return ScheduleList.First().GetMultiplierArr;
                }
                else
                    return RITE.GenerateMultiplierArr(1);
            }
        }

        public HashSet<RITCharacteristic> RITChars { get; private set; }

        public ScheduleOfRITEs(string _name):base(_name)
        {
            RITChars = new HashSet<RITCharacteristic>();
            IsLocation = false;
        }

        public ScheduleOfRITEs(string _name, HashSet<RITE> rites, HashSet<RITCharacteristic> _ritChars)
            : base(_name, rites)
        {
            RITChars = _ritChars;
            IsLocation = false;
            foreach (RITE rite in rites)
            {
                this.AddItem(rite);
            }
        }

        public override void AddItem(RITE Item)
        {
            ScheduleList.Add(Item);
            if (ScheduleList.Count == 1)
                IsLocation = true;
            else if (ScheduleList.Count > 1)
                IsLocation = false;
        }

        public void AddCharacteristic(RITCharacteristic Item)
        {
            RITChars.Add(Item);
        }

        public override bool IsLargerOrEqualThan(Schedule<RITE> other)
        {
            ScheduleOfRITEs otherSch = other as ScheduleOfRITEs;
            if (otherSch == null)
                return false;

            if (IsLocation)
            {
                if (this.RITChars.IsSupersetOf(otherSch.RITChars))
                    return true;
                else
                    return false;
            }
            else
            {
                if (this.ScheduleList.IsSupersetOf(other.ScheduleList))
                    return true;
                else
                    return false;
            }
        }

        #region override Equality Used For PrimarySubjects
        public override bool ScheduleListEquals(Schedule<RITE> other)
        {
            ScheduleOfRITEs otherSch = other as ScheduleOfRITEs;
            if (otherSch == null)
                return false;

            if (IsLocation)
            {
                if (this.RITChars.SetEquals(otherSch.RITChars))
                    return true;
                else
                    return false;
            }
            else
            {
                if (this.ScheduleList.SetEquals(otherSch.ScheduleList))
                    return true;
                else
                    return false;
            }
        }

        public override int GetScheduleListHashCode()
        {
            int code = 0;

            if (IsLocation)
            {
                foreach (RITCharacteristic item in RITChars)
                {
                    code = code + 31 * item.GetHashCode();
                }
            }
            else
            {
                foreach (RITE item in ScheduleList)
                {
                    code = code + 31 * item.GetHashCode();
                }
            }

            return code;
        }

        #endregion
    }

    public class ScheduleOfContracts : Schedule<Graph>
    {
        public ScheduleOfContracts(string _name):base(_name)
        {}

        public ScheduleOfContracts(string _name, HashSet<Graph> contracts)
            : base(_name, contracts) {}

        public override void AddItem(Graph Item)
        {
            ScheduleList.Add(Item);
        }
    }
}
