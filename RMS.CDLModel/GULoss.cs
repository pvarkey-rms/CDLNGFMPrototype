using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractObjectModel
{
    public class GULoss
    {
        public Dictionary<long, Loss> FlattenedGULosses;
        public SortedDictionary<DateTime, double> InputTimeSeries;
        public Dictionary<long, Dictionary<string, double>> GULossByExposure;
        public Dictionary<long, Dictionary<string, double>> GULossByRiskItem;
        public double TotalGULoss;
    }
}
