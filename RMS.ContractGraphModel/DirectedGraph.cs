using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.ContractGraphModel
{
    class DirectedGraph<T> : HashSet<INode<T>>, IDirectedGraph<T>, IEnumerable<INode<T>>
    {
        Dictionary<INode<T>, HashSet<INode<T>>> ParentsForNodeMap;
        Dictionary<INode<T>, HashSet<INode<T>>> ChildrenForNodeMap;

        static readonly ICollection<INode<T>> EMPTYNodeCollection = (ICollection<INode<T>>)Enumerable.Empty<Node<T>>();

        public DirectedGraph() : base() 
        {
            ParentsForNodeMap = new Dictionary<INode<T>, HashSet<INode<T>>>();
            ChildrenForNodeMap = new Dictionary<INode<T>, HashSet<INode<T>>>();
        }

        public DirectedGraph(DirectedGraph<T> CopyFromThis) : base(CopyFromThis)
        {
            this.ParentsForNodeMap = CopyFromThis.ParentsForNodeMap;
            this.ChildrenForNodeMap = CopyFromThis.ChildrenForNodeMap;
        }

        /// <summary>
        /// Make a parent-child (i.e. directed) edge from the <code>parent</code> node to the <code>child</code> node. 
        /// If <code>AddAnyNodesNotPresent</code> is <code>true</code>, add into the graph any node not already present. 
        /// Fail, if either node is <code>null</code>; or, if <code>AddAnyNodesNotPresent</code> is false 
        /// and either node is not present in the graph.
        /// </summary>
        /// <returns><code>true</code> on success; <code>false</code> otherwise.</returns>
        public virtual bool MakeParentChildEdge(INode<T> parent, INode<T> child, bool AddAnyNodesNotPresent = false)
        {
            if (parent == null || child == null)
                return false;
            if (!AddAnyNodesNotPresent)
            {
                if (!Contains(parent) || !Contains(child))
                    return false;
            }

            if (!Contains(parent))
                Add(parent);
            
            if (!Contains(child))
                Add(child);

            if (!ParentsForNodeMap.ContainsKey(child))
                ParentsForNodeMap.Add(child, new HashSet<INode<T>>());

            if (!ChildrenForNodeMap.ContainsKey(parent))
                ChildrenForNodeMap.Add(parent, new HashSet<INode<T>>());

            return ParentsForNodeMap[child].Add(parent) && ChildrenForNodeMap[parent].Add(child);
        }

        public bool DeleteParentChildEdge(INode<T> parent, INode<T> child)
        {
            if (parent == null || child == null)
                throw new NullReferenceException();
            
            if (!Contains(parent))
                throw new KeyNotFoundException();

            if (!Contains(child))
                throw new KeyNotFoundException();

            if (!(ParentsForNodeMap.ContainsKey(child) && ParentsForNodeMap[child].Contains(parent))
                                ||
                !(ChildrenForNodeMap.ContainsKey(parent) && ChildrenForNodeMap[parent].Contains(child)))
                return false;

            return ParentsForNodeMap[child].Remove(parent) && ChildrenForNodeMap[parent].Remove(child);
        }

        public ICollection<INode<T>> GetParentsOfNode(INode<T> node)
        {
            if (!Contains(node))
                throw new KeyNotFoundException();
            if(!ParentsForNodeMap.ContainsKey(node))
                return EMPTYNodeCollection;
            return (ICollection<INode<T>>)ParentsForNodeMap[node];
        }
        public ICollection<INode<T>> GetChildrenOfNode(INode<T> node)
        {
            if (!Contains(node))
                throw new KeyNotFoundException();
            if (!ChildrenForNodeMap.ContainsKey(node))
                return EMPTYNodeCollection;
            return (ICollection<INode<T>>)ChildrenForNodeMap[node];
        }

        public bool IsReachable(INode<T> SourceNode, INode<T> TargetNode)
        {
            if (!Contains(SourceNode) || !Contains(TargetNode))
                throw new KeyNotFoundException();
            if (!ChildrenForNodeMap.ContainsKey(SourceNode))
                return false;
            if (ChildrenForNodeMap[SourceNode].Contains(TargetNode))
                return true;
            foreach (INode<T> Child in ChildrenForNodeMap[SourceNode])
            {
                if (IsReachable(Child, TargetNode))
                    return true;
                else
                    continue;
            }
            return false;
        }

        #region Overrides
        
        public bool Remove(INode<T> node)
        {
            if (!Contains(node))
                return false;

            bool success = true;

            // Get parents & remove child edge to node
            ICollection<INode<T>> ParentsOfNode = GetParentsOfNode(node);
            foreach (INode<T> ParentOfNode in ParentsOfNode)
            {
                ICollection<INode<T>> ChildrenOfParentOfNode = GetChildrenOfNode(ParentOfNode);
                success &= ChildrenOfParentOfNode.Remove(node);
            }

            // Get children & remove parent edge from node
            ICollection<INode<T>> ChildrenOfNode = GetChildrenOfNode(node);
            foreach (INode<T> ChildOfNode in ChildrenOfNode)
            {
                ICollection<INode<T>> ParentsOfChildrenOfNode = GetParentsOfNode(ChildOfNode);
                success &= ParentsOfChildrenOfNode.Remove(node);
            }

            // Remove node
            success &= base.Remove(node);

            return success;
        }
                
        #endregion
    }
}
