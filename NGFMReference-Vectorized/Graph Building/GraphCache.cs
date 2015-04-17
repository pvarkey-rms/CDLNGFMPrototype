using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;



namespace NGFMReference
{
    public class GraphBuildCache
    {
        private Dictionary<long, GraphInfo> graphList;
        private PartitionDataAdpator PDataAdaptor;
        private Dictionary<long, BuildSettings> exSettings;
        private Dictionary<long, ExposureDataAdaptor> expData;
        private Dictionary<long, IRITEindexMapper> indexMappings;
        private BuildSettings DefualtBuildSetting;

        public GraphBuildCache(PartitionDataAdpator _PDataAdaptor, RAPSettings rapSettings)
        {
            graphList = new Dictionary<long, GraphInfo>();
            exSettings = new Dictionary<long, BuildSettings>();
            expData = new Dictionary<long, ExposureDataAdaptor>();
            indexMappings = new Dictionary<long, IRITEindexMapper>();
            DefualtBuildSetting = new BuildSettings(GraphType.Auto, rapSettings);
            PDataAdaptor = _PDataAdaptor;
        }

        public void Add(long conID, GraphInfo contract)
        {
            if (!graphList.ContainsKey(conID))
                graphList.Add(conID, contract);
        }

        public void Add(long conID, IRITEindexMapper mapper)
        {
            if (!indexMappings.ContainsKey(conID))
                indexMappings.Add(conID, mapper);
        }

        public bool GetGraphInfo(long conID, out GraphInfo contract)
        {
            if (graphList.TryGetValue(conID, out contract))
            {
                contract.Graph.PeriodReset();
                return true;
            }
            else
                return false;

                //GraphBuilder builder = new GraphBuilder();
                //ExposureDataAdaptor expData = new ExposureDataAdaptor(_pd, conID);

                //if (exSettings.ContainsKey(conID))
                //{
                //    contract = builder.MakeGraph(exSettings[conID].GraphType, expData);
                //    Add(conID, contract);
                //    return contract;
                //}
                //else
                //    throw new InvalidOperationException("Cannot find execution settings for contract: " + conID);
            
        }

        public BuildSettings GetSettings(long conID)
        {
            BuildSettings settings;
            if (!exSettings.TryGetValue(conID, out settings))
                settings = DefualtBuildSetting;

            return settings;
        }

        public ExposureDataAdaptor GetExposure(long conID)
        {
            return PDataAdaptor.GetExposureAdaptor(conID);
        }

        public bool GetIndexMapper(long conID, out IRITEindexMapper mapper)
        {
            if (indexMappings.TryGetValue(conID, out mapper))
            {
                return true;
            }
            else
                return false;

        }

        //public Graph GetContract(long conID, GraphType type)
        //{
        //    Graph contract;
        //    if (graphList.TryGetValue(conID, out contract))
        //        return contract;
        //    else
        //    {
        //        GraphBuilder builder = new GraphBuilder(this);
        //        ExposureDataAdaptor expData = new ExposureDataAdaptor(_pd, conID);

        //        AddBuildSetting(conID, new BuildSettings(type));
        //        contract = builder.MakeGraph(type, expData);
        //        Add(conID, contract);
        //        return contract;            
        //    }
        //}

        public void AddBuildSetting(long conID, BuildSettings settings)
        {
            if (!exSettings.ContainsKey(conID))
                exSettings.Add(conID, settings);
            else
                exSettings[conID] = settings;
        }

    }

    public class BuildSettings
    {
        public GraphType GraphType { get; set; }
        public RAPSettings RAPsettings { get; set; }

        public BuildSettings(GraphType type, RAPSettings _RAPsettings)
        {
            GraphType = type;
            RAPsettings = _RAPsettings;
        }

    }
}
