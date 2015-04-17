using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    public class Ratio<V> : Expression<V>, IEquatable<Ratio<V>>
        where V : Value
    {
        #region Fields
        public IExpression<V> Expression;
        #endregion

        #region Constructors
        public Ratio() : this(null) { }
        public Ratio(double ratio) : this(new NumericExpression(ratio)) { }
        public Ratio(Expression<V> e)
        {
            Expression = e;
        }
        public Ratio(object o) : this((Expression<V>)o) { }
        #endregion

        #region Methods
        public IExpression<V> GetExpression()
        {
            return Expression;
        }
        public override double GetEvaluatedValue(params object[] bindings)
        {
            return GetExpression().GetEvaluatedValue(bindings);
        }
        public override string ToString()
        {
            return "RATIO(" + Expression.ToString() + ")";
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Ratio<V>))
                return false;

            Ratio<V> e = obj as Ratio<V>;

            return this.Equals(e);
        }

        public bool Equals(Ratio<V> e)
        {
            if (e == null)
            {
                return false;
            }

            return (Expression.Equals(e.Expression));
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + Expression.GetHashCode();
            return hash;
        }
        #endregion
    }
}
