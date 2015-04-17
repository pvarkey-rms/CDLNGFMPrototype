using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;
using Rms.DataServices.DataObjects;
using RMS.Prototype.NGFM;

namespace NGFMReference
{
    public class PartitionDataAdpator
    {
        private PartitionData PData; 
        private Dictionary<long, ExposureDataAdaptor> expData;
        private static NGFMPrototype NGFM_API;

        public PartitionDataAdpator(PartitionData _PData)
        {
            PData = _PData;
            expData = new Dictionary<long,ExposureDataAdaptor>();
        }

        static PartitionDataAdpator()
        {
            NGFM_API = new NGFMPrototype();
        }

        public ExposureDataAdaptor GetExposureAdaptor(long conID)
        {
            ExposureDataAdaptor exposure;
            if (!expData.TryGetValue(conID, out exposure))
            {
                ContractExposure contractExposure = GetExposure(conID);
                if (contractExposure.ContractType.IsReinsuranceContract())
                    exposure = GetTreatyExposureData(contractExposure);
                else
                {
                    exposure = new ExposureDataAdaptor(contractExposure, NGFM_API);
                    exposure.GetPrimaryData();
                }

                expData.Add(conID, exposure);           
            }

            return exposure;
        }

        private ExposureDataAdaptor GetTreatyExposureData(ContractExposure contractExposure)
        {
            ExposureDataAdaptor treatyAdaptor = new ExposureDataAdaptor(contractExposure, NGFM_API);

            treatyAdaptor.ExtractPositionData();

            foreach (long conID in treatyAdaptor.Positions.GetDependentContracts())
            {
                ExposureDataAdaptor childContractData = GetExposureAdaptor(conID);
                treatyAdaptor.CombineExposure(childContractData);
            }

            return treatyAdaptor;
        }

        private ContractExposure GetExposure(long conID)
        {
            ContractExposure contractExposure;
            contractExposure = PData.Exposures.Where(exp => exp.ExposureID == conID).FirstOrDefault();

            if (contractExposure == null)
                throw new ArgumentOutOfRangeException("Cannot find contracts with id: " + conID + " in Partition Data.");
            else
                return contractExposure;
        }
    }
}
