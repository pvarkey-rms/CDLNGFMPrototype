using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class TimeWindow
    {
        public double start { get; protected set; }
        public double end { get; protected set; }

        public void SetStartandEnd(double StartValue, double EndValue)
        {
            start = StartValue;
            end = EndValue;
        }
    }
}
