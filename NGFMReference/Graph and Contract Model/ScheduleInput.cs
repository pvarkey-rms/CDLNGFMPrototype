using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class ScheduleInput : IScheduleInput
    {
        public ScheduleOfRITEs Schedule; 
        public bool IsPerRisk {get; set;}
        public int NumOfRites { get { return Schedule.ScheduleList.Count; } }
        public bool IsLocation
        {
            get
            {
                if (GetScheduleRITEList().Count == 1)
                    return true;
                else return false;
            }
        }
        public string ScheduleName { get { return Schedule.Name; } }

        public ScheduleInput()
        {
            Schedule = new ScheduleOfRITEs(null);
            IsPerRisk = true;
        }

        public ScheduleInput(ScheduleOfRITEs _schedule, bool _isPerRisk = false)
        {
            Schedule = _schedule;
            IsPerRisk = _isPerRisk;
        }

        public HashSet<RITE> GetScheduleRITEList()
        {
            return Schedule.ScheduleList;
        
        }
    }

    public interface IScheduleInput
    {
        bool IsPerRisk { get; }
        bool IsLocation { get; }
        int NumOfRites { get; }
        string ScheduleName {get;}
        HashSet<RITE> GetScheduleRITEList();
    }

    public enum ScheduleCompareOutcome
    {
        Child = -1,
        Equal = 0,
        Parent = 1,
        Overlap = 2,       
        Disjoin = 3
    }

}

