using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;

namespace NGFMReference
{
    class PLTGenerator
    {
        private ExposureDataAdaptor expData;
        private PartitionData PD;
        private long conID;
        private COLCollection COLSet;        
        private TimeStyle timestyle;
        private LossStyle lossStyle;
        public PeriodLossTable PLT; 
             
        public PLTGenerator(PartitionData _PD, long _conID, int pid, COLCollection _COLSet, TimeStyle _timestyle, LossStyle _lossStyle, DateTime _start, DateTime _end)
        {
            PartitionDataAdpator PData = new PartitionDataAdpator(_PD);
            expData = PData.GetExposureAdaptor(conID);
            PD = _PD;
            conID = _conID;
            COLSet = _COLSet;
            timestyle = _timestyle;
            lossStyle = _lossStyle;
            PLT = new PeriodLossTable(pid, _COLSet, _timestyle, _lossStyle, _start, _end);        
        }

        public void GeneratePLT()
        { 
            //number of events and event numbers in the period
            List<int> eventList = GenerateEvents();
      
            //call GUInputGenerator            
            GUInputGenerator GUGen = new GUInputGenerator(PD, conID, COLSet, timestyle, lossStyle);
            foreach (int et in eventList)
            {
                GUGen.GenerateRITELoss(et);
                PLT.PGULosses.Add(GUGen.GULosses);            
            }                        
        }

        public List<int> GenerateEvents()
        {
            List<int> eventList = new List<int>();
            int numOfEvents = 0;
      
            int currSeed = PLT.PID;
            Random RandGen = new Random(currSeed);                          
            double prob = RandGen.NextDouble(); //uniformly between 0 and 1

            if (prob < 0.7)  //80% chance generate one event
                numOfEvents = 1;
            else if (prob >= 0.7 && prob < 0.8)
                numOfEvents = 2;
            else if (prob >= 0.8 && prob < 0.85)
                numOfEvents = 3;
            else if (prob >= 0.85 && prob < 0.9)
                numOfEvents = 4;
            else
                numOfEvents = RandGen.Next(5, 200);

            Random eventGen = new Random(currSeed * numOfEvents);

            for (int i = 1; i <= numOfEvents; i++)
            {                
                eventList.Add(eventGen.Next());
            }
            return eventList;        
        }

        public void UpdatePLT()
        { 
        
        }

        public void CopyPLT()
        { 
        
        }

    }
}
