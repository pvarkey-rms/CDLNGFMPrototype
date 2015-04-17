using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using RMS.ContractObjectModel;
using RMS.ContractObjectModel.Contracts;

using Contract = RMS.ContractObjectModel._Contract;
using Subject = RMS.ContractObjectModel.__Subject;
using Schedule = RMS.ContractObjectModel._Schedule;


namespace RMS.Prototype.NGFM
{
    public class _ContractGraphBuilder
    {
        public static void _BuildGraphFromIR(string ir)
        {
            HashSet<Contract> contracts = new HashSet<Contract>();

            string separator = ",";

            _Contract currentContract = null;

            int nCovers = 0;
            int nodeExtnId = 0;
            CoverNode currentCoverNode = null;
            bool processingCovers = false;
            HashSet<CoverNode> coverNodes = null;
            int pValueType = -1;
            bool _hasPercentCover = false;
            bool _hasPercentAffected = false;

            int nNodes = 0;
            bool processingNodes = false;
            HashSet<TermNode> nodes = null;
            TermNode currentTermNode = null;

            //string[] lines = Regex.Split(ir, System.Environment.NewLine).ToList().Where(s => !string.IsNullOrEmpty(s)).ToArray<string>();
            //int c = 0;
            //while (true)
            //{
            //    if (c >= lines.Length)
            //        break;

            //    string line = lines[c];

            //    string[] values = Regex.Split(line, separator);

            //    if (values == null || values.Length == 0)
            //    {
            //        c++;
            //        continue;
            //    }

            //    //Trim values
            //    for (int j = 0; j < values.Length; j++)
            //    {
            //        values[j] = values[j].Trim();
            //    }
            //}

            foreach (string line in Regex.Split(ir, System.Environment.NewLine).ToList().Where(s => !string.IsNullOrEmpty(s)))
            {
                string[] values = Regex.Split(line, separator);

                if (values == null || values.Length == 0)
                    continue;

                //Trim values
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = values[i].Trim();
                }

                if (values[0].Equals(";"))
                {
                    if (currentContract != null)
                    {
                        if (currentCoverNode != null)
                        {
                            coverNodes.Add(currentCoverNode);
                            currentCoverNode = null;
                        }

                        // add covers to contract
                        foreach (CoverNode coverNode in coverNodes)
                            currentContract.AddCoverNode(coverNode);

                        contracts.Add(currentContract);
                    }
                    currentContract = null;
                    continue;
                }

                if (values[0].Equals("CONTRACT"))
                {
                    if (currentContract != null)
                    {
                        if (currentCoverNode != null)
                        {
                            coverNodes.Add(currentCoverNode);
                            currentCoverNode = null;
                        }

                        // add covers to contract
                        foreach (CoverNode coverNode in coverNodes)
                            currentContract.AddCoverNode(coverNode);

                        contracts.Add(currentContract);
                    }
                    currentContract = new Primary(values[1]);
                    currentContract.Subject = new Subject(new Schedule(), new HashSet<ICauseOfLoss>(), new HashSet<string>(), false);
                    currentContract.Initialize();
                    continue;
                }

                if (values[0].Equals("nCover"))
                {
                    nCovers = int.Parse(values[1]);
                    processingCovers = true;
                    continue;
                }

                if (processingCovers)
                {
                    if (coverNodes == null)
                        coverNodes = new HashSet<CoverNode>();

                    if (values[0].Equals("PARENT"))
                    {
                        if (currentCoverNode != null)
                        {
                            coverNodes.Add(currentCoverNode);
                        }
                        if (pValueType == 1)
                            _hasPercentCover = true;
                        else if (pValueType == 2)
                            _hasPercentAffected = true;
                        currentCoverNode = new CoverNode();
                        currentCoverNode.ExtnId = (nodeExtnId++).ToString();
                        currentCoverNode.parentId = values[1];
                        continue;
                    }

                    if (values[0].Equals("nChild"))
                    {
                        currentCoverNode.nChild = int.Parse(values[1]);
                        continue;
                    }

                    if (values[0].Equals("CHILD"))
                    {
                        currentCoverNode.vSubCover.Add(values[1]);
                        continue;
                    }

                    if (values[0].Equals("SHARE"))
                    {
                        currentCoverNode.Share = double.Parse(values[1]); // ? *= 0.01
                        continue;
                    }

                    if (values[0].Equals("OCCLIM"))
                    {
                        currentCoverNode.Limit = double.Parse(values[1]);
                        continue;
                    }

                    if (values[0].Equals("AGGLIM"))
                    {
                        currentCoverNode.AggLimit = double.Parse(values[1]);
                        continue;
                    }

                    if (values[0].Equals("OCCLIMPC"))
                    {
                        currentCoverNode.m_limitPC = double.Parse(values[1]);
                        pValueType = 1;
                        continue;
                    }

                    if (values[0].Equals("AGGLIMPC"))
                    {
                        currentCoverNode.m_agglimitPC = double.Parse(values[1]);
                        pValueType = 1;
                        continue;
                    }

                    if (values[0].Equals("OCCLIMPA"))
                    {
                        currentCoverNode.m_limitPA = double.Parse(values[1]);
                        pValueType = 2;
                        continue;
                    }

                    if (values[0].Equals("AGGLIMPA"))
                    {
                        currentCoverNode.m_agglimitPA = double.Parse(values[1]);
                        pValueType = 2;
                        continue;
                    }

                    if (values[0].Equals("OCCATT"))
                    {
                        currentCoverNode.Attach = double.Parse(values[1]);
                        continue;
                    }

                    if (values[0].Equals("AGGATT"))
                    {
                        currentCoverNode.m_aggach = double.Parse(values[1]);
                        continue;
                    }

                    if (values[0].Equals("OCCATTPC"))
                    {
                        currentCoverNode.m_attachPC = double.Parse(values[1]);
                        pValueType = 1;
                        continue;
                    }

                    if (values[0].Equals("AGGATTPC"))
                    {
                        currentCoverNode.m_aggachPC = double.Parse(values[1]);
                        pValueType = 1;
                        continue;
                    }

                    if (values[0].Equals("OCCATTPA"))
                    {
                        currentCoverNode.m_attachPA = double.Parse(values[1]);
                        pValueType = 2;
                        continue;
                    }

                    if (values[0].Equals("AGGATTPA"))
                    {
                        currentCoverNode.m_aggachPA = double.Parse(values[1]);
                        pValueType = 2;
                        continue;
                    }

                    if (values[0].Equals("SETS"))
                    {
                        if (values[1].Equals("MAX"))
                        {
                            currentCoverNode._setsType = (int)ContractEntry.COVER_SETS_MAX;
                        }
                        else if (values[1].Equals("MIN"))
                        {
                            currentCoverNode._setsType = (int)ContractEntry.COVER_SETS_MIN;
                        }
                        else if (values[1].Equals("SUM"))
                        {
                            currentCoverNode._setsType = (int)ContractEntry.COVER_SETS_SUM;
                        }
                        continue;
                    }

                    if (values[0].Equals("PAY"))
                    {
                        currentCoverNode._payfType = (int)ContractEntry.COVER_PAY;
                        currentCoverNode._pay = double.Parse(values[1]);
                        continue;
                    }

                    if (values[0].Equals("PAYF"))
                    {
                        // TODO : _pay?
                        if (values[1].Equals("MAX"))
                        {
                            currentCoverNode._payfType = (int)ContractEntry.COVER_PAYF_MAX;
                        }
                        else if (values[1].Equals("MIN"))
                        {
                            currentCoverNode._payfType = (int)ContractEntry.COVER_PAYF_MIN;
                        }
                        continue;
                    }

                    if (values[0].Equals("PAYP"))
                    {
                        currentCoverNode._payfType = (int)ContractEntry.COVER_PAYP;
                        currentCoverNode._pay = double.Parse(values[1]) * 0.01;
                        continue;
                    }

                    if (values[0].Equals("PAYFP"))
                    {
                        // TODO : _pay?
                        if (values[1].Equals("MAX"))
                        {
                            currentCoverNode._payfType = (int)ContractEntry.COVER_PAYFP_MAX;
                        }
                        else if (values[1].Equals("MIN"))
                        {
                            currentCoverNode._payfType = (int)ContractEntry.COVER_PAYFP_MIN;
                        }
                        continue;
                    }

                    if (values[0].Equals("nNode"))
                    {
                        coverNodes.Add(currentCoverNode);
                        currentCoverNode = null;
                        processingCovers = false;
                        processingNodes = true;
                        nNodes = int.Parse(values[1]);
                        currentContract.nNodes = nNodes;
                        if (pValueType == 1)
                            _hasPercentCover = true;
                        else if (pValueType == 2)
                            _hasPercentAffected = true;
                        continue;
                    }
                }

                if (processingNodes)
                {
                    if (nodes == null)
                        nodes = new HashSet<TermNode>();

                    if (values[0].Equals("PARENT"))
                    {
                        if (currentTermNode != null)
                        {
                            nodes.Add(currentTermNode);
                        }
                        currentTermNode = new TermNode();
                        currentTermNode.ExtnId = (nodeExtnId++).ToString();
                        for (int k = 1; k < values.Length; k++)
                            currentTermNode.AddParentIdx(values[k]);
                        continue;
                    }


                }
            }

            if (currentContract != null)
            {
                if (currentCoverNode != null)
                {
                    coverNodes.Add(currentCoverNode);
                    currentCoverNode = null;
                }

                // add covers to contract
                foreach (CoverNode coverNode in coverNodes)
                    currentContract.AddCoverNode(coverNode);

                contracts.Add(currentContract);
            }

        }

        public static void BuildGraphFromJISONJSON(Dictionary<string, object> JISONJsonParseResult)
        {
            Dictionary<string, object> Declarations = (Dictionary<string, object>)JISONJsonParseResult["Declarations"];

            if (!Declarations.ContainsKey("Type") || Declarations["Type"].ToString().ToLower().Equals("insurance"))
            {
                _Contract contract = new Primary(Declarations["Name"].ToString());

                // Rebuild contract subject

                _Schedule schedule = new Schedule(Declarations["_Schedule"].ToString().Trim());

                HashSet<ICauseOfLoss> ContractSubjectCausesOfLoss = new HashSet<ICauseOfLoss>();
                string[] causes_of_loss = Declarations["CauseOfLoss"].ToString().Trim().Split(',');
                foreach (string cause_of_loss in causes_of_loss)
                    ContractSubjectCausesOfLoss.Add(new Peril(cause_of_loss.Trim()));

                string[] _coverages = Declarations["CoverageType"].ToString().Trim().Split(',');
                HashSet<string> coverages = new HashSet<string>();
                for (int i = 0; i < _coverages.Length; i++)
                    coverages.Add(_coverages[i].Trim());

                contract.Subject = new Subject(schedule, ContractSubjectCausesOfLoss, coverages, false);
            }
        }
    }
}
