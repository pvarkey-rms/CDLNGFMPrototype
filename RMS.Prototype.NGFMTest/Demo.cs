using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RMS.ContractObjectModel;

namespace RMS.Prototype.NGFMTest
{
    class Demo
    {
        public static double SergeysBitwiseMax(double a, double b)
        {
            int d = ((int)(b - a) >> 31) & 1;
            return a * d + b * (d ^ 1);
        }

        public static long ElapsedNanoSeconds(Stopwatch watch)
        {
            return watch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;
        }

        static void Main(string[] args)
        {
            //double a = 43.52;
            //double b = 40000566.78234234;
            //double max;

            //Stopwatch sw = Stopwatch.StartNew();


            //sw.Restart();
            //max = SergeysBitwiseMax(a, b);
            //sw.Stop();

            //Console.WriteLine("SergeysBitwiseMax(" + a + ", " + b + ") = " + max + " took " + ElapsedNanoSeconds(sw) + " ns!");


            //sw.Restart();
            //max = Math.Max(a, b);
            //sw.Stop();

            //Console.WriteLine("Math.Max(" + a + ", " + b + ") = " + max + " took " + ElapsedNanoSeconds(sw) + " ns!");


            SymbolicExpression ident = new SymbolicExpression("symbolic");
            Console.WriteLine(ident);

            SimpleExpression<SymbolicValue> SymbolicSE = new SymbolicExpression("symbolic_exspression");
            Console.WriteLine(SymbolicSE.GetValue());

            SymbolicValue sv = new SymbolicValue("sv");
            IValue<AValue> sive = sv;
            dynamic dsv = new SymbolicValue("sv");
            object osv = new SymbolicValue("sv");

            Console.WriteLine(sv.Equals(dsv));
            Console.WriteLine(dsv.Equals(sv));
            Console.WriteLine(dsv.Equals(sive));
            Console.WriteLine(osv.Equals(dsv));
            Console.WriteLine(sv.Equals(osv));
            Console.WriteLine(osv.Equals(sv));
            Console.WriteLine(osv.Equals(sive));
            Console.WriteLine(sive.Equals(osv));

            IExpression<IValue<AValue>> sSV = SymbolicSE;

            List<IExpression<IValue<AValue>>> aivL = new List<IExpression<IValue<AValue>>>();
            aivL.Add(sSV);
            aivL.Add(SymbolicSE);

            NumericExpression n = new NumericExpression(6.7);
            Console.WriteLine(n.GetValue());

            SimpleExpression<NumericValue> NumericSE = new NumericExpression(3.4);
            Console.WriteLine(NumericSE.GetValue());
            
            FunctionInvocation<int> f = new FunctionInvocation<int>(
                                    delegate(object[] parameters)
                                    {
                                        int sum = 0;
                                        foreach (object parameter in parameters)
                                            sum += (int)parameter;
                                        return new NumericValue(sum);
                                    },
                                    5, 7, 8);
            Console.WriteLine("!! FunctionInvocation !! " + f.GetValue());

            // money

            Money<SymbolicValue> moneyFromSymbolicValue = new Money<SymbolicValue>(ident, Currency.GBP);
            Console.WriteLine(moneyFromSymbolicValue.GetValue());

            Money<SymbolicValue> moneyFromSymbolicExpression = new Money<SymbolicValue>(SymbolicSE, Currency.GBP);
            Console.WriteLine(moneyFromSymbolicExpression.GetValue());

            Money<NumericValue> moneyFromNumericValue = new Money<NumericValue>(n, Currency.GBP);
            Console.WriteLine(moneyFromNumericValue.GetValue());

            Money<NumericValue> moneyFromNumericExpression = new Money<NumericValue>(NumericSE, Currency.GBP);
            Console.WriteLine(moneyFromNumericExpression.GetValue());

            // percentage

            Percentage<SymbolicValue> symSEPerc = new Percentage<SymbolicValue>(SymbolicSE);
            Console.WriteLine(symSEPerc);

            Percentage<NumericValue> numPerc = new Percentage<NumericValue>(n);
            Console.WriteLine(numPerc);

            // Ratio

            Ratio<SymbolicValue> symbolicRatio = new Ratio<SymbolicValue>(ident);
            Console.WriteLine(symbolicRatio);

            Ratio<Value> funcInvRatio = new Ratio<Value>(f);
            Console.WriteLine(funcInvRatio);

            Ratio<NumericValue> percNumRatio = new Ratio<NumericValue>(numPerc);
            Console.WriteLine(percNumRatio);

            // participation

            Participation<SymbolicValue> symbolicRatioParticipation = new Participation<SymbolicValue>(symbolicRatio);
            Console.WriteLine(symbolicRatioParticipation);

            Participation<Value> funcInvRatioParticipation = new Participation<Value>(funcInvRatio);
            Console.WriteLine(funcInvRatioParticipation);

            Participation<NumericValue> percNumRatioParticipation = new Participation<NumericValue>(percNumRatio);
            Console.WriteLine(percNumRatioParticipation);

            // _Limit specification

            LimitSpecification<MoneyValue<SymbolicValue>> moneyFromSymbolicExpressionPayoutSpec = 
                new LimitSpecification<MoneyValue<SymbolicValue>>(moneyFromSymbolicExpression);
            Console.WriteLine(moneyFromSymbolicExpressionPayoutSpec);

            LimitSpecification<MoneyValue<NumericValue>> moneyFromNumericValuePayoutSpec =
                new LimitSpecification<MoneyValue<NumericValue>>(moneyFromNumericValue, true);
            Console.WriteLine(moneyFromNumericValuePayoutSpec);

            LimitSpecification<NumericValue> numPercPayoutSpec =
                new LimitSpecification<NumericValue>(numPerc, true);
            Console.WriteLine(numPercPayoutSpec);

            // _Limit

            Limit<MoneyValue<SymbolicValue>> moneyFromSymbolicExpressionPayout
                = new Limit<MoneyValue<SymbolicValue>>(moneyFromSymbolicExpressionPayoutSpec, 1);

            // _Attachment

            Attachment<NumericValue> NumericSEAttachment = new Attachment<NumericValue>(NumericSE);

            // cover

            Cover<NumericValue, MoneyValue<SymbolicValue>, NumericValue> coverNumSymbNum
                = new Cover<NumericValue, MoneyValue<SymbolicValue>, NumericValue>(percNumRatioParticipation, moneyFromSymbolicExpressionPayout, NumericSEAttachment);
            Console.WriteLine(coverNumSymbNum);

            ICover<Value, Value, Value> cNSN
                = (ICover<Value, Value, Value>)coverNumSymbNum;

            Cover<SymbolicValue> coverNumShareNumPayout 
                = new Cover<SymbolicValue>(symbolicRatioParticipation, "\"MyCoverLabel\"");
            Console.WriteLine(coverNumShareNumPayout);

            Console.ReadKey();
        }
    }
}
