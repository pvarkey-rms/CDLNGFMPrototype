using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

namespace RMS.ContractObjectModel
{
    public abstract class AppConfig : IDisposable
    {
        public static AppConfig Change(string path)
        {
            return new ChangeAppConfig(path);
        }

        public abstract void Dispose();

        private class ChangeAppConfig : AppConfig
        {
            private readonly string oldConfig =
                AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE").ToString();

            private bool disposedValue;

            public ChangeAppConfig(string path)
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", path);
                //AppDomain.CurrentDomain.SetData("APPBASE", );
                ResetConfigMechanism();
            }

            public override void Dispose()
            {
                if (!disposedValue)
                {
                    AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", oldConfig);
                    ResetConfigMechanism();


                    disposedValue = true;
                }
                GC.SuppressFinalize(this);
            }

            private static void ResetConfigMechanism()
            {
                typeof(ConfigurationManager)
                    .GetField("s_initState", BindingFlags.NonPublic |
                                             BindingFlags.Static)
                    .SetValue(null, 0);

                typeof(ConfigurationManager)
                    .GetField("s_configSystem", BindingFlags.NonPublic |
                                                BindingFlags.Static)
                    .SetValue(null, null);

                typeof(ConfigurationManager)
                    .Assembly.GetTypes()
                    .Where(x => x.FullName ==
                                "System.Configuration.ClientConfigPaths")
                    .First()
                    .GetField("s_current", BindingFlags.NonPublic |
                                           BindingFlags.Static)
                    .SetValue(null, null);
            }
        }
    }

    /// <summary>
    /// An abstraction for "ground-up" sampled losses (or, damage ratios)
    /// </summary>
    public class Loss
    {
        public static Dictionary<SymbolicValue, HashSet<SymbolicValue>> COLEquivalencyMap;

        static Loss()
        {
            COLEquivalencyMap = new Dictionary<SymbolicValue, HashSet<SymbolicValue>>();

            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(directoryName);

            using (AppConfig.Change(directoryName + "\\App.config"))
            {
                string[] arr = ConfigurationManager.AppSettings["CauseOfLossEquivalency"]
                    .Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (null != arr && arr.Length > 0)
                {
                    foreach (string str in arr)
                    {
                        string[] elem = str.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        if (null != elem && elem.Length > 0)
                        {
                            if (!COLEquivalencyMap.ContainsKey(elem[0]))
                                COLEquivalencyMap.Add(elem[0], new HashSet<SymbolicValue>());
                            COLEquivalencyMap[elem[0]].Add(elem[1]);
                        }
                    }
                }
            }
        }

        // TODO: change to List<Tuple<SymbolicValie, double>> to ensure that number of samples are the same irrespective of cause of loss
        public Dictionary<SymbolicValue, List<double>> AmountByCOL { get; private set; }

        //public Dictionary<string, Dictionary<DateTime, double>> GUByCOLByTime { get; private set; }
        
        public bool ContainsCOL(SymbolicValue COL)
        {
            if (AmountByCOL.ContainsKey(COL))
                return true;

            if (!COLEquivalencyMap.ContainsKey(COL))
                return false;

            foreach (SymbolicValue EquivalentCOL in COLEquivalencyMap[COL])
                if (AmountByCOL.ContainsKey(EquivalentCOL))
                    return true;

            return false;
        }

        Dictionary<SymbolicValue, List<double>> SamplesForCOL = new Dictionary<SymbolicValue,List<double>>();
        public List<double> GetSamplesForCOL(SymbolicValue COL)
        {
            if (SamplesForCOL.ContainsKey(COL))
                return SamplesForCOL[COL];

            IEnumerable<double> samples = null;

            if (AmountByCOL.ContainsKey(COL))
                samples = AmountByCOL[COL];

            if (!COLEquivalencyMap.ContainsKey(COL))
            {
                SamplesForCOL.Add(COL, samples.ToList<double>());

                return SamplesForCOL[COL];
            }

            foreach (SymbolicValue EquivalentCOL in COLEquivalencyMap[COL])
            {
                if (EquivalentCOL.Equals(COL))
                    continue;
                if (AmountByCOL.ContainsKey(EquivalentCOL))
                {
                    if (samples == null)
                        samples = AmountByCOL[EquivalentCOL];
                    else
                        samples = samples.Zip(AmountByCOL[EquivalentCOL], (a, b) => a + b);
                }
            }

            SamplesForCOL.Add(COL, samples.ToList<double>());

            return SamplesForCOL[COL];
        }

        Dictionary<SymbolicValue, double> TotalGUForCOL = new Dictionary<SymbolicValue,double>();
        public double this[SymbolicValue COL]
        {
            get 
            {
                if (TotalGUForCOL.ContainsKey(COL))
                    return TotalGUForCOL[COL];

                double total = 0.0;

                if (AmountByCOL.ContainsKey(COL))
                    total += AmountByCOL[COL].Zip(FactorArray, (x, y) => x * y).Sum();
                // @FACTORARRAY REPLACE
                //if (AmountByCOL.ContainsKey(COL))
                //    total += AmountByCOL[COL].Sum() * Factor;

                if (COLEquivalencyMap.ContainsKey(COL))
                {
                    foreach (SymbolicValue EquivalentCOL in COLEquivalencyMap[COL])
                    {
                        if (EquivalentCOL.Equals(COL))
                            continue;
                        if (AmountByCOL.ContainsKey(EquivalentCOL))
                        {
                            total += AmountByCOL[EquivalentCOL].Zip(FactorArray, (x, y) => x * y).Sum();
                            // @FACTORARRAY REPLACE
                            //total += AmountByCOL[EquivalentCOL].Sum() * Factor;
                        }
                    }
                }

                TotalGUForCOL.Add(COL, total);

                return TotalGUForCOL[COL];
            }
        }

        // TODO: delete
        public int NumBuildings { get; set; }

        /// <summary>
        /// Multi-Building Scaling Factor
        /// </summary>
        public double Factor { get; set; }
        List<double> _FactorArray;
        bool IsArrayified = false;
        double[] FactorArrayArraified;
        public double[] FactorArray 
        {
            get
            {
                if (!IsArrayified)
                {
                    if (_FactorArray != null)
                        FactorArrayArraified = _FactorArray.ToArray();
                    IsArrayified = true;
                }
                return FactorArrayArraified;
            }
        }

        public DateTime Timestamp { get; private set; }
        public IEnumerable<SymbolicValue> CausesOfLoss
        {
            get
            {
                return AmountByCOL.Keys;
            }
        }
        // TODO: delete
        public double Amount
        {
            get
            {
                double TotalAmount = 0.0;
                foreach (SymbolicValue COL in AmountByCOL.Keys)
                {
                    TotalAmount += this[COL];
                    // @FACTORCHANGES REPLACE
                    //TotalAmount += Loss.WeightedSum(AmountByCOL[COL], NumBuildings);
                    //if (NumBuildings < AmountByCOL[COL].Count)
                    //    throw new Exception("Number of buildings is lesser than size of multi-building vector for COL " + COL.ToString() + "!");
                    //int TempNumBuildings = NumBuildings;
                    //int SamplesCount = AmountByCOL[COL].Count;
                    //foreach (double SampledLoss in AmountByCOL[COL])
                    //{
                    //    int NumBuildingsPerSample = (int)Math.Ceiling((double)TempNumBuildings / (double)SamplesCount--);
                    //    TotalAmount += (SampledLoss * NumBuildingsPerSample);
                    //    TempNumBuildings -= NumBuildingsPerSample;
                    //}
                }
                return TotalAmount;
            }
        }

        public static double WeightedSum(List<double> samples, int NumBuildings)
        {
            double WeightedSum = 0.0;
            if (NumBuildings < samples.Count)
                throw new Exception("Number of buildings is lesser than size of multi-building sample vector!");
            int TempNumBuildings = NumBuildings;
            int SamplesCount = samples.Count;
            foreach (double SampledLoss in samples)
            {
                int NumBuildingsPerSample = (int)Math.Ceiling((double)TempNumBuildings / (double)SamplesCount--);
                WeightedSum += (SampledLoss * NumBuildingsPerSample);
                TempNumBuildings -= NumBuildingsPerSample;
            }
            return WeightedSum;
        }

        public static double[] WeightedSplit(double Aggregate, List<double> samples, int NumBuildings)
        {
            if (NumBuildings < samples.Count)
                throw new Exception("Number of buildings is lesser than size of multi-building sample vector!");

            double[] WeightedSplit = new double[NumBuildings];

            double WeightedSum = Loss.WeightedSum(samples, NumBuildings);
                        
            int TempNumBuildings = NumBuildings;
            int SamplesCount = samples.Count;
            int i = 0;
            foreach (double SampledLossPerBuilding in samples)
            {
                int NumBuildingsPerSample = (int)Math.Ceiling((double)TempNumBuildings / (double)SamplesCount--);
                double Split = Aggregate * (SampledLossPerBuilding / WeightedSum);
                for (int j = 0; j < NumBuildingsPerSample; j++)
                {
                    WeightedSplit[i + j] = Split;
                }
                i += NumBuildingsPerSample;
                TempNumBuildings -= NumBuildingsPerSample;
            }
            return WeightedSplit;
        }

        #region Constructors
        public Loss(DateTime _Timestamp, string sCOL, List<double> _GULossVector, double _Factor = 1.0)
        {
            Timestamp = _Timestamp;

            List<double> GULossVector = _GULossVector.Clone();

            AmountByCOL = new Dictionary<SymbolicValue, List<double>>();
            AmountByCOL.Add(sCOL, GULossVector);
            SamplesForCOL.Remove(sCOL);
            TotalGUForCOL.Remove(sCOL);

            //GUByCOLByTime = new Dictionary<string, Dictionary<DateTime, double>>();
            //if (!GUByCOLByTime.ContainsKey(sCOL))
            //    GUByCOLByTime.Add(sCOL, new Dictionary<DateTime, double>());
            //if (!GUByCOLByTime[sCOL].ContainsKey(_Timestamp))
            //    GUByCOLByTime[sCOL].Add(_Timestamp, GULossVector.Sum() * _Factor);
            //else
            //    GUByCOLByTime[sCOL][_Timestamp] += (GULossVector.Sum() * _Factor);

            // Default number of buildings = number of samples
            int NumSamples = 1;
            foreach (List<double> SampledLoss in AmountByCOL.Values)
            {
                NumSamples = SampledLoss.Count;
                break;
            }
            NumBuildings = NumSamples;

            Factor = _Factor;
            _FactorArray = new List<double>(Enumerable.Repeat(Factor, NumSamples));
            IsArrayified = false;
        }
        #endregion

        public void Append(DateTime _Timestamp, string COL, List<double> _GULossVector, double _Factor = 1.0)
        {
            if (_Timestamp < Timestamp)
                Timestamp = _Timestamp;

            if (!AmountByCOL.ContainsKey(COL))
            {
                AmountByCOL.Add(COL, _GULossVector.Clone());
            }
            else
            {
                AmountByCOL[COL].AddRange(_GULossVector);
                if (AmountByCOL[COL].Count > _FactorArray.Count)
                {
                    _FactorArray.AddRange(Enumerable.Repeat(_Factor, _GULossVector.Count));
                    IsArrayified = false;
                    //FactorArray = FactorArray.Concat(Enumerable.Repeat(_Factor, GULossVector.Count)).ToArray();
                    NumBuildings += _GULossVector.Count;
                }
            }
            SamplesForCOL.Remove(COL);
            TotalGUForCOL.Remove(COL);

            //if (!GUByCOLByTime.ContainsKey(COL))
            //    GUByCOLByTime.Add(COL, new Dictionary<DateTime, double>());
            //if (!GUByCOLByTime[COL].ContainsKey(_Timestamp))
            //    GUByCOLByTime[COL].Add(_Timestamp, GULossVector.Sum() * _Factor);
            //else
            //    GUByCOLByTime[COL][_Timestamp] += (GULossVector.Sum() * _Factor);

            Factor = _Factor;
        }

        public void AddOrReplaceAmountByCOL(Loss loss)
        {
            foreach (var kv in loss.AmountByCOL)
                if (!AmountByCOL.ContainsKey(kv.Key))
                    AmountByCOL.Add(kv.Key, kv.Value);
                else
                    AmountByCOL[kv.Key] = AmountByCOL[kv.Key].Zip(kv.Value, (a, b) => a + b).ToList<double>();
        }

        public override string ToString()
        {
            StringBuilder stringified = new StringBuilder();
            stringified.Append("@ " + Timestamp.ToString() + ": ");
            foreach (SymbolicValue COL in AmountByCOL.Keys)
            {
                stringified.Append("{(");
                foreach (double LossSample in AmountByCOL[COL])
                {
                    stringified.Append(LossSample + ", ");
                }
                stringified.Remove(stringified.Length-2, 2);
                stringified.Append(") BY " + COL + "}, ");
            }
            stringified.Remove(stringified.Length - 2, 2);
            return stringified.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (this == obj)
                return true;
            if (!(obj is Loss))
                return false;
            Loss that = obj as Loss;
            return Equals(this.AmountByCOL, that.AmountByCOL) && 
                (this.Timestamp.Equals(that.Timestamp)) && (this.CausesOfLoss.Equals(that.CausesOfLoss));
        }

        private static bool Equals(Dictionary<SymbolicValue, List<double>> SomeAmountByCOL, Dictionary<SymbolicValue, List<double>> OtherAmountByCOL)
        {
            if ((SomeAmountByCOL == null) && (OtherAmountByCOL == null))
                throw new NullReferenceException("Null values cannot be compared for equality!");
            if ((SomeAmountByCOL == null) || (OtherAmountByCOL == null))
                return false;
            if (!SomeAmountByCOL.Count.Equals(OtherAmountByCOL.Count))
                return false;
            foreach (SymbolicValue COL in SomeAmountByCOL.Keys)
            {
                if (!OtherAmountByCOL.ContainsKey(COL))
                    return false;
                if (SomeAmountByCOL[COL].Count != OtherAmountByCOL[COL].Count)
                    return false;
                for (int i = 0; i < SomeAmountByCOL[COL].Count; i++)
                {
                    if (SomeAmountByCOL[COL][i] != OtherAmountByCOL[COL][i])
                        return false;
                }
            }
            return true;
        }
    }

    static partial class ObjectUtilities
    {
        public static List<double> Clone(this List<double> t)
        {
            List<double> kop = new List<double>(t.Count);
            int x;
            for (x = 0; x < t.Count; x++)
            {
                kop.Add(t[x]);
            }
            return kop;
        }
    }
}
