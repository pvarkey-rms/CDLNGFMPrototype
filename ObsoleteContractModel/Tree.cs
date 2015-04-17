using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS.ContractObjectModel
{
    public class Tree<T> where T : Node
    {
        #region Fields
        private bool m_bIsInit;

        private T m_rootNode;

        //cache of nodes in tree, indexed by internal id
        private Dictionary<int,T> m_mapIntIdToNode;
        //mapping of external node id to internal id
        private Dictionary<string,int> m_mapExtnIdToIntId;
        #endregion

        #region Constructors
        public Tree()
        {
            this.m_mapIntIdToNode = new Dictionary<int,T>();
            this.m_mapExtnIdToIntId = new Dictionary<string,int>();

            this.m_bIsInit = false;
        }
        #endregion

        #region Methods
        public void Initialize(string rootName)
        {
            Node rootNode = Activator.CreateInstance<T>();
            rootNode.ExtnId = rootName;
            
            //rootNode.Level = 0;

            this.SetRootNode((T)rootNode);
            m_bIsInit = true;
        }

        public void SetRootNode(T rootNode)
        {
            this.m_rootNode = rootNode;
            this.m_mapExtnIdToIntId.Add(rootNode.ExtnId, rootNode.IntId);
            this.m_mapIntIdToNode.Add(rootNode.IntId, rootNode);
        }

        public int GetNodeIntIdByExtnId(string extnId)
        {
            if (this.m_mapExtnIdToIntId.ContainsKey(extnId))
            {
                return this.m_mapExtnIdToIntId[extnId];
            }
            else
            {
                return -1;
            }
        }
        public T GetNodeByIntId(int intId)
        {
            if (this.m_mapIntIdToNode.ContainsKey(intId))
            {
                return this.m_mapIntIdToNode[intId];
            }
            else
            {
                return null;
            }
        }

        private void SearchForParent(Node parentNode, Node nodeToAdd)
        {
            bool bFound = false;

            List<Node> vInitChildren = new List<Node>();
            foreach (Node n in parentNode.vChildNode)
            {
                vInitChildren.Add(n);
            }


            int nInitChildren = vInitChildren.Count;
            for (int cIdx=0; cIdx < nInitChildren; cIdx++)
            {
                Node childNode = vInitChildren[cIdx];

                if (nodeToAdd.IsProperSubsetTo(childNode))
                {
                    //drill into the tree further
                    //SearchForParent(parentNode.vChildNode[cIdx], nodeToAdd);
                    SearchForParent(childNode, nodeToAdd);
                    bFound = true;
                }
                else if (childNode.IsProperSubsetTo(nodeToAdd) )
                {
                    //sever prior node dependency
                    parentNode.vChildNode.Remove(childNode);
                    childNode.vParentNode.Remove(parentNode);

                    //remove subject as risk
                    parentNode.Subject.Schedule.RemoveRiskItemFromSchedule(childNode);

                    //todo: need to reconsider internal id mechanism
                    childNode.Description = childNode.IntId.ToString();

                    //need to insert above the child
                    childNode.AddParentNode(nodeToAdd);
                    nodeToAdd.AddChildNode(childNode);

                    nodeToAdd.AddParentNode(parentNode);

                    nodeToAdd.Subject.Schedule.AddRiskItemToSchedule(childNode);
                    parentNode.Subject.Schedule.AddRiskItemToSchedule(nodeToAdd);

                    //don't want to mess up the iteration
                    parentNode.InsertChildNode(nodeToAdd, cIdx);
                    bFound = true;
                }
            }

            if (!bFound )
            {
                parentNode.AddChildNode(nodeToAdd);

                nodeToAdd.Description = nodeToAdd.IntId.ToString();
                parentNode.Subject.Schedule.AddRiskItemToSchedule(nodeToAdd);

                nodeToAdd.AddParentNode(parentNode);
            }
        }

        public void HangNodeToLowestChild(T nodeToAdd, bool bDetectRelationship)
        {
            if (!this.m_mapIntIdToNode.ContainsKey(nodeToAdd.IntId))
            {
                List<T> leafNodes = GetAllLeafNodes() ;
                //foreach (T leafnode in leafNodes)
                {

                    List<T> vAncestorsStack = new List<T>();
                    
                    List<T> ancestors = GetAllAncestors(leafNodes, vAncestorsStack);
                    ancestors.Distinct();

                    foreach (T ancestor in ancestors)
                    {
                            ancestor.Visited = false;
                    }
                }
                 bool bFound = false;
  
                //foreach (T leafnode in leafNodes)
                {

                    List<T> vAncestorsStack = new List<T>();
                    List<Node> hookedNode = new List<Node>();

                    List<T> ancestors = GetAllAncestors(leafNodes, vAncestorsStack);
     
                    foreach (T ancestor in ancestors)
                    {
                        if (!ancestor.Visited && !bFound)
                        {
                            ancestor.Visited = true;

                            if (nodeToAdd.IsProperSubsetTo(ancestor))
                            {
                                if (!ancestor.vChildNode.Contains(nodeToAdd))
                                {
                                    //if new parent is not an ancestor for already hooked

                                    if (CheckRelationship(ancestor, hookedNode))
                                    {
                                        ancestor.AddChildNode(nodeToAdd);
                                        nodeToAdd.AddParentNode(ancestor);
                                        ancestor.Subject.Schedule.AddRiskItemToSchedule(nodeToAdd);
                                        //bFound = true;
                                        hookedNode.Add(ancestor);
                                    }
                                }
                            }
                            else if (ancestor.IsProperSubsetTo(nodeToAdd))
                            {
                                List<Node> parents = ancestor.vParentNode.ToList();
                                foreach (Node parent in parents) // all of the ancestor's parents will now have the new node as child
                                {
                                    if (nodeToAdd.IsProperSubsetTo(parent))
                                    {
                                        if (parent.vChildNode.Contains(ancestor))
                                        {
                                            parent.vChildNode.Remove(ancestor);
                                            parent.Subject.Schedule.RemoveRiskItemFromSchedule(ancestor);
                                            ancestor.vParentNode.Remove(parent);
                                        }

                                        if (!parent.vChildNode.Contains(nodeToAdd))
                                        {
                                            if (CheckRelationship(parent,hookedNode))
                                            {
                                                parent.vChildNode.Add(nodeToAdd);
                                                parent.Subject.Schedule.AddRiskItemToSchedule(nodeToAdd);
                                                nodeToAdd.AddParentNode(parent);
                                                //bFound = true;
                                                hookedNode.Add(parent);
                                            }
                                        }
                                    }

                                }

                                if (!nodeToAdd.vChildNode.Contains(ancestor))
                                {
                                    nodeToAdd.AddChildNode(ancestor);
                                    ancestor.AddParentNode(nodeToAdd);
                                }
                            }
                        }
                    }
 
                }
            }
        }


        private bool CheckRelationship (Node t, List<Node> hookedNodes)
        {
            foreach (Node n in hookedNodes)
            {
                if (n.IsProperSubsetTo(t))
                    return false ;
            }

            return true;
        }

        public void SearchUpTreeRecursive(T node, T nodeToAdd)
        {
            TermNode tn = node as TermNode;
            if (!tn.Visited)
            {
                tn.Visited = true;
                if (nodeToAdd.IsProperSubsetTo(node))
                {//hang node 
                    node.AddChildNode(nodeToAdd);
                    nodeToAdd.AddParentNode(node);
                    SetPathToRootVisited(node);
                }
                else if (node.IsProperSubsetTo(nodeToAdd))
                {//insert node
                }
            }
        }


        public void SetPathToRootVisited(T node)
        {
            if (node.vParentNode.Count == 0)
                return;

            foreach (T p in node.vParentNode)
            {
                SetPathToRootVisited(p);
            }
        }


        public void AddNodeToTree(T node, bool bDetectRelationship)
        {
            //Set all visited false;

            if (!this.m_mapIntIdToNode.ContainsKey(node.IntId))
            {
                if (bDetectRelationship)
                {
                    SearchForParent(this.m_rootNode, node);
                    //HangNodeToLowestChild(node, true );
                }
                else
                {
                    //to be determined
                }

                this.m_mapExtnIdToIntId.Add(node.ExtnId, node.IntId);
                this.m_mapIntIdToNode.Add(node.IntId, node);
            }
        }

        public static List<T> GetAllAncestors(List<T> nodes, List<T> vAncestorStack)
        {
            foreach (T node in nodes)
            {
                if (!vAncestorStack.Contains(node))
                    vAncestorStack.Add(node);
            }

            List<T> parents= new List<T>();
        
            foreach (T node in nodes)
            {
                foreach (T parent in node.vParentNode)
                {
                    parents.Add(parent);
                }
            }

            if (parents.Count == 0)
                return vAncestorStack;
           return GetAllAncestors(parents, vAncestorStack);

        }

        public static List<T> GetAllAncestors(T node)
        {
            List<T> vParentNode = new List<T>();

            foreach (T parentNode in node.vParentNode)
            {
                vParentNode.Add(parentNode);
                vParentNode.AddRange(GetAllAncestors(parentNode));
            }
            return vParentNode;
        }


        public List<T> GetAllNodes()
        {
            return this.m_mapIntIdToNode.Values.ToList();
        }

        public List<T> GetAllLeafNodes()
        {
            List<T> vLeafNodes = new List<T>();

            foreach (T t in this.m_mapIntIdToNode.Values)
            {
                if (t.vChildNode.Count == 0)
                {
                    vLeafNodes.Add(t);
                }
            }

            return vLeafNodes;
        }
        #endregion

        #region Properties
        public T RootNode
        {
            get { return this.m_rootNode; }
            set { this.m_rootNode = value; }
        }

        public Dictionary<int, T> MapIntIdToNode
        {
            get { return this.m_mapIntIdToNode; }
        }
        #endregion
    }
}
