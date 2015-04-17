using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    public interface ILimit<out V>
        where V : IValue<AValue>
    {
        ILimitSpecification<V> GetLimitSpecification();
        double GetEvaluatedExpression(params object[] bindings);
        TimeBasis GetTimeBasis();
        int GetNumberReinstatements();
        bool IsPAY();
    }

    public class Limit<V> : ILimit<V>, IEquatable<Limit<V>>
        where V : IValue<AValue>
    {
        LimitSpecification<V> LimitSpecification;
        TimeBasis LimitTimeBasis = TimeBasis.Default;
        int NumberReinstatements;

        //public Limit() : this(null, TimeBasis.Default) { }
        public Limit(double amount, TimeBasis timeBasis, int numberReinstatements) 
            : this(new LimitSpecification<V>(amount), timeBasis, numberReinstatements) { }
        public Limit(IExpression<V> AmountExpression, TimeBasis timeBasis, int numberReinstatements)
            : this(new LimitSpecification<V>(AmountExpression), timeBasis, numberReinstatements) { }
        public Limit(LimitSpecification<V> limitSpecification, int numberReinstatements) 
            : this(limitSpecification, TimeBasis.Default, numberReinstatements) { }
        public Limit(LimitSpecification<V> limitSpecification, TimeBasis timeBasis, int numberReinstatements)
        {
            this.LimitSpecification = limitSpecification;
            this.LimitTimeBasis = timeBasis;
            this.NumberReinstatements = numberReinstatements;
        }

        #region Methods
        public override string ToString()
        {
            return "PAYOUT(" + LimitSpecification.ToString() + ", " + LimitTimeBasis.ToString() + ")";
        }
        public ILimitSpecification<V> GetLimitSpecification()
        {
            return LimitSpecification;
        }
        public double GetEvaluatedExpression(params object[] bindings)
        {
            return GetLimitSpecification().GetEvaluatedExpression(bindings);
        }
        public TimeBasis GetTimeBasis()
        {
            return LimitTimeBasis;
        }
        public int GetNumberReinstatements()
        {
            return NumberReinstatements;
        }
        public bool IsPAY()
        {
            return GetLimitSpecification().IsPAY();
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Limit<V>))
                return false;

            Limit<V> e = obj as Limit<V>;

            return this.Equals(e);
        }

        public bool Equals(Limit<V> e)
        {
            if (e == null)
            {
                return false;
            }

            return (LimitSpecification.Equals(e.LimitSpecification) && LimitTimeBasis.Equals(e.LimitTimeBasis));
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + LimitSpecification.GetHashCode();
            hash = hash * 37 + LimitTimeBasis.GetHashCode();
            return hash;
        }
        #endregion
    }
}
