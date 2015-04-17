using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public abstract class Graph : IEquatable<Graph>
    {
        protected bool isOverlap;
        protected long contractID;
        public bool IsExecuted { get; set; }
        public bool IsOverlapped{ get { return isOverlap;}}
        public bool GraphReady { get; set; }

        public long ContractID { get { return contractID; } }

        public ReferenceResultOutput exResults { get; set; }
        public Declarations Declarations { get; protected set; }

        public virtual Dictionary<long, Declarations> DeclarationsForAssociatedContracts
        { get { return new Dictionary<long, Declarations> { { contractID, Declarations }}; } }   

        protected HashSet<CoverageAtomicRITE> atomicCoverageRITEs;
        public abstract HashSet<AtomicRITE> AtomicRites { get; }
        
        public LossTimeSeries PayoutTimeSeries
        {
            get
            {
                if (IsExecuted)
                    return exResults.PayoutTimeSeries;
                else
                    throw new InvalidOperationException("Cannot get payout from graph that has not been executed");
            }
        }

        public Graph(long ID)
        {
            IsExecuted = false;
            contractID = ID;
        }
        public Graph(long ID, Declarations _declarations)
        {
            IsExecuted = false;
            contractID = ID;
            Declarations = _declarations;
        }
        //public Graph(long ID, Dictionary<long, Declarations> _declarationsSet)
        //{
        //    IsExecuted = false;
        //    contractID = ID;
        //    Declarations = _declarationsSet[ID];
        //    DeclarationsSet = _declarationsSet;
        //}

        public abstract void Initialize();

        public abstract void EventReset();

        public abstract void PeriodReset();

        public abstract void HoursClauseWindowReset();

        public abstract void  SetAggStates(Dictionary<string, AggState> CoverAggregateStates);

        public abstract Dictionary<string, AggState> GetAggStates();

        public abstract bool CheckOverlapped();

        public abstract HashSet<CoverageAtomicRITE> GetCoverageRITEs();


        //IEquatable Overloads
        public bool Equals(Graph other)
        {
            if (other == null)
                return false;

            if (this.contractID == other.contractID)
                return true;
            else
                return false;


        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            Graph graphObj = obj as Graph;
            if (graphObj == null)
                return false;
            else
                return Equals(graphObj);
        }

        public override int GetHashCode()
        {
            return (int)this.contractID;
        }

        public override string ToString()
        {
            return this.contractID.ToString();
        }
    }

    public abstract class GraphOfNodes : Graph
    {  
        protected List<GraphNode> graphNodes;  //raintest, temporily make this to add treaty node manually
        protected Dictionary<GraphNode, List<GraphNode>> parentToChildrenMap;
            
        protected Dictionary<int, List<GraphNode>> _levelNodeDict;
        public Dictionary<int, List<GraphNode>> LevelNodeDict
        {
            get { return _levelNodeDict; }
        }

        public List<GraphNode> TopNodes{get; protected set;}
        
        public List<GraphNode> GraphNodes 
        {
            get { return graphNodes; }
        }
        public Dictionary<GraphNode, List<GraphNode>> ParentToChildrenMap
        {
            get { return parentToChildrenMap; }
        }
        

        public override HashSet<CoverageAtomicRITE> GetCoverageRITEs()
        { return atomicCoverageRITEs; } 


        //Create COL and Loss Type sets..
        //protected COLCollection EQ = new COLCollection(new HashSet<string>() { "EQ" });
        //protected COLCollection EQSH = new COLCollection(new HashSet<string>() { "EQSH" });
        //protected COLCollection EQFF = new COLCollection(new HashSet<string>() { "EQFF" });
        //protected COLCollection SHFF = new COLCollection(new HashSet<string>() { "EQSH", "EQFF" });
        //protected COLCollection EQSHFF = new COLCollection(new HashSet<string>() { "EQSHFF" });

        public GraphOfNodes(long ID):base(ID)
        {

        }

        public GraphOfNodes(long ID, List<GraphNode> _topNodes, List<GraphNode> _graphNodes, Dictionary<GraphNode, List<GraphNode>> _parentToChildrenMap, Declarations _declarations)
                        :base(ID, _declarations)
        {
            IsExecuted = false;
            contractID = ID;
            TopNodes = _topNodes;
            graphNodes = _graphNodes;
            parentToChildrenMap = _parentToChildrenMap;
        }

        public override void Initialize()
        {
            BuildAtomicRITEs();
            isOverlap = CheckOverlapped();
            if (isOverlap)
            {
                AssignLevelToNode();
            }
            GraphReady = true;
        }

        //public abstract List<GraphNode> GetChildrenForNode(GraphNode parentNode);
        
        public List<GraphNode> GetChildrenForNode(GraphNode parentNode)
        {
            if (GraphNodes.Contains(parentNode))
            {
                if (ParentToChildrenMap.ContainsKey(parentNode))
                {
                    return ParentToChildrenMap[parentNode];
                }
                else
                    return new List<GraphNode>();//throw new InvalidOperationException("Node does not have any children!");
            }
            else
                throw new InvalidOperationException("Graph does not contain node specified!");
        }

        public List<IAllocatable> GetIAChildrenForIAllocatable(IAllocatable parentIA)
        {
            GraphNode parentNode = parentIA as GraphNode;
            
            List<IAllocatable> nodeList = new List<IAllocatable>();

            if (GraphNodes.Contains(parentNode))
            {
                if (ParentToChildrenMap.ContainsKey(parentNode))
                {
                    
                    nodeList = ParentToChildrenMap[parentNode].Cast<IAllocatable>().ToList();
                }       
            }
            else
                throw new InvalidOperationException("Graph does not contain node specified!");

            //add  AtomicRITE
            List<IAllocatable> aRiteList = new List<IAllocatable>();

            foreach (AtomicRITE aR in parentNode.ResidualAtomicRITEs)
            {
                IAllocatable aIA = aR as IAllocatable;
                aRiteList.Add(aIA);
            }

            return nodeList.Union(aRiteList).ToList();            
        }
    
        public override void EventReset()
        {
            foreach (GraphNode node in graphNodes)
            {
                node.Reset();
            }

            foreach (AtomicRITE aRITE in AtomicRites)
            {
                aRITE.Reset();
            }

    
            IsExecuted = false;
            exResults = null;
            //PayoutTimeSeries = null;  just for testing 
        }

        public override void PeriodReset()
        {
            EventReset();
            foreach (GraphNode aNode in GraphNodes)
            {
                aNode.PeriodReset();
            }
        }

        public override void HoursClauseWindowReset()
        {
            foreach (GraphNode node in graphNodes)
            {
                node.Reset();
                node.PeriodReset();
            }
            exResults = null;
        }

        public override void  SetAggStates(Dictionary<string, AggState> CoverAggregateStates)
        {
            List<CoverNode> coverNodes = graphNodes.Where(node => node is CoverNode).Cast<CoverNode>().ToList();

            foreach (KeyValuePair<string, AggState> pair in CoverAggregateStates)
            {
                CoverNode NodeToUpdate = coverNodes.Where(node => node.CoverName == pair.Key).First();
                NodeToUpdate.CurrentAggState = new AggState(pair.Value);
            }     
        }

        public override Dictionary<string, AggState> GetAggStates()
        {
            Dictionary<string, AggState> CoverAggregateStates = new Dictionary<string, AggState>();

            List<CoverNode> coverNodes = graphNodes.Where(node => node is CoverNode).Cast<CoverNode>().ToList();

            foreach (CoverNode cNode in coverNodes)
            {
                CoverAggregateStates.Add(cNode.CoverName, new AggState(cNode.CurrentAggState));
            }

            return CoverAggregateStates;
        }

        public override bool CheckOverlapped()
        {    //overlap should be only mattered in Term tree
            //if overlap detected, then the overlap methodology should only apply to the term tree part
            foreach (GraphNode pNode in GraphNodes)
            {
                HashSet<AtomicRITE> aRite = new HashSet<AtomicRITE>();
                List<GraphNode> childrenNodes = new List<GraphNode>(GetChildrenForNode(pNode));

                if (NodesAreAll(childrenNodes) == 1)
                //if (childrenNodes[0] is TermNode) //all childrenNodes should be in the same type
                    //only consider overlap in TermNodes
                {
                    foreach (GraphNode childNode in GetChildrenForNode(pNode))
                    {
                        HashSet<AtomicRITE> SubjectARITEs = childNode.Subject.GetAtomicRites();
                        int oldCounter = aRite.Count;
                        aRite.UnionWith(SubjectARITEs);//this will remove duplicate
                        if (aRite.Count < oldCounter + SubjectARITEs.Count) //overlap detected
                        {
                            return true;
                        }
                    }
                }
            }
            return false;        
        }

        public int NodesAreAll(List<GraphNode> inputList)
        {
            int termNodeCount = 0;
            //return 0: empty list; 1: TermNode; 2: CoverNode; 3: mixed Node
            if (inputList.Count == 0)
                return 0;

            foreach (GraphNode node in inputList)
            {
                if (node is TermNode)
                    termNodeCount += 1;                  
            }

            if (termNodeCount == inputList.Count)
                return 1; //all TermNode
            else if (termNodeCount == 0)
                return 2; //all CoverNode 
            else
                return 3; //Null list                        
        }

        public void AssignLevelToNode()
        {
            //assign level 0 to the most-bottom CoverNodes which are directly connected to the TermNodes
            List<GraphNode> bottomCoverNodes = new List<GraphNode>();
            _levelNodeDict = new Dictionary<int, List<GraphNode>>();

            foreach (GraphNode gNode in GraphNodes)
            {
                if (gNode is CoverNode)
                {
                    List<GraphNode> childrenNodes = new List<GraphNode>(GetChildrenForNode(gNode));
                    if (NodesAreAll(childrenNodes) == 1) //all TermNodes
                        bottomCoverNodes.Add(gNode);
                    else if (NodesAreAll(childrenNodes) == 3) //mixed GraphNodes
                        throw new InvalidOperationException("Children Nodes must be the same type!");                
                }
            }

            _levelNodeDict.Add(0, bottomCoverNodes);
            
            int levelNum = 0;
            List<GraphNode> allChildrenList = new List<GraphNode>();

            foreach (GraphNode node in _levelNodeDict[levelNum])
            {
                allChildrenList.AddRange(GetChildrenForNode(node));
            }

            
            int childrenCount = allChildrenList.Count;

            while (childrenCount > 0)
            {
                levelNum += 1;
                
                List<GraphNode> levelList = new List<GraphNode>();
                foreach (GraphNode cNode in allChildrenList)
                {
                    foreach (KeyValuePair<int, List<GraphNode>> kvp in _levelNodeDict)
                    {
                        if (kvp.Value.Contains(cNode))
                            kvp.Value.Remove(cNode);
                    }
                    levelList.Add(cNode);
                }
                var levelListNoDup = levelList.Distinct().ToList();
                _levelNodeDict.Add(levelNum, levelListNoDup);

                allChildrenList = new List<GraphNode>();
                foreach (GraphNode node in _levelNodeDict[levelNum])
                {
                    allChildrenList.AddRange(GetChildrenForNode(node));
                }                
                childrenCount = allChildrenList.Count;                                                 
            }                                                    
        }

        public abstract void BuildAtomicRITEs();

        protected HashSet<AtomicRITE> RecursiveGetAtomicRITEs(GraphNode node, HashSet<AtomicRITE> currentRITESet)
        {
            if (node.AtomicRITEsAdded == true)
                return new HashSet<AtomicRITE>(node.AllAtomicRITEs.Cast<AtomicRITE>());

            //Get AtomicRites from Children
            HashSet<AtomicRITE> ARITEsFromChildren = new HashSet<AtomicRITE>();

            if (node.Subject.IsDerived)
            {
                foreach (GraphNode childNode in GetChildrenForNode(node))
                {
                    ARITEsFromChildren.UnionWith(RecursiveGetAtomicRITEs(childNode, currentRITESet));
                }

                node.ResidualAtomicRITEs = new HashSet<AtomicRITE>();
                node.AllAtomicRITEs = ARITEsFromChildren;
                node.AtomicRITEsAdded = true;

                return ARITEsFromChildren;
            }
            else
            {
                foreach (GraphNode childNode in GetChildrenForNode(node))
                {
                    ARITEsFromChildren.UnionWith(RecursiveGetAtomicRITEs(childNode, currentRITESet));
                }

                //Get Atomic Rites for subject of Node
                HashSet<AtomicRITE> SubjectARITEs = GetAtomicRiteForSubject(node.Subject, currentRITESet);
                node.ResidualAtomicRITEs = new HashSet<AtomicRITE>(SubjectARITEs.Except(ARITEsFromChildren));
                node.AllAtomicRITEs = new HashSet<AtomicRITE>(node.ResidualAtomicRITEs.Union(ARITEsFromChildren));
                node.AtomicRITEsAdded = true;
                currentRITESet.UnionWith(node.ResidualAtomicRITEs);

                return node.AllAtomicRITEs;
            }
        }

        private HashSet<AtomicRITE> GetAtomicRiteForSubject(Subject subject, HashSet<AtomicRITE> currentRITESet)
        {
            HashSet<AtomicRITE> SubjectARITEs = subject.GetAtomicRites();
            HashSet<AtomicRITE> CommonARITEs = new HashSet<AtomicRITE>(SubjectARITEs.Intersect(currentRITESet));

            foreach (AtomicRITE aRITE in CommonARITEs)
            {
                SubjectARITEs.Remove(aRITE);
                SubjectARITEs.Add(currentRITESet.Where(elem => elem.Equals(aRITE)).FirstOrDefault());
            }

            return SubjectARITEs;
        }

        public abstract LossTimeSeries GetNodeSubjectLoss(GraphNode node);
    }

    public class GraphOfMatrix: Graph
    {
        protected IExecutableMatrixGraph matrixGraph;
        public IExecutableMatrixGraph Graph { get { return matrixGraph; } }
        //public CoverNode TopCover { get; private set; }
        public override HashSet<AtomicRITE> AtomicRites { get { throw new NotImplementedException(); } }

        public GraphOfMatrix(long ID, IExecutableMatrixGraph _matrixGraph, Declarations _declarations)
            : base(ID, _declarations)
        {
            matrixGraph = _matrixGraph;
            //TopCover = _topCover;
        }

        public override void Initialize()
        {
            isOverlap = CheckOverlapped();
            GraphReady = true;
            atomicCoverageRITEs = new HashSet<CoverageAtomicRITE>();
        }

        public override void EventReset()
        {
            IsExecuted = false;
            exResults = null;
        }

        public override void PeriodReset() { }

        public override void HoursClauseWindowReset()
        { exResults = null; }

        public override void SetAggStates(Dictionary<string, AggState> CoverAggregateStates)
        {

        }

        public override Dictionary<string, AggState> GetAggStates()
        {
            throw new NotImplementedException("im so tired of this..");
        }

        public override bool CheckOverlapped()
        {
            return false;
        }

        public override HashSet<CoverageAtomicRITE> GetCoverageRITEs()
        {
            //Matrix graph is always primary for now..
            return atomicCoverageRITEs;
        }


    }
}
