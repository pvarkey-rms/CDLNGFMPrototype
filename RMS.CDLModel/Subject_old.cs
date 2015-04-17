using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ewah;

using Rms.Cdl.Backend.DataObjects;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    public class Subject_old : SubjectPosition, IEquatable<Subject>
    {
        #region Primary Fields
        //public HashSet<SymbolicValue> ExposureTypes;
        //public HashSet<SymbolicValue> CausesOfLoss;
        public Schedule Schedule;
        private Dictionary<string, HashSet<long>> ResolvedSchedule;
        private Dictionary<long, int> CoverageIdExposureTypeMap;
        public bool PerRisk = false;
        #endregion

        #region Derived Fields
        public HashSet<int> ResolvedExposureTypes;
        public HashSet<long> RITEIds;

        /// <summary>
        /// This field explodes the subject and stores the fine-grained component tuples in the 3D space in which the Subject lives
        /// The tuple is orgranized as : ResolvedExposureType, Schedule component, CauseOfLoss
        /// TODO: make this a list of hashes later (to make subset and '-' (i.e. minus) operations efficient)
        /// </summary>
        //private HashSet<Tuple<int, long, SymbolicValue>> Components;
        private Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> Components;
        #endregion

        private UniversalSubjectPosition UniversalSubject;

        #region Constructors
        public Subject() 
            : this(null, new Schedule(), new HashSet<SymbolicValue>(), new HashSet<SymbolicValue>(), null, null, false) { }
        
        public Subject(UniversalSubjectPosition universalSubject,
            Schedule Schedule, 
            HashSet<SymbolicValue> CausesOfLoss, 
            HashSet<SymbolicValue> ExposuresTypes, 
            Dictionary<string, HashSet<long>> resolvedSchedule = null, 
            Dictionary<long, int> coverageIdExposureTypeMap = null,
            bool Resolution = false)
        {
            this.UniversalSubject = universalSubject;
            this.CoverageIdExposureTypeMap = coverageIdExposureTypeMap;

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
                    this.ResolvedExposureTypes = UniversalSubject.GetResolvedExposureTypes();

                if (this.CausesOfLoss.Count == 0)
                    this.CausesOfLoss = UniversalSubjectPosition.GetCausesOfLoss();

                if (this.RITEIds.Count == 0)
                    this.RITEIds = UniversalSubject.GetAllRITEIds();
            }

            this.PerRisk = Resolution;

            // Build components
            
            //Components = new HashSet<Tuple<int, long, SymbolicValue>>();
            Components = new Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>>();

            foreach (SymbolicValue CauseOfLoss in this.CausesOfLoss)
            {
                if (!Components.ContainsKey(CauseOfLoss))
                    Components.Add(CauseOfLoss, new Dictionary<int, HashSet<long>>());
                foreach (int ResolvedExposureType in this.ResolvedExposureTypes)
                {
                    if (!Components[CauseOfLoss].ContainsKey(ResolvedExposureType))
                        Components[CauseOfLoss].Add(ResolvedExposureType, new HashSet<long>());
                    foreach (long RITEId in this.RITEIds)
                    {
                        if (CoverageIdExposureTypeMap.ContainsKey(RITEId) && CoverageIdExposureTypeMap[RITEId].Equals(ResolvedExposureType))
                        {
                            //Components.Add(new Tuple<int, long, SymbolicValue>(ResolvedExposureType, RITEId, CauseOfLoss));
                            Components[CauseOfLoss][ResolvedExposureType].Add(RITEId);
                        }
                    }
                    if (Components[CauseOfLoss][ResolvedExposureType].Count == 0)
                        Components[CauseOfLoss].Remove(ResolvedExposureType);
                }
                if (Components[CauseOfLoss].Count == 0)
                    Components.Remove(CauseOfLoss);
            }
        }

        //public Subject(UniversalSubjectPosition universalSubject,
        //    HashSet<Tuple<int, long, SymbolicValue>> ComponentsDifference,
        //    Dictionary<string, HashSet<long>> resolvedSchedule = null, bool _Resolution = false, bool _isNotConstrained = false)
        //{
        //    this.UniversalSubject = universalSubject;

        //    this.Schedule = new Schedule(new HashSet<SymbolicValue>() { "Residual" });

        //    this.PerRisk = _Resolution;
        //    this.isNotConstrained = _isNotConstrained;

        //    if (resolvedSchedule != null)
        //    {
        //        this.ResolvedSchedule = resolvedSchedule;
        //    }

        //    this.ExposureTypes = new HashSet<SymbolicValue>();
        //    this.CausesOfLoss = new HashSet<SymbolicValue>();
        //    this.RITEIds = new HashSet<long>();

        //    this.Components = ComponentsDifference;

        //    foreach (Tuple<int, long, SymbolicValue> component in ComponentsDifference)
        //    {
        //        ExposureTypes.Add(new SymbolicValue(((ExposureType.EExposureType)component.Item1).ToString()));
        //        RITEIds.Add(component.Item2);
        //        CausesOfLoss.Add(component.Item3);
        //    }

        //    this.ResolvedExposureTypes = new HashSet<int>();
        //    foreach (SymbolicValue _ExposureType in ExposureTypes)
        //    {
        //        //ExposureType.ExposureTypeGroup ExposureTypeGroup = new ExposureType.ExposureTypeGroup();
        //        //ExposureTypeGroup.Set((ExposureType.EExposureType)(Enum.Parse(typeof(ExposureType.EExposureType), _ExposureType.ToString())));
        //        //ResolvedExposureTypes.UnionWith(ExposureTypeGroup.GetIndividualIntExposureTypes());
        //        //TODO : Use the above approach (3 lines of code) when platform is fixed and ready
        //        ResolvedExposureTypes.UnionWith(
        //                ExposureType.GetIndividualIntExposureTypes(
        //                    (ExposureType.EExposureType)(Enum.Parse(typeof(ExposureType.EExposureType), _ExposureType.ToString()))
        //                )
        //        );
        //    }
        //}

        public Subject(UniversalSubjectPosition universalSubject,
            Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> Components2Difference,
            Dictionary<string, HashSet<long>> resolvedSchedule = null, bool _Resolution = false, bool _isNotConstrained = false)
        {
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
                    foreach (long RITEId in Components[COL][ResolvedExposureType])
                        RITEIds.Add(RITEId);
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
                    CopyFromSubject.CoverageIdExposureTypeMap,
                    CopyFromSubject.PerRisk) { }
        #endregion

        #region API
        public UniversalSubjectPosition GetUniversalSubject()
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            return UniversalSubject;
        }

        //public HashSet<Tuple<int, long, SymbolicValue>> GetComponents()
        //{
        //    if (UniversalSubject == null)
        //        throw new NotSupportedException("UniversalSubject not yet initialized!");

        //    return Components;
        //}

        public Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> GetComponents2()
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            return Components;
        }

        public Subject Minus(Subject OtherSubject)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> Components2Difference =
                this.Minus(OtherSubject.GetComponents2());

            return new Subject(this.GetUniversalSubject(), Components2Difference, this.ResolvedSchedule, this.PerRisk, this.IsNotConstrained());

            //HashSet<Tuple<int, long, SymbolicValue>> ComponentsDifference = 
            //    this.Minus(OtherSubject.GetComponents());

            //return new Subject(this.GetUniversalSubject(), ComponentsDifference, this.ResolvedSchedule, this.PerRisk, this.IsNotConstrained());
        }

        public bool IsNotConstrained()
        {
            return isNotConstrained;
        }

        public bool IsDerived()
        {
            return isDerived;
        }

        public void HardResetSchedule(HashSet<SymbolicValue> NewScheduleSymbols, Dictionary<string, HashSet<long>> resolvedSchedule)
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
                    this.RITEIds = UniversalSubject.GetAllRITEIds();
            }

            // Rebuild components

            Components = new Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>>();

            foreach (SymbolicValue CauseOfLoss in this.CausesOfLoss)
            {
                if (!Components.ContainsKey(CauseOfLoss))
                    Components.Add(CauseOfLoss, new Dictionary<int, HashSet<long>>());
                foreach (int ResolvedExposureType in this.ResolvedExposureTypes)
                {
                    if (!Components[CauseOfLoss].ContainsKey(ResolvedExposureType))
                        Components[CauseOfLoss].Add(ResolvedExposureType, new HashSet<long>());
                    foreach (long RITEId in this.RITEIds)
                    {
                        if (CoverageIdExposureTypeMap.ContainsKey(RITEId) && CoverageIdExposureTypeMap[RITEId].Equals(ResolvedExposureType))
                        {
                            //Components.Add(new Tuple<int, long, SymbolicValue>(ResolvedExposureType, RITEId, CauseOfLoss));
                            Components[CauseOfLoss][ResolvedExposureType].Add(RITEId);
                        }
                    }
                    if (Components[CauseOfLoss][ResolvedExposureType].Count == 0)
                        Components[CauseOfLoss].Remove(ResolvedExposureType);
                }
                if (Components[CauseOfLoss].Count == 0)
                    Components.Remove(CauseOfLoss);
            }

            //Components = new HashSet<Tuple<int, long, SymbolicValue>>();

            //foreach (int ResolvedExposureType in this.ResolvedExposureTypes)
            //{
            //    foreach (long RITEId in this.RITEIds)
            //    {
            //        foreach (SymbolicValue CauseOfLoss in this.CausesOfLoss)
            //        {
            //            if (CoverageIdExposureTypeMap.ContainsKey(RITEId) && CoverageIdExposureTypeMap[RITEId].Equals(ResolvedExposureType))
            //                Components.Add(new Tuple<int, long, SymbolicValue>(ResolvedExposureType, RITEId, CauseOfLoss));
            //        }
            //    }
            //}
        }
        #endregion

        #region Private Methods
        //private HashSet<Tuple<int, long, SymbolicValue>> Minus(HashSet<Tuple<int, long, SymbolicValue>> OtherComponents)
        //{
        //    HashSet<Tuple<int, long, SymbolicValue>> Difference = new HashSet<Tuple<int, long, SymbolicValue>>(this.Components);
        //    Difference.ExceptWith(OtherComponents);
        //    return Difference;
        //}

        private Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> Minus(Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> OtherComponents2)
        {
            Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> Difference 
                = new Dictionary<SymbolicValue,Dictionary<int,HashSet<long>>>();

            foreach (SymbolicValue COL in Components.Keys)
            {
                if (!Difference.ContainsKey(COL))
                    Difference.Add(COL, new Dictionary<int,HashSet<long>>());
                foreach (int ResolvedExposureType in Components[COL].Keys)
                {
                    if (!Difference[COL].ContainsKey(ResolvedExposureType))
                        Difference[COL].Add(ResolvedExposureType, new HashSet<long>());
                    Difference[COL][ResolvedExposureType].UnionWith(Components[COL][ResolvedExposureType]);
                }
            }

            foreach (SymbolicValue COL in OtherComponents2.Keys)
            {
                if (!Difference.ContainsKey(COL))
                    continue;
                else
                {
                    foreach (int ResolvedExposureType in OtherComponents2[COL].Keys)
                    {
                        if (!Difference[COL].ContainsKey(ResolvedExposureType))
                            continue;
                        Difference[COL][ResolvedExposureType].ExceptWith(OtherComponents2[COL][ResolvedExposureType]);
                        if (Difference[COL][ResolvedExposureType].Count == 0)
                            Difference[COL].Remove(ResolvedExposureType);
                    }
                    if (Difference[COL].Count == 0)
                        Difference.Remove(COL);
                }
            }

            return Difference;
        }
        #endregion

        #region Methods
        public bool IsSubsetOf(Subject otherSubject)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            return IsSubsetOf(this.Components, otherSubject.GetComponents2()) && !(!this.PerRisk && otherSubject.PerRisk);

            //return this.Components.IsSubsetOf(otherSubject.GetComponents()) && !(!this.PerRisk && otherSubject.PerRisk);
        }

        private bool IsSubsetOf(Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> PurportedSubset,
            Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> Set)
        {
            //TODO: null checking

            if (PurportedSubset.Count > Set.Count)
                return false;

            foreach (SymbolicValue COL in PurportedSubset.Keys)
            {
                if (!Set.ContainsKey(COL))
                    return false;

                if (PurportedSubset[COL].Count > Set[COL].Count)
                    return false;

                foreach (int ResolvedExposureType in PurportedSubset[COL].Keys)
                {
                    if (!Set[COL].ContainsKey(ResolvedExposureType))
                        return false;

                    if (PurportedSubset[COL][ResolvedExposureType].Count > Set[COL][ResolvedExposureType].Count)
                        return false;

                    if (!PurportedSubset[COL][ResolvedExposureType].IsSubsetOf(Set[COL][ResolvedExposureType]))
                        return false;
                }
            }

            return true;
        }

        public bool IsProperSubsetOf(Subject subject)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            if (this.IsSubsetOf(subject))
            {
                if (subject.IsSubsetOf(this))
                    return false;
                else
                    return true;
            }
            else
                return false;
        }

        public bool Overlaps(Subject otherSubject)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            return Overlaps(this.Components, otherSubject.GetComponents2());

            //return this.Components.Overlaps(otherSubject.GetComponents());
        }

        private bool Overlaps(Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> PurportedOverlappingSet,
            Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> Set)
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
                        else if (PurportedOverlappingSet[COL][ResolvedExposureType].Overlaps(Set[COL][ResolvedExposureType]))
                            return true;
                    }
            }

            return false;
        }

        public bool OverlapsWithoutInclusion(Subject otherSubject)
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            return this.Overlaps(otherSubject) && !this.IsSubsetOf(otherSubject) && !otherSubject.IsSubsetOf(this);
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

            Dictionary<SymbolicValue, Dictionary<int, HashSet<long>>> OtherComponents2 = s.GetComponents2();

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
                    if (Components[COL][ResolvedExposureType].Count != OtherComponents2[COL][ResolvedExposureType].Count)
                        return false;

                    foreach (long RITEId in Components[COL][ResolvedExposureType])
                    {
                        if (!OtherComponents2[COL][ResolvedExposureType].Contains(RITEId))
                            return false;
                    }
                }
            }

            return true;

            //return Components.SetEquals(s.GetComponents());
        }

        public override int GetHashCode()
        {
            if (UniversalSubject == null)
                throw new NotSupportedException("UniversalSubject not yet initialized!");

            int hash = 23;
            
            hash = hash * 37 + PerRisk.GetHashCode();

            //if ((Components == null) || (Components.Count == 0))
            //    hash = hash * 37 + 41;
            //else
            //    foreach (Tuple<int, long, SymbolicValue> Component in Components)
            //    {
            //        hash = hash * 37 + Component.Item1.GetHashCode();
            //        hash = hash * 37 + Component.Item2.GetHashCode();
            //        hash = hash * 37 + Component.Item3.GetHashCode();
            //    }

            if ((Components == null) || (Components.Count == 0))
                hash = hash * 37 + 41;
            else
                foreach (SymbolicValue COL in Components.Keys)
                {
                    hash = hash * 37 + COL.GetHashCode();
                    foreach (int ResolvedExposureType in Components[COL].Keys)
                    {
                        hash = hash * 37 + ResolvedExposureType.GetHashCode();
                        foreach (long RITEId in Components[COL][ResolvedExposureType])
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
        //    for (var i = 1; i <= 3; i++)
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
