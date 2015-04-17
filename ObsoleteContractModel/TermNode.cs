using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.ContractObjectModel.Sublimits;
using RMS.ContractObjectModel.Deductibles;
using RMS.Utilities;
using System.Collections;
using System.Xml;

namespace RMS.ContractObjectModel
{
    public class TermNode : Node, IComparable
    {
        public enum ENUM_TERM_NODE_TYPE
        {
            LOCATION_CVG,
            LOCATION,
            POLICY,
            UNKNOWN
        };

        #region Fields

        //protected ENUM_TERM_NODE_TYPE m_enumTermNodeType;

        protected SortedSet<TermObject> m_sortTermObj;
        //included to speed up processing
        protected _Contract m_contract;

        protected List<int> m_vSarId;
        protected bool m_bDummyNode;
        protected double m_prelimReSar;
        protected List<List<int>> bucketList;
        protected string tree_xml="";
        private static Dictionary<int, TermNode> MapIntIdToNode;
        private static Dictionary<int, RiskItem> MapIntIdToRit;
        private static int Level;
        private int m_Level;
        protected ENUM_TERM_NODE_TYPE m_termNodeType;

        public HashSet<string> _pParentIdx;

        #endregion

        #region Constructors

        public TermNode()
        {
            this.m_sortTermObj = new SortedSet<TermObject>();
            this.m_termNodeType = ENUM_TERM_NODE_TYPE.UNKNOWN;

            this.m_vSarId = new List<int>();

            this.m_bDummyNode = false;

            this._pParentIdx = new HashSet<string>();
        }

        //public TermNode()
        //{
        //    this.m_sortTermObj = new SortedSet<TermObject>();
        //    this.m_termNodeType = ENUM_TERM_NODE_TYPE.UNKNOWN;

        //    this.m_vSarId = new List<int>();

        //    this.m_bDummyNode = false;
        //}
        #endregion

        #region Methods
        public override bool IsSubsetTo(Node n)
        {
            if (n is TermNode)
            {
                TermNode tn = (TermNode)n;
                return base.Subject.IsSubsetTo(tn.Subject);
            }
            else
            {
                //should just return false?
                throw new Exception("nodes are not of same type for subset comparison");
            }
        }

        public override bool IsProperSubsetTo(Node n)
        {
            if (n is TermNode)
            {
                TermNode tn = (TermNode)n;
                return base.Subject.IsProperSubsetTo(tn.Subject);
            }
            else
            {
                //should just return false?
                throw new Exception("nodes are not of same type for proper subset comparison");
            }
        }
        public override bool Overlaps(Node n)
        {
            if (n is TermNode)
            {
                TermNode tn = (TermNode)n;
                return base.Subject.Overlaps(tn.Subject);
            }
            else
            {
                //should just return false?
                throw new Exception("nodes are not of same type for proper subset comparison");
            }
        }

        public void AddTermObject(TermObject termObj)
        {
            this.m_sortTermObj.Add(termObj);
        }

        public void AddParentIdx(string parentIdx)
        {
            _pParentIdx.Add(parentIdx);
        }

        public virtual void DetermineSubjectsAtRisk()
        {
            for (int cIdx = 0; cIdx < this.m_vChildNode.Count; ++cIdx)
            {
                TermNode child = (TermNode)this.m_vChildNode[cIdx];

                child.DetermineSubjectsAtRisk();

                List<int> vChildSarId = child.vSarId;
                for (int sIdx = 0; sIdx < vChildSarId.Count; ++sIdx)
                {
                    this.AddSarId(vChildSarId[sIdx]);
                }
            }

            _Schedule schedule = this.m_subject.Schedule;

            IList vLocCvg = schedule.SetSchedule.Where(riskItem => riskItem is LocationCvg).ToList();
            foreach (LocationCvg lcvg in vLocCvg)
            {
                this.AddSarId(lcvg.LocationExtnId);
            }
        }

        private void AddSarId(int sarId)
        {
            if(!this.m_vSarId.Contains(sarId))
            {
                this.m_vSarId.Add(sarId);
            }
        }
        public XmlDocument BuildXMLRepresentation(SimulationMsg cdlMsg, bool bDebug)
        {
            tree_xml = "<xml id="+"\""+IntId.ToString()+"\">\n\t" + BuildXMLRepresentationRecurse(cdlMsg, bDebug) + "\n</xml>";
            XmlDocument docTree = new XmlDocument();
            docTree.LoadXml(tree_xml);

            return docTree;
        }





        public string BuildXMLRepresentationRecurse(SimulationMsg cdlMsg, bool bDebug)
        {
            if (bDebug)
            {
                cdlMsg.SendMessage(string.Format("Calculate Loss TermNode: intId={0}, begin {1}", this.m_intId, this.CalcState));
            }

            //aggregate losses from children
            for (int cIdx = 0; cIdx < this.m_vChildNode.Count; ++cIdx)
            {
                TermNode child = (TermNode)this.m_vChildNode[cIdx];
                tree_xml += "\n<child" + " id = \"" + child.IntId.ToString() + "\">\n" + child.BuildXMLRepresentationRecurse(cdlMsg, bDebug) + "\n</child>\n";

                if (bDebug)
                {
                    cdlMsg.SendMessage(string.Format("For TermNode: intId={0} initial {1}, Adding Child TermNode: intId={2}, {3}",
                        this.m_intId, this.m_calcState, child.IntId, child.CalcState));
                }
            }

            _Schedule sar = this.m_subject.Schedule;

            if (sar.SetSchedule.Count > 0)
            {
                string leaf = "";
                foreach (RiskItem riskItem in sar.SetSchedule)
                {
                    if (riskItem is LocationCvg)
                    {
                        string newLeaf = riskItem.IntId.ToString().PadLeft(2, '0');
                        for (int n1 = 0; n1 < tree_xml.Length / 2; n1++)
                        {
                            if (newLeaf.Equals(tree_xml.Substring(n1 * 2, 2))) //no need to add twice
                                newLeaf = "";
                        }

                        leaf += newLeaf;

                    }
                }

                if (leaf.Length > 0)
                    tree_xml += leaf;
            }

            return tree_xml;
        }

        public void CalculateLossMain(SimulationMsg cdlMsg, bool bDebug, Dictionary<int, TermNode> map)
        {
            MapIntIdToNode = map;
            MapIntIdToRit = new Dictionary<int,RiskItem>() ;
            AddLocCvgToMap();

            CalculateLoss(cdlMsg, bDebug);//recurse
        }


        public void AddLocCvgToMap()
        {
            //aggregate losses from children
            for (int cIdx = 0; cIdx < this.m_vChildNode.Count; ++cIdx)
            {
                TermNode child = (TermNode)this.m_vChildNode[cIdx];
                child.AddLocCvgToMap();
            }

            _Schedule sar = this.m_subject.Schedule;

            if (sar.SetSchedule.Count > 0)
            {
                foreach (RiskItem riskItem in sar.SetSchedule)
                {
                    if (riskItem is LocationCvg)
                    {
                        if (!MapIntIdToRit.ContainsKey(riskItem.IntId))
                            MapIntIdToRit.Add(riskItem.IntId, riskItem);
                    }
                }

            }

            return ;
        }

        public bool IsOverlap(int a, int b)
        {
            string descA ;
            string descB ;

            if (MapIntIdToNode.ContainsKey(a))
                descA = MapIntIdToNode[a].ExtnId;
            else
                return false;//overlap for loccvg cannot be done at this time.

            if (MapIntIdToNode.ContainsKey(b))
                descB = MapIntIdToNode[b].ExtnId;
            else
                return false;//overlap for loccvg cannot be done at this time

            descA = descA.Replace("{","");
            descB = descB.Replace("{","");
            descA = descA.Replace("}","");
            descB = descB.Replace("}","");

            string[] stringIdentityA = descA.Split('-');
            string[] stringIdentityB = descB.Split('-');
            bool subsetMatch = false;

            if (stringIdentityA[1].Length != stringIdentityB[1].Length) //_Schedule
            {
                if (stringIdentityB[1].Contains(stringIdentityA[1]) || stringIdentityA[1].Contains(stringIdentityB[1]))
                {
                    subsetMatch = true;
                }
            }

            if (!subsetMatch)
                return false;

            string[] locCvgA = stringIdentityA[3].Split(',');
            string[] locCvgB = stringIdentityB[3].Split(',');

            bool bMatchSch = false;
            if (locCvgA.Length <= locCvgB.Length)
            {
                foreach (string locA in locCvgA)
                {
                    foreach (string locB in locCvgB)
                    {
                        if (locA.Equals(locB))
                        {
                            bMatchSch = true;
                            break;
                        }
                    }
                }

            }
            else
            {
                foreach (string locB in locCvgB)
                {
                    foreach (string locA in locCvgA)
                    {
                        if (locB.Equals(locA))
                        {
                            bMatchSch = true;
                            break;
                        }
                    }
                }
            }

            if (!bMatchSch)
                return false;

            //col = cause of loss 
            string[] colGrpA = stringIdentityA[2].Split(','); 
            string[] colGrpB = stringIdentityB[2].Split(',');

            bool bMatchCol = false;
            if (colGrpA.Length <= colGrpB.Length)
            {
                foreach (string colA in colGrpA)
                {
                    foreach (string colB in colGrpB)
                    {
                        if (colA.Equals(colB))
                        {
                            bMatchCol = true;
                            break;
                        }
                    }
                }

            }
            else
            {
                foreach (string colB in colGrpB)
                {
                    foreach (string colA in colGrpA)
                    {
                        if (colB.Equals(colB))
                        {
                            bMatchCol = true;
                            break;
                        }
                    }
                }
            }



            return bMatchCol;
        }

        public override void CalculateLoss(SimulationMsg cdlMsg, bool bDebug)
        {
            if (bDebug)
            {
                cdlMsg.SendMessage(string.Format("Calculate Loss TermNode: intId={0}, begin {1}", this.m_intId, this.CalcState)); 
            }

            //aggregate losses from children
            for (int cIdx = 0; cIdx < this.m_vChildNode.Count; ++cIdx)
            {
                TermNode child = (TermNode)this.m_vChildNode[cIdx];
                child.CalculateLoss(cdlMsg, bDebug);

                if (bDebug)
                {
                    cdlMsg.SendMessage(string.Format("For TermNode: intId={0} initial {1}, Adding Child TermNode: intId={2}, {3}",
                        this.m_intId, this.m_calcState, child.IntId, child.CalcState));
                }
            }
            
            
            if (BucketList != null)
            {
                List<int> overlapBuckets = new List<int>();
                for (int b0=0; b0<bucketList.Count; b0++)
                {
                    List<int> bucket=bucketList[b0];
                    for (int b1 = 0; b1 < bucket.Count-1; b1++)
                    {
                        for (int b2 = 1; b2 < bucket.Count; b2++)
                        {
                            if (IsOverlap(bucket[b1], bucket[b2]))
                            {
                                if (!overlapBuckets.Contains(b0))
                                    overlapBuckets.Add(b0);
                            }
                        }
                    }
                }

                foreach (int ob in overlapBuckets)
                    BucketList.Remove(bucketList[ob]) ;

                int b=0;
                CalcState[] calcStateArray = new CalcState[BucketList.Count];

                foreach (List<int> bucket in BucketList)
                {
                    calcStateArray[b] = new CalcState();
                    calcStateArray[b].X=0 ;
                    calcStateArray[b].S=0 ;
                    calcStateArray[b].D=0 ;

                    foreach (int intId in bucket)
                    {
                        if (MapIntIdToNode.ContainsKey(intId))
                        {
                            TermNode tn = MapIntIdToNode[intId];
                            _Schedule sar = tn.Subject.Schedule;
                            foreach (RiskItem riskItem in sar.SetSchedule)
                            {
                                calcStateArray[b].X += riskItem.CalcState.X;
                                calcStateArray[b].S += riskItem.CalcState.S;
                                calcStateArray[b].D += riskItem.CalcState.D;
                            }
                        }
                        else if (MapIntIdToRit.ContainsKey(intId))
                        {
                            RiskItem ri = MapIntIdToRit[intId];
                            calcStateArray[b].X += ri.CalcState.X;
                            calcStateArray[b].S += ri.CalcState.S;
                            calcStateArray[b].D += ri.CalcState.D;
                        }

                    }

                    foreach (TermObject termObj in this.m_sortTermObj)
                    {
                        termObj.ApplyTermObject(calcStateArray[b]);
                    }

                    b++;
                }

                this.m_calcState = FindWinningBucket(calcStateArray, BucketList);
            }
            else
            {

                _Schedule sar = this.m_subject.Schedule;
                foreach (RiskItem riskItem in sar.SetSchedule)
                {
                    this.m_calcState.X += riskItem.CalcState.X;
                    this.m_calcState.S += riskItem.CalcState.S;
                    this.m_calcState.D += riskItem.CalcState.D;
                }

                //apply term objects to the term node..
                foreach (TermObject termObj in this.m_sortTermObj)
                {
                    termObj.ApplyTermObject(this.m_calcState);
                }
            }

          
            if (bDebug)
            {
                cdlMsg.SendMessage(string.Format("Calculate Loss TermNode: intId={0}, final {1}", this.m_intId, this.CalcState));
            }
        }

        CalcState FindWinningBucket(CalcState[] calcStateArray, List<List<int>> bl)
        {
            double maxR = 0.0;
            int winB = -1;
            for (int n = 0; n < calcStateArray.Length-1; n++)
            {
                int n1 = n + 1;
                {
                    double Rn = Math.Max(0, calcStateArray[n].S - calcStateArray[n].D - calcStateArray[n].X);
                    double Rn1 = Math.Max(0, calcStateArray[n1].S - calcStateArray[n1].D - calcStateArray[n1].X);
                    if (Rn > maxR)
                    {
                        maxR = Rn;
                        winB = n;
                    }
                    else if (Rn1 > maxR)
                    {
                        maxR = Rn1;
                        winB = n1;
                    }
                }
            }

            //make sure only one bucket has the highest R
            int wbCnt = 0;
            for (int w = 0; w < calcStateArray.Length; w++)
            {
                double Rn = Math.Max(0, calcStateArray[w].S - calcStateArray[w].D - calcStateArray[w].X);
                if (Rn == maxR)
                    wbCnt++;
            }

            if (wbCnt == 1)
                return calcStateArray[winB];

            //smallest number of children
            int minC=0;

            for (int n = 0; n < calcStateArray.Length - 1; n++)
            {
                int n1 = n + 1;

                if (bl[n].Count < bl[n1].Count)
                    minC = bl[n].Count;
                else
                    minC = bl[n1].Count;
            }

                        //make sure only one bucket has small number of children

            int minCCnt= 0 ;

            for (int n = 0; n < calcStateArray.Length; n++)
            {
                if (bl[n].Count == minC)
                    minCCnt++;
            }

            if (minCCnt == 1)
                return calcStateArray[minC];

            return calcStateArray[0];

        }

        public override double GetLossState()
        {
            return Math.Max(this.m_calcState.S - this.m_calcState.D - this.m_calcState.X,0);
        }
        
        public string ToSarString()
        {
            return StringUtil.BuildStringFromList(this.m_vSarId);
        }

        public override string ToString()
        {
            StringBuilder childSet = new StringBuilder();

            childSet.Append("{ ");
            foreach (Node n in this.m_vChildNode)
            {
                childSet.Append(string.Format("{0},",n.IntId));
            }
            childSet.Remove(childSet.Length - 1, 1).Append(" }");

            StringBuilder parentSet = new StringBuilder();
            parentSet.Append("{ ");
            foreach (Node n in this.m_vParentNode)
            {
                parentSet.Append(string.Format("{0},", n.IntId));
            }
            parentSet.Remove(parentSet.Length - 1, 1).Append(" }");
            
            return string.Format("TermNode: intId={0}, extnId={1}, parentIntId={2}, childIntId={3}, subjectIntId={4}", this.m_intId, this.m_extnId, parentSet.ToString(),childSet.ToString(), this.m_subject.IntId);
        }
        #endregion

        #region Properties
        public ENUM_TERM_NODE_TYPE TermNodeType
        {
            get { return this.m_termNodeType; }
            set { this.m_termNodeType = value; }
        }
        public bool bDummyNode
        {
            get { return this.m_bDummyNode; }
            set { this.m_bDummyNode = value; }
        }
        public List<int> vSarId
        {
            get { return this.m_vSarId; }
            set { this.m_vSarId = value; }
        }
        public _Contract Contract
        {
            get { return this.m_contract; }
            set { this.m_contract = value; }
        }
        public SortedSet<TermObject> SortTermObject
        {
            get { return this.m_sortTermObj; }
            set { this.m_sortTermObj = value; }
        }
        public double PrelimReSar
        {
            get { return this.m_prelimReSar; }
            set { this.m_prelimReSar = value; }
        }
        public List<List<int>> BucketList
        {
            get { return this.bucketList; }
            set { this.bucketList = value; }
        }

       
        //public ENUM_TERM_NODE_TYPE EnumTermNodeType
        //{
        //    get { return this.m_enumTermNodeType; }
        //    set { this.m_enumTermNodeType = value; }
        //}
        #endregion

        public int CompareTo(object obj)
        {
            if (this == obj)
            {
                return 0;
            }

            if (obj is TermNode)
            {
                TermNode tnComp = (TermNode)obj;

                if (this.m_prelimReSar == tnComp.PrelimReSar)
                {
                    double thisLossState = this.GetLossState();
                    double compLossState = tnComp.GetLossState();

                    if (thisLossState == compLossState)
                    {
                        return this.m_intId > tnComp.IntId ? 1 : -1;
                    }
                    else
                    {
                        return thisLossState > compLossState ? 1 : -1;
                    }
                }
                else
                {
                    return this.m_prelimReSar > tnComp.PrelimReSar ? 1 : -1;
                }
            }
            else
            {
                //do we need to throw an exception here?
                return 0;
            }
        }
    }
}
