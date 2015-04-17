using System; 
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractGraphModel
{
    [Serializable]
    [ProtoContract]
    public class GraphOperationState
    {
        [ProtoMember(1)]
        private bool HasGraphChangedSinceLastOperation = false; // or, true?

        [ProtoMember(2)]
        object[] inputs = null;

        public GraphOperationState() : this(false, null) { }

        public GraphOperationState(params object[] _inputs) : this(false, _inputs) { }

        public GraphOperationState(bool _HasGraphChangedSinceLastExecution) :
            this(_HasGraphChangedSinceLastExecution, null) { }

        public GraphOperationState(bool _HasGraphChangedSinceLastExecution, 
            params object[] _inputs)
        {
            HasGraphChangedSinceLastOperation = _HasGraphChangedSinceLastExecution;
            if (_inputs != null && _inputs.Length != 0)
            {
                inputs = new object[_inputs.Length];
                Array.Copy(_inputs, inputs, _inputs.Length);
            }
        }

        public void RegisterModificationInGraphTopology()
        {
            HasGraphChangedSinceLastOperation = true;
        }

        public bool HasGraphBeenModifiedSinceLastOperation()
        {
            return HasGraphChangedSinceLastOperation;
        }

        public bool HasOperationStateChanged()
        {
            if (HasGraphBeenModifiedSinceLastOperation())
                return true;

            return false;
        }

        #region Strongly Typed State Change API
        public bool HasOperationStateChanged(Dictionary<string, HashSet<long>> Schedule,
            Dictionary<long, Loss> CoverageIdGULossMap)
        {
            // check if inputs are valid
            if ((Schedule == null) || (CoverageIdGULossMap == null))
                throw new Exception("Input parameters cannot be null");

            if (inputs == null) // un-initialized inputs
                return true;

            if ((inputs.Length != 2) 
                || !(inputs[0] is Dictionary<string, HashSet<long>>)
                || !(inputs[1] is Dictionary<long, Loss>))
                throw new Exception("Parameters do not match state types and/or order!");

            if (HasGraphBeenModifiedSinceLastOperation())
                return true;

            if ((Schedule == null) || (Schedule.Count == 0) || (CoverageIdGULossMap == null) || (CoverageIdGULossMap.Count == 0))
                return false;

            Dictionary<string, HashSet<long>> ThisSchedule = inputs[0] as Dictionary<string, HashSet<long>>;
            Dictionary<long, Loss> ThisCoverageIdGULossMap = inputs[1] as Dictionary<long, Loss>;

            if ((ThisSchedule == null) || (ThisSchedule.Count == 0)
                || (ThisCoverageIdGULossMap == null) || (ThisCoverageIdGULossMap.Count == 0))
                return true;

            IEqualityComparer<Loss> valueComparer = EqualityComparer<Loss>.Default;

            if (!(CoverageIdGULossMap.Count == ThisCoverageIdGULossMap.Count
                    && CoverageIdGULossMap.Keys.All(key => ThisCoverageIdGULossMap.ContainsKey(key)
                    && valueComparer.Equals(CoverageIdGULossMap[key], ThisCoverageIdGULossMap[key]))))
                return true;

            if (!(Schedule.Count == ThisSchedule.Count
                    && Schedule.Keys.All(key => ThisSchedule.ContainsKey(key)
                    && Schedule[key].SetEquals(ThisSchedule[key]))))
                return true;

            return false;
        }

        public bool HasOperationStateChanged(Dictionary<long, Dictionary<string, TermAllocationPosition>> RITEAllocationAfterTermGraphExecution)
        {
            // check if inputs are valid
            if (RITEAllocationAfterTermGraphExecution == null)
                throw new Exception("Input parameter cannot be null");

            if (inputs == null) // un-initialized inputs
                return true;

            if ((inputs.Length != 1)
                || !(inputs[0] is ConcurrentDictionary<long, Dictionary<string, TermAllocationPosition>>))
                throw new Exception("Parameters do not match state types and/or order!");

            if (HasGraphBeenModifiedSinceLastOperation())
                return true;

            if ((RITEAllocationAfterTermGraphExecution == null) || (RITEAllocationAfterTermGraphExecution.Count == 0))
                return false;

            ConcurrentDictionary<long, Dictionary<string, TermAllocationPosition>> ThisRITEAllocationAfterTermGraphExecution
                = inputs[0] as ConcurrentDictionary<long, Dictionary<string, TermAllocationPosition>>;

            if ((ThisRITEAllocationAfterTermGraphExecution == null) 
                || (ThisRITEAllocationAfterTermGraphExecution.Count == 0))
                return true;

            if (!   (   
                        (RITEAllocationAfterTermGraphExecution.Count == ThisRITEAllocationAfterTermGraphExecution.Count)
                        && RITEAllocationAfterTermGraphExecution.Keys.All
                                (
                                    key => ThisRITEAllocationAfterTermGraphExecution.ContainsKey(key)
                                            && (RITEAllocationAfterTermGraphExecution[key].Count == ThisRITEAllocationAfterTermGraphExecution[key].Count)
                                            && RITEAllocationAfterTermGraphExecution[key].Keys.All
                                                (
                                                    key2 => ThisRITEAllocationAfterTermGraphExecution[key].ContainsKey(key2)
                                                             && RITEAllocationAfterTermGraphExecution[key][key2].Equals(ThisRITEAllocationAfterTermGraphExecution[key][key2])
                                                )
                                )
                     )
                )
                return true;

            return false;
        }

        public bool HasOperationStateChanged(double SubjectLoss)
        {
            if (inputs == null) // un-initialized inputs
                return true;

            if ((inputs.Length != 1)
                || !(inputs[0] is double))
                throw new Exception("Parameters do not match state types and/or order!");

            if (HasGraphBeenModifiedSinceLastOperation())
                return true;

            double ThisSubjectLoss = (double)inputs[0];

            if (ThisSubjectLoss != SubjectLoss)
                return true;

            return false;
        }
        #endregion
    }
}
