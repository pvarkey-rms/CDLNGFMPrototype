using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class FunctionInvocation<V> : Expression<Value>, IEquatable<FunctionInvocation<V>>
        //where V : class
    {
        #region Inner Types
        public delegate IEvaluatableValue Function(params object[] parameters);
        #endregion

        #region Fields
        string FunctionName;
        object[] parameters;
        Function delegatedFunction;
        #endregion

        #region Constructors
        public FunctionInvocation() : this(CastAndReturnFirstParam, null) { }
        public FunctionInvocation(Function f) : this(f, null) { }
        public FunctionInvocation(string fName) : this(fName, null) { }
        public FunctionInvocation(params object[] parameters) : this(CastAndReturnFirstParam, parameters) { }
        public FunctionInvocation(Function f, params object[] parameters)
        {
            FunctionName = null;
            delegatedFunction = f;
            this.parameters = parameters;
            value = ((delegatedFunction == null || (FunctionName != null && parameters != null)) ? null : new Value(delegatedFunction(parameters)));
        }
        public FunctionInvocation(string fName, params object[] parameters)
        {
            FunctionName = fName;
            delegatedFunction = null;
            this.parameters = parameters;
            switch (fName.ToUpper())
            {
                case "SUM":
                    delegatedFunction = delegate(object[] _parameters)
                    {
                        double sum = 0;
                        foreach (object parameter in _parameters)
                            sum += (double)parameter;
                        return new NumericValue(sum);
                    };
                    break;
                case "MAX":
                    delegatedFunction = delegate(object[] _parameters)
                    {
                        double max = double.MinValue;
                        foreach (object parameter in _parameters)
                            max = Math.Max(max, (double)(parameter));
                        return new NumericValue(max);
                    };
                    break;
                case "MIN":
                    delegatedFunction = delegate(object[] _parameters)
                    {
                        double min = double.MaxValue;
                        foreach (object parameter in _parameters)
                            min = Math.Min(min, (double)(parameter));
                        return new NumericValue(min);
                    };
                    break;
            }
            value = null;
        }

        public FunctionInvocation(FunctionInvocation<V> CopyFrom, object[] NewParameters)
        {
            FunctionName = CopyFrom.FunctionName;
            delegatedFunction = CopyFrom.delegatedFunction;
            this.parameters = NewParameters;
            value = ((delegatedFunction == null || (FunctionName != null && NewParameters != null)) ? null : new Value(delegatedFunction(NewParameters))); ;
        }
        #endregion

        #region Methods
        public override Value GetValue()
        {
            return value;
        }
        public override double GetEvaluatedValue(params object[] bindings)
        {
            if (FunctionName != null)
            {
                return EvaluateFunctionByName(FunctionName, parameters, bindings);
            }
            //if (value != null)
            //    return GetValue().GetEvaluatedValue(bindings);
            //if (delegatedFunction != null)
            //{
            //    SetValue(new Value(delegatedFunction(parameters)));
            //    return GetValue().GetEvaluatedValue(bindings);
            //}
            throw new Exception("Function unspecified!");
        }
        public override void SetValue(IValue<AValue> returnValue)
        {
            value = new NumericValue((returnValue as IEvaluatableValue).GetEvaluatedValue());
        }
        public void SetFunctionName(string functionName)
        {
            FunctionName = functionName;
            delegatedFunction = null;
        }
        public string GetFunctionName()
        {
            return FunctionName;
        }
        public void SetFunctionName(string functionName, params object[] parameters)
        {
            FunctionName = functionName;
            delegatedFunction = null;
            this.parameters = parameters;
        }
        public Function GetFunctionDelegate()
        {
            return delegatedFunction;
        }
        public void SetFunctionDelegate(Function functionDelegate)
        {
            FunctionName = null;
            delegatedFunction = functionDelegate;
        }
        public void SetFunctionDelegate(Function functionDelegate, params object[] parameters)
        {
            FunctionName = null;
            delegatedFunction = functionDelegate;
            this.parameters = parameters;
        }
        public object[] GetParameters()
        {
            return parameters;
        }
        public void SetParameters(params object[] parameters)
        {
            this.parameters = parameters;
        }
        private static IEvaluatableValue CastAndReturnFirstParam(params object[] defaultReturnValue)
        {
            return (IEvaluatableValue)(V)defaultReturnValue[0];
        }
        public override string ToString()
        {
            return delegatedFunction.Method.Name + "(" + string.Join(",", parameters) + ")";
        }
        #endregion

        private static double EvaluateFunctionByName(string FunctionName, object[] parameters, object[] bindings)
        {
            // pre-process parameters (recursively) if they happen to be function calls!
            if (parameters != null)
            {
                object[] ProcessedParameters = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++ )
                {
                    if (parameters[i] is FunctionInvocation<Value>)
                    {
                        ProcessedParameters[i] = ((FunctionInvocation<Value>)parameters[i]).GetEvaluatedValue(bindings);
                    }
                    else if (parameters[i] is FunctionInvocation<double>)
                    {
                        ProcessedParameters[i] = ((FunctionInvocation<double>)parameters[i]).GetEvaluatedValue(bindings);
                    }
                    else
                        ProcessedParameters[i] = parameters[i];
                }
                parameters = ProcessedParameters;
            }

            switch (FunctionName.ToUpper())
            {
                case "SUBJECT":
                    if ((parameters == null) || !((parameters.Length == 1) || (parameters.Length == 2)) || !(parameters[0] is IExpression<Value>))
                        throw new Exception("Incorrect parameter specification for SUBJECT function!");
                    if ((bindings == null) || !(bindings[0] is Dictionary<SimpleExpression<SymbolicValue>, double>))
                        throw new Exception("Incorrect binding specification for SUBJECT function!");
                    IExpression<Value> SUBJECTPercentage = parameters[0] as IExpression<Value>;
                    return SUBJECTPercentage.GetEvaluatedValue()
                                * ((Dictionary<SimpleExpression<SymbolicValue>, double>)bindings[0])[new SymbolicExpression("Subject")] / 100.0;
                case "RCV":
                    if ((parameters == null) || (parameters.Length != 2) || !(parameters[0] is IExpression<Value>) || !(parameters[1] is SymbolicExpression))
                        throw new Exception("Incorrect parameter specification for RCV function!");
                    if ((bindings == null) || !(bindings[0] is Dictionary<SimpleExpression<SymbolicValue>, double>))
                        throw new Exception("Incorrect binding specification for RCV function!");
                    IExpression<Value> RCVPercentage = parameters[0] as IExpression<Value>;
                    SymbolicExpression AffectedOrCovered = parameters[1] as SymbolicExpression;
                    switch (AffectedOrCovered.GetValue().ToString())
                    {
                        case "Covered":
                            return RCVPercentage.GetEvaluatedValue()
                                * ((Dictionary<SimpleExpression<SymbolicValue>, double>)bindings[0])[new SymbolicExpression("RCVCovered")] / 100.0;
                        case "Affected":
                            return RCVPercentage.GetEvaluatedValue()
                                * ((Dictionary<SimpleExpression<SymbolicValue>, double>)bindings[0])[new SymbolicExpression("RCVAffected")] / 100.0;
                        default:
                            throw new Exception("Unknown function exception!");
                    }
                case "WAITINGPERIOD":
                    if ((parameters == null) || (parameters.Length != 2) || !(parameters[0] is IExpression<Value>) || !(parameters[1] is SymbolicExpression))
                        throw new Exception("Incorrect parameter specification for WAITINGPERIOD function!");
                    if ((bindings == null) || !(bindings[0] is Dictionary<SimpleExpression<SymbolicValue>, double>))
                        throw new Exception("Incorrect binding specification for WAITINGPERIOD function!");
                    IExpression<Value> WaitingPeriod = parameters[0] as IExpression<Value>;
                    SymbolicExpression WaitingPeriodTimeUnit = parameters[1] as SymbolicExpression;
                    switch (WaitingPeriodTimeUnit.GetValue().ToString())
                    {
                        case "Days":
                            return (WaitingPeriod.GetEvaluatedValue() / (double)365)
                                * ((Dictionary<SimpleExpression<SymbolicValue>, double>)bindings[0])[new SymbolicExpression("RCVAffected")];
                        case "Months":
                            return (WaitingPeriod.GetEvaluatedValue() / (double)12)
                                * ((Dictionary<SimpleExpression<SymbolicValue>, double>)bindings[0])[new SymbolicExpression("RCVAffected")];
                        default:
                            throw new Exception("Unknown function exception!");
                    }
                case "MAX":
                    if (parameters == null)
                        throw new Exception("Incorrect parameter specification!");
                    
                    double max = double.MinValue;
                    foreach (object parameter in parameters)
                    {
                        if (parameter is double)
                            max = Math.Max(max, (double)parameter);
                        else if (parameter is SimpleExpression<NumericValue>)
                            max = Math.Max(max, ((SimpleExpression<NumericValue>)parameter).GetEvaluatedValue());
                        else if (parameter is SimpleExpression<SymbolicValue>)
                        {
                            if ((bindings == null) || !(bindings[0] is Dictionary<SimpleExpression<SymbolicValue>, double>))
                                throw new Exception("Incorrect binding specification!");
                            Dictionary<SimpleExpression<SymbolicValue>, double> binding = bindings[0] as Dictionary<SimpleExpression<SymbolicValue>, double>;
                            if (!binding.ContainsKey(parameter as SimpleExpression<SymbolicValue>))
                                throw new Exception("Insufficient bindings for parameters!");
                            max = Math.Max(max, (double)binding[parameter as SimpleExpression<SymbolicValue>]);
                        }
                        else
                            throw new Exception("Incorrect parameter specification!");
                    }
                    return max;
                case "MIN":
                    if (parameters == null)
                        throw new Exception("Incorrect parameter specification!");

                    double min = double.MaxValue;
                    foreach (object parameter in parameters)
                    {
                        if (parameter is double)
                            min = Math.Min(min, (double)parameter);
                        else if (parameter is SimpleExpression<NumericValue>)
                            min = Math.Min(min, ((SimpleExpression<NumericValue>)parameter).GetEvaluatedValue());
                        else if (parameter is SimpleExpression<SymbolicValue>)
                        {
                            if ((bindings == null) || !(bindings[0] is Dictionary<SimpleExpression<SymbolicValue>, double>))
                                throw new Exception("Incorrect binding specification!");
                            Dictionary<SimpleExpression<SymbolicValue>, double> binding = bindings[0] as Dictionary<SimpleExpression<SymbolicValue>, double>;
                            if (!binding.ContainsKey(parameter as SimpleExpression<SymbolicValue>))
                                throw new Exception("Insufficient bindings for parameters!");
                            min = Math.Min(min, (double)binding[parameter as SimpleExpression<SymbolicValue>]);
                        }
                        else
                            throw new Exception("Incorrect parameter specification!");
                    }
                    return min;
                case "SUM":
                    if (parameters == null)
                        throw new Exception("Incorrect parameter specification!");
                    
                    double sum = 0.0;
                    foreach (object parameter in parameters)
                    {
                        if (parameter is double)
                            sum += (double)parameter;
                        else if (parameter is SimpleExpression<NumericValue>)
                            sum += ((SimpleExpression<NumericValue>)parameter).GetEvaluatedValue();
                        else if (parameter is SimpleExpression<SymbolicValue>)
                        {
                            if ((bindings == null) || !(bindings[0] is Dictionary<SimpleExpression<SymbolicValue>, double>))
                                throw new Exception("Incorrect binding specification!");
                            Dictionary<SimpleExpression<SymbolicValue>, double> binding = bindings[0] as Dictionary<SimpleExpression<SymbolicValue>, double>;
                            if (!binding.ContainsKey(parameter as SimpleExpression<SymbolicValue>))
                                throw new Exception("Insufficient bindings for parameters!");
                            sum += (double)binding[parameter as SimpleExpression<SymbolicValue>];
                        }
                        else
                            throw new Exception("Incorrect parameter specification!");
                    }
                    return sum;
                default:
                    throw new Exception("Unknown function exception!");
            }
        }

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(FunctionInvocation<V>))
                return false;

            FunctionInvocation<V> e = obj as FunctionInvocation<V>;

            return this.Equals(e);
        }

        public bool Equals(FunctionInvocation<V> e)
        {
            if (e == null)
            {
                return false;
            }

            bool IsEqual = true;

            if (FunctionName != null)
                IsEqual &= FunctionName.Equals(e.FunctionName);
            else if (e.FunctionName != null)
                return false;

            if (parameters != null)
            {
                if ((e.parameters == null) || (parameters.Length != e.parameters.Length))
                    return false;
                for (int i = 0; i < parameters.Length; i++)
                    IsEqual &= parameters[i].Equals(e.parameters[i]);
            }
            else if (e.parameters != null)
                return false;

            if (delegatedFunction != null)
                IsEqual &= delegatedFunction.Equals(e.delegatedFunction);
            else if (e.delegatedFunction != null)
                return false;

            return IsEqual;
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + ((delegatedFunction != null) ? delegatedFunction.GetHashCode() : 41);
            hash = hash * 37 + ((FunctionName != null) ? FunctionName.GetHashCode() : 41);
            if (parameters == null || parameters.Length == 0)
                hash = hash * 37 + 41;
            else
                foreach (object parameter in parameters)
                    hash = hash * 37 + parameter.GetHashCode();
            return hash;
        }
        #endregion
    }
}
