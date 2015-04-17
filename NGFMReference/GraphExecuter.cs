using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NGFMReference;

namespace NGFMReference
{
    public abstract class GraphExecuter
    {
        protected Graph graph;

        public double Execute(Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> inputloss)
        {
            GUInputEngine guinputengine = new GUInputEngine(inputloss, graph);
            return Execute(guinputengine);
            
        } //Execute
        
        public double Execute(GUInputEngine guinputengine)
        {
            GetGUForAtomicRITEs(guinputengine);

            List<TimeWindow> timewindows;
            if(graph.Declarations.IsHoursClause)
                timewindows = guinputengine.GenerateWindows(graph.Declarations.HoursClauses[0].Duration);
            else
                timewindows = new List<TimeWindow>(){new TimeWindow()};

            List<ExecutionOutput> lst_execoutputs = new List<ExecutionOutput>();
            foreach (TimeWindow tw in timewindows)
            {
                lst_execoutputs.Add(ExecuteWindow(tw, guinputengine));
            }

            //find the timewindow that has the maximum payout
            ExecutionOutput MaxPayout_ExecutionOutput = lst_execoutputs.First();
            foreach (ExecutionOutput exout in lst_execoutputs)
            {
                if (exout.Payout > MaxPayout_ExecutionOutput.Payout)
                    MaxPayout_ExecutionOutput = exout;
            }

            graph.exResults = MaxPayout_ExecutionOutput;
            return MaxPayout_ExecutionOutput.Payout;                      
        } //Execute

        private ExecutionOutput ExecuteWindow(TimeWindow timewindow, GUInputEngine guLossesEngine)
        {
            ApplyWindowToGU(timewindow);  //unrestricted, raintest  

            List<CoverNode> TopCovers = graph.TopNodes.OfType<CoverNode>().ToList();

            ExecutionOutput totalExOutput = new ExecutionOutput();
            foreach (CoverNode coverNode in TopCovers)
            {
                totalExOutput += ExecuteCover(guLossesEngine, coverNode);
            }

            return totalExOutput;                        
        }

        public ExecutionOutput ExecuteCover(GUInputEngine guinputengine, CoverNode topCover)
        {
            if (graph.IsOverlapped == false)
            {
                RecursiveExecution(topCover, guinputengine);
                //Allocate Graph
                GraphAllocation Allocater = new GraphAllocation(graph);
                Allocater.AllocateGraph();
            }
            else
            {               
                ExecuteOverlappedGraph(guinputengine);
            }

            graph.IsExecuted = true;
            graph.exResults = new ExecutionOutput(topCover.Payout, graph.AtomicRites);
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
                List<GraphNode> childrenNodes = graph.GetChildrenForNode(currTermNode);
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
                inputlosses = graph.GetNodeSubjectLoss(currTermNode).AllLoss();
                currTermNode.CurrentLossStateCollection.SetSubjectLosses(inputlosses);
                
                if (currTermNode.IsPerRisk == true && currTermNode.PrimarySubject.Schedule.ActNumOfBldgs > 1)
                    aggType = Aggregation.PerBuilding;
                else 
                {
                    //need reset the lossState to act as only one building
                    LossStateCollection tempLossStateCollection = new LossStateCollection(1);
                    tempLossStateCollection.collection[0] = currTermNode.CurrentLossStateCollection.GetTotalSum;
                    currTermNode.CurrentLossStateCollection = tempLossStateCollection;                
                }                

                //if no childrenNodes, nothing to do with the Interaction: the interaction terms are all zeros.
                if (childrenNodes.Count > 0)
                {
                    //initialize InterObj
                    InteractionObject[] InterObj = GetInterObj(currTermNode, aggType, childrenTermNodes);
                    TermFunctionalEngine tFunEng = new TermFunctionalEngine(currTermNode, aggType);
                    tFunEng.TermFunction(InterObj);
                 
                    //Interaction
                    //InteractionEngine InterEng = new InteractionEngine(currTermNode, aggType, InterObj);
                    //InterEng.Interaction();
                }
                else
                {
                    TermFunctionalEngine tFunEng = new TermFunctionalEngine(currTermNode, aggType);
                    tFunEng.TermFunction(new InteractionObject[0]); //no interaction
                }
                            
               //Final Adjustment
                for (int i = 0; i < currTermNode.CurrentLossStateCollection.NumBldgs; i++)
                {
                    currTermNode.CurrentLossStateCollection.collection[i].AdjustR();      
                }
                currTermNode.CurrentLossStateCollection.CalcTotalSum();

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

               // Parallel.ForEach(graph.GetChildrenForNode(currCoverNode), childNode =>
               foreach (GraphNode childNode in graph.GetChildrenForNode(currCoverNode))
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
              // );
                
                CoverNodeFunctionalEngine coverNodeFuncEng = new CoverNodeFunctionalEngine(currCoverNode);
                coverNodeFuncEng.CoverNodeFunction();
                currCoverNode.Executed = true;

            } //currNode is Cover Node
        }

        private InteractionObject[] GetInterObj(TermNode currNode, Aggregation aggType,  List<TermNode> childrenNodes)
        {

            InteractionObject[] InterObj = new InteractionObject[currNode.CurrentLossStateCollection.NumBldgs];                    
            for (int i = 0; i < currNode.CurrentLossStateCollection.NumBldgs; i++)
            {
                InterObj[i] = new InteractionObject();
            }
   
            foreach (TermNode childNode in childrenNodes)
            {                
                //if (currNode.TermIsPerRisk && currNode.CurrentLossStateCollection.NumBldgs > 1)
                if (aggType == Aggregation.PerBuilding)
                {
                    for (int i = 0; i < currNode.CurrentLossStateCollection.NumBldgs; i++)
                    {
                        InterObj[i].UpdateInterObjState(childNode.CurrentLossStateCollection.collection[i]);
                    }
                }
                else
                {
                    LossState childLossesState = new LossState(childNode.CurrentLossStateCollection.GetTotalSum);
                    InterObj[0].UpdateInterObjState(childLossesState);
                }
            }  //foreach child
            return InterObj;
        }
     
        private InteractionObject[] GetInterObjForOverlap(TermNode currNode, List<GraphNode> childrenNodes, Aggregation aggType, HashSet<CoverageAtomicRITE> SubjectARites)
        {

            InteractionObject[] InterObj = new InteractionObject[currNode.CurrentLossStateCollection.NumBldgs];
            for (int i = 0; i < currNode.CurrentLossStateCollection.NumBldgs; i++)
            {
                InterObj[i] = new InteractionObject();
            }
            //Get Single largest Deductible, based on the children nodes, not AtomicRites

            foreach (CoverageAtomicRITE aRite in SubjectARites)
            {
                if (aggType == Aggregation.PerBuilding)
                {
                    if (currNode.CurrentLossStateCollection.NumBldgs != aRite.RITE.ActNumOfBldgs)
                        throw new InvalidOperationException("AtomicRite NumOfBuilding must be equal to its parent's numOfBuilding");

                    for (int i = 0; i < currNode.CurrentLossStateCollection.NumBldgs; i++)
                    {
                        InterObj[i].UpdateInterObjStateForARITE(aRite.GetLossState().collection[i]);
                    }
                }
                else
                {
                    //AllocationState aRiteAllocState = new AllocationState(aRite.CurrentAllocationState);
                    InterObj[0].UpdateInterObjStateForARITE(aRite.GetLossState().GetTotalSum);
                }
            }  //foreach child

            //update for SingleLargestDed
            foreach (GraphNode childNode in childrenNodes)
            {
                TermNode childTermNode = childNode as TermNode;
                if (aggType == Aggregation.PerBuilding)
                {
                    if (currNode.CurrentLossStateCollection.NumBldgs != currNode.CurrentLossStateCollection.NumBldgs)
                        throw new InvalidOperationException("child NumOfBuilding must be equal to its parent's numOfBuilding");

                    for (int i = 0; i < currNode.CurrentLossStateCollection.NumBldgs; i++)
                    {
                        InterObj[i].LargestDedFromChildren = Math.Max(InterObj[i].LargestDedFromChildren, childTermNode.CurrentLossStateCollection.collection[i].D); //UpdateInterObjStateForARITE(aRite.GetLossState().collection[i]);
                    }
                }
                else
                {
                    //AllocationState aRiteAllocState = new AllocationState(aRite.CurrentAllocationState);
                    InterObj[0].LargestDedFromChildren = Math.Max(InterObj[0].LargestDedFromChildren, childTermNode.CurrentLossStateCollection.GetTotalSum.D); //(aRite.GetLossState().GetTotalSum);
                }
            }
            return InterObj;
        }

        protected abstract void GetGUForAtomicRITEs(GUInputEngine guLossesEngine);

        protected void ApplyWindowToGU(TimeWindow window)
        {
            foreach (AtomicRITE aRITE in graph.AtomicRites)
            {
                aRITE.SetSubjectLoss(aRITE.OriginalSubjectLoss.ApplyWindow(window));
            }
        }

        private void ExecuteOverlappedGraph(GUInputEngine guLossesEngine)
        {
            List<int> levelList = new List<int>();
            foreach (int aLevel in graph.LevelNodeDict.Keys)
            {
                levelList.Add(aLevel);
            }

            
            for (int i = levelList.Max(); i > 0; i--)
            {
                HashSet<CoverageAtomicRITE> wholeList = new HashSet<CoverageAtomicRITE>();
                foreach (GraphNode currNode in graph.LevelNodeDict[i])
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
                    childrenNodes = graph.GetChildrenForNode(currTermNode);

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
                            for (int j = 0; j < childNode.RITE.ActNumOfBldgs; j++)
                            {
                                //childNode.GetLossState().collection[j].S = childNode.GetAllocationState().collection[j].S;
                                childNode.GetLossState().collection[j].R = childNode.GetAllocState().collection[j].R;
                                childNode.GetLossState().collection[j].D = childNode.GetAllocState().collection[j].D;
                                childNode.GetLossState().collection[j].X = childNode.GetLossState().collection[j].S - childNode.GetLossState().collection[j].R - childNode.GetLossState().collection[j].D;

                                childNode.GetAllocState().collection[j].R = 0;    //refresh to be compared                                                   
                            }                            
                        //}
                        //else
                        //{
                        //    double loss = childNode.GetLossState().GetTotalSum.S;
                        //    childNode.ResetLossState(1);
                        //    childNode.GetLossState().collection[0].S = loss;
                        //    childNode.GetLossState().collection[0].R = childNode.GetAllocationStateSummed().R;
                        //    childNode.GetLossState().collection[0].D = childNode.GetAllocationStateSummed().D;
                        //    childNode.GetLossState().collection[0].X = childNode.GetLossState().collection[0].S - childNode.GetLossState().collection[0].R - childNode.GetLossState().collection[0].D;
                        
                        //    //reset R for next level comparison
                        //    childNode.GetAllocationStateSummed().R = 0;
                        //}
                    }              
                }
            //then lelve i = 0, all are bottom CoverNode's which are connected to TermNode
            foreach (CoverNode currCoverNode in graph.LevelNodeDict[0])
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
            inputlosses = graph.GetNodeSubjectLoss(currTermNode).AllLoss();
            if (currTermNode.IsPerRisk)
                currTermNode.CurrentLossStateCollection.SetSubjectLosses(inputlosses);
            else
                currTermNode.CurrentLossStateCollection.SetSubjectLosses(new double[]{inputlosses.Sum()});


            //if (isLowestLevel) //initialize to GU loss
            //{
                foreach (CoverageAtomicRITE cRite in SubjectCRITEs)
                {
                    //double[] GULoss = guLossesEngine.GetGUForCoverageRITE(CoverageAtomicRITE cRITE);
                    //if (cRite.GetLossState().collection[0].S == 0)  //if not assign the GU loss yet
                    if (cRite.GetLossState().GetTotalSum.S == 0)  //if not assign the GU loss yet
                    {
                        guLossesEngine.GetGUForCoverageRITE(cRite);
                        int[] multiArr = RITE.GenerateMultiplierArr(cRite.RITE.ActNumOfBldgs).ToArray();

                        for (int i = 0; i < cRite.RITE.ActNumOfBldgs; i++)
                        { 
                            //cRite.GetLossState().collection[i].S = GULoss[i] * multiArr[i];
                            cRite.GetLossState().collection[i].S = cRite.GetLossState().collection[i].S * multiArr[i];
                            cRite.GetLossState().collection[i].R = cRite.GetLossState().collection[i].S;
                            cRite.GetLossState().collection[i].D = 0;
                            cRite.GetLossState().collection[i].X = 0;                           
                        }               
                    }

                    for (int i = 0; i < cRite.RITE.ActNumOfBldgs; i++)
                    {
                        //init the allocation state
                        cRite.GetAllocState().collection[i].S = cRite.GetLossState().collection[i].S;
                     }
                    //cRite.CurrentLossState = cRite.CurrentLossStateCollection.GetTotalSum;                                   
                }
            //}

            Aggregation aggType = Aggregation.Summed;

            if (currTermNode.IsPerRisk && currTermNode.PrimarySubject.Schedule.ActNumOfBldgs > 1)
                aggType = Aggregation.PerBuilding;
            //else
            //{ 
                //need reset the lossState to act as only one building
                //LossStateCollection tempLossStateCollection = new LossStateCollection(1);
                //tempLossStateCollection.collection[0] = currTermNode.CurrentLossStateCollection.GetTotalSum;
                //currTermNode.CurrentLossStateCollection = tempLossStateCollection;
            //}
                                
            //if no childrenNodes, nothing to do with the Interaction: the interaction terms are all zeros.
            if (SubjectCRITEs.Count > 0)
            {
                //initialize InterObj
                InteractionObject[] InterObj = GetInterObjForOverlap(currTermNode, childrenNodes, aggType, SubjectCRITEs);
                TermFunctionalEngine tFunEng = new TermFunctionalEngine(currTermNode, aggType);
                tFunEng.TermFunction(InterObj);
            }
            else 
            {
                TermFunctionalEngine tFunEng = new TermFunctionalEngine(currTermNode, aggType);
                tFunEng.TermFunction(new InteractionObject[0]);
            }

            //Final Adjustment
            for (int i = 0; i < currTermNode.CurrentLossStateCollection.NumBldgs; i++)
            {
                currTermNode.CurrentLossStateCollection.collection[i].AdjustR();
            } 
        }

        private void AllocateOverlappedCoverNode(CoverNode currCoverNode, HashSet<CoverageAtomicRITE> SubjectARITEs)
        {
            int numOfChildren = SubjectARITEs.Count;

            //for cover node, always allocate as summed            
            Double childrenRSum = SubjectARITEs.Sum(item => item.GetLossState().GetTotalSum.R);
            Double childrenSSum = SubjectARITEs.Sum(item => item.GetLossState().GetTotalSum.S);
            AllocationStateCollection[] MidAllocationStateCollection = new AllocationStateCollection[numOfChildren];                

            int j = 0;
            foreach (CoverageAtomicRITE childNode in SubjectARITEs)
            {
                MidAllocationStateCollection[j] = new AllocationStateCollection(childNode.GetAllocState().NumBldgs);                 
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
                            MidAllocationStateCollection[j].collection[jj].P = temp * childNode.GetLossState().collection[jj].S / childNode.GetLossState().GetTotalSum.S;
                        else
                            MidAllocationStateCollection[j].collection[jj].P = temp / childNode.GetAllocState().NumBldgs;                                             
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
                            MidAllocationStateCollection[j].collection[jj].P = temp * childNode.GetLossState().collection[jj].S / childNode.GetLossState().GetTotalSum.S;
                        else
                            MidAllocationStateCollection[j].collection[jj].P = temp / childNode.GetAllocState().NumBldgs;                                             
                    }
                    j++;
                }                
            }

            j = 0;
            foreach (CoverageAtomicRITE childNode in SubjectARITEs)
            {                                    
                for (int jj = 0; jj < childNode.GetAllocState().NumBldgs; jj++)
                {
                    MidAllocationStateCollection[j].collection[jj].P += childNode.GetAllocState().collection[jj].P;
                    MidAllocationStateCollection[j].collection[jj].S = childNode.GetAllocState().collection[jj].S;
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
            AllocationStateCollection[] MidAllocationStateCollection = new AllocationStateCollection[numOfChildren];

            int j = 0;
            foreach (CoverageAtomicRITE childNode in SubjectARITEs)
            {
                MidAllocationStateCollection[j] = new AllocationStateCollection(childNode.GetAllocState().NumBldgs);
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
                    Double childrenDSum = SubjectARITEs.Sum(item => item.GetLossState().collection[i].D);
                    Double childrenRSum = SubjectARITEs.Sum(item => item.GetLossState().collection[i].R);
                    Double childrenSSum = SubjectARITEs.Sum(item => item.GetLossState().collection[i].S);
                    Double diffD = currTermNode.CurrentLossStateCollection.collection[i].D - childrenDSum;

                    if (childrenRSum == 0)
                    {
                        j = 0;
                        foreach (AtomicRITE childNode in SubjectARITEs)
                        {
                            MidAllocationStateCollection[j].collection[i].R = currTermNode.CurrentLossStateCollection.collection[i].R  * childNode.GetLossState().collection[i].S / childrenSSum;
                            j++;
                        }
                    }
                    else if (diffD >= 0)
                    {
                        j = 0;
                        foreach (AtomicRITE childNode in SubjectARITEs)
                        {
                            MidAllocationStateCollection[j].collection[i].R = childNode.GetLossState().collection[i].R * (1 - diffD / childrenRSum);
                            MidAllocationStateCollection[j].collection[i].D = childNode.GetLossState().collection[i].S - childNode.GetLossState().collection[i].X - MidAllocationStateCollection[j].collection[i].R;
                            j++;
                        }
                    }
                    else
                    {
                        j = 0;
                        foreach (AtomicRITE childNode in SubjectARITEs)
                        {
                            MidAllocationStateCollection[j].collection[i].D = childNode.GetLossState().collection[i].D * (1 + diffD / childrenDSum);
                            MidAllocationStateCollection[j].collection[i].R = childNode.GetLossState().collection[i].S - childNode.GetLossState().collection[i].X - MidAllocationStateCollection[j].collection[i].D;
                            j++;
                        }
                    }

                    Double childrenRaSum = MidAllocationStateCollection.Sum(item => item.collection[i].R);
                    int jj = 0;
                    if (diffD >= 0 && childrenRaSum == 0)
                    {
                        foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                        {
                            MidAllocationStateCollection[jj].collection[i].R = childNode.GetLossState().collection[i].R;
                            jj++;
                        }
                        childrenRaSum = MidAllocationStateCollection.Sum(item => item.collection[i].R);
                    }

                    jj = 0;
                    foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                    {
                        if (currTermNode.CurrentLossStateCollection.collection[i].R == 0)
                            MidAllocationStateCollection[jj].collection[i].R = 0;
                        else if (childrenRaSum > 0)
                        {
                            MidAllocationStateCollection[jj].collection[i].R = currTermNode.CurrentLossStateCollection.collection[i].R * MidAllocationStateCollection[jj].collection[i].R / childrenRaSum;
                        }

                        MidAllocationStateCollection[jj].collection[i].S = childNode.GetAllocState().collection[i].S;
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
                AllocationStateCollection[] tempMid = new AllocationStateCollection[numOfChildren];
                j = 0;
                foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                {
                    tempMid[j] = new AllocationStateCollection(1);
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
                        tempMid[j].collection[0].R = currTermNode.GetLossState().GetTotalSum.R * childNode.GetLossState().GetTotalSum.S / childrenSSum;
                        j++;
                    }
                }
                else if (diffD >= 0)
                {
                    j = 0;
                    foreach (AtomicRITE childNode in SubjectARITEs)
                    {
                        tempMid[j].collection[0].R = childNode.GetLossState().GetTotalSum.R * (1 - diffD / childrenRSum);
                        tempMid[j].collection[0].D = childNode.GetLossState().GetTotalSum.S - childNode.GetLossState().GetTotalSum.X - tempMid[j].collection[0].R;
                        j++;
                    }
                }
                else
                {
                    j = 0;
                    foreach (AtomicRITE childNode in SubjectARITEs)
                    {
                        tempMid[j].collection[0].D = childNode.GetLossState().GetTotalSum.D * (1 + diffD / childrenDSum);
                        tempMid[j].collection[0].R = childNode.GetLossState().GetTotalSum.S - childNode.GetLossState().GetTotalSum.X - tempMid[j].collection[0].D;
                        j++;
                    }
                }

                Double childrenRaSum = tempMid.Sum(item => item.collection[0].R);
                int k = 0;

                if (diffD >= 0 && childrenRaSum == 0)
                {
                    foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                    {
                        tempMid[k].collection[0].R = childNode.GetLossState().GetTotalSum.R;
                        k++;
                    }
                    childrenRaSum = tempMid.Sum(item => item.collection[0].R);
                }

                k = 0;
                foreach (CoverageAtomicRITE childNode in SubjectARITEs)
                {
                    if (currTermNode.GetLossState().GetTotalSum.R == 0)
                        tempMid[k].collection[0].R = 0;
                    else if (childrenRaSum > 0)
                        tempMid[k].collection[0].R = currTermNode.GetLossState().GetTotalSum.R * tempMid[k].collection[0].R / childrenRaSum;

                    if (tempMid[k].collection[0].R > childNode.GetAllocState().GetTotalSum.R ||
                        (tempMid[k].collection[0].R == childNode.GetAllocState().GetTotalSum.R &&
                          tempMid[k].collection[0].D > childNode.GetAllocState().GetTotalSum.D))
                    {
                        for (int i = 0; i < childNode.GetAllocState().NumBldgs; i++)
                        {
                            MidAllocationStateCollection[k].collection[i].D = tempMid[k].collection[0].D * childNode.GetAllocState().collection[i].S / childNode.GetAllocState().GetTotalSum.S;
                            MidAllocationStateCollection[k].collection[i].R = tempMid[k].collection[0].R * childNode.GetAllocState().collection[i].S / childNode.GetAllocState().GetTotalSum.S;
                            MidAllocationStateCollection[k].collection[i].S = childNode.GetAllocState().collection[i].S;
                        }
                        childNode.SetAllocState(MidAllocationStateCollection[k]);
                    }
                    k++;
                }
            }  
        }
    }


    public class PrimaryGraphExecuter : GraphExecuter
    {
        public PrimaryGraph primaryGraph
        {
            get
            {
                PrimaryGraph pgraph = graph as PrimaryGraph;
                return pgraph;
            }
        }

        public PrimaryGraphExecuter(PrimaryGraph _graph)
        {
            graph = _graph;
        }

        protected override void GetGUForAtomicRITEs(GUInputEngine guLossesEngine)
        {
            bool GotGUForAll = true;
            foreach (CoverageAtomicRITE aRITE in primaryGraph.GetCoverageRITEs())
            {
                guLossesEngine.GetGUForCoverageRITE(aRITE);
            }
        }
    }

    public class TreatyGraphExecuter : GraphExecuter
    {
        public TreatyGraph treatyGraph
        {
            get
            {
                TreatyGraph tgraph = graph as TreatyGraph;
                return tgraph;
            }
        }

        public TreatyGraphExecuter(TreatyGraph _graph)
        {
            graph = _graph;
        }

        protected override void GetGUForAtomicRITEs(GUInputEngine guLossesEngine)
        {
            bool GotGUForAll = true;
            foreach (ContractAtomicRITE aRITE in treatyGraph.ContractRITEs)
            {
                if (!aRITE.contractGraph.IsExecuted)
                {
                    GraphExecuter executer;
                    if (aRITE.contractGraph is PrimaryGraph)
                        executer = new PrimaryGraphExecuter(aRITE.contractGraph as PrimaryGraph);
                    else
                        executer = new TreatyGraphExecuter(aRITE.contractGraph as TreatyGraph);

                    executer.Execute(guLossesEngine);
                }
            }
        }
    }

    public class ExecutionOutput
    {
        public double Payout { get; protected set; }
        public List<AllocatedLoss> AllocatedLosses { get; protected set; }

        //public ExecutionOutput(double _payout, HashSet<CoverageAtomicRITE> rites)
        //{
        //    Payout = _payout;
        //    AllocatedLosses = new List<AllocatedLoss>();
        //    foreach (CoverageAtomicRITE covRITE in rites)
        //    {
        //        if (covRITE.AllocatedLoss > 0)
        //        {
        //            AllocatedLoss allocLoss = new AllocatedLoss();
        //            allocLoss.RITE = covRITE.RITE;
        //            allocLoss.ExpType = covRITE.ExpType;
        //            allocLoss.Subperil = covRITE.SubPeril;
        //            allocLoss.RITECharID = covRITE.RITCharacterisiticID;
        //            allocLoss.timestamp = covRITE.TimeStamp;
        //            allocLoss.Loss = covRITE.AllocatedLoss;
        //            AllocatedLosses.Add(allocLoss);
        //        }
        //    }

        //}

        public ExecutionOutput()
        {
            Payout = 0;
            AllocatedLosses = new List<AllocatedLoss>();
        }

        public ExecutionOutput(double _payout, HashSet<AtomicRITE> rites)
        {
            Payout = _payout;  //raintest
            
            AllocatedLosses = new List<AllocatedLoss>();
            foreach (AtomicRITE aRITE in rites)
            {
                aRITE.SetAllocState(aRITE.GetAllocState());
                foreach(TimeLoss timeloss in aRITE.AllocatedLossSeries)
                if (timeloss.Loss > 0)
                {
                    AllocatedLoss allocLoss = new AllocatedLoss();
                    allocLoss.ExpType = aRITE.ExpType;
                    allocLoss.Subperil = aRITE.SubPeril;
                    allocLoss.Loss = timeloss.Loss;
                    allocLoss.timestamp = timeloss.Time;
                    AllocatedLosses.Add(allocLoss);

                    //Remove when architecture for allcoation finalized !!!!!!!!!
                    if (aRITE is CoverageAtomicRITE)
                        allocLoss.ExposureID = (aRITE as CoverageAtomicRITE).RITCharacterisiticID;
                    else
                        allocLoss.ExposureID = (aRITE as ContractAtomicRITE).contractGraph.ContractID;
                    //Remove above ////////////////////////////////////////////////////
                }
            }             
        }

        public ExecutionOutput(double _payout, List<AllocatedLoss> losses)
        {
            Payout = _payout;
            AllocatedLosses = losses;
        }

        public LossTimeSeries GetPayoutTimeSeries()
        {
            var timeGroups =
                from allocLoss in AllocatedLosses
                group allocLoss by allocLoss.timestamp into g
                select new { Time = g.Key, Payouts = g};
            
            LossTimeSeries series = new LossTimeSeries(1);

            foreach (var g in timeGroups)
            {
                double totalpayout = g.Payouts.Select(payout => payout.Loss).Sum();
                series.AddLoss(g.Time, totalpayout);
            }

            return series;
        }

        public LossTimeSeries GetFilteredTimeSeries(string subperil, ExposureType exType)
        {
            var timeGroups =
                from allocLoss in AllocatedLosses
                group allocLoss by allocLoss.timestamp into g
                select new { Time = g.Key, Payouts = g };

            LossTimeSeries series = new LossTimeSeries(1);

            foreach (var g in timeGroups)
            {
                double totalpayout = g.Payouts.Where(payout => payout.Subperil == subperil && payout.ExpType == exType)
                                              .Select(payout => payout.Loss)
                                              .Sum();
                series.AddLoss(g.Time, totalpayout);
            }

            return series;
        }

        public static ExecutionOutput operator +(ExecutionOutput exOutput1, ExecutionOutput exOutput2)
        {
            double payout = exOutput1.Payout + exOutput2.Payout;

            List<AllocatedLoss> allocatedLosses = exOutput1.AllocatedLosses;

            foreach(AllocatedLoss allocLoss in exOutput2.AllocatedLosses)
            {
                AllocatedLoss sameLoss = allocatedLosses.Where(aLoss => aLoss.ExposureID == allocLoss.ExposureID)
                                                        .FirstOrDefault();

                if (sameLoss != null)
                    sameLoss.Loss += allocLoss.Loss;
                else
                    allocatedLosses.Add(allocLoss);
            }

            return new ExecutionOutput(payout, allocatedLosses);
        }
    }

    public class AllocatedLoss
    {
        public RITE RITE { get; set; }
        public ExposureType ExpType { get; set; }
        public string Subperil { get; set; }
        public long ExposureID { get; set; }
        public uint timestamp { get; set; }
        public double Loss { get; set; }
    }
}
