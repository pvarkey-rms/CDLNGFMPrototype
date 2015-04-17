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
using RMS.Prototype.NGFM;


namespace NGFM.Reference.MatrixHDFM
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
        private NGFMPrototype ParsingAPI;

        public bool TreatyExposure { get; private set; }
        public HashSet<RITE> ContractRITES
        { get { return _rites; } }
        public HashSet<ScheduleOfRITEs> Schedules
        { get { return _schedules; } }
        public HashSet<RITCharacteristic> Characteristics
        { get { return _characteristics; } }
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

        public ExposureDataAdaptor(ContractExposure contractExposure, NGFMPrototype _ParsingAPI)
        {
            ParsingAPI = _ParsingAPI;
            _contractExposure = contractExposure;

            _contractJSON = GetJSONForContract();
            EDSDataExtract = new ExposureDataExtractor(_contractExposure);

            _characteristics = new HashSet<RITCharacteristic>();
            _rites = new HashSet<RITE>();
            _schedules = new HashSet<ScheduleOfRITEs>();
            _characteristicsDict = new Dictionary<long, RITCharacteristic>();
            _ritesDict = new Dictionary<long, RITE>();
            _schedulesDict = new Dictionary<string, ScheduleOfRITEs>();

            if (_contractExposure.ContractType.IsReinsuranceContract())
                TreatyExposure = true; 
            else
                TreatyExposure = false; 
        }
       
        public void GetPrimaryData()
        {
            if (TreatyExposure)
                throw new InvalidOperationException("Cannot use this method to get exposure data for reinsurance contract");

            EDSDataExtract.ExtractRiteMap();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var LocationGroups = EDSDataExtract.CoverageIdAttrMap.GroupBy(pair => pair.Value.RITExposureId);

            foreach(var LocGroup in LocationGroups)
            {
                RITE newRITE = BuildRITE(LocGroup.First());
                _rites.Add(newRITE);
                _ritesDict.Add(LocGroup.Key, newRITE);

                foreach(KeyValuePair<long, RITEAttributes> pair in LocGroup)
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
                if (EDSschedule.Value.Type == SubscheduleType.RITCHARACTERISTICS)
                    newSchedule = BuildScheduleFromRITChars2(EDSschedule.Key, Ids);
                else if (EDSschedule.Value.Type == SubscheduleType.RITEXPOSURES)
                    newSchedule = BuildScheduleFromRITs2(EDSschedule.Key, Ids);
                else
                    throw new InvalidOperationException("Cannot support building schedules of type: " + EDSschedule.Value.Type.ToString());

                _schedules.Add(newSchedule);
                _schedulesDict.Add(newSchedule.Name, newSchedule);
            }

            stopwatch.Stop();

            ////////////////////////OLD OLD OLD Version//////////////////////////////////
            //_schedules.Clear();
            //_rites.Clear();
            //_characteristics.Clear();

            //stopwatch.Reset();
            //stopwatch.Start();

            //foreach (KeyValuePair<long, RITEAttributes> RiteAttribute in EDSDataExtract.CoverageIdAttrMap)
            //{
            //    RITCharacteristic newRITChar = BuildRITCharacteristic(RiteAttribute);
            //    _characteristics.Add(newRITChar);
            //    RITE parentRITE = _rites.Where(rite => rite.ExposureID == RiteAttribute.Value.RITExposureId).FirstOrDefault();

            //    if (parentRITE != null)
            //    {
            //        parentRITE.AddCharacteristic(newRITChar);
            //    }
            //    else
            //    {
            //        RITE newRITE = BuildRITE(RiteAttribute);
            //        _rites.Add(newRITE);
            //        newRITE.AddCharacteristic(newRITChar);
            //    }

            //}

            //foreach (KeyValuePair<string, Subschedule> EDSschedule in EDSDataExtract.SubSchedule)
            //{
            //    ScheduleOfRITEs newSchedule;
            //    HashSet<long> Ids = (EDSschedule.Value.Ids != null) ? EDSschedule.Value.Ids : new HashSet<long>(EDSschedule.Value.CompressedIds.Enumerable());
            //    if (EDSschedule.Value.Type == SubscheduleType.RITCHARACTERISTICS)
            //        newSchedule = BuildScheduleFromRITChars(EDSschedule.Key, Ids);
            //    else if (EDSschedule.Value.Type == SubscheduleType.RITEXPOSURES)
            //        newSchedule = BuildScheduleFromRITs(EDSschedule.Key, Ids);
            //    else
            //        throw new InvalidOperationException("Cannot support building schedules of type: " + EDSschedule.Value.Type.ToString());

            //    _schedules.Add(newSchedule);
            //}

            //stopwatch.Stop();
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

        private RITCharacteristic BuildRITCharacteristic(KeyValuePair<long, RITEAttributes> RiteAttribute)
        {
            return new RITCharacteristic(RiteAttribute.Key, (ExposureType)RiteAttribute.Value.ExposureType, RiteAttribute.Value.Value);
        }

        private RITE BuildRITE(KeyValuePair<long, RITEAttributes> RiteAttribute)
        {
            return new RITE(RiteAttribute.Value.RITExposureId, RiteAttribute.Value.NumBuildings);
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

                long RITEID = EDSDataExtract.CoverageIdAttrMap[RITCharID].RITExposureId;
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
            if (ParsingAPI == null)
                throw new InvalidOperationException("You must set the Parsing API before using this method!");
            string CDL = _contractExposure.Contract.CDLString;
            string strJSON;
            Dictionary<string, object> jsonParseResult;
            jsonParseResult = ParsingAPI.ParseCDLUsingJISONJS(CDL);

            return jsonParseResult;
        }

        public ScheduleOfRITEs GetSchedule(string name)
        {
            ScheduleOfRITEs schedule;
            if (!_schedulesDict.TryGetValue(name, out schedule))
                throw new ArgumentOutOfRangeException("Cannot find schedule in exposure data with name: " + name);

            return schedule;
        }

        public void CombineExposure(ExposureDataAdaptor otherExposure)
        {
            this._characteristics.UnionWith(otherExposure.Characteristics);
            this._rites.UnionWith(otherExposure.ContractRITES);
            this._schedules.UnionWith(otherExposure.Schedules);
        }

    }

   

    
}
