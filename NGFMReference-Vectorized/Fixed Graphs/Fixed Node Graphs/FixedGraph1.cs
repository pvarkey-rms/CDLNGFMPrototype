using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class FixedGraph1 : FixedPrimaryGraph
    {
        public FixedGraph1(ExposureDataAdaptor expData) : base(expData) { }

        public override void Initialize()
        {
                         
           // //Create RITECharacteristics
           // RITCharacteristic RITChar18 = new RITCharacteristic(19419518, ExposureType.Building, 100000);
           // RITCharacteristic RITChar19 = new RITCharacteristic(19419519, ExposureType.Contents, 10000);
           // RITCharacteristic RITChar20 = new RITCharacteristic(19419520, ExposureType.Building, 100000);
           // RITCharacteristic RITChar21 = new RITCharacteristic(19419521, ExposureType.Contents, 10000);
           // _characteristics.Add(RITChar18);
           // _characteristics.Add(RITChar19);
           // _characteristics.Add(RITChar20);
           // _characteristics.Add(RITChar21);

           // //Create RITEs
           // RITE rite57 = new RITE(11324657, 1);
           // rite57.AddCharacteristic(RITChar18);
           // rite57.AddCharacteristic(RITChar19);
           // RITE rite58 = new RITE(11324658, 1);
           // rite58.AddCharacteristic(RITChar20);
           // rite58.AddCharacteristic(RITChar21);
           // _rites.Add(rite57);
           // _rites.Add(rite58);

           // //Create AtomicRITE
           //// AtomicRITE(string _subperil, ExposureType _expType, RITE _rite, long ID)
           // AtomicRITE aRite5718EQ = new AtomicRITE("EQ", ExposureType.Building, rite57, 19419518);
           // AtomicRITE aRite5718WS = new AtomicRITE("WS", ExposureType.Building, rite57, 19419518);
           // AtomicRITE aRite5719EQ = new AtomicRITE("EQ", ExposureType.Building, rite57, 19419519);
           // AtomicRITE aRite5719WS = new AtomicRITE("WS", ExposureType.Building, rite57, 19419519);
           // AtomicRITE aRite5820EQ = new AtomicRITE("EQ", ExposureType.Building, rite58, 19419520);
           // AtomicRITE aRite5820WS = new AtomicRITE("WS", ExposureType.Building, rite58, 19419520);
           // AtomicRITE aRite5821EQ = new AtomicRITE("EQ", ExposureType.Building, rite58, 19419521);
           // AtomicRITE aRite5821WS = new AtomicRITE("WS", ExposureType.Building, rite58, 19419521);

           // _atomicCoverageRITEs.Add(aRite5718EQ);
           // _atomicCoverageRITEs.Add(aRite5718WS);
           // _atomicCoverageRITEs.Add(aRite5719EQ);
           // _atomicCoverageRITEs.Add(aRite5719WS);
           // _atomicCoverageRITEs.Add(aRite5820EQ);
           // _atomicCoverageRITEs.Add(aRite5820WS);
           // _atomicCoverageRITEs.Add(aRite5821EQ);
           // _atomicCoverageRITEs.Add(aRite5821WS);

            //Create Schedules
            ScheduleOfRITEs S2729_EQ = expdata.Schedules.ToList()[0];
            ScheduleOfRITEs S2729_EQ_59491 = expdata.Schedules.ToList()[1];
            ScheduleOfRITEs S2729_EQ_59492 = expdata.Schedules.ToList()[2];

            //Create Subjects
            PrimarySubject Node41Sub = new PrimarySubject(S2729_EQ_59491, Building, EQ);
            PrimarySubject Node42Sub = new PrimarySubject(S2729_EQ_59491, Contents, EQ);

            PrimarySubject Node31Sub = new PrimarySubject(S2729_EQ_59491, Loss, EQ);                                                             
            PrimarySubject Node32Sub = new PrimarySubject(S2729_EQ_59492, Loss, EQ);

            PrimarySubject Node21Sub = new PrimarySubject(S2729_EQ, Loss, EQ);       

            ///////////// New Contructors for Node, for reading from JSON prototype///Sunny
            //Create Nodes and Add to Node List
            //loccvg terms
            //TermNode Node41 = new TermNode(Node41Sub, false, false, 8000000, 10000000); //loccvg
            TermNode Node41 = new TermNode(Node41Sub); //loccvg
            graphNodes.Add(Node41);
            TermNode Node42 = new TermNode(Node42Sub);  //loccvg
            graphNodes.Add(Node42);

            //loc terms
            TermNode Node31 = new TermNode(Node31Sub); //loc
            graphNodes.Add(Node31);
            TermNode Node32 = new TermNode(Node32Sub); //loc
            graphNodes.Add(Node32);

            //policy term
            TermNode Node21 = new TermNode(Node21Sub);  //policy term, no limit term, so limit = 0?
            graphNodes.Add(Node21);

            //policy cover
            CoverNode Node11 = new CoverNode(Node21Sub, "L102_3222");  //policy layer
            graphNodes.Add(Node11);

            //Build Parent to Child Mapping
            List<GraphNode> Node11Children = new List<GraphNode>() { Node21 };
            parentToChildrenMap.Add(Node11, Node11Children);

            List<GraphNode> Node21Children = new List<GraphNode>() { Node31, Node32 };
            parentToChildrenMap.Add(Node21, Node21Children);

            List<GraphNode> Node31Children = new List<GraphNode>() { Node41, Node42 };
            parentToChildrenMap.Add(Node31, Node31Children);

            AssignLevelToNode();

            TopNodes = new List<GraphNode>(){Node11};

            BuildAtomicRITEs();

            GraphReady = true;

        } //end of Initialize

    }
}
