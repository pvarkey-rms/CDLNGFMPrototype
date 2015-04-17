using System; 
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using RMS.ContractObjectModel;
using Rms.Utilities;

namespace RMS.ContractGraphModel
{
    [Serializable]
    [ProtoContract]
    class TermGraph_Experimental : TermGraph
    {
        #region Fields
        #endregion
        
        #region Overlap Fields
        #endregion

        #region Constructors
        public TermGraph_Experimental() : base()
        {
        }

        public TermGraph_Experimental(TermGraph_Experimental CopyFromThisTermGraph)
            : base(CopyFromThisTermGraph)
        {
        }
        #endregion

        #region Graph Methods

        public bool Add(ITerm<Value> Term, Dictionary<long, RiskItemCharacteristicIDAttributes> CoverageIdAttrMap,
            Dictionary<string, HashSet<long>> ResolvedSchedule, bool CheckForPerRisk = true)
        {
            Subject NodeIdentity = Term.GetSubject();

            bool IsAddSuccessful = true;

            if (NodeIdentity == null)
                NodeIdentity = EMPTYSUBJECTCONSTRAINT;

            TermCollection TermCollection = (IdentityMap.ContainsKey(NodeIdentity) ? 
                IdentityMap[NodeIdentity].GetContent() : new TermCollection(NodeIdentity));

            // Switching off the following check intentionally, in order to allow redundant terms
            //if (TermCollection.Contains(Term))
            //    throw new ArgumentException("A term node with the same identity (i.e. subject) already contains this term in its collection!");

            IsAddSuccessful = TermCollection.Add(Term);

            if (IsAddSuccessful && !IdentityMap.ContainsKey(NodeIdentity))
            {
                TermNode _TermNode = new TermNode(TermCollection);

                IsAddSuccessful &= base.Add(_TermNode);

                if (IsAddSuccessful)
                {
                    IdentityMap.Add(NodeIdentity, _TermNode);
                    // A newly added term node (i.e. with no parent or child links) is both a root and a leaf, trivially.
                    IsAddSuccessful &= RootNodes.Add(_TermNode);
                    ExecutionState.RegisterModificationInGraphTopology();
                }
            }

            return IsAddSuccessful;
        }

        // TODO : are these values important
        enum SubjectComparisonOutcome
        {
            Child = -1,
            Equal = 100,
            NA = 0,
            Parent = 1,
            Overlap = 2,
            Overlap_Parent = 3,
            Overlap_Child = 4,
            Disjoint = 5
        }

        SubjectComparisonOutcome[,] OriginalSubjectRelationshipMatrix;
        List<TermNode> OriginalPerRiskNodes;
        HashSet<TermNode> DistinctPerRiskNodes;
        HashSet<TermNode> OriginalNonPerRiskNodes;
        HashSet<TermNode> FinalNodes;
        Dictionary<long, TermCollection>[] RiskItemToTermCollectionAdditions;

        public bool Rebuild()
        {
            Initialize();

            bool IsRebuildSuccessful = true;

            ExecutionState.RegisterModificationInGraphTopology();

            return IsRebuildSuccessful;
        }

        private void Initialize()
        {
            long MaxSubjectIDBeforePerRiskExplosion = 0;

            foreach (TermNode ATermNode in this)
            {
                Subject ATermNodeSubject = ATermNode.GetContent().GetSubject();
                MaxSubjectIDBeforePerRiskExplosion = Math.Max(MaxSubjectIDBeforePerRiskExplosion, ATermNodeSubject.ID);
            }

            OriginalSubjectRelationshipMatrix = new SubjectComparisonOutcome[MaxSubjectIDBeforePerRiskExplosion + 1, MaxSubjectIDBeforePerRiskExplosion + 1];
            OriginalPerRiskNodes = new List<TermNode>();

            foreach (TermNode ATermNode in this)
            {
                Subject ATermNodeSubject = ATermNode.GetContent().GetSubject();

                if (ATermNodeSubject.PerRisk)
                    OriginalPerRiskNodes.Add(ATermNode);

                foreach (TermNode AnotherTermNode in
                    this.Where(tn => tn.GetContent().GetSubject().ID != ATermNodeSubject.ID))
                {
                    Subject AnotherTermNodeSubject = AnotherTermNode.GetContent().GetSubject();

                    SubjectComparisonOutcome _SubjectComparisonOutcome
                        = CompareSubjects(AnotherTermNodeSubject, ATermNodeSubject);

                    OriginalSubjectRelationshipMatrix[AnotherTermNodeSubject.ID, ATermNodeSubject.ID] = _SubjectComparisonOutcome;
                }
            }

            DistinctPerRiskNodes = new HashSet<TermNode>(OriginalPerRiskNodes);

            OriginalNonPerRiskNodes = new HashSet<TermNode>(this as IEnumerable<TermNode>);
            OriginalNonPerRiskNodes.ExceptWith(OriginalPerRiskNodes);

            FinalNodes = new HashSet<TermNode>();
            foreach (TermNode OriginalNonPerRiskNode in OriginalNonPerRiskNodes)
            {
                //NodeTuple nodeTuple = new NodeTuple(false, node.Subject.ID, null);
                //FinalNodeTupleArray[node.Subject.ID] = nodeTuple;
                FinalNodes.Add(OriginalNonPerRiskNode);
            }

            RiskItemToTermCollectionAdditions = new Dictionary<long, TermCollection>[MaxSubjectIDBeforePerRiskExplosion + 1];
        }

        private void ExplodePerRisk()
        {
            SetDistinctPerRiskNodes();
        }

        private void SetDistinctPerRiskNodes()
        {
            for (int i = 0; i < OriginalPerRiskNodes.Count(); i++)
            {
                TermNode TermNodeI = OriginalPerRiskNodes[i];
                Subject sI = TermNodeI.GetContent().GetSubject();

                #region Compare to original per risk nodes
                for (int j = i + 1; j < OriginalPerRiskNodes.Count(); j++)
                {
                    TermNode TermNodeJ = OriginalPerRiskNodes[j];
                    Subject sJ = TermNodeJ.GetContent().GetSubject();

                    if (OriginalSubjectRelationshipMatrix[sI.ID, sJ.ID] == SubjectComparisonOutcome.Equal)
                    {
                        TermNodeJ.GetContent().UnionWith(TermNodeI.GetContent());
                        DistinctPerRiskNodes.Remove(TermNodeI);
                    }

                    else if (CompareSubjectDimensions(sI.CausesOfLoss, sJ.CausesOfLoss) == SubjectComparisonOutcome.Equal
                        && CompareSubjectDimensions(sI.ResolvedExposureTypes, sJ.ResolvedExposureTypes) == SubjectComparisonOutcome.Equal)
                    {
                        if (OriginalSubjectRelationshipMatrix[sI.ID, sJ.ID] == SubjectComparisonOutcome.Child)
                        {
                            //foreach (RITE rite in s2.Schedule.ScheduleList)
                            //{
                            //    RiteDedsAddition[s1.ID].Add(rite, tNode2.Deductibles);
                            //    RiteLimitsAddition[s1.ID].Add(rite, tNode2.Limits);
                            //}
                            DistinctPerRiskNodes.Remove(TermNodeJ);
                        }
                        else if (OriginalSubjectRelationshipMatrix[sI.ID, sJ.ID] == SubjectComparisonOutcome.Parent)
                        {
                            //foreach (RITE rite in s1.Schedule.ScheduleList)
                            //{
                            //    RiteDedsAddition[s2.ID].Add(rite, tNode1.Deductibles);
                            //    RiteLimitsAddition[s2.ID].Add(rite, tNode1.Limits);
                            //}
                            DistinctPerRiskNodes.Remove(TermNodeI);
                        }
                        else if (OriginalSubjectRelationshipMatrix[sI.ID, sJ.ID] == SubjectComparisonOutcome.Overlap_Child ||
                            OriginalSubjectRelationshipMatrix[sI.ID, sJ.ID] == SubjectComparisonOutcome.Overlap_Parent)
                        {
                        }
                        else
                            ; // TODO : ??????
                    }
                }
                #endregion Compare to original per risk nodes

                #region Get RITE singleton original non per risk nodes
                List<TermNode> OriginalSingletonNonPerRiskNodes = new List<TermNode>();

                foreach (TermNode OriginalNonPerRiskNode in OriginalNonPerRiskNodes)
                {
                    Subject subject = OriginalNonPerRiskNode.GetContent().GetSubject();
                    if (subject.RITEIds.Count == 1)
                        OriginalSingletonNonPerRiskNodes.Add(OriginalNonPerRiskNode);
                }
                #endregion

                #region Compare to singleton non per risk nodes
                foreach (TermNode OriginalSingletonNonPerRiskNode in OriginalSingletonNonPerRiskNodes)
                {
                    Subject OriginalSingletonNonPerRiskNodesSubject = OriginalSingletonNonPerRiskNode.GetContent().GetSubject();
                    if (CompareSubjectDimensions(sI.CausesOfLoss, OriginalSingletonNonPerRiskNodesSubject.CausesOfLoss)
                                == SubjectComparisonOutcome.Equal
                        && CompareSubjectDimensions(sI.ResolvedExposureTypes, OriginalSingletonNonPerRiskNodesSubject.ResolvedExposureTypes)
                                == SubjectComparisonOutcome.Equal)
                    {
                        if (sI.RITEIds.Contains(OriginalSingletonNonPerRiskNodesSubject.RITEIds.First()))
                        {
                            OriginalNonPerRiskNodes.Remove(OriginalSingletonNonPerRiskNode);
                            FinalNodes.Remove(OriginalSingletonNonPerRiskNode);
                            //RiteDedsAddition[s1.ID].Add(s3.Schedule.ScheduleList.First(), tNode3.Deductibles);
                            //RiteLimitsAddition[s1.ID].Add(s3.Schedule.ScheduleList.First(), tNode3.Limits);
                        }
                    }
                }
                #endregion Compare to singleton non per risk nodes
            }
        }

        SubjectComparisonOutcome CompareSubjectDimensions<T>(HashSet<T> set1, HashSet<T> set2)
        {
            if (set1.Count == set2.Count && set1.IsSubsetOf(set2))
            {
                return SubjectComparisonOutcome.Equal;
            }
            else if (set1.Count < set2.Count && set1.IsProperSubsetOf(set2))
            {
                return SubjectComparisonOutcome.Parent;
            }
            else if (set1.Count > set2.Count && set1.IsProperSupersetOf(set2))
            {
                return SubjectComparisonOutcome.Child;
            }
            else if (set1.Overlaps(set2))
            {
                return SubjectComparisonOutcome.Overlap;
            }
            else
            {
                return SubjectComparisonOutcome.Disjoint;
            }
        }

        SubjectComparisonOutcome CompareSubjects(Subject s1, Subject s2)
        {
            SubjectComparisonOutcome COLComparisonOutcome = CompareSubjectDimensions(s1.CausesOfLoss, s2.CausesOfLoss);
            SubjectComparisonOutcome ExposureTypesComparisonOutcome = CompareSubjectDimensions(s1.ResolvedExposureTypes, s2.ResolvedExposureTypes);
            SubjectComparisonOutcome RITEsComparisonOutcome = CompareSubjectDimensions(s1.RITEIds, s2.RITEIds);

            if (COLComparisonOutcome == SubjectComparisonOutcome.Equal
                && ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Equal
                && RITEsComparisonOutcome == SubjectComparisonOutcome.Equal)
            {
                if (s1.PerRisk == s2.PerRisk)
                    return SubjectComparisonOutcome.Equal;
                else if (s1.PerRisk && !s2.PerRisk)
                    return SubjectComparisonOutcome.Parent;
                else
                    return SubjectComparisonOutcome.Child;
            }

            else if ((COLComparisonOutcome == SubjectComparisonOutcome.Parent || COLComparisonOutcome == SubjectComparisonOutcome.Equal)
                && (ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Parent || ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Equal)
                && (RITEsComparisonOutcome == SubjectComparisonOutcome.Parent || RITEsComparisonOutcome == SubjectComparisonOutcome.Equal))
            {
                return SubjectComparisonOutcome.Parent;
            }

            else if ((COLComparisonOutcome == SubjectComparisonOutcome.Child || COLComparisonOutcome == SubjectComparisonOutcome.Equal)
                && (ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Child || ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Equal)
                && (RITEsComparisonOutcome == SubjectComparisonOutcome.Child || RITEsComparisonOutcome == SubjectComparisonOutcome.Equal))
            {
                return SubjectComparisonOutcome.Child;
            }

            else if ((COLComparisonOutcome == SubjectComparisonOutcome.Parent || COLComparisonOutcome == SubjectComparisonOutcome.Equal)
                && (ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Parent || ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Equal)
                && (RITEsComparisonOutcome == SubjectComparisonOutcome.Overlap))
            {
                return SubjectComparisonOutcome.Overlap_Parent;
            }

            else if ((COLComparisonOutcome == SubjectComparisonOutcome.Child || COLComparisonOutcome == SubjectComparisonOutcome.Equal)
                && (ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Child || ExposureTypesComparisonOutcome == SubjectComparisonOutcome.Equal)
                && (RITEsComparisonOutcome == SubjectComparisonOutcome.Overlap))
            {
                return SubjectComparisonOutcome.Overlap_Child;
            }

            else
                return SubjectComparisonOutcome.Disjoint;
        }

        #endregion
    }
}
