using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace RMS.ContractObjectModel
{
    [Serializable]
    [ProtoContract]
    [ProtoInclude(15, typeof(SymbolicValue))]
    public class Value : IValue<AValue>, IEquatable<Value>, IEvaluatableValue
    {
        private object value;

        #region Constructors
        public Value() : this(null) { }
        public Value(object o)
        {
            value = o;
        }
        public Value(string s)
        {
            value = s;
        }
        #endregion

        #region Implicit Operators
        public static implicit operator Value(int x)
        {
            return new Value((object)x);
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return value.ToString();
        }

        public virtual double GetEvaluatedValue(params object[] bindings)
        {
            // TODO: throw exception here
            return (double)value;
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Value))
                return false;

            Value v = obj as Value;

            return (value == v.value);
        }

        public virtual bool Equals(Value v)
        {
            if (v == null)
            {
                return false;
            }

            return (value == v.value);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + value.GetHashCode();
            return hash;
        }
        #endregion

        #region Operators
        public static Value operator +(Value av1, Value av2) { return null; }
        public static Value operator -(Value av1, Value av2) { return null; }
        public static Value operator *(AValue av1, Value av2) { return null; }
        public static Value operator /(Value av1, Value av2) { return null; }
        #endregion
    }
}
