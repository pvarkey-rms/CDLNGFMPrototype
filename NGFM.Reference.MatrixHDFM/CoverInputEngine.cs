using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    //For covers:
    public class CoverInputEngine
    {
        private IGraphState graphstate;
        private IExecutableMatrixGraph graph;
        private IVectorEvent Event;

        public CoverInputEngine(IGraphState _graphstate, IExecutableMatrixGraph _graph, IVectorEvent _event)
        {
            graphstate = _graphstate;
            graph = _graph;
            Event = _event;
        }

        public void Run()
        {
            //Get GULoss for Residule subjects for the lowest cover level
            float[] GULossVector = Event.LossVector;

            int NumOfResidules = graph.GetCoverResiduleInfo().NumOfResidules;
            int[] ResiduleIndicies = graph.GetCoverResiduleInfo().GetGULossIndicies();
            int uniqueIndex;
            float[] ResiduleSubjectLoss = graphstate.GetLowestCoverLevelResiduleState().GetSubjectLoss();

            for (int i = 0; i < NumOfResidules; i++)
            {
                uniqueIndex = ResiduleIndicies[i];
                ResiduleSubjectLoss[i] = GULossVector[uniqueIndex];
            }

            //Get Recoverable for Arite subjects for the lowest cover level
            float[] AriteSubjectLoss = graphstate.GetLowestCoverLevelAriteState().GetSubjectLoss();
            int NumOfArites = graph.GetCoverAriteInfo().NumOfARITEs;
            int[] RecoverLevelIndex = graph.GetCoverAriteInfo().GetRecoverableLevelIndex();
            int[] RecoverIndices = graph.GetCoverAriteInfo().GetRecoverableIndicies();
            int[] AriteGULosses = new int[NumOfArites];

            for (int i = 0; i < NumOfArites; i++)
            {
                AriteSubjectLoss[i] = graphstate.GetARITELevelState(RecoverLevelIndex[i]).GetRecoverable()[RecoverIndices[i]];
                AriteGULosses[i] = graph.GetAtomicRITEInfo(RecoverLevelIndex[i]).GetGULossIndicies()[RecoverIndices[i]];
            }
        }
    }
    
   
}
