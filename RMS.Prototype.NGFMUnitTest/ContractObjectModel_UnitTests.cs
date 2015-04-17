using Microsoft.VisualStudio.TestTools.UnitTesting; 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel.Tests
{
    [TestClass]
    public class ContractObjectModel_UnitTests
    {
        [TestMethod]
        public void Test_01_SymbolicExpression()
        {
            string msg = "Error: expected return of {0} should be \"{1}\".";

            string str = "symbolic";
            SymbolicExpression ident = new SymbolicExpression(str);
            Assert.AreEqual(str, ident.ToString(), msg, "SymbolicExpression.ToString()", str);

            str = "symbolic_exspression";
            SimpleExpression<SymbolicValue> SymbolicSE = new SymbolicExpression(str);
            Assert.AreEqual(str, SymbolicSE.GetValue(), msg, "SimpleExpression<SymbolicValue>.GetValue()", str);
        }

        [TestMethod]
        public void Test_02_SymbolicValue()
        {
            string msg = "Error: wrong definition of IEquatable interface for: {0}.";

            SymbolicValue sv = new SymbolicValue("sv");
            IValue<AValue> sive = sv;
            dynamic dsv = new SymbolicValue("sv");
            object osv = new SymbolicValue("sv");

            Assert.IsTrue(sv.Equals(dsv), msg, "SymbolicValue");
            Assert.IsTrue(dsv.Equals(sv), msg, "SymbolicValue");
            Assert.IsTrue(dsv.Equals(sive), msg, "SymbolicValue and IValue<AValue>");
            Assert.IsTrue(osv.Equals(dsv), msg, "SymbolicValue");
            Assert.IsTrue(sv.Equals(osv), msg, "SymbolicValue");
            Assert.IsTrue(osv.Equals(sv), msg, "SymbolicValue");
            Assert.IsTrue(osv.Equals(sive), msg, "SymbolicValue and IValue<AValue>");
            Assert.IsTrue(sive.Equals(osv), msg, "SymbolicValue and IValue<AValue>");
        }

        [TestMethod]
        public void Test_03_AddSimpleExpressionToList()
        {
            SimpleExpression<SymbolicValue> SymbolicSE = new SymbolicExpression("symbolic_exspression");
            IExpression<IValue<AValue>> sSV = SymbolicSE;
            List<IExpression<IValue<AValue>>> aivL = new List<IExpression<IValue<AValue>>>();

            try
            {
                aivL.Add(sSV);
                aivL.Add(SymbolicSE);
            }
            catch (Exception e)
            {
                Assert.Inconclusive("Error: {0}", e.Message);
            }
        }

        [TestMethod]
        public void Test_04_SimpleNumericExpressionValue()
        {
            string msg = "Error: expected value of {0} should be {1}.";

            double d = 6.7;
            NumericExpression n = new NumericExpression(d);
            Assert.AreEqual(d.ToString(), n.GetValue().ToString(), msg, "NumericExpression", d);

            d = 3.4;
            SimpleExpression<NumericValue> NumericSE = new NumericExpression(3.4);
            Assert.AreEqual(d.ToString(), NumericSE.GetValue().ToString(), msg, "SimpleExpression<NumericValue>", d);
        }

        [TestMethod]
        public void Test_05_FunctionInvocation()
        {
            FunctionInvocation<int> f = new FunctionInvocation<int>(
                                    delegate(object[] parameters)
                                    {
                                        int sum = 0;
                                        foreach (object parameter in parameters)
                                            sum += (int)parameter;
                                        return new NumericValue(sum);
                                    },
                                    5, 7, 8);

            Assert.AreEqual("20", f.GetValue().ToString(),
                "Error: wrong execution of delegate function in {0}", "FunctionInvocation<int>");
        }

        [TestMethod]
        public void Test_06_Money()
        {
            SymbolicExpression ident = new SymbolicExpression("symbolic");
            Money<SymbolicValue> moneyFromSymbolicValue = new Money<SymbolicValue>(ident, Currency.GBP);
            Assert.AreEqual("(symbolic*20) GBP", moneyFromSymbolicValue.ToString(),
                "Error in Money from SymbolicExpression.");

            SimpleExpression<SymbolicValue> SymbolicSE = new SymbolicExpression("symbolic_exspression");
            Money<SymbolicValue> moneyFromSymbolicExpression = new Money<SymbolicValue>(SymbolicSE, Currency.GBP);
            Assert.AreEqual("(symbolic_exspression*20) GBP", moneyFromSymbolicExpression.ToString(),
                "Error in Money from SimpleExpression<SymbolicValue>.");

            NumericExpression n = new NumericExpression(6.7);
            Money<NumericValue> moneyFromNumericValue = new Money<NumericValue>(n, Currency.GBP);
            Assert.AreEqual("134 GBP", moneyFromNumericValue.ToString(),
                "Error in Money from NumericExpression.");

            SimpleExpression<NumericValue> NumericSE = new NumericExpression(3.4);
            Money<NumericValue> moneyFromNumericExpression = new Money<NumericValue>(NumericSE, Currency.GBP);
            Assert.AreEqual("68 GBP", moneyFromNumericExpression.GetValue().ToString(),
                "Error in Money from SimpleExpression<NumericValue>.");
        }

        [TestMethod]
        public void Test_07_Percentage()
        {
            SimpleExpression<SymbolicValue> SymbolicSE = new SymbolicExpression("symbolic_exspression");
            Percentage<SymbolicValue> symSEPerc = new Percentage<SymbolicValue>(SymbolicSE);
            Assert.AreEqual("symbolic_exspression%", symSEPerc.ToString(),
                "Error in Percentage<SymbolicValue> from SimpleExpression<SymbolicValue>.");

            NumericExpression n = new NumericExpression(6.7);
            Percentage<NumericValue> numPerc = new Percentage<NumericValue>(n);
            Assert.AreEqual("6.7%", numPerc.ToString(),
                "Error in Percentage<NumericValue> from NumericExpression");
        }

        [TestMethod]
        public void Test_08_Ratio()
        {
            SymbolicExpression ident = new SymbolicExpression("symbolic");
            Ratio<SymbolicValue> symbolicRatio = new Ratio<SymbolicValue>(ident);
            Assert.AreEqual("RATIO(symbolic)", symbolicRatio.ToString(),
                "Error in Ratio<SymbolicValue> from SymbolicExpression.");

            FunctionInvocation<int> f = new FunctionInvocation<int>(
                        delegate(object[] parameters)
                        {
                            int sum = 0;
                            foreach (object parameter in parameters)
                                sum += (int)parameter;
                            return new NumericValue(sum);
                        },
                        5, 7, 8);
            Ratio<Value> funcInvRatio = new Ratio<Value>(f);
            Assert.AreEqual("RATIO(<Test_08_Ratio>b__b(5,7,8))", funcInvRatio.ToString(),
                "Error in Ratio<Value> from FunctionInvocation<int>.");

            NumericExpression n = new NumericExpression(6.7);
            Percentage<NumericValue> numPerc = new Percentage<NumericValue>(n);
            Ratio<NumericValue> percNumRatio = new Ratio<NumericValue>(numPerc);
            Assert.AreEqual("RATIO(6.7%)", percNumRatio.ToString(),
                "Error in Ratio<NumericValue> from Percentage<NumericValue>.");
        }

        [TestMethod]
        public void Test_09_Participation()
        {
            SymbolicExpression ident = new SymbolicExpression("symbolic");
            Ratio<SymbolicValue> symbolicRatio = new Ratio<SymbolicValue>(ident);
            Participation<SymbolicValue> symbolicRatioParticipation = new Participation<SymbolicValue>(symbolicRatio);
            Assert.AreEqual("PARTICIPATION(RATIO(symbolic))", symbolicRatioParticipation.ToString(),
                "Error in Participation<SymbolicValue> from Ratio<SymbolicValue>.");

            FunctionInvocation<int> f = new FunctionInvocation<int>(
                        delegate(object[] parameters)
                        {
                            int sum = 0;
                            foreach (object parameter in parameters)
                                sum += (int)parameter;
                            return new NumericValue(sum);
                        },
                        5, 7, 8);
            Ratio<Value> funcInvRatio = new Ratio<Value>(f);
            Participation<Value> funcInvRatioParticipation = new Participation<Value>(funcInvRatio);
            Assert.AreEqual("PARTICIPATION(RATIO(<Test_09_Participation>b__d(5,7,8)))", funcInvRatioParticipation.ToString(),
                "error in Participation<Value> from Ratio<Value> from FunctionInvocation<int>.");

            NumericExpression n = new NumericExpression(6.7);
            Percentage<NumericValue> numPerc = new Percentage<NumericValue>(n);
            Ratio<NumericValue> percNumRatio = new Ratio<NumericValue>(numPerc);
            Participation<NumericValue> percNumRatioParticipation = new Participation<NumericValue>(percNumRatio);
            Assert.AreEqual("PARTICIPATION(RATIO(6.7%))", percNumRatioParticipation.ToString(),
                "Error in Participation<NumericValue> from Ratio<NumericValue>.");
        }

        [TestMethod]
        public void Test_10_LimitSpecification()
        {
            SimpleExpression<SymbolicValue> SymbolicSE = new SymbolicExpression("symbolic_exspression");
            Money<SymbolicValue> moneyFromSymbolicExpression = new Money<SymbolicValue>(SymbolicSE, Currency.GBP);
            LimitSpecification<MoneyValue<SymbolicValue>> moneyFromSymbolicExpressionPayoutSpec =
                new LimitSpecification<MoneyValue<SymbolicValue>>(moneyFromSymbolicExpression);
            Assert.AreEqual("PAYOUTEXPRESSION((symbolic_exspression*20) GBP)", moneyFromSymbolicExpressionPayoutSpec.ToString(),
                "Error in LimitSpecification<MoneyValue<SymbolicValue>> from Money<SymbolicValue>.");

            NumericExpression n = new NumericExpression(6.7);
            Money<NumericValue> moneyFromNumericValue = new Money<NumericValue>(n, Currency.GBP);
            LimitSpecification<MoneyValue<NumericValue>> moneyFromNumericValuePayoutSpec =
                new LimitSpecification<MoneyValue<NumericValue>>(moneyFromNumericValue, true);
            Assert.AreEqual("PAYOUTEXPRESSION(PAY 134 GBP)", moneyFromNumericValuePayoutSpec.ToString(),
                "Error in LimitSpecification<MoneyValue<NumericValue>> from Money<NumericValue>.");

            Percentage<NumericValue> numPerc = new Percentage<NumericValue>(n);
            LimitSpecification<NumericValue> numPercPayoutSpec =
                new LimitSpecification<NumericValue>(numPerc, true);
            Assert.AreEqual("PAYOUTEXPRESSION(PAY 6.7%)", numPercPayoutSpec.ToString(),
                "Error in LimitSpecification<NumericValue> from Percentage<NumericValue>.");
        }

        [TestMethod]
        public void Test_11_Cover()
        {
            SimpleExpression<SymbolicValue> SymbolicSE = new SymbolicExpression("symbolic_exspression");
            Money<SymbolicValue> moneyFromSymbolicExpression = new Money<SymbolicValue>(SymbolicSE, Currency.GBP);
            LimitSpecification<MoneyValue<SymbolicValue>> moneyFromSymbolicExpressionPayoutSpec =
                new LimitSpecification<MoneyValue<SymbolicValue>>(moneyFromSymbolicExpression);

            Limit<MoneyValue<SymbolicValue>> moneyFromSymbolicExpressionPayout =
                new Limit<MoneyValue<SymbolicValue>>(moneyFromSymbolicExpressionPayoutSpec, 1);

            SimpleExpression<NumericValue> NumericSE = new NumericExpression(3.4);
            Attachment<NumericValue> NumericSEAttachment = new Attachment<NumericValue>(NumericSE);
            NumericExpression n = new NumericExpression(6.7);
            Percentage<NumericValue> numPerc = new Percentage<NumericValue>(n);
            Ratio<NumericValue> percNumRatio = new Ratio<NumericValue>(numPerc);
            Participation<NumericValue> percNumRatioParticipation = new Participation<NumericValue>(percNumRatio);

            Cover<NumericValue, MoneyValue<SymbolicValue>, NumericValue> coverNumSymbNum
                = new Cover<NumericValue, MoneyValue<SymbolicValue>, NumericValue>(
                    percNumRatioParticipation,
                    moneyFromSymbolicExpressionPayout,
                    NumericSEAttachment);
            string str = "COVER \n{\n\tShare: PARTICIPATION(RATIO(6.7%))\n\t_Limit: PAYOUT(PAYOUTEXPRESSION((symbolic_exspression*20) GBP), Occurrence)\n\t_Attachment: XS(3.4, Occurrence, IsItFranchise=False)\n}";
            Assert.AreEqual(str, coverNumSymbNum.ToString(),
                    "Error in Cover<NumericValue, MoneyValue<SymbolicValue>, NumericValue> Constructor.");

            ICover<Value, Value, Value> cNSN
                = (ICover<Value, Value, Value>)coverNumSymbNum;

            SymbolicExpression ident = new SymbolicExpression("symbolic");
            Ratio<SymbolicValue> symbolicRatio = new Ratio<SymbolicValue>(ident);
            Participation<SymbolicValue> symbolicRatioParticipation = new Participation<SymbolicValue>(symbolicRatio);
            Cover<SymbolicValue> coverNumShareNumPayout
                = new Cover<SymbolicValue>(symbolicRatioParticipation, "\"MyCoverLabel\"");
            str = "COVER \"MyCoverLabel\"\n{\n\tShare: PARTICIPATION(RATIO(symbolic))\n\t_Limit: \n\t_Attachment: \n}";
            Assert.AreEqual(str, coverNumShareNumPayout.ToString(),
                "Error in Cover<SymbolicValue> Constructor.");
        }
    }
}
