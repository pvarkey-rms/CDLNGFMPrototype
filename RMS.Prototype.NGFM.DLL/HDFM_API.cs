using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;
using RMS.Prototype.NGFM;
using RMS.ContractObjectModel;

namespace RMS.Prototype.NGFM.DLL
{
    public class HDFM_API
    {
        HDFM _handleHDFM;

        #region API

        public HDFM_API()
        {
            _handleHDFM = new HDFM();
        }

        public HDFM_API(string ContractExposuresProtobufExtractDirectory)
            : this(Directory.GetFiles(ContractExposuresProtobufExtractDirectory, "*.dat")) { }

        public HDFM_API(string[] ContractExposuresProtobufExtractFiles) : this()
        {
            Prepare(ContractExposuresProtobufExtractFiles);
        }

        public void Prepare(string[] ContractExposuresProtobufExtractFiles)
        {
            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(directoryName);

            _handleHDFM.Clear();

            foreach (string file in Utilities.GetExtantFiles(ContractExposuresProtobufExtractFiles))
            {
                if (file.EndsWith(".dat"))
                {
                    PartitionData partitionData = Utilities.DeserializePartitionData(file);
                    _handleHDFM.Append(partitionData);
                }
            }
        }

        public HDFM_API(PartitionData partitionData, bool OnlyBuildCOM = false)
            : this()
        {
            Prepare(partitionData, OnlyBuildCOM);
        }

        public void Prepare(PartitionData partitionData, bool OnlyBuildCOM = false)
        {
            _handleHDFM.Clear();
            _handleHDFM.Prepare(partitionData, OnlyBuildCOM);
        }

        public Dictionary<long, ResultPosition> ExecuteFM(int eventID,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> EventOccurrenceDRs,
            bool ShouldAllocate, 
            int MaxConcurrencyContracts,
            params long[] exposureIDs)
        {
            return _handleHDFM.ProcessEvent(eventID, EventOccurrenceDRs, ShouldAllocate, MaxConcurrencyContracts, exposureIDs);
        }

        public Dictionary<long, ResultPosition> ProcessDamageRatiosFromFile(int eventID, 
            string filePath,
            bool ShouldAllocate,
            int MaxConcurrencyContracts,
            params long[] exposureIDs)
        {
            Dictionary<long, ResultPosition> results = 
                _handleHDFM.ProcessEventFile(eventID, filePath, ShouldAllocate, MaxConcurrencyContracts, exposureIDs);
            return results;
        }

        public ContractExposureData GetContractData(long conExpId)
        {
            return _handleHDFM.GetContractData(conExpId);
        }
        
        public double GetContractExposureAmount(long conExpId)
        {
            return _handleHDFM.GetContractExposureAmount(conExpId);
        }

        public void InterruptOrResetExecutionStates(params long[] ids)
        {
            _handleHDFM.InterruptOrResetExecutionStates(ids);
        }

        #endregion
    }
}
