using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class StepPolicyGraph : FixedPrimaryGraph
    {
        public StepPolicyGraph(ExposureDataAdaptor expData) : base(expData) { }

        public override void Initialize()
        {         
            //Create Schedules
            ScheduleOfRITEs S16_EQ = expdata.Schedules.ToList()[0];
            ScheduleOfRITEs S16_EQ_5229 =  expdata.Schedules.ToList()[1];
            ScheduleOfRITEs S16_EQ_5228 =  expdata.Schedules.ToList()[2];
            ScheduleOfRITEs S16_EQ_46 = expdata.Schedules.ToList()[3];
            ScheduleOfRITEs S16_EQ_47 =  expdata.Schedules.ToList()[4];
            ScheduleOfRITEs S16_EQ_48 = expdata.Schedules.ToList()[5];

            //Create Subjects
            PrimarySubject Node27BSub = new PrimarySubject(S16_EQ, Building, EQ);
            PrimarySubject Node27CSub = new PrimarySubject(S16_EQ, Contents, EQ);
            PrimarySubject Node27BISub = new PrimarySubject(S16_EQ, BI, EQ);

            PrimarySubject Node25Sub = new PrimarySubject(S16_EQ, Building_Contents, EQ);
            PrimarySubject Node26Sub = new PrimarySubject(S16_EQ, Contents_BI, EQ);
            PrimarySubject Node27Sub = new PrimarySubject(S16_EQ, Loss, EQ);

            PrimarySubject Node11Sub = new PrimarySubject(S16_EQ, Loss, EQ);


            //Create Nodes and Add to Node List          
            CoverNode Node27B = new CoverNode(Node27BSub, "L939_27_Building"); 
            graphNodes.Add(Node27B);

            CoverNode Node27C = new CoverNode(Node27CSub, "L939_27_Contents"); 
            graphNodes.Add(Node27C);

            CoverNode Node27BI = new CoverNode(Node27BISub, "L939_27_BI"); 
            graphNodes.Add(Node27BI);

            CoverNode Node27 = new CoverNode(Node27Sub, "L939_27"); 
            graphNodes.Add(Node27);

            CoverNode Node26 = new CoverNode(Node26Sub, "L938_26");
            graphNodes.Add(Node26);

            CoverNode Node25 = new CoverNode(Node25Sub, "L937_25"); 
            graphNodes.Add(Node25);


            CoverNode Node11 = new CoverNode(Node11Sub, "Blanket_EQ");  
            graphNodes.Add(Node11);

            //Build Parent to Child Mapping
            List<GraphNode> Node11Children = new List<GraphNode>() { Node25, Node26, Node27 };
            parentToChildrenMap.Add(Node11, Node11Children);

            List<GraphNode> Node27Children = new List<GraphNode>() { Node27B, Node27C, Node27BI };
            parentToChildrenMap.Add(Node27, Node27Children);

            AssignLevelToNode();

            TopNodes = new List<GraphNode>(){Node11};

            BuildAtomicRITEs();

            GraphReady = true;

        } //end of Initialize
    }
}
