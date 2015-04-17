using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFMReference;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public abstract class GraphExecuter
    {
        protected Graph graph;

        //public double Execute(GULossAdaptor guLoss)
        //{
        //    GUInputEngine guinputengine = new GUInputEngine(inputloss, graph);
        //    return Execute(guinputengine);
        //} //Execute

        //-------------------------------------------
        //implement infinite numbers of LO's, based on Slava's spec
        public ReferenceResultOutput OptimalWindowsIterator(List<TimeWindow> sortedTimeWindows, GULossAdaptor guLoss)
        {
            int numOfWindows = sortedTimeWindows.Count();
            int hc = graph.Declarations.HoursClauses[0].Duration;
            List<int>[] optimalWindowsArr = new List<int>[numOfWindows];
            float[] totalPayout = new float[numOfWindows];  //cumulative
            float[] totalSubjectLoss = new float[numOfWindows]; //cumulative
            Dictionary<string, AggState>[] aggStateArr = new Dictionary<string, AggState>[numOfWindows];
            Dictionary<string, AggState> preEventGraphAggSate = graph.GetAggStates();

            if (numOfWindows == 0)
            {
                return new ReferenceResultOutput(0, 0);
            }

            //to init when i = 0, that is the first window (first iteration)     
            ReferenceResultOutput firstResult = ExecuteWindow(sortedTimeWindows[0], guLoss);
            totalPayout[0] = (float)firstResult.TotalPayout;
            totalSubjectLoss[0] = (float)firstResult.TotalSubjectLoss;          
            optimalWindowsArr[0] = new List<int>{0};
            if (graph is GraphOfNodes)
                aggStateArr[0] = graph.GetAggStates();

            float preNonOverlapTotalPayout = 0;
            float preNonOverlapTotalSubjectLoss = 0;
            List<int> preNonOverlapOptimalWindows;
            Dictionary<string, AggState> preNonOverlapAggState = null;

            //iterate rest windows till finish
            for (int i = 1; i < numOfWindows; i++)
            {
                //find the latest window j, which is before window i but not overlapping with window i
                int j = latestNonOverlapWindow(i, hc, sortedTimeWindows);
                if (j < 0)
                {
                    preNonOverlapTotalPayout = 0;
                    preNonOverlapTotalSubjectLoss = 0;
                    preNonOverlapOptimalWindows = new List<int>();
                    preNonOverlapAggState = preEventGraphAggSate;
                }
                else
                {
                    preNonOverlapTotalPayout = totalPayout[j];
                    preNonOverlapTotalSubjectLoss = totalSubjectLoss[j];
                    preNonOverlapOptimalWindows = new List<int>();
                    preNonOverlapOptimalWindows.AddRange(optimalWindowsArr[j]);
                    preNonOverlapAggState = aggStateArr[j];
                }

                //exec this window
                graph.HoursClauseWindowReset();
                if (preNonOverlapAggState != null && graph is GraphOfNodes)                
                    graph.SetAggStates(preNonOverlapAggState); //to set to "j"'s agg state
                TimeWindow currWindow = sortedTimeWindows[i];
                ReferenceResultOutput currResult = ExecuteWindow(currWindow, guLoss);                
                float currPayout = (float)currResult.TotalPayout;
                float currSubjectLoss = (float)currResult.TotalSubjectLoss;
                totalSubjectLoss[i] = currSubjectLoss + preNonOverlapTotalSubjectLoss;

                //compare               
                if (totalPayout[i - 1] < currPayout + preNonOverlapTotalPayout ||
                            (totalPayout[i] == 0 && totalSubjectLoss[i] > totalSubjectLoss[i-1]))  //include window i in the optimal
                {
                    totalPayout[i] = currPayout + preNonOverlapTotalPayout;                    
                    preNonOverlapOptimalWindows.Add(i);
                    optimalWindowsArr[i] = preNonOverlapOptimalWindows;
                    aggStateArr[i] = graph.GetAggStates();
                }
                else
                {
                    totalPayout[i] = totalPayout[i - 1];
                    totalSubjectLoss[i] = totalSubjectLoss[i - 1];
                    optimalWindowsArr[i] = optimalWindowsArr[i - 1];
                    aggStateArr[i] = aggStateArr[i - 1];
                    graph.SetAggStates(aggStateArr[i - 1]);
                }                                
            }
            ReferenceResultOutput result = new ReferenceResultOutput((double)totalPayout[numOfWindows-1], (double)totalSubjectLoss[numOfWindows-1]);
            return result;
        }

        public int latestNonOverlapWindow(int i, int hc, List<TimeWindow> sortedTimeWindows)
        {            
            for (int k = i - 1; k >= 0; k--)
            {
                if (sortedTimeWindows[k].end < sortedTimeWindows[i].start)
                    return k;         
            }            
            return -1;  //not found        
        }
        //------- end of iterator -----------
        
        public ReferenceResultOutput Execute(GULossAdaptor guLoss)
        {
            PrepareGraphForExecution(guLoss);
            ReferenceResultOutput Output;
            
            if (ShouldProcessEvent(guLoss) == false)
            {
                Output = new ReferenceResultOutput(0, 0);
                graph.exResults = Output;
                graph.IsExecuted = true;
                return Output;
            }

            QuerryableLossOutput SubjectLoss = GetSubjectLoss();

            HoursClauseWindowGenerator WindowGenerator = new HoursClauseWindowGenerator(graph.Declarations);
            List<TimeWindow> timewindows = WindowGenerator.Generate(SubjectLoss.TotalTimeSeries);

            #region old code
            //if (graph.Declarations.IsHoursClause)
            //    timewindows = guLoss.GenerateWindows(graph.Declarations.HoursClauses[0].Duration);
            //else
            //    timewindows = new List<TimeWindow>() { new TimeWindow() };

            //List<ReferenceResultOutput> lst_resultOutputs = new List<ReferenceResultOutput>();

            //foreach (TimeWindow tw in timewindows)
            //{
            //    graph.HoursClauseWindowReset();
            //    lst_resultOutputs.Add(ExecuteWindow(tw, guLoss));       
            //}

            ////find the timewindow that has the maximum payout
            //ReferenceResultOutput MaxPayout_ResultOutput = lst_resultOutputs.First();
            //foreach (ReferenceResultOutput resultOutput in lst_resultOutputs)
            //{
            //    if (resultOutput.TotalPayout > MaxPayout_ResultOutput.TotalPayout)
            //        MaxPayout_ResultOutput = resultOutput;
            //}

            #endregion

            Output = ProcessWindows(timewindows, guLoss);
            Output.SetSubjectLoss(SubjectLoss);

            graph.exResults = Output;
            graph.IsExecuted = true;
            

            return Output;
          
        } //Execute


        private ReferenceResultOutput ProcessWindows(List<TimeWindow> TimeWindows, GULossAdaptor guLoss)
        {            
            List<Dictionary<string, AggState>> lst_aggState = new List<Dictionary<string, AggState>>();

            if ((graph.Declarations.HoursClauses.Count() == 0) || graph.Declarations.HoursClauses[0].OnlyOnce)
            {
                List<ReferenceResultOutput> lst_resultOutputs = new List<ReferenceResultOutput>();
                 Dictionary<string, AggState> preGraphState = new  Dictionary<string, AggState>();
                 if (graph is GraphOfNodes)
                    preGraphState = graph.GetAggStates();
  
                foreach (TimeWindow tw in TimeWindows)
                {                                                            
                    graph.HoursClauseWindowReset();
                    graph.SetAggStates(preGraphState);       
                                 
                    lst_resultOutputs.Add(ExecuteWindow(tw, guLoss));

                    //then save the agg state for all covers for this window
                    if (graph is GraphOfNodes)                                       
                        lst_aggState.Add(graph.GetAggStates());
                }

                //find the timewindow that has the maximum payout
                ReferenceResultOutput MaxPayout_ResultOutput = new ReferenceResultOutput(0, 0);
                if (lst_resultOutputs.Count() > 0)
                    MaxPayout_ResultOutput = lst_resultOutputs.First();

                Dictionary<string, AggState> MaxPayoutAggState = new Dictionary<string, AggState>();

                if (graph is GraphOfNodes)
                {
                    MaxPayoutAggState = graph.GetAggStates();
                    if (lst_aggState.Count() > 0)                
                        MaxPayoutAggState = lst_aggState.First();
                    else
                        MaxPayoutAggState = graph.GetAggStates();
                }

                int i = 0;
                foreach (ReferenceResultOutput resultOutput in lst_resultOutputs)
                {
                    if (resultOutput.TotalPayout > MaxPayout_ResultOutput.TotalPayout)
                    {
                        MaxPayout_ResultOutput = resultOutput;
                        if (graph is GraphOfNodes)
                            MaxPayoutAggState = lst_aggState[i];
                    }
                    i++;
                }
                //then set the graph aggstate
                if (graph is GraphOfNodes) 
                    graph.SetAggStates(MaxPayoutAggState);

                return MaxPayout_ResultOutput;
            }
            else
            {
                return OptimalWindowsIterator(TimeWindows, guLoss);
            }            
        }

        protected abstract void PrepareGraphForExecution(GULossAdaptor guLoss);

        protected abstract QuerryableLossOutput GetSubjectLoss();

        protected abstract ReferenceResultOutput ExecuteWindow(TimeWindow timewindow, GULossAdaptor guLoss);

        protected abstract bool ShouldProcessEvent(GULossAdaptor guLoss);
    }

    
    public abstract class GraphOfNodesExecuter:GraphExecuter
    {
        public GraphOfNodes NodeGraph
        {
            get
            {
                GraphOfNodes ngraph = graph as GraphOfNodes;
                return ngraph;
            }
        }

        protected override ReferenceResultOutput ExecuteWindow(TimeWindow timewindow, GULossAdaptor guLoss)
        {
            ApplyWindowToGU(timewindow);  //unrestricted, raintest  

            List<CoverNode> TopCovers = NodeGraph.TopNodes.OfType<CoverNode>().ToList();
            
            //Sunny temporary hack... delete immeadiatley
            //GUInputEngine guInputForNodeGraph = new GUInputEngine(guLoss.GetDictTypeLosses(NodeGraph.ContractID), NodeGraph);
            GUInputEngine guInputForNodeGraph = null;
            
            ReferenceResultOutput totalExOutput = new ReferenceResultOutput(0, 0);
            foreach (CoverNode coverNode in TopCovers)
            {
                totalExOutput += ExecuteCover(guInputForNodeGraph, coverNode);
            }

            return totalExOutput;                        
        }

        private ReferenceResultOutput ExecuteCover(GUInputEngine guinputengine, CoverNode topCover)
        {
            if (graph.IsOverlapped == false)
            {
                RecursiveExecution(topCover, guinputengine);
                //Allocate Graph
                GraphAllocation Allocater = new GraphAllocation(NodeGraph);
                Allocater.AllocateGraph();
            }
            else
            {               
                ExecuteOverlappedGraph(guinputengine);
            }

            graph.exResults = new ReferenceResultOutput(topCover.Payout, 0, graph.AtomicRites);
            return graph.exResults;
        }

        private void RecursiveExecution(GraphNode currNode, GUInputEngine guLossesEngine)
        {
            Aggregation aggType = Aggregation.Summed;

            if (currNode is TermNode)
            {
                TermNode currTermNode = currNode as TermNode;

                //Execution for Term Node here...
                if (currTermNode.Executed == true)
                    return;
                
                //execute child node first
                List<GraphNode> childrenNodes = NodeGraph.GetChildrenForNode(currTermNode);
                List<TermNode> childrenTermNodes = new List<TermNode>();

                foreach (GraphNode childNode in childrenNodes)
                //Parallel.ForEach(childrenNodes, childNode =>
                {
                    TermNode childTermNode = childNode as TermNode;
                    if (childTermNode == null)
                        throw new InvalidOperationException("Term node's children must be Term node.");
                    childrenTermNodes.Add(childTermNode);

                    if (childNode.Executed == false)
                        RecursiveExecution(childTermNode, guLossesEngine);
                }
                //);
                //has not executed, get the GU loss first
                double[] inputlosses;

                //inputlosses = graph.GetNodeSubjectLoss(currTermNode).AllLoss().Zip(currTermNode.PrimarySubject.Schedule.MultiplierArr, (d1, d2) => d1 * d2).ToArray();
                inputlosses = NodeGraph.GetNodeSubjectLoss(currTermNode).AllLoss();
                currTermNode.CurrentLossStateCollection.SetSubjectLosses(inputlosses);
                
                if (currTermNode.IsPerRisk == true && currTermNode.PrimarySubject.Schedule.ActNumOfBldgs > 1)
                    aggType = Aggregation.PerBuilding;
                else 
                {
                    //need reset the lossState to act as only one building
                    currTermNode.CurrentLossStateCollection = new LossStateCollection2(currTermNode.CurrentLossStateCollection.GetTotalSum);              
                }                

                //if no childrenNodes, nothing to do with the Interaction: the interaction terms are all zeros.
                if (childrenNodes.Count > 0)
                {
                    //initialize InterObj
                    //InteractionObject[] InterObj = GetInterObj(currTermNode, aggType, childrenTermNodes);
                    //TermFunctionalEngine tFunEng = new TermFunctionalEngine(currTermNode, aggType);
                    //tFunEng.TermFunction(InterObj);

                    LossStateCollection2 InterObj = GetInterObj(currTermNode, aggType, childrenTermNodes);
                    TermFunctionalEngine_4 tFunEng = new TermFunctionalEngine_4(currTermNode, aggType);
                    tFunEng.TermFunction(InterObj);
                 
                    //Interaction
                    //InteractionEngine InterEng = new InteractionEngine(currTermNode, aggType, InterObj);
                    //InterEng.Interaction();
                }
                else
                {
                    TermFunctionalEngine_4 tFunEng = new TermFunctionalEngine_4(currTermNode, aggType);
                    tFunEng.TermFunction(new LossStateCollection2(1)); //no interaction
                }
                            
               //Final Adjustment
                currTermNode.CurrentLossStateCollection.AdjustR();
                //for (int i = 0; i < currTermNode.CurrentLossStateCollection.NumBldgs; i++)
                //{
                //    currTermNode.CurrentLossStateCollection.collection[i].AdjustR();      
                //}

                //currTermNode.CurrentLossStateCollection.CalcTotalSum();

                currTermNode.Executed = true;              
            }
            else  //currNode is Cover Node
            {
                CoverNode currCoverNode = currNode as CoverNode;

                //Execution for Cover Node here...
                if (currCoverNode.Executed == true)
                    return;

                foreach (AtomicRITE aRite in currCoverNode.ResidualAtomicRITEs)
                {
                    currCoverNode.Payout += aRite.GetLossState().GetTotalSum.R;
                }

                foreach (GraphNode childNode in NodeGraph.GetChildrenForNode(currCoverNode))
                {
                   RecursiveExecution(childNode, guLossesEngine);

                   if (childNode is TermNode)
                   {
                       TermNode childTermNode = childNode as TermNode;
                       //LossState currentLossState = new LossState(childTermNode.CurrentLossStateCollection.GetTotalSum);

                       //currCoverNode.Payout += currentLossState.R;
                       currCoverNode.Payout += childTermNode.CurrentLossStateCollection.GetTotalSum.R;
                   }
                   else
                   {
                       CoverNode childCoverNode = childNode as CoverNode;
                       currCoverNode.Payout += childCoverNode.Payout;
                   }
                }
                
                CoverNodeFunctionalEngine coverNodeFuncEng = new CoverNodeFunctionalEngine(currCoverNode);
                coverNodeFuncEng.CoverNodeFunction();
                currCoverNode.Executed = true;

            } //currNode is Cover Node
        }

        protected override void PrepareGraphForExecution(GULossAdaptor guLoss)
        {
            GetGUForAtomicRITEs(guLoss);
        }

        protected override QuerryableLossOutput GetSubjectLoss()
        {
            List<AtomicLoss> AtomicSubjectLosses = new List<AtomicLoss>();
            foreach (AtomicRITE aRITE in NodeGraph.AtomicRites)
            {
                foreach (TimeLoss timeloss in aRITE.OriginalSubjectLoss)
                //if (timeloss.Loss > 0)
                {
                    AtomicLoss subjectLoss = new AtomicLoss();
                    subjectLoss.ExpType = aRITE.ExpType;
                    subjectLoss.Subperil = aRITE.SubPeril;
                    subjectLoss.Loss = timeloss.Loss;
                    subjectLoss.timestamp = timeloss.Time;
                    AtomicSubjectLosses.Add(subjectLoss);

                    //Remove when architecture for allcoation finalized !!!!!!!!!
                    if (aRITE is CoverageAtomicRITE)
                        subjectLoss.ExposureID = (aRITE as CoverageAtomicRITE).RITCharacterisiticID;
                    else
                        subjectLoss.ExposureID = (aRITE as ContractAtomicRITE).contractGraph.Graph.ContractID;
                    //Remove above ////////////////////////////////////////////////////
                }
            }

            return new QuerryableLossOutput(AtomicSubjectLosses);
        }

        protected abstract void GetGUForAtomicRITEs(GULossAdaptor guLoss);

        private LossStateCollection2 GetInterObj(TermNode currNode, Aggregation aggType,  List<TermNode> childrenNodes)
        {

            LossStateCollection2 InterObj = new LossStateCollection2(currNode.CurrentLossStateCollection.NumBldgs);                    
            //for (int i = 0; i < currNode.CurrentLossStateCollection.NumBldgs; i++)
            //{
            //    InterObj[i] = new InteractionObject();
            //}
   
            foreach (TermNode childNode in childrenNodes)
            {                
                //if (currNode.TermIsPerRisk && currNode.CurrentLossStateCollection.NumBldgs > 1)
                if (aggType == Aggregation.PerBuilding)
                {
                    InterObj.SumLossesFrom(childNode.CurrentLossStateCollection);

                }
                else
                {
                    //LossState childLossesState = new LossState(childNode.CurrentLossStateCollection.GetTotalSum);
                    //InterObj[0].UpdateInterObjState(childLossesState);

                    LossStateCollection2 tempcol = new LossStateCollection2(childNode.CurrentLossStateCollection.GetTotalSum);
                    InterObj.SumLossesFrom(tempcol);
                }
            }  //foreach child
            return InterObj;
        }

        private LossStateCollection2 GetInterObjForOverlap(TermNode currNode, List<GraphNode> childrenNodes, Aggregation aggType, HashSet<CoverageAtomicRITE> SubjectARites)
        {

            LossStateCollection2 InterObj = new LossStateCollection2(currNode.CurrentLossStateCollection.NumBldgs);

            //Get Single largest Deductible, based on the children nodes, not AtomicRites

            foreach (CoverageAtomicRITE aRite in SubjectARites)
            {
                if (aggType == Aggregation.PerBuilding)
                {
                    if (currNode.CurrentLossStateCollection.NumBldgs != aRite.RITE.ActNumOfSampleBldgs)
                        throw new InvalidOperationException("AtomicRite NumOfBuilding must be equal to its parent's numOfBuilding");

                    InterObj.SumLossesFrom(aRite.GetLossState());
                }
                else
                {
                    LossStateCollection2 tempcol = new LossStateCollection2(aRite.GetLossState().GetTotalSum);
                    InterObj.SumLossesFrom(tempcol);                    
                }
            }  //foreach child

            //update for SingleLargestDed
            //TODO: Rain, we changed InterObj to be lossState2, but we need field LargestDedFromChildren, which is not
            //part of lossState. since we do not need this feature for now, do it later.
            //foreach (GraphNode childNode in childrenNodes)
            //{
            //    TermNode childTermNode = childNode as TermNode;
            //    if (aggType == Aggregation.PerBuilding)
            //    {
            //        if (currNode.CurrentLossStateCollection.NumBldgs != currNode.CurrentLossStateCollection.NumBldgs)
            //            throw new InvalidOperationException("child NumOfBuilding must be equal to its parent's numOfBuilding");

            //        double[] currentDedStateCol = childTermNode.CurrentLossStateCollection.Deductible;
            //        for (int i = 0; i < currNode.CurrentLossStateCollection.NumBldgs; i++)
            //        {
            //            InterObj[i].LargestDedFromChildren = Math.Max(InterObj[i].LargestDedFromChildren, currentDedStateCol[i]); //UpdateInterObjStateForARITE(aRite.GetLossState().collection[i]);
            //        }
            //    }
            //    else
            //    {
            //        //AllocationState aRiteAllocState = new AllocationState(aRite.CurrentAllocationState);
            //        InterObj[0].LargestDedFromChildren = Math.Max(InterObj[0].LargestDedFromChildren, childTermNode.CurrentLossStateCollection.GetTotalSum.D); //(aRite.GetLossState().GetTotalSum);
            //    }
            //}
            return InterObj;
        }
            
        protected void ApplyWindowToGU(TimeWindow window)
        {
            foreach (AtomicRITE aRITE in graph.AtomicRites)
            {
                aRITE.SetSubjectLoss(aRITE.OriginalSubjectLoss.ApplyWindow(window));
            }
        }

        private void ExecuteOverlappedGraph(GUInputEngine guLossesEngine)
        {
            GraphOfNodes ngraph = graph as GraphOfNodes;
            List<int> levelList = new List<int>();
            foreach (int aLevel in ngraph.LevelNodeDict.Keys)
            {
                levelList.Add(aLevel);
            }


            for (int i = levelList.Max(); i > 0; i--)
            {
                HashSet<CoverageAtomicRITE> wholeList = new HashSet<CoverageAtomicRITE>();
                foreach (GraphNode currNode in ngraph.LevelNodeDict[i])
                {
                    //they should be all TermNode
                    TermNode currTermNode = currNode as TermNode;

                    //find its AtomicRITEs
                    HashSet<AtomicRITE> SubjectARITEs = currTermNode.AllAtomicRITEs; //currTermNode.GetAtomicRites();

                    //HashSet<CoverageAtomicRITE> SubjectCRITEs = currTermNode.AllAtomicRITEs;
                    HashSet<CoverageAtomicRITE> SubjectCRITEs = new HashSet<CoverageAtomicRITE>();

                    foreach (AtomicRITE aRite in SubjectARITEs)
                    {
                        CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
                        if (cRite == null)
                            throw new InvalidOperationException("Overlap AtomicRite has to be all CoverageAtomicRite");

                        SubjectCRITEs.Add(cRite);
                    }
                    List<GraphNode> childrenNodes = new List<GraphNode>();
                    childrenNodes = ngraph.GetChildrenForNode(currTermNode);

                    ExecuteOverlappedTermNode(currTermNode, childrenNodes, guLossesEngine, SubjectCRITEs);
                    currTermNode.Executed = true;
                    AllocateOverlappedTermNode(currTermNode, SubjectCRITEs);  //allocate back to the AtomicRites
                    wholeList.UnionWith(SubjectCRITEs);
                }

                //when this level is done, for each child AtomicRite
                //then copy the Allocated value to Loss value for next iteration
                foreach (CoverageAtomicRITE childNode in wholeList)
                {
                    //first compare AllocationState, Per-Building or Summed Wins
                    //if (childNode.GetAllocState().GetTotalSum.R > childNode.GetAllocationStateSummed().R ||
                    //    (childNode.GetAllocState().GetTotalSum.R == childNode.GetAllocationStateSummed().R &&
                    //    childNode.GetAllocState().GetTotalSum.D > childNode.GetAllocationStateSummed().D))
                    //{
                    for (int j = 0; j < childNode.RITE.ActNumOfSampleBldgs; j++)
                    {
                        //childNode.GetLossState().collection[j].S = childNode.GetAllocationState().collection[j].S;
                        childNode.GetLossState().Recoverable[j] = childNode.GetAllocState().Recoverable[j];
                        childNode.GetLossState().Deductible[j] = childNode.GetAllocState().Deductible[j];
                        childNode.GetLossState().Excess[j] = childNode.GetLossState().SubjectLoss[j] - childNode.GetLossState().Recoverable[j] - childNode.GetLossState().Deductible[j];

                        childNode.GetAllocState().Recoverable[j] = 0;    //refresh to be compared 
                        childNode.GetLossState().CalcTotalSum();                          
                    }             
                }
            }
            //then lelve i = 0, all are bottom CoverNode's which are connected to TermNode
            foreach (CoverNode currCoverNode in ngraph.LevelNodeDict[0])
            {   //simple sum up all children's R
                HashSet<AtomicRITE> SubjectARITEs = currCoverNode.AllAtomicRITEs; //currCoverNode.AllAtomicRITEs;
                HashSet<CoverageAtomicRITE> SubjectCRITEs = new HashSet<CoverageAtomicRITE>();
                foreach (AtomicRITE aRite in SubjectARITEs)
                {
                    CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
                    SubjectCRITEs.Add(cRite);
                }
                //HashSet<CoverageAtomicRITE> SubjectCRITEs = currCoverNode.Subject.GetAtomicRites();
                currCoverNode.Payout = SubjectCRITEs.Sum(item => item.GetLossState().GetTotalSum.R);
                CoverNodeFunctionalEngine coverNodeFuncEng = new CoverNodeFunctionalEngine(currCoverNode);
                coverNodeFuncEng.CoverNodeFunction();
                currCoverNode.Executed = true;
                AllocateOverlappedCoverNode(currCoverNode, SubjectCRITEs);
            }
        }

        private void ExecuteOverlappedTermNode(TermNode currTermNode, List<GraphNode> childrenNodes, GUInputEngine guLossesEngine, HashSet<CoverageAtomicRITE> SubjectCRITEs)
        {
            //find its AtomicRITEs
            // HashSet<AtomicRITE> SubjectARITEs = currTermNode.Subject.GetAtomicRites();

            //get the node's Subject loss
            double[] inputlosses;
            //inputlosses = guLossesEngine.GetGUForSubject(currTermNode.PrimarySubject).Zip(currTermNode.PrimarySubject.Schedule.MultiplierArr, (d1, d2) => d1 * d2).ToArray();
            //inputlosses = guLossesEngine.GetGUForSubject(currTermNode.PrimarySubject); 
            inputlosses = NodeGraph.GetNodeSubjectLoss(currTermNode).AllLoss();
            if (currTermNode.IsPerRisk)
                currTermNode.CurrentLossStateCollection.SetSubjectLosses(inputlosses);
            else
                currTermNode.CurrentLossStateCollection.SetSubjectLosses(new double[] { inputlosses.Sum() });

            foreach (CoverageAtomicRITE cRite in SubjectCRITEs)
            {
                //double[] GULoss = guLossesEngine.GetGUForCoverageRITE(CoverageAtomicRITE cRITE);
                //if (cRite.GetLossState().collection[0].S == 0)  //if not assign the GU loss yet
                if (cRite.GetLossState().GetTotalSum.S == 0)  //if not assign the GU loss yet
                {
                    guLossesEngine.GetGUForCoverageRITE(cRite);
                    double[] multiArr = RITE.GenerateMultiplierArr(cRite.RITE.ActNumOfSampleBldgs).ToArray();

                    for (int i = 0; i < cRite.RITE.ActNumOfSampleBldgs; i++)
                    {
                        //cRite.GetLossState().collection[i].S = GULoss[i] * multiArr[i];
                        cRite.GetLossState().SubjectLoss[i] = cRite.GetLossState().SubjectLoss[i] * multiArr[i];
                        cRite.GetLossState().Recoverable[i] = cRite.GetLossState().SubjectLoss[i] * multiArr[i];                          
                        cRite.GetLossState().Deductible[i] = 0;
                        cRite.GetLossState().Excess[i] = 0;
                    }
                }

                for (int i = 0; i < cRite.RITE.ActNumOfSampleBldgs; i++)
                {
                    //init the allocation state
                    cRite.GetAllocState().CalcTotalSum();
                    cRite.GetAllocState().SubjectLoss[i] = cRite.GetLossState().SubjectLoss[i];
                }                                                  
            }           

            Aggregation aggType = Aggregation.Summed;

            if (currTermNode.IsPerRisk && currTermNode.PrimarySubject.Schedule.ActNumOfBldgs > 1)
                aggType = Aggregation.PerBuilding;

            //if no childrenNodes, nothing to do with the Interaction: the interaction terms are all zeros.
            if (SubjectCRITEs.Count > 0)
            {
                //initialize InterObj
                LossStateCollection2 InterObj = GetInterObjForOverlap(currTermNode, childrenNodes, aggType, SubjectCRITEs);
                TermFunctionalEngine_4 tFunEng = new TermFunctionalEngine_4(currTermNode, aggType);
                tFunEng.TermFunction(InterObj);
            }
            else
            {
                TermFunctionalEngine_4 tFunEng = new TermFunctionalEngine_4(currTermNode, aggType);
                tFunEng.TermFunction(new LossStateCollection2(1));
            }

            //Final Adjustment                           
            currTermNode.CurrentLossStateCollection.AdjustR();           
        }

        private void AllocateOverlappedCoverNode(CoverNode currCoverNode, HashSet<CoverageAtomicRITE> SubjectARITEs)
        {
            int numOfChildren = SubjectARITEs.Count;

            //for cover node, always allocate as summed            
            Double childrenRSum = SubjectARITEs.Sum(item => item.GetLossState().GetTotalSum.R);
            Double childrenSSum = SubjectARITEs.Sum(item => item.GetLossState().GetTotalSum.S);
            AllocationStateCollection2[] MidAllocationStateCollection = new AllocationStateCollection2[numOfChildren];

            int j = 0;
            foreach (CoverageAtomicRITE childNode in SubjectARITEs)
            {
                MidAllocationStateCollection[j] = new AllocationStateCollection2(childNode.GetAllocState().NumBldgs);
                j++;
            }

            if (childrenRSum != 0)
            {
                j = 0;  //child
                foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                {
                    //devided among children
                    double temp = currCoverNode.GetPayout() * childNode.GetLossState().GetTotalSum.R / childrenRSum;
                    for (int jj = 0; jj < childNode.GetAllocState().NumBldgs; jj++)
                    {
                        //divided among building
                        if (childNode.GetLossState().GetTotalSum.S > 0)
                            MidAllocationStateCollection[j].Payout[jj] = temp * childNode.GetLossState().SubjectLoss[jj] / childNode.GetLossState().GetTotalSum.S;
                        else
                            MidAllocationStateCollection[j].Payout[jj] = temp / childNode.GetAllocState().NumBldgs;
                    }
                    j++;
                }
            }
            else
            {
                j = 0;  //child
                foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                {
                    //devided among children
                    double temp = currCoverNode.GetPayout() * childNode.GetLossState().GetTotalSum.S / childrenSSum;
                    for (int jj = 0; jj < childNode.GetAllocState().NumBldgs; jj++)
                    {
                        //divided among building
                        if (childNode.GetLossState().GetTotalSum.S > 0)
                            MidAllocationStateCollection[j].Payout[jj] = temp * childNode.GetLossState().SubjectLoss[jj] / childNode.GetLossState().GetTotalSum.S;
                        else
                            MidAllocationStateCollection[j].Payout[jj] = temp / childNode.GetAllocState().NumBldgs;
                    }
                    j++;
                }
            }

            j = 0;
            foreach (CoverageAtomicRITE childNode in SubjectARITEs)
            {
                for (int jj = 0; jj < childNode.GetAllocState().NumBldgs; jj++)
                {
                    MidAllocationStateCollection[j].Payout[jj] += childNode.GetAllocState().Payout[jj];
                    MidAllocationStateCollection[j].SubjectLoss[jj] = childNode.GetAllocState().SubjectLoss[jj];
                }
                childNode.SetAllocState(MidAllocationStateCollection[j]);
                j++;
            }
        }

        private void AllocateOverlappedTermNode(TermNode currTermNode, HashSet<CoverageAtomicRITE> SubjectARITEs)
        {
            //find its AtomicRITEs
            //HashSet<AtomicRITE> SubjectARITEs = currTermNode.Subject.GetAtomicRites();
            Aggregation aggType = Aggregation.Summed;

            if (currTermNode.IsPerRisk && currTermNode.PrimarySubject.Schedule.ActNumOfBldgs > 1)
                aggType = Aggregation.PerBuilding;

            int numOfChildren = SubjectARITEs.Count;
            AllocationStateCollection2[] MidAllocationStateCollection = new AllocationStateCollection2[numOfChildren];

            int j = 0;
            foreach (CoverageAtomicRITE childNode in SubjectARITEs)
            {
                MidAllocationStateCollection[j] = new AllocationStateCollection2(childNode.GetAllocState().NumBldgs);
                j++;
            }

            //Allocation has to co-op with multi-building
            //always allocate to per-building           
            if (aggType == Aggregation.PerBuilding)
            {   // do it per-building
                foreach (CoverageAtomicRITE cRite in SubjectARITEs)
                {
                    if (cRite.GetLossState().NumBldgs != currTermNode.CurrentLossStateCollection.NumBldgs)
                        throw new InvalidOperationException("AtomicRite NumOfBuilding must be equal to its parent's numOfBuilding");
                }

                for (int i = 0; i < currTermNode.CurrentLossStateCollection.NumBldgs; i++)
                {
                    Double childrenDSum = SubjectARITEs.Sum(item => item.GetLossState().Deductible[i]);
                    Double childrenRSum = SubjectARITEs.Sum(item => item.GetLossState().Recoverable[i]);
                    Double childrenSSum = SubjectARITEs.Sum(item => item.GetLossState().SubjectLoss[i]);
                    Double diffD = currTermNode.CurrentLossStateCollection.Deductible[i] - childrenDSum;

                    if (childrenRSum == 0)
                    {
                        j = 0;
                        foreach (AtomicRITE childNode in SubjectARITEs)
                        {
                            MidAllocationStateCollection[j].Recoverable[i] = currTermNode.CurrentLossStateCollection.Recoverable[i] * childNode.GetLossState().SubjectLoss[i] / childrenSSum;
                            j++;
                        }
                    }
                    else if (diffD >= 0)
                    {
                        j = 0;
                        foreach (AtomicRITE childNode in SubjectARITEs)
                        {
                            MidAllocationStateCollection[j].Recoverable[i] = childNode.GetLossState().Recoverable[i] * (1 - diffD / childrenRSum);
                            MidAllocationStateCollection[j].Deductible[i] = childNode.GetLossState().SubjectLoss[i] - childNode.GetLossState().Excess[i] - MidAllocationStateCollection[j].Recoverable[i];
                            j++;
                        }
                    }
                    else
                    {
                        j = 0;
                        foreach (AtomicRITE childNode in SubjectARITEs)
                        {
                            MidAllocationStateCollection[j].Deductible[i] = childNode.GetLossState().Deductible[i] * (1 + diffD / childrenDSum);
                            MidAllocationStateCollection[j].Recoverable[i] = childNode.GetLossState().SubjectLoss[i] - childNode.GetLossState().Excess[i] - MidAllocationStateCollection[j].Deductible[i];
                            j++;
                        }
                    }

                    Double childrenRaSum = MidAllocationStateCollection.Sum(item => item.Recoverable[i]);
                    int jj = 0;
                    if (diffD >= 0 && childrenRaSum == 0)
                    {
                        foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                        {
                            MidAllocationStateCollection[jj].Recoverable[i] = childNode.GetLossState().Recoverable[i];
                            jj++;
                        }
                        childrenRaSum = MidAllocationStateCollection.Sum(item => item.Recoverable[i]);
                    }

                    jj = 0;
                    foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                    {
                        if (currTermNode.CurrentLossStateCollection.Recoverable[i] == 0)
                            MidAllocationStateCollection[jj].Recoverable[i] = 0;
                        else if (childrenRaSum > 0)
                        {
                            MidAllocationStateCollection[jj].Recoverable[i] = currTermNode.CurrentLossStateCollection.Recoverable[i] * MidAllocationStateCollection[jj].Recoverable[i] / childrenRaSum;
                        }

                        MidAllocationStateCollection[jj].SubjectLoss[i] = childNode.GetAllocState().SubjectLoss[i];
                        //if (MidAllocationStateCollection[jj].collection[i].R > childNode.GetAllocState().collection[i].R ||
                        //      (MidAllocationStateCollection[jj].collection[i].R == childNode.GetAllocState().collection[i].R
                        //          && MidAllocationStateCollection[jj].collection[i].D > childNode.GetAllocState().collection[i].D))
                        //{
                        //    childNode.SetAllocState(MidAllocationStateCollection[jj]);
                        //    //childNode.GetAllocState().collection[i].R = MidAllocationStateCollection[jj].collection[i].R;
                        //    //childNode.GetAllocState().collection[i].D = MidAllocationStateCollection[jj].collection[i].D;
                        //}                       
                        jj++;
                    }
                }  //end of building i 
                //compare,always compare the summed, then pick the winner

                int ii = 0;
                foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                {
                    if (MidAllocationStateCollection[ii].GetTotalSum.R > childNode.GetAllocState().GetTotalSum.R ||
                             (MidAllocationStateCollection[ii].GetTotalSum.R == childNode.GetAllocState().GetTotalSum.R
                                 && MidAllocationStateCollection[ii].GetTotalSum.D > childNode.GetAllocState().GetTotalSum.D))
                    {
                        childNode.SetAllocState(MidAllocationStateCollection[ii]);
                    }
                    ii++;
                }
            }
            else //not per-risk, but still allocate to per-building
            {
                AllocationStateCollection2[] tempMid = new AllocationStateCollection2[numOfChildren];
                j = 0;
                foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                {
                    tempMid[j] = new AllocationStateCollection2(1);
                    j++;
                }

                Double childrenDSum = SubjectARITEs.Sum(item => item.GetLossState().GetTotalSum.D);
                Double childrenRSum = SubjectARITEs.Sum(item => item.GetLossState().GetTotalSum.R);
                Double childrenSSum = SubjectARITEs.Sum(item => item.GetLossState().GetTotalSum.S);
                Double diffD = currTermNode.GetLossState().GetTotalSum.D - childrenDSum;

                if (childrenRSum == 0)
                {
                    j = 0;
                    foreach (AtomicRITE childNode in SubjectARITEs)
                    {
                        tempMid[j].Recoverable[0] = currTermNode.GetLossState().GetTotalSum.R * childNode.GetLossState().GetTotalSum.S / childrenSSum;
                        j++;
                    }
                }
                else if (diffD >= 0)
                {
                    j = 0;
                    foreach (AtomicRITE childNode in SubjectARITEs)
                    {
                        tempMid[j].Recoverable[0] = childNode.GetLossState().GetTotalSum.R * (1 - diffD / childrenRSum);
                        tempMid[j].Deductible[0] = childNode.GetLossState().GetTotalSum.S - childNode.GetLossState().GetTotalSum.X - tempMid[j].Recoverable[0];
                        j++;
                    }
                }
                else
                {
                    j = 0;
                    foreach (AtomicRITE childNode in SubjectARITEs)
                    {
                        tempMid[j].Deductible[0] = childNode.GetLossState().GetTotalSum.D * (1 + diffD / childrenDSum);
                        tempMid[j].Recoverable[0] = childNode.GetLossState().GetTotalSum.S - childNode.GetLossState().GetTotalSum.X - tempMid[j].Deductible[0];
                        j++;
                    }
                }

                Double childrenRaSum = tempMid.Sum(item => item.Recoverable[0]);
                int k = 0;

                if (diffD >= 0 && childrenRaSum == 0)
                {
                    foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                    {
                        tempMid[k].Recoverable[0] = childNode.GetLossState().GetTotalSum.R;
                        k++;
                    }
                    childrenRaSum = tempMid.Sum(item => item.Recoverable[0]);
                }

                k = 0;
                foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                {
                    if (currTermNode.GetLossState().GetTotalSum.R == 0)
                        tempMid[k].Recoverable[0] = 0;
                    else if (childrenRaSum > 0)
                        tempMid[k].Recoverable[0] = currTermNode.GetLossState().GetTotalSum.R * tempMid[k].Recoverable[0] / childrenRaSum;

                    if (tempMid[k].Recoverable[0] > childNode.GetAllocState().GetTotalSum.R ||
                        (tempMid[k].Recoverable[0] == childNode.GetAllocState().GetTotalSum.R &&
                          tempMid[k].Deductible[0] > childNode.GetAllocState().GetTotalSum.D))
                    {
                        childNode.GetAllocState().CalcTotalSum();
                        for (int i = 0; i < childNode.GetAllocState().NumBldgs; i++)
                        {
                            MidAllocationStateCollection[k].Deductible[i] = tempMid[k].Deductible[0] * childNode.GetAllocState().SubjectLoss[i] / childNode.GetAllocState().GetTotalSum.S;
                            MidAllocationStateCollection[k].Recoverable[i] = tempMid[k].Recoverable[0] * childNode.GetAllocState().SubjectLoss[i] / childNode.GetAllocState().GetTotalSum.S;
                            MidAllocationStateCollection[k].SubjectLoss[i] = childNode.GetAllocState().SubjectLoss[i];
                            MidAllocationStateCollection[k].Excess[i] = MidAllocationStateCollection[k].SubjectLoss[i] - MidAllocationStateCollection[k].Recoverable[i] - MidAllocationStateCollection[k].Deductible[i];
                        }
                        childNode.SetAllocState(MidAllocationStateCollection[k]);
                    }
                    k++;
                }
            }
        }
    }

    public class PrimaryGraphOfNodesExecuter : GraphOfNodesExecuter
    {
        public PrimaryGraph primaryGraph
        {
            get
            {
                PrimaryGraph pgraph = graph as PrimaryGraph;
                return pgraph;
            }
        }

        public PrimaryGraphOfNodesExecuter(PrimaryGraph _graph)
        {
            graph = _graph;
        }

        protected override void GetGUForAtomicRITEs(GULossAdaptor guLoss)
        {
            bool GotGUForAll = true;
            GUInputEngine guInputForNodeGraph = new GUInputEngine(guLoss.GetDictTypeLosses(NodeGraph.ContractID), NodeGraph);
            foreach (CoverageAtomicRITE aRITE in primaryGraph.GetCoverageRITEs())
            {
                guInputForNodeGraph.GetGUForCoverageRITE(aRITE);
            }
        }

        protected override bool ShouldProcessEvent(GULossAdaptor guLoss)
        {
            //Not implemented now. Only return true;
            return true;
        }
    }

    public class TreatyGraphOfNodesExecuter : GraphOfNodesExecuter
    {
        public TreatyGraph treatyGraph
        {
            get
            {
                TreatyGraph tgraph = graph as TreatyGraph;
                return tgraph;
            }
        }

        public TreatyGraphOfNodesExecuter(TreatyGraph _graph)
        {
            graph = _graph;
        }

        protected override void GetGUForAtomicRITEs(GULossAdaptor guLoss)
        {
            bool GotGUForAll = true;
            foreach (ContractAtomicRITE aRITE in treatyGraph.ContractRITEs)
            {
                if (!aRITE.contractGraph.Graph.IsExecuted)
                {
                    GraphExecuterAdaptor MainExecuter = new GraphExecuterAdaptor(aRITE.contractGraph);

                    MainExecuter.RunExecution(guLoss);
                }
            }
        }

        protected override bool ShouldProcessEvent(GULossAdaptor guLoss)
        {
            if (guLoss.GetEarliestEventDateAmongContracts() < treatyGraph.Declarations.Inception 
                || guLoss.GetEarliestEventDateAmongContracts() > treatyGraph.Declarations.Expiration)
                return false;
            else
                return true;
        }
    }

    public class GraphOfMatrixExecuter : GraphExecuter
    {
        public GraphOfMatrix MatrixGraph
        {
            get
            {
                GraphOfMatrix mgraph = graph as GraphOfMatrix;
                return mgraph;
            }
        }

        public GraphOfMatrixExecuter(GraphOfMatrix matrixgraph)
        {
            graph = matrixgraph;
        }

        protected override void PrepareGraphForExecution(GULossAdaptor guLoss)
        { }

        protected override QuerryableLossOutput GetSubjectLoss()
        {
            //Default empty implementation....no actual subject losses returned..
            return new QuerryableLossOutput();
        }

        protected override ReferenceResultOutput ExecuteWindow(TimeWindow timewindow, GULossAdaptor guLoss)
        {
            //Does not currently apply time window, thus does not support hour clause on Primary contract...

            NGFM.Reference.MatrixHDFM.MatrixGraphExecuter matrixGraphExecuter = new NGFM.Reference.MatrixHDFM.MatrixGraphExecuter(MatrixGraph.Graph);
            NGFM.Reference.MatrixHDFM.IVectorEvent lossEvent = guLoss.GetVectorTypeLosses(MatrixGraph.ContractID);
            //double totalPayout = matrixGraphExecuter.Run(lossEvent);

            MatrixResultOutput ReferenceOutPut = matrixGraphExecuter.Run(lossEvent);

            ReferenceOutPut.EventStartDate = guLoss.GetEventStartDate(MatrixGraph.ContractID);

            AllocatedLossAdaptor allocationHelper = guLoss.GetAllocationHelper(MatrixGraph.ContractID);

            List<AtomicLoss> AllocatedLossList = allocationHelper.GetAllocatedLossList(ReferenceOutPut.AllocatedRitePayout, ReferenceOutPut.RiteGULossIndicies, lossEvent.TimeStamps, MatrixGraph.Declarations);

            return new ReferenceResultOutput(ReferenceOutPut.TotalPayOut, ReferenceOutPut.TotalGULoss, AllocatedLossList, ReferenceOutPut.EventStartDate);
        }

        protected override bool ShouldProcessEvent(GULossAdaptor guLoss)
        {
            //Not Implemented now only return true
            return true;
        }
    }

}
