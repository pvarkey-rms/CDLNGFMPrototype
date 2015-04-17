using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractGraphModel
{
    using IGenericCover = ICover<Value, Value, Value>;

    [Serializable]
    [ProtoContract]
    class CoverNode : Node<IGenericCover>, IEquatable<CoverNode>
    {
        #region Fields

        bool IsItMultiBuildingPerRisk = false;

        #endregion

        public void MarkAsMultiBuildingPerRisk()
        {
            IsItMultiBuildingPerRisk = true;
        }

        public bool IsMultiBuildingPerRisk()
        {
            return IsItMultiBuildingPerRisk;
        }

        public CoverNode() : base() { }
        public CoverNode(IGenericCover cover) : base(cover) { }
        public CoverNode(CoverNode CoverNode)
            : this(CoverNode.GetContent()) 
        {
            IsItMultiBuildingPerRisk = CoverNode.IsMultiBuildingPerRisk();
        }

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != typeof(CoverNode))
                return false;

            CoverNode cn = obj as CoverNode;

            return this.Equals(cn);
        }

        public bool Equals(CoverNode cn)
        {
            if (cn == null)
                return false;

            return (GetContent().Equals(cn.GetContent()));
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region API
        public SubjectPosition GetSubject()
        {
            return GetContent().GetSubject();
        }

        public CoverExecutionPosition Execute(CoverExecutionPosition _SubjectPosition,
            Dictionary<SimpleExpression<SymbolicValue>, double> Bindings,
            Dictionary<SimpleExpression<SymbolicValue>, double> AggregateStateBindings)
        {
            double[] P_vector = new double[_SubjectPosition.S_vector.Length];

            for (int i = 0; i < _SubjectPosition.S_vector.Length; i++)
            {
                double SubjectPosition = _SubjectPosition.S_vector[i];

                Bindings.Remove(new SymbolicExpression("Subject"));
                Bindings.Add(new SymbolicExpression("Subject"), SubjectPosition);

                double P = SubjectPosition;

                // does cover attach?
                var Attachment = GetContent().GetAttachment();
                double CodedAttachment = 0.0;
                if (Attachment != null)
                {
                    CodedAttachment = Attachment.GetEvaluatedExpression(Bindings);

                    if (SubjectPosition <= (CodedAttachment - AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")])) // it does not attach
                    {
                        P_vector[i] = 0.0;
                        if (Attachment.GetTimeBasis() == TimeBasis.Aggregate)
                        {
                            AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")]
                                = AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")] + SubjectPosition;
                        }
                        continue;
                    }

                    if (Attachment.GetTimeBasis() == TimeBasis.Aggregate)
                        AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")] = CodedAttachment;

                    // apply attachment
                    P = (Attachment.IsFranchise()) ? SubjectPosition : Math.Max(SubjectPosition - (CodedAttachment - AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")]), 0.0);
                }

                // apply payout (i.e. limit?) function
                var Limit = GetContent().GetLimit();
                if (Limit != null)
                {
                    double CodedLimit = Limit.GetEvaluatedExpression(Bindings);
                    if (Limit.IsPAY())
                        P = CodedLimit;
                    else
                    {
                        P = Math.Min(CodedLimit - AggregateStateBindings[new SymbolicExpression("AggregateLimitState")], P);

                        if (Limit.GetTimeBasis() == TimeBasis.Aggregate)
                        {
                            AggregateStateBindings[new SymbolicExpression("AggregateLimitState")]
                                = Math.Min(CodedLimit, AggregateStateBindings[new SymbolicExpression("AggregateLimitState")] + P);
                        }
                    }
                }

                // apply share
                double share = 100;
                var Share = GetContent().GetParticipation();
                if (Share != null)
                {
                    share = GetContent().GetParticipation().GetEvaluatedValue(Bindings);
                }
                P *= (share / 100.0);
                P_vector[i] = P;
            }

            return new CoverExecutionPosition(_SubjectPosition.S_vector, P_vector, _SubjectPosition.FactorArray, _SubjectPosition.NumBuildings);
        }

        public double Execute(Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, Dictionary<SimpleExpression<SymbolicValue>, double> AggregateStateBindings)
        {
            double SubjectPosition = Bindings[new SymbolicExpression("Subject")];

            double P = SubjectPosition;

            // does cover attach?
            var Attachment = GetContent().GetAttachment();
            double CodedAttachment = 0.0;
            if (Attachment != null)
            {
                CodedAttachment = Attachment.GetEvaluatedExpression(Bindings);

                if (SubjectPosition <= (CodedAttachment - AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")])) // it does not attach
                {
                    if (Attachment.GetTimeBasis() == TimeBasis.Aggregate)
                    {
                        AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")]
                            = AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")] + SubjectPosition;
                    }
                    return 0.0;
                }

                if (Attachment.GetTimeBasis() == TimeBasis.Aggregate)
                    AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")] = CodedAttachment;

                // apply attachment
                P = (Attachment.IsFranchise()) ? SubjectPosition : Math.Max(SubjectPosition - (CodedAttachment - AggregateStateBindings[new SymbolicExpression("AggregateAttachmentState")]), 0.0);
            }

            // apply payout (i.e. limit?) function
            var Limit = GetContent().GetLimit();
            if (Limit != null)
            {
                double CodedLimit = Limit.GetEvaluatedExpression(Bindings);
                if (Limit.IsPAY())
                    P = CodedLimit;
                else
                {
                    P = Math.Min(CodedLimit - AggregateStateBindings[new SymbolicExpression("AggregateLimitState")], P);

                    if (Limit.GetTimeBasis() == TimeBasis.Aggregate)
                    {
                        AggregateStateBindings[new SymbolicExpression("AggregateLimitState")]
                            = Math.Min(CodedLimit, AggregateStateBindings[new SymbolicExpression("AggregateLimitState")] + P);
                    }
                }
            }

            // apply share
            double share = 100;
            var Share = GetContent().GetParticipation();
            if (Share != null)
            {
                share = GetContent().GetParticipation().GetEvaluatedValue(Bindings);
            }
            P *= (share / 100.0);

            return P;
        }

        #endregion
    }
}
