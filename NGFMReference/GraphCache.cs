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
        private Dictionary<long, Graph> graphList;
        private PartitionDataAdpator PDataAdaptor;
        private Dictionary<long, BuildSettings> exSettings;
        private Dictionary<long, ExposureDataAdaptor> expData;

        public GraphBuildCache(PartitionDataAdpator _PDataAdaptor)
        {
            graphList = new Dictionary<long, Graph>();
            exSettings = new Dictionary<long, BuildSettings>();
            expData = new Dictionary<long, ExposureDataAdaptor>();
            PDataAdaptor = _PDataAdaptor;
        }

        public void Add(long conID, Graph contract)
        {
            if (!graphList.ContainsKey(conID))
                graphList.Add(conID, contract);
        }

        public bool GetContract(long conID, out Graph contract)
        {
            if (graphList.TryGetValue(conID, out contract))
            {
                contract.Reset();
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
                settings = new BuildSettings(GraphType.Auto);

            return settings;
        }

        public ExposureDataAdaptor GetExposure(long conID)
        {
            return PDataAdaptor.GetExposureAdaptor(conID);
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

        public BuildSettings(GraphType type)
        {
            GraphType = type;
        }

    }
}
