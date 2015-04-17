using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;

namespace NGFMReference
{
    public class GraphBuilder
    {
        GraphBuildCache graphCache;

        public GraphBuilder(GraphBuildCache _graphCache)
        {
            graphCache = _graphCache;
        }

        public GraphInfo MakeGraph(GraphType type, ExposureDataAdaptor expData, IRITEindexMapper indexMapper)
        {
            GraphInfo graphInfo;
            
            if (graphCache.GetGraphInfo(expData.ContractID, out graphInfo))
                return graphInfo;

            Graph graph;
            switch (type)
            {
                case GraphType.Auto:
                    ContractExtractor contract = GetContract(expData, graphCache);
                    contract.Extract();

                    AutoGraphStyle style = GetGraphStyle(contract);
                    //AutoGraphStyle style = AutoGraphStyle.Matrix;
                    //AutoGraphStyle style = AutoGraphStyle.Node;

                    AutoGraphBuilder builder;

                     if (style == AutoGraphStyle.Node)
                        builder = new AutoGraphOfNodesBuilder(contract);
                    else
                    {
                        builder = new AutoGraphOfMatrixBuilder(contract, indexMapper);
                    }

                    graph = builder.Build();
                    graphInfo = new GraphInfo(style, graph);
                    break;
                case GraphType.FixedGraph1:
                    graph = new FixedGraph1(expData);
                    graphInfo = new GraphInfo(AutoGraphStyle.Node, graph);
                    break;
                case GraphType.FixedGraph2:
                    graph = new FixedGraph2(expData);
                    graphInfo = new GraphInfo(AutoGraphStyle.Node, graph);
                    break;
                case GraphType.FixedGraphOverlap:
                    graph = new FixedGraphOverlap(expData);
                    graphInfo = new GraphInfo(AutoGraphStyle.Node, graph);
                    break;
                case GraphType.FixedGraphOverlapSubperil:
                    graph = new FixedGraphOverlapSubperil(expData);
                    graphInfo = new GraphInfo(AutoGraphStyle.Node, graph);
                    break;
                case GraphType.FixedGraphOverlapSubperil2:
                    graph = new FixedGraphOverlapSubperil2(expData);
                    graphInfo = new GraphInfo(AutoGraphStyle.Node, graph);
                    break;
                case GraphType.FixedTreaty1:
                    graph = new FixedTreaty1(expData, graphCache);
                    graphInfo = new GraphInfo(AutoGraphStyle.Node, graph);
                    break;
                //case GraphType.FixedMatrixGraphJPTY:
                //    graph  = new FixedMatrixGraphJPTY(expData);
                //    graphInfo = new GraphInfo(AutoGraphStyle.Matrix, graph);
                //    //graph.Initialize();
                //    break;

                default:
                    throw new NotSupportedException("Cannot currently support this treaty type");
            }


            graph.Initialize();

            if (graph is FixedPrimaryGraph || graph is FixedTreaty1)   ///need to remove FixedTreaty1 condition
                GetTermsForGraph(expData, graph as GraphOfNodes);

            graph.PeriodReset();

            graphCache.Add(graph.ContractID, graphInfo);
            return graphInfo;  
        }

        private void GetTermsForGraph(ExposureDataAdaptor expData, GraphOfNodes graph)
        {
            string message;
            DeclarationExtractor declarationExtractor = new DeclarationExtractor(expData);
            declarationExtractor.GetDeclarations(graph.Declarations, out message);
            FinancialTermExtractor fintermExtractor;

            if (graph.Declarations.ContractType == "Primary Policy")
                fintermExtractor = new PrimaryTermExtractor(expData, graph.Declarations);
            else if (graph.Declarations.ContractType == "Catastrophe Treaty")
                fintermExtractor = new TreatyTermExtractor(expData, graph.Declarations, graphCache);
            else
                throw new NotSupportedException();

            fintermExtractor.GetTermsForGraph(graph, out message);
        }

        private ContractExtractor GetContract(ExposureDataAdaptor expData, GraphBuildCache graphCache)
        {
            //Make all graph nodes for Graph
            List<GraphNode> IntitialGraphNodes = new List<GraphNode>();
            ContractExtractor contract;
            if (expData.TreatyExposure)
                contract = new TreatyContractExtractor(expData, graphCache);
            else
                contract = new PrimaryContractExtractor(expData);

            return contract;
        }

        private AutoGraphStyle GetGraphStyle(ContractExtractor contract)
        {
            if (contract.IsTreaty)
                return AutoGraphStyle.Node;
            else
            {
                PrimaryContractExtractor primaryContract = contract as PrimaryContractExtractor;

                List<PrimarySubject>  primarysubjects = primaryContract.GetAllTermSubjects();

                foreach (PrimarySubject priSub in primarysubjects)
                {
                    DeductibleCollection dedCol;
                    if(primaryContract.GetDeductiblesForSubject(priSub, out dedCol))
                        foreach (Deductible ded in dedCol)
                        {                            
                            if (ded.DedInterType == DedInteractionType.SingleLargest)
                                return AutoGraphStyle.Node;

                            if (ded.DedType == TermValueType.PercentAffected || ded.DedType == TermValueType.PercentLoss)
                                return AutoGraphStyle.Node;
                        }
                }

                return AutoGraphStyle.Matrix;
            }
        }

    }

    public class GraphInfo
    {
        public AutoGraphStyle Style { get; set; }
        public Graph Graph { get; set; }

        public GraphInfo(AutoGraphStyle _style, Graph _graph)
        {
            Style = _style;
            Graph = _graph;
        }
    }

    public enum AutoGraphStyle
    {
        Matrix,
        Node
    }

    public enum GraphType
    {
        Treaty,
        Auto,
        FixedGraph1,
        FixedGraph2,
        FixedGraphOverlap,
        FixedGraphOverlapSubperil,
        FixedGraphOverlapSubperil2,
        FixedTreaty1,
        FixedMatrixGraph,
        FixedMatrixGraphAmlin1, 
        FixedMatrixGraphAmlin2,
        FixedMatrixGraphJPTY,
        FixedMatrixGraphPerRisk
    }
}
