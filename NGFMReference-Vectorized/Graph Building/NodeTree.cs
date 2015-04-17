using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public class NodeTree
    {
        public bool IsOverlapped { get; private set; }
        public long ContractID { get; private set; }

        //TermNode related
        public List<TermNode> OriginalTermNodes { get; private set; }
        public HashSet<TermNode> OriginalTermNodesMinusPerRisk { get; private set; }
        public List<TermNode> OriginalPerRiskTermNodes { get; private set; }
        public NodeCompareOutcome[,] OriginalTermNodeTreeMatrix { get; private set; }

        //CoverNode related
        public List<CoverNode> OriginalCoverNodes { get; private set; }
        public List<CoverNode> OriginalDerivedCoverNodes { get; private set; }
        public List<CoverNode> OriginalLeafCoverNodes { get; private set; }
        //public Dictionary<CoverNode, HashSet<GraphNode>> OriginalCoverNodeChildrenMap { get; set; }
        //public Dictionary<CoverNode, HashSet<GraphNode>> OriginalCoverNodeOverlapMap { get; set; }
        public Dictionary<CoverNode, HashSet<CoverNode>> OriginalDerivedCoverNodeChildrenMap { get; set; }  
        //Leaf CoverNodes' children are alll AtomicRites
        public Dictionary<CoverNode, HashSet<CoverNode>> OriginalCoverNodeParentsMap { get; set; }
        public Dictionary<CoverNode, HashSet<CoverNode>> CoverNodeExplosionDict { get; set; }  //PerRisk explosion infor

        //Some private variables, only used for convenience in this class to explode PerRisk nodes 
        private HashSet<TermNode> DistinctPerRiskTermNodes { get; set; }
        private Dictionary<RITE, TermNode>[] PerRiskNodeDict { get; set; }
        //PerRisk NodeID, Rite, exploded Node
        public NodeTuple[] FinalNodeTupleArray { get; set; }
        //all nodeID,  <exploded?, parent node ID, rite>

        public HashSet<TermNode> FinalTermNodes { get; set; }
        public HashSet<CoverNode> FinalLeafCoverNodes { get; set; }
        public HashSet<CoverNode> FinalDerivedCoverNodes { get; set; }

        //NodeID, <RITE, Ded/Limit>, when combine Term nodes for PerRisk, we need merge their terms
        public Dictionary<RITE, DeductibleCollection>[] RiteDedsAddition { get; set; }
        public Dictionary<RITE, LimitCollection>[] RiteLimitsAddition { get; set; }

        //major output for TermGraph
        public Dictionary<TermNode, HashSet<TermNode>> TermParentChildrenMap { get; set; }
        public Dictionary<TermNode, HashSet<TermNode>> TermChildParentsMap { get; set; } 

        //major output for CoverGraph        
        public Dictionary<CoverNode, HashSet<CoverNode>> CoverNodeChildParentsMap { get; set; }
        public Dictionary<CoverNode, HashSet<CoverNode>> CoverNodeParentChildrenMap { get; set; }
           
    
        //Constructor
        public NodeTree(List<TermNode> _inputTermList, List<CoverNode> _inputCoverList)
        {
            OriginalTermNodes = new List<TermNode>(_inputTermList);
            OriginalCoverNodes = new List<CoverNode>(_inputCoverList);

            OriginalLeafCoverNodes = new List<CoverNode>();
            OriginalDerivedCoverNodes = new List<CoverNode>();

            foreach (CoverNode cNode in OriginalCoverNodes)
            {
                if (cNode.Subject.IsDerived)
                {
                    OriginalDerivedCoverNodes.Add(cNode);
                }
                else
                {
                    OriginalLeafCoverNodes.Add(cNode);
                }            
            }  //only need to connect Leaf Cover Nodes with Term Nodes.

            OriginalPerRiskTermNodes = new List<TermNode>();
            foreach (TermNode node in OriginalTermNodes)
            {
                if (node.IsPerRisk)
                    OriginalPerRiskTermNodes.Add(node);
            }

            int maxSubjectID = 0;
            foreach (TermNode node in OriginalTermNodes)
            {
                maxSubjectID = Math.Max(maxSubjectID, node.Subject.ID);
            }

            //init variables
            OriginalTermNodeTreeMatrix = new NodeCompareOutcome[maxSubjectID + 1, maxSubjectID + 1];
            TermParentChildrenMap = new Dictionary<TermNode, HashSet<TermNode>>();
            TermChildParentsMap = new Dictionary<TermNode, HashSet<TermNode>>();                        
            OriginalCoverNodeParentsMap = new Dictionary<CoverNode, HashSet<CoverNode>>();            
            OriginalDerivedCoverNodeChildrenMap = new Dictionary<CoverNode, HashSet<CoverNode>>();
            DistinctPerRiskTermNodes = new HashSet<TermNode>(OriginalPerRiskTermNodes);
            PerRiskNodeDict = new Dictionary<RITE, TermNode>[maxSubjectID + 1];

            OriginalTermNodesMinusPerRisk = new HashSet<TermNode>(OriginalTermNodes);
            OriginalTermNodesMinusPerRisk.ExceptWith(OriginalPerRiskTermNodes);
            FinalNodeTupleArray = new NodeTuple[10000000];
            FinalTermNodes = new HashSet<TermNode>();
            FinalLeafCoverNodes = new HashSet<CoverNode>();
            FinalDerivedCoverNodes = new HashSet<CoverNode>();          
            
            CoverNodeExplosionDict = new Dictionary<CoverNode, HashSet<CoverNode>>();
            CoverNodeChildParentsMap = new Dictionary<CoverNode, HashSet<CoverNode>>();
            CoverNodeParentChildrenMap = new Dictionary<CoverNode, HashSet<CoverNode>>();

            foreach (TermNode node in OriginalTermNodesMinusPerRisk)
            {
                NodeTuple nodeTuple = new NodeTuple(false, node.Subject.ID, null);
                FinalNodeTupleArray[node.Subject.ID] = nodeTuple;
                FinalTermNodes.Add(node);
            }

            RiteDedsAddition = new Dictionary<RITE, DeductibleCollection>[maxSubjectID + 1];
            RiteLimitsAddition = new Dictionary<RITE, LimitCollection>[maxSubjectID + 1];
        }

        //main method
        public void Run()
        {
            //Console.WriteLine("Node tree start at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));
            BuildInitialTree();
            ExplodePerRiskTermNodes();
            UpdateTermParentChildrenMappingForPerRisk();
            UpdateTermChildParentsMappingForPerRisk();
            UpdateCoverGraphForPerRisk();
            //Console.WriteLine("Node tree end at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));
            //Compare();  //for testing
        }

        //----------------------------------------------
        public void Compare() //for testing purpose
        {
            Console.WriteLine(" compare start at: " + DateTime.Now.ToString("h:mm:ss tt"));
            bool result = false;
            int counter = 0;

            //foreach (GraphNode node1 in FinalNodes)
            //for (int i = 1; i <= FinalNodes.Count(); i++)
            foreach (TermNode node1 in FinalTermNodes)
            {
                //TermNode node1 = FinalNodes[i];
                //if (counter > 300000 * 2000)
                //    break;
                //foreach (TermNode node2 in FinalNodes)
                foreach (TermNode node2 in FinalTermNodes)
                // for (int j = i+1; j < FinalNodes.Count(); j++)
                {
                    // TermNode node2 = FinalNodes[j];
                    counter++;
                    //if (node1.Subject.ID == node2.Subject.ID)
                    //     result = false;
                    if (node1 is TermNode & node2 is TermNode)
                        result = IsLargerThan(node1, node2);
                    else
                        result = false;
                    //else if (node1 is CoverNode & node2 is CoverNode)
                    //{
                    //    result = false;
                    //}
                    //else if (node1 is CoverNode & node2 is TermNode)
                    //{
                    //    //return this.subject.IsLargerThan(smallNode.Subject as PrimarySubject)
                    //    //      || this.subject.Equals(smallNode.Subject as PrimarySubject);
                    //    result = true;
                    //}
                    //else if (node1 is TermNode & node2 is CoverNode)
                    //    result = false;                    

                }
            }

            Console.WriteLine(" Compare end at: " + DateTime.Now.ToString("h:mm:ss tt"));
        }
        //----------------------------------------------

        public void BuildInitialTree()
        {
            //assume in originalTermNodes, there are no duplicate nodes
            GrowTermNodeTree(OriginalTermNodes, OriginalTermNodes);


            //Leaf Cover Nodes have been changed to link to AtomicRites
            //Below is only used for LeafCover's when linking to term nodes
            //foreach (CoverNode cNode in OriginalLeafCoverNodes)
            //{
            //    PrimarySubject cSubject = cNode.Subject as PrimarySubject;

            //    foreach (GraphNode node in OriginalNodes)
            //    {
            //        TermNode tNode = node as TermNode;
            //        PrimarySubject tSubject = tNode.Subject as PrimarySubject;
            //        NodeCompareOutcome result = CompareTwoSubjects(cSubject, tSubject);
            //        if (result == NodeCompareOutcome.Parent)
            //        {
            //            if (OriginalCoverNodeChildrenMap.ContainsKey(cNode))
            //            {
            //                OriginalCoverNodeChildrenMap[cNode].Add(tNode);
            //            }
            //            else
            //            {
            //                OriginalCoverNodeChildrenMap.Add(cNode, new HashSet<GraphNode>() { tNode });
            //            }
            //        }
            //        else if (result == NodeCompareOutcome.Overlap_Parent)
            //        {
            //            if (OriginalCoverNodeOverlapMap.ContainsKey(cNode))
            //            {
            //                OriginalCoverNodeOverlapMap[cNode].Add(tNode);
            //            }
            //            else
            //            {
            //                OriginalCoverNodeOverlapMap.Add(cNode, new HashSet<GraphNode>() { tNode });
            //            }
            //        }
            //    }
            //}

            //Derived CoverGraph
            foreach (CoverNode cNode in OriginalDerivedCoverNodes)
            {
                OriginalDerivedCoverNodeChildrenMap.Add(cNode, new HashSet<CoverNode>());
                foreach (CoverNode cNode2 in OriginalCoverNodes)
                {
                    if (cNode.Subject.ID != cNode2.Subject.ID)
                    {
                        if (cNode.Subject.ChildrenCoverNodeList.Contains(cNode2.Cover.CoverName))
                            OriginalDerivedCoverNodeChildrenMap[cNode].Add(cNode2);                    
                    }
                }
            }
            //
            foreach (CoverNode cNode in OriginalCoverNodes)
            {
                OriginalCoverNodeParentsMap.Add(cNode, new HashSet<CoverNode>());
                foreach (CoverNode cNode2 in OriginalCoverNodes)
                {
                    if (cNode.Subject.ID != cNode2.Subject.ID)
                    {
                        if (cNode2.Subject.ChildrenCoverNodeList != null  && cNode2.Subject.ChildrenCoverNodeList.Contains(cNode.Cover.CoverName))
                            OriginalCoverNodeParentsMap[cNode].Add(cNode2);
                    }                
                }
            }
        }

        public void GrowTermNodeTree(List<TermNode> nodelist1, List<TermNode> nodelist2)
        {  
            //TODO: we could reduce the loop by half. then there will be more "if" statement, and we need two sets of 
            //cSet and pSet. looks messy, but probably more efficient.
            foreach (TermNode node1 in nodelist1)
            {
                PrimarySubject subject1 = node1.Subject as PrimarySubject;

                HashSet<TermNode> cSet = new HashSet<TermNode>();
                HashSet<TermNode> pSet = new HashSet<TermNode>();

                foreach (TermNode node2 in nodelist2)
                {
                    if (node1.Subject.ID == node2.Subject.ID)
                        continue;

                    PrimarySubject subject2 = node2.Subject as PrimarySubject;
                    NodeCompareOutcome result = CompareTwoSubjects(subject2, subject1);
                    OriginalTermNodeTreeMatrix[node2.Subject.ID, node1.Subject.ID] = result;

                    if (!node1.IsPerRisk & !node2.IsPerRisk)
                    {
                        if (result == NodeCompareOutcome.Parent)                        
                            cSet.Add(node2);                        
                        else if (result == NodeCompareOutcome.Child)                        
                            pSet.Add(node2);                        
                    }
                }
                
                if (cSet.Count() > 0)
                    TermParentChildrenMap.Add(node1, cSet);

                //will keep no-parent child as well
                if (!node1.IsPerRisk)    
                    TermChildParentsMap.Add(node1, pSet);
            }
        }

        public void SetDistinctPerRiskTermNodes()
        {
            //remove children Per Risk Subjects
            for (int i = 0; i < OriginalPerRiskTermNodes.Count(); i++)
            {
                TermNode tNode1 = OriginalPerRiskTermNodes[i];
                PrimarySubject s1 = tNode1.Subject as PrimarySubject;

                for (int j = i + 1; j < OriginalPerRiskTermNodes.Count(); j++)
                {
                    TermNode tNode2 = OriginalPerRiskTermNodes[j];
                    PrimarySubject s2 = tNode2.Subject as PrimarySubject;
                  
                    if (OriginalTermNodeTreeMatrix[s1.ID, s2.ID] == NodeCompareOutcome.Equal)
                    { 
                        //merge terms together                                                
                        tNode2.Deductibles.AddOtherCollection(tNode1.Deductibles);
                        tNode2.Limits.AddOtherCollection(tNode1.Limits);
                        DistinctPerRiskTermNodes.Remove(tNode1);
                    }
                    else if (CompareTwoCOLs(s1.CauseOfLossSet.Collection, s2.CauseOfLossSet.Collection) == NodeCompareOutcome.Equal &&
                              CompareTwoExpTypes(s1.ExposureTypes.MappedTypes, s2.ExposureTypes.MappedTypes) == NodeCompareOutcome.Equal)
                    {
                        if (OriginalTermNodeTreeMatrix[s1.ID, s2.ID] == NodeCompareOutcome.Child)
                        {
                            foreach (RITE rite in s2.Schedule.ScheduleList)
                            {
                                RiteDedsAddition[s1.ID].Add(rite, tNode2.Deductibles);
                                RiteLimitsAddition[s1.ID].Add(rite, tNode2.Limits);
                            }
                            DistinctPerRiskTermNodes.Remove(tNode2);
                        }
                        else if (OriginalTermNodeTreeMatrix[s1.ID, s2.ID] == NodeCompareOutcome.Parent)
                        {
                            foreach (RITE rite in s1.Schedule.ScheduleList)
                            {
                                RiteDedsAddition[s2.ID].Add(rite, tNode1.Deductibles);
                                RiteLimitsAddition[s2.ID].Add(rite, tNode1.Limits);
                            }
                            DistinctPerRiskTermNodes.Remove(tNode1);
                        }

                        else if (OriginalTermNodeTreeMatrix[s1.ID, s2.ID] == NodeCompareOutcome.Overlap_Child || OriginalTermNodeTreeMatrix[s1.ID, s2.ID] == NodeCompareOutcome.Overlap_Parent)                        
                        {
                            HashSet<RITE> overlapRites = new HashSet<RITE>();
                            overlapRites.UnionWith(s1.Schedule.ScheduleList);
                            overlapRites.IntersectWith(s2.Schedule.ScheduleList);
                            foreach (RITE rite in overlapRites)
                            {
                                RiteDedsAddition[s1.ID].Add(rite, tNode2.Deductibles);
                                RiteLimitsAddition[s1.ID].Add(rite, tNode2.Limits);
                            }
                            //just take way the overlap part from one of the nodes
                            s2.Schedule.ScheduleList.ExceptWith(overlapRites);
                            //this is ok, because the per-risk term in s2 will overlap with s1's parent and child
                            //silently change the Node, but already merged the terms to other node
                        }
                    }
                }  //compare to perRiskNodes

                //in original non-PerRisk nodes, possibly there are duplicates with exploded PerRisk nodes
                List<TermNode> OriginalSingletonNotPerRiskNodes = new List<TermNode>();

                foreach (TermNode node in OriginalTermNodesMinusPerRisk)
                {
                    PrimarySubject subject = node.Subject as PrimarySubject;
                    if (subject.Schedule.ScheduleList.Count() == 1)
                        OriginalSingletonNotPerRiskNodes.Add(node);
                }

                foreach (TermNode tNode3 in OriginalSingletonNotPerRiskNodes)
                {
                    //TermNode tNode3 = node3 as TermNode;
                    PrimarySubject s3 = tNode3.Subject as PrimarySubject;
                    if (CompareTwoCOLs(s1.CauseOfLossSet.Collection, s3.CauseOfLossSet.Collection) == NodeCompareOutcome.Equal &&
                              CompareTwoExpTypes(s1.ExposureTypes.MappedTypes, s3.ExposureTypes.MappedTypes) == NodeCompareOutcome.Equal)
                    {
                        if (s1.Schedule.ScheduleList.Contains(s3.Schedule.ScheduleList.First()))
                        //if (s1.Schedule.ScheduleList.Overlaps(s3.Schedule.ScheduleList))
                        {
                            OriginalTermNodesMinusPerRisk.Remove(tNode3);
                            FinalTermNodes.Remove(tNode3);
                            RiteDedsAddition[s1.ID].Add(s3.Schedule.ScheduleList.First(), tNode3.Deductibles);
                            RiteLimitsAddition[s1.ID].Add(s3.Schedule.ScheduleList.First(), tNode3.Limits);
                        }
                    }
                } //compare to singleton NotPerRiskNodes
            }
        }


        public void ExplodePerRiskTermNodes()
        {
            SetDistinctPerRiskTermNodes();

            //Now we have unique list of Per Risk Subject, safely explode them, will not have duplicates among those exploded terms
            foreach (TermNode tNode in DistinctPerRiskTermNodes)
            {
                //TermNode tNode = node as TermNode;
                PrimarySubject s = tNode.Subject as PrimarySubject;
                Dictionary<RITE, TermNode> temp = new Dictionary<RITE, TermNode>();  //riteID, newSubjectID

                //explode them
                foreach (RITE rite in s.Schedule.ScheduleList)
                {
                    HashSet<RITE> aRite = new HashSet<RITE>{rite};
                    
                    ScheduleOfRITEs newSchedule = new ScheduleOfRITEs("Exploded " + rite.ToString(), aRite, rite.RiskCharacteristics);
                    PrimarySubject newSubject = FormNewSubject(s, newSchedule);
                    TermNode newTermNode = FormNewTermNode(newSubject, tNode);

                    //update newTermNode with Rite Ded/Limit addition
                    if (RiteDedsAddition[s.ID] != null && RiteDedsAddition[s.ID].ContainsKey(rite))
                    {
                        newTermNode.Deductibles.AddOtherCollection(RiteDedsAddition[s.ID][rite]);
                    }
                    if (RiteLimitsAddition[s.ID] != null && RiteLimitsAddition[s.ID].ContainsKey(rite))
                    {
                        newTermNode.Limits.AddOtherCollection(RiteLimitsAddition[s.ID][rite]);
                    }
                    temp.Add(rite, newTermNode);

                    NodeTuple nt = new NodeTuple(true, s.ID, rite);
                    FinalNodeTupleArray[newSubject.ID] = nt;
                    FinalTermNodes.Add(newTermNode);
                }
                PerRiskNodeDict[s.ID] = temp;
            }
        }

        public void UpdateTermChildParentsMappingForPerRisk()
        {
            HashSet<TermNode> newNodes = new HashSet<TermNode>(FinalTermNodes);
            newNodes.ExceptWith(OriginalTermNodesMinusPerRisk);

            foreach (TermNode node in newNodes)
            {
                PrimarySubject newSubject = node.Subject as PrimarySubject;
                int pNodeID = FinalNodeTupleArray[newSubject.ID].ParentSubjectID;
                HashSet<TermNode> pSet = new HashSet<TermNode>();

                //compare to original non-PerRisk nodes
                foreach (TermNode notPerRiskNode in OriginalTermNodesMinusPerRisk)
                {
                    PrimarySubject notPerRiskSub = notPerRiskNode.Subject as PrimarySubject;

                    //If the notPerRiskSub is parent of the perRiskNode, then all exploded will be child of this notPerRiskSub
                    if (OriginalTermNodeTreeMatrix[pNodeID, notPerRiskSub.ID] == NodeCompareOutcome.Parent)
                    {
                        pSet.Add(notPerRiskNode);
                    }
                    //else, if the notPerRiskSub and the perRiskNode are NOT disjoint, then have to compare any exploded to this notPerRiskNode
                    else if (OriginalTermNodeTreeMatrix[pNodeID, notPerRiskSub.ID] != NodeCompareOutcome.Disjoin)                               
                    {
                        NodeCompareOutcome result = CompareTwoSubjects(newSubject, notPerRiskSub);
                        if (result == NodeCompareOutcome.Parent)
                            pSet.Add(notPerRiskNode);
                        else if (result == NodeCompareOutcome.Child)
                            TermChildParentsMap[notPerRiskNode].Add(node);
                    }

                    //then check if this new node could be parent of notPerRiskNode
                    //if (notPerRiskSub.Schedule.ScheduleList.Count == 1 && OriginalNodeTreeMatrix[pNodeID, notPerRiskSub.ID] == NodeCompareOutcome.Overlap_Child 
                    //       && notPerRiskSub.Schedule.ScheduleList.First().ExposureID == FinalNodeTupleArray[newSubject.ID].RITE.ExposureID)
                    //{
                    //    ChildParentsMap[notPerRiskNode].Add(node);
                    //}
                }  //foreach notPerRiskNode

                //then compare to those perRiskNodes, it is possible that one of them is parent of the other, bbut the possibility is limited to
                //the perRiskNodes are Parent or Overlap_Parent.
                foreach (TermNode otherNode in DistinctPerRiskTermNodes)
                {
                    PrimarySubject otherSub = otherNode.Subject as PrimarySubject;
                    if (OriginalTermNodeTreeMatrix[pNodeID, otherSub.ID] == NodeCompareOutcome.Parent || OriginalTermNodeTreeMatrix[pNodeID, otherSub.ID] == NodeCompareOutcome.Overlap_Parent)
                    {
                        if (PerRiskNodeDict[otherSub.ID].ContainsKey(FinalNodeTupleArray[newSubject.ID].RITE))
                            pSet.Add(PerRiskNodeDict[otherSub.ID][FinalNodeTupleArray[newSubject.ID].RITE]);
                    }
                }  //foreach PerRiskNode

                TermChildParentsMap.Add(node, pSet);
            }

            //Rest are commented out. This was used to explode LeafCover nodes when they are linked to TermNodes and AtomicRites
            //--------------------------------------------------------------------------
            //update for CoverNode PerRisk  --leaf cover node first
            //foreach (CoverNode cNode in OriginalLeafCoverNodes)
            //{
            //    PrimarySubject s = cNode.Subject as PrimarySubject;
            //    HashSet<GraphNode> children = new HashSet<GraphNode>();
            //    HashSet<GraphNode> overlap = new HashSet<GraphNode>();
            //    if (OriginalCoverNodeChildrenMap.ContainsKey(cNode))
            //    { 
            //        children.UnionWith(OriginalCoverNodeChildrenMap[cNode]);
            //    }
            //    if (OriginalCoverNodeOverlapMap.ContainsKey(cNode))
            //    {
            //        overlap.UnionWith(OriginalCoverNodeOverlapMap[cNode]);
            //    }

            //    if (cNode.IsPerRisk)  //need generate new Cover node for each Rite
            //    {                                        
            //        children.UnionWith(overlap);

            //        //explode them
            //        foreach (RITE rite in s.Schedule.ScheduleList)
            //        {
            //            HashSet<RITE> aRite = new HashSet<RITE>() { rite };
            //            ScheduleOfRITEs newSchedule = new ScheduleOfRITEs("Exploded " + rite.ToString(), aRite, rite.RiskCharacteristics);
            //            PrimarySubject newSubject = FormNewSubject(s, newSchedule);
            //            CoverNode newCoverNode = FormNewCoverNode(newSubject, cNode.CoverName + "_" + rite.ExposureID, cNode);
            //            FinalLeafCoverNodes.Add(newCoverNode);
            //            LeafCoverNodeChildrenMap.Add(newCoverNode, new HashSet<GraphNode>());

            //            foreach (TermNode tNode in children)
            //            { 
            //               PrimarySubject ss = tNode.Subject as PrimarySubject;
            //               if (ss.Schedule.ScheduleList.Count() == 1 && ss.Schedule.ScheduleList.First().ExposureID == rite.ExposureID)
            //               {
            //                   LeafCoverNodeChildrenMap[newCoverNode].Add(tNode);                              
            //               }
            //               else if (PerRiskNodeDict[ss.ID].ContainsKey(rite))
            //               {
            //                   LeafCoverNodeChildrenMap[newCoverNode].Add(PerRiskNodeDict[ss.ID][rite]);
            //               }
            //            }
            //        }                
            //    }  //CoverNode is PerRisk
            //    else 
            //    {                   
            //        FinalLeafCoverNodes.Add(cNode);
            //        LeafCoverNodeChildrenMap.Add(cNode, new HashSet<GraphNode>());
            //        foreach (TermNode tNode in children)
            //        {
            //            PrimarySubject ss = tNode.Subject as PrimarySubject;
            //            if (tNode.IsPerRisk)
            //            {
            //                LeafCoverNodeChildrenMap[cNode].UnionWith(PerRiskNodeDict[ss.ID].Values);
            //            }
            //            else
            //            {
            //                LeafCoverNodeChildrenMap[cNode].Add(tNode);
            //            }
            //        }
            //        foreach (TermNode tNode in overlap)
            //        {
            //            PrimarySubject ss = tNode.Subject as PrimarySubject;
            //            if (tNode.IsPerRisk)
            //            {
            //                foreach (RITE rite in s.Schedule.ScheduleList)
            //                {                           
            //                    if (PerRiskNodeDict[ss.ID].ContainsKey(rite))
            //                    {
            //                        LeafCoverNodeChildrenMap[cNode].Add(PerRiskNodeDict[ss.ID][rite]);
            //                    }
            //                }
            //            }
            //        }
            //    } //CoverNode is not PerRisk            
            //}  //for each leafCoverNode
        }

        public void UpdateTermParentChildrenMappingForPerRisk()
        {
            //not necessary. This can be formed purely using Child-Parents mapping
            foreach (TermNode PerRiskNode in DistinctPerRiskTermNodes)
            {
                PrimarySubject PerRiskSub = PerRiskNode.Subject as PrimarySubject;

                //compre to orginal non-PerRisk nodes
                foreach (TermNode notPerRiskNode in OriginalTermNodesMinusPerRisk)
                {
                    PrimarySubject notPerRiskSub = notPerRiskNode.Subject as PrimarySubject;
                    HashSet<TermNode> cSet = new HashSet<TermNode>();

                    //if notPerRiskNode is parent of perRiskNode, then all exploded are the child of the notPerRiskNode
                    if (OriginalTermNodeTreeMatrix[PerRiskSub.ID, notPerRiskSub.ID] == NodeCompareOutcome.Parent)
                    {                                               
                        foreach (RITE rite in PerRiskNodeDict[PerRiskSub.ID].Keys)
                        {
                            cSet.Add(PerRiskNodeDict[PerRiskSub.ID][rite]);
                        }

                        if (TermParentChildrenMap.ContainsKey(notPerRiskNode))
                            TermParentChildrenMap[notPerRiskNode].UnionWith(cSet);
                        else
                            TermParentChildrenMap.Add(notPerRiskNode, cSet);
                    }
                    //else, check if any notPerRiskNode could be child of the exploded, which is only possible if the notPerRiskNode
                        //is Single Rite Node
                    else
                    {
                        if (notPerRiskSub.Schedule.ScheduleList.Count() == 1)
                        {
                            foreach (RITE rite in PerRiskNodeDict[PerRiskSub.ID].Keys)
                            {
                                if (notPerRiskSub.Schedule.ScheduleList.First().ExposureID == rite.ExposureID)
                                {
                                    if (OriginalTermNodeTreeMatrix[PerRiskSub.ID, notPerRiskSub.ID] == NodeCompareOutcome.Overlap_Parent)
                                    {
                                        cSet.Add(PerRiskNodeDict[PerRiskSub.ID][rite]);
                                        if (TermParentChildrenMap.ContainsKey(notPerRiskNode))
                                            TermParentChildrenMap[notPerRiskNode].UnionWith(cSet);
                                        else
                                            TermParentChildrenMap.Add(notPerRiskNode, cSet);
                                    }
                                    else if (OriginalTermNodeTreeMatrix[PerRiskSub.ID, notPerRiskSub.ID] == NodeCompareOutcome.Overlap_Child || OriginalTermNodeTreeMatrix[PerRiskSub.ID, notPerRiskSub.ID] == NodeCompareOutcome.Child)
                                    {
                                        if (TermParentChildrenMap.ContainsKey(PerRiskNodeDict[PerRiskSub.ID][rite])) //this should usually not happen
                                        {
                                            TermParentChildrenMap[PerRiskNodeDict[PerRiskSub.ID][rite]].Add(notPerRiskNode); //this loop may not be valid
                                        }
                                        else
                                        {
                                            TermParentChildrenMap.Add(PerRiskNodeDict[PerRiskSub.ID][rite], new HashSet<TermNode> { notPerRiskNode });
                                        }
                                    }
                                }
                            }
                        }
                        //notPerRiskNode is overlap_parent of perRiskNode, then check if the exploded RITE is in the notPerRiskSub.
                        else if (OriginalTermNodeTreeMatrix[PerRiskSub.ID, notPerRiskSub.ID] == NodeCompareOutcome.Overlap_Parent)
                        {
                            foreach (RITE rite in PerRiskNodeDict[PerRiskSub.ID].Keys)
                            {
                                if (notPerRiskSub.Schedule.ScheduleList.Contains(rite))
                                {
                                    if (TermParentChildrenMap.ContainsKey(notPerRiskNode))
                                        TermParentChildrenMap[notPerRiskNode].Add(PerRiskNodeDict[PerRiskSub.ID][rite]);
                                    else
                                        TermParentChildrenMap.Add(notPerRiskNode, new HashSet<TermNode>() { PerRiskNodeDict[PerRiskSub.ID][rite] });
                                }
                            }
                        }
                    }
                }

                //again, still have to check Nodes among exploded, only needed is the OriginalPerRiskTermNodes are Parent or Overlap_parent related.
                foreach (TermNode other in DistinctPerRiskTermNodes)
                {
                    PrimarySubject otherSub = other.Subject as PrimarySubject;
                    if (PerRiskSub.ID != otherSub.ID && (OriginalTermNodeTreeMatrix[PerRiskSub.ID, otherSub.ID] == NodeCompareOutcome.Parent || OriginalTermNodeTreeMatrix[PerRiskSub.ID, otherSub.ID] == NodeCompareOutcome.Overlap_Parent))
                    {
                        foreach (RITE rite in PerRiskNodeDict[PerRiskSub.ID].Keys)
                        {
                            if (PerRiskNodeDict[otherSub.ID].ContainsKey(rite))
                            {
                                if (TermParentChildrenMap.ContainsKey(PerRiskNodeDict[otherSub.ID][rite]))
                                    TermParentChildrenMap[PerRiskNodeDict[otherSub.ID][rite]].Add(PerRiskNodeDict[PerRiskSub.ID][rite]);
                                else
                                    TermParentChildrenMap.Add(PerRiskNodeDict[otherSub.ID][rite], new HashSet<TermNode> { PerRiskNodeDict[PerRiskSub.ID][rite] });
                            }
                        }
                    }
                }
            }
        }

        public void UpdateCoverGraphForPerRisk()
        {       
            List<CoverNode> OriginalLeafCover = new List<CoverNode>();
            Dictionary<int, List<CoverNode>> allLevelCoverNodes = new Dictionary<int, List<CoverNode>>();
            Dictionary<int, int> allLevelPartition = new Dictionary<int, int>();
            Dictionary<int, int> allLevelExpansion = new Dictionary<int, int>();

            //string[] stringSeparators = new string[] { "_on_" };

            //explode LeafCoverNodes first
            foreach (CoverNode cNode in OriginalLeafCoverNodes)
            {
                PrimarySubject s = cNode.Subject as PrimarySubject;
                CoverNodeExplosionDict.Add(cNode, new HashSet<CoverNode>());

                if (cNode.IsPerRisk)  //need generate new Cover node for each Rite
                {
                    //explode the coverNode
                    foreach (RITE rite in s.Schedule.ScheduleList)
                    {
                        HashSet<RITE> aRite = new HashSet<RITE>() { rite };
                        ScheduleOfRITEs newSchedule = new ScheduleOfRITEs("Exploded " + rite.ToString(), aRite, rite.RiskCharacteristics);
                        PrimarySubject newSubject = FormNewSubject(s, newSchedule);
                        CoverNode newCoverNode = FormNewCoverNode(newSubject, cNode.CoverName + "_on_" + rite.ExposureID, cNode);
                        //newCoverNode.SetFirstRITEID(rite.ExposureID);
                        newCoverNode.SetFirstRITEcharID(rite.RiskCharacteristics.First().ID);
                        //newCoverNode.CoverFactorIndex = rite.FactorIndex;
                        FinalLeafCoverNodes.Add(newCoverNode);
                        OriginalLeafCover.Add(cNode);
                        CoverNodeExplosionDict[cNode].Add(newCoverNode);
                    }
                    cNode.IsExploded = true;
                }  //CoverNode is PerRisk
                else
                {
                    FinalLeafCoverNodes.Add(cNode);
                    OriginalLeafCover.Add(cNode);
                    CoverNodeExplosionDict[cNode].Add(cNode);
                    cNode.IsExploded = true; //not perRisk, do not need to explode, so always exploded
                } //CoverNode is not PerRisk
            }

            //recursively explode derived cover nodes, CoverNodeParentChildrenMap is updated
            foreach (CoverNode derivedNode in OriginalDerivedCoverNodes)
            {
                RecuriveExplodeCoverNodeForPerRisk(derivedNode);
            }

            //to form CoverNodeChildParentMap from CoverNodeParentChildrenMap
            FormCoverNodeChildParentsMap();
        }

        public void FormCoverNodeChildParentsMap()
        {
            HashSet<CoverNode> allCoverNode = new HashSet<CoverNode>(FinalLeafCoverNodes);
            allCoverNode.UnionWith(FinalDerivedCoverNodes);

            foreach (CoverNode node in allCoverNode)
            {
                CoverNodeChildParentsMap.Add(node, new HashSet<CoverNode>());
                foreach (CoverNode pNode in CoverNodeParentChildrenMap.Keys)
                {
                    if (CoverNodeParentChildrenMap[pNode].Contains(node))
                        CoverNodeChildParentsMap[node].Add(pNode);
                }
            }            
        }

        public void RecuriveExplodeCoverNodeForPerRisk(CoverNode node)
        {            
            string[] stringSeparators = new string[] { "_on_" };

            if (node.IsExploded)
                return;

            CoverNodeExplosionDict.Add(node, new HashSet<CoverNode>());

            //every child should be exploded first, then the parent can be exploded
            foreach (CoverNode child in OriginalDerivedCoverNodeChildrenMap[node])
            {
                if (!child.IsExploded)
                    RecuriveExplodeCoverNodeForPerRisk(child);            
            }

            HashSet<CoverNode> allChildren = new HashSet<CoverNode>();
            Dictionary<String, List<String>> diffLocs = new Dictionary<String, List<String>>();
            Dictionary<String, HashSet<CoverNode>> diffLocsToNode = new Dictionary<String, HashSet<CoverNode>>();

            //after all children are exploded, allChildren HashSet stores the explosed children
            foreach (CoverNode child in OriginalDerivedCoverNodeChildrenMap[node])
            {
                allChildren.UnionWith(CoverNodeExplosionDict[child]);
            }

            Dictionary<String, float> tempFactorDict = new Dictionary<String, float>(); //Derived cover node factor should be related to its location
            Dictionary<String, int> tempFactorIndexDict = new Dictionary<String, int>(); //Derived cover node factor should be related to its location
            Dictionary<String, long> tempRitecharDict = new Dictionary<String, long>(); //Derived cover node factor should be related to its location

            if (node.IsPerRisk)
            {
                //explode, all children must be PerRisk as well, meaning all children cover name should have "_on_" in it
                //fina all distinct locations
                foreach (CoverNode child in allChildren)
                {
                    String[] tempString = child.CoverName.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                    String attLocString = "";

                    if (tempString.Count() > 1)
                        attLocString = tempString[1];
                    else
                        throw new NotImplementedException("child is not PerRisk while parent is");

                    if (diffLocs.ContainsKey(attLocString))
                    {
                        diffLocs[attLocString].Add(child.CoverName);
                        diffLocsToNode[attLocString].Add(child);
                    }
                    else 
                    {
                        diffLocs.Add(attLocString, new List<String> { child.CoverName });
                        diffLocsToNode.Add(attLocString, new HashSet<CoverNode> { child });
                        tempRitecharDict.Add(attLocString, child.GetFirstRITEcharID());
                        //tempFactorDict.Add(attLocString, child.CoverFactor);
                        //tempFactorIndexDict.Add(attLocString, child.CoverFactorIndex);
                    }

                    
                    //TODO: check if the Factors are the same form all children. if not, throw exception
                }

                foreach (String locString in diffLocs.Keys)
                {
                    String newCoverName = node.CoverName + "_on_" + locString;
                    PrimarySubject newSubject = new PrimarySubject(diffLocs[locString], true, node.Subject.AggFunctionName);
                    CoverNode newCoverNode = FormNewCoverNode(newSubject, newCoverName, node);
                    CoverNodeParentChildrenMap.Add(newCoverNode, diffLocsToNode[locString]);
                    //newCoverNode.CoverFactor = tempFactorDict[locString];
                    //newCoverNode.CoverFactorIndex = tempFactorIndexDict[locString];
                    newCoverNode.SetFirstRITEcharID(tempRitecharDict[locString]);
                    FinalDerivedCoverNodes.Add(newCoverNode);
                    CoverNodeExplosionDict[node].Add(newCoverNode);
                }
                node.IsExploded = true;
            } //isPerRisk
            else             
            {
                CoverNodeParentChildrenMap.Add(node, allChildren);
                FinalDerivedCoverNodes.Add(node);
                CoverNodeExplosionDict[node].Add(node);
                node.IsExploded = true;
            } //not perrisk
        }

        public bool IsLargerThan(TermNode sourceNode, TermNode compareNode)
        {
            if (!FinalNodeTupleArray[sourceNode.Subject.ID].Exploded && !FinalNodeTupleArray[compareNode.Subject.ID].Exploded)
            {
                if (OriginalTermNodeTreeMatrix[sourceNode.Subject.ID, compareNode.Subject.ID] == NodeCompareOutcome.Child)
                    return true;
                else
                    return false;
            }
            else if (FinalNodeTupleArray[sourceNode.Subject.ID].Exploded && FinalNodeTupleArray[compareNode.Subject.ID].Exploded)
            {
                if (FinalNodeTupleArray[sourceNode.Subject.ID].RITE.ExposureID != FinalNodeTupleArray[compareNode.Subject.ID].RITE.ExposureID)
                    return false;
                else if (OriginalTermNodeTreeMatrix[FinalNodeTupleArray[sourceNode.Subject.ID].ParentSubjectID, FinalNodeTupleArray[compareNode.Subject.ID].ParentSubjectID] == NodeCompareOutcome.Child ||
                         OriginalTermNodeTreeMatrix[FinalNodeTupleArray[sourceNode.Subject.ID].ParentSubjectID, FinalNodeTupleArray[compareNode.Subject.ID].ParentSubjectID] == NodeCompareOutcome.Overlap_Child)
                    return true;
                else
                    return false;
            }
            else if (!FinalNodeTupleArray[sourceNode.Subject.ID].Exploded && FinalNodeTupleArray[compareNode.Subject.ID].Exploded)
            {
                if (OriginalTermNodeTreeMatrix[FinalNodeTupleArray[sourceNode.Subject.ID].ParentSubjectID, FinalNodeTupleArray[compareNode.Subject.ID].ParentSubjectID] == NodeCompareOutcome.Disjoin)
                    return false;
                else if (OriginalTermNodeTreeMatrix[FinalNodeTupleArray[sourceNode.Subject.ID].ParentSubjectID, FinalNodeTupleArray[compareNode.Subject.ID].ParentSubjectID] == NodeCompareOutcome.Child)
                    return true;
                else
                {
                    PrimarySubject s1 = sourceNode.Subject as PrimarySubject;
                    PrimarySubject s2 = compareNode.Subject as PrimarySubject;

                    if (CompareTwoSubjects(s1, s2) == NodeCompareOutcome.Child)
                        return true;
                }
            }

            return false;
        }

        public bool IsLargerThan_map(TermNode sourceNode, TermNode compareNode)
        {
            if (TermParentChildrenMap.ContainsKey(sourceNode) && TermParentChildrenMap[sourceNode].Contains(compareNode))
                return true;
            else return false;
        }

        public PrimarySubject FormNewSubject(PrimarySubject s, ScheduleOfRITEs _schedule)
        {
            return new PrimarySubject(_schedule, s.ExposureTypes, s.CauseOfLossSet, s.IsPerRisk, s.AggFunctionName);
        }

        public PrimarySubject FormNewSubject(List<string> _childrenCoverNodeList, bool _isPerRisk, FunctionType _aggFunctionName)
        {
            return new PrimarySubject(_childrenCoverNodeList, _isPerRisk, _aggFunctionName);           
        }

        public TermNode FormNewTermNode(PrimarySubject s, TermNode tNode)
        {
            //TermNode tNode = node as TermNode;
            return new TermNode(s, tNode);
        }

        public CoverNode FormNewCoverNode(PrimarySubject s, String  cName, CoverNode node)
        {            
            return new CoverNode(s, cName, node);
        }

        public NodeCompareOutcome CompareTwoCOLs(HashSet<CauseOfLoss> list1, HashSet<CauseOfLoss> list2)
        {
            if (list1.Count == list2.Count && list1.IsSubsetOf(list2))
            {
                return NodeCompareOutcome.Equal;
            }
            else if (list1.Count < list2.Count && list1.IsProperSubsetOf(list2))
            {
                return NodeCompareOutcome.Parent;
            }
            else if (list1.Count > list2.Count && list1.IsProperSupersetOf(list2))
            {
                return NodeCompareOutcome.Child;
            }
            else if (list1.Overlaps(list2))
            {
                return NodeCompareOutcome.Overlap;
            }
            else
            {
                return NodeCompareOutcome.Disjoin;
            }
        }

        public NodeCompareOutcome CompareTwoSchedules(ScheduleOfRITEs s1, ScheduleOfRITEs s2)
        {
            HashSet<RITE> list1 = s1.ScheduleList;
            HashSet<RITE> list2 = s2.ScheduleList;

            if (list1.Count == list2.Count && list1.IsSubsetOf(list2))
            {
                return NodeCompareOutcome.Equal;
            }
            else if (list1.Count < list2.Count && list1.IsProperSubsetOf(list2))
            {
                return NodeCompareOutcome.Parent;
            }
            else if (list1.Count > list2.Count && list1.IsProperSupersetOf(list2))
            {
                return NodeCompareOutcome.Child;
            }
            else if (list1.Overlaps(list2))
            {
                return NodeCompareOutcome.Overlap;
            }
            else
            {
                return NodeCompareOutcome.Disjoin;
            }
        }


        public NodeCompareOutcome CompareTwoExpTypes(HashSet<ExposureType> list1, HashSet<ExposureType> list2)
        {
            if (list1.Count == list2.Count && list1.IsSubsetOf(list2))
            {
                return NodeCompareOutcome.Equal;
            }
            else if (list1.Count < list2.Count && list1.IsProperSubsetOf(list2))
            {
                return NodeCompareOutcome.Parent;
            }
            else if (list1.Count > list2.Count && list1.IsProperSupersetOf(list2))
            {
                return NodeCompareOutcome.Child;
            }
            else if (list1.Overlaps(list2))
            {
                return NodeCompareOutcome.Overlap;
            }
            else
            {
                return NodeCompareOutcome.Disjoin;
            }
        }

        public NodeCompareOutcome CompareTwoSubjects(PrimarySubject s1, PrimarySubject s2)
        {
            NodeCompareOutcome colOut = CompareTwoCOLs(s1.CauseOfLossSet.Collection, s2.CauseOfLossSet.Collection);
            NodeCompareOutcome expOut = CompareTwoExpTypes(s1.ExposureTypes.MappedTypes, s2.ExposureTypes.MappedTypes);
            NodeCompareOutcome schOut = CompareTwoSchedules(s1.Schedule, s2.Schedule);

            if (colOut == NodeCompareOutcome.Equal && expOut == NodeCompareOutcome.Equal && schOut == NodeCompareOutcome.Equal)
            {
                if (s1.IsPerRisk == s2.IsPerRisk)
                    return NodeCompareOutcome.Equal;
                else if (s1.IsPerRisk && !s2.IsPerRisk)
                    return NodeCompareOutcome.Parent;
                else
                    return NodeCompareOutcome.Child;
            }

            else if ((colOut == NodeCompareOutcome.Parent || colOut == NodeCompareOutcome.Equal) &&
                     (expOut == NodeCompareOutcome.Parent || expOut == NodeCompareOutcome.Equal) &&
                     (schOut == NodeCompareOutcome.Parent || schOut == NodeCompareOutcome.Equal))
                return NodeCompareOutcome.Parent;

            else if ((colOut == NodeCompareOutcome.Child || colOut == NodeCompareOutcome.Equal) &&
                     (expOut == NodeCompareOutcome.Child || expOut == NodeCompareOutcome.Equal) &&
                     (schOut == NodeCompareOutcome.Child || schOut == NodeCompareOutcome.Equal))
                return NodeCompareOutcome.Child;

            else if ((colOut == NodeCompareOutcome.Parent || colOut == NodeCompareOutcome.Equal) &&
                     (expOut == NodeCompareOutcome.Parent || expOut == NodeCompareOutcome.Equal) &&
                     (schOut == NodeCompareOutcome.Overlap))
                return NodeCompareOutcome.Overlap_Parent;

            else if ((colOut == NodeCompareOutcome.Child || colOut == NodeCompareOutcome.Equal) &&
                     (expOut == NodeCompareOutcome.Child || expOut == NodeCompareOutcome.Equal) &&
                     (schOut == NodeCompareOutcome.Overlap))
                return NodeCompareOutcome.Overlap_Child;

            else
                return NodeCompareOutcome.Disjoin;
        }
    }

    public enum NodeCompareOutcome
    {
        Child = -1,
        Equal = 100,
        NA = 0,
        Parent = 1,
        Overlap = 2,
        Overlap_Parent = 3,
        Overlap_Child = 4,
        Disjoin = 5
    }

    public class NodeTuple
    {
        public bool Exploded { get; set; }
        public RITE RITE { get; set; }
        public int ParentSubjectID { get; set; }

        public NodeTuple(bool _exploded, int _parentSId, RITE _rite)
        {
            Exploded = _exploded;
            RITE = _rite;
            ParentSubjectID = _parentSId;
        }
    }

}
