using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    public interface IAttachment<out V>
        where V : IValue<AValue>
    {
        IExpression<V> GetExpression();
        double GetEvaluatedExpression(Dictionary<SimpleExpression<SymbolicValue>, double> Bindings);
        TimeBasis GetTimeBasis();
        bool IsFranchise();
    }

    public class Attachment<V> : IAttachment<V>, IEquatable<Attachment<V>>
        where V : IValue<AValue>
    {
        IExpression<V> expression;
        TimeBasis AttachmentTimeBasis = TimeBasis.Default;
        bool IsItFranchise = false;

        public Attachment() : this(null, TimeBasis.Default, false) { }
        public Attachment(IExpression<V> expression) : this(expression, TimeBasis.Default, false) { }
        public Attachment(IExpression<V> expression, TimeBasis timeBasis) : this(expression, timeBasis, false) { }
        public Attachment(IExpression<V> expression, bool isFranchise) : this(expression, TimeBasis.Default, isFranchise) { }
        public Attachment(IExpression<V> expression, TimeBasis timeBasis, bool isFranchise)
        {
            this.expression = expression;
            this.AttachmentTimeBasis = timeBasis;
            this.IsItFranchise = isFranchise;
        }

        #region Methods
        public override string ToString()
        {
            return "XS(" + expression.ToString() + ", " + AttachmentTimeBasis.ToString() + ", " + "IsItFranchise=" + IsItFranchise.ToString() + ")";
        }
        public IExpression<V> GetExpression()
        {
            return expression;
        }
        public double GetEvaluatedExpression(Dictionary<SimpleExpression<SymbolicValue>, double> Bindings)
        {
            return GetExpression().GetEvaluatedValue(Bindings);
        }
        public TimeBasis GetTimeBasis()
        {
            return AttachmentTimeBasis;
        }
        public bool IsFranchise()
        {
            return IsItFranchise;
        }
        #endregion Methods

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Attachment<V>))
                return false;

            Attachment<V> e = obj as Attachment<V>;

            return this.Equals(e);
        }

        public bool Equals(Attachment<V> e)
        {
            if (e == null)
            {
                return false;
            }

            return (expression.Equals(e.expression)
                    && AttachmentTimeBasis.Equals(e.AttachmentTimeBasis)
                    && (IsItFranchise == e.IsItFranchise));
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + expression.GetHashCode();
            hash = hash * 37 + AttachmentTimeBasis.GetHashCode();
            hash = hash * 37 + IsItFranchise.GetHashCode();
            return hash;
        }
        #endregion
    }
}
