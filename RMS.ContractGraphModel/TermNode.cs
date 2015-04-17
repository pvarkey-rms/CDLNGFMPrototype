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
    class TermNode : Node<TermCollection>, IEquatable<TermNode>
    {
        #region Fields

        [ProtoMember(1)]
        bool IsItMultiBuildingPerRisk = false;

        #endregion

        #region Constructors
        public TermNode() : base() {  }
        public TermNode(TermCollection Terms) : base(Terms) {  }
        public TermNode(TermNode TermNode)
            : this(TermNode.GetContent()) 
        {
            IsItMultiBuildingPerRisk = TermNode.IsMultiBuildingPerRisk();
        }
        #endregion

        #region Graph API

        public virtual bool IsChildOf(TermNode AnotherTermNode)
        {
            return GetSubject().IsSubsetOf(AnotherTermNode.GetSubject());
        }

        public virtual bool Overlaps(TermNode AnotherTermNode)
        {
            return GetSubject().Overlaps(AnotherTermNode.GetSubject());
        }

        public virtual bool OverlapsWithoutInclusion(TermNode AnotherTermNode)
        {
            return GetSubject().OverlapsWithoutInclusion(AnotherTermNode.GetSubject());
        }

        public virtual void AddParent(TermNode Parent)
        {
            throw new NotSupportedException();
        }

        public virtual void RemoveParent(TermNode Parent)
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

        public TermExecutionPosition Execute(List<TermExecutionPosition> _SubjectPositions,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings)
        {
            #region IF per-risk, multi-building
            if (IsMultiBuildingPerRisk())
            {
                // Compute aggregate subject position
                TermExecutionPosition ExecutionPosition
                    = _SubjectPositions.Aggregate(new TermExecutionPosition(), 
                            (accumulator, it) => accumulator + it);
                // NEW ALGO....

                int MultiBldgSampleCount = ExecutionPosition.S_vector.Length;
                double[] S_MultiBldg = new double[MultiBldgSampleCount];
                double[] D_MultiBldg = new double[MultiBldgSampleCount];
                double[] X_MultiBldg = new double[MultiBldgSampleCount];

                #region Iterate over terms in this collection, in order (and, internally, over building samples)

                for (int i = 0; i < MultiBldgSampleCount; i++)
                {
                    S_MultiBldg[i] = ExecutionPosition.S_vector[i];
                    D_MultiBldg[i] = ExecutionPosition.D_vector[i];
                    X_MultiBldg[i] = ExecutionPosition.X_vector[i];
                }

                foreach (IGenericTerm term in this.GetContent())
                {
                    for (int i = 0; i < MultiBldgSampleCount; i++)
                    {
                        Bindings[new SymbolicExpression("Subject")] = 
                            S_MultiBldg[i];

                        double A = term.GetEvaluatedExpression(Bindings);

                        if (term is ISublimit)
                        {
                            // Functional Form
                            double X_ = 0.0;
                            if (!(term as ISublimit).IsNetOfDeductible())
                                // 1. Ground-up Sublimit
                                X_ = Math.Max(S_MultiBldg[i] - A, 0.0);
                            else
                                // 5. Net of Deductible Sublimit
                                X_ = Math.Max(S_MultiBldg[i] - D_MultiBldg[i] - A, 0.0);

                            // Interaction
                            double X__ = Math.Max(X_MultiBldg[i], X_);

                            // Final Adjustment
                            X_MultiBldg[i] = Math.Min(X__, S_MultiBldg[i] - D_MultiBldg[i]);
                        }
                        
                        else // if (term is IDeductible)
                        {
                            // Functional Form
                            double D_ = 0.0;
                            if (!(term as IDeductible).IsFranchise())
                                D_ = Math.Min(S_MultiBldg[i], A);
                            else
                                D_ = (S_MultiBldg[i] > A) ? 0.0 : S_MultiBldg[i];

                            // Interaction
                            double D__ = 0.0;
                            switch ((term as IDeductible).GetInteraction())
                            {
                                case Interaction.SINGLELARGEST:
                                    D__ = _SubjectPositions.Select(s => s.D_vector.Max()).Max();
                                    break;
                                case Interaction.MIN:
                                    if (!(term as IDeductible).IsAbsorbable())
                                        D__ = Math.Max(D_MultiBldg[i], D_);
                                    else
                                        D__ = Math.Max(D_MultiBldg[i], D_ - X_MultiBldg[i]);
                                    break;
                                case Interaction.MAX:
                                    D__ = Math.Min(D_MultiBldg[i], D_);
                                    break;
                            }

                            // Final Adjustment
                            D_MultiBldg[i] = Math.Min(D__, S_MultiBldg[i] - X_MultiBldg[i]);
                        }
                    } // end of for loop iteration over multi-building samples
                } // end of for loop iteration over terms
                #endregion

                #region Iterate over building samples (and, internally, over terms, in order)
                // ALGO....
                //List<TermExecutionPosition.Component> SampledMultiBuildingOutputPositionComponents = 
                //    new List<TermExecutionPosition.Component>();

                //foreach (TermExecutionPosition.Component SampledMultiBuildingExecutionPositionComponent 
                //    in ExecutionPosition.components)
                //{
                //    TermExecutionPosition.Component SampledMultiBuildingOutputPositionComponent = 
                //        new TermExecutionPosition.Component(SampledMultiBuildingExecutionPositionComponent);

                //    Bindings.Remove(new SymbolicExpression("Subject"));
                //    Bindings.Add(new SymbolicExpression("Subject"), 
                //        SampledMultiBuildingOutputPositionComponent.S);

                //    // Iterate over terms in this collection, in order

                //    foreach (IGenericTerm term in this.GetContent())
                //    {
                //        double A = term.GetEvaluatedExpression(Bindings);

                //        if (term is ISublimit)
                //        {
                //            // Functional Form
                //            double X_ = 0.0;
                //            if (!(term as ISublimit).IsNetOfDeductible())
                //                // 1. Ground-up Sublimit
                //                X_ = Math.Max(SampledMultiBuildingOutputPositionComponent.S - A, 0.0);
                //            else
                //                // 5. Net of Deductible Sublimit
                //                X_ = Math.Max(SampledMultiBuildingOutputPositionComponent.S - SampledMultiBuildingOutputPositionComponent.D - A, 0.0);

                //            // Interaction
                //            double X__ = Math.Max(SampledMultiBuildingOutputPositionComponent.X, X_);

                //            // Final Adjustment
                //            SampledMultiBuildingOutputPositionComponent.X = Math.Min(X__, SampledMultiBuildingOutputPositionComponent.S - SampledMultiBuildingOutputPositionComponent.D);
                //        }
                //        else // if (term is IDeductible)
                //        {
                //            // Functional Form
                //            double D_ = 0.0;
                //            if (!(term as IDeductible).IsFranchise())
                //                D_ = Math.Min(SampledMultiBuildingOutputPositionComponent.S, A);
                //            else
                //                D_ = (SampledMultiBuildingOutputPositionComponent.S > A) ? 0.0 : SampledMultiBuildingOutputPositionComponent.S;

                //            // Interaction
                //            double D__ = 0.0;
                //            switch ((term as IDeductible).GetInteraction())
                //            {
                //                case Interaction.SINGLELARGEST:
                //                    D__ = _SubjectPositions.Select(s => s.components.Select(c => c.D).Max()).Max();
                //                    break;
                //                case Interaction.MIN:
                //                    if (!(term as IDeductible).IsAbsorbable())
                //                        D__ = Math.Max(SampledMultiBuildingOutputPositionComponent.D, D_);
                //                    else
                //                        D__ = Math.Max(SampledMultiBuildingOutputPositionComponent.D, D_ - SampledMultiBuildingOutputPositionComponent.X);
                //                    break;
                //                case Interaction.MAX:
                //                    D__ = Math.Min(ExecutionPosition.D, D_);
                //                    break;
                //            }

                //            // Final Adjustment
                //            SampledMultiBuildingOutputPositionComponent.D = Math.Min(D__, SampledMultiBuildingOutputPositionComponent.S - SampledMultiBuildingOutputPositionComponent.X);
                //        }
                //    }

                //    SampledMultiBuildingOutputPositionComponents.Add(SampledMultiBuildingOutputPositionComponent);
                //}
                #endregion

                // Calculate number of buildings

                int NumBuildings = 1;

                Subject _TermNodeSubject = this.GetSubject();

                if (_TermNodeSubject.PerRisk)
                    NumBuildings = _TermNodeSubject.NumBuildings;

                return new TermExecutionPosition(S_MultiBldg, D_MultiBldg, X_MultiBldg, ExecutionPosition.FactorArray, NumBuildings);
            }
            #endregion IF per-risk, multi-building

            #region Single-building OR Summed
            else
            {
                // Compute aggregate subject position & coalesce
                _SubjectPositions.ForEach(x => x.Coalesce());
                TermExecutionPosition ExecutionPosition
                    = _SubjectPositions.Aggregate(new TermExecutionPosition(), (accumulator, it) => accumulator + it);
                ExecutionPosition.Coalesce(); // @FACTORCHANGES : ExecutionPosition.Coalesce(1);
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

            if (obj.GetType() != typeof(TermNode))
                return false;

            TermNode tn = obj as TermNode;

            return this.Equals(tn);
        }

        public bool Equals(TermNode tn)
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
