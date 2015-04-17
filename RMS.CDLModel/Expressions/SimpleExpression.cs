using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace RMS.ContractObjectModel
{
    [Serializable]
    [ProtoContract]
    public class SimpleExpression<V> : Expression<V>, IEquatable<SimpleExpression<V>>
        where V : Value
    {
        #region Constructors
        public SimpleExpression() : base() { }
        public SimpleExpression(V v) : base(v) { }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(SimpleExpression<V>))
                return false;

            SimpleExpression<V> e = obj as SimpleExpression<V>;

            return (value.Equals(e.value));
        }

        public bool Equals(SimpleExpression<V> e)
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
            hash = hash * 37 + value.GetHashCode();
            return hash;
        }
        #endregion
    }
}
