using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.DataServices.DataObjects;
using Rms.Analytics.DataService.Zip;
using RMS.ContractObjectModel;
using Rms.DataServices.DataObjects.CDL;
using Rms.Cdl.Backend.DataObjects;
using Rms.DataServices.Common;

namespace NGFMReference
{
    class ExposureDataExtractor
    {

        private ContractExposure ConExp;

        public StringBuilder BugMessage { set; get; }
        public void BugLog(string msg)
        {
            BugMessage.Append("\n\r\t " + msg);
        }
        public void ClearBugLog(string msg = "")
        {
            BugMessage = new StringBuilder();
            BugMessage.Append(msg);
        }
        public string GetBugLog() { return (null != BugMessage) ? BugMessage.ToString() : ""; }
        // map: ExposureId -> Parameters
        public Dictionary<long, ExtractRITCharInfo> CoverageIdAttrMap { protected set; get; }

        public bool AreMapsExtracted()
        {
            return (null != CoverageIdAttrMap && CoverageIdAttrMap.Count() > 0)
                && (null != ResolvedSchedule && ResolvedSchedule.Count() > 0);
        }

        public Dictionary<string, Subschedule> SubSchedule { protected set; get; }
        // map: scheduleName -> set of coverageIds
        public Dictionary<string, HashSet<long>> ResolvedSchedule { protected set; get; }
        // map: ScheduleSymbol -> binaryRite
        //public Dictionary<string, BinaryRITE> BinarySchedule { protected set; get; }
        //public BinaryRITE GetBinaryRiteIds()
        //{
        //    BinaryRITE res = null;
        //    if (null != BinarySchedule)
        //        res = BinarySchedule.Select(kvp2 => kvp2.Value).Aggregate((a, b) => a + b);
        //    return res;
        //}
        public HashSet<long> GetRiteIds()
        {
            //if (null != BinarySchedule)
            //    return GetBinaryRiteIds().GetElements();
            //else return new HashSet<long>();

            return MergeValues(ResolvedSchedule);
        }


        public ExposureDataExtractor(ContractExposure _contractExposure)
        {
            ConExp = _contractExposure;
            BugMessage = new StringBuilder();
        }

        private HashSet<T> MergeValues<K, T>(Dictionary<K, HashSet<T>> dict)
        {
            if (null != dict && dict.Count() > 0)
                return dict.Where(elem => null != elem.Value)
                    .Select(kv => kv.Value)
                    .Aggregate(new HashSet<T>(), (a, b) => { if (null != b) a.UnionWith(b); return a; });
            else return new HashSet<T>();
        }

        #region Maps

        private ExposureType GetExpType(RiteType.ERiteType type)
        {
            return (ExposureType)((int)type);
        }

        public void ExtractRiteMap()
        {
            if (AreMapsExtracted())
                return;
            try
            {
                bool flag0 = false, flag1 = false, flag2 = false;

                CoverageIdAttrMap = null;

                if (ConExp != null)
                {
                    #region Extraction of Subschedule and preparation of ResolvedSchedule
                    if (null != ConExp.Subschedules)
                    {
                        var ser = new ProtoCompressedSerializer();
                        Dictionary<string, Subschedule> dss =
                            ser.Deserialize<Dictionary<string, Subschedule>>(ConExp.Subschedules, 0, ConExp.Subschedules.Length);
                        if (null != dss)
                        {
                            SubSchedule = dss.Where(p => p.Value != null && p.Value.Type != null && (p.Value.Ids != null || p.Value.CompressedIds != null))
                                .ToDictionary(p => p.Key, p => p.Value);

                            ResolvedSchedule = SubSchedule.ToDictionary(p => p.Key,
                                p => ((p.Value.Ids != null) ? p.Value.Ids : new HashSet<long>(p.Value.CompressedIds.Enumerable())));
                        }

                        if (ResolvedSchedule == null || ResolvedSchedule.Count() == 0)
                            BugLog("Error: Contract Exposure Schedules couldn't be deserialized.");
                    }
                    else
                        BugLog("Error: Contract Exposure doesn't contain Schedules.");
                    #endregion

                    #region Extraction of Rite Attributes
                    if (ConExp.ContractSubjectExposures != null)
                    {
                        flag0 = true;
                        FactorGenerator generator = new FactorGenerator();

                        foreach (ContractSubjectExposureOfRiteSchedule cseRites in ConExp.ContractSubjectExposures)
                        {
                            if (null != cseRites.RITECollectionExposure && null != cseRites.RITECollectionExposure.RITExposures)
                            {
                                flag1 = true;

                                foreach (RITExposure ritExposure in cseRites.RITECollectionExposure.RITExposures)
                                {
                                    if (ritExposure.RiskitemCharacteristicsList != null && ritExposure.RiskitemCharacteristicsList.Items != null)
                                    {
                                        flag2 = true;
                                        foreach (RiskItemCharacteristicsValuation idxEntry in ritExposure.RiskitemCharacteristicsList.Items)
                                        {
                                            if (null == CoverageIdAttrMap)
                                                CoverageIdAttrMap = new Dictionary<long, ExtractRITCharInfo>();

                                            if (!CoverageIdAttrMap.ContainsKey(idxEntry.Id))
                                            {
                                                var P = new ExtractRITCharInfo();
                                                P.ExposureType = idxEntry.RiteTypeId;
                                                P.NumBuildings = ritExposure.CommonCharacteristics.NumBuildings;
                                                P.RITExposureId = ritExposure.ExposureID;
                                                P.Value = idxEntry.RITExposureValuationList.First().Value;
                                                P.OriginalRITECharacteristicId = idxEntry.ParentId;
                                                P.NumSamples = generator.GetFactor(ritExposure).NumOfSamples;
                                                //P.NumSamples = generator.GetNumOfSamples(ritExposure);                                                
                                                P.Factor = generator.GetFactor(ritExposure).Factor;

                                                //needed for NumSamples algorithm
                                                P.NumUnits = ritExposure.CommonCharacteristics.NumUnits;
                                                P.HazardScaleFactor = ritExposure.CommonCharacteristics.TaxValue;
                                                P.OriginWeightsGeoIDLookupIntCode = ritExposure.CommonCharacteristics.FireAlarm;
                                                P.OriginWEightsLOBLookupCode = ritExposure.CommonCharacteristics.FirePlace;
                                                P.DisAggResolutionIntCode = ritExposure.CommonCharacteristics.NumberOfEscalators;
                                                P.Address = ritExposure.Address;
                                                //--

                                                if (ritExposure.ClonedExposureId != null)
                                                    P.OriginalRITExposureId = (long)ritExposure.ClonedExposureId;
                                                //else
                                                //    throw new InvalidOperationException("Clone Exposure ID is null!");
                                                CoverageIdAttrMap.Add(idxEntry.Id, P);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else BugLog("Error: Contract Exposure Subject isn't exist.");
                    #endregion
                }

                if (!flag0) BugLog("Error: Contract Exposure data wasn't uploaded from extract file.");
                else if (!flag1) BugLog("Error: Contract Exposure Risk Item Collection isn't exist.");
                else if (!flag2) BugLog("Error: Contract Exposure Risk Item Characteristics List isn't exist.");

                #region Check
                //HashSet<long> coverageIDs1 = null, coverageIDs2 = null;

                //if (null != ResolvedSchedule)
                //    coverageIDs1 =
                //        ResolvedSchedule.Aggregate(new HashSet<long>(), (a, b) => { a.UnionWith(b.Value); return a; });
                //if (null != RiskItemCharacteristicIDAttributeMap)
                //    coverageIDs2 = new HashSet<long>(RiskItemCharacteristicIDAttributeMap.Keys);

                //int n1 = (null != coverageIDs1) ? coverageIDs1.Count() : 0;
                //int n2 = (null != coverageIDs2) ? coverageIDs2.Count() : 0;
                //if (n1 != n2)
                //{
                //    string msg = string.Format("Number of RITEs({0}) in Subschedules is not equal to number of RITEs({1}) in Collection of ContractSubjectExposures", n1, n2);
                //    throw new Exception(msg);
                //}
                #endregion
            }
            catch (Exception ex) { BugLog("Error: " + ex.Message); }
        }
        #endregion
    }
}
