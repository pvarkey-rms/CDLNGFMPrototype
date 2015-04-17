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

        public Graph MakeGraph(GraphType type, ExposureDataAdaptor expData)
        {
            Graph graph;

            if (graphCache.GetContract(expData.ContractID, out graph))
                return graph;
           
            switch (type)
            {
                case GraphType.Auto:
                    AutoGraphBuilder builder = new AutoGraphBuilder(expData, graphCache);
                    graph = builder.Build();
                    break;
                case GraphType.FixedGraph1:
                    graph = new FixedGraph1(expData);
                    //graph.Initialize();
                    break;
                case GraphType.FixedGraph2:
                    graph = new FixedGraph2(expData);
                    //graph.Initialize();
                    break;
                case GraphType.FixedGraphOverlap:
                    graph = new FixedGraphOverlap(expData);
                    //graph.Initialize();
                    break;
                case GraphType.FixedGraphOverlapSubperil:
                    graph = new FixedGraphOverlapSubperil(expData);
                    //graph.Initialize();
                    break;
                case GraphType.FixedGraphOverlapSubperil2:
                    graph = new FixedGraphOverlapSubperil2(expData);
                    //graph.Initialize();
                    break;
                case GraphType.FixedTreaty1:
                    graph = new FixedTreaty1(expData, graphCache);
                    //graph.Initialize();
                    break;
                default:
                    throw new NotSupportedException("Cannot currently support this treaty type");
            }
            graph.Initialize();


            if (graph is FixedPrimaryGraph || graph is FixedTreaty1)   ///need to remove FixedTreaty1 condition
                GetTermsForGraph(expData, graph);

            graph.Reset();
            graphCache.Add(graph.ContractID, graph);
            return graph;  
        }

        private void GetTermsForGraph(ExposureDataAdaptor expData, Graph graph)
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
        FixedTreaty1
    }
}
