using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public class DescriptiveGraphBuilder
    {        
        public NodeTree CompleteGraph {get; private set;}  //has all parent-children relations, but NOT transitive reduced
        private List<TermNode> TermNodeList {get; set;}
        private List<CoverNode> CoverNodeList {get; set;}
       

        public DescriptiveGraphBuilder(List<TermNode> tNodeList, List<CoverNode> cNodeList)
        {
            TermNodeList = new List<TermNode>(tNodeList);
            CoverNodeList = new List<CoverNode>(cNodeList);
        }

        public DescriptiveGraph FormDescriptiveGraph()
        {
            Boolean termNodesIsOverlapped;
            Dictionary<TermNode, HashSet<TermNode>> termChildParentsMap;
            DescriptiveGraph DGraph = new DescriptiveGraph();
            CompleteGraph = FormCompleteGraph();
            UpdateTermNodes(CompleteGraph);
            UpdateCoverGraphForReinstatements(CompleteGraph);
            Dictionary<TermNode, HashSet<TermNode>> termGraph = FormGeneralTermGraph(CompleteGraph, out termNodesIsOverlapped, out termChildParentsMap);
            Dictionary<CoverNode, HashSet<CoverNode>> coverGraph = CompleteGraph.CoverNodeParentChildrenMap;
            Dictionary<CoverNode, CoverNodeRecoverableSubject> coverGraphTermGraphMapping = new Dictionary<CoverNode, CoverNodeRecoverableSubject>();

            DescriptiveGraph dGraph =  new DescriptiveGraph(termGraph, coverGraph, coverGraphTermGraphMapping, termNodesIsOverlapped);
            dGraph.SetCoverGraphChildParentsMap(CompleteGraph.CoverNodeChildParentsMap);
            dGraph.SetTermGraphChildParentsMap(termChildParentsMap);
            dGraph.SetLeafCoverNodes(CompleteGraph.FinalLeafCoverNodes);
            dGraph.SetDerivedCoverNodes(CompleteGraph.FinalDerivedCoverNodes);
            return dGraph;
        }

        public void UpdateCoverGraphForReinstatements(NodeTree _completeGraph)
        { 
           //for OccLimit Reinstatement cover, if it does not have any parents with AggLimit, then add a agg layer cover on top of it
            List<CoverNode> listOfCoverNodes = new List<CoverNode>();
            listOfCoverNodes.AddRange(_completeGraph.FinalDerivedCoverNodes);
            listOfCoverNodes.AddRange(_completeGraph.FinalLeafCoverNodes);

           foreach (CoverNode cNode in listOfCoverNodes)
           {
               if (cNode.Cover.LimitTimeBasis == TimeBasis.Occurrence && cNode.Cover.UnlimitedReinstatements == false && cNode.Cover.NumofReinstatements > 0)
               {
                   Boolean found = false;
                   foreach (CoverNode pCNode in _completeGraph.CoverNodeChildParentsMap[cNode])
                   {
                       if (pCNode.Cover.LimitTimeBasis == TimeBasis.Aggregate && pCNode.Cover.Unlimited == false)
                       {
                           found = true;
                           break;
                       }                   
                   }

                   if (!found) //means: No aggLimit for this OccLimit with reinstatement, we need add aggLimit layer
                   {
                       List<string> _childrenCoverNodeList = new List<string>(){cNode.CoverName};
                       PrimarySubject newSubject = new PrimarySubject(_childrenCoverNodeList);

                       Cover newCover = new Cover("Agg_on_" + cNode.CoverName, false, new ContractModel.MonetaryValue(0.0), new ContractModel.MonetaryValue(cNode.Cover.LimitAmount * (cNode.Cover.NumofReinstatements + 1)), new ContractModel.PercentValue(100), false, TimeBasis.Aggregate, TimeBasis.Aggregate, TermValueType.Numeric, 0, true);
                       CoverNode newCoverNode = new CoverNode(newSubject, newCover.CoverName);
                       newCoverNode.Cover = newCover;
                       //then add this Cover to completeGraph
                       _completeGraph.CoverNodeChildParentsMap[cNode].Add(newCoverNode);
                       _completeGraph.CoverNodeParentChildrenMap.Add(newCoverNode, new HashSet<CoverNode>{cNode});
                       _completeGraph.FinalDerivedCoverNodes.Add(newCoverNode);
                   }                   
               }                        
           }
        }

        public NodeTree FormCompleteGraph()
        {
            //Do the Node Tree
            NodeTree CompleteGraph = new NodeTree(TermNodeList, CoverNodeList);
            CompleteGraph.Run();

            return CompleteGraph;
        }

        public void UpdateTermNodes(NodeTree _completeGraph)
        {
            foreach (TermNode tNode in _completeGraph.FinalTermNodes)
            {
                //evaluate percent ded and limit      
                foreach (Deductible dedObj in tNode.Deductibles.GetDedList())
                {
                    if (dedObj.DedType == TermValueType.PercentCovered)
                    {
                        dedObj.UpdatePercentDed((float)tNode.GetTIV()/tNode.GetNumOfBuildingsOrigin());
                    }
                }
                foreach (Limit limObj in tNode.Limits.GetLimList())
                {
                    if (limObj.LimType == TermValueType.PercentCovered)
                    {
                        limObj.UpdatePercentLimit((float)tNode.GetTIV() / tNode.GetNumOfBuildingsOrigin());
                    }
                }
            }
        }

        protected Dictionary<TermNode, HashSet<TermNode>> FormGeneralTermGraph(NodeTree _completeGraph, 
                                                               out Boolean termNodesIsOverlapped, out Dictionary<TermNode, HashSet<TermNode>> termGraphChildParentsMap)
        {

            //this should return the GeneralGraph object which is to be generated by Sunny
            //do: Transitive Reduction, and add AtomicRite
            //NodeTree generalGraph = _completeGraph;           
            termNodesIsOverlapped = IsOverlappedByTermNodes(_completeGraph.TermChildParentsMap);
            Dictionary<TermNode, HashSet<TermNode>> termGraph;
            if (termNodesIsOverlapped)
                termGraph = FormGeneralTermGraphOverlapped(_completeGraph, out termGraphChildParentsMap);
            else
                termGraph = FormGereralTermGraphNonOverlapped(_completeGraph, out termGraphChildParentsMap);

            return termGraph;
        }

        protected Dictionary<TermNode, HashSet<TermNode>> FormGereralTermGraphNonOverlapped(NodeTree _completeGraph, out Dictionary<TermNode, HashSet<TermNode>> termGraphChildParentsMap)
        {
            Dictionary<TermNode, HashSet<TermNode>> TermCPMap = _completeGraph.TermChildParentsMap;
            Dictionary<TermNode, HashSet<TermNode>> TermPCMap = _completeGraph.TermParentChildrenMap;

            //TermNodes relationship after Transitive Reduction
            Dictionary<TermNode, HashSet<TermNode>> ReducedTermCPMap = new Dictionary<TermNode, HashSet<TermNode>>();
            Dictionary<TermNode, HashSet<TermNode>> ReducedTermPCMap = new Dictionary<TermNode, HashSet<TermNode>>();

            //form levels           
            Dictionary<int, HashSet<TermNode>> nodesByLevel = new Dictionary<int, HashSet<TermNode>>();

            //find the levels
            int numOfNodeLevels = 0;
            nodesByLevel = SetLevels(TermCPMap, out numOfNodeLevels);

            HashSet<TermNode> cSet;
            HashSet<TermNode> pSet;

            //if there are no TermNodes, no transitive rduction, do nothing
            if (numOfNodeLevels == 0)
            {
            }

            for (int j = 0; j < numOfNodeLevels; j++)
            {
                foreach (TermNode node in nodesByLevel[j])
                {
                    TermNode tNode = node as TermNode;
                    cSet = new HashSet<TermNode>();
                    pSet = new HashSet<TermNode>();

                    if (TermPCMap.ContainsKey(tNode))
                        cSet.UnionWith(TermPCMap[tNode]);

                    if (TermCPMap.ContainsKey(tNode))
                        pSet.UnionWith(TermCPMap[tNode]);

                    if ((j + 1) < numOfNodeLevels)
                        cSet.IntersectWith(nodesByLevel[j + 1]);

                    if ((j - 1) > 0)
                        pSet.IntersectWith(nodesByLevel[j - 1]);

                    ReducedTermCPMap.Add(tNode, pSet);  //every node will be added to child-parents map

                    if (cSet.Count() > 0)
                        ReducedTermPCMap.Add(tNode, cSet); //only nodes with children will be added to PC map
                }
            }

            termGraphChildParentsMap = ReducedTermCPMap;
            return ReducedTermPCMap;
        }

        protected Dictionary<TermNode, HashSet<TermNode>> FormGeneralTermGraphOverlapped(NodeTree _completeGraph, out Dictionary<TermNode, HashSet<TermNode>> termGraphChildParentsMap)
        {
            NodeTree generalGraph = _completeGraph;
            Dictionary<TermNode, HashSet<TermNode>> TermCPMap = _completeGraph.TermChildParentsMap;
            Dictionary<TermNode, HashSet<TermNode>> TermPCMap = _completeGraph.TermParentChildrenMap;

            //TermNodes relationship after Transitive Reduction
            Dictionary<TermNode, HashSet<TermNode>> ReducedTermCPMap = new Dictionary<TermNode, HashSet<TermNode>>();
            Dictionary<TermNode, HashSet<TermNode>> ReducedTermPCMap = new Dictionary<TermNode, HashSet<TermNode>>();

            //direct child of pNode should be those whose parents are not children of pNode
            foreach (TermNode pNode in TermPCMap.Keys)
            {
                ReducedTermPCMap.Add(pNode, new HashSet<TermNode>());
                HashSet<TermNode> allChildren = new HashSet<TermNode>(TermPCMap[pNode]);
                foreach (TermNode cNode in allChildren)
                {
                    HashSet<TermNode> childSParents = new HashSet<TermNode>(TermCPMap[cNode]);
                    if (!childSParents.Overlaps(TermPCMap[pNode]))
                    {
                        ReducedTermPCMap[pNode].Add(cNode);
                    }
                }
            }
            termGraphChildParentsMap = ReducedTermCPMap;
            return ReducedTermPCMap;
        }

        protected void FormGeneralCoverGraph(NodeTree _completeGraph)
        {
            CoverNodeRecoverableSubject cNodeRecSub = new CoverNodeRecoverableSubject(new HashSet<AtomicRITE>(), new HashSet<TermNode>());
        }

        public Dictionary<int, HashSet<TermNode>> SetLevels(Dictionary<TermNode, HashSet<TermNode>> CPMap, out int numOfNodeLevels)
        {
            Dictionary<int, HashSet<TermNode>> nodesByLevel = new Dictionary<int, HashSet<TermNode>>();
            numOfNodeLevels = 0;

            foreach (TermNode node in CPMap.Keys)
            {
                int numOfParents = CPMap[node].Count();

                if (nodesByLevel.ContainsKey(numOfParents))
                    nodesByLevel[numOfParents].Add(node);
                else
                {
                    numOfNodeLevels++;
                    nodesByLevel.Add(numOfParents, new HashSet<TermNode> { node });
                }
            }
            return nodesByLevel;
        }

        public bool IsOverlappedByTermNodes(Dictionary<TermNode, HashSet<TermNode>> CPMap)
        {
            foreach (TermNode node in CPMap.Keys)
            {
                List<TermNode> pList = CPMap[node].ToList();
                for (int i = 0; i < pList.Count(); i++)
                {
                    TermNode node1 = pList[i];
                    for (int j = i + 1; j < pList.Count(); j++)
                    {
                        TermNode node2 = pList[j];
                        if (!CPMap[node1].Contains(node2) && !CPMap[node2].Contains(node1))
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
