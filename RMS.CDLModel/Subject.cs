using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

using Rms.Cdl.Backend.DataObjects;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    [Serializable]
    [ProtoContract]
    public class Subject : SubjectPosition, IEquatable<Subject>
    {
        #region Primary Fields

        static long _id = 0;
        static object _idLocker = new object();
        public long ID { get; private set; }

        [ProtoMember(1)]
        public Schedule Schedule;

        [ProtoMember(2)]
        private Dictionary<string, HashSet<long>> ResolvedSchedule;

        private Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap;

        [ProtoMember(4)]
        public bool PerRisk = false;

        private int _NumBuildings = 1;
        public int NumBuildings {
            get { return _NumBuildings; }
            set { _NumBuildings = value;  }
        }

        #endregion

        #region Derived Fields
        public HashSet<int> ResolvedExposureTypes { private set; get; }
        public HashSet<long> RITEIds { private set; get; }
        public HashSet<long> RiskItemIds { private set; get; }

        /// <summary>
        /// This field explodes the subject and stores the fine-grained component tuples in the 3D space in which the Subject lives
        /// The tuple is orgranized as : CauseOfLoss, ExposureType, [Schedule, Schedule']>
        /// TODO: make this a list of hashes later (to make subset and '-' (i.e. minus) operations efficient)
        /// </summary>
        private Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> Components;
        #endregion

        private UniversalSubjectPosition UniversalSubject;

        #region Constructors
        public Subject() 
            : this(null, new Schedule(), new HashSet<SymbolicValue>(), new HashSet<SymbolicValue>(), null, null, false) { }
        
        void SetID()
        {
            lock (_idLocker)
            {
                ID = _id++;
            }
        }

        public Subject(UniversalSubjectPosition universalSubject,
            Schedule Schedule, 
            HashSet<SymbolicValue> CausesOfLoss, 
            HashSet<SymbolicValue> ExposuresTypes, 
            Dictionary<string, HashSet<long>> resolvedSchedule = null,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap = null,
            bool Resolution = false)
        {
            SetID();

            this.UniversalSubject = universalSubject;
            this.CoverageIdAttrMap = CoverageIdAttrMap;

            this.Schedule = Schedule;
            this.RITEIds = new HashSet<long>();
            if (resolvedSchedule != null)
            {
                this.ResolvedSchedule = resolvedSchedule;

                foreach (SymbolicValue ScheduleSymbol in this.Schedule.ScheduleSymbols)
                {
                    if (ResolvedSchedule != null)
                        if (ResolvedSchedule.ContainsKey(ScheduleSymbol.ToString()))
                            RITEIds.UnionWith(ResolvedSchedule[ScheduleSymbol.ToString()]);
                }
            }

            //this.RiskItemIds = new HashSet<long>(this.RITEIds.Select(x => CoverageIdAttrMap[x].RITExposureId).Distinct());

            this.CausesOfLoss = CausesOfLoss;
            
            this.ExposureTypes = ExposuresTypes;
            this.ResolvedExposureTypes = new HashSet<int>();
            foreach (SymbolicValue _ExposureType in ExposureTypes)
            {
                //ExposureType.ExposureTypeGroup ExposureTypeGroup = new ExposureType.ExposureTypeGroup();
                //ExposureTypeGroup.Set((ExposureType.EExposureType)(Enum.Parse(typeof(ExposureType.EExposureType), _ExposureType.ToString())));
                //ResolvedExposureTypes.UnionWith(ExposureTypeGroup.GetIndividualIntExposureTypes());
                //TODO : Use the above approach (3 lines of code) when platform is fixed and ready
                ResolvedExposureTypes.UnionWith(
                        ExposureType.GetIndividualIntExposureTypes(
                            (ExposureType.EExposureType)(Enum.Parse(typeof(ExposureType.EExposureType), _ExposureType.ToString()))
                        )
                );
            }

            // If all dimensions are empty, then subject is not constrained

            if ((this.ResolvedExposureTypes.Count == 0) && (this.CausesOfLoss.Count == 0) && (this.RITEIds.Count == 0))
                isNotConstrained = true;

            // If any dimension is empty, set from universal subject

            if (UniversalSubject != null)
            {
                if (this.ResolvedExposureTypes.Count == 0)
                    this.ResolvedExposureTypes = UniversalSubject.ResolvedExposureTypes;

                if (this.CausesOfLoss.Count == 0)
                    this.CausesOfLoss = UniversalSubjectPosition.CausesOfLoss;

                if (this.RITEIds.Count == 0)
                {
                    this.RITEIds = UniversalSubject.AllRITEIds;
                }
            }

            this.PerRisk = Resolution;

            // Build components
            
            //Components = new HashSet<Tuple<int, long, SymbolicValue>>();
            Components = new Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>>();

            if (this.RITEIds == null)
                throw new NullReferenceException("Universal Subject RITEs cannot be null (can be empty)!");
            if (this.RITEIds.Count != 0)
            {
                foreach (SymbolicValue CauseOfLoss in this.CausesOfLoss)
                {
                    if (!Components.ContainsKey(CauseOfLoss))
                        Components.Add(CauseOfLoss, new Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>());
                    foreach (int ResolvedExposureType in this.ResolvedExposureTypes)
                    {
                        if (!Components[CauseOfLoss].ContainsKey(ResolvedExposureType))
                            Components[CauseOfLoss].Add(ResolvedExposureType, Tuple.Create(new HashSet<long>(), new HashSet<long>()));
                        foreach (long RITEId in this.RITEIds)
                        {
                            if (CoverageIdAttrMap.ContainsKey(RITEId) && CoverageIdAttrMap[RITEId].ExposureType.Equals(ResolvedExposureType))
                                Components[CauseOfLoss][ResolvedExposureType].Item1.Add(RITEId);
                            else
                                Components[CauseOfLoss][ResolvedExposureType].Item2.Add(RITEId);
                        }
                        if (Components[CauseOfLoss][ResolvedExposureType].Item1.Count == 0)
                            Components[CauseOfLoss].Remove(ResolvedExposureType);
                    }
                    if (Components[CauseOfLoss].Count == 0)
                        Components.Remove(CauseOfLoss);
                }
            }
        }

        public Subject(UniversalSubjectPosition universalSubject,
            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> Components2Difference,
            Dictionary<string, HashSet<long>> resolvedSchedule = null, 
            bool _Resolution = false, 
            bool _isNotConstrained = false)
        {
            SetID();

            this.UniversalSubject = universalSubject;

            this.Schedule = new Schedule(new HashSet<SymbolicValue>() { "Residual" });

            this.PerRisk = _Resolution;
            this.isNotConstrained = _isNotConstrained;

            if (resolvedSchedule != null)
            {
                this.ResolvedSchedule = resolvedSchedule;
            }

            this.ExposureTypes = new HashSet<SymbolicValue>();
            this.CausesOfLoss = new HashSet<SymbolicValue>();
            this.RITEIds = new HashSet<long>();

            this.Components = Components2Difference;

            foreach (SymbolicValue COL in Components.Keys)
            {
                CausesOfLoss.Add(COL);
                foreach (int ResolvedExposureType in Components[COL].Keys)
                {
                    ExposureTypes.Add(new SymbolicValue(((ExposureType.EExposureType)ResolvedExposureType).ToString()));
                    HashSet<long> Components_COL_ResolvedExposureType = Components[COL][ResolvedExposureType].Item1;
                    foreach (long RITEId in Components_COL_ResolvedExposureType)
                        this.RITEIds.Add(RITEId);
                }
            }

            this.ResolvedExposureTypes = new HashSet<int>();
            foreach (SymbolicValue _ExposureType in ExposureTypes)
            {
                //ExposureType.ExposureTypeGroup ExposureTypeGroup = new ExposureType.ExposureTypeGroup();
                //ExposureTypeGroup.Set((ExposureType.EExposureType)(Enum.Parse(typeof(ExposureType.EExposureType), _ExposureType.ToString())));
                //ResolvedExposureTypes.UnionWith(ExposureTypeGroup.GetIndividualIntExposureTypes());
                //TODO : Use the above approach (3 lines of code) when platform is fixed and ready
                ResolvedExposureTypes.UnionWith(
                        ExposureType.GetIndividualIntExposureTypes(
                            (ExposureType.EExposureType)(Enum.Parse(typeof(ExposureType.EExposureType), _ExposureType.ToString()))
                        )
                );
            }
        }

        public Subject(Subject CopyFromSubject) :
            this(CopyFromSubject.GetUniversalSubject(),
                    new Schedule(CopyFromSubject.Schedule),
                    new HashSet<SymbolicValue>(CopyFromSubject.CausesOfLoss),
                    new HashSet<SymbolicValue>(CopyFromSubject.ExposureTypes),
                    CopyFromSubject.ResolvedSchedule,
                    CopyFromSubject.CoverageIdAttrMap,
                    CopyFromSubject.PerRisk) { }
        #endregion

        #region API
        public UniversalSubjectPosition GetUniversalSubject()
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            return UniversalSubject;
        }

        public Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> GetComponents()
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            return Components;
        }

        public Subject Minus(Subject OtherSubject)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> Components2Difference =
                this.Minus(OtherSubject.GetComponents());

            return new Subject(this.GetUniversalSubject(), Components2Difference, 
                this.ResolvedSchedule, this.PerRisk, this.IsNotConstrained());
        }

        public bool IsNotConstrained()
        {
            return isNotConstrained;
        }

        public bool IsDerived()
        {
            return isDerived;
        }

        public void HardResetSchedule(HashSet<SymbolicValue> NewScheduleSymbols, 
            Dictionary<string, HashSet<long>> resolvedSchedule)
        {
            Schedule.ScheduleSymbols = NewScheduleSymbols;
            this.RITEIds = new HashSet<long>();
            if (resolvedSchedule != null)
            {
                this.ResolvedSchedule = resolvedSchedule;

                foreach (SymbolicValue ScheduleSymbol in this.Schedule.ScheduleSymbols)
                {
                    if (ResolvedSchedule != null)
                        if (ResolvedSchedule.ContainsKey(ScheduleSymbol.ToString()))
                            RITEIds.UnionWith(ResolvedSchedule[ScheduleSymbol.ToString()]);
                }
            }

            // If all dimensions are empty, then subject is not constrained

            if ((this.ResolvedExposureTypes.Count == 0) && (this.CausesOfLoss.Count == 0) && (this.RITEIds.Count == 0))
                isNotConstrained = true;

            // If any dimension is empty, set from universal subject

            if (UniversalSubject != null)
            {
                if (this.RITEIds.Count == 0)
                {
                    if (this.RITEIds.Count == 0)
                        this.RITEIds = UniversalSubject.AllRITEIds;
                }
            }

            // Rebuild components

            Components = new Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>>();

            foreach (SymbolicValue CauseOfLoss in this.CausesOfLoss)
            {
                if (!Components.ContainsKey(CauseOfLoss))
                    Components.Add(CauseOfLoss, new Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>());
                foreach (int ResolvedExposureType in this.ResolvedExposureTypes)
                {
                    if (!Components[CauseOfLoss].ContainsKey(ResolvedExposureType))
                        Components[CauseOfLoss].Add(ResolvedExposureType, Tuple.Create(new HashSet<long>(), new HashSet<long>()));
                    foreach (long RITEId in this.RITEIds)
                    {
                        if (CoverageIdAttrMap.ContainsKey(RITEId) && CoverageIdAttrMap[RITEId].ExposureType.Equals(ResolvedExposureType))
                            Components[CauseOfLoss][ResolvedExposureType].Item1.Add(RITEId);
                        else
                            Components[CauseOfLoss][ResolvedExposureType].Item2.Add(RITEId);
                    }
                    if (Components[CauseOfLoss][ResolvedExposureType].Item1.Count == 0)
                        Components[CauseOfLoss].Remove(ResolvedExposureType);
                }
                if (Components[CauseOfLoss].Count == 0)
                    Components.Remove(CauseOfLoss);
            }
        }
        #endregion

        #region Private Methods

        private Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> Minus(
            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> OtherComponents2)
        {
            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> Difference
                = new Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>>();

            foreach (SymbolicValue COL in Components.Keys)
            {
                if (!Difference.ContainsKey(COL))
                    Difference.Add(COL, new Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>());
                foreach (int ResolvedExposureType in Components[COL].Keys)
                {
                    if (!Difference[COL].ContainsKey(ResolvedExposureType))
                        Difference[COL].Add(ResolvedExposureType, Tuple.Create(new HashSet<long>(), new HashSet<long>()));
                    Difference[COL][ResolvedExposureType].Item1
                        .UnionWith(Components[COL][ResolvedExposureType].Item1);
                    if (OtherComponents2.ContainsKey(COL) && OtherComponents2[COL].ContainsKey(ResolvedExposureType))
                    {
                        Difference[COL][ResolvedExposureType].Item1
                            .ExceptWith(OtherComponents2[COL][ResolvedExposureType].Item1);
                        if (Difference[COL][ResolvedExposureType].Item1.Count== 0)
                            Difference[COL].Remove(ResolvedExposureType);
                    }
                    if (Difference[COL].ContainsKey(ResolvedExposureType))
                    {
                        Difference[COL][ResolvedExposureType].Item2.UnionWith(UniversalSubject.AllRITEIds);
                        Difference[COL][ResolvedExposureType].Item2.ExceptWith(Difference[COL][ResolvedExposureType].Item1);
                    }
                }
                if (Difference[COL].Count == 0)
                    Difference.Remove(COL);
            }

            return Difference;
        }
        #endregion

        #region Methods
        public bool IsSubsetOf(Subject otherSubject, bool CheckProper = false)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            bool IsPerRiskSubordinate = this.PerRisk && !otherSubject.PerRisk;

            if (IsPerRiskSubordinate)
                CheckProper = false; // In this case, IsProperSubsetOf becomes IsSubsetOf. TODO: Prove!

            return IsSubsetOf(this.Components, otherSubject.GetComponents(), CheckProper) 
                && !(!this.PerRisk && otherSubject.PerRisk);
        }

        private bool IsSubsetOf(Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> PurportedSubset,
            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> Set, bool CheckProper = false)
        {
            //TODO: null checking

            bool AtleastOneProper = false;
            bool StrictlyLesserComponents = false;

            int PurportedSubsetCount = PurportedSubset.Count;
            int SetCount = Set.Count;

            if (PurportedSubsetCount > SetCount)
                return false;

            if (PurportedSubsetCount < SetCount)
                StrictlyLesserComponents = true;

            foreach (SymbolicValue COL in PurportedSubset.Keys)
            {
                if (!Set.ContainsKey(COL))
                    return false;

                int PurportedSubsetCount_COL = PurportedSubset[COL].Count;
                int SetCount_COL = Set[COL].Count;

                if (PurportedSubsetCount_COL > SetCount_COL)
                    return false;

                if (PurportedSubsetCount_COL < SetCount_COL)
                    StrictlyLesserComponents = true;

                foreach (int ResolvedExposureType in PurportedSubset[COL].Keys)
                {
                    if (!Set[COL].ContainsKey(ResolvedExposureType))
                        return false;

                    HashSet<long> PurportedSubset_COL_ResolvedExposureType =
                        PurportedSubset[COL][ResolvedExposureType].Item1;

                    HashSet<long> Set_COL_ResolvedExposureType =
                        Set[COL][ResolvedExposureType].Item1;

                    int PurportedSubsetCount_COL_ResolvedExposureType = PurportedSubset_COL_ResolvedExposureType.Count;
                    int SetCount_COL_ResolvedExposureType = Set_COL_ResolvedExposureType.Count;

                    if (PurportedSubsetCount_COL_ResolvedExposureType > SetCount_COL_ResolvedExposureType)
                        return false;

                    if (PurportedSubsetCount_COL_ResolvedExposureType < SetCount_COL_ResolvedExposureType)
                        StrictlyLesserComponents = true;

                    if (!PurportedSubset_COL_ResolvedExposureType.IsSubsetOf(Set_COL_ResolvedExposureType))
                        return false;

                    if (CheckProper && !StrictlyLesserComponents && !AtleastOneProper)
                    {
                        if (PurportedSubset[COL][ResolvedExposureType].Item1.Count
                                == Set[COL][ResolvedExposureType].Item1.Count)
                            AtleastOneProper = true;
                    }
                }
            }

            return ((CheckProper && (AtleastOneProper || StrictlyLesserComponents)) || (!CheckProper));
        }

        public bool IsProperSubsetOf(Subject subject)
        {
            return this.IsSubsetOf(subject, true);
        }

        public bool Overlaps(Subject otherSubject)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            return Overlaps(this.Components, otherSubject.GetComponents());
        }

        private bool Overlaps(Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> PurportedOverlappingSet,
            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> Set)
        {
            //TODO: null checking

            foreach (SymbolicValue COL in PurportedOverlappingSet.Keys)
            {
                if (!Set.ContainsKey(COL))
                    continue;
                else
                    foreach (int ResolvedExposureType in PurportedOverlappingSet[COL].Keys)
                    {
                        if (!Set[COL].ContainsKey(ResolvedExposureType))
                            continue;
                        else if (PurportedOverlappingSet[COL][ResolvedExposureType].Item1
                                .Overlaps(Set[COL][ResolvedExposureType].Item1))
                            return true;
                    }
            }

            return false;
        }

        public bool OverlapsWithoutInclusion(Subject otherSubject)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            if (this.IsSubsetOf(otherSubject))
                return false;

            if (otherSubject.IsSubsetOf(this))
                return false;

            return this.Overlaps(otherSubject);

            //return this.Overlaps(otherSubject) && !this.IsSubsetOf(otherSubject) && !otherSubject.IsSubsetOf(this);
        }
        #endregion

        #region Operator Overloads
        public static Subject operator -(Subject subject1, Subject subject2)
        {
            return subject1.Minus(subject2);
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Subject))
                return false;

            Subject s = obj as Subject;

            return this.Equals(s);
        }

        public bool Equals(Subject s)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            if (s == null)
            {
                return false;
            }

            if (PerRisk != s.PerRisk)
                return false;

            Dictionary<SymbolicValue, Dictionary<int, Tuple<HashSet<long>, HashSet<long>>>> OtherComponents2 
                = s.GetComponents();

            if (Components.Keys.Count != OtherComponents2.Keys.Count)
                return false;

            if (!Components.Keys.SequenceEqual(OtherComponents2.Keys))
                return false;

            foreach (SymbolicValue COL in Components.Keys)
            {
                if (Components[COL].Keys.Count != OtherComponents2[COL].Keys.Count)
                    return false;

                if (!Components[COL].Keys.SequenceEqual(OtherComponents2[COL].Keys))
                    return false;

                foreach (int ResolvedExposureType in Components[COL].Keys)
                {
                    HashSet<long> Components_COL_ResolvedExposureType
                        = Components[COL][ResolvedExposureType].Item1;
                    HashSet<long> OtherComponents2_COL_ResolvedExposureType
                        = OtherComponents2[COL][ResolvedExposureType].Item1;

                    if (Components_COL_ResolvedExposureType.Count
                            != OtherComponents2_COL_ResolvedExposureType.Count)
                        return false;

                    foreach (long RITEId in Components_COL_ResolvedExposureType)
                    {
                        if (!OtherComponents2_COL_ResolvedExposureType.Contains(RITEId))
                            return false;
                    }
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            int hash = 23;
            
            hash = hash * 37 + PerRisk.GetHashCode();

            if ((Components == null) || (Components.Count == 0))
                hash = hash * 37 + 41;
            else
                foreach (SymbolicValue COL in Components.Keys)
                {
                    hash = hash * 37 + COL.GetHashCode();
                    foreach (int ResolvedExposureType in Components[COL].Keys)
                    {
                        hash = hash * 37 + ResolvedExposureType.GetHashCode();
                        HashSet<long> Components_COL_ResolvedExposureType
                            = Components[COL][ResolvedExposureType].Item1;
                        foreach (long RITEId in Components_COL_ResolvedExposureType)
                        {
                            hash = hash * 37 + RITEId.GetHashCode();
                        }
                    }
                }

            return hash;
        }
        #endregion

        //#region Hacks
        ///// <summary>
        ///// Use <code>ExposureTypeGroup.GetIndividualIntExposureTypes</code> from the platform, when fixed and available.
        ///// Shelveset Bitmap logic fix for yielding individual exposure types
        ///// </summary>
        ///// <returns></returns>
        //private IEnumerable<int> GetIndividualIntExposureTypes(ExposureType.EExposureType exposureType)
        //{
        //    int _value = 0;
        //    _value |= (int)exposureType;
        //    for (var i = 1; i <= SizeOf; i++)
        //    {
        //        if ((_value & 1 << (i - 1)) > 0)
        //        {
        //            yield return (int)(ExposureType.EExposureType)(_value & 1 << (i - 1));
        //        }
        //    }
        //}
        //#endregion
    }
}
