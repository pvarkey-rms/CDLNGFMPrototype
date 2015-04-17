using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;
using Rms.DataServices.DataObjects;
using Rms.Analytics.Module.Common;
using System.Diagnostics;


using Noesis.Javascript;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public class ReferencePrototype : IDisposable
    {
        private List<string> PerilModelList;
        private PartitionDataAdpator PDataAdaptor;
        private GraphBuildCache GraphCache;

        public PositionData Positions;

        private long fixedGraphCount;

        #region Contructors

        public ReferencePrototype(PartitionData PD, RAPSettings _rapsettings, SubSamplingAnalysisSetting _subSamplingSettings)
        {
            InitilizePrototype(PD, _rapsettings, _subSamplingSettings);
        }

        public ReferencePrototype(PartitionData PD, RAPSettings _rapsettings, JavascriptContext JSContext, SubSamplingAnalysisSetting _subSamplingSettings)
        {
            InitilizePrototype(PD, _rapsettings, _subSamplingSettings, JSContext);
        }

        public ReferencePrototype(PartitionData PD, ModuleSettingsProvider _moduleSettings)
        {
            SetModelList(_moduleSettings);

            RAPSettings _rapsettings = GetRAPSettings(_moduleSettings);
            SubSamplingAnalysisSetting _subSamplingSettings = GetSubSamplingSettings(_moduleSettings);

            InitilizePrototype(PD, _rapsettings, _subSamplingSettings);
        }

        public ReferencePrototype(PartitionData PD, ModuleSettingsProvider _moduleSettings, JavascriptContext JSContext)
        {
            SetModelList(_moduleSettings);

            RAPSettings _rapsettings = GetRAPSettings(_moduleSettings);
            SubSamplingAnalysisSetting _subSamplingSettings = GetSubSamplingSettings(_moduleSettings);

            InitilizePrototype(PD, _rapsettings, _subSamplingSettings, JSContext);
        }

        #endregion

        public void ReferencePrepare(GraphType type = GraphType.Auto)
        {
            foreach (long ConID in PDataAdaptor.ConIds)
            {
                PrepareContract(ConID, GraphType.Auto);
            }
        }

        public void PrepareContract(long ConID, GraphType type = GraphType.Auto)
        {
            ExposureDataAdaptor expData = PDataAdaptor.GetExposureAdaptor(ConID);
            IRITEindexMapper mapper = GetMapperForContract(expData);
            GraphInfo graphInfo = GetGraph(type, expData, mapper);      
        }

        public ReferenceResultOutput Execute(long conID, GraphType type, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> guLossDict)
        {
            ExposureDataAdaptor expData = PDataAdaptor.GetExposureAdaptor(conID);
            string error;

            // Gu Loss can be for many contracts for need Dictionary of Mapper as input..
            Dictionary<long, IRITEindexMapper> indexMappers = GetMappers(expData);

            //Need to get mapper for current executing contract to build graph..
            IRITEindexMapper mapperForContract = indexMappers[conID];

            GraphInfo graphInfo = GetGraph(type, expData, mapperForContract);
            Graph graph = graphInfo.Graph;

            GULossAdaptor guLossAdaptor = new GULossAdaptor(indexMappers, guLossDict, graph.DeclarationsForAssociatedContracts, true);

            GraphExecuterAdaptor MainExecuter = new GraphExecuterAdaptor(graphInfo);

            return MainExecuter.RunExecution(guLossAdaptor); 
        }

        public ReferenceResultOutput ExecutePeriod(long conID, GraphType type, List<Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>> ListOfguLossDict)
        {
            ExposureDataAdaptor expData = PDataAdaptor.GetExposureAdaptor(conID);
            string error;

            // Gu Loss can be for many contracts for need Dictionary of Mapper as input..
            Dictionary<long, IRITEindexMapper> indexMappers = GetMappers(expData);

            //Need to get mapper for current executing contract to build graph..
            IRITEindexMapper mapperForContract = indexMappers[conID];

            GraphInfo graphInfo = GetGraph(type, expData, mapperForContract);
            Graph graph = graphInfo.Graph;

            //Output object
            ReferenceResultOutput ResultOutput = new ReferenceResultOutput(0, 0);

            foreach (Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>
                            guLossDict in ListOfguLossDict)
            {
                //Execute each event
                graph.EventReset();

                GULossAdaptor guLossAdaptor = new GULossAdaptor(indexMappers, guLossDict, graph.DeclarationsForAssociatedContracts, true);

                GraphExecuterAdaptor MainExecuter = new GraphExecuterAdaptor(graphInfo);

                ResultOutput += MainExecuter.RunExecution(guLossAdaptor);
            }

            return ResultOutput;
        }

        public ReferenceResultOutput Execute(long conID, GraphType type, IVectorEvent guLossEvent)
        {
            //ExposureDataAdaptor expData = PDataAdaptor.GetExposureAdaptor(conID);
            //string error;

            //IRITEindexMapper indexMapper = GetMapper(expData);

            //GraphInfo graphInfo = GetGraph(type, expData, indexMapper);
            //Graph graph = graphInfo.Graph;

            //GULossAdaptor guLossAdaptor = new GULossAdaptor(indexMapper, guLossEvent, graph.Declarations, true);

            //GraphExecuterAdaptor MainExecuter = new GraphExecuterAdaptor(graphInfo);

            //return MainExecuter.RunExecution(guLossAdaptor);      
     
            throw new NotSupportedException("Reference Prototype no longer supports Vector type input!");
        }

        private void InitilizePrototype(PartitionData PD, RAPSettings _rapsettings, SubSamplingAnalysisSetting _subSamplingSettings)
        {
            PDataAdaptor = new PartitionDataAdpator(PD, _subSamplingSettings);
            Positions = new PositionData();
            GraphCache = new GraphBuildCache(PDataAdaptor, _rapsettings);
        }

        private void InitilizePrototype(PartitionData PD, RAPSettings _rapsettings, SubSamplingAnalysisSetting _subSamplingSettings, JavascriptContext JSContext)
        {
            PDataAdaptor = new PartitionDataAdpator(PD, JSContext, _subSamplingSettings);
            Positions = new PositionData();
            GraphCache = new GraphBuildCache(PDataAdaptor, _rapsettings);
        }

        private GraphInfo GetGraph(GraphType type, ExposureDataAdaptor expData, IRITEindexMapper mapper)
        {
            GraphBuilder builder = new GraphBuilder(GraphCache);
            return builder.MakeGraph(type, expData, mapper);
        }

        private Dictionary<long, IRITEindexMapper> GetMappers(ExposureDataAdaptor expData)
        {
            Dictionary<long, IRITEindexMapper> indexMappers = new Dictionary<long,IRITEindexMapper>();

            if (expData.TreatyExposure)
            {
                foreach (long conID in expData.Positions.GetDependentContracts())
                {
                    ExposureDataAdaptor childContractData = PDataAdaptor.GetExposureAdaptor(conID);
                    IRITEindexMapper childMapper = GetMapperForContract(childContractData);
                    indexMappers.Add(childContractData.ContractID, childMapper);
                }
                indexMappers.Add(expData.ContractID, null);
            }
            else
            {
                IRITEindexMapper Mapper = GetMapperForContract(expData);
                indexMappers.Add(expData.ContractID, Mapper);
            }

            return indexMappers;
        }

        private IRITEindexMapper GetMapperForContract(ExposureDataAdaptor expData)
        {
            IRITEindexMapper indexMapper;

            if (GraphCache.GetIndexMapper(expData.ContractID, out indexMapper))
                return indexMapper;
            else
            {
                RAPSettings rapsettings = GraphCache.GetSettings(expData.ContractID).RAPsettings;
                indexMapper = new RITEmapper1(expData, rapsettings, new RMSSubPerilConfig());
                GraphCache.Add(expData.ContractID, indexMapper);
                return indexMapper;
            }
        }

        public void LoadGraphBuildSettings(Dictionary<long, BuildSettings> settings)
        {
            foreach (KeyValuePair<long, BuildSettings> setting in settings)
            {
                GraphCache.AddBuildSetting(setting.Key, setting.Value);
            }
        }

        public void AddBuildSettings(long id, BuildSettings setting)
        {
            GraphCache.AddBuildSetting(id, setting);
        }

        public void AddBuildSettings(long id, GraphType type, RAPSettings settings)
        {
            GraphCache.AddBuildSetting(id, new BuildSettings(type, settings));
        }

        private RAPSettings GetRAPSettings(ModuleSettingsProvider settings)
        {
            //Get All SubPerils for All Models that are in teh Peril DLM analysis...
            HashSet<string> AllRMSSubPerils = new HashSet<string>();

            foreach (string ModelName in PerilModelList)
            {
                List<string> subPerilsForModel;
                if (settings.TryGetList("SubPerils", ModelName, out subPerilsForModel))
                {
                    AllRMSSubPerils.UnionWith(subPerilsForModel);
                }
            }

            return new RAPSettings(AllRMSSubPerils);
        }

        private SubSamplingAnalysisSetting GetSubSamplingSettings(ModuleSettingsProvider settings)
        {
            //Get All SubSampling Settings for Model in the analysis...

            string ModelName = PerilModelList[0];

            bool UseSubSampling;
            double NmbrSampleBldgScaleFactor;
            int MinSampledBldgs;
            int MaxSampledBldgs;
            bool UseScaleFromExtract;
            bool UseIncompleteExtracts;
            bool SubSampleWithReplacement;
            string EventWeightsFile;
            string OriginWeightsFile;

            //Get UseSubSampling
            if (!settings.TryGet("UseSubSampling", out UseSubSampling))
            {
                UseSubSampling = false;
            }

            //Get NmbrSampleBldgScaleFactor
            if (!settings.TryGet("NmbrSampleBldgScaleFactor", out NmbrSampleBldgScaleFactor))
            {
                NmbrSampleBldgScaleFactor = 1;
            }

            //Get MinSampledBldgs
            if (!settings.TryGet("MinSampledBldgs", out MinSampledBldgs))
            {
                MinSampledBldgs = 0;
            }

            //Get UseScaleFromExtract
            if (!settings.TryGet("UseScaleFromExtract", out UseScaleFromExtract))
            {
                UseScaleFromExtract = false;
            }

            //Get UseIncompleteExtracts
            if (!settings.TryGet("UseIncompleteExtracts", out UseIncompleteExtracts))
            {
                UseIncompleteExtracts = false;
            }

            //Get SubSampleWithReplacement
            if (!settings.TryGet("SubSampleWithReplacement", out SubSampleWithReplacement))
            {
                SubSampleWithReplacement = false;
            }

            //Get MaxSampledBldgs
            if (!settings.TryGet("MaxSampledBldgs", out MaxSampledBldgs))
            {
                MaxSampledBldgs = int.MaxValue;
            }

            //Get EventWeightsFile
            if (!settings.TryGet("EventWeightsFile", out EventWeightsFile))
            {
                EventWeightsFile = String.Empty;
            }

            //Get UseSubSampling
            if (!settings.TryGet("OriginWeightsFile", out OriginWeightsFile))
            {
                OriginWeightsFile = String.Empty;
            }


            return new SubSamplingAnalysisSetting(UseSubSampling, NmbrSampleBldgScaleFactor, MinSampledBldgs, MaxSampledBldgs, EventWeightsFile, OriginWeightsFile);
        }

        private void SetModelList(ModuleSettingsProvider settings)
        {
            List<string> ModelList;

            if (settings.TryGetList("Models", out ModelList))
            {
                if (ModelList.Count > 1)
                    throw new NotSupportedException("Current Reference Prototype cannot handle multi-model DLM runs");

                PerilModelList = ModelList;
            }
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
                PDataAdaptor.Dispose();
            }

            // Free any unmanaged objects here. 
            
            disposed = true;
        }

        ~ReferencePrototype()
        {
            Dispose(false);
        }


        #endregion

    }

    public class RAPSettings
    {
        public HashSet<string> SubPerils {private set; get;}

        public RAPSettings(HashSet<string> _subPerils)
        {
            SubPerils = _subPerils;
        }

    }

}
