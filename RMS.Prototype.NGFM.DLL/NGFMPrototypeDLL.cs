using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;
using RMS.Prototype.NGFM;
using RMS.ContractObjectModel;

namespace RMS.Prototype.NGFM.DLL
{
    public class NGFMPrototypeDLL
    {
        NGFMPrototype _handleNGFMPrototype;

        #region API

        public NGFMPrototypeDLL(PartitionData partitionData)
        {
            _handleNGFMPrototype = new NGFMPrototype();

            _handleNGFMPrototype.Prepare(partitionData);
        }

        public Dictionary<long, ResultPosition> ProcessEvent(int eventID,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            bool ShouldAllocate,
            int MaxConcurrencyContracts,
            params long[] exposureIDs)
        {
            return _handleNGFMPrototype.ProcessEvent(eventID, EventOccurrenceDRs, ShouldAllocate, MaxConcurrencyContracts, exposureIDs);
        }

        public ConcurrentDictionary<long, ResultPosition> ProcessEvent_OLDAPI(int eventID,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            params long[] exposureIDs)
        {
            _handleNGFMPrototype.ProcessEvent_OLDNEWNEWAPI(eventID, EventOccurrenceDRs, exposureIDs);
            return _handleNGFMPrototype.GetResultPositions(eventID, exposureIDs);
        }

        /// <summary>
        /// Old API (non parallel for each event)
        /// </summary>
        /// <param name="lossesPerSubPeril"></param>
        /// <param name="exposureIDs"></param>
        /// <returns></returns>
        public Dictionary<long, double> ExecuteFM(
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            params long[] exposureIDs)
        {
            int eventId = 0;
            _handleNGFMPrototype.ProcessEvent_OLDNEWNEWAPI(eventId, EventOccurrenceDRs, exposureIDs);
            return _handleNGFMPrototype.GetResultPositions(eventId, exposureIDs).ToDictionary(kv => kv.Key, kv => kv.Value.PayOut);
        }

        public Dictionary<long, Dictionary<string, double>> GetRiteAllocations(long contractId)
        {
            var rall = GetRiteAllocations(0, contractId);

            if (rall != null && rall.ContainsKey(contractId))
                return rall[contractId];
            else return new Dictionary<long, Dictionary<string, double>>();
        }

        public ConcurrentDictionary<long, ResultPosition> GetResultPositions(int eventID, params long[] contractIDs)
        {
            return _handleNGFMPrototype.GetResultPositions(eventID, contractIDs);
        }

        public ConcurrentDictionary<int, ConcurrentDictionary<long, ResultPosition>> GetResultPositions()
        {
            return _handleNGFMPrototype.GetResultPositions();
        }

        public Dictionary<long, Dictionary<long, Dictionary<string, double>>> GetRiteAllocations(int eventID, params long[] contractIDs)
        {
           var rall = _handleNGFMPrototype.GetResultPositions(eventID, contractIDs);
           if (rall != null && rall.Count() > 0)
               return rall.ToDictionary(kv => kv.Key, kv => kv.Value.RITEAllocation);
           else return new Dictionary<long, Dictionary<long, Dictionary<string, double>>>();
        }

        public double GetContractExposureAmount(long conExpId)
        {
            return _handleNGFMPrototype.GetContractExposureAmount(conExpId);
        }

        public Dictionary<long, double> GetExposureValues()
        {
            return _handleNGFMPrototype.GetExposureValues();
        }

        public void InterruptOrResetExecutionStates(params long[] ids)
        {
            _handleNGFMPrototype.InterruptOrResetExecutionStates(ids);
        }

        public void RemoveResultPositions(int eventID)
        {
            _handleNGFMPrototype.RemoveResultPositions(eventID);
        }

        #endregion
    }
}
