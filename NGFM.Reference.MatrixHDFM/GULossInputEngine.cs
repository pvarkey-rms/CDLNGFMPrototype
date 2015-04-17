using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class GULossInputEngine
    {
        private IGraphState graphstate;
        private IExecutableMatrixGraph graph;
        private IVectorEvent Event;

        public GULossInputEngine(IGraphState _graphstate, IExecutableMatrixGraph _graph, IVectorEvent _event)
        {
            graphstate = _graphstate;
            graph = _graph;
            Event = _event;
        }

        public void Run()
        {
            float[] GULossVector = Event.LossVector;

            for (int level = 0; level < graph.NumOfLevels; level++)
            {
                int NumOfARTIEs = graph.GetAtomicRITEInfo(level).NumOfARITEs;
                int[] AtomicRITEIndicies = graph.GetAtomicRITEInfo(level).GetGULossIndicies();
                int uniqueIndex;
                float[] AtomicRITESubjectLoss = graphstate.GetARITELevelState(level).GetSubjectLoss();

                for (int i = 0; i < NumOfARTIEs; i++)
                {
                    uniqueIndex = AtomicRITEIndicies[i];
                    AtomicRITESubjectLoss[i] = GULossVector[uniqueIndex];
                }
            }

        }

    }

}
