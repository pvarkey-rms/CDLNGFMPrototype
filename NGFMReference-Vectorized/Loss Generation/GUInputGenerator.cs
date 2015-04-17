using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;
using System.IO;

namespace NGFMReference
{
    public enum LossStyle
    {
        GroundUp,
        DamagaeRatio
    }

    public enum TimeStyle
    {
        RandomTimeStamps,
        ConstantTimeStamps
    }

    public class GUInputGenerator
    { //to generate GU loss for each Rite

        private ExposureDataAdaptor expData;
        private HashSet<string> subperils;
        private int EventID { get; set; }
        private TimeStyle timestyle;
        private LossStyle lossstyle;
        
        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> GULosses; 
        //RITE.ID -> numBldgs -> <timestamp -> list of loss>

        //public GUInputGenerator(long conID, COLCollection _COLSet, TimeStyle _timestyle, LossStyle _lossstyle)
        //{
        //    PartitionDataAdpator PData = new PartitionDataAdpator(PD);
        //    expData = PData.GetExposureAdaptor(conID);
        //    subperils = _COLSet.GetSubperils();
        //    timestyle = _timestyle;
        //    lossstyle = _lossstyle;
      
        //}

        public GUInputGenerator(ExposureDataAdaptor _expData, HashSet<string> _subperils, TimeStyle _timestyle, LossStyle _lossstyle)
        {
            expData = _expData;
            subperils = _subperils;
            timestyle = _timestyle;
            lossstyle = _lossstyle;         
        }

        public DateTime RandomTimeStamp() //not used
        {
            DateTime start = new DateTime(1990, 1, 1);  //earliest timestamp possible
            Random RandGen = new Random();

            int range = (DateTime.Today - start).Days;
            start = start.AddDays(RandGen.Next(range));
            return start;
        }

        public List<int> GenerateLossDatesCluster(int eventID, int dateLag)
        {
            List<int> dateList = new List<int>();
            //generate number of days affected
            int currSeed = eventID;
            var RandGen = new Random(currSeed);
            //dateLag means that all losses happened within these days.
           
            //generate those dates
            currSeed = eventID * dateLag;
            RandGen = new Random(currSeed);
            int refDate = RandGen.Next(1, 365);

            for (int i = 0; i < dateLag; i++)
            {
                int addDate = RandGen.Next(0, 365) % dateLag;
                dateList.Add(Math.Min(365, refDate + addDate));
            }
            return dateList;
        }

        public bool GenerateRITELoss(int eventID)
        {            
            bool success = true;
            double factor = 1.0;
            //generate factor from FactorGenerator
            FactorGenerator fGen = new FactorGenerator();   
        
            GULosses = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>(); 
            Dictionary<long, Tuple<double, uint, List<float>>> temp; 

            foreach (string subperil in subperils)
            {
                temp = new Dictionary<long, Tuple<double, uint, List<float>>>();
                foreach (RITCharacteristic RITChar in expData.Characteristics)
                {
                    long RITCharID = RITChar.ID;

                    if (expData.UseAggregatedRITEs)
                    {
                        HashSet<long> DisAggRITChars = expData.AggregateIDtoDisaggIDs[RITCharID];

                        Dictionary<long, int> NumOfSamplesByRITChar;
                        if (RITChar.ParentRITE.IsAggregate)
                            NumOfSamplesByRITChar = BuildNumOfSampleDictionary(RITChar, DisAggRITChars, eventID, RITChar.ParentRITE.ActNumOfSampleBldgs);
                        else
                            NumOfSamplesByRITChar = new Dictionary<long,int> {{RITCharID, RITChar.ParentRITE.ActNumOfSampleBldgs}};

                        double FactorForAggLoc = RITChar.ParentRITE.Factor;

                        foreach (long DisAggRITCharID in DisAggRITChars)
                        {
                            int IntialnumSamples = NumOfSamplesByRITChar[DisAggRITCharID];
                            int FinalnumSamples = Math.Min(expData.MaxNumSamplesPerHiResLocation, IntialnumSamples);

                            double AdjustementForFactor = 1;
                            if(FinalnumSamples > 0)
                                AdjustementForFactor = (double)IntialnumSamples / FinalnumSamples;

                            double FactorForDisaggLoc = FactorForAggLoc * AdjustementForFactor;
                            float RITCharTIV = (float)RITChar.TIVPerBuilding;

                            List<float> LossList = GenerateRandomLossForRITE(eventID, subperil, DisAggRITCharID, FinalnumSamples, RITCharTIV);
                            uint eventNumDays = GenerateRandomDateTimeForRITE(eventID, subperil, DisAggRITCharID);
                            ////factor generator
                            //FactorInfo fInfo= fGen.GetFactor(RITChar.ParentRITE.ExposureID); //the sample of size is for RIT (location)
                            temp.Add(DisAggRITCharID, Tuple.Create(FactorForDisaggLoc, eventNumDays, LossList));
                        }
                    }
                    else
                    {
                        int numSamples = RITChar.ParentRITE.ActNumOfSampleBldgs;
                        float RITCharTIV = (float)RITChar.TIVPerBuilding;

                        List<float> LossList = GenerateRandomLossForRITE(eventID, subperil, RITCharID, numSamples, RITCharTIV);
                        uint eventNumDays = GenerateRandomDateTimeForRITE(eventID, subperil, RITCharID);
                        ////factor generator
                        //FactorInfo fInfo= fGen.GetFactor(RITChar.ParentRITE.ExposureID); //the sample of size is for RIT (location)
                        temp.Add(RITCharID, Tuple.Create((double)RITChar.ParentRITE.Factor, eventNumDays, LossList));
                    }

                }

                GULosses.Add(subperil.ToString(), new Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>()
                {                   
                    {0, temp}
                }); 
            }
                  
            return success;
        }

        private List<float> GenerateRandomLossForRITE(int eventID, string subperil, long RITCharId, int NumSamples, float RITCharTIV)
        {
            List<float> LossList = new List<float>();
            int COLHashCode = subperil.GetHashCode() + 1;
            int currSeed = ((int)RITCharId * eventID * COLHashCode) % Int32.MaxValue;
            var RandGen = new Random(currSeed);

            for (int i = 0; i < NumSamples; i++)
            {
                float DR;

                //this is to vary the DR, add whatever possible

                if (eventID % 10 == 0)
                    DR = (float)(RandGen.NextDouble() * 0.0001);
                else if (eventID % 10 == 1)
                    DR = (float)(RandGen.NextDouble() * (0.001 - 0.0001) + 0.0001);
                else if (eventID % 10 == 2)
                    DR = (float)(RandGen.NextDouble() * (0.01 - 0.001) + 0.001);
                else if (eventID % 10 == 3)
                    DR = (float)(RandGen.NextDouble() * (0.03 - 0.01) + 0.01);
                else if (eventID % 10 == 4)
                    DR = (float)(RandGen.NextDouble() * (0.04 - 0.03) + 0.03);
                else if (eventID % 10 == 5)
                    DR = (float)(RandGen.NextDouble() * (0.06 - 0.04) + 0.04);
                else if (eventID % 10 == 6)
                    DR = (float)(RandGen.NextDouble() * (0.2 - 0.1) + 0.1);
                else if (eventID % 10 == 7)
                    DR = (float)(RandGen.NextDouble() * (0.5 - 0.2) + 0.2);
                else
                    DR = (float)(RandGen.NextDouble());

                //DR = 100/RITCharTIV; //TODO: raintest

                if (lossstyle == LossStyle.GroundUp)
                    LossList.Add(DR * RITCharTIV);
                else if (lossstyle == LossStyle.DamagaeRatio)
                    LossList.Add(DR);
            }

            return LossList;
        }

        private uint GenerateRandomDateTimeForRITE(int eventID, string subperil, long RITCharId)
        {
            uint eventNumDays;
            int COLHashCode = subperil.GetHashCode() + 1;
            int currSeed = ((int)RITCharId * eventID * COLHashCode) % Int32.MaxValue;
            var RandGen = new Random(currSeed);

            if (timestyle == TimeStyle.RandomTimeStamps)
            {
                if (eventID % 10 == 0) //totally uniformly random timeStamp
                    eventNumDays = (uint)RandGen.Next(0, 365);
                else if (eventID % 10 == 1)
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 1);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
                else if (eventID % 10 == 2)
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 2);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
                else if (eventID % 10 == 3)
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 3);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
                else if (eventID % 10 == 4)
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 4);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
                else if (eventID % 10 == 5)
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 5);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
                else if (eventID % 10 == 6)
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 6);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
                else if (eventID % 10 == 7)
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 7);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
                else if (eventID % 10 == 8)
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 8);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
                else 
                {
                    List<int> dateList = GenerateLossDatesCluster(eventID, 9);
                    eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                }
            }
            else
                eventNumDays = 1;

            return eventNumDays;
        }

        public static void WriteGUToFile(long conIndex, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> RefguLoss)
        {
            // write GU to csv file        
            string filePath = @"C:\LocalNGFM\DRFiles\guDR-" + conIndex + ".csv";
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }
            else
            {
                FileStream fileStream = File.Open(filePath, FileMode.Open);
                fileStream.SetLength(0);
                fileStream.Close();
            }

            using (var file = new StreamWriter(filePath))
                foreach (var entry in RefguLoss)
                    file.WriteLine("{0}, {1}", entry.Key, "loss");
        }


        public Dictionary<long, int> BuildNumOfSampleDictionary(RITCharacteristic AggRITChar, HashSet<long> DisAggRITCharIDs, int eventID, int TotalNumSamples)
        {
            
            Dictionary<long, int> NumSamples = new Dictionary<long, int>();

            //Order RITCharIds
            List<long> OrderedDisAggRITCharIDs = DisAggRITCharIDs.OrderBy(id => expData.DisaggRitCharToDisAggRIT[id]).ToList();
            //List<long> OrderedDisAggRITIDs = DisAggRITCharIDs.Select(id => expData.DisaggRitCharToDisAggRIT[id]).OrderBy(id => id).ToList();
            int NumPartitions = DisAggRITCharIDs.Count - 1;

            Dictionary<long, int> SamplePartitions = new Dictionary<long, int>();
            int[] samples = new int[NumPartitions];

            long AggLocID = AggRITChar.ParentRITE.ExposureID;
            int currSeed = ((int)AggLocID * eventID) % Int32.MaxValue;
            var RandGen = new Random(currSeed);

            for (int i = 0; i< NumPartitions; i++)
            {
                samples[i] = RandGen.Next(TotalNumSamples + 1);
            }

            samples = samples.OrderBy(sample => sample).ToArray();

            for (int i = 0; i < NumPartitions; i++)
            {
                long DisAggRITCharID = OrderedDisAggRITCharIDs[i];

                SamplePartitions.Add(DisAggRITCharID, samples[i]);
            }

            long LastDisAggRITCharID = OrderedDisAggRITCharIDs[NumPartitions];
            SamplePartitions.Add(LastDisAggRITCharID, TotalNumSamples);

            int lastPartition = 0;
            foreach (long DisAggRITCharID in OrderedDisAggRITCharIDs)
            {
                int CurrentPartition = SamplePartitions[DisAggRITCharID];
                int CurrentNumOfSamples = CurrentPartition - lastPartition;
                
                NumSamples.Add(DisAggRITCharID, CurrentNumOfSamples);
                lastPartition = CurrentPartition;
            }

            int DebugToatlNumSamples = NumSamples.Select(pair => pair.Value).Sum();


            return NumSamples;
        }
    }

    public class GUInputGeneratorFactory : IDisposable
    {
        private HashSet<string> subperils;
        private TimeStyle timestyle;
        private LossStyle lossstyle;
        private PartitionDataAdpator PData;
        private bool NeedDisposePData;

        public GUInputGeneratorFactory(PartitionData PD, COLCollection _COLSet, SubSamplingAnalysisSetting subSmaplingSettings, TimeStyle _timestyle, LossStyle _lossstyle)
        {
            PData = new PartitionDataAdpator(PD, subSmaplingSettings);
            NeedDisposePData = true;
            subperils = _COLSet.GetSubperils();
            timestyle = _timestyle;
            lossstyle = _lossstyle;
        }

        public GUInputGeneratorFactory(PartitionData PD, HashSet<string> _subperils, SubSamplingAnalysisSetting subSmaplingSettings, TimeStyle _timestyle, LossStyle _lossstyle)
        {
            PData = new PartitionDataAdpator(PD, subSmaplingSettings);
            NeedDisposePData = true;
            subperils = _subperils;
            timestyle = _timestyle;
            lossstyle = _lossstyle;
        }

        public GUInputGeneratorFactory(PartitionDataAdpator _PData, COLCollection _COLSet, TimeStyle _timestyle, LossStyle _lossstyle)
        {
            PData = _PData;
            NeedDisposePData = false;
            subperils = _COLSet.GetSubperils();
            timestyle = _timestyle;
            lossstyle = _lossstyle;
        }

        public GUInputGeneratorFactory(PartitionDataAdpator _PData, HashSet<string> _subperils, TimeStyle _timestyle, LossStyle _lossstyle)
        {
            PData = _PData;
            NeedDisposePData = false;
            subperils = _subperils;
            timestyle = _timestyle;
            lossstyle = _lossstyle;
        }

        public GUInputGenerator GetGeneratorForContract(long ConID)
        {
            ExposureDataAdaptor expData = PData.GetExposureAdaptor(ConID);
            return new GUInputGenerator(expData, subperils, timestyle, lossstyle);
        }

        public GUInputGenerator GetGeneratorForContract(ExposureDataAdaptor _expData)
        {
            return new GUInputGenerator(_expData, subperils, timestyle, lossstyle);
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
                if(NeedDisposePData)
                    PData.Dispose();
            }

            // Free any unmanaged objects here. 
            
            disposed = true;
        }

        ~GUInputGeneratorFactory()
        {
            Dispose(false);
        }


        #endregion
    }
}




