using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Rms.DataServices.DataObjects;
using Rms.DataServices.DataObjects.CDL;
using Rms.Analytics.DataService.Zip;
using RMS.ContractObjectModel;
using Noesis.Javascript;


namespace NGFMReference
{
    public class ExposureDataAdaptor
    {
        private ExposureDataExtractor EDSDataExtract;
        private Dictionary<long, RITCharacteristic> _characteristicsDict;
        private Dictionary<long, RITE> _ritesDict;
        private Dictionary<string, ScheduleOfRITEs> _schedulesDict;

        private HashSet<RITCharacteristic> _characteristics;
        private HashSet<RITE> _rites;
        private HashSet<ScheduleOfRITEs> _schedules;
        private ContractExposure _contractExposure;
        private Dictionary<string, object> _contractJSON;
        private PositionData _positions;
        private SubSamplingAnalysisSetting SubSamplingSettings;

        private bool UseJSContext;
        private bool treatyIncludesPrimaryData;
        
        //private NGFMPrototype ParsingAPI;
        private JavascriptContext JSContext;
        private JavascriptParser JSParser;

        public bool TreatyExposure { get; private set; }
        public bool TreatyIncludesPrimaryData
        {
            get
            {
                if (TreatyExposure)
                    return treatyIncludesPrimaryData;
                else
                    throw new InvalidOperationException("Field cannot be called on non-traty exposures!");
            }
            private set { treatyIncludesPrimaryData = value; }
        }
        public HashSet<RITE> ContractRITES
        { get { return _rites; } }
        public HashSet<ScheduleOfRITEs> Schedules
        { get { return _schedules; } }
        public HashSet<RITCharacteristic> Characteristics
        { get { return _characteristics; } }
        public Dictionary<long, RITCharacteristic> CharacteristicsDict
            { get { return _characteristicsDict; } }
        public Dictionary<string, object> ContractJSON
        { get { return _contractJSON; } }
        public long ContractID { get { return _contractExposure.ExposureID; } }
        public PositionData Positions
        {
            get
            {
                if (_contractExposure.ContractType.IsReinsuranceContract())
                {
                    return _positions;
                }
                else
                    throw new InvalidOperationException("Cannot get position data for primary contract");                  
            }
        }
        public Dictionary<long, long> RiskCharIDtoAggregateID { get; private set; }
        public Dictionary<long, HashSet<long>> AggregateIDtoDisaggIDs { get; private set; }
        public Dictionary<long, long> DisaggRitCharToDisAggRIT { get; private set; }
        public COLCollection ContractCOLs { get; private set; }

        //public Dictionary<long, int> RiskCharIDtoNumSamples { get; private set; }
        public bool UseAggregatedRITEs { get; private set; }

        //Max number of samples that will be processed per location
        public int MaxNumSamplesPerHiResLocation;

        public ExposureDataAdaptor(ContractExposure contractExposure, JavascriptParser _jsParser, SubSamplingAnalysisSetting _subSamplingSettings)
        {
            JSParser = _jsParser;
            _contractExposure = contractExposure;
            SubSamplingSettings = _subSamplingSettings;
            MaxNumSamplesPerHiResLocation = SubSamplingSettings.MaxSampleBldgs;
            //Unnecessary if no more multiplier arrays...
            RITE.MaxNumOfBldgs = MaxNumSamplesPerHiResLocation;

            _contractJSON = GetJSONForContract();
            ContractCOLs = GetContractCOLs();
            UseAggregatedRITEs = IsExtractVersion2Disaggregated();
            EDSDataExtract = new ExposureDataExtractor(_contractExposure);

            _characteristics = new HashSet<RITCharacteristic>();
            _rites = new HashSet<RITE>();
            _schedules = new HashSet<ScheduleOfRITEs>();
            _characteristicsDict = new Dictionary<long, RITCharacteristic>();
            _ritesDict = new Dictionary<long, RITE>();
            _schedulesDict = new Dictionary<string, ScheduleOfRITEs>();

            RiskCharIDtoAggregateID = new Dictionary<long, long>();
            AggregateIDtoDisaggIDs = new Dictionary<long, HashSet<long>>();
            DisaggRitCharToDisAggRIT = new Dictionary<long, long>();

            if (_contractExposure.ContractType.IsReinsuranceContract())
            {
                TreatyExposure = true;
                treatyIncludesPrimaryData = false;
            }
            else
                TreatyExposure = false; 
        }

        //public ExposureDataAdaptor(ContractExposure contractExposure, NGFMPrototype _ParsingAPI)
        //{
        //    //ParsingAPI = _ParsingAPI;
        //    UseJSContext = false;
        //    _contractExposure = contractExposure;

        //    _contractJSON = GetJSONForContract();
        //    EDSDataExtract = new ExposureDataExtractor(_contractExposure);

        //    _characteristics = new HashSet<RITCharacteristic>();
        //    _rites = new HashSet<RITE>();
        //    _schedules = new HashSet<ScheduleOfRITEs>();
        //    _characteristicsDict = new Dictionary<long, RITCharacteristic>();
        //    _ritesDict = new Dictionary<long, RITE>();
        //    _schedulesDict = new Dictionary<string, ScheduleOfRITEs>();

        //    if (_contractExposure.ContractType.IsReinsuranceContract())
        //        TreatyExposure = true;
        //    else
        //        TreatyExposure = false;
        //}
       
        public void GetPrimaryData()
        {
            if (TreatyExposure)
                throw new InvalidOperationException("Cannot use this method to get exposure data for reinsurance contract");

            EDSDataExtract.ExtractRiteMap();

            Dictionary<long, ExtractRITCharInfo> CoverageIdAttrMap = EDSDataExtract.CoverageIdAttrMap;

            if (UseAggregatedRITEs)
                CoverageIdAttrMap = AggregateRITChars(CoverageIdAttrMap);
            else
            {
                foreach (KeyValuePair<long, ExtractRITCharInfo> pair in CoverageIdAttrMap)
                {
                    RiskCharIDtoAggregateID.Add(pair.Key, pair.Key);
                    AggregateIDtoDisaggIDs.Add(pair.Key, new HashSet<long> { pair.Key });
                }
            }

            #region old RITE aggreagation code


            //foreach (var LocGroup in LocationGroups)
            //{
            //    if (UseAggregatedRITEs)
            //    {
            //        long RiteID = LocGroup.First().Value.RITExposureId;
            //        long OriginalRiteID = (long)LocGroup.First().Value.OriginalRITExposureId;

            //        if (RiteID == OriginalRiteID)
            //        {
            //            RITE newRITE = BuildRITE(LocGroup.First());
            //            _rites.Add(newRITE);
            //            _ritesDict.Add(LocGroup.Key, newRITE);

            //            foreach (KeyValuePair<long, ExtractRITCharInfo> pair in LocGroup)
            //            {
            //                RITCharacteristic newRITChar = BuildRITCharacteristic(pair);
            //                _characteristics.Add(newRITChar);
            //                _characteristicsDict.Add(pair.Key, newRITChar);
            //                newRITE.AddCharacteristic(newRITChar);
            //            }
            //        }
            //        else
            //        {
            //            foreach (KeyValuePair<long, ExtractRITCharInfo> pair in LocGroup)
            //            {
            //                RiskCharIDtoAggregateID.Add(pair.Key, (long)pair.Value.OriginalRITECharacteristicId);
            //            }
            //        }
            //    }
            //    else
            //    {
            //        RITE newRITE = BuildRITE(LocGroup.First());
            //        _rites.Add(newRITE);
            //        _ritesDict.Add(LocGroup.Key, newRITE);

            //        foreach (KeyValuePair<long, ExtractRITCharInfo> pair in LocGroup)
            //        {
            //            RITCharacteristic newRITChar = BuildRITCharacteristic(pair);
            //            _characteristics.Add(newRITChar);
            //            _characteristicsDict.Add(pair.Key, newRITChar);
            //            newRITE.AddCharacteristic(newRITChar);
            //        }
            //    }
            //}

#endregion

            var LocationGroups = CoverageIdAttrMap.GroupBy(pair => pair.Value.RITExposureId);

            foreach (var LocGroup in LocationGroups)
            {
                RITE newRITE = BuildRITE(LocGroup.First());
                _rites.Add(newRITE);
                _ritesDict.Add(LocGroup.Key, newRITE);

                foreach (KeyValuePair<long, ExtractRITCharInfo> pair in LocGroup)
                {
                    RITCharacteristic newRITChar = BuildRITCharacteristic(pair);
                    _characteristics.Add(newRITChar);
                    _characteristicsDict.Add(pair.Key, newRITChar);
                    newRITE.AddCharacteristic(newRITChar);
                }
            }


            foreach (KeyValuePair<string, Subschedule> EDSschedule in EDSDataExtract.SubSchedule)
            {
                ScheduleOfRITEs newSchedule;
                HashSet<long> Ids = (EDSschedule.Value.Ids != null) ? EDSschedule.Value.Ids : new HashSet<long>(EDSschedule.Value.CompressedIds.Enumerable());

                if (EDSschedule.Value.Type == SubscheduleType.RITCHARACTERISTICS) //|| EDSschedule.Value.Name == "Blanket_FL" || EDSschedule.Value.Name == "Blanket_EQ" || EDSschedule.Value.Name == "Blanket_WS")    //Sunny Hack
                    newSchedule = BuildScheduleFromRITChars2(EDSschedule.Key, Ids);
                else if (EDSschedule.Value.Type == SubscheduleType.RITEXPOSURES)
                    try { newSchedule = BuildScheduleFromRITs2(EDSschedule.Key, Ids); }
                    catch(IndexOutOfRangeException exp)
                    {
                        newSchedule = BuildScheduleFromRITChars2(EDSschedule.Key, Ids);
                    }
                else
                    throw new InvalidOperationException("Cannot support building schedules of type: " + EDSschedule.Value.Type.ToString());

                _schedules.Add(newSchedule);
                _schedulesDict.Add(newSchedule.Name, newSchedule);
            }

         }

        public void ExtractPositionData()
        {
            if (!TreatyExposure)
                throw new InvalidOperationException("Cannot use this method to get position data for primary contract");

            if (null != _contractExposure && null != _contractExposure.Positions)
            {
                Dictionary<string, HashSet<long>> PosDict = _contractExposure.Positions
                        .Where(elem => null != elem)
                        .ToDictionary(elem => elem.PositionName.ToUpper().Trim(), elem => new HashSet<long>(elem.LossSourcePositionIDs.ToArray()));
                _positions = new PositionData(PosDict);
            }
        }

        private RITCharacteristic BuildRITCharacteristic(KeyValuePair<long, ExtractRITCharInfo> RiteAttribute)
        {
            return new RITCharacteristic(RiteAttribute.Key, (ExposureType)RiteAttribute.Value.ExposureType, RiteAttribute.Value.Value);
        }

        private RITE BuildRITE(KeyValuePair<long, ExtractRITCharInfo> RiteAttribute)
        {
            int NumSamples = GetNumOfSamplesForRITE(RiteAttribute.Value);

            return new RITE(RiteAttribute.Value.RITExposureId, RiteAttribute.Value.NumBuildings, NumSamples, RiteAttribute.Value.IsAggregateLowRes);
        }

        private ScheduleOfRITEs BuildScheduleFromRITChars(string name, HashSet<long> EDSschedule)
        {
            ScheduleOfRITEs newSchedule = new ScheduleOfRITEs(name);

            foreach (long RITCharID in EDSschedule)
            {
                long RITEID = EDSDataExtract.CoverageIdAttrMap[RITCharID].RITExposureId;

                RITCharacteristic characteristicToAdd = _characteristics.Where(ritchar => ritchar.ID == RITCharID).FirstOrDefault();
                RITE ritetoAdd = _rites.Where(rite => rite.ExposureID == RITEID).FirstOrDefault();

                if (characteristicToAdd != null)
                    newSchedule.AddCharacteristic(characteristicToAdd);
                else
                    throw new IndexOutOfRangeException("Error building schedule with RITCharacteristics: Cannot find RITCharacteristic with id = " + RITCharID + " in Exposure Data");

                if (ritetoAdd != null)
                    newSchedule.AddItem(ritetoAdd);
                else
                    throw new IndexOutOfRangeException("Error building schedule with RITEs: Cannot find RITE with id = " + RITEID + " in Exposure Data");
            }

            return newSchedule;
        }

        private ScheduleOfRITEs BuildScheduleFromRITChars2(string name, HashSet<long> EDSschedule)
        {
            ScheduleOfRITEs newSchedule = new ScheduleOfRITEs(name);

            foreach (long RITCharID in EDSschedule)
            {
                RITCharacteristic characteristicToAdd;
                if(!_characteristicsDict.TryGetValue(RITCharID, out characteristicToAdd))
                    throw new IndexOutOfRangeException("Error building schedule with RITCharacteristics: Cannot find RITCharacteristic with id = " + RITCharID + " in Exposure Data");
                newSchedule.AddCharacteristic(characteristicToAdd);

                //long RITEID = EDSDataExtract.CoverageIdAttrMap[RITCharID].RITExposureId;
                long RITEID = characteristicToAdd.ParentRITE.ExposureID;

                RITE ritetoAdd;
                if (!_ritesDict.TryGetValue(RITEID, out ritetoAdd))
                    throw new IndexOutOfRangeException("Error building schedule with RITEs: Cannot find RITE with id = " + RITEID + " in Exposure Data");
                newSchedule.AddItem(ritetoAdd);
            }

            return newSchedule;
        }

        private ScheduleOfRITEs BuildScheduleFromRITs(string name, HashSet<long> EDSschedule)
        {
            ScheduleOfRITEs newSchedule = new ScheduleOfRITEs(name);

            foreach (long riteID in EDSschedule)
            {
                RITE ritetoAdd = _rites.Where(rite => rite.ExposureID == riteID).FirstOrDefault();

                if (ritetoAdd != null)
                {
                    newSchedule.AddItem(ritetoAdd);
                    newSchedule.RITChars.UnionWith(ritetoAdd.RiskCharacteristics);
                }
                else
                    throw new IndexOutOfRangeException("Error building schedule with RITEs: Cannot find RITE with id = " + riteID + " in Exposure Data");
            }

            return newSchedule;
        }

        private ScheduleOfRITEs BuildScheduleFromRITs2(string name, HashSet<long> EDSschedule)
        {
            ScheduleOfRITEs newSchedule = new ScheduleOfRITEs(name);

            foreach (long riteID in EDSschedule)
            {
                RITE ritetoAdd;
                if (!_ritesDict.TryGetValue(riteID, out ritetoAdd))
                    throw new IndexOutOfRangeException("Error building schedule with RITEs: Cannot find RITE with id = " + riteID + " in Exposure Data");                

                newSchedule.AddItem(ritetoAdd);
                newSchedule.RITChars.UnionWith(ritetoAdd.RiskCharacteristics);          
            }

            return newSchedule;
        }

        public void AddSchedule(string name, HashSet<RITE> rites)
        {
            ScheduleOfRITEs newSchedule = new ScheduleOfRITEs(name);
            foreach(RITE rite in rites)
            {
                newSchedule.AddItem(rite);
                newSchedule.RITChars.UnionWith(rite.RiskCharacteristics);
            }
            _schedules.Add(newSchedule);
            _schedulesDict.Add(newSchedule.Name, newSchedule);
        }

        public void AddScheduleFromRITChars(string name, HashSet<RITCharacteristic> RITChars)
        {
            ScheduleOfRITEs newSchedule = new ScheduleOfRITEs(name);
            foreach (RITCharacteristic RITChar in RITChars)
            {
                newSchedule.AddItem(RITChar.ParentRITE);
                newSchedule.AddCharacteristic(RITChar);
            }
            _schedules.Add(newSchedule);
            _schedulesDict.Add(newSchedule.Name, newSchedule);
        }

        public Dictionary<string, object> GetJSONForContract()
        {
            string CDL = _contractExposure.Contract.CDLString;
            string strJSON;
            Dictionary<string, object> jsonParseResult;

            //if (UseJSContext)
            //{
            //    CDL = CDL.Replace(System.Environment.NewLine, "     ");
            //    jsonParseResult = (Dictionary<string, object>)(JSContext.Run("grammarAst.parse('" + CDL + "')"));
            //}
            //else
            //{
            //    if (ParsingAPI == null)
            //        throw new InvalidOperationException("You must set the Parsing API before using this method!");          
            //    jsonParseResult = ParsingAPI.ParseCDLUsingJISONJS(CDL);
            //}

            jsonParseResult = JSParser.ParseCDL(CDL);

            return jsonParseResult;
        }

        public ScheduleOfRITEs GetSchedule(string name)
        {
            ScheduleOfRITEs schedule;
            if (!_schedulesDict.TryGetValue(name, out schedule))
                throw new ArgumentOutOfRangeException("Cannot find schedule in exposure data with name: " + name);

            return schedule;
        }

        public void AddExposureToTreaty(ExposureDataAdaptor otherExposure)
        {
            if (!TreatyIncludesPrimaryData)
                this.UseAggregatedRITEs = otherExposure.UseAggregatedRITEs;
            else
            {
                if (this.UseAggregatedRITEs != otherExposure.UseAggregatedRITEs)
                    throw new InvalidOperationException("Cannot combine extracts from different versions of disaggregation tool, all input primary contracts to a Treaty must be of same disaggreation version!");
            }

            this._characteristics.UnionWith(otherExposure.Characteristics);
            this._rites.UnionWith(otherExposure.ContractRITES);
            this._schedules.UnionWith(otherExposure.Schedules);

            //Combine Dictionarys
            this._characteristicsDict = MergeDictionarys(this._characteristicsDict, otherExposure._characteristicsDict);
            this._ritesDict = MergeDictionarys(this._ritesDict, otherExposure._ritesDict);
            this._schedulesDict = MergeDictionarys(this._schedulesDict, otherExposure._schedulesDict);

            //Combine Contract COls
            this.ContractCOLs.UnionWith(otherExposure.ContractCOLs);

            //Combine Aggregate Mappings
            this.RiskCharIDtoAggregateID = MergeDictionarys(this.RiskCharIDtoAggregateID, otherExposure.RiskCharIDtoAggregateID);
            this.DisaggRitCharToDisAggRIT = MergeDictionarys(this.DisaggRitCharToDisAggRIT, otherExposure.DisaggRitCharToDisAggRIT);
            this.AggregateIDtoDisaggIDs = MergeDictionarys(this.AggregateIDtoDisaggIDs, otherExposure.AggregateIDtoDisaggIDs);
        }

        private static Dictionary<TKey,TValue>
                 MergeDictionarys<TKey, TValue>(Dictionary<TKey, TValue> dictionary1, Dictionary<TKey, TValue> dictionary2)
        {
            List<Dictionary<TKey, TValue>> dictionaries = new List<Dictionary<TKey, TValue>>();
            dictionaries.Add(dictionary1);
            dictionaries.Add(dictionary2);

            Dictionary<TKey, TValue> result = dictionaries.SelectMany(dict => dict)
                                                         .ToLookup(pair => pair.Key, pair => pair.Value)
                                                         .ToDictionary(group => group.Key, group => group.First());

            return result;
        }

        private Dictionary<long, ExtractRITCharInfo> AggregateRITChars(Dictionary<long, ExtractRITCharInfo> CoverageIdAttrMap)
        {
            Dictionary<long, ExtractRITCharInfo> AggregatedRITCharsOutput = new Dictionary<long, ExtractRITCharInfo>();

            var AggreagateRITCharGroups = EDSDataExtract.CoverageIdAttrMap.GroupBy(pair => pair.Value.OriginalRITECharacteristicId);

            foreach (var AggRITCharGroup in AggreagateRITCharGroups)
            {                
                ExtractRITCharInfo SampleDisAggCharInfo = AggRITCharGroup.First().Value;
                ExtractRITCharInfo AggRITCharInfo = new ExtractRITCharInfo(SampleDisAggCharInfo);


                if (AggRITCharGroup.Key != AggRITCharGroup.First().Key)
                    AggRITCharInfo.IsAggregateLowRes = true;

                AggRITCharInfo.NumBuildings = 0;
                AggRITCharInfo.Value = 0;
                //AggRITCharInfo.NumSamples = 0;
                //AggRITCharInfo.Factor = 1;               
                AggRITCharInfo.RITExposureId = AggRITCharInfo.OriginalRITExposureId;
                AggRITCharInfo.OriginalRITECharacteristicId = AggRITCharGroup.Key;


                AggregateIDtoDisaggIDs.Add(AggRITCharInfo.OriginalRITECharacteristicId, new HashSet<long>());

                foreach (KeyValuePair<long, ExtractRITCharInfo> pair in AggRITCharGroup)
                {
                    AggRITCharInfo.NumBuildings += pair.Value.NumBuildings;
                    AggRITCharInfo.Value += pair.Value.Value;

                    RiskCharIDtoAggregateID.Add(pair.Key, AggRITCharInfo.OriginalRITECharacteristicId);
                    AggregateIDtoDisaggIDs[AggRITCharInfo.OriginalRITECharacteristicId].Add(pair.Key);

                    DisaggRitCharToDisAggRIT.Add(pair.Key, pair.Value.RITExposureId);
                }
                
                AggregatedRITCharsOutput.Add(AggRITCharGroup.Key, AggRITCharInfo);                
            }

            return AggregatedRITCharsOutput;
        }

        private int GetNumOfSamplesForRITE(ExtractRITCharInfo rite)
        {
            int NumSamples;
            AggNumSamplesGenerator sampleNumGen = new AggNumSamplesGenerator(SubSamplingSettings);

            if (rite.IsAggregateLowRes)
                //NumSamples = 200;
                NumSamples = sampleNumGen.GetNumOfSamples(rite);
            else
                NumSamples = Math.Min(MaxNumSamplesPerHiResLocation, rite.NumBuildings);

            return NumSamples;
        }

        private bool IsExtractVersion2Disaggregated()
        {
            if (_contractExposure.ContractStatus== null)
                return false;

            string contractStatusString = _contractExposure.ContractStatus.ContractStatusName;

            if (contractStatusString == null)
                return false;

            string[] contractStatusSubStrings = contractStatusString.Split('_');

            if (contractStatusSubStrings[0] != "Bound" || contractStatusSubStrings.Count() > 2)
                throw new InvalidOperationException("Unknown ContractStatus Type in Extract");

            if (contractStatusSubStrings.Count() == 2 && contractStatusSubStrings[1] == "Disaggregated")
                return true;
            else
                return false;
        }

        private COLCollection GetContractCOLs()
        {
            COLCollection CausesofLoss;

            object Component;
            _contractJSON.TryGetValue("Declarations", out Component);
            Dictionary<String, Object> cdl_declarations = Component as Dictionary<String, object>;

            if (cdl_declarations.ContainsKey("CausesOfLoss"))
                CausesofLoss = new COLCollection(Convert.ToString(cdl_declarations["CausesOfLoss"]));
            else
                CausesofLoss = CauseOfLoss.GetDefaultCOLs();

            return CausesofLoss;
        }

    }

   

    
}
