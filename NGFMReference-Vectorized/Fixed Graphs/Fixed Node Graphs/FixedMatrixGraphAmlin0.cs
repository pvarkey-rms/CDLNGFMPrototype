using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    //public class FixedGraph1 : FixedPrimaryGraph
    //{
    //    public FixedGraph1(ExposureDataAdaptor expData) : base(expData) { }

    //    public override void Initialize()
    //    {

    //        //Create RITECharacteristics
    //        int n = 1136+4819;   //number of RITEs
    //        RITCharacteristic[] RITCharArr = new RITCharacteristic[n];
    //        RITE[] RITEArr = new RITE[n];
    //        AtomicRITE[] atomicRITEArr = new AtomicRITE[n];
    //        ScheduleOfRITEs[] scheduleArr = new ScheduleOfRITEs[n];
    //        PrimarySubject[] subjectArr = new PrimarySubject[n];
    //        TermNode[] termNodeArr = new TermNode[n];
    //        ScheduleOfRITEs coverSchedule = new ScheduleOfRITEs("S151.FL");

    //        // AtomicRITE aRite5718EQ = new AtomicRITE("EQ", ExposureType.Building, rite57, 19419518);

    //        for (int i = 0; i < n; i++)
    //        {
    //            RITCharArr[i] = new RITCharacteristic(i+1, ExposureType.Building, 1000000);
    //            RITEArr[i] = new RITE(i+1, 1);  //1 = number of building
    //            RITEArr[i].AddCharacteristic(RITCharArr[i]);
    //            //AtomicRITEArr[i] = new AtomicRITE("EQ", ExposureType.Building, RITEArr[i], (long)(i+1)); 
    //            scheduleArr[i] = new ScheduleOfRITEs("schedule" + i);
    //            scheduleArr[i].AddItem(RITEArr[i]);
    //            coverSchedule.AddItem(RITEArr[i]);

    //            subjectArr[i] = new PrimarySubject(scheduleArr[i], Building, EQ);
    //            termNodeArr[i] = new TermNode(subjectArr[i]);
    //            graphNodes.Add(termNodeArr[i]);

    //            //_characteristics.Add(RITCharArr[i]);
    //        }

    //        PrimarySubject Node11Sub = new PrimarySubject(coverSchedule, Building, EQ);
    //        CoverNode Node11 = new CoverNode(Node11Sub, "L2012RGAUWPO4_150");
    //        graphNodes.Add(Node11);

    //        //Build Parent to Child Mapping
    //        List<GraphNode> Node11Children = new List<GraphNode>() { Node21 };
    //        parentToChildrenMap.Add(Node11, Node11Children);

    //        List<GraphNode> Node21Children = new List<GraphNode>() { Node31, Node32 };
    //        parentToChildrenMap.Add(Node21, Node21Children);

    //        List<GraphNode> Node31Children = new List<GraphNode>() { Node41, Node42 };
    //        parentToChildrenMap.Add(Node31, Node31Children);

    //        AssignLevelToNode();

    //        TopNodes = new List<GraphNode>() { Node11 };

    //        BuildAtomicRITEs();

    //        GraphReady = true;

    //    } //end of Initialize

    //}
}

