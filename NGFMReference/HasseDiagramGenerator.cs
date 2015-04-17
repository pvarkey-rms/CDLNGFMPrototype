using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;

namespace NGFMReference
{
    public class HasseDiagramGenerator
    {

        public HasseDiagram Generate(List<GraphNode> NodesForGraph)
        {
            //LargerThanTest(NodesForGraph);
            HasseDiagram HDM = new HasseDiagram(HasseNodeFactory.NodeType.STRING);
            HasseNodeCollection elements = new HasseNodeCollection();

            foreach (GraphNode node in NodesForGraph)
            {
                HasseNode hasseNode = node as HasseNode;
                HDM.AddNode(node);
            }

            return HDM;
        }

        private void LargerThanTest(List<GraphNode> NodesForGraph)
        {
            bool test, test1, test2, test3, test4, test5, test6, test7;

            test = NodesForGraph[0].IsLargerThan(NodesForGraph[1]);
            test1 = NodesForGraph[1].IsLargerThan(NodesForGraph[2]);
            test2 = NodesForGraph[2].IsLargerThan(NodesForGraph[1]);
            test3 = NodesForGraph[3].IsLargerThan(NodesForGraph[3]);
            test4 = NodesForGraph[3].IsLargerThan(NodesForGraph[2]);
            test5 = NodesForGraph[4].IsLargerThan(NodesForGraph[2]);
            test6 = NodesForGraph[2].IsLargerThan(NodesForGraph[3]);
            test7 = NodesForGraph[3].IsLargerThan(NodesForGraph[1]);

        }

    }
}
