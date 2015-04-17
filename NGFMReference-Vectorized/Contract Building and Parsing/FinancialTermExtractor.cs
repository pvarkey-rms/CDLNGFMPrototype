using System; 
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;
using NGFMReference.ContractModel;

using Rms.DataServices.DataObjects;

namespace NGFMReference
{
    public abstract class FinancialTermExtractor
    {
        protected ExposureDataAdaptor ExpData{get; private set;}
        protected Declarations Declarations { get; private set; }
        protected Subject ContractSubject;

        public FinancialTermExtractor(ExposureDataAdaptor _expData, Declarations _Declarations)
        {
            ExpData = _expData;
            Declarations = _Declarations;
        }

        public abstract bool GetTermsForGraph(GraphOfNodes graph, out String error);

        protected abstract void SetContractSubject();

        public delegate void UpdateGraphDelegate(Dictionary<string, object> termDictionary, GraphOfNodes graph);

        protected bool GetContractComponentForGraph(GraphOfNodes graph, Dictionary<string, object> jsonParseResult, string ComponentName, out string message)
        {
            message = "";
            object Component;
            jsonParseResult.TryGetValue(ComponentName, out Component); 
            object[] list = Component as object[];
            UpdateGraphDelegate UpdateGraphForComponent;

            switch (ComponentName)
            {
                case "Deductibles":
                    UpdateGraphForComponent = new UpdateGraphDelegate(UpdateGraphWithDed);
                    break;
                case "Sublimits":
                    UpdateGraphForComponent = new UpdateGraphDelegate(UpdateGraphWithLim);
                    break;
                case "Covers":
                    UpdateGraphForComponent = new UpdateGraphDelegate(UpdateGraphWithCover);
                    break;
                default:
                    message = "Cannot handle financial term updates from Contract Component " + ComponentName + " at this time.";
                    return false;
            }

            foreach (object Obj in list)
            {
                Dictionary<string, object> Dict = Obj as Dictionary<string, object>;
                try
                {
                    UpdateGraphForComponent(Dict, graph);
                }
                catch (InvalidOperationException e)
                {
                    message = "Error getting terms for Contract Component " + ComponentName + ": " + e.Message;
                    return false;
                }
            }

            return true;    
        }

        private void UpdateGraphWithDed(Dictionary<string, object> termDictionary, GraphOfNodes graph)
        {
            bool nodefound = false;
            int count = 0;
            Subject cdlSub = GetSubjectForTerm(termDictionary);

            foreach (GraphNode node in graph.GraphNodes)
            {
                count++;
                if (node is TermNode)
                {
                    TermNode termNode = node as TermNode;
                    
                    if (termNode.Subject.Equals(cdlSub))
                    {
                        nodefound = true;
                        Deductible ded = TermParser.GetDedForTerm(termDictionary, graph.Declarations);
                        termNode.Deductibles.Add(ded);
                        break;
                    }
                }
            }
            if (!nodefound)
                throw new InvalidOperationException("Cannot find Node in fixed graph, for JSON term with subject: (" + cdlSub.ToString() + ")");
        }
        
        private void UpdateGraphWithLim(Dictionary<string, object> termDictionary, GraphOfNodes graph)
        {
            bool nodefound = false;
            Subject cdlSub = GetSubjectForTerm(termDictionary);

            foreach (GraphNode node in graph.GraphNodes)
            {
                if (node is TermNode)
                {
                    TermNode termNode = node as TermNode;
                 
                    if (termNode.Subject.Equals(cdlSub))
                    {
                        nodefound = true;
                        Limit limit = TermParser.GetLimitForTerm(termDictionary, graph.Declarations);
                        termNode.Limits.Add(limit);
                        break;
                    }
                }
            }
            if (!nodefound)
                throw new InvalidOperationException("Cannot find Node in fixed graph, for JSON term with subject: (" + cdlSub.ToString() + ")");
        }
        
        private void UpdateGraphWithCover(Dictionary<string, object> coverDictionary, GraphOfNodes graph) 
        {
            bool nodefound = false;
            Type type = typeof(DedInteractionType);
            Subject sub = GetSubjectForCover(coverDictionary);

            foreach (GraphNode node in graph.GraphNodes)
            {
                if (node is CoverNode)
                {
                    CoverNode coverNode = node as CoverNode;  
                    Cover cover = TermParser.GetCoverForTerm(coverDictionary);
                    if (coverNode.Subject.Equals(sub)
                        && coverNode.CoverName.Trim() == cover.CoverName.Trim())                    
                    {
                        nodefound = true;
                        coverNode.Cover = cover;
                        break;
                    }
                }
            }
            if (!nodefound)
                throw new InvalidOperationException("Cannot find Node in fixed graph, for JSON cover with subject: (" + sub.ToString() + ")");
        }
  
        protected abstract Subject GetSubjectForTerm(Dictionary<string, object> termDict);

        private Subject GetSubjectForCover(Dictionary<string, object> coverDict)
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
                return GetSubjectForTerm(coverDict);
        }

        protected COLCollection GetCOLHashSet(string inputstring)
        {
            if (inputstring == "")
                return ContractSubject.CauseOfLossSet;
            else
                return new COLCollection(inputstring);     
        }

        protected ExposureTypeCollection GetExpTypeHashSet(string inputstring)
        {
            if (inputstring == "Loss" || inputstring == "")
                inputstring = "Building, Contents, BI";
            Type ExpType = typeof(ExposureType);
            IEnumerable<string> stringList = inputstring.Split(',').Select(str => str.Trim());
            IEnumerable<ExposureType> colList = stringList.Select(str => (ExposureType)Enum.Parse(ExpType, str));

            return new ExposureTypeCollection (new HashSet<ExposureType>(colList));
        }

     
        //private double SetTIVFromEDS(RITCharacteristic RITChar, ContractExposure exp)
        //{
        //    double TIV = 0;
        //    bool ritefound = false;
        //    //hardcoded to take first Subeject Exposure...will handle multiple sublect exposuires later..
        //    ContractSubjectExposureOfRiteSchedule subjectExposures = exp.ContractSubjectExposures[0] as ContractSubjectExposureOfRiteSchedule;

        //    if (subjectExposures != null)
        //    {
        //        foreach (RITExposure RiskLoc in subjectExposures.RITECollectionExposure.RITExposures)
        //        {
        //            List<RiskItemCharacteristicsValuation> EDSrites = RiskLoc.RiskitemCharacteristicsList.Items;
        //            foreach (RiskItemCharacteristicsValuation EDSrite in EDSrites)
        //            {
        //                if (EDSrite.Id == RITChar.ID)
        //                {
        //                    ritefound = true;
        //                    RITChar.TIV = GetTIVFromRiskItemCharacteristic(EDSrite);
        //                    break;
        //                }
        //            }
        //            if (ritefound)
        //                break;
        //        }

        //        if (!ritefound)
        //            throw new ArgumentOutOfRangeException("Cannot find RITE characteristic with ID: " + RITChar.ID + " in Contract exposure...");

        //        return TIV;
        //    }
        //    else
        //        throw new InvalidOperationException("Contract subject exposure of type portflio, not RITE..");       
        //}

        //private double GetTIVFromRiskItemCharacteristic(RiskItemCharacteristicsValuation RITEChar)
        //{
        //    double TIV = 0;
        //    if (RITEChar.RITExposureValuationList.Count > 0)
        //    {
        //        foreach (RITExposureValuation ExpValuation in RITEChar.RITExposureValuationList)
        //        {
        //            TIV = TIV + ExpValuation.Value;
        //        } 
        //    }
        //    else
        //        throw new ArgumentException("RITECharacteristic has an empty valutaion list..");

        //    return TIV;
        //}
    }

    public class PrimaryTermExtractor : FinancialTermExtractor
    {
        public PrimaryTermExtractor(ExposureDataAdaptor _expData, Declarations _Declarations)
            :base(_expData, _Declarations)
        {
            SetContractSubject();
        }

        public override bool GetTermsForGraph(GraphOfNodes graph, out String error)
        {
            Dictionary<string, object> jsonParseResult = ExpData.ContractJSON;

            error = "";
            bool success;

            success = GetContractComponentForGraph(graph, jsonParseResult, "Deductibles", out error);
            if (!success)
                return false;
            success = GetContractComponentForGraph(graph, jsonParseResult, "Sublimits", out error);
            if (!success)
                return false;
            success = GetContractComponentForGraph(graph, jsonParseResult, "Covers", out error);
            if (!success)
                return false;

            return true;
        }

        protected override void SetContractSubject()
        {
            ContractSubject = new PrimarySubject(Declarations.Schedule, Declarations.ExposureTypes, Declarations.CausesofLoss);
        }

        protected override Subject GetSubjectForTerm(Dictionary<string, object> termDict)
        {
            string termSchedule = termDict["Schedule"].ToString();
            COLCollection termCOL = GetCOLHashSet(termDict["CausesOfLoss"].ToString());
            ExposureTypeCollection termExp = GetExpTypeHashSet(termDict["ExposureTypes"].ToString());
            ScheduleOfRITEs schedule = GetSchedule(termSchedule);

            PrimarySubject sub = new PrimarySubject(schedule, termExp, termCOL);

            return sub;
        }

        private ScheduleOfRITEs GetSchedule(string name)
        {
            return ExpData.Schedules.Where(sch => sch.Name == name).FirstOrDefault();
        }

    }

    public class TreatyTermExtractor : FinancialTermExtractor
    {
        private GraphBuildCache graphCache;

        public TreatyTermExtractor(ExposureDataAdaptor _expData, Declarations _Declarations, GraphBuildCache _graphCache)
            : base(_expData, _Declarations)
        {
            graphCache = _graphCache;
            SetContractSubject();
        }

        public override bool GetTermsForGraph(GraphOfNodes graph, out String error)
        {
            Dictionary<string, object> jsonParseResult = ExpData.ContractJSON;

            error = "";
            bool success;

            success = GetContractComponentForGraph(graph, jsonParseResult, "Covers", out error);
            if (!success)
                return false;

            return true;
        }

        protected override void SetContractSubject()
        {
            List<string> grossPositions = Declarations.GrossPosition;
            List<string> cededPositions = Declarations.CededPosition;
            ScheduleOfContracts grossSchedule = new ScheduleOfContracts(string.Join(",", grossPositions.ToArray()));
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

            ContractSubject = new ReinsuranceSubject(grossSchedule, cededSchedule, Declarations.ExposureTypes, Declarations.CausesofLoss); 
        }

        protected override Subject GetSubjectForTerm(Dictionary<string, object> termDict)
        {
            string termSchedule = termDict["Schedule"].ToString();
            COLCollection termCOL = GetCOLHashSet(termDict["CausesOfLoss"].ToString());
            ExposureTypeCollection termExp = GetExpTypeHashSet(termDict["ExposureTypes"].ToString());

            ScheduleOfContracts schedule;
            if (termSchedule == "")
            {
                ReinsuranceSubject treatyConCub = ContractSubject as ReinsuranceSubject;
                schedule = treatyConCub.GrossSchedule;
            }
            else
                throw new NotSupportedException("Cannot support treaty terms with Schedule Of Contract subjects...");

            return new ReinsuranceSubject(schedule, termExp, termCOL);
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

    public class DeclarationExtractor
    {
        private ExposureDataAdaptor expData;

        public DeclarationExtractor(ExposureDataAdaptor _expData)
        {
            expData = _expData;
        }

        public bool GetDeclarations(Declarations declarations, out string message)
        {
            message = "";
            object Component;
            expData.ContractJSON.TryGetValue("Declarations", out Component);
            Dictionary<String, Object> list = Component as Dictionary<String, object>;

            return SetDeclarations(declarations, list, out message);

        }

        private bool SetDeclarations(Declarations graph_declarations, Dictionary<String, Object> cdl_declarations, out string message)
        {
            message = "";
            try
            {
                graph_declarations.Name = Convert.ToString(cdl_declarations["Name"]);

                if (cdl_declarations.ContainsKey("Currency"))
                    graph_declarations.Currency = Convert.ToString(cdl_declarations["Currency"]);
                if (cdl_declarations.ContainsKey("Inception"))
                    graph_declarations.Inception = Convert.ToDateTime(cdl_declarations["Inception"]);
                else
                    graph_declarations.Inception = DateTime.Now;
                if (cdl_declarations.ContainsKey("Expiration"))
                    graph_declarations.Expiration = Convert.ToDateTime(cdl_declarations["Expiration"]);
                else
                    graph_declarations.Expiration = graph_declarations.Inception.AddYears(1);

                if (cdl_declarations.ContainsKey("ContractType"))
                    graph_declarations.ContractType = Convert.ToString(cdl_declarations["ContractType"]);
                else
                    throw new NotSupportedException("This prototype requies CDL to specify a recognized Contract Type");

                if (cdl_declarations.ContainsKey("CausesOfLoss"))
                    graph_declarations.CausesofLoss = new COLCollection(Convert.ToString(cdl_declarations["CausesOfLoss"]));
                else
                    graph_declarations.CausesofLoss = CauseOfLoss.GetDefaultCOLs();

                if (cdl_declarations.ContainsKey("ExposureTypes"))
                    graph_declarations.ExposureTypes = ExposureTypeCollection.BuildFromString(Convert.ToString(cdl_declarations["ExposureTypes"]));
                else
                    graph_declarations.ExposureTypes = ExposureTypeCollection.GetDefaultExposureTypes();
                
                if (cdl_declarations.ContainsKey("Schedule"))
                {
                    try {graph_declarations.Schedule = expData.GetSchedule(Convert.ToString(cdl_declarations["Schedule"]));}
                    catch(Exception ex){graph_declarations.Schedule = null;}
                }

                if (cdl_declarations.ContainsKey("GrossPosition"))
                    graph_declarations.GrossPosition = Convert.ToString(cdl_declarations["GrossPosition"]).Split(',').ToList();
                else
                    graph_declarations.GrossPosition = new List<string>();

                if (cdl_declarations.ContainsKey("CededPosition"))
                    graph_declarations.CededPosition = Convert.ToString(cdl_declarations["CededPosition"]).Split(',').ToList();
                else
                    graph_declarations.CededPosition = new List<string>();

                //Handle Claims Adjustment Options
                if (cdl_declarations.ContainsKey("Claims Adjustment Options"))
                {
                    Dictionary<String, Object> ClaimsAdjustmentOptions = cdl_declarations["Claims Adjustment Options"] as Dictionary<String, Object>;
                    if (ClaimsAdjustmentOptions.ContainsKey("Claims Adjustment Sublimits"))
                    {
                        string sublimitAdjustment = ClaimsAdjustmentOptions["Claims Adjustment Sublimits"].ToString();
                        if (sublimitAdjustment == "Net Of Deductible")
                            graph_declarations.GroundUpSublimits = false;
                        else if (sublimitAdjustment == "GroundUp")
                            graph_declarations.GroundUpSublimits = true;
                        else
                            throw new JSONParseException("Cannot support Claims Adjustment Option for Sublimits: " + sublimitAdjustment);
                    }

                    if (ClaimsAdjustmentOptions.ContainsKey("Claims Adjustment Deductibles"))
                    {
                        string dedAdjustment = ClaimsAdjustmentOptions["Claims Adjustment Deductibles"].ToString();
                        if (dedAdjustment == "Absorbable")
                            graph_declarations.MinimumAbsorbingDed = true;
                        else
                            throw new JSONParseException("Cannot support Claims Adjustment Option for Deductibles: " + dedAdjustment);
                    }
                }
                
                //Get Hours clause declarations
                graph_declarations.HoursClauses = new List<HoursClause>();
                Object[] hoursclauses;
                if (cdl_declarations.ContainsKey("Hours Clauses"))
                {
                    hoursclauses = cdl_declarations["Hours Clauses"] as object[];
                    List<Object> lst_hoursclauses = hoursclauses.OfType<Object>().ToList();

                    foreach (Dictionary<String, Object> hc in lst_hoursclauses)
                    {
                        HoursClause objHC = new HoursClause();
                        objHC.SetHoursClause(hc);
                        graph_declarations.HoursClauses.Add(objHC);
                    }

                    graph_declarations.IsHoursClause = true;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }


    }
}
