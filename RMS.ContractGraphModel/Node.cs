using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractGraphModel
{
    public class Node<T> : INode<T>, IEquatable<Node<T>>
    {
        T Content;

        public Node() {}
        public Node(T Contents)
        {
            this.Content = Contents;
        }

        public T GetContent()
        {
            return Content;
        }

        #region Equality Overrides
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj.GetType() != typeof(Node<T>))
                return false;

            Node<T> n = obj as Node<T>;

            return this.Equals(n);
        }

        public bool Equals(Node<T> n)
        {
            if (n == null)
                return false;

            return (Content.Equals(n.Content));
        }

        public override int GetHashCode()
        {
            int hash = 23;
            hash = hash * 37 + Content.GetHashCode();
            return hash;
        }
        #endregion
    }
}
