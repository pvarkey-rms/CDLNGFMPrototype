using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public abstract class PrimaGraph : Graph
    {
        /*
        public List<GraphNode> _graphNodes;  //raintest, temporily make this to add treaty node manually
        protected Dictionary<GraphNode, List<GraphNode>> _parentToChildrenMap;
        protected List<RITCharacteristic> _characteristics;
        protected List<RITE> _rites;
        protected List<Schedule> _schedules;
        protected List<AtomicRITE> _atomicRITEs;

        public Declarations Declarations { get; protected set; }
        public GraphNode TopNode { get; protected set; }
        public bool GraphReady { get; set; }
        

        //public List<RITE> ContractRITES
        //{ get { return _rites; } }
        //public List<Schedule> Schedules
        //{ get { return _schedules; } }
        //public List<RITCharacteristic> Characteristics
        //{ get { return _characteristics; } }
 

        //Create COL and Loss Type sets..
        //protected COLCollection EQ = new COLCollection(new HashSet<string>() { "EQ" });
        //protected COLCollection EQSH = new COLCollection(new HashSet<string>() { "EQSH" });
        //protected COLCollection EQFF = new COLCollection(new HashSet<string>() { "EQFF" });
        //protected COLCollection SHFF = new COLCollection(new HashSet<string>() { "EQSH", "EQFF" });
        //protected COLCollection EQSHFF = new COLCollection(new HashSet<string>() { "EQSHFF" });

        protected COLCollection EQ = new COLCollection(new HashSet<string>() { "EQ" });
        protected COLCollection WS = new COLCollection(new HashSet<string>() { "WS" });
        protected COLCollection EQWS = new COLCollection(new HashSet<string>() { "EQ", "WS" });

        protected HashSet<ExposureType> Building = new HashSet<ExposureType>() { ExposureType.Building };
        protected HashSet<ExposureType> Contents = new HashSet<ExposureType>() { ExposureType.Contents };
        protected HashSet<ExposureType> BI = new HashSet<ExposureType>() { ExposureType.BI };
        protected HashSet<ExposureType> Building_Contents = new HashSet<ExposureType>() { ExposureType.Building, ExposureType.Contents };
        protected HashSet<ExposureType> Loss = new HashSet<ExposureType>() { ExposureType.Building, ExposureType.Contents, ExposureType.BI };
        */
        public override void Initialize()
        { 
            throw new NotImplementedException();        
        }      
             
    }
}
