using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    public interface IDeductible
    {
        Interaction GetInteraction();
        bool IsFranchise();
        bool IsAbsorbable();
    }

    public interface IDeductible<V> : ITerm<V>
        where V : Value
    {
    }
    
    public class Deductible<V> : Term<V>, IDeductible<V>, IDeductible
        where V : Value
    {
        #region Fields
        Interaction DeductibleInteraction;
        bool IsItFranchise;
        bool IsItAbsorbable;
        #endregion

        #region Constructors
        public Deductible(IExpression<V> expression, string Label = null, bool absorbable = false)
            : this(expression, new Subject(), Interaction.MIN, false, TimeBasis.Default, Label) { }
        public Deductible(IExpression<V> expression, bool franchise, string Label = null, bool absorbable = false)
            : this(expression, new Subject(), Interaction.MIN, franchise, TimeBasis.Default, Label, absorbable) { }
        public Deductible(IExpression<V> expression, Interaction interaction, string Label = null, bool absorbable = false) 
            : this(expression, new Subject(), interaction, false, TimeBasis.Default, Label, absorbable) { }
        public Deductible(IExpression<V> expression, Subject subjectContraint, TimeBasis timeBasis = TimeBasis.Default, string Label = null, bool absorbable = false)
            : this(expression, subjectContraint, Interaction.MIN, false, timeBasis, Label, absorbable) { }
        public Deductible(IExpression<V> expression, Subject subjectContraint, string Label = null, bool absorbable = false)
            : this(expression, subjectContraint, Interaction.MIN, false, TimeBasis.Default, Label, absorbable) { }
        public Deductible(IExpression<V> expression, Subject subjectContraint, Interaction interaction = Interaction.MIN, bool franchise = false, 
            TimeBasis timeBasis = TimeBasis.Default, string Label = null, bool absorbable = false)
            : base(expression, subjectContraint, timeBasis, Label) 
        {
            this.DeductibleInteraction = interaction;
            this.IsItFranchise = franchise;
            this.IsItAbsorbable = absorbable;
        }

        public Deductible(Deductible<V> CopyFromDeductible)
            : base(CopyFromDeductible)
        {
            this.IsItFranchise = CopyFromDeductible.IsFranchise();
            this.IsItAbsorbable = CopyFromDeductible.IsAbsorbable();
            this.DeductibleInteraction = CopyFromDeductible.GetInteraction();
        }
        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this == obj)
                return true;

            if (obj.GetType() != typeof(Deductible<V>))
                return false;

            Deductible<V> thatDeductible = obj as Deductible<V>;

            return (this.Equals(thatDeductible));
        }

        public bool Equals(Deductible<V> thatDeductible)
        {
            return base.Equals(thatDeductible) && this.GetInteraction().Equals(thatDeductible.GetInteraction())
                && this.IsFranchise().Equals(thatDeductible.IsFranchise()) && this.IsAbsorbable().Equals(thatDeductible.IsAbsorbable());
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region Methods
        public Interaction GetInteraction()
        {
            return DeductibleInteraction;
        }
        public bool IsFranchise()
        {
            return IsItFranchise;
        }
        public bool IsAbsorbable()
        {
            return IsItAbsorbable;
        }
        #endregion
    }
}
