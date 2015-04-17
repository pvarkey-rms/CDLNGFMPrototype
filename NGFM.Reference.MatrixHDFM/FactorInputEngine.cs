using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class FactorInputEngine
    {
        private IGraphState graphstate;
        private IExecutableMatrixGraph graph;
        private IVectorEvent Event;

        public FactorInputEngine(IGraphState _graphstate, IExecutableMatrixGraph _graph, IVectorEvent _event)
        {
            graphstate = _graphstate;
            graph = _graph;
            Event = _event;
        }

        public void Run()
        {
            float[] FactorVector = Event.Factors;

            //this step will assgin Factor to AtomicRite, residual rites, and all nodes in that level
            //loop through each level

            //Lowest Level: only AtomicRite
            if (graph.NumOfTermLevels > 0)
                DoLowestLevel(FactorVector);

            //TermLevels
            int level = 0;
            int totalTermLevels = graph.NumOfTermLevels;
            for (level = 0; level < totalTermLevels; level++)
            {
                DoTermLevels(FactorVector, level);
            }   
            
            //Lowest Cover level, only Atomic Rites and Residual Atomic Rites
            level = graph.LowestCoverLevel;
            DoLowestCoverLevel(FactorVector);

            //for other Cover Levels, since there is one a TotalCoverState, which is formed during execution, 
            //we have to form the factors array there as the order of execution matters a lot
        }

        public void DoLowestLevel(float[] FactorVector) //only having AtomicRites
        {
            int level = graph.NumOfTermLevels;
            ILevelAtomicRITEInfo lowestaRiteInfor = graph.GetAtomicRITEInfo(level);
            int NumOfaRites = lowestaRiteInfor.NumOfARITEs;
            int[] aRiteFactorsIndex = lowestaRiteInfor.GetaRiteInfo().FactorsIndex;
            float[] aRiteFactors = graphstate.GetARITELevelState(level).GetFactors();

            //translate the FactorsIndex to Factors
            for (int i = 0; i < NumOfaRites; i++)
            {
                int uniqueIndex = aRiteFactorsIndex[i];
                if (uniqueIndex == -1)
                    aRiteFactors[i] = 1;
                else

                    aRiteFactors[i] = FactorVector[uniqueIndex];
            }
        } //DoLowestLevel

        public void DoTermLevels(float[] FactorVector, int level) //only having AtomicRites
        {
            AtomicRITELevelInfo aRiteInfor = graph.GetAtomicRITEInfo(level).GetaRiteInfo();
            SimpleLevelInfo nodeInfo = graph.GetNodeAggInfo(level).GetSimpleLevelInfo();

            //Get Rites Factors
            int NumOfaRites = aRiteInfor.Size;
            int[] aRiteFactorsIndex = aRiteInfor.FactorsIndex;
            float[] aRiteFactors = graphstate.GetARITELevelState(level).GetFactors();
            for (int i = 0; i < NumOfaRites; i++)
            {
                int uniqueIndex = aRiteFactorsIndex[i];
                if (uniqueIndex == -1)
                    aRiteFactors[i] = 1;
                else

                    aRiteFactors[i] = FactorVector[uniqueIndex];
            }

            //Get Node Factors
            int NumOfNodes = nodeInfo.Size;
            int[] nodeFactorsIndex = nodeInfo.FactorsIndex;
            float[] nodeFactors = graphstate.GetTermLevelState(level).GetFactors();
            for (int i = 0; i < NumOfNodes; i++)
            {
                int uniqueIndex = nodeFactorsIndex[i];
                if (uniqueIndex == -1)
                    nodeFactors[i] = 1;
                else

                    nodeFactors[i] = FactorVector[uniqueIndex];
            }
        }  //DoTermLevels

        public void DoLowestCoverLevel(float[] FactorVector)
        {
            int level = graph.LowestCoverLevel;
            LowestCoverLevelInfo lowestCoverLevelInfo = graph.GetLowestCoverLevelInfo();
            int[] aRiteFactorsIndex = lowestCoverLevelInfo.LeafCoveraRiteFactorsIndex;
            int[] residulaRiteFactorsIndex = lowestCoverLevelInfo.LeafCoverResidualFactorsIndex;
            float[] aRiteFactors = graphstate.GetLowestCoverLevelAriteState().GetFactors();
            float[] residualFactors = graphstate.GetLowestCoverLevelResiduleState().GetFactors();

            int aRiteNum = aRiteFactorsIndex.Count();
            int rRiteNum = residulaRiteFactorsIndex.Count();
            for (int i = 0; i < aRiteNum; i++)
            {
                int uniqueIndex = aRiteFactorsIndex[i];
                if (uniqueIndex == -1)
                    aRiteFactors[i] = 1;
                else

                    aRiteFactors[i] = FactorVector[uniqueIndex];
            }

            for (int i = 0; i < rRiteNum; i++)
            {
                int uniqueIndex = residulaRiteFactorsIndex[i];
                if (uniqueIndex == -1)
                    residualFactors[i] = 1;
                else

                    residualFactors[i] = FactorVector[uniqueIndex];
            }
        }        

    }

}

