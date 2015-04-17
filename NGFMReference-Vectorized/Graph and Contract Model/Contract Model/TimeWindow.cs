using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class TimeWindow
    {
        public DateTime start { get; protected set; }
        public DateTime end { get; protected set; }
        public bool IsUnrestricted { get; protected set; }

        public void SetStartandEnd(DateTime StartValue, DateTime EndValue)
        {
            IsUnrestricted = false;
            start = StartValue;
            end = EndValue;
        }

        public TimeWindow(DateTime StartValue, DateTime EndValue)
        {
            IsUnrestricted = false;
            start = StartValue;
            end = EndValue;
        }

        public TimeWindow()
        {
            IsUnrestricted = true;
        }

    }
}
