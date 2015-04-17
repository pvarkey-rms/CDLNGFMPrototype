using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class Deductible
    {
        public bool DedIsPerRisk { get; private set; }
        public Value Value { get; private set; }
        public bool DedIsFranchise { get; private set; }
        public TermValueType DedType { get; private set; }
        public DedInteractionType DedInterType { get; private set; }
        public double Amount { get { return this.Value.Amount; } }

        public Deductible()
        {

        }

        public Deductible(bool _dedIsFranchise, DedInteractionType _dedInterType, Value _deductible, bool _dedIsPerRisk, TermValueType _dedBaseType)
        {
            DedIsFranchise = _dedIsFranchise;
            Value = _deductible;
            DedInterType = _dedInterType;
            DedIsPerRisk = _dedIsPerRisk;
            DedType = _dedBaseType;
        }

        public void SetFinTerms(bool _dedIsFranchise, DedInteractionType _dedInterType, Value _deductible, bool _dedIsPerRisk, TermValueType _dedBaseType)
        {
            DedIsFranchise = _dedIsFranchise;
            Value = _deductible;
            DedInterType = _dedInterType;
            DedIsPerRisk = _dedIsPerRisk;
            DedType = _dedBaseType;
        }

    }

    public class DeductibleCollection :IEnumerable<Deductible>
    {
        private List<Deductible> dedList;
        public bool IsPerRisk 
        {
            get
            {
                if (dedList.Count == 0)
                    return false;
                else
                    return dedList[0].DedIsPerRisk;
            }
        }

        public DeductibleCollection()
        {
            dedList = new List<Deductible>();
        }

        //public double GetMinDed()
        //{
        //    double minDed = 0.0;
        //    foreach (Deductible d in dedList)
        //    {
        //        if (d.DedInterType == DedInteractionType.MIN)
        //            minDed = Math.Max(minDed, d.Amount);                
        //    }
        //    return minDed;
        //}

        //public double GetMaxDed()
        //{
        //    double maxDed = 0.0;
        //    foreach (Deductible d in dedList)
        //    {
        //        if (d.DedInterType == DedInteractionType.MAX)
        //            maxDed = Math.Min(maxDed, d.Amount);
        //    }
        //    return maxDed;
        //}

        //public double GetAbsorbingDed()
        //{
        //    double absorbingDed = 0.0;
        //    foreach (Deductible d in dedList)
        //    {
        //        if (d.DedInterType == DedInteractionType.Absorbing)
        //            absorbingDed = Math.Max(absorbingDed, d.Amount);
        //    }
        //    return absorbingDed;
        //}


        //public double GetSingleLargestDed()
        //{
        //    double singleLargestDed = 0.0;
        //    foreach (Deductible d in dedList)
        //    {
        //        if (d.DedInterType == DedInteractionType.SingleLargest)
        //            singleLargestDed = Math.Max(singleLargestDed, d.Amount);
        //    }
        //    return singleLargestDed;
        //}
        

        public DeductibleCollection(Deductible ded)
        {
            dedList = new List<Deductible>();
            dedList.Add(ded);
        }

        public void Add(Deductible ded)
        {
            if (dedList.Count == 0 || ded.DedIsPerRisk == dedList[0].DedIsPerRisk)
                dedList.Add(ded);
            else
                throw new InvalidOperationException("Cannot have two deductibles in a term's deductible collection, with different Per Risk values!");
        }

        public IEnumerator<Deductible> GetEnumerator()
        {
            return dedList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            // Lets call the generic version here
            return this.GetEnumerator();
        }

        public DeductibleCollection SortByExecOrder(DeductibleCollection preSorted)
        { 
            //set the Ded Collection order as the execution follows
            DeductibleCollection postSorted = new DeductibleCollection();

            int numOfDed = preSorted.dedList.Count;
            for (int i = 0; i < numOfDed; i++)
            {
                if (preSorted.dedList[i].DedInterType == DedInteractionType.SingleLargest)
                {
                    postSorted.Add(preSorted.dedList[i]);
                }
            }


            for (int i = 0; i < numOfDed; i++)
            {
                if (preSorted.dedList[i].DedInterType == DedInteractionType.MIN || preSorted.dedList[i].DedInterType == DedInteractionType.Absorbing)
                {
                    postSorted.Add(preSorted.dedList[i]);
                }
            }

            for (int i = 0; i < numOfDed; i++)
            {
                if (preSorted.dedList[i].DedInterType == DedInteractionType.MAX)
                {
                    postSorted.Add(preSorted.dedList[i]);                       
                }
            }

           if (preSorted.dedList.Count != postSorted.dedList.Count)
                throw new InvalidOperationException("Not all Deductibles are places in sorted order");
                         
            return postSorted;
        }
    }
}
