using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class TreatyGraph : Graph
    {
        public TreatyGraph(long ID):base(ID)
        {
           
        }

        public TreatyGraph(long ID, List<GraphNode> _topNodes, List<GraphNode> _graphNodes, Dictionary<GraphNode, List<GraphNode>> _parentToChildrenMap, Declarations _declarations)
            :base(ID, _topNodes, _graphNodes, _parentToChildrenMap, _declarations)
        {

        }

        protected HashSet<ContractAtomicRITE> _contractRITEs;

        public HashSet<ContractAtomicRITE> ContractRITEs
        { get { return _contractRITEs; } }

        public override HashSet<AtomicRITE> AtomicRites
        {
            get
            {
                return new HashSet<AtomicRITE>(_contractRITEs.Cast<AtomicRITE>());
            }
        }

        public override void BuildAtomicRITEs()
        {
            _contractRITEs = new HashSet<ContractAtomicRITE>();
            HashSet<AtomicRITE> currentRITESet = new HashSet<AtomicRITE>();

            foreach (CoverNode coverNode in TopNodes.OfType<CoverNode>())
            {
                _contractRITEs.UnionWith(RecursiveGetAtomicRITEs(coverNode, currentRITESet).Cast<ContractAtomicRITE>());
            }
        }

        public override HashSet<CoverageAtomicRITE> GetCoverageRITEs()
        {
            HashSet<CoverageAtomicRITE> coverageRITEs = new HashSet<CoverageAtomicRITE>();
            foreach (ContractAtomicRITE contractRITE in _contractRITEs)
            {
                coverageRITEs.UnionWith(contractRITE.contractGraph.GetCoverageRITEs());
            }

            return coverageRITEs;
        }

        //public TreatyGraph(List<Graph> _primaryGraphs)
        //{ 
         //   PrimaryGraphs = _primaryGraphs;  
        //}
        //public int HoursClause { get; set; }

        //public DateTime? MinTimeStamp
        //{
        //    get
        //    {
        //        return PrimaryGraphs.Min(item => item.TopNode.TimeStamp);
        //    }

        //}

        //public DateTime? MaxTimeStamp
        //{
        //    get 
        //    {
        //        return PrimaryGraphs.Min(item => item.TopNode.TimeStamp);
        //    }        
        //}

        //public List<Tuple<DateTime?, DateTime?>> GetTimeWindows()
        //{
        //    List<Tuple<DateTime?, DateTime?>> tWindowList = new List<Tuple<DateTime?, DateTime?>>();

        //    TimeSpan t = new TimeSpan(0, HoursClause, 0, 0);
        //    DateTime? startDate = MinTimeStamp;
             
        //    while (startDate < MaxTimeStamp)
        //    {
        //        DateTime? endDate = startDate + t;
        //        tWindowList.Add(new Tuple<DateTime?, DateTime?>(startDate, endDate));
        //        startDate = endDate;
        //    }
        //    return tWindowList;
        //}

        public override void Initialize()
        {
            throw new NotImplementedException();          
        }

        public void FormTreatyGraph()
        {
            ////Build Graph here....
            //_graphNodes = new List<GraphNode>();
            //_parentToChildrenMap = new Dictionary<GraphNode, List<GraphNode>>();
            ////_rites = new List<RITE>();
            ////_schedules = new List<Schedule>();
            ////_characteristics = new List<RITCharacteristic>();

            //foreach (Graph aGraph in PrimaryGraphs)
            //{
            //    _graphNodes.Add(aGraph.TopNode);
            //    //_rites.AddRange(aGraph.ContractRITES);
            //    //_schedules.AddRange(aGraph.Schedules);
            //    //_characteristics.AddRange(aGraph.Characteristics);            
            //}
        
            //Subject Node00Sub = new Subject(null); 
            ////childrenCoverList is null for now

            //CoverNode Node00 = new CoverNode(Node00Sub);
            //_graphNodes.Add(Node00);
             
            ////Build Parent to Child Mapping
            //List<GraphNode> Node00Children = new List<GraphNode>();
            //foreach (Graph aGraph in PrimaryGraphs)
            //{
            //    Node00Children.Add(aGraph.TopNode);
            //    _graphNodes.Add(aGraph.TopNode);
            //}            
            //_parentToChildrenMap.Add(Node00, Node00Children);
         
            ////add TopNode treaty term manually for now           
            //Node00.SetFinTerms(false, 500.0, 1000000.0, 100);
            //HoursClause = 72;

            //TopNode = Node00;
            //GraphReady = true;
        } //end of Initialize

        public override LossTimeSeries GetNodeSubjectLoss(GraphNode node)
        {
            if (node.SubjectLoss != null)
                return node.SubjectLoss;

            LossTimeSeries subjectLoss = new LossTimeSeries(1);
            //Recursively gets subject loss for a Node
            foreach (GraphNode childnode in GetChildrenForNode(node))
            {
                subjectLoss.MergeTimeSeries(GetNodeSubjectLoss(childnode), Aggregation.Summed);
            }

            foreach (AtomicRITE rite in node.ResidualAtomicRITEs)
            {
                subjectLoss.MergeTimeSeries(rite.OriginalSubjectLoss, Aggregation.Summed);
            }

            return subjectLoss;

        }
       
    }
}

