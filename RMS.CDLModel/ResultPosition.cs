using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class ResultPosition
    {
        public double TotalGULoss { set; get; }

        public Dictionary<long, Dictionary<string, double>> GULossByRITE { set; get; }

        public Dictionary<long, Dictionary<string, double>> GULossByRiskItem { set; get; }

        public double PayOut { set; get; }

        public Dictionary<long, Dictionary<string, double>> RITEAllocation { set; get; }

        public Dictionary<long, Dictionary<string, double>> RiskItemAllocation { set; get; }

        public SortedDictionary<DateTime, double> TimeAllocation { set; get; }

        public SortedDictionary<DateTime, double> InputTimeSeries { set; get; }

        public List<Tuple<SortedDictionary<DateTime, double>, ResultPosition>> ResultPositionsByHoursClauseOccurrences { set; get; }

        public List<Tuple<HoursClause, HoursClauseOutput>> HoursClauseResults { set; get; }
        
        // TODO : deprecate
        public TimeSpan HoursClause { set; get; }

        public ResultPosition()
        {
            PayOut = 0.0;
        }

        // TODO : deprecate
        public bool IsInsideHoursClause(DateTime dt)
        {
            return ((null != ResultPositionsByHoursClauseOccurrences && ResultPositionsByHoursClauseOccurrences.Count > 0) || 
                    (null != HoursClauseResults && HoursClauseResults.Count > 0)) &&
                (TimeAllocation != null) && (TimeAllocation.ContainsKey(dt));
        }
    }

    public static class ResultPositionExtensions
    {
        public static ResultPosition UnionWith(this ResultPosition ThisResultPosition, ResultPosition SomeOtherResultPosition)
        {
            ResultPosition UnionResultPosition = new ResultPosition();

            UnionResultPosition.PayOut = ThisResultPosition.PayOut + SomeOtherResultPosition.PayOut;

            UnionResultPosition.TotalGULoss = ThisResultPosition.TotalGULoss + SomeOtherResultPosition.TotalGULoss;

            UnionResultPosition.TimeAllocation = ThisResultPosition.TimeAllocation;

            foreach (KeyValuePair<DateTime, double> TimeAllocationKVP in SomeOtherResultPosition.TimeAllocation)
            {
                if (!UnionResultPosition.TimeAllocation.ContainsKey(TimeAllocationKVP.Key))
                    UnionResultPosition.TimeAllocation.Add(TimeAllocationKVP.Key, TimeAllocationKVP.Value);
                else
                {
                    UnionResultPosition.TimeAllocation[TimeAllocationKVP.Key] += TimeAllocationKVP.Value;
                }
            }

            if (ThisResultPosition.RITEAllocation != null && SomeOtherResultPosition.RITEAllocation != null)
            {
                UnionResultPosition.RITEAllocation = ThisResultPosition.RITEAllocation;

                foreach (KeyValuePair<long, Dictionary<string, double>> RITEAllocationKVP in SomeOtherResultPosition.RITEAllocation)
                {
                    if (!UnionResultPosition.RITEAllocation.ContainsKey(RITEAllocationKVP.Key))
                        UnionResultPosition.RITEAllocation.Add(RITEAllocationKVP.Key, RITEAllocationKVP.Value);
                    else
                    {
                        foreach (KeyValuePair<string, double> RITEAllocationByCOLKVP in RITEAllocationKVP.Value)
                        {
                            if (!UnionResultPosition.RITEAllocation[RITEAllocationKVP.Key].ContainsKey(RITEAllocationByCOLKVP.Key))
                                UnionResultPosition.RITEAllocation[RITEAllocationKVP.Key].Add(RITEAllocationByCOLKVP.Key, RITEAllocationByCOLKVP.Value);
                            else
                                UnionResultPosition.RITEAllocation[RITEAllocationKVP.Key][RITEAllocationByCOLKVP.Key] += RITEAllocationByCOLKVP.Value;
                        }
                    }
                }

                UnionResultPosition.RiskItemAllocation = ThisResultPosition.RiskItemAllocation;

                foreach (KeyValuePair<long, Dictionary<string, double>> RiskItemAllocationKVP in SomeOtherResultPosition.RiskItemAllocation)
                {
                    if (!UnionResultPosition.RiskItemAllocation.ContainsKey(RiskItemAllocationKVP.Key))
                        UnionResultPosition.RiskItemAllocation.Add(RiskItemAllocationKVP.Key, RiskItemAllocationKVP.Value);
                    else
                    {
                        foreach (KeyValuePair<string, double> RiskItemAllocationByCOLKVP in RiskItemAllocationKVP.Value)
                        {
                            if (!UnionResultPosition.RiskItemAllocation[RiskItemAllocationKVP.Key].ContainsKey(RiskItemAllocationByCOLKVP.Key))
                                UnionResultPosition.RiskItemAllocation[RiskItemAllocationKVP.Key].Add(RiskItemAllocationByCOLKVP.Key, RiskItemAllocationByCOLKVP.Value);
                            else
                                UnionResultPosition.RiskItemAllocation[RiskItemAllocationKVP.Key][RiskItemAllocationByCOLKVP.Key] += RiskItemAllocationByCOLKVP.Value;
                        }
                    }
                }
            }

            return UnionResultPosition;
        }
    }

    public class HoursClauseOutput
    {
        private double[] MaximumPayout;
        private double[] MaximumPayoutLOSubject;
        private List<DateTime>[] MaximumPayoutLOStartingDays;

        public HoursClauseOutput(double[] MaximumPayout, double[] MaximumPayoutLOSubject, List<DateTime>[] MaximumPayoutLOStartingDays)
        {
            // TODO: Complete member initialization
            this.MaximumPayout = MaximumPayout;
            this.MaximumPayoutLOSubject = MaximumPayoutLOSubject;
            this.MaximumPayoutLOStartingDays = MaximumPayoutLOStartingDays;
        }
    }
}
