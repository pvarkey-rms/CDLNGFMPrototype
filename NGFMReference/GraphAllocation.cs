using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFMReference;

namespace NGFMReference
{
    public class GraphAllocation
    {
        private Graph graph;
        
        public GraphAllocation(Graph _graph)
        {
            graph = _graph;
        }

        public void AllocateGraph()
        {
            CoverNode topCover = graph.TopNodes[0] as CoverNode;

            if (topCover == null)
                throw new InvalidOperationException("Top node must be a cover node! Cannot execute graph...");

            IAllocatable topIA = topCover as IAllocatable;

            //init the allocation state to be the lossState
            topCover.GetAllocState().collection[0].P = topCover.GetPayout();

            RecursiveAllocateIAllocatable(topIA);
        } //AllocateGraph

        private void RecursiveAllocateIAllocatable(IAllocatable currNode)
        {
            List<IAllocatable> childrenAndRites = graph.GetIAChildrenForIAllocatable(currNode);

            AllocationEngine allocater = new AllocationEngine(currNode, childrenAndRites, StateType.AllocatedState, StateType.LossState);
            allocater.Run();

            //Allocate all children nodes
            List<IAllocatable> childNodes = graph.GetChildrenForNode((GraphNode)currNode).Cast<IAllocatable>().ToList();
            foreach (IAllocatable childnode in childNodes)
            {
                RecursiveAllocateIAllocatable(childnode);
            }
        }
    }

    public interface IAllocatable
    {
        bool AllocateByRecoverable { get; }
        bool AllocateRecoverableFirst { get; }

        void SetAllocState(AllocationStateCollection state);
        LossStateCollection GetLossState();
        AllocationStateCollection GetAllocState();
    }

    public class AllocationEngine
    {
        private IAllocatable parent;
        private List<IAllocatable> childrenNodes;
        private StateType ParentType ;
        private StateType ChildrenType;

        public AllocationEngine(IAllocatable _parent, List<IAllocatable> _childrenNodes, StateType _ParentType, StateType _ChildrenType)
        {
            parent = _parent;
            childrenNodes = _childrenNodes;
            ParentType = _ParentType;
            ChildrenType = _ChildrenType;
        }

        public void Run()
        {
            if (childrenNodes.Count == 0)
                return;

            if (parent.AllocateRecoverableFirst)
            {
                AllocateRecoverable(parent, childrenNodes);
            }
            AllocatePayout(parent, childrenNodes);            
        }

        private void AllocateRecoverable(IAllocatable currNode, List<IAllocatable> childrenNodes)
        {
            Double childrenDSum = childrenNodes.Sum(item => item.GetLossState().GetTotalSum.D);
            Double childrenRSum = childrenNodes.Sum(item => item.GetLossState().GetTotalSum.R);
            Double diffD = currNode.GetAllocState().GetTotalSum.D - childrenDSum;
            int numOfChildren = childrenNodes.Count;

            if (childrenRSum == 0)
            {
                Double childrenSSum = childrenNodes.Sum(item => item.GetLossState().GetTotalSum.S);
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().collection[0].R = currNode.GetAllocState().collection[0].R * childNode.GetLossState().GetTotalSum.S / childrenSSum;
                }
            }
            else if (diffD >= 0)
            {
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().collection[0].R = (childNode.GetLossState().GetTotalSum.R * (1 - diffD / childrenRSum));
                    childNode.GetAllocState().collection[0].D = (childNode.GetLossState().GetTotalSum.S - childNode.GetLossState().GetTotalSum.X - childNode.GetAllocState().collection[0].R);
                }
            }
            else
            {
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().collection[0].D = childNode.GetLossState().GetTotalSum.D * (1 + diffD / childrenDSum);
                    childNode.GetAllocState().collection[0].R = childNode.GetLossState().GetTotalSum.S - childNode.GetLossState().GetTotalSum.X - childNode.GetAllocState().collection[0].D;
                }
            }

            Double childrenRaSum = childrenNodes.Sum(item => item.GetAllocState().GetTotalSum.R);
            if (diffD >= 0 && childrenRaSum == 0)
            {                
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().collection[0].R = childNode.GetLossState().GetTotalSum.R;
                    childrenRaSum += childNode.GetLossState().GetTotalSum.R;
                }             
            }
            foreach (IAllocatable childNode in childrenNodes)
            {
                if (currNode.GetAllocState().collection[0].R == 0)
                    childNode.GetAllocState().collection[0].R = 0;
                else if (childrenRaSum > 0)    
                    childNode.GetAllocState().collection[0].R = (currNode.GetAllocState().collection[0].R * childNode.GetAllocState().collection[0].R / childrenRaSum);
            }
        }

        private void AllocatePayout(IAllocatable currNode, List<IAllocatable> childrenNodes)
        {
            int numOfChildren = childrenNodes.Count;

            if (!parent.AllocateRecoverableFirst) //need copy LossState R, D, S, X to AllocationState
            {
                foreach (IAllocatable childNode in childrenNodes)
                {
                    if (childNode.AllocateByRecoverable)
                    {
                        childNode.GetAllocState().collection[0].R = childNode.GetLossState().GetTotalSum.R;
                        childNode.GetAllocState().collection[0].D = childNode.GetLossState().GetTotalSum.D;
                        childNode.GetAllocState().collection[0].X = childNode.GetLossState().GetTotalSum.X;
                        childNode.GetAllocState().collection[0].S = childNode.GetLossState().GetTotalSum.S;
                    }
                }
            
            }

            double childrenPSum = childrenNodes.Sum(item => item.GetAllocState().collection[0].P); 
            double childrenRSum = childrenNodes.Sum(item => item.GetAllocState().collection[0].R);

            if ((childrenPSum == 0 && !currNode.AllocateByRecoverable) || (childrenRSum == 0 && currNode.AllocateByRecoverable))
            {
                double childrenSSum = childrenNodes.Sum(item => item.GetLossState().GetTotalSum.S);
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().collection[0].P = currNode.GetAllocState().collection[0].P * childNode.GetLossState().GetTotalSum.S / childrenSSum;
                }
            }
            else
            {
                if (currNode.AllocateByRecoverable)
                {
                    foreach (IAllocatable childNode in childrenNodes)
                    {
                        childNode.GetAllocState().collection[0].P = currNode.GetAllocState().collection[0].P * childNode.GetAllocState().collection[0].R / childrenRSum;
                    }
                }
                else
                {
                    foreach (IAllocatable childNode in childrenNodes)
                    {
                        childNode.GetAllocState().collection[0].P = currNode.GetAllocState().collection[0].P * childNode.GetAllocState().collection[0].P / childrenPSum;
                    }
                }
            }
        }        
    }
     
    public class AllocationState
    {
        public double S { get; set; }
        public double X { get; set; }
        public double R { get; set; }
        public double D { get; set; }
        public double P { get; set; }


        public AllocationState()
        {
            S = 0.0;
            X = 0.0;
            R = 0.0;
            D = 0.0;
            P = 0.0;
        }

        public void Adjust()
        {
            X = Math.Min(X, S - D);
            D = Math.Min(D, S - X);
            R = S - X - D;
        }

        public void Reset()
        {
            S = 0.0;
            X = 0.0;
            R = 0.0;
            D = 0.0;
            P = 0.0;
        }

        public static AllocationState operator +(AllocationState loss1, AllocationState loss2)
        {
            AllocationState sum = new AllocationState();
            sum.S = loss1.S + loss2.S;
            sum.X = loss1.X + loss2.X;
            sum.D = loss1.D + loss2.D;
            sum.P = loss1.P + loss2.P;
            sum.R = loss1.R + loss2.R;
            return sum;
        }

        public LossState CopyToLossState()
        {
            LossState copiedState = new LossState();
            copiedState.D = D;
            copiedState.X = X;
            copiedState.R = R;
            copiedState.S = S;

            return copiedState;
        }
    }

    public class AllocationStateCollection : IEnumerable<AllocationState>
    {
        public AllocationState[] collection;
        public int NumBldgs
        {
            get { return collection.Count(); }
        }
        public AllocationState GetTotalSum
        {
            get
            {
                AllocationState total = new AllocationState();
                foreach (AllocationState loss in collection)
                {
                    total += loss;
                }
                return total;
            }
        }
        public double Loss
        {
            get
            {
                double total = 0;
                foreach (AllocationState loss in collection)
                {
                    total += loss.R;
                }
                return total;
            }
        }

        public AllocationStateCollection(int NumBldgs)
        {
            collection = new AllocationState[NumBldgs];
            for (int i = 0; i < NumBldgs; i++)
            {
                collection[i] = new AllocationState();
            }
        }

        public void SumLossesFrom(AllocationStateCollection otherLosses)
        {
            if (this.NumBldgs == otherLosses.NumBldgs)
            {
                collection.Zip(otherLosses.collection, (a, b) => a + b);
            }
            else if (this.NumBldgs == 1)
            {
                collection[0] += otherLosses.GetTotalSum;
            }

        }

        public LossStateCollection CopyToLossState()
        {
            LossStateCollection lossCol = new LossStateCollection(NumBldgs);
            int i = 0;
            foreach (AllocationState allocState in collection)
            {
                lossCol.collection[i] = allocState.CopyToLossState();
                i = i + 1;
            }

            return lossCol;
        }

        public double GetPayout()
        {
            double payout = 0;
            foreach (AllocationState state in collection)
            {
                payout += state.P;
            }

            return payout;
        }

        public void Reset()
        {
            for (int i = 0; i < NumBldgs; i++)
            {
                collection[i].Reset();
            }
        }

        public IEnumerator<AllocationState> GetEnumerator()
        {
            foreach (AllocationState state in collection)
            {
                // Lets check for end of list (its bad code since we used arrays)
                if (state == null)
                {
                    break;
                }

                // Return the current element and then on next function call 
                // resume from next element rather than starting all over again;
                yield return state;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }

    public enum StateType
    {
        LossState,
        AllocatedState
    }
}
