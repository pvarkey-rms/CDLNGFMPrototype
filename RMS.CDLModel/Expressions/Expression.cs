using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class Expression<V> : IExpression<V>, IEquatable<Expression<V>>
        where V : Value
    {
        protected V value;

        #region Constructors
        public Expression()
        {
            //value = null;
        }

        public Expression(V v)
        {
            value = v;
        }

        public Expression(Expression<V> e)
        {
            value = (V)e.GetValue();
        }
        #endregion

        #region Methods
        public virtual V GetValue()
        {
            return value;
        }

        public virtual double GetEvaluatedValue(params object[] bindings)
        {
            return GetValue().GetEvaluatedValue(bindings);
        }

        public virtual void SetValue(IValue<AValue> v)
        {
            value = (V)v;
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Expression<V>))
                return false;

            Expression<V> e = obj as Expression<V>;

            return (value.Equals(e.value));
        }

        public virtual bool Equals(Expression<V> e)
        {
            if (e == null)
            {
                return false;
            }

            return (value.Equals(e.value));
        }

        public override int GetHashCode()
        {
            int hash = 23;
            if(value != null)
                hash = hash * 37 + value.GetHashCode();
            return hash;
        }
        #endregion
    }
}
