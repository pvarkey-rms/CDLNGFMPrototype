using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFMReference;
using Rms.DataServices.DataObjects;
using Rms.Analytics.DataService.Zip;
using ProtoBuf;
using System.Net;

namespace Neo4JTestProject
{
    class Program
    {
        private int m_LevelCount = 0;
        static void Main(string[] args)
        {
            Program obj = new Program();

            // Init
            GraphClient client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();

            //Construct Graph
            GraphOfNodes graphNodes = obj.getGraphInfo();
            Dictionary<int, int> SubjectIdLevelMapping = obj.getLevelDictionary(graphNodes.LevelNodeDict);
            obj.m_LevelCount = SubjectIdLevelMapping.Keys.Count;
            Dictionary<int, Level> levelDictionary = obj.createLevelObjects();
            //obj.createGraph(graphNodes,SubjectIdLevelMapping, client);

            //Get queries
            obj.createMatrixArrays(client, levelDictionary);
        }

        private Dictionary<int, Level> createLevelObjects()
        {
            Dictionary<int, Level> levelDictionary = new Dictionary<int, Level>();
            for (int i=1; i <= m_LevelCount; i++)
            {
                levelDictionary.Add(i, new Level());
            }
            return levelDictionary;
        }

        GraphOfNodes getGraphInfo()
        {
            //string filepath = @"D:\Work\Sample-Protobuffs\Franchise-Perrisk\test\rites_batch0.dat";
            string filepath = @"D:\Work\Sample-Protobuffs\jpty_case.dat";

            PartitionData PD = Deserialize(filepath);
            PartitionDataAdpator PDataAdaptor = new PartitionDataAdpator(PD);

            ReferencePrototype refprototype = new ReferencePrototype(PD);
            //long contId = 11324656;
            long contId = 42586728;
            //long contId = 11331;
            refprototype.ReferencePrepare(GraphType.Auto);
            
            ExposureDataAdaptor expData = PDataAdaptor.GetExposureAdaptor(contId);


            GraphInfo graphInfo = GetGraph(GraphType.Auto, expData, new GraphBuildCache(PDataAdaptor));
            GraphOfNodes graphNodes = (GraphOfNodes)graphInfo.Graph;
            graphNodes.AssignLevelToNode();
            return graphNodes;

        }

        void createGraph(GraphOfNodes graphNodes, Dictionary<int, int> SubjectIdLevelMapping, GraphClient client)
        {
            List<GraphNode> lst_graphNodes = graphNodes.GraphNodes;

            Dictionary<GraphNode, List<GraphNode>> lst_relationships = graphNodes.ParentToChildrenMap;
            if (lst_graphNodes == null || lst_relationships == null)
                return;

            
            Dictionary<TermNode, NodeReference<Neo4jTermNode>> m_term_nodes = new Dictionary<TermNode, NodeReference<Neo4jTermNode>>();
            Dictionary<CoverNode, NodeReference<Neo4jCoverNode>> m_cover_nodes = new Dictionary<CoverNode, NodeReference<Neo4jCoverNode>>();


            //Add Nodes
            foreach (GraphNode node in lst_graphNodes)
            {
                if (node is TermNode)
                {
                    TermNode term_node = (TermNode)node;
                    
                    //term_node.ded;
                    int id = node.Subject.ID;
                    Neo4jTermNode tn = new Neo4jTermNode();
                    tn.NodeID = id;
                    tn.NumberOfBuildings= term_node.PrimarySubject.Schedule.ActNumOfBldgs;
                    tn.IsPerRisk = term_node.IsPerRisk;
                    tn.Level = SubjectIdLevelMapping[id];

                    if (term_node.Deductibles.GetDedList() != null && term_node.Deductibles.GetDedList().Count != 0)
                        tn.Deductible = term_node.Deductibles.GetDedList().First().Amount;
                    
                    if (term_node.Limits.GetLimList() != null && term_node.Limits.GetLimList().Count != 0)
                        tn.Limit = term_node.Limits.GetLimList().First().Amount;
                    
                    var ref1 = client.Create(tn);
                    m_term_nodes.Add(term_node, (NodeReference<Neo4jTermNode>)ref1);
                }
                else if (node is CoverNode)
                {
                    CoverNode cover_node = (CoverNode)node;
                    //term_node.ded;
                    string coverName = cover_node.CoverName;
                    Neo4jCoverNode cn = new Neo4jCoverNode();
                    cn.Name = coverName;
                    cn.IsPerRisk = cover_node.IsPerRisk;
                    cn.Level = SubjectIdLevelMapping[cover_node.Subject.ID];

                    var ref1 = client.Create(cn);
                    m_cover_nodes.Add(cover_node, (NodeReference<Neo4jCoverNode>)ref1);
                }
            }

            //Add relationships
            foreach (KeyValuePair<GraphNode, List<GraphNode>> rel in lst_relationships)
            {
                //Parent is  term Node
                if (rel.Key is TermNode)
                {
                    NodeReference<Neo4jTermNode> parentTermNode;
                    m_term_nodes.TryGetValue(rel.Key as TermNode, out parentTermNode);
                    List<GraphNode> lst_childnodes = rel.Value;
                    foreach (GraphNode child in lst_childnodes)
                    {
                        NodeReference<Neo4jTermNode> childGraphTermNode;
                        m_term_nodes.TryGetValue(child as TermNode, out childGraphTermNode);

                        if (child != null)
                            client.CreateRelationship<Neo4jTermNode, TermChildRelationship>(parentTermNode, new TermChildRelationship(childGraphTermNode));
                    }
                }

                //Parent is  Cover Node
                else if (rel.Key is CoverNode)
                {
                    NodeReference<Neo4jCoverNode> parentTermNode;
                    m_cover_nodes.TryGetValue(rel.Key as CoverNode, out parentTermNode);
                    List<GraphNode> lst_childnodes = rel.Value;
                    foreach (GraphNode child in lst_childnodes)
                    {
                        if (child is TermNode)
                        {
                            NodeReference<Neo4jTermNode> childGraphTermNode;
                            m_term_nodes.TryGetValue(child as TermNode, out childGraphTermNode);

                            if (child != null)
                                client.CreateRelationship<Neo4jCoverNode, CoverChildRelationship>(parentTermNode, new CoverChildRelationship(childGraphTermNode));
                        }

                        else if (child is CoverNode)
                        {
                            NodeReference<Neo4jCoverNode> childGraphCoverNode;
                            m_cover_nodes.TryGetValue(child as CoverNode, out childGraphCoverNode);

                            if (child != null)
                                client.CreateRelationship<Neo4jCoverNode, DerivedCoverChildRelationship>(parentTermNode, new DerivedCoverChildRelationship(childGraphCoverNode));
                        }
                        else
                            throw new NotSupportedException("Can only handle nodes of type term and cover");
                    }
                }
             }
            // Create entities
            //var refA = client.Create(new Person() { Name = "Person A" });
            //var refB = client.Create(new Person() { Name = "Person B" });
            //var refC = client.Create(new Person() { Name = "Person C" });
            //var refD = client.Create(new Person() { Name = "Person D" });
 
            //// Create relationships
            //client.CreateRelationship(refA, new KnowsRelationship(refB));
            //client.CreateRelationship(refB, new KnowsRelationship(refC));
            //client.CreateRelationship(refB, new HatesRelationship(refD), new HatesData("Crazy guy")));
            //client.CreateRelationship(refC, new HatesRelationship(refD), new HatesData("Don't know why...")));
            //client.CreateRelationship(refD, new KnowsRelationship(refA));
        }

        void createMatrixArrays(GraphClient client, Dictionary<int, Level> levelDictionary)
        {
            //get all root nodes
            var results = client.Cypher.Match("(n{Level:0})")
                            .Return((n) => new
                            {
                                Neo4jCoverNode = n.As<Neo4jCoverNode>(),
                            }).Results;

            //initialize level 0 partition array
            int[] Level0Partition = new int[results.Count()];

            int RootNodeCounter = 0;
            // create limit, deductible arrays and partition arrays for the cover nodes
            foreach (var rootnode in results)
            {
                Level0Partition[RootNodeCounter] = RootNodeCounter + 1; //update level 0 partition array
                IEnumerable<Neo4jTermNode> coverChildren = getCoverChildren(rootnode.Neo4jCoverNode.Name, client);

                if (coverChildren != null)
                {
                    //update the children level objects
                    updateChildrenArrays(coverChildren, client, 1);
                }

                RootNodeCounter++;
                Console.WriteLine("{0} is the Level 0 Node", rootnode.Neo4jCoverNode.Name);
            }
            //set the level 0 partition array
            levelDictionary[0].PartitionArray = Level0Partition;

            Console.WriteLine("Done");
        }

        private void updateChildrenArrays(IEnumerable<Neo4jTermNode> lst_nodes, GraphClient client, int Level)
        {
            foreach(Neo4jTermNode termnode in lst_nodes)
            {
                IEnumerable<Neo4jTermNode> childrenNodes = getTermChildren(termnode.NodeID, client);
                if (childrenNodes!= null)
                {
                    foreach (Neo4jTermNode termNode in childrenNodes)
                    {
                        updateChildrenArrays(getTermChildren(termNode.NodeID, client), client, Level+1);
                    }
                } else 
                {

                }
                //get the array size using the per risk and number of buildings properties
                //getArraySize(rootname, client);
            }
        }

        private IEnumerable<Neo4jTermNode> getCoverChildren(string rootname, GraphClient client)
        {
            var results = client.Cypher.Match("(a{Name:'" + rootname + "'})-->(n)")
                           .Return((n) => new
                           {
                               Neo4jTermNode = n.As<Neo4jTermNode>()
                           }).Results;
            return (IEnumerable <Neo4jTermNode>)results;
        }
        private IEnumerable<Neo4jTermNode> getTermChildren(int NodeID, GraphClient client)
        {
            var results = client.Cypher.Match("(a{NodeID:" + NodeID + "})-->(n)")
                           .Return((n) => new
                           {
                               Neo4jTermNode = n.As<Neo4jTermNode>()
                           }).Results;
            return (IEnumerable<Neo4jTermNode>)results;
        }
        private void getArraySize(string rootname, GraphClient client)
        {
            var results = client.Cypher.Match("(a{Name:'"+rootname+"'})-->(n{Level:1,IsPerRisk:true})")
                            .Return((n) => new
                            {
                                Neo4jTermNode = n.As<Neo4jTermNode>() 
                            }).Results;
            int arraysize = results.Sum(v => Convert.ToInt32(v.Neo4jTermNode.NumberOfBuildings));
        }

        private Dictionary<int, int> getLevelDictionary(Dictionary<int, List<GraphNode>> LevelDictionary)
        {
            Dictionary<int, int> SubjectIdLevelMapping = new Dictionary<int, int>();

            foreach (int level in LevelDictionary.Keys)
            {
                List<GraphNode> graphNodes = LevelDictionary[level];
                foreach (GraphNode gn in graphNodes)
                {
                    SubjectIdLevelMapping.Add(gn.Subject.ID, level);
                }
            }

            return SubjectIdLevelMapping;
        }

        public static PartitionData Deserialize(string file)
        {
            PartitionData result = null;
            using (var wc = new WebClient().OpenRead(file))
            {
                try
                {
                    result = ProtoBuf.Serializer.Deserialize<PartitionData>(wc);
                }
                catch (ProtoBuf.ProtoException p)
                {
                    //Log.Fatal("Error Reading a protobuf file" + p.Message);
                    Console.WriteLine("Error Reading a protobuf file \"{0}\": {1}", file, p.Message);
                }
            }
            return result;
        }

        private GraphInfo GetGraph(GraphType type, ExposureDataAdaptor expData, GraphBuildCache GraphCache)
        {
            GraphBuilder builder = new GraphBuilder(GraphCache);
            return builder.MakeGraph(type, expData);
        }
    }


    class Neo4jNode
    {

    }

    class Neo4jTermNode : Neo4jNode
    {
        public Neo4jTermNode()
        {
        }

        //public Neo4jTermNode(int id)
        //{
        //    NodeID = id;
        //}

        //public Neo4jTermNode(TermNode termnode)
        //{
        //    NodeID = termnode.Subject.ID;
        //}

        public int NodeID { get; set; }
        public double Deductible { get; set; }
        public bool IsPerRisk { get; set; }
        public double Limit { get; set; }

        public int NumberOfBuildings { get; set; }

        public int Level { get; set; }
    }

    class Neo4jCoverNode : Neo4jNode
    {
        public Neo4jCoverNode()
        {
        }

        //public Neo4jCoverNode(string _coverName)
        //{
        //    Name = _coverName;
        //}

        //public Neo4jCoverNode(CoverNode coverNode)
        //{
        //    Name = coverNode.CoverName;
        //}
        public string Name { get; set; }

        public bool IsPerRisk { get; set; }

        public int Level { get; set; }
    }

    //class Neo4jNodeFactory
    //{
    //    public Neo4jNodeFactory()
    //    {

    //    }

    //    public Neo4jNode BuildNode(GraphNode refGraphNode)
    //    {
    //        if (refGraphNode is TermNode)
    //            return new Neo4jTermNode(refGraphNode as TermNode);
    //        else if (refGraphNode is CoverNode)
    //            return new Neo4jCoverNode(refGraphNode as CoverNode);
    //        else
    //            throw new NotSupportedException("Only can build nodes of type Term and Cover");
    //    }
    //}

    public class TermChildRelationship : Relationship, IRelationshipAllowingSourceNode<Neo4jTermNode>, IRelationshipAllowingTargetNode<Neo4jTermNode>
    {
        public static readonly string TypeKey = "Interaction";

        public TermChildRelationship(NodeReference targetNode)
            : base(targetNode)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }

    public class CoverChildRelationship : Relationship, IRelationshipAllowingSourceNode<Neo4jCoverNode>, IRelationshipAllowingTargetNode<Neo4jTermNode>
    {
        public static readonly string TypeKey = "Recoverable";

        public CoverChildRelationship(NodeReference targetNode)
            : base(targetNode)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }

    public class DerivedCoverChildRelationship : Relationship, IRelationshipAllowingSourceNode<Neo4jCoverNode>, IRelationshipAllowingTargetNode<Neo4jCoverNode>
    {
        public static readonly string TypeKey = "Payout";

        public DerivedCoverChildRelationship(NodeReference targetNode)
            : base(targetNode)
        { }

        public override string RelationshipTypeKey
        {
            get { return TypeKey; }
        }
    }

    class Level
    {
        public int[] PartitionArray { get; set; }
        public int[] DeductibleArray { get; set; }
        public int[] LimitArray { get; set; }
    }
}
