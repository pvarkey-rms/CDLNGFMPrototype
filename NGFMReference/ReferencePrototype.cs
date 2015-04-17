using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rms.Analytics.DataService.Zip;
using Rms.DataServices.DataObjects;
using System.Diagnostics;

namespace NGFMReference
{
    public class ReferencePrototype
    {
        private PartitionDataAdpator PDataAdaptor;
        private GraphBuildCache GraphCache;
        public PositionData Positions;

        private long fixedGraphCount;

        public ReferencePrototype(PartitionData PD)
        {
            PDataAdaptor = new PartitionDataAdpator(PD);
            Positions = new PositionData();
            GraphCache = new GraphBuildCache(PDataAdaptor);
        }

        public double Execute(long conID, GraphType type, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> guLoss)
        {
            ExposureDataAdaptor expData = PDataAdaptor.GetExposureAdaptor(conID);
            string error;

            //Stopwatch watch = new Stopwatch();
            //watch.Start();
            Graph graph = GetGraph(type, expData);
            //watch.Stop();

            //long graphTime = watch.ElapsedMilliseconds;
            
            GraphExecuter Executer;
            if (graph is PrimaryGraph)
                Executer = new PrimaryGraphExecuter(graph as PrimaryGraph);
            else if (graph is TreatyGraph)
                Executer = new TreatyGraphExecuter(graph as TreatyGraph);
            else
                throw new NotSupportedException("Can only handle graph of type Treaty and Primary");

            //Execute Graph and Allocate graph    
            double payout = Executer.Execute(guLoss);

            //Allocate Graph
            //GraphAllocation Allocater = new GraphAllocation(graph);
            //Allocater.AllocateGraph();

            return payout;
        }

        private Graph GetGraph(GraphType type, ExposureDataAdaptor expData)
        {
            GraphBuilder builder = new GraphBuilder(GraphCache);
            return builder.MakeGraph(type, expData);
        }
       
        //public void SetPosition(string position, HashSet<long> contractIDs, PartitionData primaryPD)
        //{
        //    throw new NotSupportedException("Im tired");
        //}

        //public void SetPosition(string position, Dictionary<long, GraphType> contracts, PartitionData primaryPD)
        //{

        //}

        //public void LoadPositions(int conIndex)
        //{
        //    //ContractExposure contractExposure = _pd.Exposures[conIndex];
        //    //if (contractExposure != null   && contractExposure.Positions != null  )
        //    //{
        //    //     contractExposure.Positions[0].
        //    //        .Where(elem => null != elem.)
        //    //        .ToDictionary(elem => elem.PositionName, elem => new HashSet<long>(elem.LossSourcePositionIDs.ToArray()));
        //    //}

        //    //HashSet<Graph> graphsForPosition = new HashSet<Graph>();

        //    //foreach (Position position in contractExposure.Positions)
        //    //{
        //    //    foreach(long conID in position.LossSourcePositionIDs)
        //    //    {
        //    //        ExposureDataAdaptor expData = new ExposureDataAdaptor(_pd, conID);
        //    //        graphsForPosition.Add(GetGraph(Index_Type.Value, expData));

        //    //    }



        //    //}

        //    //Positions.Add(position, graphsForPosition);
                
            //}

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

        public void AddBuildSettings(long id, GraphType type)
        {
            GraphCache.AddBuildSetting(id, new BuildSettings(type));
        }

        public void InputLossForGraph(long ID, LossTimeSeries series)
        {
            Graph Contract;

            if (GraphCache.GetContract(ID, out Contract))
            {
                //Contract.PayoutTimeSeries = series;
                Contract.IsExecuted = true;
            }
            else
                throw new InvalidOperationException("Graph for contract: " + ID + " must be built before setting Losses");
        }
    }

}
