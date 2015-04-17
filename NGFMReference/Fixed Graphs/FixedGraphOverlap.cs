using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class FixedGraphOverlap : FixedPrimaryGraph
    {
        public FixedGraphOverlap(ExposureDataAdaptor expData) : base(expData) { }

        public override void Initialize()
        {
            ////Build Graph here....
            //_graphNodes = new List<GraphNode>();
            //_parentToChildrenMap = new Dictionary<GraphNode, List<GraphNode>>();
            //_rites = new List<RITE>();
            //_schedules = new List<Schedule>();
            //_characteristics = new List<RITCharacteristic>();
            //_atomicCoverageRITEs = new HashSet<AtomicRITE>();

            ////Add Contract Declarations
            //Declarations = new Declarations();

            ////Create RITECharacteristics
            ////Rite 11334
            //RITCharacteristic RITChar53 = new RITCharacteristic(3253, ExposureType.Building, 1000000);
            //RITCharacteristic RITChar54 = new RITCharacteristic(3254, ExposureType.Contents, 500000);
            //RITCharacteristic RITChar55 = new RITCharacteristic(3255, ExposureType.BI, 100000);

            ////Rite 11332
            //RITCharacteristic RITChar56 = new RITCharacteristic(3256, ExposureType.Building, 2000000);
            //RITCharacteristic RITChar57 = new RITCharacteristic(3257, ExposureType.Contents, 1000000);
            //RITCharacteristic RITChar58 = new RITCharacteristic(3258, ExposureType.BI, 500000);

            ////Rite 11333
            //RITCharacteristic RITChar59 = new RITCharacteristic(3259, ExposureType.Building, 500000);
            //RITCharacteristic RITChar60 = new RITCharacteristic(3260, ExposureType.Contents, 500000);
            //RITCharacteristic RITChar61 = new RITCharacteristic(3261, ExposureType.BI, 50000);

            //_characteristics.Add(RITChar53);
            //_characteristics.Add(RITChar54);
            //_characteristics.Add(RITChar55);
            //_characteristics.Add(RITChar56);
            //_characteristics.Add(RITChar57);
            //_characteristics.Add(RITChar58);                   
            //_characteristics.Add(RITChar59);
            //_characteristics.Add(RITChar60);
            //_characteristics.Add(RITChar61);
          
            ////Create RITEs
            //RITE rite34 = new RITE(11334, 1);
            //rite34.AddCharacteristic(RITChar53);
            //rite34.AddCharacteristic(RITChar54);
            //rite34.AddCharacteristic(RITChar55);
            
            //RITE rite32 = new RITE(11332, 1);
            //rite32.AddCharacteristic(RITChar56);
            //rite32.AddCharacteristic(RITChar57);
            //rite32.AddCharacteristic(RITChar58);

            //RITE rite33 = new RITE(11333, 1);
            //rite33.AddCharacteristic(RITChar59);
            //rite33.AddCharacteristic(RITChar60);
            //rite33.AddCharacteristic(RITChar61);
            
            //_rites.Add(rite32);
            //_rites.Add(rite33);
            //_rites.Add(rite34);

            ////Create AtomicRITE
            //// AtomicRITE(string _subperil, ExposureType _expType, RITE _rite, long ID)
            //AtomicRITE aRite3453EQ = new AtomicRITE("EQ", ExposureType.Building, rite34, 11334);
            //AtomicRITE aRite3454EQ = new AtomicRITE("EQ", ExposureType.Contents, rite34, 11334);
            //AtomicRITE aRite3455EQ = new AtomicRITE("EQ", ExposureType.BI, rite34, 11334);

            //AtomicRITE aRite3256EQ = new AtomicRITE("EQ", ExposureType.Building, rite32, 11332);
            //AtomicRITE aRite3257EQ = new AtomicRITE("EQ", ExposureType.Contents, rite32, 11332);
            //AtomicRITE aRite3258EQ = new AtomicRITE("EQ", ExposureType.BI, rite32, 11332);

            //AtomicRITE aRite3359EQ = new AtomicRITE("EQ", ExposureType.Building, rite33, 11333);
            //AtomicRITE aRite3360EQ = new AtomicRITE("EQ", ExposureType.Contents, rite33, 11333);
            //AtomicRITE aRite3361EQ = new AtomicRITE("EQ", ExposureType.BI, rite33, 11333);

            //_atomicCoverageRITEs.Add(aRite3453EQ);
            //_atomicCoverageRITEs.Add(aRite3454EQ);
            //_atomicCoverageRITEs.Add(aRite3455EQ);
            //_atomicCoverageRITEs.Add(aRite3256EQ);
            //_atomicCoverageRITEs.Add(aRite3257EQ);
            //_atomicCoverageRITEs.Add(aRite3258EQ);
            //_atomicCoverageRITEs.Add(aRite3359EQ);
            //_atomicCoverageRITEs.Add(aRite3360EQ);
            //_atomicCoverageRITEs.Add(aRite3361EQ);

            ////Create Schedules
            //Schedule S16_EQ = new Schedule("S16.EQ");
            //S16_EQ.AddRITE(rite32);
            //S16_EQ.AddRITE(rite33);
            //S16_EQ.AddRITE(rite34);
            //_schedules.Add(S16_EQ);

            //Schedule S16_EQ_5229 = new Schedule("S16.EQ.SubPolicy5229");
            //S16_EQ_5229.AddRITE(rite34);
            //S16_EQ_5229.AddRITE(rite32);
            //_schedules.Add(S16_EQ_5229);
            
            //Schedule S16_EQ_5229_5228 = new Schedule("S16.EQ.SubPolicy5229.SubPolicy5228");
            //S16_EQ_5229_5228.AddRITE(rite34);
            //_schedules.Add(S16_EQ_5229_5228);

            //Schedule S16_EQ_5229_5228_46 = new Schedule("S16.EQ.SubPolicy5229.SubPolicy5228.46");
            //S16_EQ_5229_5228.AddRITE(rite34);
            //_schedules.Add(S16_EQ_5229_5228_46);

            //Schedule S16_EQ_5229_47 = new Schedule("S16.EQ.SubPolicy5229.47");
            //S16_EQ_5229_5228.AddRITE(rite32);
            //_schedules.Add(S16_EQ_5229_47);

            //Schedule S16_EQ_48 = new Schedule("S16.EQ.48");
            //S16_EQ_48.AddRITE(rite33);
            //_schedules.Add(S16_EQ_48);

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

            PrimarySubject Node56Sub = new PrimarySubject(S16_EQ_5229_47, Building, EQ);
            PrimarySubject Node57Sub = new PrimarySubject(S16_EQ_5229_47, Contents, EQ);
            PrimarySubject Node58Sub = new PrimarySubject(S16_EQ_5229_47, BI, EQ);

            PrimarySubject Node59Sub = new PrimarySubject(S16_EQ_48, Building, EQ);
            PrimarySubject Node60Sub = new PrimarySubject(S16_EQ_48, Contents, EQ);
            PrimarySubject Node61Sub = new PrimarySubject(S16_EQ_48, BI, EQ);

            PrimarySubject Node46Sub = new PrimarySubject(S16_EQ_5229_5228_46, Loss, EQ);
            PrimarySubject Node47Sub = new PrimarySubject(S16_EQ_5229_47, Loss, EQ);
            PrimarySubject Node48Sub = new PrimarySubject(S16_EQ_48, Loss, EQ);

            PrimarySubject NodeBSub = new PrimarySubject(S16_EQ, Building, EQ);
            PrimarySubject NodeCSub = new PrimarySubject(S16_EQ, Contents, EQ);
            int ii = 4;
            PrimarySubject NodeBISub = new PrimarySubject(S16_EQ, BI, EQ);

            PrimarySubject Node21Sub = new PrimarySubject(S16_EQ, Loss, EQ);

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
            CoverNode Node11 = new CoverNode(Node21Sub, "L938_365");  
            graphNodes.Add(Node11);

            //Build Parent to Child Mapping
            List<GraphNode> Node11Children = new List<GraphNode>() { Node21 };
            parentToChildrenMap.Add(Node11, Node11Children);

            List<GraphNode> Node21Children = new List<GraphNode>() { Node46, Node47, Node48, NodeB, NodeC, NodeBI};
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

            List<GraphNode> NodeBChildren = new List<GraphNode>() { Node53, Node56, Node59 };
            parentToChildrenMap.Add(NodeB, NodeBChildren);

            List<GraphNode> NodeCChildren = new List<GraphNode>() { Node54, Node57, Node60 };
            parentToChildrenMap.Add(NodeC, NodeCChildren);

            List<GraphNode> NodeBIChildren = new List<GraphNode>() { Node55, Node58, Node61 };
            parentToChildrenMap.Add(NodeBI, NodeBIChildren);

            TopNodes = new List<GraphNode>(){Node11};
            AssignLevelToNode();
            BuildAtomicRITEs();            
            GraphReady = true;
        } //end of Initialize

    }
}

