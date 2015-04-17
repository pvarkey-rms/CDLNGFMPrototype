using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class FixedGraph3 : FixedPrimaryGraph
    {
        public FixedGraph3(ExposureDataAdaptor expData) : base(expData) { }

        public override void Initialize()
        {
            ////Build Graph here....
            //_graphNodes = new List<GraphNode>();
            //_parentToChildrenMap = new Dictionary<GraphNode, List<GraphNode>>();
            //_rites = new List<RITE>();
            //_schedules = new List<Schedule>();
            //_characteristics = new List<RITCharacteristic>();

            ////Add Contract Declarations
            //Declarations = new Declarations();

            ////Create RITECharacteristics
            //RITCharacteristic RITChar18 = new RITCharacteristic(19419518, ExposureType.Building, 100000);
            //RITCharacteristic RITChar19 = new RITCharacteristic(19419519, ExposureType.Contents, 10000);
            //RITCharacteristic RITChar20 = new RITCharacteristic(19419520, ExposureType.Building, 100000);
            //RITCharacteristic RITChar21 = new RITCharacteristic(19419521, ExposureType.Contents, 10000);
            //_characteristics.Add(RITChar18);
            //_characteristics.Add(RITChar19);
            //_characteristics.Add(RITChar20);
            //_characteristics.Add(RITChar21);

            ////Create RITEs
            //RITE rite57 = new RITE(11324657, 1);
            //rite57.AddCharacteristic(RITChar18);
            //rite57.AddCharacteristic(RITChar19);
            //RITE rite58 = new RITE(11324658, 1);
            //rite58.AddCharacteristic(RITChar20);
            //rite58.AddCharacteristic(RITChar21);
            //_rites.Add(rite57);
            //_rites.Add(rite58);

            ////Create Schedules
            //Schedule S2729_EQ = new Schedule("S2729.EQ");
            //S2729_EQ.AddRITE(rite57);
            //S2729_EQ.AddRITE(rite58);
            //_schedules.Add(S2729_EQ);

            //Schedule S2729_EQ_59491 = new Schedule("S2729.EQ.59491");
            //S2729_EQ_59491.AddRITE(rite57);
            //_schedules.Add(S2729_EQ_59491);

            //Schedule S2729_EQ_59492 = new Schedule("S2729.EQ.59492");
            //S2729_EQ_59492.AddRITE(rite58);
            //_schedules.Add(S2729_EQ_59492);

            //Create Schedules
            ScheduleOfRITEs S2729_EQ = expdata.Schedules.ToList()[0];
            ScheduleOfRITEs S2729_EQ_59491 = expdata.Schedules.ToList()[1];
            ScheduleOfRITEs S2729_EQ_59492 = expdata.Schedules.ToList()[2];

            //Create Subjects
            PrimarySubject Node41Sub = new PrimarySubject(S2729_EQ_59491, Building, EQWS);
            PrimarySubject Node42Sub = new PrimarySubject(S2729_EQ_59491, Contents, EQWS);

            PrimarySubject Node31Sub = new PrimarySubject(S2729_EQ_59491, Loss, EQWS);
            PrimarySubject Node32Sub = new PrimarySubject(S2729_EQ_59492, Loss, EQWS);

            PrimarySubject Node21Sub = new PrimarySubject(S2729_EQ, Loss, EQWS);

            PrimarySubject Node01Sub = new PrimarySubject(null, null, null);
            Node01Sub.IsDerived = true;
            List<String> temp = new List<String>();
            temp.Add("Node11");
            Node01Sub.ChildrenCoverNodeList = temp;

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

            //treaty layer
            CoverNode Node01 = new CoverNode(Node01Sub, "OccLim");
           //_graphNodes.Add(Node01);

            //Build Parent to Child Mapping
            List<GraphNode> Node01Children = new List<GraphNode>() { Node11 };
            parentToChildrenMap.Add(Node01, Node01Children);

            List<GraphNode> Node11Children = new List<GraphNode>() { Node21 };
            parentToChildrenMap.Add(Node11, Node11Children);

            List<GraphNode> Node21Children = new List<GraphNode>() { Node31, Node32 };
            parentToChildrenMap.Add(Node21, Node21Children);

            List<GraphNode> Node31Children = new List<GraphNode>() { Node41, Node42 };
            parentToChildrenMap.Add(Node31, Node31Children);

            //raintest, add TopNode treaty term manually for now   
            Cover topCover = new Cover("TreatyCover", false, new MonetaryValue(600.0), new MonetaryValue(1000000.0), new PercentValue(100), false, TimeBasis.Occurrence, TimeBasis.Occurrence, TermValueType.Numeric);
            Node01.Cover = topCover;
            //end of hack

            TopNodes = new List<GraphNode>(){Node01};
            GraphReady = true;
        } //end of Initialize

    }
}
