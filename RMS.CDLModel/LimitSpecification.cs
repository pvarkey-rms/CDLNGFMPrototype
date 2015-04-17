using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    public interface ILimitSpecification<out V>
        where V : IValue<AValue>
    {
        IExpression<V> GetExpression();
        bool IsPAY();
        double GetEvaluatedExpression(params object[] bindings);
    }

    public class LimitSpecification<V> : ILimitSpecification<V>, IEquatable<LimitSpecification<V>>
        where V : IValue<AValue>
    {
        IExpression<V> expression;
        bool PAY;

        public LimitSpecification() : this(null, false) { }
        public LimitSpecification(double amount) : this((IExpression<V>)(new NumericExpression(amount))) { }
        public LimitSpecification(IExpression<V> e) : this(e, false) { }
        public LimitSpecification(IExpression<V> e, bool pay)
        {
            expression = e;
            PAY = pay;
        }

        #region Methods
        public override string ToString()
        {
            return "PAYOUTEXPRESSION(" + ((PAY) ? "PAY " : "") + expression.ToString() + ")";
        }
        public IExpression<V> GetExpression()
        {
            return expression;
        }
        public double GetEvaluatedExpression(params object[] bindings)
        {
            return GetExpression().GetEvaluatedValue(bindings);
        }
        public bool IsPAY()
        {
            return PAY;
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(LimitSpecification<V>))
                return false;

            LimitSpecification<V> e = obj as LimitSpecification<V>;

            return this.Equals(e);
        }

        public bool Equals(LimitSpecification<V> e)
        {
            if (e == null)
            {
                return false;
            }

            return (expression.Equals(e.expression) && (PAY == e.PAY));
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + expression.GetHashCode();
            hash = hash * 37 + PAY.GetHashCode();
            return hash;
        }
        #endregion
    }
}
