using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class FixedTreaty1 : TreatyGraph
    {
        private ExposureDataAdaptor expdata;
        private GraphBuildCache cache;


        public FixedTreaty1(ExposureDataAdaptor _expData, GraphBuildCache _cache)
            : base(_expData.ContractID)
        {
            expdata = _expData;
            cache = _cache;
        }


        public override void Initialize()
        {
            _contractRITEs = new HashSet<ContractAtomicRITE>();
            //Build Graph here....
            graphNodes = new List<GraphNode>();
            parentToChildrenMap = new Dictionary<GraphNode, List<GraphNode>>();
            atomicCoverageRITEs = new HashSet<CoverageAtomicRITE>();

            //Add Contract Declarations
            Declarations = new Declarations();

            Graph overlapGraph;
            cache.GetContract(11331, out overlapGraph);
            Graph normalGraph;
            cache.GetContract(11324656, out normalGraph);

            ScheduleOfContracts schedule = new ScheduleOfContracts("PositionA_Gross", 
                                                                    new HashSet<Graph>() { overlapGraph, normalGraph });
            ReinsuranceSubject TreatyNodeSub = new ReinsuranceSubject(schedule, Loss, EQ);
            CoverNode TreatyNode = new CoverNode(TreatyNodeSub, "OccLim");
            //TreatyNode.SetCoverTerms(false, 100, 100000, 1);
            graphNodes.Add(TreatyNode);
            this.TopNodes = new List<GraphNode>(){TreatyNode};

            this.BuildAtomicRITEs();
        }

        protected COLCollection EQ = new COLCollection(new HashSet<string>() { "EQ" });
        protected ExposureTypeCollection Loss = new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.Building, ExposureType.Contents, ExposureType.BI });

    }
}
