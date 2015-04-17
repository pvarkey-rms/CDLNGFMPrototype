using System; 
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    // TODO : Remove Serializable and ProtoContract
    // TODO : only use SymbolicValue and SymbolicExpression for external symbols (and, perhaps, remove even there!)
    [Serializable]
    [ProtoContract]
    public class Contract
    {
        #region Fields

        [ProtoMember(1)]
        public string Name = "";

        [ProtoMember(2)]
        public SubjectPosition ContractSubject;

        //[ProtoMember(3)]
        public Dictionary<string, object> Declarations;

        bool _AreSublimitsNetOfDeductible = false;
        bool _AreDeductiblesAbsorbable = false;

        //[ProtoMember(4)]
        public List<ICover<Value, Value, Value>> Covers;

        //[ProtoMember(5)]
        public List<ITerm<Value>> Sublimits;

        //[ProtoMember(6)]
        public List<ITerm<Value>> Deductibles;

        //[ProtoMember(7)]
        UniversalSubjectPosition UniversalSubject;
        Dictionary<string, HashSet<long>> ResolvedSchedule;

        Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap;

        static readonly List<HoursClause> EMPTYLIST_HOURSCLAUSE = new List<HoursClause>();
        public static Contract EMPTYCONTRACT = null;
        
        #endregion

        #region Constructors

        public Contract(Dictionary<string, object> IR, 
            Dictionary<string, HashSet<long>> resolvedSchedule,
            Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap)
        {
            this.ResolvedSchedule = resolvedSchedule;
            this.CoverageIdAttrMap = CoverageIdAttrMap;

            // Construct universal subject position
            HashSet<long> AllRITEIds = new HashSet<long>();
            if (resolvedSchedule != null)
                AllRITEIds = resolvedSchedule.Aggregate(new HashSet<long>(), (a, b) => { a.UnionWith(b.Value); return a; });

            HashSet<long> AllRiskItemIds = new HashSet<long>();
            if (CoverageIdAttrMap != null)
                AllRiskItemIds = new HashSet<long>(AllRITEIds.Where(x => CoverageIdAttrMap.ContainsKey(x)).Select(x => CoverageIdAttrMap[x].RITExposureId).Distinct());

            UniversalSubject = new UniversalSubjectPosition(AllRITEIds, AllRiskItemIds);

            // Declarations
            Declarations = (Dictionary<string, object>)IR["Declarations"];

            // Claims Adjustment Settings
            if (!Declarations.ContainsKey("Claims Adjustment Options")
                || Declarations["Claims Adjustment Options"] == null)
            {
                _AreSublimitsNetOfDeductible = false;
                _AreDeductiblesAbsorbable = false;
            }
            else
            {
                if (!((Declarations["Claims Adjustment Options"] as Dictionary<string, object>).ContainsKey("Claims Adjustment Sublimits"))
                    || (Declarations["Claims Adjustment Options"] as Dictionary<string, object>)["Claims Adjustment Sublimits"] == null)
                    _AreSublimitsNetOfDeductible = false;
                else
                    _AreSublimitsNetOfDeductible = (Declarations["Claims Adjustment Options"] as Dictionary<string, object>)["Claims Adjustment Sublimits"].ToString().Equals("Net Of Deductible");

                if (!((Declarations["Claims Adjustment Options"] as Dictionary<string, object>).ContainsKey("Claims Adjustment Deductibles"))
                    || (Declarations["Claims Adjustment Options"] as Dictionary<string, object>)["Claims Adjustment Deductibles"] == null)
                    _AreDeductiblesAbsorbable = false;
                else
                    _AreDeductiblesAbsorbable = (Declarations["Claims Adjustment Options"] as Dictionary<string, object>)["Claims Adjustment Deductibles"].ToString().Equals("Absorbable");
            }

            
            object NameObject;
            if(Declarations.TryGetValue("Name", out NameObject))
                this.Name = NameObject.ToString();

            object CausesOfLossObject;
            Declarations.TryGetValue("CausesOfLoss", out CausesOfLossObject);

            object ScheduleObject;
            Declarations.TryGetValue("Schedule", out ScheduleObject);

            object ExposureTypesObject;
            Declarations.TryGetValue("ExposureTypes", out ExposureTypesObject);

            object GrossPositionObject;
            Declarations.TryGetValue("GrossPosition", out GrossPositionObject);

            object CededPositionObject;
            Declarations.TryGetValue("CededPosition", out CededPositionObject);

            if (GrossPositionObject != null)
            {
                if (CededPositionObject != null)
                    this.ContractSubject = new NetPosition(ToHashSet(GrossPositionObject.ToString()), ToHashSet(CededPositionObject.ToString()));
                else if (CausesOfLossObject != null)
                    this.ContractSubject = new SubjectPosition(ToHashSet(GrossPositionObject.ToString()), ToHashSet(CausesOfLossObject.ToString()));
                else
                    this.ContractSubject = new SubjectPosition(ToHashSet(GrossPositionObject.ToString()));
            }
            else if (CausesOfLossObject != null && ScheduleObject != null && ExposureTypesObject != null)
                this.ContractSubject = new Subject(
                    UniversalSubject, new Schedule(ToHashSet(ScheduleObject.ToString())), ToHashSet(CausesOfLossObject.ToString()), ToHashSet(ExposureTypesObject.ToString()),
                    ResolvedSchedule, CoverageIdAttrMap);

            // Covers

            object[] covers = (object[])IR["Covers"];

            Covers = new List<ICover<Value, Value, Value>>(covers.Length);

            foreach (object cover in covers)
            {
                Dictionary<string, object> coverData = (Dictionary<string, object>)cover;

                Covers.Add(ConstructCover(coverData));
            }

            // Sublimits

            object[] sublimits = (object[])IR["Sublimits"];

            Sublimits = new List<ITerm<Value>>(sublimits.Length);

            foreach (object sublimit in sublimits)
            {
                Sublimits.Add(ConstructSublimit((Dictionary<string, object>)sublimit, AreSublimitsNetOfDeductible()));
            }

            // Deductibles

            object[] deductibles = (object[])IR["Deductibles"];

            Deductibles = new List<ITerm<Value>>(deductibles.Length);

            foreach (object deductible in deductibles)
            {
                Deductibles.Add(ConstructDeductible((Dictionary<string, object>)deductible, AreDeductiblesAbsorbable()));
            }
        }

        private HashSet<SymbolicValue> ToHashSet(string CSVString)
        {
            return new HashSet<SymbolicValue>(CSVString.Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new SymbolicValue(x))); 
        }

        #endregion

        #region Public APIs

        private IList<HoursClause> HoursClauses = null;

        public IList<HoursClause> GetHoursClauses()
        {
            if (HoursClauses != null)
                return HoursClauses;

            if (!Declarations.ContainsKey("Hours Clauses") 
                || (Declarations["Hours Clauses"] as object[]).Length == 0)
                return EMPTYLIST_HOURSCLAUSE;
            HoursClauses = 
                new List<HoursClause>((Declarations["Hours Clauses"] as object[]).Length);
            foreach (object _HoursClauseKVP in (Declarations["Hours Clauses"] as object[]))
            {
                Dictionary<string, object> HoursClauseKVP = _HoursClauseKVP as Dictionary<string, object>;
                HashSet<SymbolicValue> CausesOfLoss = null;
                if (HoursClauseKVP.ContainsKey("CausesOfLoss"))
                {
                    CausesOfLoss = new HashSet<SymbolicValue>();
                    foreach (string COL in ((string)HoursClauseKVP["CausesOfLoss"]).Split(','))
                        CausesOfLoss.Add(new SymbolicValue(COL.Trim()));
                }

                HoursClauses.Add(new HoursClause(int.Parse(HoursClauseKVP["Duration"].ToString()),
                        (HoursClause.Unit)Enum.Parse(typeof(HoursClause.Unit), 
                        (string)HoursClauseKVP["DurationTimeUnit"]), 
                        bool.Parse((string)HoursClauseKVP["OnlyOnce"]),
                        CausesOfLoss));
            }
            return HoursClauses;
        }

        public DateTime GetInception()
        {
            if (!Declarations.ContainsKey("Inception") || Declarations["Inception"] == null)
                return DateTime.Today;
            else
                return DateTime.Parse(Declarations["Inception"] as string);
        }

        public DateTime GetExpiration()
        {
            if (!Declarations.ContainsKey("Expiration") || Declarations["Expiration"] == null)
                return DateTime.Today.AddYears(1);
            else
                return DateTime.Parse(Declarations["Expiration"] as string);
        }

        public bool AreSublimitsNetOfDeductible()
        {
            return _AreSublimitsNetOfDeductible;
        }

        public bool AreDeductiblesAbsorbable()
        {
            return _AreDeductiblesAbsorbable;
        }

        public bool IsThisReinsurance()
        {
            if (!Declarations.ContainsKey("ContractType")
                || Declarations["ContractType"] == null
                || Declarations["ContractType"].ToString().ToUpper().Trim().Equals("INSURANCE")
                || Declarations["ContractType"].ToString().ToUpper().Trim().Equals("PRIMARY POLICY")
                || Declarations["ContractType"].ToString().ToUpper().Trim().Equals("PRIMARY")
                || Declarations["ContractType"].ToString().ToUpper().Trim().Equals("PRIMARY CONTRACT")
                || Declarations["ContractType"].ToString().ToUpper().Trim().Equals("INSURANCE CONTRACT"))
                return false;
            else
                return true;
        }

        public ISet<SymbolicValue> GetScheduleSymbols()
        {
            HashSet<SymbolicValue> AllScheduleSymbols = new HashSet<SymbolicValue>();
            //if (ContractSubject is Subject)
            //    AllScheduleSymbols.UnionWith((ContractSubject as Subject).Schedule.ScheduleSymbols);

            foreach (ICover<Value, Value, Value> Cover in Covers)
            {
                if (!Cover.GetSubject().isDerived)
                    AllScheduleSymbols.UnionWith((Cover.GetSubject() as Subject).Schedule.ScheduleSymbols);
            }

            foreach (ITerm<Value> Sublimit in Sublimits)
                AllScheduleSymbols.UnionWith((Sublimit.GetSubject() as Subject).Schedule.ScheduleSymbols);

            foreach (ITerm<Value> Deductible in Deductibles)
                AllScheduleSymbols.UnionWith((Deductible.GetSubject() as Subject).Schedule.ScheduleSymbols);

            return AllScheduleSymbols;
        }

        public ISet<SymbolicValue> GetCausesOfLoss()
        {
            HashSet<SymbolicValue> CausesOfLoss = new HashSet<SymbolicValue>(ContractSubject.CausesOfLoss);
            foreach (ITerm<Value> Term in Sublimits)
                CausesOfLoss.UnionWith(Term.GetSubject().CausesOfLoss);
            foreach (ITerm<Value> Term in Deductibles)
                CausesOfLoss.UnionWith(Term.GetSubject().CausesOfLoss);
            return CausesOfLoss;
        }

        public Dictionary<string, bool> GetPositionToOperation()
        {
            Dictionary<string, bool> GrossTrueCededFalsePositionMap = new Dictionary<string, bool>();
            if (ContractSubject is SubjectPosition)
            {
                foreach (SymbolicValue GrossPositionSymbol in ContractSubject.GrossPosition)
                {
                    GrossTrueCededFalsePositionMap.Add(GrossPositionSymbol.ToString(), true);
                }
                foreach (SymbolicValue CededPositionSymbol in ContractSubject.CededPosition)
                {
                    GrossTrueCededFalsePositionMap.Add(CededPositionSymbol.ToString(), false);
                }
            }
            return GrossTrueCededFalsePositionMap;
        }
        #endregion

        // Utilitiy to convert class name string to namespace-qualified name
        private static Type GetType(string ClassName, string TypesNamespacePrefix = "RMS.ContractObjectModel.")
        {
            if (ConcreteTypeMap.ContainsKey(ClassName))
                return ConcreteTypeMap[ClassName];

            if (ConcreteTypeMap.ContainsKey(TypesNamespacePrefix + ClassName))
                return ConcreteTypeMap[TypesNamespacePrefix + ClassName];

            //try
            //{
            //    if (Type.GetType(ClassName) != null)
            //        return Type.GetType(ClassName);
            //}
            //catch { }

            //try
            //{
            //    if (Type.GetType(TypesNamespacePrefix + ClassName) != null)
            //        return Type.GetType(TypesNamespacePrefix + ClassName);
            //}
            //catch { }

            Stack<int> GenericCounterStack = new Stack<int>();
            Stack<int> GenericStartIndexStack = new Stack<int>();
            Dictionary<int, int> GenericCountMapByStartIndex = new Dictionary<int, int>();
            int Count = 1;
            int GenericStartIndex = -1;
            int PreviousHighestGenericIndex = -1;

            foreach (char c in ClassName)
            {
                if (c.Equals('<'))
                {
                    if (GenericStartIndex != -1)
                    {
                        GenericCounterStack.Push(Count);
                        GenericStartIndexStack.Push(GenericStartIndex);
                    }
                    Count = 1;
                    GenericStartIndex = PreviousHighestGenericIndex + 1;
                    PreviousHighestGenericIndex = Math.Max(GenericStartIndex, PreviousHighestGenericIndex);
                }
                else if (c.Equals(','))
                {
                    Count++;
                }
                else if (c.Equals('>'))
                {
                    GenericCountMapByStartIndex[GenericStartIndex] = Count;
                    if (GenericCounterStack.Count != 0) 
                        Count = GenericCounterStack.Pop();
                    if (GenericStartIndexStack.Count != 0) 
                        GenericStartIndex = GenericStartIndexStack.Pop();
                }
            }
            
            ClassName = ClassName.Replace("<" + TypesNamespacePrefix, "<");
            
            StringBuilder FullyQualifiedClassName = new StringBuilder(TypesNamespacePrefix);
            
            GenericStartIndex = 0;
            foreach (char c in ClassName)
            {
                if (c.Equals('<'))
                {
                    FullyQualifiedClassName.Append("`" + GenericCountMapByStartIndex[GenericStartIndex].ToString() 
                        + "[" + TypesNamespacePrefix);
                    GenericStartIndex++;
                }
                else if (c.Equals(','))
                {
                    FullyQualifiedClassName.Append("," + TypesNamespacePrefix);
                }
                else if (c.Equals('>'))
                {
                    FullyQualifiedClassName.Append("]");
                }
                else
                    FullyQualifiedClassName.Append(c);
            }

            ClassName = FullyQualifiedClassName.ToString();

            return Type.GetType(ClassName);
        }

        #region Type Maps
        static readonly Dictionary <string, Type> ConcreteTypeMap = new Dictionary<string,Type>()
        {
            {"NumericValue", typeof(NumericValue)},
            {"SymbolicValue", typeof(SymbolicValue)},
            {"SimpleExpression<NumericValue>", typeof(SimpleExpression<NumericValue>)},
            {"SimpleExpression<SymbolicValue>", typeof(SimpleExpression<SymbolicValue>)},
            {"Percentage<NumericValue>", typeof(Percentage<NumericValue>)},
            {"Percentage<SymbolicValue>", typeof(Percentage<SymbolicValue>)},
            {"MoneyValue<NumericValue>", typeof(MoneyValue<NumericValue>)},
            {"MoneyValue<SymbolicValue>", typeof(MoneyValue<SymbolicValue>)},
            {"Money<NumericValue>", typeof(Money<NumericValue>)},
            {"Money<SymbolicValue>", typeof(Money<SymbolicValue>)},
            {"Ratio<NumericValue>", typeof(Ratio<NumericValue>)},
            {"Ratio<SymbolicValue>", typeof(Ratio<SymbolicValue>)},
            {"Participation<NumericValue>", typeof(Participation<NumericValue>)},
            {"Participation<SymbolicValue>", typeof(Participation<SymbolicValue>)},
            {"Cover<NumericValue>", typeof(Cover<NumericValue>)},
            {"Cover<SymbolicValue>", typeof(Cover<SymbolicValue>)},
            {"Cover<NumericValue,Value,NumericValue>", typeof(Cover<NumericValue,Value,NumericValue>)},
            {"Cover<NumericValue,NumericValue,NumericValue>", typeof(Cover<NumericValue,NumericValue,NumericValue>)},
            {"Cover<NumericValue,NumericValue,SymbolicValue>", typeof(Cover<NumericValue,NumericValue,SymbolicValue>)},
            {"Cover<NumericValue,SymbolicValue,NumericValue>", typeof(Cover<NumericValue,SymbolicValue,NumericValue>)},
            {"LimitSpecification<NumericValue>", typeof(LimitSpecification<NumericValue>)},
            {"LimitSpecification<SymbolicValue>", typeof(LimitSpecification<SymbolicValue>)},
            {"LimitSpecification<MoneyValue<NumericValue>>", typeof(LimitSpecification<MoneyValue<NumericValue>>)},
            {"LimitSpecification<MoneyValue<SymbolicValue>>", typeof(LimitSpecification<MoneyValue<SymbolicValue>>)},
            {"Limit<NumericValue>", typeof(Limit<NumericValue>)},
            {"Limit<SymbolicValue>", typeof(Limit<SymbolicValue>)},
            {"Limit<MoneyValue<NumericValue>>", typeof(Limit<MoneyValue<NumericValue>>)},
            {"Limit<MoneyValue<SymbolicValue>>", typeof(Limit<MoneyValue<SymbolicValue>>)},
            {"Attachment<NumericValue>", typeof(Attachment<NumericValue>)},
            {"Attachment<SymbolicValue>", typeof(Attachment<SymbolicValue>)},
            {"Attachment<MoneyValue<NumericValue>>", typeof(Attachment<MoneyValue<NumericValue>>)},
            {"Attachment<MoneyValue<SymbolicValue>>", typeof(Attachment<MoneyValue<SymbolicValue>>)},
            {"FunctionInvocation<int>", typeof(FunctionInvocation<int>)},
            {"FunctionInvocation<double>", typeof(FunctionInvocation<double>)},
            {"FunctionInvocation<string>", typeof(FunctionInvocation<string>)},
            {"FunctionInvocation<object>", typeof(FunctionInvocation<object>)},
            {"ArithmeticExpression", typeof(ArithmeticExpression)},
            {"ArithmeticTerm", typeof(ArithmeticTerm)}
        };
        #endregion

        private ICover<Value, Value, Value> ConstructCover(Dictionary<string, object> coverData)
        {
            // label

            string Label = null;

            if (coverData.ContainsKey("Label"))
                Label = (string)coverData["Label"];

            // participation

            string ParticipationValueType
                = (string)(((Dictionary<string, object>)coverData["Participation"])["ValueType"]);

            object Participation
                = ConstructParticipation(coverData);

            // _Limit

            object Limit = null;

            string LimitValueType = "Value";

            if (coverData.ContainsKey("LimitSpecification"))
            {
                Limit = ConstructLimit(coverData);

                LimitValueType
                    = (string)(((Dictionary<string, object>)coverData["LimitSpecification"])["ValueType"]);
            }

            // _Attachment

            object Attachment = null;

            string AttachmentValueType = "Value";

            if (coverData.ContainsKey("AttachmentSpecification"))
            {
                AttachmentValueType
                    = (string)(((Dictionary<string, object>)coverData["AttachmentSpecification"])["ValueType"]);

                Attachment = ConstructAttachment(coverData);
            }

            // Subject Constraint

            SubjectPosition _Subject = ConstructSubject(coverData);

            // Derived Subject, if any

            object DerivedSubject
                = null;

            if (coverData.ContainsKey("DerivedSubject"))
                DerivedSubject = ConstructDerivedSubject((Dictionary<string,object>)coverData["DerivedSubject"]);

            if (DerivedSubject != null)
            {
                _Subject.isNotConstrained = true;
                _Subject.isDerived = true;
            }

            // build cover

            return (ICover<Value, Value, Value>)Activator.CreateInstance(
                    GetType("Cover<" + ParticipationValueType + "," + LimitValueType + "," + AttachmentValueType + ">"),
                    Participation, Limit, Attachment, _Subject, DerivedSubject, Label);
        }

        private object ConstructParticipation(Dictionary<string, object> coverData)
        {
            Dictionary<string, object> ParticipationMap
                = (Dictionary<string, object>)coverData["Participation"];

            string ParticipationValueType
                    = (string)(ParticipationMap["ValueType"]);

            string ParticipationExpressionType
                = (string)(ParticipationMap["ExpressionType"]);

            object ParticipationValue =
                Activator.CreateInstance(GetType(ParticipationValueType),
                    ParticipationMap["Value"]);

            object ParticipationExpression
                = Activator.CreateInstance(GetType(ParticipationExpressionType), ParticipationValue);

            object ParticipationRatio
                = Activator.CreateInstance(GetType("Ratio<" + ParticipationValueType + ">"), ParticipationExpression);

            object Participation
                = Activator.CreateInstance(GetType("Participation<" + ParticipationValueType + ">"), ParticipationRatio);

            return Participation;
        }

        private object ConstructLimit(Dictionary<string, object> coverData)
        {
            Dictionary<string, object> LimitSpecificationMap
                = (Dictionary<string, object>)coverData["LimitSpecification"];

            object LimitSpecificationExpression = ConstructExpression(LimitSpecificationMap);

            string LimitValueType
                    = (string)(LimitSpecificationMap["ValueType"]);

            object LimitSpecification
                = Activator.CreateInstance(
                    GetType("LimitSpecification<" + LimitValueType + ">"),
                    LimitSpecificationExpression,
                    bool.Parse((string)LimitSpecificationMap["PAY"]));

            TimeBasis LimitTimeBasis = (TimeBasis)System.Enum.Parse(typeof(TimeBasis), (string)coverData["LimitTimeBasis"]);

            int NumberReinstatments = int.Parse(coverData["LimitReinstatements"].ToString());

            //if (NumberReinstatments == -1) // i.e. UNSPECIFIED
            //{
            //    NumberReinstatments = IsThisReinsurance() ? 0 : -1; // i.e. for primaries, leave it unspecified
            //}

            object Limit //= ConstructLimit(coverData);
                = Activator.CreateInstance(
                    GetType("Limit<" + LimitValueType + ">"),
                    LimitSpecification, LimitTimeBasis, NumberReinstatments);

            return Limit;
        }

        private object ConstructAttachment(Dictionary<string, object> coverData)
        {

            Dictionary<string, object> AttachmentSpecificationMap
                = (Dictionary<string, object>)coverData["AttachmentSpecification"];

            object AttachmentExpression = ConstructExpression(AttachmentSpecificationMap);

            string AttachmentValueType
                    = (string)(AttachmentSpecificationMap["ValueType"]);
            
            TimeBasis AttachmentTimeBasis = (TimeBasis)System.Enum.Parse(typeof(TimeBasis), (string)coverData["AttachmentTimeBasis"]);

            object Attachment //= ConstructLimit(coverData);
                = Activator.CreateInstance(
                    GetType("Attachment<" + AttachmentValueType + ">"),
                    AttachmentExpression, AttachmentTimeBasis,
                    bool.Parse((string)coverData["IsFranchise"]));

            return Attachment;
        }

        private ITerm<Value> ConstructSublimit(Dictionary<string, object> KeyValueData, bool IsItNetOfDeductible = false)
        {
            ITerm<Value> Term = (ITerm<Value>)ConstructTerm(KeyValueData);

            return (ITerm<Value>)Activator.CreateInstance(
                    GetType("Sublimit<" + Term.GetType().GetGenericArguments()[0] + ">"),
                    Term.GetExpression(), Term.GetSubject(),
                    Term.GetTimeBasis(), IsItNetOfDeductible, Term.GetLabel());
        }

        private ITerm<Value> ConstructDeductible(Dictionary<string, object> KeyValueData, bool IsItAbsorbable = false)
        {
            ITerm<Value> Term = (ITerm<Value>)ConstructTerm(KeyValueData);

            bool IsFranchise = false;
            if (KeyValueData.ContainsKey("IsFranchise"))
                IsFranchise = bool.Parse((string)KeyValueData["IsFranchise"]);

            Interaction _Interaction
                = (Interaction)Enum.Parse(typeof(Interaction), (string)KeyValueData["Interaction"]);

            return (ITerm<Value>)Activator.CreateInstance(
                    GetType("Deductible<" + Term.GetType().GetGenericArguments()[0] + ">"),
                    Term.GetExpression(), Term.GetSubject(), 
                    _Interaction, IsFranchise,
                    Term.GetTimeBasis(), Term.GetLabel(), IsItAbsorbable);
        }

        private object ConstructTerm(Dictionary<string, object> KeyValueData)
        {
            // label

            string Label = null;

            if (KeyValueData.ContainsKey("Label"))
                Label = (string)KeyValueData["Label"];

            // subject

            SubjectPosition _Subject = ConstructSubject(KeyValueData);

            TimeBasis _TimeBasis
                = (TimeBasis)System.Enum.Parse(typeof(TimeBasis), (string)KeyValueData["TimeBasis"]);

            object _Expression = ConstructExpression(KeyValueData);

            string _ValueType
               = (string)(KeyValueData["ValueType"]);

            object Term
                = Activator.CreateInstance(
                    GetType("Term<" + _ValueType + ">"),
                    _Expression, _Subject, _TimeBasis, Label);

            return Term;
        }

        private SubjectPosition ConstructSubject(Dictionary<string, object> KeyValueData)
        {
            object CausesOfLossObject;
            HashSet<SymbolicValue> CausesOfLoss = new HashSet<SymbolicValue>();
            if (KeyValueData.TryGetValue("CausesOfLoss", out CausesOfLossObject))
            {
                CausesOfLoss.UnionWith(ToHashSet(CausesOfLossObject.ToString()));
            }

            object ScheduleObject;
            HashSet<SymbolicValue> ScheduleSymbols = new HashSet<SymbolicValue>();
            if (KeyValueData.TryGetValue("Schedule", out ScheduleObject))
            {
                ScheduleSymbols.UnionWith(ToHashSet(ScheduleObject.ToString()));
            }
            Schedule _Schedule = new Schedule(ScheduleSymbols);

            object ExposureTypesObject;
            HashSet<SymbolicValue> ExposureTypes = new HashSet<SymbolicValue>();
            if (KeyValueData.TryGetValue("ExposureTypes", out ExposureTypesObject))
            {
                ExposureTypes.UnionWith(ToHashSet(ExposureTypesObject.ToString()));
            }

            bool Resolution = false;
            if (KeyValueData.ContainsKey("PerRisk"))
                Resolution = bool.Parse((string)KeyValueData["PerRisk"]);

            if ((CausesOfLoss.Count == 0) && (ScheduleSymbols.Count == 0) && (ExposureTypes.Count == 0))
            {
                SubjectPosition CoverSubjectPosition = new SubjectPosition();
                if (this.ContractSubject is Subject)
                {
                    CoverSubjectPosition = new Subject(this.ContractSubject as Subject);
                    ((Subject)CoverSubjectPosition).PerRisk = Resolution;
                }
                else if (this.ContractSubject is NetPosition)
                    CoverSubjectPosition = new NetPosition(this.ContractSubject as NetPosition);
                else if (this.ContractSubject is SubjectPosition)
                    CoverSubjectPosition = new SubjectPosition(this.ContractSubject as SubjectPosition);

                return CoverSubjectPosition;
            }


            return new Subject(UniversalSubject, _Schedule, CausesOfLoss, ExposureTypes, ResolvedSchedule, CoverageIdAttrMap, Resolution);
        }

        private object ConstructDerivedSubject(Dictionary<string, object> KeyValueData)
        {
            object[] Value = null;

            Value = (object[])Array.CreateInstance(GetType((string)KeyValueData["FunctionParameterValueType"]), 
                ((Object[])KeyValueData["Value"]).Length);
            int i = 0;
            foreach (object value in (Object[])KeyValueData["Value"])
            {
                object element = ConstructExpression((Dictionary<string, object>)value);
                Value[i++] = element;
            }

            object Expression
                = Activator.CreateInstance(typeof(FunctionInvocation<IValue<AValue>>), (string)KeyValueData["FunctionName"], Value);

            return Expression;
        }

        private object ConstructExpression(Dictionary<string, object> KeyValueData)
        {
            string ExpressionType
                = (string)(KeyValueData["ExpressionType"]);

            if (ExpressionType.Equals("ArithmeticExpression"))
                return ConstructArithmeticExpression((Dictionary<string, object>)KeyValueData["Value"]);

            string ValueType
                = (string)(KeyValueData["ValueType"]);

            string MonetaryExpressionValueType = null;

            if (KeyValueData.ContainsKey("MonetaryExpressionValueType"))
                MonetaryExpressionValueType
                    = (string)(KeyValueData["MonetaryExpressionValueType"]);

            string MonetaryExpressionType = null;

            if (KeyValueData.ContainsKey("MonetaryExpressionType"))
                MonetaryExpressionType
                    = (string)(KeyValueData["MonetaryExpressionType"]);

            object MonetaryValue = null;

            if ((MonetaryExpressionValueType != null) && (MonetaryExpressionType != null))
            {
                MonetaryValue =
                    Activator.CreateInstance(GetType(MonetaryExpressionValueType),
                        KeyValueData["Value"]);
            }

            object MonetaryExpression = null;

            if (MonetaryValue != null)
            {
                MonetaryExpression =
                    Activator.CreateInstance(GetType(MonetaryExpressionType), MonetaryValue);
            }

            object Value = null;

            if (KeyValueData["Value"].GetType().IsArray)
            {
                Value = (object[])Array.CreateInstance(GetType((string)KeyValueData["FunctionParameterValueType"]), ((Object[])KeyValueData["Value"]).Length);
                int i = 0;
                foreach (object value in (Object[])KeyValueData["Value"])
                {
                    object element = ConstructExpression((Dictionary<string, object>)value);
                    ((object[])Value)[i++] = element;
                }
            }
            else
                Value = (MonetaryExpression != null) ?
                            Activator.CreateInstance(GetType(ValueType), MonetaryExpression, (Currency)(Enum.Parse(typeof(Currency), (string)KeyValueData["Currency"])))
                            : Activator.CreateInstance(GetType(ValueType), KeyValueData["Value"]);

            object Expression
                = null;

            if (ExpressionType.StartsWith("FunctionInvocation"))
            {
                Expression
                    = Activator.CreateInstance(GetType(ExpressionType), (string)KeyValueData["FunctionName"], Value);
            }
            else
                Expression = Activator.CreateInstance(GetType(ExpressionType), Value);

            return Expression;
        }

        private object ConstructArithmeticExpression(Dictionary<string, object> _ArithmeticExpression)
        {
            object RightOperandTerm = ConstructArithmeticTerm((Dictionary<string, object>)_ArithmeticExpression["RightOperandTerm"]);

            object LeftOperandExpression = null;
            object Operator = null;

            if (_ArithmeticExpression.ContainsKey("LeftOperandExpression"))
            {
                LeftOperandExpression
                    = ConstructArithmeticExpression((Dictionary<string, object>)_ArithmeticExpression["LeftOperandExpression"]);
                Operator = (ArithmeticOperator)Enum.Parse(typeof(ArithmeticOperator), (string)_ArithmeticExpression["Operator"]);

                return Activator.CreateInstance(GetType("ArithmeticExpression"),
                    LeftOperandExpression, Operator, RightOperandTerm);
            }

            return Activator.CreateInstance(GetType("ArithmeticExpression"), RightOperandTerm);
        }

        private object ConstructArithmeticTerm(Dictionary<string, object> _ArithmeticTerm)
        {
            object RightOperandFactor = ConstructExpression((Dictionary<string, object>)_ArithmeticTerm["RightOperandFactor"]);

            object LeftOperandTerm = null;
            object Operator = null;

            if (_ArithmeticTerm.ContainsKey("LeftOperandTerm"))
            {
                LeftOperandTerm
                    = ConstructArithmeticTerm((Dictionary<string, object>)_ArithmeticTerm["LeftOperandTerm"]);
                Operator = (ArithmeticOperator)Enum.Parse(typeof(ArithmeticOperator), (string)_ArithmeticTerm["Operator"]);

                return Activator.CreateInstance(GetType("ArithmeticTerm"), 
                    LeftOperandTerm, Operator, RightOperandFactor);
            }

            return Activator.CreateInstance(GetType("ArithmeticTerm"), RightOperandFactor);
        }

        private object GetGenericInstance(Type GenericTypeTemplate, Type[] GenericTypeArgs, params object[] initiators)
        {
            //object Ratio = GetGenericInstance(typeof(Ratio<>),
            //        new Type[] { ConcreteTypeMap[ParticipationValueType] }, ParticipationExpression);
            Type genericExpressionType = GenericTypeTemplate.MakeGenericType(GenericTypeArgs);
            object instance =
                Activator.CreateInstance(genericExpressionType, initiators);
            return instance;
        }
    
    }
}
