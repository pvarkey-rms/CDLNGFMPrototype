using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractGraphModel
{
    class TermNodeWithGuid_ObsoleteV1 : TermNode_ObsoleteV1
    {
        #region Fields

        public Guid GUID { private set; get; }
        public HashSet<Guid> Parents { private set; get; }

        #endregion Fields

        #region Constructors

        public TermNodeWithGuid_ObsoleteV1() : base() { Init(); }
        public TermNodeWithGuid_ObsoleteV1(TermNode_ObsoleteV1 TermNode) : base(TermNode) { Init(); }
        private void Init()
        {
            GUID = Guid.NewGuid();
            Parents = new HashSet<Guid>();
        }

        #endregion

        #region Graph API Overrides

        public override void AddParent(TermNode_ObsoleteV1 Parent)
        {
            if (!(Parent is TermNodeWithGuid_ObsoleteV1))
                throw new NotSupportedException();
            Parents.Add((Parent as TermNodeWithGuid_ObsoleteV1).GUID);
        }

        public override void RemoveParent(TermNode_ObsoleteV1 Parent)
        {
            if (!(Parent is TermNodeWithGuid_ObsoleteV1))
                throw new NotSupportedException();
            Parents.Remove((Parent as TermNodeWithGuid_ObsoleteV1).GUID);
        }

        public override bool IsChildOf(TermNode_ObsoleteV1 AnotherTermNode)
        {
            if (!(AnotherTermNode is TermNodeWithGuid_ObsoleteV1))
                return GetSubject().IsSubsetOf(AnotherTermNode.GetSubject());
            else
            {
                return this.Parents.Contains(((TermNodeWithGuid_ObsoleteV1)AnotherTermNode).GUID);
            }
            
        }

        public override bool Overlaps(TermNode_ObsoleteV1 AnotherTermNode)
        {
            if (this.Parents.Overlaps(((TermNodeWithGuid_ObsoleteV1)AnotherTermNode).Parents))
                return true;
            return base.Overlaps(AnotherTermNode);
            // DISABLING THE FOLLOWING DUE TO CHICKEN AND EGG PROBLEM
            //if (!(AnotherTermNode is TermNodeWithGuid_ObsoleteV1))
            //    return GetSubject().Overlaps(AnotherTermNode.GetSubject());
            //else
            //    return this.Parents.Overlaps(((TermNodeWithGuid_ObsoleteV1)AnotherTermNode).Parents);
        }

        public override bool OverlapsWithoutInclusion(TermNode_ObsoleteV1 AnotherTermNode)
        {
            if (!this.Parents.IsSubsetOf(((TermNodeWithGuid_ObsoleteV1)AnotherTermNode).Parents)
                && !((TermNodeWithGuid_ObsoleteV1)AnotherTermNode).Parents.IsSubsetOf(this.Parents)
                && this.Parents.Overlaps(((TermNodeWithGuid_ObsoleteV1)AnotherTermNode).Parents))
                return true;
            return base.OverlapsWithoutInclusion(AnotherTermNode);
            // DISABLING THE FOLLOWING DUE TO CHICKEN AND EGG PROBLEM
            //if (!(AnotherTermNode is TermNodeWithGuid_ObsoleteV1))
            //    return GetSubject().OverlapsWithoutInclusion(AnotherTermNode.GetSubject());
            //else
            //{
            //    if (this.IsChildOf(((TermNodeWithGuid_ObsoleteV1)AnotherTermNode)))
            //        return false;

            //    if (((TermNodeWithGuid_ObsoleteV1)AnotherTermNode).IsChildOf(this))
            //        return false;

            //    return this.Overlaps(AnotherTermNode);
            //}
        }

        #endregion

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is TermNode_ObsoleteV1))
                return false;

            if (obj.GetType() == typeof(TermNode_ObsoleteV1))
                return base.Equals(obj);

            TermNodeWithGuid_ObsoleteV1 tnwguid = obj as TermNodeWithGuid_ObsoleteV1;

            return this.Equals(tnwguid);
        }

        public bool Equals(TermNodeWithGuid_ObsoleteV1 tnwguid)
        {
            if (tnwguid == null)
                return false;

            return this.GUID.Equals(tnwguid.GUID);
        }

        public override int GetHashCode()
        {
            return this.GUID.GetHashCode();
        }
        #endregion
    }
}
