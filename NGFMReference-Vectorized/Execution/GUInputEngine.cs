using System; 
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class GUInputEngine
    {
        private Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double,uint, List<float>>>>> GULosses;
        private GraphOfNodes graph;
        private int ContractYear;

        public GUInputEngine(Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> _guLosses, GraphOfNodes _graph)
        {
            GULosses = _guLosses;
            graph = _graph;
            //Get Contract Year
            ContractYear = graph.Declarations.Inception.Year;
        }

        public double[] GetGUForSubject(PrimarySubject sub)
        {
            int numBldgs = sub.Schedule.ActNumOfBldgs;
            double[] guloss = new double[numBldgs];

            foreach (string subperil in sub.CauseOfLossSet.GetSubperils())
            {
                foreach (RITCharacteristic RITChar in sub.Schedule.RITChars)
                {
                    if (sub.ExposureTypes.Contains(RITChar.ExpType))
                    {
                        double[] RITELoss;
                        uint timestamp;
                        string error;
                        if (GetRITCharacteristicLoss(RITChar, subperil, out RITELoss, out timestamp, out error, numBldgs))
                            guloss = guloss.Zip(RITELoss, (x, y) => x + y).ToArray();
                        else
                            throw new GUInputEngineException(error);
                    }
                }
            }

            return guloss;
        }

        //public double[] GetGUForNode(GraphNode node)
        //{
        //    PrimarySubject sub = (PrimarySubject)node.Subject;
        //    int numBldgs = sub.Schedule.ActNumOfBldgs;
        //    double[] subjectloss = new double[numBldgs];
        //    List<RITCharacteristic> LeftoverRiteChars = sub.Schedule.RITChars.ToList();

        //    foreach (GraphNode childnode in graph.GetChildrenForNode(node))
        //    {
        //        double[] nodeLosses = GetGUForNode(childnode);
                
        //        if (childnode is TermNode)
        //        {
        //            TermNode termNode = childnode as TermNode;  
        //            if (numBldgs == 1)
        //            {
        //                subjectloss[0] += nodeLosses.Sum();
        //            }
        //            else if (numBldgs == nodeLosses.Count())
        //            {
        //                subjectloss.Zip(nodeLosses, (a, b) => a + b);
        //            }
        //        }
        //        else
        //        {
        //            CoverNode covNode = childnode as CoverNode;
        //            subjectloss[0] += covNode.Payout;
        //        }

        //        LeftoverRiteChars = LeftoverRiteChars.Except(childnode.Subject.Schedule.RITChars).ToList();
        //    }

        //    foreach (RITCharacteristic RITChar in LeftoverRiteChars)
        //    {
        //        foreach (string subperil in sub.CauseOfLossSet.GetSubperils())
        //        {
        //            if (sub.ExposureTypes.Contains(RITChar.ExpType))
        //            {
        //                double[] RITELoss;
        //                uint timestamp;
        //                string error;
        //                if (GetRITCharacteristicLoss(RITChar, subperil, out RITELoss, out timestamp, out error, numBldgs))
        //                    subjectloss = subjectloss.Zip(RITELoss, (x, y) => x + y).ToArray();
        //                else
        //                    throw new GUInputEngineException(error);
        //            }
        //        }
        //    }

        //    return subjectloss;
        //}

        private bool GetRITCharacteristicLoss(RITCharacteristic RITChar, string subperil, out double[] loss, out uint timestamp, out string error, int numBldgs)
        {
            bool success = true;
            error = "";
            long CharId = RITChar.ID;
            double BuildingTIV = RITChar.TIV / RITChar.ParentRITE.NumOfTrueBldgs;
            loss = new double[1];
            loss[0] = 0;
            timestamp = 0;

            Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>> AfterCOL;

            if (GULosses.TryGetValue(subperil, out AfterCOL))
            {
                Dictionary<long, Tuple<double, uint, List<float>>> AfterSample;
                if (AfterCOL.TryGetValue(0, out AfterSample))
                {
                    Tuple<double, uint, List<float>> DamageRatio;
                    if (AfterSample.TryGetValue(CharId, out DamageRatio))
                    {
                        timestamp = DamageRatio.Item2;

                        if (DamageRatio.Item3.Count == numBldgs)
                        {                           
                            double[] multiplyArr = RITE.GenerateMultiplierArr(RITChar.ParentRITE.NumOfSampleBldgs);
                            loss = DamageRatio.Item3.Zip(multiplyArr, (d1, d2) => (double)d1 * d2).ToArray();
                            //loss = Array.ConvertAll(DamageRatio.Item2.ToArray(), x => (double)x);
                            success = true;
                        }
                        else if (numBldgs == 1)
                        {
                            double[] multiplyArr = RITE.GenerateMultiplierArr(RITChar.ParentRITE.NumOfSampleBldgs);                       
                            loss[0] = DamageRatio.Item3.Zip(multiplyArr, (d1, d2) => d1 * d2).Sum();
                            //loss[0] = DamageRatio.Item2.ToArray().Sum();
                            success = true;
                        }
                        else
                        {
                            success = false;
                            error = "Error getting GU for RITE ID: " + RITChar.ID + ". GU losses specified not matching for NumBldgs...";
                        }
                    }
                    else
                    {
                        success = false;
                        error = "Error getting GU for RITE ID: " + RITChar.ID + ". Cannot find RITE ID in GU Input Loss dictionary";
                    }
                }
                else
                {
                    success = false;
                    error = "Error getting GU for RITE ID: " + RITChar.ID + ". Cannot find Sample 0 in GU Input Loss dictionary, currenlty hard-coded to take sample id = 0";
                }
            }
        
            return success;
        }

        public void GetGUForCoverageRITE(CoverageAtomicRITE aRITE)
        {
            double[] GULossArray;
            uint timestamp;
            string error;

            RITCharacteristic RIT = aRITE.RITE.RiskCharacteristics.Where(RITChar => RITChar.ID == aRITE.RITCharacterisiticID).FirstOrDefault();
            if (GetRITCharacteristicLoss(RIT, aRITE.SubPeril, out GULossArray, out timestamp, out error, aRITE.RITE.ActNumOfSampleBldgs))
            {
                DateTime ActualTimeStamp = new DateTime(ContractYear, 1, 1);
                ActualTimeStamp = ActualTimeStamp.AddDays((double)timestamp-1);
                aRITE.SetSubjectLoss(new LossTimeSeries(ActualTimeStamp, GULossArray));
            }
            else
                throw new GUInputEngineException(error);
        }


    }

    public class GUInputEngineException : Exception
    {
        public GUInputEngineException(string msg)
            : base(msg)
        {
        }

    }
}
