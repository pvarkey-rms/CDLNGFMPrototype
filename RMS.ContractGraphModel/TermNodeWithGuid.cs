using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractGraphModel
{
    class TermNodeWithGuid : TermNode
    {
        #region Fields

        public Guid GUID { private set; get; }
        public HashSet<Guid> Parents { private set; get; }

        #endregion Fields

        #region Constructors

        public TermNodeWithGuid() : base() { Init(); }
        public TermNodeWithGuid(TermNode TermNode) : base(TermNode) { Init(); }
        private void Init()
        {
            GUID = Guid.NewGuid();
            Parents = new HashSet<Guid>();
        }

        #endregion

        #region Graph API Overrides

        public override void AddParent(TermNode Parent)
        {
            if (!(Parent is TermNodeWithGuid))
                throw new NotSupportedException();
            Parents.Add((Parent as TermNodeWithGuid).GUID);
        }

        public override void RemoveParent(TermNode Parent)
        {
            if (!(Parent is TermNodeWithGuid))
                throw new NotSupportedException();
            Parents.Remove((Parent as TermNodeWithGuid).GUID);
        }

        public override bool IsChildOf(TermNode AnotherTermNode)
        {
            if (!(AnotherTermNode is TermNodeWithGuid))
                return GetSubject().IsSubsetOf(AnotherTermNode.GetSubject());
            else
            {
                return this.Parents.Contains(((TermNodeWithGuid)AnotherTermNode).GUID);
            }
            
        }

        public override bool Overlaps(TermNode AnotherTermNode)
        {
            if (this.Parents.Overlaps(((TermNodeWithGuid)AnotherTermNode).Parents))
                return true;
            return base.Overlaps(AnotherTermNode);
            // DISABLING THE FOLLOWING DUE TO CHICKEN AND EGG PROBLEM
            //if (!(AnotherTermNode is TermNodeWithGuid))
            //    return GetSubject().Overlaps(AnotherTermNode.GetSubject());
            //else
            //    return this.Parents.Overlaps(((TermNodeWithGuid)AnotherTermNode).Parents);
        }

        public override bool OverlapsWithoutInclusion(TermNode AnotherTermNode)
        {
            if (!this.Parents.IsSubsetOf(((TermNodeWithGuid)AnotherTermNode).Parents)
                && !((TermNodeWithGuid)AnotherTermNode).Parents.IsSubsetOf(this.Parents)
                && this.Parents.Overlaps(((TermNodeWithGuid)AnotherTermNode).Parents))
                return true;
            return base.OverlapsWithoutInclusion(AnotherTermNode);
            // DISABLING THE FOLLOWING DUE TO CHICKEN AND EGG PROBLEM
            //if (!(AnotherTermNode is TermNodeWithGuid))
            //    return GetSubject().OverlapsWithoutInclusion(AnotherTermNode.GetSubject());
            //else
            //{
            //    if (this.IsChildOf(((TermNodeWithGuid)AnotherTermNode)))
            //        return false;

            //    if (((TermNodeWithGuid)AnotherTermNode).IsChildOf(this))
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

            if (obj.GetType() == typeof(TermNode))
                return base.Equals(obj);

            if (obj.GetType() != typeof(TermNodeWithGuid))
                return false;

            TermNodeWithGuid tnwguid = obj as TermNodeWithGuid;

            return this.Equals(tnwguid);
        }

        public bool Equals(TermNodeWithGuid tnwguid)
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
