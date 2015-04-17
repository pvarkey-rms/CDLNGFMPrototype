using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class HoursClause
    {
        public double Duration { get; protected set; }
        public string DurationTimeUnit { get; protected set; }
        public string CausesOfLoss { get; protected set; }
        public string Filter { get; protected set; }

        public HoursClause()
        {
           
        }

        public void SetHoursClause(Dictionary<String,Object> hc)
        {
            Duration = Convert.ToDouble(hc["Duration"]);
            DurationTimeUnit = Convert.ToString(hc["DurationTimeUnit"]);
            CausesOfLoss = Convert.ToString(hc["CausesOfLoss"]);
            Filter = Convert.ToString(hc["Filter"]);
        }

    }
}
