using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class Percentage<V> : Expression<V>, IEquatable<Percentage<V>>
        where V : Value
    {
        #region Constructors
        public Percentage(Expression<V> e) : base(e) { }
        public Percentage(V v) : base(v) { }
        public Percentage() : base() { }
        #endregion

        #region Methods
        public override string ToString()
        {
            return GetValue().ToString() + "%";
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Percentage<V>))
                return false;

            Percentage<V> e = obj as Percentage<V>;

            return (value.Equals(e.value));
        }

        public bool Equals(Percentage<V> e)
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
            hash = hash * 37 + GetValue().GetHashCode();
            return hash;
        }
        #endregion
    }
}
