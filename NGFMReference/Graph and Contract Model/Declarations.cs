using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class Declarations
    {
        public string Name { get; set; }
        public string ContractType { get; set; }
        public string Currency { get; set; }
        public DateTime Inception { get; set; }
        public DateTime Expiration { get; set; }
        public string GrossPosition { get; set; }
        public COLCollection CausesofLoss { get; set; }
        public ScheduleOfRITEs Schedule { get; set; }
        public ExposureTypeCollection ExposureTypes { get; set; }
        public bool  GroundUpSublimits{ get; set; }
        public bool MinimumAbsorbingDed { get; set; }
        public bool ApplyCntDates { get; set; }
        public bool IsHoursClause { get; set; }
        public List<NGFMReference.HoursClause> HoursClauses { get; set; }

        public Declarations()
        {
            GroundUpSublimits = false;
            MinimumAbsorbingDed = false;
            Currency = "USD";
            ApplyCntDates = false;
            IsHoursClause = false;
        }

    }

    public enum ContractType
    {
        Primary
    }
}
