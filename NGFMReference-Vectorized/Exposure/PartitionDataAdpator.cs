using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;

using Rms.Analytics.DataService.Zip;
using Rms.DataServices.DataObjects;

using Noesis.Javascript;

namespace NGFMReference
{
    public class PartitionDataAdpator : IEnumerable<ExposureDataAdaptor>, IDisposable
    {
        private PartitionData PData; 
        private Dictionary<long, ExposureDataAdaptor> expData;
       // private NGFMPrototype NGFM_API;
        private JavascriptParser JSParser;
        private SubSamplingAnalysisSetting SubSamplingSettings;

        //public PartitionDataAdpator(PartitionData _PData)
        //{
        //    PData = _PData;
        //    expData = new Dictionary<long, ExposureDataAdaptor>();
        //    UseJSContext = false;
        //    NGFM_API = new NGFMPrototype();
        //    NGFM_API.BuildJSContext();
        //}

        //When javascript context is directly available       
        public PartitionDataAdpator(PartitionData _PData, JavascriptContext _JSContext, SubSamplingAnalysisSetting _subSamplingSettings)
        {
            PData = _PData;
            expData = new Dictionary<long, ExposureDataAdaptor>();

            JSParser = new JavascriptParser(_JSContext);
            SubSamplingSettings = _subSamplingSettings;
        }

        public PartitionDataAdpator(PartitionData _PData, SubSamplingAnalysisSetting _subSamplingSettings)
        {
            PData = _PData;
            expData = new Dictionary<long, ExposureDataAdaptor>();

            JSParser = new JavascriptParser(GetParserSettings());
            SubSamplingSettings = _subSamplingSettings;

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
                    exposure = new ExposureDataAdaptor(contractExposure, JSParser, SubSamplingSettings);
                    exposure.GetPrimaryData();
                }

                expData.Add(conID, exposure);           
            }

            return exposure;
        }

        private JavascriptParserSettings GetParserSettings()
        {
            string underscore_js;
            string grammar_ast_js;

            underscore_js = ConfigurationManager.AppSettings["underscore_js"];
            if (File.Exists(underscore_js))
            {
                FileInfo fi = new FileInfo(underscore_js);
                underscore_js = fi.FullName;
            }
            else
                throw new Exception(underscore_js + " : File Not Found!");

            grammar_ast_js = ConfigurationManager.AppSettings["cdl2js_ir_script"];
            if (File.Exists(grammar_ast_js))
            {
                FileInfo fi = new FileInfo(grammar_ast_js);
                grammar_ast_js = fi.FullName;
            }
            else
                throw new Exception(grammar_ast_js + " : File Not Found!");

            return new JavascriptParserSettings(underscore_js, grammar_ast_js);
        }

        private ExposureDataAdaptor GetTreatyExposureData(ContractExposure contractExposure)
        {
            ExposureDataAdaptor treatyAdaptor = new ExposureDataAdaptor(contractExposure, JSParser, SubSamplingSettings);

            treatyAdaptor.ExtractPositionData();

            foreach (long conID in treatyAdaptor.Positions.GetDependentContracts())
            {
                ExposureDataAdaptor childContractData = GetExposureAdaptor(conID);
                treatyAdaptor.AddExposureToTreaty(childContractData);
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

        public IEnumerator<ExposureDataAdaptor> GetEnumerator()
        {
            foreach (ContractExposure conExp in PData.Exposures)
            {
                // Return the current element and then on next function call 
                // resume from next element rather than starting all over again;
                yield return GetExposureAdaptor(conExp.ExposureID);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            // Lets call the generic version here
            return this.GetEnumerator();
        }

        public List<long> ConIds { get { return PData.Exposures.Select(exp => exp.ExposureID).ToList(); } }

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
                //NGFM_API.DisposeJSContext();
                //NGFM_API.Dispose();
                JSParser.Dispose();
            }

            // Free any unmanaged objects here. 
            
            disposed = true;
        }

        ~PartitionDataAdpator()
        {
            Dispose(false);
        }


        #endregion

    }
}
