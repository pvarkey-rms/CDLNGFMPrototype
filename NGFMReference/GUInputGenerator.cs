using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;

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
        private COLCollection COLSet;
        private int EventID { get; set; }
        private TimeStyle timestyle;
        private LossStyle lossstyle;
        
        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> GULosses; 
        //RITE.ID -> numBldgs -> <timestamp -> list of loss>

        public GUInputGenerator(PartitionData PD, long conID, COLCollection _COLSet, TimeStyle _timestyle, LossStyle _lossstyle)
        {
            PartitionDataAdpator PData = new PartitionDataAdpator(PD);
            expData = PData.GetExposureAdaptor(conID);
            COLSet = _COLSet;
            timestyle = _timestyle;
            lossstyle = _lossstyle;
        //    Dictionary<long, Dictionary<int, Tuple<int, List<float>>>> GULosses = new Dictionary<long, Dictionary<int, Tuple<int, List<float>>>>();
        }

        public DateTime RandomTimeStamp() //not used
        {
            DateTime start = new DateTime(1990, 1, 1);  //earliest timestamp possible
            Random RandGen = new Random();

            int range = (DateTime.Today - start).Days;
            start = start.AddDays(RandGen.Next(range));
            return start;
        }

        public List<int> GenerateLossDatesCluster(int eventID)
        {
            List<int> dateList = new List<int>();
            //generate number of days affected
            int currSeed = eventID;
            var RandGen = new Random(currSeed);
            int eventNumDays = 8; //all losses happenedd within 8 days
           
            //generate those dates
            currSeed = eventID * eventNumDays;
            RandGen = new Random(currSeed);
            int refDate = RandGen.Next(0, 365);

            for (int i = 0; i < eventNumDays; i++)
            {
                int addDate = RandGen.Next(0, 365) % eventNumDays;
                dateList.Add(Math.Min(365, refDate + addDate));
            }
            return dateList;
        }

        public bool GenerateRITELoss(int eventID)
        {            
            bool success = true;          
            GULosses = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>(); 
            Dictionary<long, Tuple<double, uint, List<float>>> temp; 

            foreach (string subperil in COLSet.GetSubperils())
            {
                temp = new Dictionary<long, Tuple<double, uint, List<float>>>();
                foreach (RITCharacteristic RITChar in expData.Characteristics)
                {
                    List<float> LossList = new List<float>();
                    long RITEId = RITChar.ID;
                    int COLHashCode = subperil.GetHashCode() + 1;
                    int currSeed = ((int)RITEId * eventID * COLHashCode) % Int32.MaxValue;                    
                    var RandGen = new Random(currSeed);
                    uint eventNumDays;
                    if (timestyle == TimeStyle.RandomTimeStamps)
                    {
                        if (eventID % 2 == 0) //even eventID, totally uniformly random timeStamp
                            eventNumDays = (uint)RandGen.Next(0, 365);
                        else //odd eventID, cluster losses
                        {
                            List<int> dateList = GenerateLossDatesCluster(eventID);
                            eventNumDays = (uint)dateList[RandGen.Next(0, 365) % dateList.Count];
                        }
                    }
                    else
                        eventNumDays = 1;

                    int numBldgs = RITChar.ParentRITE.NumOfBldgs;
                    float RITCharTIV = (float)RITChar.TIV / numBldgs;
                    for (int i = 0; i < RITChar.ParentRITE.ActNumOfBldgs; i++)
                    {
                        float DR;

                        //this is to vary the DR, add whatever possible
                        
                        if (eventID % 10 == 0)
                            DR = (float)(RandGen.NextDouble()*0.0001);
                        else if (eventID % 10 == 1)
                            DR = (float)(RandGen.NextDouble() * (0.001-0.0001) + 0.0001);
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

                        if (lossstyle == LossStyle.GroundUp)
                            LossList.Add(DR * RITCharTIV);
                        else if (lossstyle == LossStyle.DamagaeRatio)
                            LossList.Add(DR);
                    }

                    temp.Add(RITEId, Tuple.Create(1.0, eventNumDays, LossList));  
                }

                GULosses.Add(subperil.ToString(), new Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>()
                {                   
                    {0, temp}
                }); 
            }
                    
            return success;
        }
        
    }
}




