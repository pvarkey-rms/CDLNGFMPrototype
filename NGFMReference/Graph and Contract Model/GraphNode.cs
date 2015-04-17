using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;

namespace NGFMReference
{
    public abstract class GraphNode : HasseNode
    {
        protected Subject subject;
        public LossTimeSeries SubjectLoss {get; set;}
        public DateTime TimeStamp { get { return DateTime.Today; } }
        public bool HoursClause { get; set; }
        public abstract bool IsPerRisk{get;}

        public bool Executed { get; set; }
        public override string GetName()
        {
            return subject.ToString();
        }

        public HashSet<AtomicRITE> ResidualAtomicRITEs {get; set;}
        public HashSet<AtomicRITE> AllAtomicRITEs { get; set; }
        public bool AtomicRITEsAdded { get; set; }
        
        public Subject Subject
        {
            get { return subject; }
        }

        public GraphNode(Subject _subject, HasseNodeTypes ElementType = HasseNodeTypes.REAL, string debugInfo = "") : base(ElementType, debugInfo)
        {
            ResidualAtomicRITEs = null;
            AtomicRITEsAdded = false;
            subject = _subject;

            keystring = _subject.ToString();
            size = 100;           
        }

        public virtual void Reset()
        {
            Executed = false;
        }

        public virtual void PeriodReset()
        {
            //Needs to be thought through
        }

        //public HashSet<AtomicRITE> GetAtomicRites()
        //public void GetAtomicRites()
        //{
        //    HashSet<AtomicRITE> ARITEs = new HashSet<AtomicRITE>();

        //    foreach (RITE rite in subject.Schedule.RITEs)
        //    {
        //        foreach (string subperil in subject.CauseOfLossSet.GetSubperils())
        //        {
        //            foreach (ExposureType expType in subject.ExposureTypes)
        //            {
        //                var RITChars = rite.RiskCharacteristics.Where(RitChar => RitChar.ExpType == expType);
        //                foreach (RITCharacteristic RitChar in RITChars)
        //                {
        //                    ARITEs.Add(new CoverageAtomicRITE(subperil, expType, rite, RitChar.ID));
        //                }
        //            }
        //        }
        //    }

        //    AllAtomicRITEs = ARITEs;

        //    //return ARITEs;

        //}

        // Hasse Node overrrides /////////////////

        public override string[] GetDifferenceString(HasseNode LargerNode)
        {
            return new string[] { "Does it matter?" };

            //GraphNode LargerGraphNode = LargerNode as GraphNode;
            
            //if(LargerGraphNode != null)
            //{
            //    Subject largeSubject = LargerGraphNode.subject;
            //    Subject smallSubject = this.subject;

            //    if (smallSubject.IsDerived == true && largeSubject.IsDerived == true)
            //    {
                     

                //    return string.Join(",", ChildrenCoverNodeList.ToArray());
                //}
                //else if (smallSubject.IsDerived == true && largeSubject.IsDerived == false)
                //{
                //}
                //else if (smallSubject.IsDerived == false && largeSubject.IsDerived == true)
                //{
                //}
                //else if (smallSubject.IsDerived == false && largeSubject.IsDerived == false)
                //{
                //}
                //        if (largerSubject.IsDerived == true)
                //        {
                //            var coverDiff = largerSubject.ChildrenCoverNodeList;
                //            coverDiff.Remove(this.
                //        }
                //        else
                //        {            if (!largerSubject.IsDerived)
                //    {
                //        string Schedule = this.Schedule.GetDifferenceString(largerSubject.Schedule);
                //        var COLDiff = largerSubject.CauseOfLossSet.Except(this.CauseOfLossSet);
                //        var ExpTypesDidd = largerSubject.ExposureTypes.Except(this.ExposureTypes);

                //        return Schedule + ";" + string.Join(",", COLDiff.ToArray())
                //                              + string.Join(",", ExpTypesDidd.ToArray());
                //    }    
                //    return this.subject.GetDiffercnceString(LargerNode
                        
        }

        public override bool IsLargerThan(HasseNode smallHasseElement)
        {
            GraphNode smallNode = smallHasseElement as GraphNode;

            if (smallNode == null && smallHasseElement.KeyString == "")
                return true;

            if (this is CoverNode & smallNode is CoverNode)
            {
                if (this.subject.IsDerived)
                {
                    if (this.subject.ChildrenCoverNodeList.Contains((smallNode as CoverNode).CoverName))
                        return true;
                    else
                        return false;
                }
                else
                    return false;
            }
            else if (this is CoverNode & smallNode is TermNode)
            {
                return this.subject.IsLargerThan(smallNode.Subject as PrimarySubject)
                        || this.subject.Equals(smallNode.Subject as PrimarySubject);
            }
            else if (this is TermNode & smallNode is CoverNode)
                return false;
            else if (this is TermNode & smallNode is TermNode)
                return this.subject.IsLargerThan(smallNode.Subject as PrimarySubject);
            else
                throw new ArgumentOutOfRangeException("Cannot Compare objects of type " + smallHasseElement.GetType().ToString());
        }

        public override bool GetMaxCommonFragments(HasseNode Node1, HasseNode Node2, bool dbg,
            HasseFragmentInsertionQueue NewFragmentList, int MinimumOverlap)
        {
            return true;
        }

    }
}
