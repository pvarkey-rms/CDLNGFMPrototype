using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class MoneyValue<V> : Value, IEquatable<MoneyValue<V>>
        where V : Value, IValue<AValue>
    {
        public V faceValue;
        public Currency currencySymbol;
        public double currencyConversionMultiplier = -1.0;

        public MoneyValue(V fV) : this(fV, Currency.USD, 1.0) { }
        public MoneyValue(V fV, Currency cur) : this(fV, cur, -1.0) { }
        public MoneyValue(V fV, Currency cur, double conv) 
        { 
            this.faceValue = fV;
            this.currencySymbol = cur;
            this.currencyConversionMultiplier = conv;
        }
        public MoneyValue(IExpression<V> fVE) : this(fVE, Currency.USD, 1.0) { }
        public MoneyValue(IExpression<V> fVE, Currency cur) : this(fVE, cur, -1.0) { }
        public MoneyValue(IExpression<V> fVE, Currency cur, double conv)
        {
            this.faceValue = (V)fVE.GetValue();
            this.currencySymbol = cur;
            this.currencyConversionMultiplier = conv;
        }
        
        #region Methods
        public override string ToString()
        {
            return ((dynamic)faceValue * currencyConversionMultiplier).ToString() + " " + currencySymbol.ToString();
        }

        public override double GetEvaluatedValue(params object[] bindings)
        {
            return ((IEvaluatableValue)((dynamic)faceValue * currencyConversionMultiplier)).GetEvaluatedValue(bindings);
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(MoneyValue<V>))
                return false;

            return IsEquals(obj as MoneyValue<V>);
        }

        public bool Equals(MoneyValue<V> v)
        {
            if (v == null)
                return false;

            return IsEquals(v);
            }

        private bool IsEquals(MoneyValue<V> v)
        {
            return (faceValue == v.faceValue) && (currencySymbol == v.currencySymbol)
                && (currencyConversionMultiplier == v.currencyConversionMultiplier);
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + faceValue.GetHashCode();
            hash = hash * 37 + currencyConversionMultiplier.GetHashCode();
            hash = hash * 37 + currencySymbol.GetHashCode();
            return hash;
        }
        #endregion
    }

    public enum Currency // encoded as an exponent of 10
    {
        USD,
        GBP,
        EUR,
        JPY,
        BGN
    };

    public class Money<V> : Expression<MoneyValue<V>>, IEquatable<Money<V>>
        where V : Value
    {
        #region Inner Types
        public delegate double USDConversionMultiplier(Currency currencySymbol);
        #endregion

        #region Fields
        MoneyValue<V> monetaryValue;
        IExpression<V> monetaryExpression;
        USDConversionMultiplier usdConversionMultiplier;
        #endregion

        #region Constructors
        public Money(V v) : this(v, Currency.USD, null) { }
        public Money(V v, Currency cur, USDConversionMultiplier usdConversionMultiplierDelegate)
        {
            if (usdConversionMultiplierDelegate != null)
                usdConversionMultiplier = usdConversionMultiplierDelegate;
            else
                usdConversionMultiplier = new USDConversionMultiplier(DefaultUSDConversionMultiplier);
            this.monetaryValue = new MoneyValue<V>(v, cur, usdConversionMultiplier(cur));
            //this.value = this.monetaryValue; //JJHACK: so that GetValue() works without casting
            this.monetaryExpression = null;
        }

        public Money(MoneyValue<V> v) : this(v.faceValue, v.currencySymbol, null) { }

        public Money(IExpression<V> e, Currency cur = Currency.USD,
            USDConversionMultiplier usdConversionMultiplierDelegate = null)
        {
            if (usdConversionMultiplierDelegate != null)
                usdConversionMultiplier = usdConversionMultiplierDelegate;
            else
                usdConversionMultiplier = new USDConversionMultiplier(DefaultUSDConversionMultiplier);
            this.monetaryValue = new MoneyValue<V>((V)e.GetValue(), cur, usdConversionMultiplier(cur));
            //this.value = this.monetaryValue; //JJHACK: so that GetValue() works without casting
            this.monetaryExpression = e;
        }
        #endregion

        #region Methods
        public override MoneyValue<V> GetValue()
        {
            return monetaryValue;
        }
        public void SetValue(MoneyValue<V> value)
        {
            monetaryValue = value;
            //this.value = this.monetaryValue; //JJHACK: so that GetValue() works without casting
            usdConversionMultiplier = delegate(Currency c) { return value.currencyConversionMultiplier; };
        }
        private static double DefaultUSDConversionMultiplier(Currency currencySymbol)
        {
            if (currencySymbol == Currency.USD)
                return 1.0;
            else
                return 20.0;  // TODO : implement standard currency conversion here
        }
        public override string ToString()
        {
            return GetValue().ToString();
        }
        public override double GetEvaluatedValue(params object[] bindings)
        {
            return GetValue().GetEvaluatedValue(bindings);
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(Money<V>))
                return false;

            return (this.Equals(obj as Money<V>));
        }

        public bool Equals(Money<V> e)
        {
            if (e == null)
                return false;

            return (monetaryValue.Equals(e.monetaryValue));
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + monetaryValue.GetHashCode();
            return hash;
        }
        #endregion
    }
}
