using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public class GraphExecuterAdaptor
    {
        public GraphInfo graphInfo { private set; get; }
        public Graph graph {get {return graphInfo.Graph;}}

        public GraphExecuterAdaptor(GraphInfo _graphInfo)
        {
            graphInfo = _graphInfo;
        }

        public ReferenceResultOutput RunExecution(GULossAdaptor guLossAdaptor)
        {

            GraphExecuter Executer;
            switch (graphInfo.Style)
            {
                case AutoGraphStyle.Matrix:
                    Executer = new GraphOfMatrixExecuter(graph as GraphOfMatrix);
                    break;
                case AutoGraphStyle.Node:
                    if (graph is PrimaryGraph)
                        Executer = new PrimaryGraphOfNodesExecuter(graph as PrimaryGraph);
                    else if (graph is TreatyGraph)
                        Executer = new TreatyGraphOfNodesExecuter(graph as TreatyGraph);
                    else
                        throw new NotSupportedException("Can only handle Node graphs of type Treaty and Primary type");
                    break;
                default:
                    throw new NotSupportedException("Can only handle graph of style Matrix or Node");
            }

            return Executer.Execute(guLossAdaptor);
        }
    }
}
