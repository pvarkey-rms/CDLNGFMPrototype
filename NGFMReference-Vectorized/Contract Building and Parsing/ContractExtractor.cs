using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public abstract class ContractExtractor
    {
        protected ExposureDataAdaptor ExpData { get; private set; }
        public Declarations Declarations { get; private set; }
        
        protected Subject contractSubject;
        protected Dictionary<string, object> jsonParseResult;
        protected Dictionary<Subject, Dictionary<string, Cover>> CoverComponent;
        public long ID { get { return ExpData.ContractID; } }
        public bool IsTreaty { get { return ExpData.TreatyExposure; } }
        public Subject ContractSubject { get { return contractSubject; } }

        public ContractExtractor(ExposureDataAdaptor _expData)
        {
            ExpData = _expData;
            jsonParseResult = _expData.ContractJSON;
            CoverComponent = new Dictionary<Subject, Dictionary<string, Cover>>() ;
            Declarations = new Declarations();
            SetContractDeclarations();
        }

        public abstract void Extract();

        protected void SetContractDeclarations()
        {
            string message;
            DeclarationExtractor declarationExtractor = new DeclarationExtractor(ExpData);
            declarationExtractor.GetDeclarations(Declarations, out message);
        }

        protected abstract void SetContractSubject();

        protected abstract HashSet<Subject> GetSubjectForTerm(Dictionary<string, object> termDict);

        protected Subject GetSubjectForCover(Dictionary<string, object> coverDict)
        {
            if (coverDict.ContainsKey("DerivedSubject"))
            {               
                bool isPerRisk = false;
                if (coverDict.ContainsKey("PerRisk") & coverDict["PerRisk"].ToString() == "True")
                {
                    isPerRisk = true;
                }

                //PrimarySubject thisNewSub = new PrimarySubject(null, null, null, isPerRisk);               
                object component1;
                coverDict.TryGetValue("DerivedSubject", out component1);

                List<String> childrenList = new List<String>();
                Dictionary<string, object> tempDict = component1 as Dictionary<string, object>;
                //parse the Subject FunctionName
                String functionName;
                FunctionType functionType = FunctionType.Sum;
                if (tempDict.ContainsKey("FunctionName"))
                {
                    object comName;
                    tempDict.TryGetValue("FunctionName", out comName);                   
                    functionName = comName.ToString();
                    if (functionName == "Max")
                        functionType = FunctionType.Max;
                    else if (functionName == "Min")
                        functionType = FunctionType.Min;
                }
                PrimarySubject thisNewSub = new PrimarySubject(null, null, null, isPerRisk, functionType);
                thisNewSub.IsDerived = true;
                object component2;
                tempDict.TryGetValue("Value", out component2);
                object[] tempList2 = component2 as object[];
                foreach (object obj2 in tempList2)
                {
                    Dictionary<string, object> childrenCoverDict = obj2 as Dictionary<string, object>;
                    childrenList.Add(childrenCoverDict["Value"].ToString());
                }
                thisNewSub.ChildrenCoverNodeList = childrenList;
                return thisNewSub;
            }
            else 
                return GetSubjectForTerm(coverDict).First();
        }

        public bool GetCoverForName(Subject sub, string name, out Cover Cover)
        {
            bool found = false;
            Cover = null;
            Dictionary<string, Cover> coversForSub;
            if (CoverComponent.TryGetValue(sub, out coversForSub))
                if (coversForSub.TryGetValue(name, out Cover))
                    found = true;
            return found;
        }

        public bool GetCoversForSubject(Subject sub, out HashSet<Cover> Covers)
        {
            bool found = false;
            Covers = null;
            Dictionary<string, Cover> coversForSub;
            if (CoverComponent.TryGetValue(sub, out coversForSub))
            {
                Covers =  new HashSet<Cover>(coversForSub.Values);
                found = true;
            }

            return found;
        }

        public List<Subject> GetAllCoverSubjects()
        {
            return CoverComponent.Keys.ToList();
        }
    }

    public class TreatyContractExtractor : ContractExtractor
    {
        private GraphBuildCache graphCache;
        public Dictionary<long, Declarations> PrimaryDeclarationsSet { get; protected set; }

        public TreatyContractExtractor(ExposureDataAdaptor _expData, GraphBuildCache _graphCache)
            : base(_expData)
        {
            graphCache = _graphCache;
            PrimaryDeclarationsSet = new Dictionary<long, Declarations>();
            SetContractSubject();         
        }

        public override void Extract()
        {
            //Extract Covers
            object JSONCovers;
            jsonParseResult.TryGetValue("Covers", out JSONCovers);
            object[] Coverlist = JSONCovers as object[];

            foreach (object Obj in Coverlist)
            {
                Dictionary<string, object> Dict = Obj as Dictionary<string, object>;
                Cover cover = TermParser.GetCoverForTerm(Dict);
                Subject subject = GetSubjectForCover(Dict);
                if (CoverComponent.ContainsKey(subject))
                    CoverComponent[subject].Add(cover.CoverName, cover);
                else
                    CoverComponent.Add(subject, new Dictionary<string, Cover>() { { cover.CoverName, cover } });
            }

        }

        protected override void SetContractSubject()
        {
            List<string> grossPositions = Declarations.GrossPosition;
            List<string> cededPositions = Declarations.CededPosition;
            ScheduleOfContracts grossSchedule = new ScheduleOfContracts(string.Join( ",", grossPositions.ToArray()));
            ScheduleOfContracts cededSchedule = new ScheduleOfContracts(string.Join(",", cededPositions.ToArray())); ;
            HashSet<long> contractsIDs;

            //Build gross Position Schedule
            foreach (string grossPosition in grossPositions)
            {
                if (ExpData.Positions.ContractsForPosition(grossPosition, out contractsIDs))
                {
                    HashSet<GraphInfo> contractGraphs = BuildPosition(contractsIDs);

                    grossSchedule.AddItemList(contractGraphs);
                }
                else
                    throw new InvalidOperationException("Cannot find position: " + grossPosition + " in position data.");
            }

            //Build ceded Position Schedule
            foreach (string cededPosition in cededPositions)
            {
                if (ExpData.Positions.ContractsForPosition(cededPosition, out contractsIDs))
                {
                    HashSet<GraphInfo> contractGraphs = BuildPosition(contractsIDs);

                    cededSchedule.AddItemList(contractGraphs);
                }
                else
                    throw new InvalidOperationException("Cannot find position: " + cededPosition + " in position data.");
            }

            contractSubject = new ReinsuranceSubject(grossSchedule, cededSchedule, Declarations.ExposureTypes, Declarations.CausesofLoss);     
        }

        protected override HashSet<Subject> GetSubjectForTerm(Dictionary<string, object> termDict)
        {
            string expTypes = termDict["ExposureTypes"].ToString();
            string COLTypes = termDict["CausesOfLoss"].ToString();

            ExposureTypeCollection termExp;
            COLCollection termCOL;

            if (COLTypes == "")
                termCOL = contractSubject.CauseOfLossSet;
            else
                termCOL = new COLCollection(COLTypes);

            if (expTypes == "")
                termExp = contractSubject.ExposureTypes;
            else
                termExp = ExposureTypeCollection.BuildFromString(expTypes);

            string termSchedule = termDict["Schedule"].ToString();
            ScheduleOfContracts grossSchedule;
            ScheduleOfContracts cededSchedule;
            if (termSchedule == "")
            {
                ReinsuranceSubject treatyConCub = contractSubject as ReinsuranceSubject;
                grossSchedule = treatyConCub.GrossSchedule;
                cededSchedule = treatyConCub.CededSchedule;
            }
            else
                throw new NotSupportedException("Cannot support treaty terms with Schedule Of Contract subjects...");

            return new HashSet<Subject>() { new ReinsuranceSubject(grossSchedule, cededSchedule, termExp, termCOL) };
        }

        private HashSet<GraphInfo> BuildPosition(HashSet<long> conIDs)
        {
            HashSet<GraphInfo> position = new HashSet<GraphInfo>();
            foreach (long ID in conIDs)
            {
                GraphInfo contract;
                if (graphCache.GetGraphInfo(ID, out contract))
                    position.Add(contract);
                else
                {
                    GraphBuilder builder = new GraphBuilder(graphCache);
                    ExposureDataAdaptor expData = graphCache.GetExposure(ID);
                    GraphType type = graphCache.GetSettings(ID).GraphType;
                    IRITEindexMapper mapper = GetMapper(expData);

                    contract = builder.MakeGraph(type, expData, mapper);
                    graphCache.Add(ID, contract);
                    position.Add(contract);
                }
                PrimaryDeclarationsSet.Add(ID, contract.Graph.Declarations);
            }

            return position;
        }

        private IRITEindexMapper GetMapper(ExposureDataAdaptor expData)
        {
            IRITEindexMapper indexMapper;

            if (graphCache.GetIndexMapper(expData.ContractID, out indexMapper))
                return indexMapper;
            else
            {
                RAPSettings rapsettings = graphCache.GetSettings(expData.ContractID).RAPsettings;
                indexMapper = new RITEmapper1(expData, rapsettings, new RMSSubPerilConfig());
                graphCache.Add(expData.ContractID, indexMapper);
                return indexMapper;
            }
        }
    }

    public class PrimaryContractExtractor : ContractExtractor
    {
        protected Dictionary<PrimarySubject, DeductibleCollection> DedComponent;
        protected Dictionary<PrimarySubject, LimitCollection> LimComponent;


        public PrimaryContractExtractor(ExposureDataAdaptor _expData)
            : base(_expData)
        {
            DedComponent = new Dictionary<PrimarySubject, DeductibleCollection>();
            LimComponent = new Dictionary<PrimarySubject, LimitCollection>() ;
            SetContractSubject();
        }

        public override void Extract()
        {
            //Extract Deductibles
            object JSONDeductibles;
            jsonParseResult.TryGetValue("Deductibles", out JSONDeductibles);
            object[] Dedlist = JSONDeductibles as object[];

            foreach (object Obj in Dedlist)
            {
                Dictionary<string, object> Dict = Obj as Dictionary<string, object>;
                Deductible ded = TermParser.GetDedForTerm(Dict, Declarations);
                foreach (PrimarySubject priSub in GetSubjectForTerm(Dict))
                {
                    if (DedComponent.ContainsKey(priSub))
                        DedComponent[priSub].Add(ded);
                    else
                        DedComponent.Add(priSub, new DeductibleCollection(ded));
                }
            }

            //Extract Sublimits
            object JSONLimits;
            jsonParseResult.TryGetValue("Sublimits", out JSONLimits);
            object[] Limlist = JSONLimits as object[];

            foreach (object Obj in Limlist)
            {
                Dictionary<string, object> Dict = Obj as Dictionary<string, object>;
                Limit ded = TermParser.GetLimitForTerm(Dict, Declarations);
                foreach (PrimarySubject priSub in GetSubjectForTerm(Dict))
                {
                    if (LimComponent.ContainsKey(priSub))
                        LimComponent[priSub].Add(ded);
                    else
                        LimComponent.Add(priSub, new LimitCollection(ded));
                }
            }

            //Extract Covers
            object JSONCovers;
            jsonParseResult.TryGetValue("Covers", out JSONCovers);
            object[] Coverlist = JSONCovers as object[];

            foreach (object Obj in Coverlist)
            {
                Dictionary<string, object> Dict = Obj as Dictionary<string, object>;
                Cover cover = TermParser.GetCoverForTerm(Dict);
                Subject subject = GetSubjectForCover(Dict);
                if (CoverComponent.ContainsKey(subject))
                    CoverComponent[subject].Add(cover.CoverName, cover);
                else
                    CoverComponent.Add(subject, new Dictionary<string, Cover>() {{cover.CoverName, cover}});
            }
        }

        protected override void SetContractSubject()
        {
            //Set Universal Subject
            if (Declarations.Schedule == null)
                Declarations.Schedule = new ScheduleOfRITEs("TotalContractSchedule", ExpData.ContractRITES, ExpData.Characteristics);

            contractSubject = new PrimarySubject(Declarations.Schedule, Declarations.ExposureTypes, Declarations.CausesofLoss);
        }

        protected override HashSet<Subject> GetSubjectForTerm(Dictionary<string, object> termDict)
        {
            string expTypes = termDict["ExposureTypes"].ToString();
            string COLTypes = termDict["CausesOfLoss"].ToString();  

            ExposureTypeCollection termExp;
            COLCollection termCOL;

            if (COLTypes == "")
                termCOL = contractSubject.CauseOfLossSet;
            else
                termCOL = new COLCollection(COLTypes);

            if (expTypes == "")
                termExp = contractSubject.ExposureTypes;
            else
                termExp = ExposureTypeCollection.BuildFromString(expTypes);

            string termSchedule = termDict["Schedule"].ToString();
            ScheduleOfRITEs schedule = ExpData.GetSchedule(termSchedule);

            bool isPerRisk = false;
            if (termDict.ContainsKey("PerRisk") & termDict["PerRisk"].ToString() == "True")
            {
                isPerRisk = true;
            }

            PrimarySubject sub = new PrimarySubject(schedule, termExp, termCOL, isPerRisk);

            //DO NOT Explode
            //if (termDict.ContainsKey("PerRisk")
            //    & termDict["PerRisk"].ToString() == "True")
            //    return new HashSet<Subject>(ExplodeSubjectForPerRisk(sub).Cast<Subject>());
            //else
            //    return new HashSet<Subject>(){sub};
              
            return new HashSet<Subject>() { sub };
        }

        private HashSet<PrimarySubject> ExplodeSubjectForPerRisk(PrimarySubject primarySub)
        {
            //Per Risk, expand the schedule to all location subschedules
            if (primarySub.Schedule.IsLocation)
                return new HashSet<PrimarySubject>() { primarySub };

            HashSet<PrimarySubject> ExplodedSubjects = new HashSet<PrimarySubject>();
            foreach (RITE Rite in primarySub.Schedule.ScheduleList)
            {
                //Check if schedule already exists in exposure data with RITE
                ScheduleOfRITEs schedule;
                if (FindScheduleWithRite(Rite, out schedule))
                    ExplodedSubjects.Add(new PrimarySubject(schedule, primarySub.ExposureTypes, primarySub.CauseOfLossSet));
                else
                {
                    //Create new schedule in exposure data
                    string newScheduleName = primarySub.Schedule.Name + " ." + Rite.ExposureID;
                    ExpData.AddSchedule(newScheduleName, new HashSet<RITE>() { Rite });
                    ExplodedSubjects.Add(new PrimarySubject(ExpData.GetSchedule(newScheduleName), primarySub.ExposureTypes, primarySub.CauseOfLossSet));
                }
            }

            return ExplodedSubjects;
        }

        private bool FindScheduleWithRite(RITE rite, out ScheduleOfRITEs schedule)
        {
            schedule = null;
            bool found = false;
            foreach (ScheduleOfRITEs sch in ExpData.Schedules)
            {
                if (sch.IsLocation &&
                    sch.ScheduleList.First().Equals(rite))
                {
                    schedule = sch;
                    found = true;
                }
            }

            return found;
        }

        public bool GetDeductiblesForSubject(PrimarySubject sub, out DeductibleCollection deductibles)
        {
            if (DedComponent.TryGetValue(sub, out deductibles))
                return true;
            else
                return false;
        }

        public bool GetLimitsForSubject(PrimarySubject sub, out LimitCollection limits)
        {
            if (LimComponent.TryGetValue(sub, out limits))
                return true;
            else
                return false;
        }

        public List<PrimarySubject> GetAllTermSubjects()
        {
            List<PrimarySubject> AllSubs = DedComponent.Keys.ToList();

            foreach (PrimarySubject priSub in LimComponent.Keys)
            {
                if (!AllSubs.Contains(priSub))
                    AllSubs.Add(priSub);
            }

            return AllSubs;
        }
    }

    public class ExtractionOptions
    {
        public bool IsReinsurance { get; set; }

    }
}
