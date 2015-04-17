using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class PrimaryGraph:Graph
    {
        public PrimaryGraph(long ID):base(ID)
        {

        }

        public PrimaryGraph(long ID, List<GraphNode> _topNodes, List<GraphNode> _graphNodes, Dictionary<GraphNode, List<GraphNode>> _parentToChildrenMap, Declarations _declarations)
            :base(ID, _topNodes, _graphNodes, _parentToChildrenMap, _declarations)
        {

        }
        
        public override HashSet<AtomicRITE> AtomicRites
        {
            get
            {
                return new HashSet<AtomicRITE>(atomicCoverageRITEs.Cast<AtomicRITE>());
            }
        }

        //public override void Initialize()
        //{
        //    throw new NotImplementedException();
        //}

        public override void BuildAtomicRITEs()
        {
            atomicCoverageRITEs = new HashSet<CoverageAtomicRITE>();
            HashSet<AtomicRITE> currentRITESet = new HashSet<AtomicRITE>();
            foreach (CoverNode coverNode in TopNodes.OfType<CoverNode>())
            {
                atomicCoverageRITEs.UnionWith(RecursiveGetAtomicRITEs(coverNode, currentRITESet).Cast<CoverageAtomicRITE>());
            }
        }

        //public override void BuildAtomicRITEs()
        //{
        //    _atomicCoverageRITEs = RecursiveGetAtomicRITEs(TopNode);
        //}

        //private HashSet<CoverageAtomicRITE> RecursiveGetAtomicRITEs(GraphNode node)
        //{
        //    if (node.AtomicRITEsAdded == true)
        //        return new HashSet<CoverageAtomicRITE>(node.AllAtomicRITEs.Cast<CoverageAtomicRITE>());

        //    //Get AtomicRites from Children
        //    HashSet<CoverageAtomicRITE> ARITEsFromChildren = new HashSet<CoverageAtomicRITE>();

        //    if (node.Subject.IsDerived)
        //    {
        //        foreach (GraphNode childNode in GetChildrenForNode(node))
        //        {
        //            ARITEsFromChildren.UnionWith(RecursiveGetAtomicRITEs(childNode));
        //        }

        //        node.ResidualAtomicRITEs = new HashSet<AtomicRITE>();
        //        node.AllAtomicRITEs = new HashSet<AtomicRITE>(ARITEsFromChildren.Cast<AtomicRITE>().ToList());
        //        node.AtomicRITEsAdded = true;

        //        return ARITEsFromChildren;
        //    }
        //    else
        //    {
        //        foreach (GraphNode childNode in GetChildrenForNode(node))
        //        {
        //            ARITEsFromChildren.UnionWith(RecursiveGetAtomicRITEs(childNode));
        //        }

        //        //Get Atomic Rites for subject of Node
        //        HashSet<CoverageAtomicRITE> SubjectARITEs = node.Subject.GetAtomicRites();
        //        //var ResidualARITEs = SubjectARITEs.Except(ARITEsFromChildren);
        //        HashSet<CoverageAtomicRITE> ResidualARITEs =  new HashSet<CoverageAtomicRITE>(SubjectARITEs.Except(ARITEsFromChildren));
        //        HashSet<CoverageAtomicRITE> AllARITEs = new HashSet<CoverageAtomicRITE>(ResidualARITEs.Union(ARITEsFromChildren));
                
        //        node.ResidualAtomicRITEs = new HashSet<AtomicRITE>(ResidualARITEs.Cast<AtomicRITE>());
        //        node.AllAtomicRITEs = new HashSet<AtomicRITE>(AllARITEs.Cast<AtomicRITE>());
        //        node.AtomicRITEsAdded = true;

        //        return AllARITEs;
        //    }
        //}       

        public override LossTimeSeries GetNodeSubjectLoss(GraphNode node)
        {
            if (node.SubjectLoss != null)
                return node.SubjectLoss;

            PrimarySubject priSub = (PrimarySubject)node.Subject;
            Aggregation aggType = priSub.Schedule.IsLocation ? Aggregation.PerBuilding : Aggregation.Summed;
            LossTimeSeries subjectLoss = new LossTimeSeries(priSub.Schedule.ActNumOfBldgs);
            //Recursively gets subject loss for a Node

            foreach (AtomicRITE rite in node.AllAtomicRITEs)
            {
                subjectLoss.MergeTimeSeries(rite.SubjectLoss, aggType);
            }

            return subjectLoss;
        }

    }

    public class FixedPrimaryGraph : PrimaryGraph
    {
        protected ExposureDataAdaptor expdata;
        private static long fixedGraphCount = 0;

        public FixedPrimaryGraph(ExposureDataAdaptor _expdata)
            : base(_expdata.ContractID)
        {
            expdata = _expdata;

            //Build Graph here....
            graphNodes = new List<GraphNode>();
            parentToChildrenMap = new Dictionary<GraphNode, List<GraphNode>>();
            atomicCoverageRITEs = new HashSet<CoverageAtomicRITE>();

            //Add Contract Declarations
            Declarations = new Declarations();  
        }

        protected COLCollection EQ = new COLCollection(new HashSet<string>() { "EQ" });
        protected COLCollection WS = new COLCollection(new HashSet<string>() { "WS" });
        protected COLCollection EQWS = new COLCollection(new HashSet<string>() { "EQ", "WS" });

        protected ExposureTypeCollection Building = new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.Building });
        protected ExposureTypeCollection Contents = new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.Contents });
        protected ExposureTypeCollection BI = new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.BI });
        protected ExposureTypeCollection Building_Contents = new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.Building, ExposureType.Contents });
        protected ExposureTypeCollection Contents_BI = new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.Contents, ExposureType.BI });
        protected ExposureTypeCollection Building_BI = new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.Building, ExposureType.BI });
        protected ExposureTypeCollection Loss = new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.Building, ExposureType.Contents, ExposureType.BI });

    }
}
