using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public enum ArithmeticOperator
    {
        PLUS, MINUS, MULTIPLY, DIVIDE
    }

    public class ArithmeticExpression : Expression<Value>
    {
        ArithmeticExpression LeftOperandExpression = null;
        ArithmeticOperator Operator;
        ArithmeticTerm RightOperandTerm;

        #region Constructors
        public ArithmeticExpression(double amount) : this(new ArithmeticTerm(amount)) { }

        public ArithmeticExpression(string SymbolicAmount) : this(new ArithmeticTerm(SymbolicAmount)) { }

        public ArithmeticExpression(ArithmeticTerm _RightOperandTerm)
        {
            RightOperandTerm = _RightOperandTerm;
        }

        public ArithmeticExpression(double _LeftOperandExpression, ArithmeticOperator _Operator,
            double _RightOperandTerm)
            : this(new ArithmeticExpression(_LeftOperandExpression), _Operator, new ArithmeticTerm(_RightOperandTerm)) { }

        public ArithmeticExpression(double _LeftOperandExpression, ArithmeticOperator _Operator,
            string _RightOperandTerm)
            : this(new ArithmeticExpression(_LeftOperandExpression), _Operator, new ArithmeticTerm(_RightOperandTerm)) { }

        public ArithmeticExpression(string _LeftOperandExpression, ArithmeticOperator _Operator,
            double _RightOperandTerm)
            : this(new ArithmeticExpression(_LeftOperandExpression), _Operator, new ArithmeticTerm(_RightOperandTerm)) { }

        public ArithmeticExpression(string _LeftOperandExpression, ArithmeticOperator _Operator,
            string _RightOperandTerm)
            : this(new ArithmeticExpression(_LeftOperandExpression), _Operator, new ArithmeticTerm(_RightOperandTerm)) { }

        public ArithmeticExpression(ArithmeticExpression _LeftOperandExpression, ArithmeticOperator _Operator,
            ArithmeticTerm _RightOperandTerm)
        {
            if (_Operator != ArithmeticOperator.PLUS && _Operator != ArithmeticOperator.MINUS)
                throw new InvalidOperationException();
            LeftOperandExpression = _LeftOperandExpression;
            Operator = _Operator;
            RightOperandTerm = _RightOperandTerm;
        }
        #endregion

        #region IExpression Overides
        public override double GetEvaluatedValue(params object[] bindings)
        {
            if (LeftOperandExpression == null)
                return RightOperandTerm.GetEvaluatedValue(bindings);
            else
            {
                if (Operator == ArithmeticOperator.PLUS)
                    return LeftOperandExpression.GetEvaluatedValue(bindings) + RightOperandTerm.GetEvaluatedValue(bindings);
                else if (Operator == ArithmeticOperator.MINUS)
                    return LeftOperandExpression.GetEvaluatedValue(bindings) - RightOperandTerm.GetEvaluatedValue(bindings);
                else
                    throw new InvalidOperationException();
            }
        }
        #endregion
    }

    public class ArithmeticTerm
    {
        ArithmeticTerm LeftOperandTerm = null;
        ArithmeticOperator Operator;
        IExpression<Value> RightOperandFactor;

        #region Constructors
        public ArithmeticTerm(double amount) : this(new NumericExpression(amount)) { }

        public ArithmeticTerm(string SymbolicAmount) : this(new SymbolicExpression(SymbolicAmount)) { }

        public ArithmeticTerm(IExpression<Value> _RightOperandFactor)
        {
            RightOperandFactor = _RightOperandFactor;
        }

        public ArithmeticTerm(double _LeftOperandTerm, ArithmeticOperator _Operator,
            double _RightOperandFactor)
            : this(new ArithmeticTerm(_LeftOperandTerm), _Operator, new NumericExpression(_RightOperandFactor)) { }

        public ArithmeticTerm(double _LeftOperandTerm, ArithmeticOperator _Operator,
            string _RightOperandFactor)
            : this(new ArithmeticTerm(_LeftOperandTerm), _Operator, new SymbolicExpression(_RightOperandFactor)) { }

        public ArithmeticTerm(string _LeftOperandTerm, ArithmeticOperator _Operator,
            double _RightOperandFactor)
            : this(new ArithmeticTerm(_LeftOperandTerm), _Operator, new NumericExpression(_RightOperandFactor)) { }

        public ArithmeticTerm(string _LeftOperandTerm, ArithmeticOperator _Operator,
            string _RightOperandFactor)
            : this(new ArithmeticTerm(_LeftOperandTerm), _Operator, new SymbolicExpression(_RightOperandFactor)) { }

        public ArithmeticTerm(ArithmeticTerm _LeftOperandTerm, ArithmeticOperator _Operator,
            IExpression<Value> _RightOperandFactor)
        {
            if (_Operator != ArithmeticOperator.MULTIPLY && _Operator != ArithmeticOperator.DIVIDE)
                throw new InvalidOperationException();
            LeftOperandTerm = _LeftOperandTerm;
            Operator = _Operator;
            RightOperandFactor = _RightOperandFactor;
        }
        #endregion

        public double GetEvaluatedValue(params object[] bindings)
        {
            if (LeftOperandTerm == null)
                return RightOperandFactor.GetEvaluatedValue(bindings);
            else
            {
                if (Operator == ArithmeticOperator.MULTIPLY)
                    return LeftOperandTerm.GetEvaluatedValue(bindings) * RightOperandFactor.GetEvaluatedValue(bindings);
                else if (Operator == ArithmeticOperator.DIVIDE)
                    return LeftOperandTerm.GetEvaluatedValue(bindings) / RightOperandFactor.GetEvaluatedValue(bindings);
                else
                    throw new InvalidOperationException();
            }
        }
    }
}
