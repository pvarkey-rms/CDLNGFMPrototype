using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;

namespace NGFMReference
{
    public class PeriodLossTable
    {        
        private COLCollection COLSet;
        public int PID {get; set;}        
        public List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> PGULosses;       
        private TimeStyle timestyle;
        private LossStyle lossstyle;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public double Duration
        {
            get { return (Start - End).Days; }
        }
        public int NumOfEvents
        {
            get { return PGULosses.Count(); }
        }

        public PeriodLossTable(int pid, COLCollection _COLSet, TimeStyle _timestyle, LossStyle _lossstyle, DateTime _start, DateTime _end)
        {
            PID = pid;
            Start = _start;
            End = _end;
            COLSet = _COLSet;
            timestyle = _timestyle;
            lossstyle = _lossstyle;
            PGULosses = new List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>>();            
        }

    }
}
