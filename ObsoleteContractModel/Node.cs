using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RMS.Utilities;

namespace RMS.ContractObjectModel
{
    public abstract class Node : RiskItem
    {
        #region Fields

        protected List<Node> m_vParentNode;
        protected List<Node> m_vChildNode;
        protected __Subject m_subject;
        private bool m_visited;

        #endregion

        #region Constructors
        public Node()
        {
            this.m_vParentNode = new List<Node>();
            this.m_vChildNode = new List<Node>();
        }
        #endregion

        #region Abstract Methods
        public abstract bool IsSubsetTo(Node n);
        public abstract bool IsProperSubsetTo(Node n);
        public abstract bool Overlaps(Node n);
        public abstract void CalculateLoss(SimulationMsg cdlMsg, bool bDebug);

        #endregion

        #region Methods
        public void AddChildNode(Node n)
        {
            if (!this.m_vChildNode.Contains(n))
            {
                this.m_vChildNode.Add(n);
            }
        }
        public void InsertChildNode(Node n, int nIdx)
        {
            if (!this.m_vChildNode.Contains(n))
            {
                this.m_vChildNode.Insert(nIdx, n);
            }
        }
        public void InsertParentNode(Node n, int nIdx)
        {
            if (!this.m_vParentNode.Contains(n))
            {
                this.m_vParentNode.Insert(nIdx, n);
            }
        }
        public void AddParentNode(Node n)
        {
            if (!this.m_vParentNode.Contains(n))
            {
                this.m_vParentNode.Add(n);
            }
        }
        public static void ResetIdentity()
        {
            s_idGen.Reset();
        }
        #endregion

        #region Properties
        public __Subject Subject
        {
            get { return this.m_subject; }
            set { this.m_subject = value; }
        }
        public List<Node> vParentNode
        {
            get { return this.m_vParentNode; }
            set { this.m_vParentNode = value; }
        }
        public List<Node> vChildNode
        {
            get { return this.m_vChildNode; }
            set { this.m_vChildNode = value; }
        }
        public bool Visited
        {
            get { return this.m_visited; }
            set { this.m_visited = value; }
        }

        #endregion
    }
}
