using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;

namespace NGFMReference
{
    public class Period
    {        
        public int PID {get; private set;}
        private List<EventOccurence> PGULosses;       
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        private List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> eventLossList;

        public double Duration
        {
            get { return (Start - End).Days; }
        }
        public int NumOfEvents
        {
            get { return PGULosses.Count(); }
        }
        public List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> EventLossList
        { get { return eventLossList; } }

        public Period(int pid, DateTime _start, DateTime _end, List<EventOccurence> _PGULosses)
        {
            PID = pid;
            Start = _start;
            End = _end;
            eventLossList = new List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>>();
            PGULosses = _PGULosses;
            BuildEventLossList();
        }

        private void BuildEventLossList()
        {
            foreach (EventOccurence Event in PGULosses)
            {
                eventLossList.Add(Event.GuLosses);
            }
        }
    }

    public class EventOccurence
    {
        public int EventId { get; private set; }
        public DateTime OccurenceDate { get; private set; }
        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> GuLosses { get; private set; }

        public COLCollection COLSet { get; private set; }

        public EventOccurence(int _eventID, DateTime _occurenceDate, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>  _guLosses)
        {
            EventId = _eventID;
            OccurenceDate = _occurenceDate;
            GuLosses = _guLosses;
        }
    }
}
