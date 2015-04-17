using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    public class Participation<V> : Expression<V>, IEquatable<Participation<V>>
        where V : Value
    {
        #region Fields
        private Ratio<V> Ratio;
        #endregion

        #region Constructors
        public Participation() : this(null) { }
        public Participation(double ratio) : this(new Ratio<V>(ratio)) { }
        public Participation(Ratio<V> r)
        {
            Ratio = r;
        }
        public Participation(object o) : this((Ratio<V>)o) { }
        #endregion

        #region Methods
        public Ratio<V> GetRatio()
        {
            return Ratio;
        }
        public override double GetEvaluatedValue(params object[] bindings)
        {
            return Ratio.GetEvaluatedValue(bindings);
        }
        public override string ToString()
        {
            return "PARTICIPATION(" + Ratio.ToString() + ")";
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Participation<V>))
                return false;

            Participation<V> e = obj as Participation<V>;

            return this.Equals(e);
        }

        public bool Equals(Participation<V> e)
        {
            if (e == null)
            {
                return false;
            }

            return (Ratio.Equals(e.Ratio));
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + Ratio.GetHashCode();
            return hash;
        }
        #endregion
    }
}
