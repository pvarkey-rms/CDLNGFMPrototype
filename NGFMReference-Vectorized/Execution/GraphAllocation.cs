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
        private GraphOfNodes graph;
        
        public GraphAllocation(GraphOfNodes _graph)
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
            topCover.GetAllocState().Payout[0] = topCover.GetPayout();

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

        void SetAllocState(AllocationStateCollection2 state);
        LossStateCollection2 GetLossState();
        AllocationStateCollection2 GetAllocState();
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
                    childNode.GetAllocState().Recoverable[0] = currNode.GetAllocState().Recoverable[0] * childNode.GetLossState().GetTotalSum.S / childrenSSum;
                }
            }
            else if (diffD >= 0)
            {
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().Recoverable[0] = (childNode.GetLossState().GetTotalSum.R * (1 - diffD / childrenRSum));
                    childNode.GetAllocState().Deductible[0] = (childNode.GetLossState().GetTotalSum.S - childNode.GetLossState().GetTotalSum.X - childNode.GetAllocState().Recoverable[0]);
                }
            }
            else
            {
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().Deductible[0] = childNode.GetLossState().GetTotalSum.D * (1 + diffD / childrenDSum);
                    childNode.GetAllocState().Recoverable[0] = childNode.GetLossState().GetTotalSum.S - childNode.GetLossState().GetTotalSum.X - childNode.GetAllocState().Deductible[0];
                }
            }

            Double childrenRaSum = childrenNodes.Sum(item => item.GetAllocState().GetTotalSum.R);
            if (diffD >= 0 && childrenRaSum == 0)
            {                
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().Recoverable[0] = childNode.GetLossState().GetTotalSum.R;
                    childrenRaSum += childNode.GetLossState().GetTotalSum.R;
                }             
            }
            foreach (IAllocatable childNode in childrenNodes)
            {
                if (currNode.GetAllocState().Recoverable[0] == 0)
                    childNode.GetAllocState().Recoverable[0] = 0;
                else if (childrenRaSum > 0)    
                    childNode.GetAllocState().Recoverable[0] = (currNode.GetAllocState().Recoverable[0] * childNode.GetAllocState().Recoverable[0] / childrenRaSum);
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
                        childNode.GetAllocState().Recoverable[0] = childNode.GetLossState().GetTotalSum.R;
                        childNode.GetAllocState().Deductible[0] = childNode.GetLossState().GetTotalSum.D;
                        childNode.GetAllocState().Excess[0] = childNode.GetLossState().GetTotalSum.X;
                        childNode.GetAllocState().SubjectLoss[0] = childNode.GetLossState().GetTotalSum.S;
                    }
                }
            
            }

            double childrenPSum = childrenNodes.Sum(item => item.GetAllocState().Payout[0]);
            double childrenRSum = childrenNodes.Sum(item => item.GetAllocState().Recoverable[0]);

            if ((childrenPSum == 0 && !currNode.AllocateByRecoverable) || (childrenRSum == 0 && currNode.AllocateByRecoverable))
            {
                double childrenSSum = childrenNodes.Sum(item => item.GetLossState().GetTotalSum.S);
                foreach (IAllocatable childNode in childrenNodes)
                {
                    childNode.GetAllocState().Payout[0] = currNode.GetAllocState().Payout[0] * childNode.GetLossState().GetTotalSum.S / childrenSSum;
                }
            }
            else
            {
                if (currNode.AllocateByRecoverable)
                {
                    foreach (IAllocatable childNode in childrenNodes)
                    {
                        childNode.GetAllocState().Payout[0] = currNode.GetAllocState().Payout[0] * childNode.GetAllocState().Recoverable[0] / childrenRSum;
                    }
                }
                else
                {
                    foreach (IAllocatable childNode in childrenNodes)
                    {
                        childNode.GetAllocState().Payout[0] = currNode.GetAllocState().Payout[0] * childNode.GetAllocState().Payout[0] / childrenPSum;
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

    //public class AllocationStateCollection : IEnumerable<AllocationState>
    //{
    //    public AllocationState[] collection;
    //    public int NumBldgs
    //    {
    //        get { return collection.Count(); }
    //    }
    //    public AllocationState GetTotalSum
    //    {
    //        get
    //        {
    //            AllocationState total = new AllocationState();
    //            foreach (AllocationState loss in collection)
    //            {
    //                total += loss;
    //            }
    //            return total;
    //        }
    //    }
    //    public double Loss
    //    {
    //        get
    //        {
    //            double total = 0;
    //            foreach (AllocationState loss in collection)
    //            {
    //                total += loss.R;
    //            }
    //            return total;
    //        }
    //    }

    //    public AllocationStateCollection(int NumBldgs)
    //    {
    //        collection = new AllocationState[NumBldgs];
    //        for (int i = 0; i < NumBldgs; i++)
    //        {
    //            collection[i] = new AllocationState();
    //        }
    //    }

    //    public void SumLossesFrom(AllocationStateCollection otherLosses)
    //    {
    //        if (this.NumBldgs == otherLosses.NumBldgs)
    //        {
    //            collection.Zip(otherLosses.collection, (a, b) => a + b);
    //        }
    //        else if (this.NumBldgs == 1)
    //        {
    //            collection[0] += otherLosses.GetTotalSum;
    //        }

    //    }

    //    public LossStateCollection2 CopyToLossState()
    //    {
    //        LossStateCollection lossCol = new LossStateCollection(NumBldgs);
    //        int i = 0;
    //        foreach (AllocationState allocState in collection)
    //        {
    //            lossCol.collection[i] = allocState.CopyToLossState();
    //            i = i + 1;
    //        }

    //        return lossCol;
    //    }

    //    public double GetPayout()
    //    {
    //        double payout = 0;
    //        foreach (AllocationState state in collection)
    //        {
    //            payout += state.P;
    //        }

    //        return payout;
    //    }

    //    public void Reset()
    //    {
    //        for (int i = 0; i < NumBldgs; i++)
    //        {
    //            collection[i].Reset();
    //        }
    //    }

    //    public IEnumerator<AllocationState> GetEnumerator()
    //    {
    //        foreach (AllocationState state in collection)
    //        {
    //            // Lets check for end of list (its bad code since we used arrays)
    //            if (state == null)
    //            {
    //                break;
    //            }

    //            // Return the current element and then on next function call 
    //            // resume from next element rather than starting all over again;
    //            yield return state;
    //        }
    //    }

    //    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    //    {
    //        return GetEnumerator();
    //    }

    //}

    public class AllocationStateCollection2 : IEnumerable<AllocationState>
    {
        private double[] subjectloss;
        private double[] excess;
        private double[] recoverable;
        private double[] deductible;
        private double[] payout;

        public double[] SubjectLoss { get { return subjectloss; } set { subjectloss = value; } }
        public double[] Excess { get { return excess; } set { excess = value; } }
        public double[] Recoverable { get { return recoverable; } set { recoverable = value; } }
        public double[] Deductible { get { return deductible; } set { deductible = value; } }
        public double[] Payout { get { return payout; } set { payout = value; } }

        private int numbldgs;
        public int NumBldgs
        {
            get { return numbldgs; }
        }

        private AllocationState totalSum;
        public AllocationState GetTotalSum
        {
            get
            {
                return totalSum;
            }
        }
        public double Loss
        {
            get
            {
                return totalSum.P;
            }
        }

        public AllocationStateCollection2(int _NumBldgs)
        {
            numbldgs = _NumBldgs;
            subjectloss = new double[numbldgs];
            excess = new double[numbldgs];
            recoverable = new double[numbldgs];
            deductible = new double[numbldgs];
            payout = new double[numbldgs];

            totalSum = new AllocationState();
            CalcTotalSum();
        }

        public void CalcTotalSum()
        {
            AllocationState total = new AllocationState();

            double totalsub = 0;
            double totalexcess = 0;
            double totalrecov = 0;
            double totalded = 0;
            double totalpayout = 0;

            for (int i = 0; i < NumBldgs; i++)
            {
                totalsub += subjectloss[i];
                totalexcess += excess[i];
                totalrecov += recoverable[i];
                totalded += deductible[i];
                totalpayout += payout[i];
            }

            totalSum.S = totalsub;
            totalSum.X = totalexcess;
            totalSum.R = totalrecov;
            totalSum.D = totalded;
            totalSum.P = totalpayout;
        }

        public void SumLossesFrom(AllocationStateCollection2 otherLosses)
        {
            if (this.NumBldgs == otherLosses.NumBldgs)
            {
                double[] othersubjectloss = otherLosses.subjectloss;
                double[] otherexcess = otherLosses.excess;
                double[] otherrecoverable = otherLosses.recoverable;
                double[] otherdeductible = otherLosses.deductible;
                double[] otherpayout = otherLosses.payout;

                for (int i = 0; i < NumBldgs; i++)
                {
                    subjectloss[i] += othersubjectloss[i];
                    excess[i] += otherexcess[i];
                    recoverable[i] += otherrecoverable[i];
                    deductible[i] += otherdeductible[i];
                    payout[i] += otherpayout[i];
                }
            }
            else if (this.NumBldgs == 1)
            {
                subjectloss[0] += otherLosses.GetTotalSum.S;
                excess[0] += otherLosses.GetTotalSum.X;
                recoverable[0] += otherLosses.GetTotalSum.R;
                deductible[0] += otherLosses.GetTotalSum.D;
                payout[0] += otherLosses.GetTotalSum.P;
            }

            CalcTotalSum();

        }

        public LossStateCollection2 CopyToLossState()
        {
            LossStateCollection2 lossCol = new LossStateCollection2(NumBldgs);

            Array.Copy(subjectloss, lossCol.SubjectLoss, NumBldgs);
            Array.Copy(excess, lossCol.Excess, NumBldgs);
            Array.Copy(recoverable, lossCol.Recoverable, NumBldgs);
            Array.Copy(deductible, lossCol.Deductible, NumBldgs);

            return lossCol;
        }

        public double GetPayout()
        {
            return totalSum.P;
        }

        public void Reset()
        {
            Array.Clear(subjectloss, 0, NumBldgs);
            Array.Clear(excess, 0, NumBldgs);
            Array.Clear(recoverable, 0, NumBldgs);
            Array.Clear(deductible, 0, NumBldgs);
            Array.Clear(payout, 0, NumBldgs);

            totalSum.D = 0;
            totalSum.R = 0;
            totalSum.S = 0;
            totalSum.X = 0;
            totalSum.P = 0;
        }

        public IEnumerator<AllocationState> GetEnumerator()
        {
            for (int i = 0; i < NumBldgs; i++)
            {
                AllocationState state = new AllocationState();

                state.D = Deductible[i];
                state.R = Recoverable[i];
                state.S = SubjectLoss[i];
                state.X = Excess[i];
                state.P = Payout[i];

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
