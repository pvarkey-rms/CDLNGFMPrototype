using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public static class TermParser
    {
        public static Deductible GetDedForTerm(Dictionary<string, object> termDictionary, Declarations declarations)
        {
            bool isPerRisk = false;
            TermValueType dedPtype;
            if (termDictionary["PerRisk"].ToString() == "True")
                isPerRisk = true;
            int index = Convert.ToInt32(termDictionary["Index"]);
            bool franchise = Convert.ToBoolean(termDictionary["IsFranchise"]);
            DedInteractionType dedInterType = (DedInteractionType)Enum.Parse(typeof(DedInteractionType), termDictionary["Interaction"].ToString());
            if (dedInterType == DedInteractionType.MIN & declarations.MinimumAbsorbingDed)
                dedInterType = DedInteractionType.Absorbing;

            Value dedValue;
            TimeBasis timeBasis;

            GetInfoFromJSONTermExp(termDictionary, out dedValue, out dedPtype, out timeBasis);

            return new Deductible(franchise, dedInterType, dedValue, isPerRisk, dedPtype);
        }

        public static Limit GetLimitForTerm(Dictionary<string, object> termDictionary, Declarations declarations)
        {
            Value limValue;
            TermValueType limPtype;
            //if (!TryConvert(termDictionary["Value"], out limValue))
            //    throw new InvalidOperationException("Limit is not coded as monetary amount in CDL, cannot be supported at this time...");
            bool isPerRisk = false;
            if (termDictionary["PerRisk"].ToString() == "True")
                isPerRisk = true;
            TimeBasis timeBasis;

            GetInfoFromJSONTermExp(termDictionary, out limValue, out limPtype, out timeBasis);

            return new Limit(declarations.GroundUpSublimits, limValue, isPerRisk, limPtype);
        }

        public static Cover GetCoverForTerm(Dictionary<string, object> coverDictionary)
        {
            int index = Convert.ToInt32(coverDictionary["Index"]);
            //bool franchise = Convert.ToBoolean(coverDictionary["IsFranchise"]);

            bool unlimited = false;
            bool franchise = false;
            if (coverDictionary.ContainsKey("isFranchise"))
                franchise = true;

            //if (!GetValueFromJSONExp(Convert.ToBoolean(coverDictionary["IsFranchise"]), out franchise))
            //    throw new InvalidOperationException("IsGranchise with index " + index.ToString() + " is not coded as monetary amount in CDL, cannot be supported at this time...");

            TermValueType vType = TermValueType.Numeric;
            Value attpoint;
            Value limit = null;
            Value proRata;

            string name = coverDictionary["Label"].ToString();

            if (coverDictionary.ContainsKey("AttachmentSpecification"))
                GetInfoFromJSONCoverExp(coverDictionary["AttachmentSpecification"], out attpoint, out vType);
            else
                attpoint = new MonetaryValue(0);

            if (coverDictionary.ContainsKey("LimitSpecification"))
                GetInfoFromJSONCoverExp(coverDictionary["LimitSpecification"], out limit, out vType);
            else
                unlimited = true; ;                

            if (coverDictionary.ContainsKey("Participation"))
                GetInfoFromJSONCoverExp(coverDictionary["Participation"], out proRata, out vType);
            else
                proRata = new PercentValue(100);

            TimeBasis attTimeBasis;
            TimeBasis limitTimeBasis;

            if (coverDictionary.ContainsKey("AttachmentTimeBasis"))
                attTimeBasis = (TimeBasis)Enum.Parse(typeof(TimeBasis), coverDictionary["AttachmentTimeBasis"].ToString());
            else
                attTimeBasis = TimeBasis.Occurrence;

            if (coverDictionary.ContainsKey("LimitTimeBasis"))
                limitTimeBasis = (TimeBasis)Enum.Parse(typeof(TimeBasis), coverDictionary["LimitTimeBasis"].ToString());
            else
                limitTimeBasis = TimeBasis.Occurrence;

            return new Cover(name, franchise, attpoint, limit, (PercentValue)proRata, unlimited, attTimeBasis, limitTimeBasis, vType);
        }

        private static void GetInfoFromJSONTermExp(object obj, out Value result, out TermValueType termValType, out TimeBasis timeBasis)
        {
            Dictionary<string, object> expDict = obj as Dictionary<string, object>;

            if (expDict != null)
            {
                GetInfoFromJSONExp(expDict, out result, out termValType);
                timeBasis = (TimeBasis)Enum.Parse(typeof(TimeBasis), expDict["TimeBasis"].ToString());
            }
            else
                throw new JSONParseException("Term/Cover dictionary not in proper format");
        }

        private static void GetInfoFromJSONCoverExp(object obj, out Value result, out TermValueType termValType)
        {
            Dictionary<string, object> expDict = obj as Dictionary<string, object>;

            if (expDict != null)
            {
                GetInfoFromJSONExp(expDict, out result, out termValType);
            }
            else
                throw new JSONParseException("Term/Cover dictionary not in proper format");
        }

        private static void GetInfoFromJSONExp(Dictionary<string, object> expDict, out Value result, out TermValueType termValType)
        {
            Value parsedValue = RecursiveParseJSONValue(expDict);
            if (parsedValue is FunctionValue)
            {
                FunctionValue function = parsedValue as FunctionValue;
                if (function.Function == FunctionType.RCV)
                {
                    string RCVarg2 = (function.Arguments[1] as SymbolicValue).Symbol;
                    if (RCVarg2 == "Covered")
                        termValType = TermValueType.PercentCovered;
                    else if (RCVarg2 == "Affected")
                        termValType = TermValueType.PercentAffected;
                    else
                        throw new JSONParseException("Cannot handle RCV with second argument: " + RCVarg2);
                    result = (PercentValue)function.Arguments[0];
                }
                else if (function.Function == FunctionType.Subject)
                {
                    termValType = TermValueType.PercentLoss;
                    result = (PercentValue)function.Arguments[0];
                }
                else if (function.Function == FunctionType.Min || function.Function == FunctionType.Sum || function.Function == FunctionType.Max)
                {
                    termValType = TermValueType.PayFunction;
                    result = function;
                }   
                else
                {                                        
                    throw new JSONParseException("Cannot currently process functions of type: " + function.Function.ToString());
                }                   
            }
            else
            {
                termValType = TermValueType.Numeric;
                result = parsedValue;
            }             
        }

        private static Value RecursiveParseJSONValue(Dictionary<string, object> valueDict)
        {
            //Handle simple expressions
            if (valueDict["ExpressionType"].ToString() == "SimpleExpression<NumericValue>")
            {
                return PasreSimpleExpression(valueDict);
            }
            //Handle percent expressions
            else if (valueDict["ExpressionType"].ToString() == "Percentage<NumericValue>")
            {
                return ParsePercentExpression(valueDict);
            }
            //Handle symbolic expression
            else if (valueDict["ExpressionType"].ToString() == "SymbolicExpression")
            {
                return ParseSymbolicExpression(valueDict);
            }
            //Handle function expressions
            else if (valueDict["ExpressionType"].ToString() == "FunctionInvocation<Value>")
            {
                if (valueDict["ValueType"].ToString() == "Value")
                {
                    object[] argumentObjects = valueDict["Value"] as object[];
                    FunctionType functionType;
                    try
                    {
                        functionType = (FunctionType)Enum.Parse(typeof(FunctionType), valueDict["FunctionName"].ToString());
                    }
                    catch (ArgumentException argE) { throw new JSONParseException("Error parsing Function Name: " + argE.Message); }

                    FunctionValue function = new FunctionValue(functionType);

                    foreach (object argumentObject in argumentObjects)
                    {
                        Dictionary<string, object> argumentValueDict = argumentObject as Dictionary<string, object>;
                        function.AddArgument(RecursiveParseJSONValue(argumentValueDict));
                    }

                    return function;
                }
                else
                    throw new JSONParseException("Cannot currenlty handle Function expressions of type: " + valueDict["ValueType"].ToString());
            }
            //Handle Pay Function 
            else if (valueDict.ContainsKey("PAY"))
            {
                if (valueDict["ExpressionType"].ToString() == "FunctionInvocation<double>")
                {
                    FunctionType functionType;
                    if (valueDict.ContainsKey("FunctionName"))
                    {
                        try
                        {
                            functionType = (FunctionType)Enum.Parse(typeof(FunctionType), valueDict["FunctionName"].ToString());
                        }
                        catch (ArgumentException argE) { throw new JSONParseException("Error parsing Function Name: " + argE.Message); }
                        FunctionValue function = new FunctionValue(functionType);

                        if (valueDict["FunctionParameterValueType"].ToString() == "IExpression<Value>")
                        {
                            //need to parse the function parameters
                            if (valueDict["ValueType"].ToString() == "Value")
                            {
                                object[] argumentObjects = valueDict["Value"] as object[];
                                foreach (object argumentObject in argumentObjects)
                                {
                                    Dictionary<string, object> argumentValueDict = argumentObject as Dictionary<string, object>;
                                    if (argumentValueDict["ValueType"].ToString() == "SymbolicValue")
                                    {
                                        function.AddArgument(new SymbolicValue(argumentValueDict["ValueType"].ToString()));
                                    }
                                    else if (argumentValueDict["ValueType"].ToString() == "NumericValue")
                                    {
                                        function.AddArgument(new MonetaryValue(Convert.ToDouble(argumentValueDict["Value"].ToString())));
                                    }
                                }
                            }
                        }
                        return function;
                    }
                    else
                        throw new JSONParseException("Cannot currenlty handle Function expressions of type: " + valueDict["ValueType"].ToString());
                }
                else
                    throw new JSONParseException("Cannot currently handle expression of type: " + valueDict["ExpressionType"].ToString());
            }
            else
                throw new JSONParseException("Cannot currently handle expression of type: " + valueDict["ExpressionType"].ToString());
        }
            
        private static Value PasreSimpleExpression(Dictionary<string, object> valueDict)
        {
            if (valueDict["ValueType"].ToString() == "NumericValue" || valueDict["ValueType"].ToString() == "MoneyValue<NumericValue>")
            {
                return new MonetaryValue(Convert.ToDouble(valueDict["Value"]));
            }
            else
                throw new JSONParseException("Cannot currenlty handle simple expressions of type: " + valueDict["ValueType"].ToString());           
        }

        private static PercentValue ParsePercentExpression(Dictionary<string, object> valueDict)
        {
            if (valueDict["ValueType"].ToString() == "NumericValue")
                {
                    return new PercentValue(Convert.ToDouble(valueDict["Value"]));
                }
                else
                    throw new JSONParseException("Cannot currenlty handle percent expressions of type: " + valueDict["ValueType"].ToString());
        }

        private static SymbolicValue ParseSymbolicExpression(Dictionary<string, object> valueDict)
        {
            if (valueDict["ValueType"].ToString() == "SymbolicValue")
            {
                return new SymbolicValue((valueDict["Value"]).ToString());
            }
            else
                throw new JSONParseException("Cannot currenlty handle Symbolic expressions of type: " + valueDict["ValueType"].ToString());
        }

        private static bool TryConvert(object obj, out double result)
        {
            bool success = true;
            result = 0;
            try
            {
                result = Convert.ToDouble(obj);
            }
            catch (InvalidCastException e)
            {
                success = false;
            }

            return success;
        }
     
    }

    public class JSONParseException : InvalidOperationException
    {
        public JSONParseException(string message)
            : base(message)
        {

        }

    }
}
