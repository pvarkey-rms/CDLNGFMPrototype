using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class FixedGraphOverlapSubperil2 : FixedPrimaryGraph
    {
        public FixedGraphOverlapSubperil2(ExposureDataAdaptor expData) : base(expData) { }

        public override void Initialize()
        {
            //Create Schedules
            ScheduleOfRITEs S16_EQ = expdata.Schedules.ToList()[0];  //new Schedule("S16.EQ");
            ScheduleOfRITEs S16_EQ_5229 = expdata.Schedules.ToList()[1]; //new Schedule("S16.EQ.SubPolicy5229");
            ScheduleOfRITEs S16_EQ_5229_5228 = expdata.Schedules.ToList()[2]; //new Schedule("S16.EQ.SubPolicy5229.SubPolicy5228");
            ScheduleOfRITEs S16_EQ_5229_5228_46 = expdata.Schedules.ToList()[3]; //new Schedule("S16.EQ.SubPolicy5229.SubPolicy5228.46");
            ScheduleOfRITEs S16_EQ_5229_47 = expdata.Schedules.ToList()[4]; //new Schedule("S16.EQ.SubPolicy5229.47");
            ScheduleOfRITEs S16_EQ_48 = expdata.Schedules.ToList()[5]; //new Schedule("S16.EQ.48");

            //Create Subjects
            PrimarySubject Node53Sub = new PrimarySubject(S16_EQ_5229_5228_46, Building, EQ);
            PrimarySubject Node54Sub = new PrimarySubject(S16_EQ_5229_5228_46, Contents, EQ);
            PrimarySubject Node55Sub = new PrimarySubject(S16_EQ_5229_5228_46, BI, EQ);

            PrimarySubject Node56Sub = new PrimarySubject(S16_EQ_5229_47, Building, WS);
            PrimarySubject Node57Sub = new PrimarySubject(S16_EQ_5229_47, Contents, WS);
            PrimarySubject Node58Sub = new PrimarySubject(S16_EQ_5229_47, BI, WS);

            PrimarySubject Node59Sub = new PrimarySubject(S16_EQ_48, Building, EQWS);
            PrimarySubject Node60Sub = new PrimarySubject(S16_EQ_48, Contents, EQWS);
            PrimarySubject Node61Sub = new PrimarySubject(S16_EQ_48, BI, EQWS);

            PrimarySubject Node46Sub = new PrimarySubject(S16_EQ_5229_5228_46, Loss, EQ);
            PrimarySubject Node47Sub = new PrimarySubject(S16_EQ_5229_47, Loss, WS);
            PrimarySubject Node48Sub = new PrimarySubject(S16_EQ_48, Loss, EQWS);

            PrimarySubject NodeBSub = new PrimarySubject(S16_EQ, Building, EQ);
            PrimarySubject NodeCSub = new PrimarySubject(S16_EQ, Contents, WS);
            PrimarySubject NodeBISub = new PrimarySubject(S16_EQ, BI, EQWS);

            PrimarySubject Node21Sub = new PrimarySubject(S16_EQ, Loss, EQWS);

            //Nodes
            TermNode Node53 = new TermNode(Node53Sub); //loccvg
            graphNodes.Add(Node53);
            TermNode Node54 = new TermNode(Node54Sub); //loccvg
            graphNodes.Add(Node54);
            TermNode Node55 = new TermNode(Node55Sub); //loccvg
            graphNodes.Add(Node55);

            TermNode Node56 = new TermNode(Node56Sub); //loccvg
            graphNodes.Add(Node56);
            TermNode Node57 = new TermNode(Node57Sub); //loccvg
            graphNodes.Add(Node57);
            TermNode Node58 = new TermNode(Node58Sub); //loccvg
            graphNodes.Add(Node58);

            TermNode Node59 = new TermNode(Node59Sub); //loccvg
            graphNodes.Add(Node59);
            TermNode Node60 = new TermNode(Node60Sub); //loccvg
            graphNodes.Add(Node60);
            TermNode Node61 = new TermNode(Node61Sub); //loccvg
            graphNodes.Add(Node61);

            //loc term 
            TermNode Node46 = new TermNode(Node46Sub);  //loc
            graphNodes.Add(Node46);
            TermNode Node47 = new TermNode(Node47Sub);  //loc
            graphNodes.Add(Node47);
            TermNode Node48 = new TermNode(Node48Sub);  //loc
            graphNodes.Add(Node48);

            //policy coverage
            TermNode NodeB = new TermNode(NodeBSub);
            graphNodes.Add(NodeB);
            TermNode NodeC = new TermNode(NodeCSub);
            graphNodes.Add(NodeC);
            TermNode NodeBI = new TermNode(NodeBISub);
            graphNodes.Add(NodeBI);

            TermNode Node21 = new TermNode(Node21Sub);
            graphNodes.Add(Node21);

            //policy cover
            CoverNode Node11 = new CoverNode(Node21Sub, " L937_16");
            graphNodes.Add(Node11);

            //Build Parent to Child Mapping
            List<GraphNode> Node11Children = new List<GraphNode>() { Node21 };
            parentToChildrenMap.Add(Node11, Node11Children);

            List<GraphNode> Node21Children = new List<GraphNode>() { Node46, Node47, Node48, NodeB, NodeC, NodeBI };
            parentToChildrenMap.Add(Node21, Node21Children);

            //List<GraphNode> Node32Children = new List<GraphNode>() { Node46, Node31, Node47 };
            //_parentToChildrenMap.Add(Node32, Node32Children);

            //List<GraphNode> Node31Children = new List<GraphNode>() { Node46 };
            //_parentToChildrenMap.Add(Node31, Node31Children);

            List<GraphNode> Node46Children = new List<GraphNode>() { Node53, Node54, Node55 };
            parentToChildrenMap.Add(Node46, Node46Children);

            List<GraphNode> Node47Children = new List<GraphNode>() { Node56, Node57, Node58 };
            parentToChildrenMap.Add(Node47, Node47Children);

            List<GraphNode> Node48Children = new List<GraphNode>() { Node59, Node60, Node61 };
            parentToChildrenMap.Add(Node48, Node48Children);

            List<GraphNode> NodeBChildren = new List<GraphNode>() { Node53 };
            parentToChildrenMap.Add(NodeB, NodeBChildren);

            List<GraphNode> NodeCChildren = new List<GraphNode>() { Node57 };
            parentToChildrenMap.Add(NodeC, NodeCChildren);

            List<GraphNode> NodeBIChildren = new List<GraphNode>() { Node55, Node58, Node61 };
            parentToChildrenMap.Add(NodeBI, NodeBIChildren);

            TopNodes = new List<GraphNode>() {Node11};
            AssignLevelToNode();
            BuildAtomicRITEs();
            GraphReady = true;
        } //end of Initialize

    }
}

