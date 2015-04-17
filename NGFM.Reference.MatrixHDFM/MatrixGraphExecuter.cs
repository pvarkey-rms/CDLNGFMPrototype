using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace NGFM.Reference.MatrixHDFM
{
    public class MatrixGraphExecuter
    {               
        #region
        private static Stopwatch iniGraphState = new Stopwatch();
        private static Stopwatch aggregation = new Stopwatch();
        private static Stopwatch aggregation1 = new Stopwatch();
        private static Stopwatch interaction = new Stopwatch();
        private static Stopwatch assignGUtoARite = new Stopwatch();
        private static Stopwatch allocationtimer = new Stopwatch();
        private static Stopwatch sunnyTesttimer = new Stopwatch();
        private static Stopwatch coverAllocationtimer = new Stopwatch();

        public Stopwatch IniGraphState
        {
            get
            {
                return iniGraphState;
            }
        }

        public Stopwatch Aggregation
        {
            get
            {
                return aggregation;
            }
        }

        public Stopwatch Interaction
        {
            get
            {
                return interaction;
            }

        }

        public Stopwatch AssignGUtoARite
        {
            get
            {
                return assignGUtoARite;
            }
        }

        public Stopwatch Aggregation1
        {
            get
            {
                return aggregation1;
            }
        }

        public Stopwatch Allocationtimer
        {
            get
            {
                return allocationtimer;
            }
        }

        public Stopwatch SunnyTesttimer
        {
            get
            {
                return sunnyTesttimer;
            }
        }

        public Stopwatch CoverAllocationtimer
        {
            get
            {
                return coverAllocationtimer;
            }
        }

        #endregion

        private IExecutableMatrixGraph Graph;

        public MatrixGraphExecuter(IExecutableMatrixGraph _graph)
        {
            Graph = _graph;
        }

        //public float Execute_For(int eventID, IVectorEvent Event)
        //{
        //    ILossState lossState = new LossState(Graph);

        //    GULossInputEngine inputEngine = new GULossInputEngine(lossState, Graph, Event);
        //    inputEngine.Run();

        //    //lowest level, no children
        //    int i = Graph.NumOfLevels - 1;
        //    ILevelFinancialInfo thisLevelInfo = Graph.GetTermLevelInfo(i);
        //    int lowestlevelsize = Graph.GetAggLevelInfo(i).LevelSize;
        //    float[] codeDed = thisLevelInfo.GetCodedMinDeds().ToArray();
        //    float[] codeLim = thisLevelInfo.GetCodedLimits().ToArray();

        //    //allLevelState[i] = new LevelState(thisLevelInfo.LevelSize);

        //    //int currSeed = 31 + eventID;
        //    //var RandGen = new Random(currSeed);

        //    if (!thisLevelInfo.HasMaxDed && !thisLevelInfo.HasPercentDed)
        //    {
        //        for (int j = 0; j < lowestlevelsize; j++)
        //        {
        //            allLevelState[i].SubjectLoss[j] = guloss;
        //            allLevelState[i].Excess[j] = Math.Max(0, guloss - codeLim[j]);
        //            allLevelState[i].Deductible[j] = Math.Min(guloss, codeDed[j]);
        //            allLevelState[i].Recoverable[j] = allLevelState[i].SubjectLoss[j] - allLevelState[i].Excess[j] - allLevelState[i].Deductible[j];
        //        }
        //    }

        //    //upper levels
        //    for (i = Graph.NumOfLevels - 2; i >= 0; i--)
        //    {
        //        thisLevelInfo = Graph.GetTermLevelInfo(i);
        //        ILevelInfo childLevelInfo = Graph.GetTermLevelInfo(i + 1);

        //        float[] dedFromBelow;
        //        float[] excessFromBelow;
        //        float[] subjectLossFromBelow;
        //        float[] recovFromBelow;

        //        bool SimplyCopy = false;
        //        if (SimplyCopy)
        //        {
        //            dedFromBelow = allLevelState[i + 1].Deductible;
        //            excessFromBelow = allLevelState[i + 1].Excess;
        //            subjectLossFromBelow = allLevelState[i + 1].SubjectLoss;
        //            recovFromBelow = allLevelState[i + 1].Recoverable;
        //        }
        //        else
        //        {
        //            dedFromBelow = Utilities.SumArrayByPartitionUsingFor(allLevelState[i + 1].Deductible, childLevelInfo.GetAggregationPartitions());
        //            excessFromBelow = Utilities.SumArrayByPartitionUsingFor(allLevelState[i + 1].Excess, childLevelInfo.GetAggregationPartitions());
        //            subjectLossFromBelow = Utilities.SumArrayByPartitionUsingFor(allLevelState[i + 1].SubjectLoss, childLevelInfo.GetAggregationPartitions());
        //            recovFromBelow = Utilities.SumArrayByPartitionUsingFor(allLevelState[i + 1].Recoverable, childLevelInfo.GetAggregationPartitions());
        //        }
        //        codeDed = thisLevelInfo.GetCodedMinDeds().ToArray();
        //        codeLim = thisLevelInfo.GetCodedLimits().ToArray();

        //        allLevelState[i] = new LevelState(thisLevelInfo.LevelSize);

        //        if (!thisLevelInfo.HasMaxDed && !thisLevelInfo.HasPercentDed)
        //        {
        //            for (int j = 0; j < thisLevelInfo.LevelSize; j++)
        //            {
        //                allLevelState[i].SubjectLoss[j] = subjectLossFromBelow[j];
        //                allLevelState[i].Excess[j] = Math.Max(excessFromBelow[j], Math.Max(0, subjectLossFromBelow[j] - codeLim[j]));
        //                allLevelState[i].Deductible[j] = Math.Max(dedFromBelow[j], Math.Min(subjectLossFromBelow[j], codeDed[j]));
        //                allLevelState[i].Recoverable[j] = allLevelState[i].SubjectLoss[j] - allLevelState[i].Excess[j] - allLevelState[i].Deductible[j];
        //                float deltaD = allLevelState[i].Deductible[j] - dedFromBelow[j];
        //                if (deltaD >= 0)
        //                {
        //                    allLevelState[i].AllocateRatioR[j] = 1 - deltaD / recovFromBelow[j];
        //                    allLevelState[i].AllocateRatioD[j] = 1;
        //                }
        //                else
        //                {
        //                    allLevelState[i].AllocateRatioR[j] = 1;
        //                    allLevelState[i].AllocateRatioD[j] = 1 - deltaD / dedFromBelow[j];
        //                }
        //            }
        //        }
        //    }
        //    Graph.AllLevelState = allLevelState;
        //    return allLevelState[0].Recoverable[0];
        //}


        //test:delete later

        //public float Run(IVectorEvent Event)
        public MatrixResultOutput Run(IVectorEvent Event)
        {
            MatrixResultOutput output = new MatrixResultOutput();

            //this is to store (utlitmately output) the total GU loss for all top Term Level SubjectLoss + LowestCoverLevel's ResidualRites SubjectLoss
            //the loss is accumulated throught each term level and the lowest cover level
            float TotalGULoss = 0;

            IniGraphState.Start();
            IGraphState graphState = new GraphState(Graph);
            IniGraphState.Stop();

            //Get GU 
            AssignGUtoARite.Start();
            GULossInputEngine inputEngine = new GULossInputEngine(graphState, Graph, Event);
            inputEngine.Run();
            AssignGUtoARite.Stop();            
            
            //Get Factors
            FactorInputEngine factorEngine = new FactorInputEngine(graphState, Graph, Event);
            factorEngine.Run();

            float[] FactorVector = Event.Factors;  //this is for TotalCoverState, only used to aggregate Cover Levels

            //If there is no term, only covers, we get lowest atomic rites, then return;
            //float topLevelPayout =0;
            if (Graph.NumOfLevels > 1)
            {
                //Check Contract level Ded and Sublimit Type (Absorbabel or Net of Deductible), and choose the correct Term Engine
                //which will have diff method to apply interaction
                bool NetOfDeductible = Graph.ContractInfo.SublimitIsNetOfDeductible;
                bool DedIsAbsorbable = Graph.ContractInfo.DedIsAbsorbable;
                ITermEngine TermEngine;

                if (NetOfDeductible && DedIsAbsorbable)
                {
                    TermEngine = new TermEngine1();
                }
                else if (!NetOfDeductible && !DedIsAbsorbable)
                {
                    TermEngine = new TermEngine4();
                }
                else if (NetOfDeductible && !DedIsAbsorbable)
                {
                    TermEngine = new TermEngine3();
                }
                else
                {
                    TermEngine = new TermEngine2();
                }


                #region Sunny Test max possible speed//// delete/comment out now! Or ask Sunny !!!!!!!

                //double[] covA_Alloc = ConverterForTestRun(graphState.GetARITELevelState(2).GetSubjectLoss());
                //double[] covC_Alloc = ConverterForTestRun(graphState.GetARITELevelState(3).GetSubjectLoss());

                //SunnyTesttimer.Start();
                ////double payout = TestRunDouble(covA_Alloc, covC_Alloc);
                //double payout = TestRun(graphState);
                //SunnyTesttimer.Stop();

                #endregion


                //Loop through all levels in Graph

                IAggregator Aggregator = new Aggregator1();
                int lowestlevel = Graph.LowestLevel;

                for (int i = lowestlevel; i > 0; i--)
                {
                    Aggregation.Start();

                    ILevelState parentTermLevelState = graphState.GetTermLevelState(i - 1);
                    ILevelAtomicRITEInfo childaRITEInfo = Graph.GetAtomicRITEInfo(i);
                    ISimpleLevelState childaRITELevelState = graphState.GetARITELevelState(i);

                    //Aggregate level to parents
                    if (i == lowestlevel)
                    {
                        Aggregator.AggregateLevel(childaRITELevelState, parentTermLevelState, childaRITEInfo);
                        //TotalGULoss += childaRITELevelState.GetSubjectLoss().Zip(childaRITELevelState.GetFactors(), (x1, x2) => x1*x2).Sum();
                    }
                    else
                    {
                        ILevelNodeAggInfo childNodeAggInfo = Graph.GetNodeAggInfo(i);
                        ILevelState childTermLevelState = graphState.GetTermLevelState(i);
                        Aggregator.AggregateLevel(childTermLevelState, childaRITELevelState, parentTermLevelState, childNodeAggInfo, childaRITEInfo, aggregation1);
                        //TotalGULoss += childaRITELevelState.GetSubjectLoss().Zip(childaRITELevelState.GetFactors(), (x1, x2) => x1 * x2).Sum();                        
                    }
                    Aggregation.Stop();

                    //Do GULoss
                    if (i == 1)
                    {
                        TotalGULoss += parentTermLevelState.GetSubjectLoss().Zip(parentTermLevelState.GetFactors(), (x1, x2) => x1 * x2).Sum();
                    }

                    //Apply finacial terms to level
                    Interaction.Start();
                    ILevelFinancialInfo parentlevelFinInfo = Graph.GetTermLevelInfo(i - 1);
                    ILevelNodeAggInfo parentlevelaggInfo = Graph.GetNodeAggInfo(i - 1);
                    bool HasMaxDed = Graph.GetTermLevelInfo(i - 1).HasMaxDed;
                    bool HasFranchise = Graph.GetTermLevelInfo(i - 1).HasFranchiseDed;

                    if (i == lowestlevel)
                    {
                        TermEngine.InteractionOnLowestTermLevel(parentTermLevelState, parentlevelFinInfo, parentlevelaggInfo.NumOfNodes);
                    }
                    else
                    {
                        TermEngine.ApplyInteraction(parentTermLevelState, parentlevelFinInfo, parentlevelaggInfo.NumOfNodes);
                    }

                    Interaction.Stop();
                }


                //float topLevelPayout = graphState.GetTermLevelState(0).GetRecoverable()[0];
                //topLevelPayout = graphState.GetTermLevelState(0).GetRecoverable().Sum();

                //Allocate & loop through each level to get AriteR
                MatrixGraphAllocation Allocator = new MatrixGraphAllocation(Graph, graphState);
                allocationtimer.Start();
                Allocator.Run();
                allocationtimer.Stop();

                //float AllocatedFinalPayout = 0;

                //for (int i = 0; i <= lowestlevel; i++)
                //{
                //    float[] AriteR = graphState.GetARITELevelState(i).GetRecoverable();
                //    AllocatedFinalPayout += AriteR.Sum();
                //}
                //topLevelPayout = AllocatedFinalPayout;
            }
            //else
            //{
            //    ISimpleLevelState childaRITELevelState = graphState.GetARITELevelState(0);
            //    topLevelPayout = childaRITELevelState.GetSubjectLoss().Sum();

            //}

            //Execute covers after terms:
            int lowestCoverLevel = Graph.LowestCoverLevel;
            int NumOfCoverLevels = Graph.NumOfCoverLevels;
            ICoverLevelResiduleInfo ResiduleInfo = Graph.GetCoverResiduleInfo();
            ICoverLevelTermAriteInfo AriteInfo = Graph.GetCoverAriteInfo();
            ICoverState ResiduleState = graphState.GetLowestCoverLevelResiduleState();
            ICoverState AriteState = graphState.GetLowestCoverLevelAriteState();
            ICoverState TotalCoverState = graphState.GetTotalCoverState();
            ICoverAggregator CoverAggregator = new CoverAggregator();            

            //int StartPosition=0;
            //int EndPosition = Graph.GetCoverNodeAggInfo(Graph.LowestCoverLevel).NumOfNodes;
            int CalculationStartPosition = 0;
            int ParentStartPosition = Graph.GetCoverNodeAggInfo(Graph.LowestCoverLevel+1).NumOfNodes;
            int TopCoverLevel = lowestCoverLevel + NumOfCoverLevels;
            float TotalPayout = 0;
          
            //Get Subject Loss for the lowest cover level (GU & Recoverable)
            CoverInputEngine CoverinputEngine = new CoverInputEngine(graphState, Graph, Event);
            CoverinputEngine.Run();


            for (int i = lowestCoverLevel; i < TopCoverLevel; i++)
            {                
                //Aggregation:
                if (i == lowestCoverLevel)
                {
                    CoverAggregator.AggregateLowestLevel(ResiduleInfo, AriteInfo, ResiduleState, AriteState, TotalCoverState, graphState.GetLowestCoverLevelAriteAllocationRatio(), graphState.GetLowestCoverLevelResidualAllocationRatio());
                    TotalGULoss += ResiduleState.GetSubjectLoss().Zip(ResiduleState.GetFactors(), (x1, x2) => x1 * x2).Sum();                    
                }
                else
                {
                    ICoverAllocationState currLevelAllocationRatio = graphState.GetCoverLevelAllocationRatioState(i);
                    ICoverLevelNodeAggInfo ChildrenLevelAggInfo = Graph.GetCoverNodeAggInfo(i);
                    IDerivedCoverLevelNodeAggInfo DerivedInfo = Graph.GetDerivedCoverNodeAggInfo(i+1);
                    CoverAggregator.AggregateLevel(ParentStartPosition, TotalCoverState, ChildrenLevelAggInfo, DerivedInfo, currLevelAllocationRatio, FactorVector);

                    ParentStartPosition += Graph.GetCoverNodeAggInfo(i + 1).NumOfNodes;
                }

                //Apply Financial Terms to parent level
                ICoverLevelFinancialInfo ParentLevelFinInfo = Graph.GetCoverLevelInfo(i+1);
                ICoverEngine CoverEngine = new CoverEngine();

                int[] LeafTopCoverList = Graph.GetCoverNodeAggInfo(lowestCoverLevel + 1).GetLeafTopCoverList();
                if(i != TopCoverLevel -1)
                    CoverEngine.ApplyCoverLayer(ParentLevelFinInfo, CalculationStartPosition, TotalCoverState);
                else
                    TotalPayout = CoverEngine.ApplyTopCoverLayer(ParentLevelFinInfo, CalculationStartPosition, TotalCoverState, LeafTopCoverList, FactorVector);
                
                //Reset Position for next loop
                CalculationStartPosition = ParentStartPosition;                
            }

            //Allocate cover graph payout                       
            coverAllocationtimer.Start();
            MatrixCoverGraphAllocation CoverAllocator = new MatrixCoverGraphAllocation(Graph, graphState);
            CoverAllocator.Run();
            coverAllocationtimer.Stop();

            output.TotalGULoss = TotalGULoss;
            output.TotalPayOut = TotalPayout;

            //combine allocated Residual loss&indicies and Arite loss&indicies for output
            output.RiteGULossIndicies = (Graph.GetCoverAriteInfo().GetAriteGULossIndicies()).Concat(Graph.GetCoverResiduleInfo().GetGULossIndicies()).ToArray();
            output.AllocatedRitePayout = (graphState.GetLowestCoverLevelAriteState().GetAllocatedPayout()).Concat(graphState.GetLowestCoverLevelResiduleState().GetAllocatedPayout()).ToArray();

            return output;
            //return TotalPayout;

            //return topLevelPayout;

            //Need to get Final Payout for cover....
            #region Delete Later

            //allLevelState[i] = new LevelState(thisLevelInfo.LevelSize);
            //int currSeed = 31 + eventID;
            //var RandGen = new Random(currSeed);
                       
            //float[] subjectloss = new float[thisLevelSize];
            //Array.Copy(Graph.AllLevelState[lowestlevel].SubjectLoss, subjectloss, thisLevelSize);
            //float[] excess = new float[thisLevelSize];
            //float[] ded = new float[thisLevelSize];
            //float[] recov = new float[thisLevelSize];          

            //if (!thisLevelInfo.HasMaxDed && !thisLevelInfo.HasPercentDed)
            //{
            //    for (int j = 0; j < thisLevelSize; j++)
            //    {
            //        //float guloss = (float)(RandGen.NextDouble());
            //        //guloss = guloss * 1000000;
            //        //float guloss = Event.LossVector[thisLevelInfo.GetAtomicRITEIndicies().ToArray()[j]];

            //        float guloss = subjectloss[j];                    
            //        excess[j] = Math.Max(0, guloss - codeLim[j]);
            //        ded[j] = Math.Min(guloss, codeDed[j]);
            //        recov[j] = subjectloss[j] - excess[j] - ded[j];
            //        //allLevelState[i].SubjectLoss[j] = guloss;
            //        //allLevelState[i].Excess[j] = Math.Max(0, guloss - codeLim[j]);
            //        //allLevelState[i].Deductible[j] = Math.Min(guloss, codeDed[j]);                
            //        //allLevelState[i].Recoverable[j] = allLevelState[i].SubjectLoss[j] - allLevelState[i].Excess[j] - allLevelState[i].Deductible[j];
            //    }
            //    Array.Copy(subjectloss, allLevelState[lowestlevel].SubjectLoss, thisLevelSize);
            //    Array.Copy(excess, allLevelState[lowestlevel].Excess, thisLevelSize);
            //    Array.Copy(ded, allLevelState[lowestlevel].Deductible, thisLevelSize);
            //    Array.Copy(recov, allLevelState[lowestlevel].Recoverable, thisLevelSize);
            //}


            //    thisLevelInfo = Graph.GetTermLevelInfo(i);
            //    ILevelInfo childLevelInfo = Graph.GetTermLevelInfo(i + 1);
            //    thisLevelSize = thisLevelInfo.LevelSize;

            //    float[] dedFromBelow;
            //    float[] excessFromBelow;
            //    float[] subjectLossFromBelow;
            //    float[] recovFromBelow;
            //    float[] rRatio = new float[thisLevelSize];
            //    float[] dRatio = new float[thisLevelSize];

            //    subjectloss = new float[thisLevelSize];
            //    excess = new float[thisLevelSize];
            //    ded = new float[thisLevelSize];
            //    recov = new float[thisLevelSize];

            //    bool SimplyCopy = true;
            //    if (SimplyCopy)
            //    {
            //        dedFromBelow = allLevelState[i + 1].Deductible;
            //        excessFromBelow = allLevelState[i + 1].Excess;
            //        subjectLossFromBelow = allLevelState[i + 1].SubjectLoss;
            //        recovFromBelow = allLevelState[i + 1].Recoverable;
            //    }
            //    else
            //    {
            //        dedFromBelow = SumArrayByPartition(allLevelState[i + 1].Deductible, childLevelInfo.GetAggregationPartitions());
            //        excessFromBelow = SumArrayByPartition(allLevelState[i + 1].Excess, childLevelInfo.GetAggregationPartitions());
            //        subjectLossFromBelow = SumArrayByPartition(allLevelState[i + 1].SubjectLoss, childLevelInfo.GetAggregationPartitions());
            //        recovFromBelow = SumArrayByPartition(allLevelState[i + 1].Recoverable, childLevelInfo.GetAggregationPartitions());
            //    }






            //    codeDed = thisLevelInfo.GetCodedMinDeds().ToArray();
            //    codeLim = thisLevelInfo.GetCodedLimits().ToArray();

            //    allLevelState[i] = new LevelState(thisLevelInfo.LevelSize);
                                              
            //    if (!thisLevelInfo.HasMaxDed && !thisLevelInfo.HasPercentDed)
            //    {
            //        for (int j = 0; j < thisLevelSize; j++)
            //        {                        
            //            subjectloss[j] = subjectLossFromBelow[j];
            //            excess[j] = Math.Max(excessFromBelow[j], Math.Max(0, subjectLossFromBelow[j] - codeLim[j]));
            //            ded[j] = Math.Max(dedFromBelow[j], Math.Min(subjectLossFromBelow[j], codeDed[j]));
            //            recov[j] = subjectloss[j] - excess[j] - ded[j];

            //            float deltaD = allLevelState[i].Deductible[j] - dedFromBelow[j];
            //            if (deltaD >= 0)
            //            {
            //                rRatio[j] = 1 - deltaD / recovFromBelow[j];
            //                dRatio[j] = 1;
            //            }
            //            else
            //            {
            //                rRatio[j] = 1;
            //                dRatio[j] = 1 - deltaD / dedFromBelow[j];
            //            }
            //        }
            //        Array.Copy(subjectloss, allLevelState[i].SubjectLoss, thisLevelSize);
            //        Array.Copy(excess, allLevelState[i].Excess, thisLevelSize);
            //        Array.Copy(ded, allLevelState[i].Deductible, thisLevelSize);
            //        Array.Copy(recov, allLevelState[i].Recoverable, thisLevelSize);
            //        Array.Copy(rRatio, allLevelState[i].AllocateRatioR, thisLevelSize);
            //        Array.Copy(dRatio, allLevelState[i].AllocateRatioD, thisLevelSize);
            //    }            
            //}
            //Array.Copy(allLevelState, Graph.AllLevelState, Graph.NumOfLevels);
            #endregion
        }
             
        public double[] ConverterForTestRun(float[] input)
        {
            double[] output = new double[input.Length];

            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (float)input[i];
            }

            return output;
        }

        public float TestRun(IGraphState graphState)
        {
            int numBldgs = 22704;
            float[] covA_GU = graphState.GetARITELevelState(2).GetSubjectLoss();
            float[] covC_GU = graphState.GetARITELevelState(3).GetSubjectLoss();
            float[] covA_Alloc = new float[numBldgs];
            float[] covC_Alloc = new float[numBldgs];

            float covCSubLim = 444846.7230f;
            float locDed = 296564.4820f;

            float payout = 0;

            for (int i = 0; i < numBldgs; i++)
            {
                float covCGU = covC_GU[i];
                float covAGU = covA_GU[i];
                float LocGroupUp = covCGU + covAGU;

                float X = Math.Max(covCGU - covCSubLim, 0);
                float D = Math.Min(LocGroupUp, locDed);

                //float X = -(-LocGroupUp - locDed - Math.Abs(-LocGroupUp + locDed)) / 2;
                //float D = (LocGroupUp + locDed  - Math.Abs(LocGroupUp - locDed))/2;

                float totalR = LocGroupUp - X;
                payout += totalR - D;

                covA_Alloc[i] = covAGU / totalR * payout;
                covC_Alloc[i] = (covCGU - X) / totalR * payout;
            }

            return payout;

            //return 0;
        }

        public double TestRunDouble(double[] covA_GU, double[] covC_GU)
        {
            int numBldgs = 22704;
            double[] covA_Alloc = new double[numBldgs];
            double[] covC_Alloc = new double[numBldgs];

            double covCSubLim = 444846.7230f;
            double locDed = 296564.4820f;

            double payout = 0;

            for (int i = 0; i < numBldgs; i++)
            {
                double covCGU = covC_GU[i];
                double covAGU = covA_GU[i];
                double LocGroupUp = covCGU + covAGU;

                double X = Math.Max(covCGU - covCSubLim, 0);
                double D = Math.Min(LocGroupUp, locDed);

                //float X = -(-LocGroupUp - locDed - Math.Abs(-LocGroupUp + locDed)) / 2;
                //float D = (LocGroupUp + locDed  - Math.Abs(LocGroupUp - locDed))/2;

                double totalR = LocGroupUp - X;
                payout += totalR - D;

                covA_Alloc[i] = covAGU / totalR * payout;
                covC_Alloc[i] = (covCGU - X) / totalR * payout;
            }

            return payout;

            //return 0;
        }
             
    }

}
