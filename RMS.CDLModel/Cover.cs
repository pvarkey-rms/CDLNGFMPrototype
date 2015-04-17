using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

using RMS.ContractObjectModel;

namespace RMS.ContractObjectModel
{
    using IGenericCover = ICover<Value, Value, Value>;

    [ProtoContract]
    [ProtoInclude(1, typeof(Cover<Value, Value, Value>))]
    public interface ICover<out ShareValue, out LimitValue, out AttachmentValue>
        where ShareValue : Value
        where LimitValue : Value
        where AttachmentValue : Value
    {
        SymbolicExpression GetLabel();
        void SetLabel(string _Label); // WARNING : violates immutability of cover
        IExpression<ShareValue> GetParticipation();
        ILimit<LimitValue> GetLimit();
        IAttachment<AttachmentValue> GetAttachment();
        SubjectPosition GetSubject();
        FunctionInvocation<IValue<AValue>> GetDerivedSubject();
    }

    [Serializable]
    [ProtoContract]
    public class Cover<ShareValue, LimitValue, AttachmentValue>
        : ICover<ShareValue, LimitValue, AttachmentValue>, 
        IEquatable<Cover<ShareValue, LimitValue, AttachmentValue>>
        //IComparable<Cover<ShareValue, LimitValue, AttachmentValue>>
        where ShareValue : Value
        where LimitValue : Value
        where AttachmentValue : Value
    {
        #region Fields

        private SymbolicExpression Label;
        private Participation<ShareValue> _Participation;
        private Limit<LimitValue> _Limit;
        private Attachment<AttachmentValue> _Attachment;
        private SubjectPosition _Subject;
        private FunctionInvocation<IValue<AValue>> DerivedSubject;

        #endregion

        #region Constructors

        public Cover(Participation<ShareValue> share) 
            : this(share, null, null, null, null, null) { }
        
        public Cover(Participation<ShareValue> share, FunctionInvocation<IValue<AValue>> DerivedSubject)
            : this(share, null, null, null, DerivedSubject, null) { }
        
        public Cover(Participation<ShareValue> share, string Label) 
            : this(share, null, null, null, null, Label) { }
        
        public Cover(Participation<ShareValue> share, FunctionInvocation<IValue<AValue>> DerivedSubject, string Label)
            : this(share, null, null, null, DerivedSubject, Label) { }
        
        public Cover(Participation<ShareValue> share, SubjectPosition subject) 
            : this(share, null, null, subject, null, null) { }
        
        public Cover(Participation<ShareValue> share, SubjectPosition subject, FunctionInvocation<IValue<AValue>> DerivedSubject)
            : this(share, null, null, subject, DerivedSubject, null) { }
        
        public Cover(Participation<ShareValue> share, SubjectPosition subject, string Label) 
            : this(share, null, null, subject, null, Label) { }
        
        public Cover(Participation<ShareValue> share, SubjectPosition subject, FunctionInvocation<IValue<AValue>> DerivedSubject, string Label)
            : this(share, null, null, subject, DerivedSubject, Label) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit)
            : this(share, limit, null, null, null, null) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, FunctionInvocation<IValue<AValue>> DerivedSubject)
            : this(share, limit, null, null, DerivedSubject, null) { }
        
        public Cover(Participation<ShareValue> share, Attachment<AttachmentValue> attachment)
            : this(share, null, attachment, null, null, null) { }
        
        public Cover(Participation<ShareValue> share, Attachment<AttachmentValue> attachment, FunctionInvocation<IValue<AValue>> DerivedSubject)
            : this(share, null, attachment, null, DerivedSubject, null) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, Attachment<AttachmentValue> attachment)
            : this(share, limit, attachment, null, null, null) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, Attachment<AttachmentValue> attachment, FunctionInvocation<IValue<AValue>> DerivedSubject)
            : this(share, limit, attachment, null, DerivedSubject, null) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, string Label)
            : this(share, limit, null, null, null, Label) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, FunctionInvocation<IValue<AValue>> DerivedSubject, string Label)
            : this(share, limit, null, null, DerivedSubject, Label) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, SubjectPosition subject)
            : this(share, limit, null, subject, null, null) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, SubjectPosition subject, FunctionInvocation<IValue<AValue>> DerivedSubject)
            : this(share, limit, null, subject, DerivedSubject, null) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, SubjectPosition subject, string Label)
            : this(share, limit, null, subject, null, Label) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, SubjectPosition subject, FunctionInvocation<IValue<AValue>> DerivedSubject, string Label)
            : this(share, limit, null, subject, DerivedSubject, Label) { }
        
        public Cover(Participation<ShareValue> share, Attachment<AttachmentValue> attachment, string Label)
            : this(share, null, attachment, null, null, Label) { }
        
        public Cover(Participation<ShareValue> share, Attachment<AttachmentValue> attachment, FunctionInvocation<IValue<AValue>> DerivedSubject, string Label)
            : this(share, null, attachment, null, DerivedSubject, Label) { }
        
        public Cover(Participation<ShareValue> share, Attachment<AttachmentValue> attachment, SubjectPosition subject)
            : this(share, null, attachment, subject, null, null) { }
        
        public Cover(Participation<ShareValue> share, Attachment<AttachmentValue> attachment, SubjectPosition subject, FunctionInvocation<IValue<AValue>> DerivedSubject)
            : this(share, null, attachment, subject, DerivedSubject, null) { }
        
        public Cover(Participation<ShareValue> share, Attachment<AttachmentValue> attachment, SubjectPosition subject, string Label)
            : this(share, null, attachment, subject, null, Label) { }
        
        public Cover(Participation<ShareValue> share, Attachment<AttachmentValue> attachment, SubjectPosition subject, FunctionInvocation<IValue<AValue>> DerivedSubject, string Label)
            : this(share, null, attachment, subject, DerivedSubject,Label) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, Attachment<AttachmentValue> attachment, string Label)
            : this(share, limit, attachment, null, null, Label) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, Attachment<AttachmentValue> attachment, FunctionInvocation<IValue<AValue>> DerivedSubject, string Label)
            : this(share, limit, attachment, null, DerivedSubject,Label) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, Attachment<AttachmentValue> attachment, SubjectPosition subject)
            : this(share, limit, attachment, subject, null, null) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, Attachment<AttachmentValue> attachment, SubjectPosition subject, FunctionInvocation<IValue<AValue>> DerivedSubject)
            : this(share, limit, attachment, subject, DerivedSubject, null) { }
        
        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, Attachment<AttachmentValue> attachment, SubjectPosition subject, string Label)
            : this(share, limit, attachment, subject, null, Label) { }

        public Cover(Participation<ShareValue> share, Limit<LimitValue> limit, Attachment<AttachmentValue> attachment,
            SubjectPosition subject, FunctionInvocation<IValue<AValue>> DerivedSubject, string Label)
        {
            this.Label = (Label == null) ? null : new SymbolicExpression(Label);
            this._Participation = share;
            this._Limit = limit;
            this._Attachment = attachment;
            this._Subject = subject;
            this.DerivedSubject = DerivedSubject;
        }

        public Cover(Cover<ShareValue, LimitValue, AttachmentValue> CopyFrom, SymbolicExpression NewLabel)
        {
            this.Label = NewLabel;
            this._Participation = CopyFrom._Participation;
            this._Limit = CopyFrom._Limit;
            this._Attachment = CopyFrom._Attachment;
            this._Subject = new Subject((Subject)CopyFrom.GetSubject());
            this.DerivedSubject = CopyFrom.GetDerivedSubject();
        }

        public Cover(Cover<ShareValue, LimitValue, AttachmentValue> CopyFrom, SymbolicExpression NewLabel, 
            FunctionInvocation<IValue<AValue>> NewDerivedSubject)
        {
            this.Label = NewLabel;
            this._Participation = CopyFrom._Participation;
            this._Limit = CopyFrom._Limit;
            this._Attachment = CopyFrom._Attachment;
            this._Subject = CopyFrom.GetSubject(); // TODO : comment this later?
            this.DerivedSubject = NewDerivedSubject;
        }
        #endregion

        #region Methods

        public override string ToString()
        {
            return "COVER " + ((Label != null) ? Label.ToString() : "") + "\n{\n" +
                "\tShare: " + _Participation.ToString() + "\n" +
                "\t_Limit: " + ((_Limit != null) ? _Limit.ToString() : "") + "\n" +
                "\t_Attachment: " + ((_Attachment != null) ? _Attachment.ToString() : "") + "\n" +
                "}";
        }

        public SymbolicExpression GetLabel()
        {
            return Label;
        }

        public void SetLabel(string _Label) // WARNING : violates immutability of cover
        {
            if (this.Label != null)
                throw new InvalidOperationException("Cover labels, if present (i.e. not null), are immutable!");
            this.Label = new SymbolicExpression(_Label);
        }

        public IExpression<ShareValue> GetParticipation()
        {
            return _Participation;
        }

        public ILimit<LimitValue> GetLimit()
        {
            return _Limit;
        }

        public IAttachment<AttachmentValue> GetAttachment()
        {
            return _Attachment;
        }

        public SubjectPosition GetSubject()
        {
            return _Subject;
        }
        
        public FunctionInvocation<IValue<AValue>> GetDerivedSubject()
        {
            return DerivedSubject;
        }

        #endregion

        #region Equality Overrides

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (this == obj)
                return true;

            if (obj.GetType() != typeof(Cover<ShareValue, LimitValue, AttachmentValue>))
                return false;

            Cover<ShareValue, LimitValue, AttachmentValue> e = obj as Cover<ShareValue, LimitValue, AttachmentValue>;

            return (this.Equals(e));
        }

        public bool Equals(Cover<ShareValue, LimitValue, AttachmentValue> that)
        {
            if (that == null)
                return false;

            if (this == that)
                return true;

            if (this.Label != null)
                return this.Label.Equals(that.Label);
            else if (that.Label != null)
                return false;

            bool IsEqual = true;

            if (this._Participation != null)
                IsEqual &= this._Participation.Equals(that._Participation);
            else if (that._Participation != null)
                return false;

            if (this._Limit != null)
                IsEqual &= this._Limit.Equals(that._Limit);
            else if (that._Limit != null)
                return false;

            if (this._Attachment != null)
                IsEqual &= this._Attachment.Equals(that._Attachment);
            else if (that._Participation != null)
                return false;

            if (this._Subject != null)
                IsEqual &= this._Subject.Equals(that._Subject);
            else if (that._Subject != null)
                return false;

            if (this.DerivedSubject != null)
                IsEqual &= this.DerivedSubject.Equals(that.DerivedSubject);
            else if (that.DerivedSubject != null)
                return false;

            return IsEqual;
        }

        public override int GetHashCode()
        {
            int hash = 23;

            if (Label != null)
                return hash * 37 + Label.GetHashCode();

            if (this._Participation != null)
                hash = hash * 37 + _Participation.GetHashCode();

            if (this._Limit != null)
                hash = hash * 37 + _Limit.GetHashCode();

            if (this._Attachment != null)
                hash = hash * 37 + _Attachment.GetHashCode();

            if (this._Subject != null)
                hash = hash * 37 + _Subject.GetHashCode();

            if (this.DerivedSubject != null)
                hash = hash * 37 + DerivedSubject.GetHashCode();

            return hash;
        }

        #endregion
    }

    [Serializable]
    [ProtoContract]
    public class Cover<V> : Cover<V, V, V>
        where V : Value
    {
        #region Constructors
        public Cover(Participation<V> share) : base(share) { }
        public Cover(Participation<V> share, Limit<V> limit) : base(share, limit) { }
        public Cover(Participation<V> share, string Label) : base(share, Label) { }
        #endregion
    }

    public class WrappedCover
    {
        private IGenericCover Cover;
        private Subject subject;
        private List<SymbolicExpression> childCoverLabels;
        
        public WrappedCover(IGenericCover _cover)
        {
            Cover = _cover;
            subject = (Subject)_cover.GetSubject();
            PopulateChildCoverLables();
            ExplodedCoverLabels = new List<SymbolicExpression>();
            ExplodedCovers = new HashSet<IGenericCover>();
            IsExploded = false;
        }

        public Schedule schecule
        {
            get
            {
                return subject.Schedule;
            }
        }
        public HashSet<IGenericCover> ExplodedCovers { get; set; }
        public List<SymbolicExpression> ExplodedCoverLabels { get; set; }

        public SymbolicExpression CoverLabel
        {
            get
            {
                return Cover.GetLabel();
            }
        }
        public FunctionInvocation<IValue<AValue>> DerivedSubject
        {
            get
            {
                return GetDerivedSubject();
            }
        }
        public List<SymbolicExpression> ChildCoverLabels
        {
            get { return childCoverLabels; }
        }
        public IGenericCover GetOriginalCover()
        {
            return Cover;
        }

        public bool IsPerRisk
        {
            get
            {
                return subject.PerRisk;
            }
        }

        public bool IsDerived
        {
            get
            {
                return subject.IsDerived();
            }
        }

        public bool IsExploded
        {
            get;
            set;
        }

        private FunctionInvocation<IValue<AValue>> GetDerivedSubject()
        {
            return Cover.GetDerivedSubject();
        }
        private void PopulateChildCoverLables()
        {
            childCoverLabels = new List<SymbolicExpression>();
            if (subject.isDerived)
            {
                object[] parameteterObjects = Cover.GetDerivedSubject().GetParameters();
                foreach (SimpleExpression<SymbolicValue> childcoverlabel in parameteterObjects)
                {

                    SymbolicValue temp = new SymbolicValue(childcoverlabel.GetValue().ToString());
                    SymbolicExpression ChildLabelExpression = new SymbolicExpression(temp);

                    childCoverLabels.Add(ChildLabelExpression);
                }
            }
        }
    }

}
