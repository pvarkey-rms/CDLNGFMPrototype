using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;

namespace NGFMReference
{
    public class PLTGenerator
    {     
        private GUInputGenerator GUgenerator;
        public Period PeriodLoss { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }
        private TimeSpan PeriodRange;

        public PLTGenerator(GUInputGenerator _gugenerator, DateTime _start, DateTime _end)
        {
            GUgenerator = _gugenerator;
            Start = _start;
            End = _end;
            PeriodRange = Start - End;
        }

        public bool GeneratePeriodLoss(int PeriodID)
        { 
            //number of events and event numbers in the period
            List<int> eventList = GenerateEvents(PeriodID);

            List<EventOccurence> PGULosses = new List<EventOccurence>();

            for (int eventListIndex = 0; eventListIndex < eventList.Count; eventListIndex++)
            {
                int EventId = eventList[eventListIndex];
                //Generate event losses
                GUgenerator.GenerateRITELoss(EventId);
                //Generate event DateTime
                DateTime eventDate = GetRandomDate(PeriodID, EventId);
                EventOccurence occurence = new EventOccurence(EventId, eventDate, GUgenerator.GULosses);
                PGULosses.Add(occurence);            
            }

            PeriodLoss = new Period(PeriodID, Start, End, PGULosses);

            return true;
        }

        public DateTime GetRandomDate(int PeriodId, int EventId)
        {
            int currSeed = (PeriodId * EventId) % int.MaxValue;
            Random rnd = new Random(currSeed);

            var randTimeSpan = new TimeSpan((long)(rnd.NextDouble() * PeriodRange.Ticks));

            return Start + randTimeSpan;
        }

        public List<int> GenerateEvents(int PeriodId)
        {
            List<int> eventList = new List<int>();
            int numOfEvents = 0;

            int currSeed = PeriodId;
            Random RandGen = new Random(currSeed);                          
            double prob = RandGen.NextDouble(); //uniformly between 0 and 1

            if (prob < 0.7)  //80% chance generate one event
                numOfEvents = 1;
            else if (prob >= 0.7 && prob < 0.8)
                numOfEvents = 2;
            else if (prob >= 0.8 && prob < 0.9)
                numOfEvents = 3;
            else if (prob >= 0.9 && prob < 0.95)
                numOfEvents = 4;
            else
                numOfEvents = RandGen.Next(5, 200);

            Random eventGen = new Random(currSeed);

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

    public class PLTGenertorFactory : IDisposable
    {
        private GUInputGeneratorFactory GeneratorFactory;
        private PartitionDataAdpator pData;
        private const int PeriodLength = 6;

        private DateTime Start;
        private DateTime End;

        public PLTGenertorFactory(PartitionData PD, COLCollection _COLSet, SubSamplingAnalysisSetting subSamplingSettings, DateTime _start, TimeStyle _timestyle, LossStyle _lossstyle)
        {
            Start = _start;
            End = _start.AddYears(6);
            pData = new PartitionDataAdpator(PD, subSamplingSettings);
            GeneratorFactory = new GUInputGeneratorFactory(pData, _COLSet,  _timestyle, _lossstyle);
        }

        public PLTGenertorFactory(PartitionData PD, HashSet<string> _subperils, SubSamplingAnalysisSetting subSamplingSettings, DateTime _start, TimeStyle _timestyle, LossStyle _lossstyle)
        {
            Start = _start;
            End = _start.AddYears(6);
            pData = new PartitionDataAdpator(PD, subSamplingSettings);
            GeneratorFactory = new GUInputGeneratorFactory(pData, _subperils, _timestyle, _lossstyle);
        }

        public PLTGenerator GetGeneratorForContract(long ConID)
        {
            GUInputGenerator guGenerator = GeneratorFactory.GetGeneratorForContract(ConID);
            return new PLTGenerator(guGenerator, Start, End);
        }

        public PLTGenerator GetGeneratorForContract(ExposureDataAdaptor _expData)
        {
            GUInputGenerator guGenerator = GeneratorFactory.GetGeneratorForContract(_expData);
            return new PLTGenerator(guGenerator, Start, End);
        }

         #region IDisposable Override

        // Flag: Has Dispose already been called? 
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here. 
                GeneratorFactory.Dispose();
                pData.Dispose();           
            }

            // Free any unmanaged objects here. 
            
            disposed = true;
        }

        ~PLTGenertorFactory()
        {
            Dispose(false);
        }


        #endregion

    }
}
