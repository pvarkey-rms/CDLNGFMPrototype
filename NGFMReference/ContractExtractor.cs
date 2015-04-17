using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public abstract class ContractExtractor
    {
        protected ExposureDataAdaptor ExpData { get; private set; }
        public Declarations Declarations { get; private set; }
        protected Subject ContractSubject;
        protected Dictionary<string, object> jsonParseResult;
        protected Dictionary<Subject, Dictionary<string, Cover>> CoverComponent;

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
                PrimarySubject thisNewSub = new PrimarySubject(null, null, null);
                thisNewSub.IsDerived = true;
                object component1;
                coverDict.TryGetValue("DerivedSubject", out component1);

                List<String> childrenList = new List<String>();
                Dictionary<string, object> tempDict = component1 as Dictionary<string, object>;
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

        public TreatyContractExtractor(ExposureDataAdaptor _expData, GraphBuildCache _graphCache)
            : base(_expData)
        {
            graphCache = _graphCache;
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
            string position = Declarations.GrossPosition;
            HashSet<long> contractsIDs;

            if (ExpData.Positions.ContractsForPosition(position, out contractsIDs))
            {
                HashSet<Graph> contractGraphs = BuildPosition(contractsIDs);
                ScheduleOfContracts schedule = new ScheduleOfContracts(position, contractGraphs);
                ContractSubject = new ReinsuranceSubject(schedule, Declarations.ExposureTypes, Declarations.CausesofLoss);
            }
            else
                throw new InvalidOperationException("Cannot find position: " + position + " in position data.");
        }

        protected override HashSet<Subject> GetSubjectForTerm(Dictionary<string, object> termDict)
        {
            string expTypes = termDict["ExposureTypes"].ToString();
            string COLTypes = termDict["CausesOfLoss"].ToString();

            ExposureTypeCollection termExp;
            COLCollection termCOL;

            if (COLTypes == "")
                termCOL = ContractSubject.CauseOfLossSet;
            else
                termCOL = new COLCollection(COLTypes);

            if (expTypes == "")
                termExp = ContractSubject.ExposureTypes;
            else
                termExp = ExposureTypeCollection.BuildFromString(expTypes);

            string termSchedule = termDict["Schedule"].ToString();
            ScheduleOfContracts schedule;
            if (termSchedule == "")
            {
                ReinsuranceSubject treatyConCub = ContractSubject as ReinsuranceSubject;
                schedule = treatyConCub.Schedule;
            }
            else
                throw new NotSupportedException("Cannot support treaty terms with Schedule Of Contract subjects...");

            return new HashSet<Subject>(){new ReinsuranceSubject(schedule, termExp, termCOL)};
        }

        private HashSet<Graph> BuildPosition(HashSet<long> conIDs)
        {
            HashSet<Graph> position = new HashSet<Graph>();
            foreach (long ID in conIDs)
            {
                Graph contract;
                if (graphCache.GetContract(ID, out contract))
                    position.Add(contract);
                else
                {
                    GraphBuilder builder = new GraphBuilder(graphCache);
                    ExposureDataAdaptor expData = graphCache.GetExposure(ID);
                    GraphType type = graphCache.GetSettings(ID).GraphType;

                    contract = builder.MakeGraph(type, expData);
                    graphCache.Add(ID, contract);
                    position.Add(contract);
                }
            }

            return position;
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

            ContractSubject = new PrimarySubject(Declarations.Schedule, Declarations.ExposureTypes, Declarations.CausesofLoss);
        }

        protected override HashSet<Subject> GetSubjectForTerm(Dictionary<string, object> termDict)
        {
            string expTypes = termDict["ExposureTypes"].ToString();
            string COLTypes = termDict["CausesOfLoss"].ToString();

            ExposureTypeCollection termExp;
            COLCollection termCOL;

            if (COLTypes == "")
                termCOL = ContractSubject.CauseOfLossSet;
            else
                termCOL = new COLCollection(COLTypes);

            if (expTypes == "")
                termExp = ContractSubject.ExposureTypes;
            else
                termExp = ExposureTypeCollection.BuildFromString(expTypes);

            string termSchedule = termDict["Schedule"].ToString();
            ScheduleOfRITEs schedule = ExpData.GetSchedule(termSchedule);

            PrimarySubject sub = new PrimarySubject(schedule, termExp, termCOL);

            if (termDict.ContainsKey("PerRisk")
                & termDict["PerRisk"].ToString() == "True")
                return new HashSet<Subject>(ExplodeSubjectForPerRisk(sub).Cast<Subject>());
            else
                return new HashSet<Subject>(){sub};
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
