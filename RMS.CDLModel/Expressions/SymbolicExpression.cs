using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace RMS.ContractObjectModel
{
    [Serializable]
    [ProtoContract]
    public class SymbolicValue : Value, IEquatable<SymbolicValue>, IEvaluatableValue
    {
        #region Inner Types
        public delegate double BinderDelegate(string symbol);
        public class UnboundSymbolException : Exception 
        {
            public UnboundSymbolException(string msg) : base(msg) { }
        }
        #endregion

        #region Fields

        [ProtoMember(1)]
        public string value;

        BinderDelegate GetBoundValue = null;

        #endregion

        public SymbolicValue(string ident) : this(ident, null) { }
        public SymbolicValue(string ident, BinderDelegate binder) { this.value = ident; this.GetBoundValue = binder; }

        #region Methods
        public override string ToString()
        {
            return value;
        }

        public void SetBinder(BinderDelegate binder)
        {
            this.GetBoundValue = binder;
        }

        public override double GetEvaluatedValue(params object[] bindings)
        {
            if (GetBoundValue != null)
                return GetBoundValue(value);

            if ((bindings != null) && (bindings.Length != 0))
            {
                if (bindings[0] is BinderDelegate)
                    return (bindings[0] as BinderDelegate)(value);
                else if (bindings[0] is Dictionary<string, double>)
                {
                    if ((bindings[0] as Dictionary<string, double>).ContainsKey(value))
                        return (bindings[0] as Dictionary<string, double>)[value];
                }
            }

            throw new UnboundSymbolException("Symbol binder missing or cannot find binding for symbol " + value + "!");
        }
        #endregion

        #region Implicit Operators
        public static implicit operator SymbolicValue(string x)
        {
            return new SymbolicValue(x);
        }
        public static implicit operator SymbolicValue(double x)
        {
            return new SymbolicValue(x.ToString());
        }
        public static implicit operator SymbolicValue(NumericValue x)
        {
            return new SymbolicValue(x.ToString());
        }
        #endregion

        #region Overloaded Operators
        public static SymbolicValue operator + (SymbolicValue c1, SymbolicValue c2)
        {
            return new SymbolicValue("(" + c1.value + "+" +  c2.value + ")");
        }
        public static SymbolicValue operator - (SymbolicValue c1, SymbolicValue c2)
        {
            return new SymbolicValue("(" + c1.value + "-" + c2.value + ")");
        }
        public static SymbolicValue operator * (SymbolicValue c1, SymbolicValue c2)
        {
            return new SymbolicValue("(" + c1.value + "*" + c2.value + ")");
        }
        public static SymbolicValue operator / (SymbolicValue c1, SymbolicValue c2)
        {
            return new SymbolicValue("(" + c1.value + "/" + c2.value + ")");
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(SymbolicValue))
                return false;

            SymbolicValue v = obj as SymbolicValue;

            return (value == v.value);
        }

        public override bool Equals(Value v)
        {
            if (v == null)
            {
                return false;
            }

            if (v.GetType() != typeof(SymbolicValue))
                return false;

            return this.Equals(v as SymbolicValue);
        }

        public bool Equals(SymbolicValue sv)
        {
            if (sv == null)
            {
                return false;
            }

            return (value == sv.value);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + value.GetHashCode();
            return hash;
        }
        #endregion
    }

    public class SymbolicExpression : SimpleExpression<SymbolicValue>, IEquatable<SymbolicExpression>
    {
        #region Constructors
        public SymbolicExpression() : this((string)null) { }
        public SymbolicExpression(SymbolicValue value)
        {
            this.value = value;
        }
        public SymbolicExpression(string ident)
        {
            value = new SymbolicValue(ident);
        }
        #endregion

        #region Methods
        public override SymbolicValue GetValue()
        {
            return value;
        }
        public void SetValue(SymbolicValue val)
        {
            value = val;
        }
        public override string ToString()
        {
            return value.ToString();
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(SymbolicExpression))
                return false;

            SymbolicExpression e = obj as SymbolicExpression;

            return (value.Equals(e.value));
        }

        public bool Equals(SymbolicExpression e)
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

        #region Implicit Operators
        public static implicit operator SymbolicExpression(SymbolicValue x)
        {
            return new SymbolicExpression(x);
        }
        #endregion
    }
}
