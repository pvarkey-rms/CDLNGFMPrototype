using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class MatrixResultOutput
    {
        public double TotalGULoss { set; get; }

        public DateTime EventStartDate { set; get; }

        //public Dictionary<long, Dictionary<string, double>> GULossByRITE { set; get; }

        //public Dictionary<long, Dictionary<string, double>> GULossByRiskItem { set; get; }

        public double TotalPayOut { set; get; }

        //public Dictionary<long, Dictionary<string, double>> RITEAllocation { set; get; }

        //public Dictionary<long, Dictionary<string, double>> RiskItemAllocation { set; get; }

        //public SortedDictionary<DateTime, double> TimeAllocation { set; get; }

        //public SortedDictionary<DateTime, double> InputTimeSeries { set; get; }

        public float[] AllocatedRitePayout { get; set; }

        public int[] RiteGULossIndicies { get; set; }

         public MatrixResultOutput()
        {
            TotalGULoss = 0;
            TotalPayOut = 0.0;
        }
    }

}
