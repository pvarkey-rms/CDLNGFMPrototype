using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class NumericValue : Value, IEquatable<NumericValue>, IEvaluatableValue
    {
        #region Fields
        private double mantissa;
        private Power10 exponent;
        private double value;
        #endregion

        #region Constructors
        public NumericValue() : this(0.0) { }
        public NumericValue(double n) : this(n, null) { }
        public NumericValue(string s) 
        {
            double n;
            if (double.TryParse(s, out n))
                SetValue(n, null);
            else
                throw new FormatException("String \"" + s + "\" contains incorrect format of double value");
        }
        public NumericValue(double n, string mul)
        {
            SetValue(n, mul);
        }
        #endregion

        #region Implicit Operators
        public static implicit operator NumericValue(double x)
        {
            return new NumericValue(x);
        }
        public static implicit operator NumericValue(int x)
        {
            return new NumericValue((double)x);
        }
        #endregion

        #region Overloaded Operators
        public static NumericValue operator + (NumericValue c1, NumericValue c2)
        {
            return new NumericValue(c1.value + c2.value);
        }
        public static SymbolicValue operator + (NumericValue c1, SymbolicValue c2)
        {
            return Operation("+", c1, c2);
        }

        public static NumericValue operator - (NumericValue c1, NumericValue c2)
        {
            return new NumericValue(c1.value - c2.value);
        }
        public static SymbolicValue operator - (NumericValue c1, SymbolicValue c2)
        {
            return Operation("-", c1, c2);
        }

        public static NumericValue operator * (NumericValue c1, NumericValue c2)
        {
            return new NumericValue(c1.value * c2.value);
        }
        public static SymbolicValue operator * (NumericValue c1, SymbolicValue c2)
        {
            return Operation("*", c1, c2);
        }

        public static NumericValue operator / (NumericValue c1, NumericValue c2)
        {
            if (c2.value.Equals(0f))
                throw new DivideByZeroException("Trying to divide by zero NumericValue");
            return new NumericValue(c1.value / c2.value);
        }
        public static SymbolicValue operator / (NumericValue c1, SymbolicValue c2)
        {
            return Operation("/", c1, c2);
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return value.ToString();
        }
        public static SymbolicValue Operation(string operationName, NumericValue c1, SymbolicValue c2)
        {
            return new SymbolicValue("(" + c1.ToString() + operationName + c2.ToString() + ")");
        }
        public void SetValue(double n, string mul)
        {
            if (mul == null)
                exponent = Power10.Unit;
            else if (mul.ToLower().Equals("k") || mul.ToLower().Equals("thousand"))
                exponent = Power10.K;
            else if (mul.ToLower().Equals("m") || mul.ToLower().Equals("million"))
                exponent = Power10.M;
            else if (mul.ToLower().Equals("b") || mul.ToLower().Equals("billion"))
                exponent = Power10.B;
            else if (mul.ToLower().Equals("t") || mul.ToLower().Equals("trillion"))
                exponent = Power10.T;
            else
                exponent = Power10.Unit;

            mantissa = n;
            value = mantissa * Math.Pow(10.0, (double)exponent);
        }
        public override double GetEvaluatedValue(params object[] bindings)
        {
            return value;
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(NumericValue))
                return false;
            
            NumericValue v = obj as NumericValue;

            return (value == v.value);
        }

        public override bool Equals(Value v)
        {
            if (v == null)
            {
                return false;
            }

            if (v.GetType() != typeof(NumericValue))
                return false;

            return this.Equals(v as NumericValue);
        }

        public bool Equals(NumericValue nv)
        {
            if (nv == null)
            {
                return false;
            }

            return (value == nv.value);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + value.GetHashCode();
            return hash;
        }
        #endregion
    }

    public enum Power10 // encoded as an exponent of 10
    {
        Unit = 0,
        K = 3,
        M = 6,
        B = 9,
        T = 12
    };

    public class NumericExpression : SimpleExpression<NumericValue>, IEquatable<NumericExpression>
    {
        #region Constructors
        public NumericExpression() : this(0.0) { }
        public NumericExpression(double n) : this(n, null) { }
        public NumericExpression(double n, string mul)
        {
            value = new NumericValue(n, mul);
        }
        public NumericExpression(NumericValue nVal)
        {
            value = nVal;
        }
        #endregion

        #region Methods
        public override NumericValue GetValue()
        {
            return value;
        }
        public void SetValue(NumericValue val)
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

            if (obj.GetType() != typeof(NumericExpression))
                return false;

            NumericExpression e = obj as NumericExpression;

            return (value.Equals(e.value));
        }

        public override bool Equals(Expression<NumericValue> e)
        {
            if (e == null)
            {
                return false;
            }

            return this.Equals(e as NumericExpression);
        }

        public bool Equals(NumericExpression e)
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
