using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    [ProtoContract]
    [ProtoInclude(1, typeof(Term<Value>))]
    public interface ITerm<out V>
        where V : Value
    {
        IExpression<V> GetExpression();
        double GetEvaluatedExpression(params object[] bindings);
        TimeBasis GetTimeBasis();
        Subject GetSubject();
        string GetLabel();

        void HardResetSchedule(HashSet<SymbolicValue> NewScheduleSymbols, Dictionary<string, HashSet<long>> ResolvedSchedule);
    }

    // TODO: make this abstract
    [Serializable]
    [ProtoContract]
    public class Term<V> : ITerm<V>, IEquatable<Term<V>>
        where V : Value
    {
        #region Fields

        [ProtoMember(1)]
        string Label;

        [ProtoMember(2)]
        IExpression<V> _Expression;

        [ProtoMember(3)]
        TimeBasis _TimeBasis;

        [ProtoMember(4)]
        Subject _Subject;
        
        #endregion

        #region Constructors
        public Term(IExpression<V> expression) : this(expression, new Subject()) { }
        public Term(IExpression<V> expression, Subject subjectConstraint, TimeBasis timeBasis = TimeBasis.Default, string Label = null)
        {
            this.Label = Label;
            this._Expression = expression;
            this._Subject = subjectConstraint;
            this._TimeBasis = timeBasis;
        }

        public Term(Term<V> CopyFrom)
        {
            this.Label = CopyFrom.GetLabel();
            this._Expression = CopyFrom.GetExpression();
            this._Subject = new Subject(CopyFrom.GetSubject());
            this._TimeBasis = CopyFrom.GetTimeBasis();
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return "TERM " + ((Label != null) ? Label : "") + "\n{\n" +
                "\t_Expression: " + ((_Expression != null) ? _Expression.ToString() : "") + "\n" +
                "}";                        
        }
        public virtual string GetLabel()
        {
            return Label;
        }
        public virtual IExpression<V> GetExpression()
        {
            return _Expression;
        }
        public virtual double GetEvaluatedExpression(params object[] bindings)
        {
            return GetExpression().GetEvaluatedValue(bindings);
        }
        public virtual TimeBasis GetTimeBasis()
        {
            return _TimeBasis;
        }
        public virtual Subject GetSubject()
        {
            return _Subject;
        }
        public void HardResetSchedule(HashSet<SymbolicValue> NewScheduleSymbols, Dictionary<string, HashSet<long>> ResolvedSchedule)
        {
            _Subject.HardResetSchedule(NewScheduleSymbols, ResolvedSchedule);
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this == obj)
                return true;

            if (obj.GetType() != typeof(Term<V>))
                return false;

            Term<V> term = obj as Term<V>;

            return (this.Equals(term));
        }

        public bool Equals(Term<V> that)
        {
            if (that == null)
                return false;

            if (this == that)
                return true;

            if (this.Label != null)
                return this.Label.Equals(that.Label);
            else if (that.Label != null)
                return false;

            bool IsEqual = true;

            IsEqual &= _Expression.Equals(that._Expression);

            if (_Subject != null)
                IsEqual &= _Subject.Equals(that._Subject);
            else if (that._Subject != null)
                return false;

            IsEqual &= _TimeBasis.Equals(that._TimeBasis);
            
            return IsEqual;
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + ((Label != null) ? Label.GetHashCode() : 41);
            hash = hash * 37 + ((_Expression != null) ? _Expression.GetHashCode() : 41);
            hash = hash * 37 + _TimeBasis.GetHashCode();
            hash = hash * 37 + ((_Subject != null) ? _Subject.GetHashCode() : 41);
            return hash;
        }
        #endregion
    }
}
