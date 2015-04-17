using System; 
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RMS.ContractObjectModel;

namespace RMS.ContractGraphModel
{
    using IGenericTerm = ITerm<Value>;

    public class TermCollection : SortedSet<IGenericTerm>, IEquatable<TermCollection>
    {
        Subject subject;
        static readonly TermComparer DefaultTermComparer = new TermComparer();

        public TermCollection(Subject subject) : this(subject, DefaultTermComparer) { }
        public TermCollection(Subject subject, IComparer<IGenericTerm> comparer) : base(comparer)
        {
            this.subject = subject;
        }

        public Subject GetSubject()
        {
            return subject;
        }

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(TermCollection))
                return false;

            TermCollection terms = obj as TermCollection;

            return (this.Equals(terms));
        }

        public bool Equals(TermCollection terms)
        {
            if (terms == null)
                return false;

            return this.subject.Equals(terms.subject);
        }

        public override int GetHashCode()
        {
            int hash = 23 * 41;
            hash = hash * 37 + subject.GetHashCode();
            return hash;
        }
        #endregion
    }

    public class TermComparer : Comparer<IGenericTerm>
    {
        public override int Compare(IGenericTerm x, IGenericTerm y)
        {
            if (x == null)
            {
                if (y == null)
                    return 0;
                else
                    return 1;
            }
            else if (y == null)
            {
                return -1;
            }

            if (x.Equals(y))
                return 0;

            if (x is ISublimit)
            {
                if (y is IDeductible)
                {
                    if ((x as ISublimit).IsNetOfDeductible())
                        return 1;
                    else
                        return -1;
                }
                else // both x & y are sublimits
                {
                    if ((x as ISublimit).IsNetOfDeductible() == (y as ISublimit).IsNetOfDeductible())
                        return Math.Sign(x.GetHashCode() - y.GetHashCode());
                    else if ((x as ISublimit).IsNetOfDeductible())
                        return 1;
                    else
                        return -1;
                }
            }
            else // x is a deductible
            {
                if (y is ISublimit)
                {
                    if ((y as ISublimit).IsNetOfDeductible())
                        return -1;
                    else
                        return 1;
                }
                else // both x & y are deductibles
                {
                    int comparison = (x as IDeductible).GetInteraction() - (y as IDeductible).GetInteraction();
                    if (comparison == 0)
                        return Math.Sign(x.GetHashCode() - y.GetHashCode());
                    else if (comparison > 0)
                        return -1;
                    else
                        return 1;
                }
            }
        }
    }
}
