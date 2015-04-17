using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;

using Rms.DataServices.DataObjects;

namespace NGFMReference
{
    //class GraphNodeBuilder : FinancialTermExtractor
    //{
    //    public GraphNodeBuilder(ExposureDataAdaptor expData)
    //        : base(expData)
    //    {

    //    }

    //    public bool GetGraphNodesList(out List<GraphNode> _graphNode, out String error)
    //    {
    //        _graphNode = new List<GraphNode>();
    //        Dictionary<string, object> jsonParseResult = ExpData.ContractJSON;

    //        error = "";
    //        bool success;

    //        success = GetContractComponent(_graphNode, jsonParseResult, "Deductibles", out error);
    //        if (!success)
    //            return false;
    //        success = GetContractComponent(_graphNode, jsonParseResult, "Sublimits", out error);
    //        if (!success)
    //            return false;
    //        success = GetContractComponent(_graphNode, jsonParseResult, "Covers", out error);
    //        if (!success)
    //            return false;

    //        return true;
    //    }

    //    public delegate void AddNodeDelegate(List<GraphNode> nodeList, Dictionary<string, object> termDictionary);

    //    private bool GetContractComponent(List<GraphNode> nodeList, Dictionary<string, object> jsonParseResult, string ComponentName, out string message)
    //    {
    //        message = "";
    //        object Component;
    //        jsonParseResult.TryGetValue(ComponentName, out Component);
    //        object[] compList = Component as object[];
    //        AddNodeDelegate AddNodeForComponent;

    //        switch (ComponentName)
    //        {
    //            case "Deductibles":
    //                AddNodeForComponent = new AddNodeDelegate(AddNodeForDed);
    //                break;
    //            case "Sublimits":
    //                AddNodeForComponent = new AddNodeDelegate(AddNodeForLim);
    //                break;
    //            case "Covers":
    //                AddNodeForComponent = new AddNodeDelegate(AddNodeForCover);
    //                break;
    //            default:
    //                message = "Cannot handle financial term updates from Contract Component " + ComponentName + " at this time.";
    //                return false;
    //        }

    //        foreach (object Obj in compList)
    //        {
    //            Dictionary<string, object> Dict = Obj as Dictionary<string, object>;
    //            try
    //            {
    //                AddNodeForComponent(nodeList, Dict);
    //            }
    //            catch (InvalidOperationException e)
    //            {
    //                message = "Error getting terms for Contract Component " + ComponentName + ": " + e.Message;
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //    private void AddNodeForDed(List<GraphNode> nodeList, Dictionary<string, object> termDictionary)
    //    {
    //        bool nodefound = false;
    //        Type type = typeof(DedInteractionType);
    //        List<PrimarySubject> thisTermSubList = GetSubjectForTerm(termDictionary);

    //        bool isPerRisk = false;
    //        if (termDictionary["PerRisk"].ToString() == "True")
    //            isPerRisk = true;

    //        int index = Convert.ToInt32(termDictionary["Index"]);
    //        bool franchise = Convert.ToBoolean(termDictionary["IsFranchise"]);
    //        DedInteractionType dedInterType = (DedInteractionType)Enum.Parse(type, termDictionary["Interaction"].ToString());
    //        double dedValue;
    //        if (!TryConvert(termDictionary["Value"], out dedValue))
    //            throw new InvalidOperationException("Deductible with index " + index.ToString() + " is not coded as monetary amount in CDL, cannot be supported at this time...");

    //        foreach (PrimarySubject sub in thisTermSubList)
    //        {
    //            nodefound = false;
    //            foreach (GraphNode node in nodeList)
    //            {
    //                if (node is TermNode)
    //                {
    //                    TermNode termNode = node as TermNode;
    //                    if (termNode.Subject.Equals(sub))
    //                    {
    //                        nodefound = true;
    //                        termNode.SetDedFinTerms(franchise, dedInterType, dedValue, isPerRisk);
    //                        break;
    //                    }
    //                }
    //            }
    //            if (!nodefound) //Add the term node for Ded
    //            {
    //                TermNode newTermNode = new TermNode(sub);
    //                nodeList.Add(newTermNode);
    //                newTermNode.SetDedFinTerms(franchise, dedInterType, dedValue, isPerRisk);
    //            }
    //        }
    //    }

    //    private void AddNodeForLim(List<GraphNode> nodeList, Dictionary<string, object> termDictionary)
    //    {
    //        bool nodefound = false;
    //        double limValue;
    //        if (!TryConvert(termDictionary["Value"], out limValue))
    //            throw new InvalidOperationException("Limit is not coded as monetary amount in CDL, cannot be supported at this time...");
    //        Boolean GroundUpSublimits = true;  //TODO:

    //        bool isPerRisk = false;
    //        if (termDictionary["PerRisk"].ToString() == "True")
    //            isPerRisk = true;

    //        List<PrimarySubject> thisTermSubList = GetSubjectForTerm(termDictionary);

    //        foreach (PrimarySubject sub in thisTermSubList)
    //        {
    //            nodefound = false;
    //            foreach (GraphNode node in nodeList)
    //            {
    //                if (node is TermNode)
    //                {
    //                    TermNode termNode = node as TermNode;
    //                    if (termNode.Subject.Equals(sub))
    //                    {
    //                        nodefound = true;
    //                        termNode.SetLimFinTerms(GroundUpSublimits, limValue, isPerRisk);
    //                        break;
    //                    }
    //                }
    //            }
    //            if (!nodefound) //add term node for Lim
    //            {
    //                TermNode newTermNode = new TermNode(sub);
    //                nodeList.Add(newTermNode);
    //                newTermNode.SetLimFinTerms(GroundUpSublimits, limValue, isPerRisk);
    //            }
    //        }
    //    }

    //    private void AddNodeForCover(List<GraphNode> nodeList, Dictionary<string, object> coverDictionary)
    //    {
    //        bool nodefound = false;

    //        Type type = typeof(DedInteractionType);
    //        int index = Convert.ToInt32(coverDictionary["Index"]);
    //        bool franchise = Convert.ToBoolean(coverDictionary["IsFranchise"]);
    //        string name = coverDictionary["Label"].ToString();
    //        PrimarySubject thisCoverSub = GetSubjectForCover(coverDictionary);

    //        double attpoint;
    //        if (!GetValueFromJSONExp(coverDictionary["AttachmentSpecification"], out attpoint))
    //            throw new InvalidOperationException("attpoint with index " + index.ToString() + " is not coded as monetary amount in CDL, cannot be supported at this time...");

    //        double limit;
    //        if (!GetValueFromJSONExp(coverDictionary["LimitSpecification"], out limit))
    //            throw new InvalidOperationException("limit with index " + index.ToString() + " is not coded as monetary amount in CDL, cannot be supported at this time...");

    //        double proRata;
    //        if (!GetValueFromJSONExp(coverDictionary["Participation"], out proRata))
    //            throw new InvalidOperationException("proRata with index " + index.ToString() + " is not coded as monetary amount in CDL, cannot be supported at this time...");

    //        foreach (GraphNode node in nodeList)
    //        {
    //            if (node is CoverNode)
    //            {
    //                CoverNode coverNode = node as CoverNode;
    //                if (coverNode.Subject.Equals(thisCoverSub))
    //                {
    //                    nodefound = true;
    //                    coverNode.SetCoverTerms(franchise, attpoint, limit, proRata);
    //                    break;
    //                }
    //            }
    //        }
    //        if (!nodefound) //add the cover node
    //        {
    //            if (thisCoverSub.IsDerived)
    //            {
    //                CoverNode newCoverNode = new CoverNode(thisCoverSub);
    //                nodeList.Add(newCoverNode);
    //                newCoverNode.SetCoverTerms(franchise, attpoint, limit, proRata);
    //            }
    //            else
    //            {
    //                foreach (Schedule sch in ExpData.Schedules)
    //                {
    //                    if (sch.Name == thisCoverSub.Schedule.Name)
    //                    {
    //                        PrimarySubject updatedSub = new PrimarySubject(sch, thisCoverSub.ExposureTypes, thisCoverSub.CauseOfLossSet);
    //                        CoverNode newCoverNode = new CoverNode(updatedSub);
    //                        nodeList.Add(newCoverNode);
    //                        newCoverNode.SetCoverTerms(franchise, attpoint, limit, proRata);
    //                        break;
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    private List<PrimarySubject> GetSubjectForTerm(Dictionary<string, object> termDict)
    //    {
    //        //Schedule termSchedule = new Schedule(termDict["Schedule"].ToString());
    //        COLCollection termCOL = GetCOLHashSet(termDict["CausesOfLoss"].ToString());
    //        HashSet<ExposureType> termExp = GetExpTypeHashSet(termDict["ExposureTypes"].ToString());

    //        List<PrimarySubject> subjectList = new List<PrimarySubject>();

    //        if (termDict["PerRisk"].ToString() == "True")
    //        {
    //            //Per Risk, expand the term to all locations               
    //            foreach (Schedule sch in ExpData.Schedules)
    //            {
    //                if (sch.Name == termDict["Schedule"].ToString())
    //                {
    //                    //get all location sub-schedules for this sch
    //                    foreach (Schedule subSch in ExpData.Schedules)
    //                    {
    //                        if (subSch.IsSubScheduleOf(sch) && subSch.IsLocation)
    //                        {
    //                            PrimarySubject thisNewSub = new PrimarySubject(subSch, termExp, termCOL);
    //                            subjectList.Add(thisNewSub);
    //                        }
    //                    }
    //                    break;
    //                }
    //            }
    //        }
    //        return subjectList;
    //    }

    //    private PrimarySubject GetSubjectForCover(Dictionary<string, object> coverDict)
    //    {
    //        if (coverDict.ContainsKey("DerivedSubject"))
    //        {
    //            PrimarySubject thisNewSub = new PrimarySubject(null, null, null);
    //            thisNewSub.IsDerived = true;
    //            object component1;
    //            coverDict.TryGetValue("DerivedSubject", out component1);

    //            List<String> childrenList = new List<String>();
    //            Dictionary<string, object> tempDict = component1 as Dictionary<string, object>;
    //            object component2;
    //            tempDict.TryGetValue("Value", out component2);
    //            object[] tempList2 = component2 as object[];
    //            foreach (object obj2 in tempList2)
    //            {
    //                Dictionary<string, object> childrenCoverDict = obj2 as Dictionary<string, object>;
    //                childrenList.Add(childrenCoverDict["Value"].ToString());
    //            }
    //            thisNewSub.ChildrenCoverNodeList = childrenList;
    //            return thisNewSub;
    //        }
    //        else
    //        {
    //            Schedule coverSchedule = new Schedule(coverDict["Schedule"].ToString());
    //            //TODO: need read schedule information, update the schedule
    //            COLCollection coverCOL = GetCOLHashSet(coverDict["CausesOfLoss"].ToString());
    //            HashSet<ExposureType> coverExp = GetExpTypeHashSet(coverDict["ExposureTypes"].ToString());

    //            PrimarySubject thisNewSub = new PrimarySubject(coverSchedule, coverExp, coverCOL);
    //            return thisNewSub;
    //        }
    //    }
    //} //class GraphNodeBuilder
}
