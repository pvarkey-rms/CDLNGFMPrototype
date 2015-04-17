using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractGraphModel
{
    using IGenericTerm = ITerm<Value>;

    [Serializable]
    [ProtoContract]
    class TermNode_ObsoleteV1 : Node<TermCollection>, IEquatable<TermNode_ObsoleteV1>
    {
        #region Fields

        [ProtoMember(1)]
        bool IsItMultiBuildingPerRisk = false;

        #endregion

        #region Constructors
        public TermNode_ObsoleteV1() : base() {  }
        public TermNode_ObsoleteV1(TermCollection Terms) : base(Terms) {  }
        public TermNode_ObsoleteV1(TermNode_ObsoleteV1 TermNode) : this(TermNode.GetContent()) 
        {
            IsItMultiBuildingPerRisk = TermNode.IsMultiBuildingPerRisk();
        }
        #endregion

        #region Graph API

        public virtual bool IsChildOf(TermNode_ObsoleteV1 AnotherTermNode)
        {
            return GetSubject().IsSubsetOf(AnotherTermNode.GetSubject());
        }

        public virtual bool Overlaps(TermNode_ObsoleteV1 AnotherTermNode)
        {
            return GetSubject().Overlaps(AnotherTermNode.GetSubject());
        }

        public virtual bool OverlapsWithoutInclusion(TermNode_ObsoleteV1 AnotherTermNode)
        {
            return GetSubject().OverlapsWithoutInclusion(AnotherTermNode.GetSubject());
        }

        public virtual void AddParent(TermNode_ObsoleteV1 Parent)
        {
            throw new NotSupportedException();
        }

        public virtual void RemoveParent(TermNode_ObsoleteV1 Parent)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Execution API
        public Subject GetSubject()
        {
            return GetContent().GetSubject();
        }

        public void MarkAsMultiBuildingPerRisk()
        {
            IsItMultiBuildingPerRisk = true;
        }

        public bool IsMultiBuildingPerRisk()
        {
            return IsItMultiBuildingPerRisk;
        }

        public TermExecutionPosition_ObsoleteV1 Execute(List<TermExecutionPosition_ObsoleteV1> _SubjectPositions,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings)
        {
            #region IF per-risk, multi-building
            if (IsMultiBuildingPerRisk())
            {
                // Compute aggregate subject position
                TermExecutionPosition_ObsoleteV1 ExecutionPosition
                    = _SubjectPositions.Aggregate(new TermExecutionPosition_ObsoleteV1(), 
                            (accumulator, it) => accumulator + it);

                List<TermExecutionPosition_ObsoleteV1.Component> SampledMultiBuildingOutputPositionComponents = 
                    new List<TermExecutionPosition_ObsoleteV1.Component>();

                foreach (TermExecutionPosition_ObsoleteV1.Component SampledMultiBuildingExecutionPositionComponent 
                    in ExecutionPosition.components)
                {
                    TermExecutionPosition_ObsoleteV1.Component SampledMultiBuildingOutputPositionComponent = 
                        new TermExecutionPosition_ObsoleteV1.Component(SampledMultiBuildingExecutionPositionComponent);

                    Bindings.Remove(new SymbolicExpression("Subject"));
                    Bindings.Add(new SymbolicExpression("Subject"), 
                        SampledMultiBuildingOutputPositionComponent.S);

                    // Iterate over terms in this collection, in order

                    foreach (IGenericTerm term in this.GetContent())
                    {
                        double A = term.GetEvaluatedExpression(Bindings);

                        if (term is ISublimit)
                        {
                            // Functional Form
                            double X_ = 0.0;
                            if (!(term as ISublimit).IsNetOfDeductible())
                                // 1. Ground-up Sublimit
                                X_ = Math.Max(SampledMultiBuildingOutputPositionComponent.S - A, 0.0);
                            else
                                // 5. Net of Deductible Sublimit
                                X_ = Math.Max(SampledMultiBuildingOutputPositionComponent.S - SampledMultiBuildingOutputPositionComponent.D - A, 0.0);

                            // Interaction
                            double X__ = Math.Max(SampledMultiBuildingOutputPositionComponent.X, X_);

                            // Final Adjustment
                            SampledMultiBuildingOutputPositionComponent.X = Math.Min(X__, SampledMultiBuildingOutputPositionComponent.S - SampledMultiBuildingOutputPositionComponent.D);
                        }
                        else // if (term is IDeductible)
                        {
                            // Functional Form
                            double D_ = 0.0;
                            if (!(term as IDeductible).IsFranchise())
                                D_ = Math.Min(SampledMultiBuildingOutputPositionComponent.S, A);
                            else
                                D_ = (SampledMultiBuildingOutputPositionComponent.S > A) ? 0.0 : SampledMultiBuildingOutputPositionComponent.S;

                            // Interaction
                            double D__ = 0.0;
                            switch ((term as IDeductible).GetInteraction())
                            {
                                case Interaction.SINGLELARGEST:
                                    D__ = _SubjectPositions.Select(s => s.components.Select(c => c.D).Max()).Max();
                                    break;
                                case Interaction.MIN:
                                    if (!(term as IDeductible).IsAbsorbable())
                                        D__ = Math.Max(SampledMultiBuildingOutputPositionComponent.D, D_);
                                    else
                                        D__ = Math.Max(SampledMultiBuildingOutputPositionComponent.D, D_ - SampledMultiBuildingOutputPositionComponent.X);
                                    break;
                                case Interaction.MAX:
                                    D__ = Math.Min(ExecutionPosition.D, D_);
                                    break;
                            }

                            // Final Adjustment
                            SampledMultiBuildingOutputPositionComponent.D = Math.Min(D__, SampledMultiBuildingOutputPositionComponent.S - SampledMultiBuildingOutputPositionComponent.X);
                        }
                    }
                    SampledMultiBuildingOutputPositionComponents.Add(SampledMultiBuildingOutputPositionComponent);
                }

                // Calculate number of buildings

                int NumBuildings = 1;

                Subject _TermNodeSubject = this.GetSubject();

                if (_TermNodeSubject.PerRisk)
                    NumBuildings = _TermNodeSubject.NumBuildings;

                return new TermExecutionPosition_ObsoleteV1(SampledMultiBuildingOutputPositionComponents, NumBuildings);
            }
            #endregion IF per-risk, multi-building

            #region Single-building OR Summed
            else
            {
                // Compute aggregate subject position & coalesce
                _SubjectPositions.ForEach(x => x.Coalesce());
                TermExecutionPosition_ObsoleteV1 ExecutionPosition
                    = _SubjectPositions.Aggregate(new TermExecutionPosition_ObsoleteV1(), (accumulator, it) => accumulator + it);
                ExecutionPosition.Coalesce(1);
                Bindings.Remove(new SymbolicExpression("Subject"));
                Bindings.Add(new SymbolicExpression("Subject"), ExecutionPosition.S);

                // Iterate over terms in this collection, in order

                foreach (IGenericTerm term in this.GetContent())
                {
                    double A = term.GetEvaluatedExpression(Bindings);

                    if (term is ISublimit)
                    {
                        // Functional Form
                        double X_ = 0.0;
                        if (!(term as ISublimit).IsNetOfDeductible())
                            // 1. Ground-up Sublimit
                            X_ = Math.Max(ExecutionPosition.S - A, 0.0);
                        else
                            // 5. Net of Deductible Sublimit
                            X_ = Math.Max(ExecutionPosition.S - ExecutionPosition.D - A, 0.0);

                        // Interaction
                        double X__ = Math.Max(ExecutionPosition.X, X_);

                        // Final Adjustment
                        ExecutionPosition.X = Math.Min(X__, ExecutionPosition.S - ExecutionPosition.D);
                    }
                    else // if (term is IDeductible)
                    {
                        // Functional Form
                        double D_ = 0.0;
                        if (!(term as IDeductible).IsFranchise())
                            D_ = Math.Min(ExecutionPosition.S, A);
                        else
                            D_ = (ExecutionPosition.S > A) ? 0.0 : ExecutionPosition.S;

                        // Interaction
                        double D__ = 0.0;
                        switch ((term as IDeductible).GetInteraction())
                        {
                            case Interaction.SINGLELARGEST:
                                D__ = _SubjectPositions.Select(s => s.D).Max();
                                break;
                            case Interaction.MIN:
                                if (!(term as IDeductible).IsAbsorbable())
                                    D__ = Math.Max(ExecutionPosition.D, D_);
                                else
                                    D__ = Math.Max(ExecutionPosition.D, D_ - ExecutionPosition.X);
                                break;
                            case Interaction.MAX:
                                D__ = Math.Min(ExecutionPosition.D, D_);
                                break;
                        }

                        // Final Adjustment
                        ExecutionPosition.D = Math.Min(D__, ExecutionPosition.S - ExecutionPosition.X);
                    }
                }

                return ExecutionPosition;
            }
            #endregion Single-building OR Summed
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != typeof(TermNode_ObsoleteV1))
                return false;

            TermNode_ObsoleteV1 tn = obj as TermNode_ObsoleteV1;

            return this.Equals(tn);
        }

        public virtual bool Equals(TermNode_ObsoleteV1 tn)
        {
            if (tn == null)
                return false;

            return (GetContent().Equals(tn.GetContent()));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion
    }
}
