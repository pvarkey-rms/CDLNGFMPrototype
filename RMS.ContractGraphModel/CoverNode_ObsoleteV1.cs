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
    class CoverNode_ObsoleteV1 : Node<IGenericCover>, IEquatable<CoverNode_ObsoleteV1>
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

        public CoverNode_ObsoleteV1() : base() { }
        public CoverNode_ObsoleteV1(IGenericCover cover) : base(cover) { }
        public CoverNode_ObsoleteV1(CoverNode_ObsoleteV1 CoverNode)
            : this(CoverNode.GetContent()) 
        {
            IsItMultiBuildingPerRisk = CoverNode.IsMultiBuildingPerRisk();
        }

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != typeof(CoverNode_ObsoleteV1))
                return false;

            CoverNode_ObsoleteV1 cn = obj as CoverNode_ObsoleteV1;

            return this.Equals(cn);
        }

        public bool Equals(CoverNode_ObsoleteV1 cn)
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

        public double Execute(Dictionary<SimpleExpression<SymbolicValue>, double> Bindings)
        {
            double SubjectPosition = Bindings[new SymbolicExpression("Subject")];

            double R = SubjectPosition;

            // does cover attach?
            var Attachment = GetContent().GetAttachment();
            double attachment = 0.0;
            if (Attachment != null)
            {
                attachment = Attachment.GetEvaluatedExpression(Bindings);

                if (SubjectPosition <= attachment) // it does not attach
                {
                    return 0.0;
                }

                // apply attachment
                R = (Attachment.IsFranchise()) ? SubjectPosition : Math.Max(SubjectPosition - attachment, 0.0);
            }

            // apply payout (i.e. limit?) function
            var Limit = GetContent().GetLimit();
            if (Limit != null)
            {
                if (Limit.IsPAY())
                    R = Limit.GetEvaluatedExpression(Bindings);
                else
                    R = Math.Min(Limit.GetEvaluatedExpression(), R);
            }

            // apply share
            double share = 100;
            var Share = GetContent().GetParticipation();
            if (Share != null)
            {
                share = GetContent().GetParticipation().GetEvaluatedValue();
            }
            R *= (share / 100.0);

            return R;
        }

        //public double[] ExecuteMuiltiBldgPerRisk(Dictionary<SimpleExpression<SymbolicValue>, double> Bindings, double[] SubjectArrayLoss)
        //{

        //    double[] SubjectPosition = SubjectArrayLoss;

        //    double[] ArrayR = SubjectPosition;

        //    var Attachment = GetContent().GetAttachment();
        //    double attachment = 0.0;
        //    attachment = Attachment.GetEvaluatedExpression(Bindings);

        //    // does cover attach?

        //    for (int i = 0; i < SubjectPosition.Length; i++)
        //    {
        //        if (Attachment != null)
        //        {
        //            if (SubjectPosition[i] <= attachment) // it does not attach
        //            {
        //                ArrayR[i]=0;
        //            }

        //            // apply attachment
        //            ArrayR[0] = (Attachment.IsFranchise()) ? SubjectPosition[i] : Math.Max(SubjectPosition[i] - attachment, 0.0);
        //        }

        //        // apply payout (i.e. limit?) function
        //        var Limit = GetContent().GetLimit();
        //        if (Limit != null)
        //        {
        //            if (Limit.IsPAY())
        //                ArrayR[i] = Limit.GetEvaluatedExpression(Bindings);
        //            else
        //                ArrayR[i] = Math.Min(Limit.GetEvaluatedExpression(), ArrayR[i]);
        //        }

        //        // apply share
        //        double share = 100;
        //        var Share = GetContent().GetParticipation();
        //        if (Share != null)
        //        {
        //            share = GetContent().GetParticipation().GetEvaluatedValue();
        //        }
        //        ArrayR[i] *= (share / 100.0);
        //    }

        //    return ArrayR;
        //}

        #endregion
    }
}
