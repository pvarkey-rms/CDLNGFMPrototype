using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public class GULossAdaptor
    {
        private Dictionary<long, IRITEindexMapper> mappers;
        Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> drLossDict;
        private IVectorEvent drLossEvent;
        private GULossType lossType; 
        private GULossTransformer transformer;
        private bool convertToGU;
        private Dictionary<long, Declarations> ContractDeclarationsSet;
        private Dictionary<long,DateTime> EventStartDate;

        private Dictionary<long, AllocatedLossAdaptor> AllocationHelpers;

        public GULossAdaptor(IRITEindexMapper _mapper, IVectorEvent _guLossVector, Dictionary<long,Declarations> _declarationsSet, bool _convertToGU = true)
        {
            //mapper = _mapper;
            //ContractDeclarations = _contractDeclarations;
            //AllocationHelper = new AllocatedLossAdaptor(_mapper, ContractDeclarations);
            //drLossEvent = _guLossVector;
            //lossType = GULossType.Vector;
            //transformer = new GULossTransformer(mapper);

            //convertToGU = _convertToGU;

            throw new NotSupportedException("Losses of Vector type input is no longer supported");
        }

        public GULossAdaptor(Dictionary<long, IRITEindexMapper> _mappers, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> _guLossDict, Dictionary<long, Declarations> _declarationsSet, bool _convertToGU = true)
        {
            mappers = _mappers;
            ContractDeclarationsSet = _declarationsSet;  
            drLossDict = _guLossDict;
            lossType = GULossType.Dictionary;
            EventStartDate = new Dictionary<long, DateTime>();

            //if (aggreagetRITEmapper.UseAggregateMapping)
            //    drLossDict = AggregateAndFilter(drLossDict);

            convertToGU = _convertToGU;        
        }

        public DateTime GetEventStartDate(long ContractID)
        {
            return EventStartDate[ContractID];
        }

        public DateTime GetEarliestEventDateAmongContracts()
        {
            DateTime EarliestEventDate = DateTime.MaxValue;

            foreach (KeyValuePair<long, DateTime> kvp in EventStartDate)
            {
                if (DateTime.Compare(EarliestEventDate, kvp.Value)>0)
                {
                    EarliestEventDate = kvp.Value;
                }

            }

            return EarliestEventDate;
        }

        //Filter TimeStamp based on Inception Date and Expiration Date & Find the earliest TimeStamp for the Event
        private void FilterGULoss(IVectorEvent output, long ContractID)
        {

            float[] LossVector = output.LossVector;
            uint[] TimeStamps = output.TimeStamps;
            int Length = LossVector.Length;
            DateTime inception = ContractDeclarationsSet[ContractID].Inception;
            DateTime expiration = ContractDeclarationsSet[ContractID].Expiration;
            uint MinTimeStamp = uint.MaxValue;

            for (int i = 0; i < Length; i++)
            {
                //Find earliest TimeStamp for the Event;
                
                if (TimeStamps[i] > 0 && TimeStamps[i] < MinTimeStamp)
                {
                    MinTimeStamp = TimeStamps[i];
                }

                DateTime AriteExpiration = inception.AddDays((double)TimeStamps[i]);
                if (AriteExpiration > expiration)
                    LossVector[i] = 0;
            }
            DateTime EventStartTime = new DateTime(inception.Year,1,1);
            EventStartTime = EventStartTime.AddDays((double)MinTimeStamp - 1);
            EventStartDate.Add(ContractID, EventStartTime);
        }

        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> GetDictTypeLosses(long ContractID)
        {
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> output;

            transformer = new GULossTransformer(mappers[ContractID]);

            if (lossType == GULossType.Dictionary)
                output = drLossDict;
            else if (lossType == GULossType.Vector)
                output = transformer.VectorToDict(drLossEvent);
            else
                throw new NotSupportedException();

            if (convertToGU)
            {
                ConvertToGU(output, mappers[ContractID]);
                //convertToGU = false;
            }

            return output;
        }

        public IVectorEvent GetVectorTypeLosses(long ContractID)
        {
            IVectorEvent output;
            transformer = new GULossTransformer(mappers[ContractID]);

            if (lossType == GULossType.Dictionary)
                output = transformer.DictToVector(drLossDict);
            else if (lossType == GULossType.Vector)
                output =  drLossEvent;
            else
                throw new NotSupportedException();

            if (convertToGU)
            {
                ConvertToGU(output.LossVector, mappers[ContractID]);
                //convertToGU = false;
            }

            FilterGULoss(output,ContractID);

            return output;
        }

        public AllocatedLossAdaptor GetAllocationHelper(long ContractID)
        {
            return new AllocatedLossAdaptor(mappers[ContractID]);
        }

        private void ConvertToGU(Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> drLossDict, IRITEindexMapper mapper)
        {
            int[] orderedNumOfBldgsList = mapper.GetOrderedNumBldgs();
            float[] TIVarray = mapper.GetOrderedTIVArray();

            foreach (string subperil in drLossDict.Keys)
            {
                Dictionary<long, Tuple<double, uint, List<float>>> LossesByRITE = drLossDict[subperil][0];

                foreach (KeyValuePair<long, Tuple<double, uint, List<float>>> pair in LossesByRITE)
                {
                    int indexOfRITE = mapper.GetMappedIndex(pair.Key);
                    int NumBldgs = orderedNumOfBldgsList[indexOfRITE];                
                    List<float> losses = pair.Value.Item3;
                    for (int bldgID = 0; bldgID < NumBldgs; bldgID++)
                    {
                        float bldgTIV = TIVarray[mapper.GetMappedIndex(pair.Key, bldgID, subperil)];
                        losses[bldgID] = losses[bldgID] * bldgTIV; //TODO: raintest
                        //losses[bldgID] = 100;
                    }
                }
            }
        }

        private void ConvertToGU(float[] drLossVector, IRITEindexMapper mapper)
        {
            int NumGuLoss = drLossVector.Length;
            float[] TIVarray = mapper.GetOrderedTIVArray();
            for (int i = 0; i < NumGuLoss; i++)
            {
                drLossVector[i] = drLossVector[i] * TIVarray[i];                
            }
        }

        //private Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> 
        //    AggregateAndFilter(Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> inputDict)
        //{
        //    Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> outputDict = new Dictionary<string,Dictionary<int,Dictionary<long,Tuple<double,uint,List<float>>>>>();

        //    foreach (string subperil in drLossDict.Keys)
        //    {
        //        Dictionary<long, Tuple<double, uint, List<float>>> LossesByRITE = drLossDict[subperil][0];
        //        Dictionary<long, Tuple<double, uint, List<float>>> AggregatedLossesByRITE = new Dictionary<long, Tuple<double, uint, List<float>>>();


        //        foreach (KeyValuePair<long, Tuple<double, uint, List<float>>> pair in LossesByRITE)
        //        {
        //            long currentRITEId = pair.Key;

        //            if (aggreagetRITEmapper.RITEIinContract(currentRITEId))
        //            {
        //                long originalAggregateRITEID = aggreagetRITEmapper.OriginalAggregateID(currentRITEId);

        //                if (AggregatedLossesByRITE.ContainsKey(originalAggregateRITEID))
        //                    AggregatedLossesByRITE[originalAggregateRITEID].Item3.AddRange(LossesByRITE[currentRITEId].Item3);
        //                else
        //                    AggregatedLossesByRITE[originalAggregateRITEID] = LossesByRITE[currentRITEId];
        //            }
        //        }

        //        outputDict[subperil][0] = AggregatedLossesByRITE;
        //    }

        //    return outputDict;

        //}

        //Method needs to be updated to work with GU loss from Vector Type losses!!!!
        #region Hours Clause Window Generation

        //public List<TimeWindow> GenerateWindows(int WindowSize)
        //{
        //    //get the unique list of dates from the rites
        //    List<int> lst_dates = new List<int>();
        //    foreach (KeyValuePair<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> l1 in drLossDict)
        //    {
        //        foreach (KeyValuePair<int, Dictionary<long, Tuple<double, uint, List<float>>>> l2 in l1.Value)
        //        {
        //            foreach (KeyValuePair<long, Tuple<double, uint, List<float>>> l3 in l2.Value)
        //            {
        //                if (!lst_dates.Contains((int)l3.Value.Item2))
        //                    lst_dates.Add((int)l3.Value.Item2);
        //            }
        //        }
        //    }
        //    ArrayList arr_dates = new ArrayList(lst_dates);
        //    arr_dates.Sort();
        //    return GenerateWindows(arr_dates, WindowSize);
        //}

        ////assuming dateslist is a sorted list of dates in integer format
        //private List<TimeWindow> GenerateWindows(ArrayList DatesList, int WindowSize)
        //{
        //    double interval = WindowSize + 0.5;
        //    int count = DatesList.Count;
        //    List<TimeWindow> lst_timeWindows = new List<TimeWindow>();

        //    for (int i = 0; i < count; i++)
        //    {
        //        double current = Convert.ToDouble(DatesList[i]);
        //        double lowerlim = Convert.ToDouble(DatesList[i]) - interval;
        //        double upperlim = Convert.ToDouble(DatesList[i]) + interval;
        //        double datediff = 0;

        //        if (i < count - 1)
        //        {
        //            datediff = Convert.ToDouble(DatesList[i + 1]) - Convert.ToDouble(DatesList[i]);
        //        }

        //        TimeWindow leftWindow = new TimeWindow();
        //        leftWindow.SetStartandEnd(lowerlim, current);
        //        lst_timeWindows.Add(leftWindow);
        //        //Console.WriteLine("Left Window - (" + leftWindow.start + "," + leftWindow.end + "]");

        //        //to eliminate empty windows and exclude the last window
        //        if (datediff <= interval && i < (count - 1))
        //        {
        //            TimeWindow rightWindow = new TimeWindow();
        //            rightWindow.SetStartandEnd(current, upperlim);
        //            lst_timeWindows.Add(rightWindow);
        //            //Console.WriteLine("Right Window - (" + rightWindow.start + "," + rightWindow.end + "]");
        //        }
        //    }
        //    return lst_timeWindows;
        //}


        ////assume the DatesList is sorted
        //private List<TimeWindow> GenerateWindowsSorted(ArrayList DatesList, int WindowSize)
        //{
        //    double interval = WindowSize + 0.5;
        //    int count = DatesList.Count;            
        //    List<TimeWindow> lst_timeWindows = new List<TimeWindow>();
        //    TimeWindow oneWindow = new TimeWindow();

        //    double lastStart = Convert.ToDouble(DatesList[count - 1]);

        //    for (int i = 0; i < count; i++)
        //    {
        //        double current = Convert.ToDouble(DatesList[i]);
        //        double upperlim = Convert.ToDouble(DatesList[i]) + WindowSize - 1;

        //        if (upperlim > lastStart)
        //            continue;
        //        else if (i == count - 1)
        //        {
        //            oneWindow.SetStartandEnd(lastStart, Convert.ToDouble(DatesList[count - 1]));
        //            lst_timeWindows.Add(oneWindow);
        //        }
        //        else
        //        {
        //            oneWindow.SetStartandEnd(current, upperlim);
        //            lst_timeWindows.Add(oneWindow);
        //        }               
        //    }
        //    return lst_timeWindows;
        //}


        #endregion
        private enum GULossType
        {
            Dictionary,
            Vector
        }
    
    }

    public class GULossTransformer
    {
        private IRITEindexMapper mapper;
        private AggregateRITEMapper aggreagetRITEmapper;

        public GULossTransformer(IRITEindexMapper _mapper)
        {
            mapper = _mapper;
            aggreagetRITEmapper = mapper.GetAggregateMapper();
        }

        public IVectorEvent DictToVector(Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> drLossDict)
        {
            //First filter losses by contract and group losses by aggregate RITEs
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<List<double>, List<uint>, List<float>>>>> PostAggregateDrDict;

            PostAggregateDrDict = AggregateAndFilter(drLossDict);

            float[] GULossVector = new float[mapper.TotalNumIndexes];
            float[] FactorVector = new float[mapper.TotalNumBldgs];
            uint[] TimeStampsVector = new uint[mapper.TotalNumIndexes];

            int[] orderedNumOfBldgsList = mapper.GetOrderedNumBldgs();

            bool firstSubperil = true;
            foreach (string subperil in drLossDict.Keys)
            {
                Dictionary<long, Tuple<List<double>, List<uint>, List<float>>> LossesByRITE = PostAggregateDrDict[subperil][0];

                foreach (KeyValuePair<long, Tuple<List<double>, List<uint>, List<float>>> pair in LossesByRITE)
                {
                    int indexOfRITE = mapper.GetMappedIndex(pair.Key);
                    //int NumBldgs = orderedNumOfBldgsList[indexOfRITE];
                    
                    List<float> losses = pair.Value.Item3;
                    int NumOfSubSamples = losses.Count;

                    //Get Factors and Gu Losses
                    if (firstSubperil)
                    {
                        for (int subSampleID = 0; subSampleID < NumOfSubSamples; subSampleID++)
                        {
                            //Get GU loss from dictionary
                            int IndexInGULossVector = mapper.GetMappedIndex(pair.Key, subSampleID, subperil);
                            GULossVector[IndexInGULossVector] = losses[subSampleID];

                            //Get factor from dictionary
                            int IndexInFactorVector = mapper.GetMappedFactorIndex(pair.Key, subSampleID);
                            FactorVector[IndexInFactorVector] = (float)pair.Value.Item1[subSampleID];

                            //Get Time Stamp from dictionary
                            //int IndexInTimeStampsVector = mapper.GetMappedIndex(pair.Key, subSampleID, subperil);
                            TimeStampsVector[IndexInGULossVector] = pair.Value.Item2[subSampleID];
                        }
                    }
                    else
                    {                     
                        for (int subSampleID = 0; subSampleID < NumOfSubSamples; subSampleID++)
                        {
                            //Get GU loss from dictionary
                            int IndexInGULossVector = mapper.GetMappedIndex(pair.Key, subSampleID, subperil);
                            GULossVector[IndexInGULossVector] = losses[subSampleID];

                            //Get Time Stamp from dictionary
                            //int IndexInTimeStampsVector = mapper.GetMappedIndex(pair.Key, subSampleID, subperil);
                            TimeStampsVector[IndexInGULossVector] = pair.Value.Item2[subSampleID];
                        }
                    }

                }

                firstSubperil = false;
            }

            return new VectorLossEvent(GULossVector, FactorVector, TimeStampsVector);
        }

        public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> VectorToDict(IVectorEvent drLossEvent)
        {
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> guLossDict = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();

            throw new NotImplementedException();
        }

        public static int[] GenerateMultiplierArr(int numOfBldgs)
        {
            int MaxNumOfBldgs = 250;
            int[] multiplierArr;
            if (numOfBldgs <= MaxNumOfBldgs)
            {
                multiplierArr = new int[numOfBldgs];
                for (int i = 1; i <= numOfBldgs; i++)
                {
                    multiplierArr[i - 1] = 1;
                }
            }
            else
            {
                multiplierArr = new int[MaxNumOfBldgs];
                Double temp = (double)(numOfBldgs / MaxNumOfBldgs);
                int n = (int)(Math.Floor(temp));
                int m = numOfBldgs - n * MaxNumOfBldgs;
                for (int i = 1; i <= m; i++)
                {
                    multiplierArr[i - 1] = n + 1;
                }
                for (int i = m + 1; i <= MaxNumOfBldgs; i++)
                {
                    multiplierArr[i - 1] = n;
                }
            }
            return multiplierArr;
        }

        private Dictionary<string, Dictionary<int, Dictionary<long, Tuple<List<double>, List<uint>, List<float>>>>>
            AggregateAndFilter(Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> inputDict)
        {
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<List<double>, List<uint>, List<float>>>>> outputDict = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<List<double>, List<uint>, List<float>>>>>();
            AggregateRITEMapper aggregateRITEmapper = mapper.GetAggregateMapper();
            bool UseAggragatRITEs = aggregateRITEmapper.UseAggregateMapping;

            foreach (string subperil in inputDict.Keys)
            {
                Dictionary<long, Tuple<double, uint, List<float>>> LossesByRITE = inputDict[subperil][0];
                Dictionary<long, Tuple<List<double>, List<uint>, List<float>>> AggregatedLossesByRITE = new Dictionary<long, Tuple<List<double>, List<uint>, List<float>>>();

                foreach (KeyValuePair<long, Tuple<double, uint, List<float>>> pair in LossesByRITE)
                {
                    long currentRITEId = pair.Key;

                    if (aggregateRITEmapper.RITEInContract(currentRITEId))
                    //if (true) // Sunny Hack remove Immeadiateley !!!!!!!!!!!
                    {
                        long originalAggregateRITEID = aggregateRITEmapper.OriginalAggregateID(currentRITEId);
                        int NumOfSamples = LossesByRITE[currentRITEId].Item3.Count;

                        if (AggregatedLossesByRITE.ContainsKey(originalAggregateRITEID))
                        {
                            //Set Factors 
                            AggregatedLossesByRITE[originalAggregateRITEID].Item1.AddRange(Enumerable.Repeat(LossesByRITE[currentRITEId].Item1, NumOfSamples));
                            //Set Timestamps
                            AggregatedLossesByRITE[originalAggregateRITEID].Item2.AddRange(Enumerable.Repeat(LossesByRITE[currentRITEId].Item2, NumOfSamples));
                            //Set GULoss
                            AggregatedLossesByRITE[originalAggregateRITEID].Item3.AddRange(LossesByRITE[currentRITEId].Item3);
                        }
                        else
                        {
                            AggregatedLossesByRITE[originalAggregateRITEID] = new Tuple<List<double>, List<uint>, List<float>>(
                                                                                    Enumerable.Repeat(LossesByRITE[currentRITEId].Item1, NumOfSamples).ToList(),
                                                                                    Enumerable.Repeat(LossesByRITE[currentRITEId].Item2, NumOfSamples).ToList(),
                                                                                    new List<float>(LossesByRITE[currentRITEId].Item3));
                        }
                    }
                }

                outputDict.Add(subperil, new Dictionary<int, Dictionary<long, Tuple<List<double>, List<uint>, List<float>>>> 
                                            {{ 0, AggregatedLossesByRITE }});
            }

            return outputDict;

        }
    }

    public class AggregateRITEMapper
    {
        public Dictionary<long, long> RiskCharIDtoAggregateID { get; private set; }
        public bool UseAggregateMapping { get; private set; }

        public AggregateRITEMapper(ExposureDataAdaptor expAdaptor)
        {
            RiskCharIDtoAggregateID = expAdaptor.RiskCharIDtoAggregateID;
            UseAggregateMapping = expAdaptor.UseAggregatedRITEs;
        }

        public long OriginalAggregateID(long newRITEID)
        {
            return RiskCharIDtoAggregateID[newRITEID];
        }

        public bool RITEInContract(long newRITEID)
        {
            return RiskCharIDtoAggregateID.ContainsKey(newRITEID);
        }

    }

}
