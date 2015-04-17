using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference.ContractModel
{
    public class HoursClause
    {
        public int Duration { get; protected set; }
        public string DurationTimeUnit { get; protected set; }
        public string CausesOfLoss { get; protected set; }
        public string Filter { get; protected set; }
        public Boolean OnlyOnce { get; protected set; }

        public HoursClause()
        {
           
        }

        public void SetHoursClause(Dictionary<String,Object> hc)
        {
            Duration = Convert.ToInt32(hc["Duration"]);
            DurationTimeUnit = Convert.ToString(hc["DurationTimeUnit"]);
            CausesOfLoss = Convert.ToString(hc["CausesOfLoss"]);
            OnlyOnce = Convert.ToBoolean(hc["OnlyOnce"]);

            //Filter = Convert.ToString(hc["Filter"]);
            //Rain, temp fix to change hour clause to days
            if (DurationTimeUnit == "Hours")
            {
                Duration = Duration / 24;
                DurationTimeUnit = "Days";
            }
        }

    }
}
