using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;

namespace NGFMReference
{
    public class AutoGraphBuilder
    {
        private ExposureDataAdaptor expData;
        private GraphBuildCache graphChache;

        public AutoGraphBuilder(ExposureDataAdaptor _expData, GraphBuildCache _graphChache)
        {
            expData = _expData;
            graphChache = _graphChache;
        }

        public Graph Build()
        {
            //Make all graph nodes for Graph
            List<GraphNode> IntitialGraphNodes = new List<GraphNode>();
            ContractExtractor contract;
            if (expData.TreatyExposure)
                contract = new TreatyContractExtractor(expData, graphChache);
            else
                contract = new PrimaryContractExtractor(expData);

            //Get All Cover Nodes
            contract.Extract();
            List<Subject> graphSubs = contract.GetAllCoverSubjects();
            IntitialGraphNodes.AddRange(GetCoverNodes(contract, graphSubs));

            //Get All Term Nodes
            if (!expData.TreatyExposure)
            {
                PrimaryContractExtractor primaryContract = (PrimaryContractExtractor)contract;
                List<PrimarySubject> termgraphSubs = primaryContract.GetAllTermSubjects();
                IntitialGraphNodes.AddRange(GetTermNodes(primaryContract, termgraphSubs));
            }

            //Build Hasse Diagram from Graph Nodes
            HasseDiagramGenerator HasseBuilder = new HasseDiagramGenerator();
            HasseDiagram HasseDiagram = HasseBuilder.Generate(IntitialGraphNodes);

            //Convert Hasse Diagram to Reference.Graph object
            List<GraphNode> TopGraphNodes = new List<GraphNode>();
            List<GraphNode> FinalGraphNodes = HasseDiagram.HasseDiagramNodes.Where(pair => pair.Key != "{}")                                          
                                                                            .Select(pair => pair.Value)
                                                                            .Cast<GraphNode>()
                                                                            .ToList();
            Dictionary<GraphNode, List<GraphNode>> ParentToChildrenMap = new Dictionary<GraphNode, List<GraphNode>>();

            foreach (GraphNode graphnode in FinalGraphNodes)
            {
                List<GraphNode> childrenList = graphnode.EdgesToCovered.Select(edge => edge.LowerNode)
                                                                      .Where(node => node.KeyString != "")
                                                                      .Cast<GraphNode>().ToList();
                if (childrenList.Count > 0)
                    ParentToChildrenMap.Add(graphnode, childrenList);

                if (graphnode.EdgesToCovering.Count == 0)
                    TopGraphNodes.Add(graphnode);
            }

            Graph graph;
            if (expData.TreatyExposure)
            {
                graph = new TreatyGraph(expData.ContractID, TopGraphNodes, FinalGraphNodes, ParentToChildrenMap, contract.Declarations);
            }
            else
            {
                graph = new PrimaryGraph(expData.ContractID, TopGraphNodes, FinalGraphNodes, ParentToChildrenMap, contract.Declarations);
            }

            //graph.BuildAtomicRITEs();
            //graph.AssignLevelToNode();
            //graph.GraphReady = true;
            return graph;
        }

        private List<GraphNode> GetCoverNodes(ContractExtractor contract, List<Subject> graphSubs)
        {
            List<GraphNode> coverNodes = new List<GraphNode>();

            foreach (Subject sub in graphSubs)
            {
                HashSet<Cover> covers;
                if (contract.GetCoversForSubject(sub, out covers))
                {
                    foreach (Cover cover in covers)
                    {
                        CoverNode node = new CoverNode(sub, cover.CoverName);
                        node.Cover = cover;
                        coverNodes.Add(node); 
                    }                                                                                                                                                           
                }              
            }

            return coverNodes;
        }

        private List<GraphNode> GetTermNodes(PrimaryContractExtractor contract, List<PrimarySubject> graphSubs)
        {
            List<GraphNode> termNodes = new List<GraphNode>();
            foreach (PrimarySubject sub in graphSubs)
            {
                TermNode node = new TermNode(sub);

                DeductibleCollection Deds;
                if (contract.GetDeductiblesForSubject(sub, out Deds))
                    node.Deductibles = Deds;                                                                                                                                                
                
                LimitCollection Lims;
                if (contract.GetLimitsForSubject(sub, out Lims))
                    node.Limits = Lims;
                                                                                                                                                                                          
                termNodes.Add(node);   
            }

            return termNodes;
        }
    }      
}
