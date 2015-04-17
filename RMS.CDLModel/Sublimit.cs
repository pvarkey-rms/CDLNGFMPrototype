using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    public interface ISublimit
    {
        bool IsNetOfDeductible();
    }

    public class Sublimit<V> : Term<V>, ITerm<V>, IEquatable<Sublimit<V>>, ISublimit
        where V : Value
    {
        #region Fields
        bool IsItNetOfDeductible;
        #endregion

        #region Constructors
        public Sublimit(IExpression<V> expression, string Label = null) 
            : this(expression, new Subject(), TimeBasis.Default, Label) { }
        public Sublimit(IExpression<V> expression, bool isitNetOfDeductible, string Label = null)
            : this(expression, new Subject(), TimeBasis.Default, isitNetOfDeductible, Label) { }
        public Sublimit(IExpression<V> expression, Subject subjectConstraint, string Label = null)
            : this(expression, subjectConstraint, TimeBasis.Default, Label) { }
        public Sublimit(IExpression<V> expression, Subject subjectConstraint, bool isitNetOfDeductible, string Label = null)
            : this(expression, subjectConstraint, TimeBasis.Default, isitNetOfDeductible, Label) { }
        public Sublimit(IExpression<V> expression, Subject subjectConstraint, TimeBasis timeBasis = TimeBasis.Default, string Label = null)
            : this(expression, subjectConstraint, timeBasis, false, Label) { }
        public Sublimit(IExpression<V> expression, Subject subjectConstraint, TimeBasis timeBasis = TimeBasis.Default, bool isitNetOfDeductible = false, string Label = null)
            : base(expression, subjectConstraint, timeBasis, Label) 
        {
            this.IsItNetOfDeductible = isitNetOfDeductible;
        }

        public Sublimit(Sublimit<V> CopyFromSublimit)
            : base(CopyFromSublimit)
        {
            this.IsItNetOfDeductible = CopyFromSublimit.IsNetOfDeductible();
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this == obj)
                return true;

            if (obj.GetType() != typeof(Sublimit<V>))
                return false;

            Sublimit<V> thatSublimit = obj as Sublimit<V>;

            return (this.Equals(thatSublimit));
        }

        public bool Equals(Sublimit<V> thatSublimit)
        {
            return base.Equals(thatSublimit) && this.IsNetOfDeductible().Equals(thatSublimit.IsNetOfDeductible());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region Methods
        public bool IsNetOfDeductible()
        {
            return IsItNetOfDeductible;
        }
        #endregion
    }
}
