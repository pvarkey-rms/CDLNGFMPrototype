using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class HoursClause
    {
        #region Inner Types
        public enum Unit
        {
            Hours,
            Days,
            Weeks
        };
        #endregion

        #region Fields
        int _Duration;
        public int Duration { get { return _Duration; } set { _Duration = value; } }

        Unit _DurationTumeUnit = Unit.Hours;
        public Unit DurationTimeUnit { get { return _DurationTumeUnit; } set { _DurationTumeUnit = value; } }

        HashSet<SymbolicValue> _CausesOfLoss;
        public HashSet<SymbolicValue> CausesOfLoss { get { return _CausesOfLoss; } set { _CausesOfLoss = value; } }

        bool _OnlyOnce = false;
        public bool OnlyOnce { get { return _OnlyOnce; } set { _OnlyOnce = value; } }
        #endregion

        public HoursClause(int duration, Unit _unit = Unit.Hours, 
            bool onlyOnce = false, HashSet<SymbolicValue> causesOfLoss = null)
        {
            this._Duration = duration;
            this._DurationTumeUnit = _unit;
            this._CausesOfLoss = causesOfLoss;
            this._OnlyOnce = onlyOnce;
        }

        public HoursClause(int duration) : this(duration, Unit.Hours) { }

        public TimeSpan GetDuration()
        {
            if (_DurationTumeUnit == Unit.Days)
            {
                return new TimeSpan(_Duration, 0, 0, 0);
            }

            if (_DurationTumeUnit == Unit.Hours)
            {
                return new TimeSpan(0, _Duration, 0, 0);
            }

            throw new NotSupportedException();
        }

    }
}
