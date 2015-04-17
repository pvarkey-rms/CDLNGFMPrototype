using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{

    public class MatrixCoverGraphAllocation
    {
        private IExecutableMatrixGraph Graph;
        private IGraphState graphState;

        ICoverState TotalCoverState { get; set; }
        ICoverAllocationState ChildrenCoverLevelAllocationState { get; set; }
        ICoverLevelNodeAggInfo ParentCoverLevelAggInfo { get; set; }
        ICoverLevelNodeAggInfo ChildrenCoverLevelAggInfo { get; set; }
        ICoverLevelNodeAggInfo LeafCoverLevelAggInfo { get; set; }

        ICoverLevelResiduleInfo ResiduleInfo { get; set; }
        ICoverLevelTermAriteInfo AriteInfo { get; set; }
        ICoverState ResiduleState { get; set; }
        ICoverState AriteState { get; set; }
        ICoverAllocationState LowestCoverLevelAriteAllocationRatio { get; set; }
        ICoverAllocationState LowestCoverLevelResidualAllocationRatio { get; set; }
        float[] TotalPayoutState { get; set; }
        float[] TotalAllocatedPayoutState { get; set; }
        int[] LeafTopCoverList { get; set; }

        //keep check the cover level position in TotalCoverState array
        int StartCoverPosition { get; set; }

        #region Constructor
        public MatrixCoverGraphAllocation(IExecutableMatrixGraph _graph, IGraphState _graphState)
        {
            Graph = _graph;
            graphState = _graphState;
        }
        #endregion

        public void Run()
        {
            TotalCoverState = graphState.GetTotalCoverState();
            TotalPayoutState = TotalCoverState.GetPayout();
            TotalAllocatedPayoutState = TotalCoverState.GetAllocatedPayout();

            int lowestCoverLevel = Graph.LowestCoverLevel;
            int NumOfCoverLevels = Graph.NumOfCoverLevels;
            int TopCoverLevel = lowestCoverLevel + NumOfCoverLevels;

            //1. top covers, simply copy (no allocation)
            ChildrenCoverLevelAggInfo = Graph.GetCoverNodeAggInfo(TopCoverLevel);
            ParentCoverLevelAggInfo = Graph.GetCoverNodeAggInfo(TopCoverLevel);
            CopyTopCoverLevelPayout();

            //2. leafTopCovers
            LeafTopCoverList = Graph.GetCoverNodeAggInfo(lowestCoverLevel + 1).GetLeafTopCoverList();
            CopyLeafTopCoverPayout();

            //3. Allocate Cover Graph Payout
            for (int i = TopCoverLevel; i > lowestCoverLevel + 1; i--)
            {
                ChildrenCoverLevelAllocationState = graphState.GetCoverLevelAllocationRatioState(i - 1);
                ChildrenCoverLevelAggInfo = Graph.GetCoverNodeAggInfo(i - 1);
                ParentCoverLevelAggInfo = Graph.GetCoverNodeAggInfo(i);
                AllocateToOneCoverLevel();
            }

            //4. Allocate Leaf Cover to lowest cover level, Atomic Rites and residual
            LowestCoverLevelAriteAllocationRatio = graphState.GetLowestCoverLevelAriteAllocationRatio();
            LowestCoverLevelResidualAllocationRatio = graphState.GetLowestCoverLevelResidualAllocationRatio();
            ResiduleInfo = Graph.GetCoverResiduleInfo();
            AriteInfo = Graph.GetCoverAriteInfo();
            ResiduleState = graphState.GetLowestCoverLevelResiduleState();
            AriteState = graphState.GetLowestCoverLevelAriteState();
            ParentCoverLevelAggInfo = Graph.GetCoverNodeAggInfo(lowestCoverLevel + 1);  //leaf cover level
            AllocateToLowestCoverLevel();
        }


        private void AllocateToLowestCoverLevel()
        {
            int[] ParentLevelChildrenMap = ParentCoverLevelAggInfo.GetChildrenMap();
            //using this ParentLevelChildrenMap will not work here, because some leaf nodes will be top nodes as well,
            //so those leaf nodes will not appear in this list
            //all other cover nodes (except the top nodes) will be in this ParentLevelChildrenMap
            //because it must aggregate to some top level cover nodes

            int parentPosition = 0; //first leaf node is indexed 0 in TotalCoverState
            int[] ResidulePartitions = ResiduleInfo.GetResiduleSubAggPartitions();
            int[] AritePartitions = AriteInfo.GetARiteAggregationPartitions();
            float[] ResiduleAllocatedArr = ResiduleState.GetAllocatedPayout();
            float[] aRiteAllocatedArr = AriteState.GetAllocatedPayout();

            float[] residualRatio = LowestCoverLevelResidualAllocationRatio.GetCoverAllocationRatioP();
            float[] aRiteRatio = LowestCoverLevelAriteAllocationRatio.GetCoverAllocationRatioP();

            int numOfParents = ResidulePartitions.Length;

            int MapPosition = 0;
            int MapPosition2 = 0;

            //loop through each parent leaf cover
            for (int i = 0; i < numOfParents; i++) //loop through each Parent cover
            {
                for (int j = MapPosition; j < AritePartitions[i]; j++)  //loop throught each child of this parent cover
                {
                    aRiteAllocatedArr[j] += TotalAllocatedPayoutState[parentPosition] * aRiteRatio[j];
                }
                MapPosition = AritePartitions[i];

                for (int j = MapPosition2; j < ResidulePartitions[i]; j++)  //loop throught each child of this parent cover
                {
                    ResiduleAllocatedArr[j] += TotalAllocatedPayoutState[parentPosition] * residualRatio[j];
                }
                MapPosition2 = ResidulePartitions[i];
                parentPosition++;
            }
        }

        private void CopyTopCoverLevelPayout()
        {
            //top level cover payouts will be copied to allocated payouts
            int[] ParentLevelChildrenMap = ParentCoverLevelAggInfo.GetChildrenMap();
            int numOfParents = ParentLevelChildrenMap.Length;

            for (int i = 0; i < numOfParents; i++) //loop through each Parent cover
            {
                TotalAllocatedPayoutState[ParentLevelChildrenMap[i]] = TotalPayoutState[ParentLevelChildrenMap[i]];
            }
        }

        private void CopyLeafTopCoverPayout()
        {
            foreach (int i in LeafTopCoverList) //loop through leaf top covers
            {
                TotalAllocatedPayoutState[i] = TotalPayoutState[i];
            }
        }

        private void AllocateToOneCoverLevel()
        {
            int[] ParentLevelChildrenMap = ParentCoverLevelAggInfo.GetChildrenMap();
            int[] ChildrenMap = ChildrenCoverLevelAggInfo.GetChildrenMap();
            int[] MapPartitions = ChildrenCoverLevelAggInfo.GetCoverAggregationPartitions();

            //int numOfParents = ParentLevelChildrenMap.Length; 
            int numOfParents = ParentCoverLevelAggInfo.NumOfNodes;
            int MapPosition = 0;

            for (int i = 0; i < numOfParents; i++) //loop through each Parent cover
            {
                for (int j = MapPosition; j < MapPartitions[i]; j++)  //loop throught each child of this parent cover
                {
                    TotalAllocatedPayoutState[ChildrenMap[j]] += TotalAllocatedPayoutState[ParentLevelChildrenMap[i]] * ChildrenCoverLevelAllocationState.GetCoverAllocationRatioP()[j];
                }
                MapPosition = MapPartitions[i];
            }
        }
    }

}
