using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;
using System.Xml;

namespace RMS.ContractObjectModel.Contracts
{
    public class Primary : _Contract
    {
        #region Fields
        //term node tree
        protected Tree<TermNode> m_trTermNode;
        protected Dictionary<string, int> childStringToIntIdMap;

        //temporary until find a cleaner approach
        //protected Dictionary<string, List<LocationCvg>> m_mapLocExtnIdToLocCvgList;

        #endregion

        #region Methods

        public Primary(string name) : base(name) { }
        
        public void AddTermNode(TermNode termNode)
        {
            this.InitCheck();
            this.m_trTermNode.AddNodeToTree(termNode, true);

            //this.m_mapLocExtnIdToLocCvgList = new Dictionary<string, List<LocationCvg>>();
        }

        public TermNode GetTermNodeBySubject(__Subject subject)
        {
            InitCheck();

            string extnId = string.Format("{0}-{1}", this.m_extnId, subject.ToShortString());

            int intId = this.m_trTermNode.GetNodeIntIdByExtnId(extnId);
            TermNode tn;
            if (intId == -1)
            {
                tn = new TermNode();
                tn.Subject = subject;
                tn.ExtnId = extnId;

                tn.Contract = this;
            }
            else
            {
                tn = this.m_trTermNode.GetNodeByIntId(intId);
            }

            return tn;
        }

        #region ToString()
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append(string.Format("Contract: intId={0}, extnId={1}, deductibleOption={2}, currency={3}, subjectIntId={4}",
                this.m_intId, this.m_extnId, this.m_deductOpt, this.m_currency, this.m_subject.IntId));

            str.Append("Cover Nodes:");
            foreach (CoverNode cn in this.m_trCoverNode.MapIntIdToNode.Values)
            {
                str.Append(string.Format("\r\n\t{0}", cn));
            }
            str.Append("Term Nodes:");
            foreach (TermNode tn in this.m_trTermNode.MapIntIdToNode.Values)
            {
                str.Append(string.Format("\r\n\t{0}", tn));
            }

            return str.ToString();
        }
        #endregion

        #endregion

        #region Contract Implementation
        public override void Reset()
        {
            base.InitCheck();

            //reset term-node calculation states
            foreach (TermNode tn in this.m_trTermNode.MapIntIdToNode.Values)
            {
                tn.CalcState.Reset();
            }
            //reset cover-node calculation states
            foreach (CoverNode cn in this.m_trCoverNode.MapIntIdToNode.Values)
            {
                cn.CalcState.Reset();
                cn.Payout = 0.0;
            }
        }
        public override void Initialize()
        {
            if (this.m_subject == null)
            {
                throw new Exception("__Subject must be set before Contract.Initialize() is called");
            }

            this.m_trTermNode = new Tree<TermNode>();
            this.m_trCoverNode = new Tree<CoverNode>();

            //this.m_trTermNode.Initialize(string.Format("{0}-[TN-ROOT]", this.m_extnId));
            //this.m_trCoverNode.Initialize(string.Format("{0}-[CN-ROOT]", this.m_extnId));

            this.m_trTermNode.Initialize(string.Format("{0}-{1}", this.m_extnId, this.m_subject.ToShortString()));
            this.m_trTermNode.RootNode.Contract = this;

            this.m_trCoverNode.Initialize(string.Format("{0}-{1}", this.m_extnId, this.m_subject.ToShortString()));

            this.m_bInit = true;
        }

        public override IdentityMap<RiskItem> GetAllRiskItems()
        {
            IdentityMap<RiskItem> idMapRiskItem = base.GetAllRiskItems();

            //term nodes
            foreach (TermNode tn in this.TrTermNode.MapIntIdToNode.Values)
            {
                idMapRiskItem.AddObjectToMap(tn);

                if (tn.Subject != null)
                {
                    foreach (RiskItem riskItem in tn.Subject.Schedule.SetSchedule)
                    {
                        idMapRiskItem.AddObjectToMap(riskItem);
                    }
                }
            }

            return idMapRiskItem;
        }

        public override double CalculateLoss(RMS.Utilities.SimulationMsg cdlMsg, bool bDebug)
        {
            InitCheck();

            
            double payout = 0.0;
            XmlDocument docTree = this.m_trTermNode.RootNode.BuildXMLRepresentation(cdlMsg, bDebug);
            //DEBUG
            //XmlDocument docTree = new XmlDocument();
            //docTree.Load(@"cdl-tree-test.xml");
            childStringToIntIdMap = new Dictionary<string, int>();
            BuildBuckets(docTree, docTree.ChildNodes);
            docTree.Save(@"cdl-tree.xml");

            this.m_trTermNode.RootNode.CalculateLossMain(cdlMsg, bDebug, this.m_trTermNode.MapIntIdToNode);
            //this.m_trTermNode.RootNode.CalculateLoss(cdlMsg, bDebug);
            this.m_trCoverNode.RootNode.CalculateLoss(cdlMsg, bDebug);

            payout = this.m_trCoverNode.RootNode.Payout;

            return payout;
        }

        private void BuildBuckets(XmlNode parent, XmlNodeList nodes)
        {
            List<string> immediateChildren = new List<string>();
            foreach (XmlNode node in nodes)
            {
                if (node.NodeType != XmlNodeType.Text)
                {
                    // Do something with the node.
                    string c = node.InnerText.Replace("\n", "");
                      c = c.Replace("\r", "");
                      c = c.Replace("\t", "");
                      if (c.Length > 0)
                      {
                          immediateChildren.Add(c);
                          if (!childStringToIntIdMap.ContainsKey(c))
                              childStringToIntIdMap.Add(c, Int16.Parse(node.Attributes["id"].Value));
                      }
                }
            }

            bool overlap = false;

            if (immediateChildren.Count >= 2)
            {
                for (int i = 0; i < immediateChildren.Count - 1; i++)
                {
                    string child1 = immediateChildren[i];
                    string child2 = immediateChildren[i + 1];

                    for (int n1 = 0; n1 < child1.Length / 2; n1++)
                    {
                        string c1 = child1.Substring(n1 * 2, 2);

                        for (int n2 = 0; n2 < child2.Length / 2; n2++)
                        {
                            string c2 = child2.Substring(n2 * 2, 2);

                            if (c1.Equals(c2))
                            {
                                overlap = true;
                                break;
                            }
                        }
                    }
                }

                if (overlap)
                {
                    List<List<int>> bucketList = new List<List<int>>();

                    for (int i = 0; i < immediateChildren.Count; i++)
                    {
                        List<int> bucket = new List<int>();
                        string child1 = immediateChildren[i];
                        string leafsAdded = "";
                        leafsAdded += child1;
                        
                        bucket.Add(childStringToIntIdMap[child1]);

                        for (int i2 = 0; i2 < immediateChildren.Count; i2++)
                        {
                            if (i != i2)
                            {
                                string child2 = immediateChildren[i2];
                                //child2 = child2.Replace(child1, "");

                                child2 = removeLeafsAdded(leafsAdded, child2);

                                if (childStringToIntIdMap.ContainsKey(child2))
                                {
                                    int nodeNo = childStringToIntIdMap[child2];
                                    bucket.Add(nodeNo);
                                    leafsAdded += child2;
                                }
                                else
                                {
                                    for (int n3 = 0; n3 < child2.Length / 2; n3++)
                                    {
                                        string c3 = child2.Substring(n3 * 2, 2);

                                        bucket.Add(Int16.Parse(c3));
                                        leafsAdded += c3;
                                    }
                                }
                            }
                        }

                        bucketList.Add(bucket);

                    }

                    string id = parent.Attributes["id"].Value;
                    this.m_trTermNode.MapIntIdToNode[Int16.Parse(id)].BucketList = bucketList;

                   
                    //remove dups

                    for (int b1 = 0; b1 < bucketList.Count; b1++)
                    {
                        List<int> bucket1 = bucketList[b1];

                        for (int b2 = 1; b2 < bucketList.Count; b2++)
                        {
                            if (b1 != b2)
                            {
                                List<int> bucket2 = bucketList[b2];

                                bool bMatch = true;
                                foreach (int val in bucket1)
                                {
                                    if (!bucket2.Contains(val))
                                    {
                                        bMatch = false;
                                        break;
                                    }

                                }

                                if (bMatch)
                                {
                                    bucketList.Remove(bucketList[b1]);
                                }
                            }
                        }
                    }

                    for (int b1 = 0; b1 < bucketList.Count; b1++)
                    {
                        List<int> bucket1 = bucketList[b1];
                        bool bMatch = false ;

                        for (int val=0; val<bucket1.Count; val++)
                        {
                            for (int val2 = 0; val2 < bucket1.Count; val2++)
                            {
                                if (val != val2)
                                {
                                    if (this.m_trTermNode.MapIntIdToNode.ContainsKey(bucket1[val]) &&
                                        this.m_trTermNode.MapIntIdToNode.ContainsKey(bucket1[val2]))
                                    {
                                        Node node1 = this.m_trTermNode.MapIntIdToNode[bucket1[val]];
                                        Node node2 = this.m_trTermNode.MapIntIdToNode[bucket1[val2]];
                                        if (node1.Overlaps(node2))
                                            bMatch = true ;
                                        if (node2.Overlaps(node1))
                                            bMatch = true ;
                                    }
                                }
                            }
                        }

                        if (bMatch)
                            bucketList.Remove(bucket1) ;
                    }

                    foreach (List<int> bucket in bucketList)
                    {
                        string b = "{";
                        foreach (int c in bucket)
                        {
                            b += c.ToString();
                            b += ",";
                        }

                        if (b.Contains(","))
                            b = b.Substring(0, b.LastIndexOf(","));

                        b += "}";

                        XmlElement el = parent.OwnerDocument.CreateElement("Bucket");
                        el.InnerText = b;
                        parent.AppendChild(el);
                    }

                }
            }


            foreach (XmlNode node in nodes)
            {
                BuildBuckets(node, node.ChildNodes);
            }
        }

        private string removeLeafsAdded(string leafsAdded, string child)
        {
            for (int n = 0; n < leafsAdded.Length / 2; n++)
            {
                string c = leafsAdded.Substring(n * 2, 2);
                child = child.Replace(c, "");
            }

            return child;
        }

        #endregion

        #region Properties
        public Tree<TermNode> TrTermNode
        {
            get { return this.m_trTermNode; }
            set { this.m_trTermNode = value; }
        }
        #endregion
    }
}
