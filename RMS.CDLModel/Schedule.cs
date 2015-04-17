using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    [Serializable]
    [ProtoContract]
    public class Schedule : IEquatable<Schedule>
    {
        [ProtoMember(1)]
        public HashSet<SymbolicValue> ScheduleSymbols;

        #region Constructors

        public Schedule() : this(new HashSet<SymbolicValue>()) { }

        public Schedule(Schedule copyFrom) : this(new HashSet<SymbolicValue>(copyFrom.ScheduleSymbols)) { }

        public Schedule(HashSet<SymbolicValue> symbols)
        {
            ScheduleSymbols = symbols;
        }

        #endregion

        #region Operator Overloads

        public static Schedule operator -(Schedule schedule1, Schedule schedule2)
        {
            return new Schedule(new HashSet<SymbolicValue>(schedule1.ScheduleSymbols.Except(schedule2.ScheduleSymbols)));
        }

        #endregion

        #region Equality Overrides

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(Schedule))
                return false;

            Schedule s = obj as Schedule;

            return (this.Equals(s));
        }

        public bool Equals(Schedule s)
        {
            if (s == null)
                return false;

            if (ScheduleSymbols == null && s.ScheduleSymbols == null)
                return true;
            else if (ScheduleSymbols == null || s.ScheduleSymbols == null)
                return false;
            else if (ScheduleSymbols.Count != s.ScheduleSymbols.Count)
                return false;
            else return ScheduleSymbols.IsSubsetOf(s.ScheduleSymbols);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            if (ScheduleSymbols != null && ScheduleSymbols.Count != 0)
                foreach (SymbolicValue ScheduleSymbol in ScheduleSymbols)
                    hash = hash * 37 + ScheduleSymbol.GetHashCode();
            return hash;
        }

        #endregion
    }
}
