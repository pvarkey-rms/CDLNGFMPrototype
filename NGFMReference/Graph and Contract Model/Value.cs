using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public abstract class Value
    {
        public abstract double Amount {get;}
    }

    public class MonetaryValue :Value
    {
        private static Dictionary<string, double> CurrencyConversions;
        private static string executionCurrency = "USD";
        public static string ExecutionCurrency { set { executionCurrency = value; } }
        private double amount;
        private string currency = "USD";

        public override double Amount 
        { get { return amount * GetConversionRate(); } }

        public MonetaryValue(double _amount)
        {
            amount = _amount;
        }

        public MonetaryValue(double _amount, string _currency)
        {
            amount = _amount;
            currency = _currency;
        }

        private double GetConversionRate()
        {
            return 1;
        }

    }

    public class PercentValue:Value
    {
        private double percent;

        public PercentValue(double _percent)
        {
            percent = _percent;
        }

        public override double Amount
        {
            get { return percent/100; }
        }

    }

    public class SymbolicValue : Value
    {
        public string Symbol{get; private set;}

        public SymbolicValue(string _symbol)
        {
            Symbol = _symbol;
        }

        public override double Amount
        {
            get { throw new NotImplementedException();}
        }

    }

    public class FunctionValue : Value
    {
        private List<Value> arguments;
        public FunctionType Function { get; private set; }
        public List<Value> Arguments { get { return arguments; } }
        public override double Amount {get{throw new NotImplementedException();}}

        public void AddArgument(Value argument)
        {
            arguments.Add(argument);
        }

        public FunctionValue(FunctionType type)
        {
            arguments = new List<Value>();
            Function = type;
        }

        public double EvaluateFunctionValue()
        {
            double result = 0.0;
            if (Function == FunctionType.Max)
            {
                foreach (Value val in arguments)
                {
                    result = Math.Max(result, val.Amount);
                }
            }
            else if (Function == FunctionType.Min)
            {
                foreach (Value val in arguments)
                {
                    result = Math.Min(result, val.Amount);
                }            
            }
            else if (Function == FunctionType.Sum)
            {
                foreach (Value val in arguments)
                {
                    result = result + val.Amount;
                }    
            }

            return result;        
        }
    }

    public enum FunctionType
    {
        RCV,
        Subject,
        Min,
        Max,
        Sum
    }
}
