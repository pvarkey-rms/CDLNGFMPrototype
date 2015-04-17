using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;
using NGFM.Reference.MatrixHDFM;
using NGFMReference.ContractModel;

namespace NGFMReference
{
    public abstract class AutoGraphBuilder
    {
        protected ContractExtractor Contract;
        protected DescriptiveGraph DGraph;
        protected PrimaryContractNodeBuilder NodeList;
        protected DescriptiveGraphBuilder DGraphBuilder;
 
        public AutoGraphBuilder(ContractExtractor _contract)
        {
            Contract = _contract;
            List<TermNode> ContractTermNodes;
            List<CoverNode> ContractCoverNodes;
            ContractNodeBuilder NodeBuilder;

            if (Contract.IsTreaty)
                NodeBuilder = new TreatyContractNodeBuilder(Contract as TreatyContractExtractor);
            else
                NodeBuilder = new PrimaryContractNodeBuilder(Contract as PrimaryContractExtractor);
   
            ContractCoverNodes = NodeBuilder.GetCoverNodes();
            ContractTermNodes = NodeBuilder.GetTermNodes();

            DGraphBuilder = new DescriptiveGraphBuilder(ContractTermNodes, ContractCoverNodes);
            DGraph = DGraphBuilder.FormDescriptiveGraph();
        }

        public abstract Graph Build(); 
       
    }

    public class AutoGraphOfNodesBuilder : AutoGraphBuilder
    {

        public AutoGraphOfNodesBuilder(ContractExtractor _contract) : base(_contract) { }

        public override Graph Build()
        {
            Console.WriteLine("Graph build start at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));

            #region delete later
            ////Make all graph nodes for Graph
            //List<GraphNode> IntitialGraphNodes = new List<GraphNode>();
            //List<CoverNode> IntitialCoverNodes = new List<CoverNode>();
            //List<GraphNode> tempIntitialCoverNodes = new List<GraphNode>();
            ////temporary variablest to cover GraphNode to CoverNode

            ////Get All Cover Nodes
            //List<Subject> graphSubs = Contract.GetAllCoverSubjects();
            //tempIntitialCoverNodes.AddRange(GetCoverNodes(Contract, graphSubs));
            //foreach (GraphNode node in tempIntitialCoverNodes)
            //{
            //    CoverNode cNode = node as CoverNode;
            //    IntitialCoverNodes.Add(cNode);
            //}

            ////Get All Term Nodes
            //if (!Contract.IsTreaty)
            //{
            //    PrimaryContractExtractor primaryContract = (PrimaryContractExtractor)Contract;
            //    List<PrimarySubject> termgraphSubs = primaryContract.GetAllTermSubjects();
            //    IntitialGraphNodes.AddRange(GetTermNodes(primaryContract, termgraphSubs));
            //}

            ////Do the Node Tree
            //NodeTree nodeTree = new NodeTree(IntitialGraphNodes, IntitialCoverNodes);
            //nodeTree.Run();
            #endregion

            //NodeTree completeGraph = DGraphBuilder.FormCompleteGraph();
            NodeTree completeGraph = DGraphBuilder.CompleteGraph;
            DGraphBuilder.UpdateTermNodes(completeGraph);
            
            //Build Hasse Diagram from Graph Nodes (for TermNodes only)
            HasseDiagramGenerator HasseBuilder = new HasseDiagramGenerator();

            List<GraphNode> InputGraphNodes = completeGraph.FinalTermNodes.Cast<GraphNode>()
                                                           .Union(completeGraph.FinalDerivedCoverNodes.Cast<GraphNode>()).ToList();

            InputGraphNodes = InputGraphNodes.Union(completeGraph.FinalLeafCoverNodes.Cast<GraphNode>()).ToList();

            HasseDiagram HasseDiagram = HasseBuilder.Generate(InputGraphNodes);
            //Convert Hasse Diagram to Reference.Graph object
            List<GraphNode> TopGraphNodes = new List<GraphNode>();
            List<GraphNode> FinalGraphNodes = HasseDiagram.HasseDiagramNodes.Where(pair => pair.Key != "{}")
                                                                            .Select(pair => pair.Value)
                                                                            .Cast<GraphNode>()
                                                                            .ToList();
            Dictionary<GraphNode, List<GraphNode>> ParentToChildrenMap = new Dictionary<GraphNode, List<GraphNode>>();

            foreach (GraphNode graphnode in FinalGraphNodes)
            {
                List<GraphNode> childrenList = graphnode.EdgesToCovered.Select(edge => edge.LowerNode)
                                                                      .Where(node => node.KeyString != "")
                                                                      .Cast<GraphNode>().ToList();
                if (childrenList.Count > 0)
                    ParentToChildrenMap.Add(graphnode, childrenList);

                if (graphnode.EdgesToCovering.Count == 0)
                    TopGraphNodes.Add(graphnode);
            }

            GraphOfNodes graph;
            if (Contract.IsTreaty)
            {
                TreatyContractExtractor treatyContract = Contract as TreatyContractExtractor;
                graph = new TreatyGraph(treatyContract.ID, TopGraphNodes, FinalGraphNodes, ParentToChildrenMap, treatyContract.Declarations, treatyContract.PrimaryDeclarationsSet);
            }
            else
            {
                graph = new PrimaryGraph(Contract.ID, TopGraphNodes, FinalGraphNodes, ParentToChildrenMap, Contract.Declarations);
            }

            //graph.BuildAtomicRITEs();
            //graph.AssignLevelToNode();
            //graph.GraphReady = true;
            Console.WriteLine(" graph end at 2: " + DateTime.Now.ToString("h:mm:ss.fff tt"));
            return graph;
        }
    }

    public class AutoGraphOfMatrixBuilder : AutoGraphBuilder
    {
        private IRITEindexMapper indexMapper;
        public AutoGraphOfMatrixBuilder(ContractExtractor _contract, IRITEindexMapper _indexMapper)
            : base(_contract)
        { indexMapper = _indexMapper; }

        //terms
        Dictionary<int, int[]> allLevelAtomicRiteIndexList { get; set; }
        Dictionary<int, int[]> allLevelAtomicRITEPartitionList { get; set; }
        Dictionary<int, TermNode[]> allLevelNodesList { get; set; }
        Dictionary<int, int[]> allLevelNodePartitionList { get; set; }
        Dictionary<int, TermNode[]> allLevelNodesPerBuildingList { get; set; }
        Dictionary<int, int[]> allLevelBuildingsList { get; set; }
        Dictionary<int, int[]> allLevelNodePerBuildindgPartitionList { get; set; }
        Dictionary<int, int[]> allLevelPartitionList { get; set; }

        //leaf cover
        private HashSet<CoverNode> allLeafCoverNodes { get; set; }
        private List<CoverNode> allLeafCoverNodesPerBuildingList { get; set; }
        private List<int> allLeafCoverNodesBuildingList { get; set; }
        private List<int> allLeafCoverLevelInfo { get; set; }
        private List<int> allLeafCoverRitesInfo { get; set; }
        private List<int> allLeafCoverAriteGULossInfo { get; set; }
        private List<int> allLeafCoverRitesPartition { get; set; }
        private List<int> allLeafCoverResidualRitesInfo { get; set; }
        private List<int> allLeafCoverResidualRitesPartition { get; set; }
        private HashSet<CoverNode> leafAlsoTopCoverList { get; set; }
        private List<int> leafAlsoTopCoverIndexList { get; set; }
        private HashSet<CoverNode> allDerivedCoverNodes { get; set; }
        private Dictionary<int, Tuple<int, int>> aRiteLocation { get; set; }
        private Dictionary<int, int> aRiteLevel { get; set; }

        //derived levels
        private Dictionary<int, List<CoverNode>> allLevelDerivedNodesList { get; set; }
        private Dictionary<int, HashSet<CoverNode>> allLevelDerivedNodes { get; set; }
        private Dictionary<int, List<CoverNode>> allLevelDerivedNodesPerBuildingList { get; set; }
        private Dictionary<int, List<int>> allLevelDerivedNodesBuildingList { get; set; }
        private Dictionary<int, List<int>> allLevelChildrenIndexMap { get; set; }
        private Dictionary<int, List<int>> allLevelChildrenPartitionMap { get; set; }
        private Dictionary<CoverNode, HashSet<CoverNode>> coverGraphCPMap { get; set; }
        private Dictionary<CoverNode, HashSet<CoverNode>> coverGraphPCMap { get; set; }
        private HashSet<CoverNode> topCoverNodes { get; set; }
        private List<CoverNode> coverNodeLongList { get; set; }
        private List<int> coverNodeBuildingLongList { get; set; }
        private Dictionary<int, FunctionType> allLevelAggSubjectFuncType { get; set; }
        private Dictionary<int, FunctionType> allLevelLimitFuncType { get; set; }
        private Dictionary<int, List<float>> allLevelAggSubjectMultiplyFactor { get; set; }
        private Dictionary<int, List<float>> allLevelLimitMultiplyFactor { get; set; }

        //Finacial Terms & Patterns (Term levels)
        private Dictionary<int, bool> allLevelIsMaxDed { get; set; }
        private Dictionary<int, bool> allLevelIsPercentDed { get; set; }
        private Dictionary<int, bool> allLevelIsFranchiseDed { get; set; }

        private Dictionary<int, float[]> allLevelMinDeds { get; set; }
        private Dictionary<int, float[]> allLevelMaxDeds { get; set; }
        private Dictionary<int, float[]> allLevelLimits { get; set; }
        private Dictionary<int, int[]> allLevelMinFranchise { get; set; }
        private Dictionary<int, int[]> allLevelMaxFranchise { get; set; }

        //co-op factors
        //private Dictionary<int, float[]> allLevelNodeFactors { get; set; }
        //private Dictionary<int, float[]> allLevelaRiteFactors { get; set; }
        //private Dictionary<int, List<float>> allCoverLevelFactors { get; set; }
        //private float[] LeafCoveraRiteFactors { get; set; }
        //private float[] LeafCoverResidualRiteFactors { get; set; }

        private bool AtomicRiteAllocatedBySummed { get; set; }

        private Dictionary<int, int[]> allLevelNodeFactorsIndex { get; set; }
        private Dictionary<int, int[]> allLevelaRiteFactorsIndex { get; set; }
        private Dictionary<int, List<int>> allCoverLevelFactorsIndex { get; set; }
        private int[] LeafCoveraRiteFactorsIndex { get; set; }
        private int[] LeafCoverResidualRiteFactorsIndex { get; set; }

        //Financial Terms & Patterns (Cover levels)
        private Dictionary<int, List<float>> allCoverLevelShares { get; set; }
        private Dictionary<int, List<float>> allCoverLevelLimits { get; set; }
        private Dictionary<int, List<float>> allCoverLevelDeds { get; set; }        
        
        public override Graph Build()
        {            
            if (DGraph.TermNodesIsOverlapped)
                throw new InvalidOperationException("TermNodes Overlap cannot be handled in Matrix Executible");

            IExecutableMatrixGraph MatrixGraph = TranslateToMatrixExecutableGraph();
            return new GraphOfMatrix(Contract.ID, MatrixGraph, Contract.Declarations);
        }
        
        public IExecutableMatrixGraph TranslateToMatrixExecutableGraph()
        {
            //Console.WriteLine("start translate " + DateTime.Now.ToString("h:mm:ss.fff tt"));

            IBuildableMatrixGraph graph = new AutoMatrixGraph();
            ContractInfo contractinfo = new ContractInfo(!Contract.Declarations.GroundUpSublimits, Contract.Declarations.MinimumAbsorbingDed);
            graph.SetContractInfo(contractinfo);

            //intermedia output
            allLevelNodesList = new Dictionary<int, TermNode[]>();
            allLevelNodePartitionList = new Dictionary<int, int[]>();

            Dictionary<TermNode, HashSet<TermNode>> TermPCMap = DGraph.TermGraph;
            Dictionary<TermNode, HashSet<TermNode>> TermCPMap = DGraph.TermGraphChildParentsMapping;           
            coverGraphPCMap = DGraph.CoverGraph; //This is allowed to have overlap
            coverGraphCPMap = DGraph.CoverGraphChildParentsMapping;
            allLeafCoverNodes = DGraph.AllLeafCoverNodes;
            allDerivedCoverNodes = DGraph.AllDerivedCoverNodes;

            Dictionary<int, HashSet<TermNode>> nodesByLevel = new Dictionary<int, HashSet<TermNode>>();

            //find the levels
            nodesByLevel = SetRootTermLevel(TermCPMap);

            int numOfTermLevels;

            SetAllLevelTermNodes(nodesByLevel, out numOfTermLevels);

            allLevelAggSubjectFuncType = new Dictionary<int, FunctionType>();
            allLevelLimitFuncType = new Dictionary<int, FunctionType>();
            allLevelAggSubjectMultiplyFactor = new Dictionary<int, List<float>>();
            allLevelLimitMultiplyFactor = new Dictionary<int, List<float>>();

            allCoverLevelDeds = new Dictionary<int, List<float>>();
            allCoverLevelLimits = new Dictionary<int, List<float>>(); ;
            allCoverLevelShares = new Dictionary<int, List<float>>();
            //allCoverLevelFactors = new Dictionary<int, List<float>>();
            allCoverLevelFactorsIndex = new Dictionary<int, List<int>>();
                      
            AtomicRITELevel lowestAtomicRiteLevel;

            if (numOfTermLevels > 0)
            {
                //Console.WriteLine("Set financial terms start " + DateTime.Now.ToString("h:mm:ss.fff tt"));
                SetFinancialTermsByLevel(numOfTermLevels);
                //Console.WriteLine("Set financial terms end " + DateTime.Now.ToString("h:mm:ss.fff tt"));

                //Console.WriteLine("Atomic Rite start " + DateTime.Now.ToString("h:mm:ss.fff tt"));
                SetAtomicRitesByLevel(numOfTermLevels);

                if (IsOverlapByAtomicRites(allLevelAtomicRiteIndexList))
                    throw new InvalidOperationException("Overlap detected by AtomicRITE");

                lowestAtomicRiteLevel = new AtomicRITELevel(allLevelAtomicRITEPartitionList[numOfTermLevels], allLevelAtomicRiteIndexList[numOfTermLevels].Count(),
                                                                           allLevelaRiteFactorsIndex[numOfTermLevels],allLevelAtomicRiteIndexList[numOfTermLevels]);
                graph.SetLowestARITELevel(lowestAtomicRiteLevel);
                //Console.WriteLine("Atomic Rite end " + DateTime.Now.ToString("h:mm:ss.fff tt"));

                for (int i = 0; i < numOfTermLevels; i++)
                {                   
                    TermLevel thisTermLevel = new TermLevel(allLevelMinDeds[i].Length, allLevelAtomicRiteIndexList[i].Length, allLevelIsMaxDed[i], allLevelIsPercentDed[i],
                                                             allLevelMinDeds[i], allLevelMaxDeds[i], allLevelLimits[i], allLevelPartitionList[i],
                                                             allLevelAtomicRITEPartitionList[i], allLevelAtomicRiteIndexList[i], allLevelIsFranchiseDed[i], allLevelMinFranchise[i], allLevelMaxFranchise[i], allLevelNodeFactorsIndex[i], allLevelaRiteFactorsIndex[i]);

                    graph.AddTermLevel(i, thisTermLevel);
                }
            }


            //Console.WriteLine("start cover graph: " + DateTime.Now.ToString(("h:mm:ss.fff tt")));
            #region Used for term only Matrix prototype to handle no term cases
            //else
            //{
            //    numOfLevels = 1;

            //    Subject topCoverSubject = Contract.GetAllCoverSubjects()[0];
            //    HashSet<AtomicRITE> Arites = topCoverSubject.GetAtomicRites();
            //    int NumOfArites = Arites.Count();
            //    int[] partitions = new int[]{NumOfArites};

            //    List<int> thisLevelAtomicRITEsIndex = new List<int>();

            //    for (int ii = 0; ii < NumOfArites; ii++)
            //    {
            //        //thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject, tNode.GetNumOfBuildings()).ToList());
            //        thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(topCoverSubject, ii).ToList());
            //    }

            //    lowestAtomicRiteLevel = new AtomicRITELevel(partitions, NumOfArites, thisLevelAtomicRITEsIndex.ToArray());

            //    graph.SetLowestARITELevel(lowestAtomicRiteLevel);
            //}
            #endregion

            //set leaf cover level
            FormLeafCoverLevel();
            //Console.WriteLine("done leafcover: " + DateTime.Now.ToString(("h:mm:ss.fff tt")));

            //then for upper Derived Cover Levels
            FormDerivedCoverLevels();
            //Console.WriteLine("done derive cover: " + DateTime.Now.ToString(("h:mm:ss.fff tt")));
            int totalCoverLevels = allLevelDerivedNodesBuildingList.Keys.ToList().Max() + 1;

            CoverLevel thisCover;
            LowestCoverLevel lowestCoverLevel;

            lowestCoverLevel = new LowestCoverLevel(allLeafCoverResidualRitesInfo.Count(), allLeafCoverRitesInfo.Count(), allLeafCoverResidualRitesPartition.ToArray(),
                                    allLeafCoverRitesPartition.ToArray(), allLeafCoverResidualRitesInfo.ToArray(), allLeafCoverLevelInfo.ToArray(), allLeafCoverRitesInfo.ToArray(), allLeafCoverAriteGULossInfo.ToArray(), LeafCoveraRiteFactorsIndex, LeafCoverResidualRiteFactorsIndex);

            graph.SetLowestCoverLevel(lowestCoverLevel);

            //for each level for each node, read Share, Limit and Attachement point
            //TODO: this could be done in 
            for (int level = 0; level < totalCoverLevels; level++)
            {
                int numNode = allLevelDerivedNodesPerBuildingList[level].Count();
                //float[] share = new float[numNode];
                //float[] lim = new float[numNode];
                //float[] ded = new float[numNode];

                //for (int i = 0; i < numNode; i++)
                //{
                //    share[i] = (float)allLevelDerivedNodesPerBuildingList[level][i].Cover.ProRata.Amount;
                //    if (allLevelDerivedNodesPerBuildingList[level][i].Cover.Unlimited)
                //        lim[i] = float.MaxValue;
                //    else
                //        lim[i] = (float)allLevelDerivedNodesPerBuildingList[level][i].Cover.LimitAmount;
                //    ded[i] = (float)allLevelDerivedNodesPerBuildingList[level][i].Cover.AttPointAmount;
                //}

                int[] mapPartition;
                int[] childrenMap;
                if (level < totalCoverLevels - 1)
                {
                    mapPartition = allLevelChildrenPartitionMap[level].ToArray();
                    childrenMap = allLevelChildrenIndexMap[level].ToArray();
                }
                else  //top cover level
                {
                    mapPartition = new int[1];
                    mapPartition[0] = numNode;
                    childrenMap = new int[numNode];
                    for (int i = 0; i < numNode; i++)
                    {
                        int k = coverNodeLongList.Count() - (numNode - i);
                        childrenMap[i] = k;
                    }                    
                }
                if (level == 0)
                {
                    thisCover = new CoverLevel(allLevelLimitFuncType[level], allLevelLimitMultiplyFactor[level].ToArray(), numNode, allCoverLevelShares[level].ToArray(), allCoverLevelLimits[level].ToArray(), allCoverLevelDeds[level].ToArray(), mapPartition, childrenMap, allCoverLevelFactorsIndex[level].ToArray(), leafAlsoTopCoverIndexList.ToArray());
                    graph.AddLeafCoverLevel(numOfTermLevels + 2 + level, thisCover);
                }
                else
                {
                    thisCover = new DerivedCoverLevel(allLevelAggSubjectFuncType[level], allLevelLimitFuncType[level], allLevelAggSubjectMultiplyFactor[level].ToArray(), allLevelLimitMultiplyFactor[level].ToArray(), numNode, allCoverLevelShares[level].ToArray(), allCoverLevelLimits[level].ToArray(), allCoverLevelDeds[level].ToArray(), mapPartition, childrenMap, allCoverLevelFactorsIndex[level].ToArray());
                    graph.AddDerivedCoverLevel(numOfTermLevels + 2 + level, (DerivedCoverLevel)thisCover);
                }
            }

            //Console.WriteLine(" graph end at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));

            return (IExecutableMatrixGraph)graph;
        }

        public void SetAllLevelTermNodes(Dictionary<int, HashSet<TermNode>> nodesByLevel, out int numOfTermLevels)
        {
            Dictionary<TermNode, HashSet<TermNode>> TermPCMap = DGraph.TermGraph;
            numOfTermLevels = 0;
            int numOfChildren = 0;

            if (nodesByLevel.Keys.Count() != 0)
            {
                numOfTermLevels++;
                TermNode[] thisLevelNodeArr = nodesByLevel[0].ToArray();
                //new TermNode[nodesByLevel[0].Count()];
                int[] levelNodePartition = new int[1];
                levelNodePartition[0] = thisLevelNodeArr.Count();

                allLevelNodesList.Add(0, thisLevelNodeArr);
                allLevelNodePartitionList.Add(0, levelNodePartition);

                TermNode[] preLevelNodeArr = thisLevelNodeArr;
                int preLevelSize = preLevelNodeArr.Length;

                for (int i = 0; i < preLevelSize; i++)
                {
                    if (TermPCMap.ContainsKey(preLevelNodeArr[i]))
                        numOfChildren++;
                }

                //for each lower level      
                //for (int i = 1; i < numOfNodeLevels; i++)
                while (numOfChildren > 0)
                {
                    numOfChildren = 0;
                    numOfTermLevels++;

                    levelNodePartition = new int[preLevelSize];

                    List<TermNode> thisLevelNodes = new List<TermNode>();

                    for (int k = 0; k < preLevelSize; k++) //for each of the parent node
                    {
                        TermNode pNode = preLevelNodeArr[k];
                        if (TermPCMap.ContainsKey(pNode))
                        {
                            thisLevelNodes.AddRange(TermPCMap[pNode].ToList());
                        }
                        levelNodePartition[k] = thisLevelNodes.Count();
                    }
                    allLevelNodesList.Add(numOfTermLevels - 1, thisLevelNodes.ToArray());
                    allLevelNodePartitionList.Add(numOfTermLevels - 1, levelNodePartition);

                    preLevelNodeArr = allLevelNodesList[numOfTermLevels - 1];
                    preLevelSize = preLevelNodeArr.Length;
                    for (int i = 0; i < preLevelSize; i++)
                    {
                        if (TermPCMap.ContainsKey(preLevelNodeArr[i]))
                            numOfChildren++;
                    }
                }  //done with TermLevelInfor                    
            }

            //TODO: temporary solution
            if (allLevelNodesList.Count > 0)
            {
                foreach (TermNode tNode in allLevelNodesList[0])
                {
                    AtomicRiteAllocatedBySummed = true;
                    if (tNode.IsPerRisk)
                        AtomicRiteAllocatedBySummed = false;
                }
            }
            else //if no terms, then Rite is not summed (actually there should not have aRite)
            {
                AtomicRiteAllocatedBySummed = false;
            }
        }

        public void SetFinancialTermsByLevel(int numOfTermLevels)
        {
            ExpandNodesForMultiBuildings();
            //Console.WriteLine("end at expand for building " + DateTime.Now.ToString("h:mm:ss.fff tt"));

            //these are all output (kept locally), will write to graph object (global variable) at the end
            allLevelPartitionList = new Dictionary<int, int[]>();

            allLevelAtomicRiteIndexList = new Dictionary<int, int[]>();
            allLevelAtomicRITEPartitionList = new Dictionary<int, int[]>();
            //allLevelaRiteFactors = new Dictionary<int, float[]>();
            allLevelaRiteFactorsIndex = new Dictionary<int, int[]>();

            allLevelIsMaxDed = new Dictionary<int, bool>();
            allLevelIsPercentDed = new Dictionary<int, bool>();
            allLevelIsFranchiseDed = new Dictionary<int, bool>();

            allLevelMinDeds = new Dictionary<int, float[]>();
            allLevelMaxDeds = new Dictionary<int, float[]>();
            allLevelLimits = new Dictionary<int, float[]>();
            allLevelMinFranchise = new Dictionary<int, int[]>();
            allLevelMaxFranchise = new Dictionary<int, int[]>();

            //allLevelFactors = new Dictionary<int, float[]>();
            //allLevelApplyFactor = new Dictionary<int, FactorPattern>();

            //Console.WriteLine("graph start at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));

            //find the levels
            //int numOfTermLevels = allLevelNodesList.Keys.ToArray().Max() + 1;

            //Top level: Level 0
            bool isMaxDed = false;
            bool isPercentDed = false;
            bool isFranchiseDed = false;

            List<float> minDedList = new List<float>();
            List<float> maxDedList = new List<float>();
            List<float> limitList = new List<float>();
            List<int> minFranchiseList = new List<int>();
            List<int> maxFranchiseList = new List<int>();

            List<int> thislevelPartitionList = new List<int>();
            List<float> factorList = new List<float>();
            List<int> factorIndexList = new List<int>();

            float minDed;
            float maxDed;
            bool minFranchised;
            bool maxFranchised;
            bool maxDedFlag;
            FactorPattern applyFactor;
            int level = 0;
            int n = 0;

            for (level = 0; level < numOfTermLevels; level++)
            {
                isMaxDed = false;
                isPercentDed = false;
                isFranchiseDed = false;
                applyFactor = FactorPattern.AllOnes;

                minDedList = new List<float>();
                maxDedList = new List<float>();
                limitList = new List<float>();
                minFranchiseList = new List<int>();
                maxFranchiseList = new List<int>();
                factorIndexList = new List<int>();
                factorList = new List<float>();

                int tempCC = allLevelNodesPerBuildingList[level].Count();
                for (n = 0; n < tempCC; n++)
                {
                    TermNode tNode = allLevelNodesPerBuildingList[level][n];
                    int bNum = allLevelBuildingsList[level][n];

                    //int origNumBuildings = tNode.GetNumOfBuildingsOrigin();
                    //double[] multiArr = GenerateMultiplierArr(origNumBuildings);

                    tNode.Deductibles.GetMaxDed(out maxDed, out maxDedFlag, out maxFranchised);
                    tNode.Deductibles.GetMinDed(out minDed, out minFranchised);

                    if (maxDedFlag)
                        isMaxDed = true;
                    if (maxFranchised == true || minFranchised == true)
                        isFranchiseDed = true;

                    //minDedList.Add(minDed * (float)multiArr[bNum]);
                    //maxDedList.Add(maxDed * (float)multiArr[bNum]);

                    minDedList.Add(minDed);
                    maxDedList.Add(maxDed);

                    if (minFranchised)
                        minFranchiseList.Add(0);
                    else
                        minFranchiseList.Add(1);

                    if (maxFranchised)
                        maxFranchiseList.Add(0);
                    else
                        maxFranchiseList.Add(1);

                    //limitList.Add((float)tNode.Limits.GetLimit() * (float)multiArr[bNum]);
                    limitList.Add((float)tNode.Limits.GetLimit());

                    //int tempFactorIndex = tNode.GetFactorIndex();
                    //factorIndexList.Add(tempFactorIndex);
   
                }

                allLevelPartitionList.Add(level, allLevelNodePerBuildindgPartitionList[level]);
                allLevelIsMaxDed.Add(level, isMaxDed);
                allLevelIsPercentDed.Add(level, isPercentDed);
                allLevelIsFranchiseDed.Add(level, isFranchiseDed);
                allLevelMinDeds.Add(level, minDedList.ToArray());
                allLevelMaxDeds.Add(level, maxDedList.ToArray());
                allLevelMaxFranchise.Add(level, maxFranchiseList.ToArray());
                allLevelMinFranchise.Add(level, minFranchiseList.ToArray());
                allLevelLimits.Add(level, limitList.ToArray());
               // allLevelFactors.Add(level, factorList.ToArray());
                //allLevelApplyFactor.Add(level, applyFactor);
            }
        }

        public void SetAtomicRitesByLevel(int numOfTermLevels)
        {
                //then add AtomicRites, start from lowest level
                //this lowest level is (numOfTermLevel + 1) and are all AtomicRiteLevel            
                List<int> thisLevelAtomicRITEsIndex = new List<int>();
                List<int> thisLevelAtomicRitePartitionList = new List<int>();
            
                List<float> thisLevelaRiteFactorList = new List<float>();
                List<int> thisLevelaRiteFactorIndexList = new List<int>();   

                int level = numOfTermLevels;  //bottom level, the level without any nodes 
                int tempC = allLevelNodesPerBuildingList[level - 1].Count(); //parent level node count, if the parent is perRisk, then it is node per building
                Dictionary<int, float> tempFactorList = new Dictionary<int, float>();
                Dictionary<int, float> totalTempFactorList = new Dictionary<int, float>();
                Dictionary<int, int> tempFactorIndexList = new Dictionary<int, int>();
                Dictionary<int, int> totalTempFactorIndexList = new Dictionary<int, int>();

                for (int n = 0; n < tempC; n++) //for each parent node
                {                   
                    TermNode tNode = allLevelNodesPerBuildingList[level - 1][n]; //parent node
                    if (tNode.IsPerRisk) //parent node is PerRisk, expand for each building
                    {
                        thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(tNode.Subject, allLevelBuildingsList[level - 1][n], out tempFactorIndexList).ToList());                        
                        thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
                        //thisLevelaRiteFactorList.AddRange(Enumerable.Repeat(1, tempFactorList.Keys.Count).ToList().Select(x => (float)x)); //parent node is perRisk, then the factor = 1.
                        thisLevelaRiteFactorIndexList.AddRange(Enumerable.Repeat(-1, tempFactorIndexList.Keys.Count).ToList().Select(x => (int)x)); //parent node is perRisk, then the factor = 1.
                    }  //perent node is perRisk
                    else
                    {                        
                        //int tempB = tNode.GetNumOfBuildingsActual();
                        //for (int ii = 0; ii < tempB; ii++)
                        //{
                        //    //thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject, tNode.GetNumOfBuildings()).ToList());                           
                        //    //thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(tNode.Subject, ii, out tempFactorIndexList).ToList());
                        //    //thisLevelaRiteFactorList.AddRange(tempFactorList.Values.ToList());
                        //    thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(tNode.Subject, ii, out tempFactorIndexList).ToList());
                        //    thisLevelaRiteFactorIndexList.AddRange(tempFactorIndexList.Values.ToList());
                        //}
                        //thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());

                        //bug fix, loop through each cRite and each of its building
                        HashSet<AtomicRITE> tempSet = tNode.Subject.GetAtomicRites();
                        foreach (AtomicRITE aRite in tempSet)
                        {
                            CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
                            int numBldgs = cRite.RITE.ActNumOfSampleBldgs;
                            for (int ii = 0; ii < numBldgs; ii++)
                            {
                                thisLevelAtomicRITEsIndex.Add(GetAtomicRitesIndexForSubject(cRite, ii, out tempFactorIndexList));
                                thisLevelaRiteFactorIndexList.AddRange(tempFactorIndexList.Values.ToList());
                            }
                        }
                        thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());    
                    }
                }
                allLevelAtomicRiteIndexList.Add(numOfTermLevels, thisLevelAtomicRITEsIndex.ToArray());
                allLevelAtomicRITEPartitionList.Add(numOfTermLevels, thisLevelAtomicRitePartitionList.ToArray());
                //allLevelaRiteFactors.Add(numOfTermLevels, thisLevelaRiteFactorList.ToArray());
                allLevelaRiteFactorsIndex.Add(numOfTermLevels, thisLevelaRiteFactorIndexList.ToArray());

                //Console.WriteLine(" atomic end lowest level at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));
                //then for each upper level, for each parent, need to find residual AtomicRites
                //top term level, no AtomicRite array
                for (level = numOfTermLevels - 1; level > 0; level--)
                {                                        
                    thisLevelAtomicRITEsIndex = new List<int>();
                    thisLevelAtomicRitePartitionList = new List<int>();
                    thisLevelaRiteFactorList = new List<float>();
                    thisLevelaRiteFactorIndexList = new List<int>(); 

                    int tempHolder = 0;
                    int tempCC = allLevelNodesPerBuildingList[level - 1].Count(); //parent count
                    for (int n = 0; n < tempCC; n++) //for each parent node
                    {
                        TermNode tNode = allLevelNodesPerBuildingList[level - 1][n]; //parent Node
                        totalTempFactorList = new Dictionary<int, float>();
                        totalTempFactorIndexList = new Dictionary<int, int>();

                        if (tNode.IsPerRisk)  //Parent node is PerRisk, so each node is perBuilding
                        {
                            HashSet<int> allRites = GetAtomicRitesIndexForSubject(tNode.Subject, allLevelBuildingsList[level - 1][n], out tempFactorIndexList);
                            //for a node, its AtomicRites' factors are the same, we need keep a dictionary to keep the atomicRite-factor relation.

                            HashSet<int> childrenRites = new HashSet<int>();
                            int tempP = allLevelNodePerBuildindgPartitionList[level][n] - 1;

                            for (int k = tempHolder; k <= tempP; k++)
                            {
                                childrenRites.UnionWith(GetAtomicRitesIndexForSubject(allLevelNodesPerBuildingList[level][k].Subject, allLevelBuildingsList[level][k], out tempFactorIndexList));
                            }

                            //find Residual atomicRite
                            allRites.ExceptWith(childrenRites);
                            thisLevelAtomicRITEsIndex.AddRange(allRites.ToList());                           
                            thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
                            //thisLevelaRiteFactorList.AddRange(Enumerable.Repeat(1, allRites.Count).ToList().Select(x => (float)x)); //parent node is perRisk, then the factor = 1.
                            thisLevelaRiteFactorIndexList.AddRange(Enumerable.Repeat(-1, allRites.Count).ToList().Select(x => (int)x)); //parent node is perRisk, then the factor = 1.

                            tempHolder = allLevelNodePerBuildindgPartitionList[level][n];
                        }
                        else //if (!tNode.IsPerRisk)
                        {
                            //tempHolder = 0;
                            HashSet<int> allRites = new HashSet<int>();

                            //do special
                            //int tempBB = tNode.GetNumOfBuildingsActual();
                            //for (int ii = 0; ii < tempBB; ii++)
                            //{
                            //    allRites.UnionWith(GetAtomicRitesIndexForSubject(tNode.Subject, ii, out tempFactorIndexList));
                            //    totalTempFactorIndexList = totalTempFactorIndexList.Union(tempFactorIndexList).ToDictionary(pair => pair.Key, pair => pair.Value);
                            //}

                            //bug fix: to replace the top, loop through each cRite and each of its building
                            //do special
                            HashSet<AtomicRITE> tempSet = tNode.Subject.GetAtomicRites();
                            foreach (AtomicRITE aRite in tempSet)
                            {
                                CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
                                int numBldgs = cRite.RITE.ActNumOfSampleBldgs;
                                for (int ii = 0; ii < numBldgs; ii++)
                                {
                                    allRites.Add(GetAtomicRitesIndexForSubject(cRite, ii, out tempFactorIndexList));
                                    totalTempFactorIndexList = totalTempFactorIndexList.Union(tempFactorIndexList).ToDictionary(pair => pair.Key, pair => pair.Value);
                                }
                            }
                            //----------------------------------------------------

                            //HashSet<int> childrenRites = new HashSet<int>();
                            //int tempPP = allLevelNodePartitionList[level][n] - 1;
                            //for (int k = tempHolder; k <= tempPP; k++)
                            //{
                            //    tempBB = allLevelNodesList[level][k].GetNumOfBuildingsActual();
                            //    for (int ii = 0; ii < tempBB; ii++)
                            //    {
                            //        childrenRites.UnionWith(GetAtomicRitesIndexForSubject(allLevelNodesList[level][k].Subject, ii, out tempFactorIndexList));
                            //    }
                            //}

                            //bug fix: to replace the top, loop through each cRite and each of its building
                            HashSet<int> childrenRites = new HashSet<int>();
                            int tempPP = allLevelNodePartitionList[level][n] - 1;
                            for (int k = tempHolder; k <= tempPP; k++)
                            {                            
                                tNode = allLevelNodesList[level][k];
                                tempSet = tNode.Subject.GetAtomicRites();
                                foreach (AtomicRITE aRite in tempSet)
                                {
                                    CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
                                    int numBldgs = cRite.RITE.ActNumOfSampleBldgs;
                                    for (int ii = 0; ii < numBldgs; ii++)
                                    {
                                        childrenRites.Add(GetAtomicRitesIndexForSubject(cRite, ii, out tempFactorIndexList));                                        
                                    }
                                } 
                            }
                            //-----------------------------------------------------------------------------
                            
                            
                            //find Residual atomicRite                                                          
                            allRites.ExceptWith(childrenRites);
                            thisLevelAtomicRITEsIndex.AddRange(allRites.ToList());
                            thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
                            //List<float> factorList = new List<float>();
                            List<int> factorIndexList = new List<int>();

                            foreach (int rIndex in allRites)
                            {
                                factorIndexList.Add(totalTempFactorIndexList[rIndex]);
                            }
                            thisLevelaRiteFactorIndexList.AddRange(factorIndexList);
                            tempHolder = allLevelNodePartitionList[level][n];
                        }
                    }
                    allLevelAtomicRiteIndexList.Add(level, thisLevelAtomicRITEsIndex.ToArray());
                    allLevelAtomicRITEPartitionList.Add(level, thisLevelAtomicRitePartitionList.ToArray());                    
                    allLevelaRiteFactorsIndex.Add(level, thisLevelaRiteFactorIndexList.ToArray());                    
                }


                //level 0: the top level should have no AtomicRiteLevel
                //TODO: we need default empty Level infor.
                allLevelAtomicRiteIndexList.Add(0, new List<int>().ToArray());
                allLevelAtomicRITEPartitionList.Add(0, new int[1]);
                allLevelaRiteFactorsIndex.Add(0, new List<int>().ToArray());
        }
       
        public void ExpandNodesForMultiBuildings()       
        {
            allLevelNodesPerBuildingList = new Dictionary<int, TermNode[]>();
            allLevelBuildingsList = new Dictionary<int, int[]>();
            allLevelNodePerBuildindgPartitionList = new Dictionary<int, int[]>();
            List<TermNode> thisLevelNodesPerBuildingList = new List<TermNode>();
            List<int> thisLevelBuildingsList = new List<int>(); ;
            List<int> thisLevelNodePerBuildindgPartitionList = new List<int>();

            //allLevelNodeFactors = new Dictionary<int, float[]>();
            //List<float> thisLevelFactors = new List<float>();

            allLevelNodeFactorsIndex = new Dictionary<int, int[]>();
            List<int> thisLevelFactorsIndex = new List<int>();

            int numOfTermLevels = allLevelNodesList.Keys.ToArray().Max() + 1;
            TermNode[] topNodes = allLevelNodesList[0];

            int level = 0;  //level index
            int b = 0;  //building index
            int n = 0; //node index

            //first to form parent-child relation
            Dictionary<int, Dictionary<TermNode, List<TermNode>>> allLevelPCMap = new Dictionary<int, Dictionary<TermNode, List<TermNode>>>();
            for (level = 0; level < numOfTermLevels - 1; level++)
            {
                TermNode[] temp;
                Dictionary<TermNode, List<TermNode>> tempHolder = new Dictionary<TermNode, List<TermNode>>();
                int tempL = allLevelNodesList[level].Length;
                for (n = 0; n < tempL; n++)
                {
                    if (n == 0)
                    {
                        temp = new TermNode[allLevelNodePartitionList[level + 1][n]];
                        Array.Copy(allLevelNodesList[level + 1], 0, temp, 0, allLevelNodePartitionList[level + 1][n]);
                    }
                    else
                    {
                        temp = new TermNode[allLevelNodePartitionList[level + 1][n] - allLevelNodePartitionList[level + 1][n - 1]];
                        Array.Copy(allLevelNodesList[level + 1], allLevelNodePartitionList[level + 1][n - 1], temp, 0, allLevelNodePartitionList[level + 1][n] - allLevelNodePartitionList[level + 1][n - 1]);
                    }

                    tempHolder.Add((TermNode)allLevelNodesList[level][n], temp.ToList());
                }
                allLevelPCMap.Add(level, tempHolder);
            }

            //top level first, slightly different than other lower levels
            //GetNumOfBuildings has already cooperate with PerRisk flag
            level = 0;    
            int tempLL = allLevelNodesList[level].Length;
            for (n = 0; n < tempLL; n++)
            {
                TermNode tNode = (TermNode)allLevelNodesList[level][n];
                int tempB = tNode.GetNumOfBuildings();                                         

                for (b = 0; b < tempB; b++)
                {
                    thisLevelNodesPerBuildingList.Add(tNode);
                    thisLevelBuildingsList.Add(b);           
                    if (tNode.IsPerRisk)        
                        //thisLevelFactorsIndex.Add(indexMapper.GetMappedFactorIndex(tNode.GetFirstRITEID(), b));  //top level node (no parent, so always assume parent is Summed term)
                        thisLevelFactorsIndex.Add(indexMapper.GetMappedFactorIndex(tNode.GetFirstRITEcharID(), b));  //top level node (no parent, so always assume parent is Summed term)
                    else
                        thisLevelFactorsIndex.Add(-1); 
                }
            }
            thisLevelNodePerBuildindgPartitionList.Add(thisLevelNodesPerBuildingList.Count());

            allLevelNodesPerBuildingList.Add(level, thisLevelNodesPerBuildingList.ToArray());
            allLevelBuildingsList.Add(level, thisLevelBuildingsList.ToArray());
            allLevelNodePerBuildindgPartitionList.Add(level, thisLevelNodePerBuildindgPartitionList.ToArray());
            allLevelNodeFactorsIndex.Add(level, thisLevelFactorsIndex.ToArray());           
      
            for (level = 1; level < numOfTermLevels; level++)
            {
                thisLevelNodesPerBuildingList = new List<TermNode>();
                thisLevelBuildingsList = new List<int>(); ;
                thisLevelNodePerBuildindgPartitionList = new List<int>();
                thisLevelFactorsIndex = new List<int>();

                long riteID = -1;

                int preLevel = level - 1;
                int tempLLL = allLevelNodesPerBuildingList[preLevel].Length;
                for (n = 0; n < tempLLL; n++)
                {
                    TermNode ptNode = (TermNode)allLevelNodesPerBuildingList[preLevel][n];
                    List<TermNode> cNodeList = allLevelPCMap[preLevel][ptNode];

                    if (ptNode.IsPerRisk)
                    {
                        foreach (TermNode cNode in cNodeList)
                        {
                            thisLevelNodesPerBuildingList.Add(cNode);
                            thisLevelBuildingsList.Add(allLevelBuildingsList[preLevel][n]);
                            thisLevelFactorsIndex.Add(-1);  //parentNode is PerRisk, the child must be perRisk, so the factor is one, which is factor index = -1
                        }
                        thisLevelNodePerBuildindgPartitionList.Add(thisLevelNodesPerBuildingList.Count());
                        //TODO: need to make sure that # of buildings in PerRisk parent is always the same tis children's # of building
                        //otherwise, throw exception
                    } //PerRisk parent
                    else
                    {
                        foreach (TermNode cNode in cNodeList)
                        {
                            if (cNode.IsPerRisk)
                                riteID = cNode.GetFirstRITEcharID(); //parentNode is Summed, childNode is PerRisk, we need the real factor
                            else
                                riteID = -1; //parentNode is summed, childNode is Summed as well, factor = 1

                            int tempBBB = cNode.GetNumOfBuildings();
                            for (b = 0; b < tempBBB; b++)
                            {
                                thisLevelNodesPerBuildingList.Add(cNode);
                                thisLevelBuildingsList.Add(b);
                                if (riteID == -1)
                                    thisLevelFactorsIndex.Add(-1);
                                else
                                    thisLevelFactorsIndex.Add(indexMapper.GetMappedFactorIndex(riteID, b));
                            }
                        }
                        thisLevelNodePerBuildindgPartitionList.Add(thisLevelNodesPerBuildingList.Count());
                    } //summed parent
                }
                allLevelNodesPerBuildingList.Add(level, thisLevelNodesPerBuildingList.ToArray());
                allLevelBuildingsList.Add(level, thisLevelBuildingsList.ToArray());
                allLevelNodePerBuildindgPartitionList.Add(level, thisLevelNodePerBuildindgPartitionList.ToArray());
                allLevelNodeFactorsIndex.Add(level, thisLevelFactorsIndex.ToArray());               
            }
        }

        public void FormLeafCoverLevel()
        {                  
            allLeafCoverNodesPerBuildingList = new List<CoverNode>();
            allLeafCoverNodesBuildingList = new List<int>();

            allLeafCoverLevelInfo = new List<int>();
            allLeafCoverRitesInfo = new List<int>();
            allLeafCoverAriteGULossInfo = new List<int>();
            allLeafCoverRitesPartition = new List<int>();

            allLeafCoverResidualRitesInfo = new List<int>();
            allLeafCoverResidualRitesPartition = new List<int>();
            int numLevels = 0;

            coverNodeLongList = new List<CoverNode>();
            coverNodeBuildingLongList = new List<int>();

            //To handle no terms case:
            if (allLevelAtomicRiteIndexList != null)
            {
                numLevels = allLevelAtomicRiteIndexList.Keys.ToArray().Max() + 1;
            }

            int currLevel = 0;
            allLevelLimitMultiplyFactor.Add(currLevel, new List<float>());
            allCoverLevelDeds.Add(currLevel, new List<float>());
            allCoverLevelLimits.Add(currLevel, new List<float>());
            allCoverLevelShares.Add(currLevel, new List<float>());
            List<int> LeafCoveraRiteFactorsIndexList = new List<int>();
            List<int> LeafCoverResidualRiteFactorsIndexList = new List<int>();

            foreach (CoverNode cNode in allLeafCoverNodes)
            {                
                PrimarySubject s = cNode.Subject as PrimarySubject;
                
                HashSet<AtomicRITE> aRiteList;
                int numBuildings = 0;

                HashSet<int> allRites;
                bool found = false;

                //parse limit functions, this is the same for all buildings, so move it out of the building block
                float limMulFactor = 1;
                //if (cNode.Cover.LimitValType == TermValueType.PayFunction)
                //{
                if (cNode.Cover.Limit is FunctionValue)
                {
                    FunctionValue limFun = cNode.Cover.Limit as FunctionValue;
                    if (limFun.Function == FunctionType.Min && limFun.Arguments.Count == 2)
                    {
                        if (limFun.Arguments[0] is SymbolicValue && limFun.Arguments[1] is MonetaryValue)
                        {
                            SymbolicValue temp0 = limFun.Arguments[0] as SymbolicValue;
                            MonetaryValue temp1 = limFun.Arguments[1] as MonetaryValue;
                            if (temp0.Symbol == "Subject")
                                cNode.Cover.Limit = new MonetaryValue(temp1.Amount);
                            else
                                throw new InvalidOperationException("Invalid Limit function type");
                        }
                        else if (limFun.Arguments[1] is SymbolicValue && limFun.Arguments[0] is MonetaryValue)
                        {
                            SymbolicValue temp0 = limFun.Arguments[1] as SymbolicValue;
                            MonetaryValue temp1 = limFun.Arguments[0] as MonetaryValue;
                            if (temp0.Symbol == "Subject")
                                cNode.Cover.Limit = new MonetaryValue(temp1.Amount);
                            else
                                throw new InvalidOperationException("Invalid Limit function type");
                        }

                        else
                            throw new InvalidOperationException("Invalid Limit function type");

                        //allLevelLimitMultiplyFactor[currLevel].Add(0);
                        limMulFactor = 0;
                    }
                    else if (limFun.Function == FunctionType.Constant && limFun.Arguments.Count == 1)
                    {
                        if (limFun.Arguments[0] is MonetaryValue)
                        {
                            MonetaryValue temp1 = limFun.Arguments[0] as MonetaryValue;
                            cNode.Cover.Limit = new MonetaryValue(temp1.Amount);
                        }
                        else
                            throw new InvalidOperationException("Invalid Limit function type");

                        //allLevelLimitMultiplyFactor[currLevel].Add((float)0.5);
                        limMulFactor = (float)0.5;
                    }
                } //FunctionValue                                
                //}     

                //read out share, limit and attachement
                float share = (float)cNode.Cover.ProRata.Amount;
                float lim = 0;                
                if (cNode.Cover.Unlimited)                        
                    lim = float.MaxValue;                    
                else                
                    lim = (float)cNode.Cover.LimitAmount;
                float ded = (float)cNode.Cover.AttPointAmount;
                //float factor = cNode.GetFactor();
                //if (factor != 1 && !cNode.IsPerRisk)
                //    applyFactor = FactorPattern.AllOnes;
                
                //Certainly, PerRisk and nonPerRisk scenarios can be combined to reduce the amount of codes, 
                //but for clarity, just separate them for easy reading
                if (cNode.IsPerRisk) //this should be already exploded per Rite
                {
                    aRiteList = s.GetAtomicRites();  //assume all RITE here should have same number of buildings                  
                    numBuildings = ((CoverageAtomicRITE)aRiteList.First()).RITE.ActNumOfSampleBldgs;
                    
                    //need to expand for perBuilding
                    for (int building = 0; building < numBuildings; building++)
                    {
                        allLeafCoverNodesPerBuildingList.Add(cNode);
                        allLeafCoverNodesBuildingList.Add(building);
                        allLevelLimitMultiplyFactor[currLevel].Add(limMulFactor);
                        allCoverLevelDeds[currLevel].Add(ded);
                        allCoverLevelLimits[currLevel].Add(lim);
                        allCoverLevelShares[currLevel].Add(share);    

                        foreach (AtomicRITE aRite in aRiteList)
                        {
                            found = false;
                            CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
                            //numBuildings = cRite.RITE.ActNumOfBldgs;                        

                            //find rites info
                            int riteIndex = GetAtomicRitesIndexForSubject(cRite, building);
                     
                            //allRites.Add(riteIndex);

                            //loop through all atomic levels, level 0 does not have RITEs
                            //if (allLevelAtomicRiteIndexList.Keys != null)
                            //{
                            for (int level = 1; level < numLevels; level++)
                            {
                                int temp = -1;
                                temp = Array.IndexOf(allLevelAtomicRiteIndexList[level], riteIndex);
                                if (temp >= 0)
                                {
                                    allLeafCoverLevelInfo.Add(level);
                                    allLeafCoverRitesInfo.Add(temp);
                                    LeafCoveraRiteFactorsIndexList.Add(-1); //parent is PerRisk, so factor = 1
                                    found = true;
                                    goto Next;
                                }
                            }
                        Next:
                            if (found == false)
                            {
                                allLeafCoverResidualRitesInfo.Add(riteIndex);
                                LeafCoverResidualRiteFactorsIndexList.Add(-1); //parent is PerRisk, so factor = 1
                            }
                            else if (found == true)
                            {
                                allLeafCoverAriteGULossInfo.Add(riteIndex);
                            }
                            //}
                        }//for each Rite
                        allLeafCoverRitesPartition.Add(allLeafCoverRitesInfo.Count());
                        allLeafCoverResidualRitesPartition.Add(allLeafCoverResidualRitesInfo.Count());
                    }//for each building
                } //cNode PerRisk
                else
                {
                    allRites = new HashSet<int>();

                    allLeafCoverNodesPerBuildingList.Add(cNode);
                    allLeafCoverNodesBuildingList.Add(0);
                    allLevelLimitMultiplyFactor[currLevel].Add(limMulFactor);
                    allCoverLevelDeds[currLevel].Add(ded);
                    allCoverLevelLimits[currLevel].Add(lim);
                    allCoverLevelShares[currLevel].Add(share);
                    //allCoverLevelFactors[currLevel].Add(factor);

                    //need to expand for perBuilding                                   
                    aRiteList = s.GetAtomicRites();
                    foreach (AtomicRITE aRite in aRiteList)
                    {
                        CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
                        numBuildings = cRite.RITE.ActNumOfSampleBldgs;
                        for (int building = 0; building < numBuildings; building++)
                        {
                            found = false;
                            //find rites info
                            int riteIndex = GetAtomicRitesIndexForSubject(cRite, building);
                      
                            //int riteFactorIndex = indexMapper.GetMappedFactorIndex(cRite.RITE.ExposureID, building);
                            int riteFactorIndex = indexMapper.GetMappedFactorIndex(cRite.RITCharacterisiticID, building);  //TODO:
                            //loop through all atomic levels, level 0 does not have RITEs
                            for (int level = 1; level < numLevels; level++)
                            {
                                int temp = -1;
                                temp = Array.IndexOf(allLevelAtomicRiteIndexList[level], riteIndex);

                                if (temp >= 0)
                                {
                                    allLeafCoverLevelInfo.Add(level);
                                    allLeafCoverRitesInfo.Add(temp);
                                    if (AtomicRiteAllocatedBySummed)  //TODO: temporary solution
                                        LeafCoveraRiteFactorsIndexList.Add(-1);
                                    else 
                                        LeafCoveraRiteFactorsIndexList.Add(riteFactorIndex);

                                    found = true;
                                    goto Next2;
                                }                        
                            }

                        Next2:
                            if (found == false)
                            {
                                allLeafCoverResidualRitesInfo.Add(riteIndex);
                                LeafCoverResidualRiteFactorsIndexList.Add(riteFactorIndex);
                            }
                            else if (found == true)
                            {
                                allLeafCoverAriteGULossInfo.Add(riteIndex);
                            }
                        } //for each building                      
                    } //foreach AtomicRite
                    allLeafCoverRitesPartition.Add(allLeafCoverRitesInfo.Count());
                    allLeafCoverResidualRitesPartition.Add(allLeafCoverResidualRitesInfo.Count());
                } //cNode nonPerRisk               
            } //foreach cNode

            List<float> limitFactor = allLevelLimitMultiplyFactor[currLevel].Distinct().ToList();
            if (limitFactor.Count() > 1)
                allLevelLimitFuncType.Add(currLevel, FunctionType.Mix);
            else if (limitFactor[0] == 1)
                allLevelLimitFuncType.Add(currLevel, FunctionType.Regular);
            else if (limitFactor[0] == 0)
                allLevelLimitFuncType.Add(currLevel, FunctionType.Min);
            else if (limitFactor[0] == 0.5)
                allLevelLimitFuncType.Add(currLevel, FunctionType.Constant);

            coverNodeLongList.AddRange(allLeafCoverNodesPerBuildingList);
            coverNodeBuildingLongList.AddRange(allLeafCoverNodesBuildingList);

            LeafCoveraRiteFactorsIndex = LeafCoveraRiteFactorsIndexList.ToArray();
            LeafCoverResidualRiteFactorsIndex = LeafCoverResidualRiteFactorsIndexList.ToArray();

            //allLevelApplyFactor.Add(currLevel, applyFactor);
        }

        public void FormDerivedCoverLevels()
        {
            int level = 0; //leaf level is 0, will add upon the highest term level later

            //form levels and nodes for each level            
            allLevelDerivedNodesList = new Dictionary<int, List<CoverNode>>();
            allLevelDerivedNodes = new Dictionary<int, HashSet<CoverNode>>();
            allLevelDerivedNodesPerBuildingList = new Dictionary<int, List<CoverNode>>();
            allLevelDerivedNodesBuildingList = new Dictionary<int, List<int>>();
            allLevelChildrenIndexMap = new Dictionary<int, List<int>>();
            allLevelChildrenPartitionMap = new Dictionary<int, List<int>>();

            allLevelDerivedNodes.Add(level, allLeafCoverNodes);
            allLevelDerivedNodesPerBuildingList.Add(level, allLeafCoverNodesPerBuildingList);
            allLevelDerivedNodesBuildingList.Add(level, allLeafCoverNodesBuildingList);
            allLevelDerivedNodesList.Add(level, allLeafCoverNodesPerBuildingList);
            leafAlsoTopCoverList = new HashSet<CoverNode>();
            leafAlsoTopCoverIndexList = new List<int>();

            HashSet<CoverNode> thisLevelNodes = new HashSet<CoverNode>();

            topCoverNodes = new HashSet<CoverNode>();
            foreach (CoverNode node in coverGraphCPMap.Keys)
            {
                if (coverGraphCPMap[node].Count() == 0)
                    topCoverNodes.Add(node);
            }

            if (allDerivedCoverNodes.Count() > 0)
            {
                leafAlsoTopCoverList = new HashSet<CoverNode>(topCoverNodes);
                topCoverNodes.ExceptWith(allLeafCoverNodes);
                leafAlsoTopCoverList.IntersectWith(allLeafCoverNodes);
                //then for those LeafNodes which are also Top Node, populate them to leafAlsoTopCoverIndexList
                int tempCCC = coverNodeLongList.Count();
                for (int i = 0; i < tempCCC; i++)
                {
                    if (leafAlsoTopCoverList.Contains(coverNodeLongList[i])) //TODO: could change to for loop for each node in leafAlsoTopCoverList
                    {
                        leafAlsoTopCoverIndexList.Add(i);
                    }
                }
            }
            int testCounter = 0;

            foreach (CoverNode node in allLevelDerivedNodes[level])  //this is leaf level
            {
                testCounter += coverGraphCPMap[node].Count();  
            }

            while (testCounter > 0) //if leaf level nodes have parents
            {
                level++;
                testCounter = 0;
                thisLevelNodes = new HashSet<CoverNode>();

                foreach (CoverNode preNode in allLevelDerivedNodes[level - 1])
                {
                    thisLevelNodes.UnionWith(coverGraphCPMap[preNode]);
                }

                allLevelDerivedNodes.Add(level, new HashSet<CoverNode>(thisLevelNodes));

                //then remove the nodes in previous levels
                //basically we need set the node in the higest level possible
                for (int preLevel = 1; preLevel < level; preLevel++)
                {
                    allLevelDerivedNodes[preLevel].ExceptWith(thisLevelNodes);
                }

                //re-set, see if there are more parents, if there are more levels to move up
                foreach (CoverNode node in allLevelDerivedNodes[level])
                {
                    testCounter += coverGraphCPMap[node].Count();
                }
            } //while testCounter > 0

            for (int i = 1; i <= level; i++)
            {
                allLevelDerivedNodes[level].ExceptWith(topCoverNodes);
            }
            allLevelDerivedNodes[level] = topCoverNodes;

            //we have determined nodes in all levels, then to form final level infor
            //start from leaf level
            //TODO: put level nodes in order, so that the child index will be clustered          
       
            Dictionary<int, HashSet<int>> buildingDict;

            //always add top level
            allCoverLevelFactorsIndex.Add(level, new List<int>());  //add the top level
            if (level == 0) //only have leaf covers
            {
                int counter = allLeafCoverNodesPerBuildingList.Count();
                for (int j = 0; j < counter; j++)                
                //foreach (CoverNode node in allLeafCoverNodesPerBuildingList)
                { 
                    CoverNode node = allLeafCoverNodesPerBuildingList[j];
                    if (node.IsPerRisk)
                        allCoverLevelFactorsIndex[level].Add(indexMapper.GetMappedFactorIndex(node.GetFirstRITEcharID(),allLeafCoverNodesBuildingList[j]));
                    else
                        allCoverLevelFactorsIndex[level].Add(-1);
                }
            }

            for (int currLevel = 1; currLevel <= level; currLevel++)
            {               
                allLevelDerivedNodesPerBuildingList.Add(currLevel, new List<CoverNode>());
                allLevelDerivedNodesBuildingList.Add(currLevel, new List<int>());
                allLevelChildrenIndexMap.Add(currLevel - 1, new List<int>());
                allLevelChildrenPartitionMap.Add(currLevel - 1, new List<int>());                
                allLevelAggSubjectMultiplyFactor.Add(currLevel, new List<float>());
                allLevelLimitMultiplyFactor.Add(currLevel, new List<float>());
                allCoverLevelDeds.Add(currLevel, new List<float>());
                allCoverLevelLimits.Add(currLevel, new List<float>());
                allCoverLevelShares.Add(currLevel, new List<float>());
                allCoverLevelFactorsIndex.Add(currLevel-1, new List<int>()); //factor is in the child level, go with the childrenmap
                               
                foreach (CoverNode currNode in allLevelDerivedNodes[currLevel])
                {
                    //parse Subject functions, same for all buildings, so move out
                    float subMulFactor = 0;
                    if (currNode.Subject.AggFunctionName == FunctionType.Sum)
                        subMulFactor = 0;                    
                    else if (currNode.Subject.AggFunctionName == FunctionType.Max)
                        subMulFactor = (float)-0.5;                  
                    else if (currNode.Subject.AggFunctionName == FunctionType.Min)
                        subMulFactor = (float)0.5;                    
                    else
                        throw new InvalidOperationException("Invalid Subject Agg function type");

                    //parse limit functions, same for all buildings, so move out
                    float limMulFactor = 1;
                    //if (currNode.Cover.LimitValType == TermValueType.PayFunction)
                    //{
                    if (currNode.Cover.Limit is FunctionValue)
                    {
                        FunctionValue limFun = currNode.Cover.Limit as FunctionValue;
                        if (limFun.Function == FunctionType.Min && limFun.Arguments.Count == 2)
                        {
                            if (limFun.Arguments[0] is SymbolicValue && limFun.Arguments[1] is MonetaryValue)
                            {
                                SymbolicValue temp0 = limFun.Arguments[0] as SymbolicValue;
                                MonetaryValue temp1 = limFun.Arguments[1] as MonetaryValue;
                                if (temp0.Symbol == "Subject")
                                    currNode.Cover.Limit = new MonetaryValue(temp1.Amount);
                                else
                                    throw new InvalidOperationException("Invalid Limit function type");
                            }
                            else if (limFun.Arguments[1] is SymbolicValue && limFun.Arguments[0] is MonetaryValue)
                            {
                                SymbolicValue temp0 = limFun.Arguments[1] as SymbolicValue;
                                MonetaryValue temp1 = limFun.Arguments[0] as MonetaryValue;
                                if (temp0.Symbol == "Subject")
                                    currNode.Cover.Limit = new MonetaryValue(temp1.Amount);
                                else
                                    throw new InvalidOperationException("Invalid Limit function type");
                            }

                            else
                                throw new InvalidOperationException("Invalid Limit function type");

                            //allLevelLimitMultiplyFactor[currLevel].Add(0);
                            limMulFactor = 0;
                        }
                        else if (limFun.Function == FunctionType.Constant && limFun.Arguments.Count == 1)
                        {
                            if (limFun.Arguments[0] is MonetaryValue)
                            {
                                MonetaryValue temp1 = limFun.Arguments[0] as MonetaryValue;
                                currNode.Cover.Limit = new MonetaryValue(temp1.Amount);
                            }
                            else
                                throw new InvalidOperationException("Invalid Limit function type");

                            //allLevelLimitMultiplyFactor[currLevel].Add((float)0.5);
                            limMulFactor = (float)0.5;
                        }
                    } //FunctionValue                                
                    // }

                    //read out share, limit and attachement
                    float share = (float)currNode.Cover.ProRata.Amount;
                    float lim = 0;
                    if (currNode.Cover.Unlimited)
                        lim = float.MaxValue;
                    else
                        lim = (float)currNode.Cover.LimitAmount;
                    float ded = (float)currNode.Cover.AttPointAmount;

                    if (currNode.IsPerRisk)  //the children should be all PerRisk, assuming they have same # of buildings
                    {
                        buildingDict = new Dictionary<int, HashSet<int>>();
                        int tempC = coverNodeLongList.Count();
                        for (int j = 0; j < tempC; j++)
                        {
                            if (coverGraphPCMap[currNode].Contains(coverNodeLongList[j])) //TODO: this takes a lot of time
                            //use for loop will not help the performamce
                            //foreach (CoverNode cNode in coverGraphPCMap[currNode])
                            {
                           //     if (cNode == coverNodeLongList[j])
                             //   {
                                    if (buildingDict.ContainsKey(coverNodeBuildingLongList[j]))
                                    {
                                        buildingDict[coverNodeBuildingLongList[j]].Add(j);
                                    }
                                    else
                                    {
                                        buildingDict.Add(coverNodeBuildingLongList[j], new HashSet<int>() { j });
                                    }
                               // }
                            }
                        }                                              

                        int maxBuilding = buildingDict.Keys.ToList().Max();
                        for (int building = 0; building <= maxBuilding; building++)
                        {
                            allLevelDerivedNodesPerBuildingList[currLevel].Add(currNode);
                            allLevelDerivedNodesBuildingList[currLevel].Add(building);
                            allLevelChildrenIndexMap[currLevel - 1].AddRange(buildingDict[building]);
                            allLevelChildrenPartitionMap[currLevel - 1].Add(allLevelChildrenIndexMap[currLevel - 1].Count());
                            allCoverLevelFactorsIndex[currLevel - 1].AddRange(Enumerable.Repeat(-1, buildingDict[building].Count).ToList().Select(x => (int)x)); //currNode is PerRisk, assume all children are perRisk, so factor = 1                            
                            if (currLevel == level)
                                allCoverLevelFactorsIndex[level].Add(indexMapper.GetMappedFactorIndex(currNode.GetFirstRITEcharID(), building));

                            coverNodeLongList.Add(currNode);
                            coverNodeBuildingLongList.Add(building);
                            allLevelAggSubjectMultiplyFactor[currLevel].Add(subMulFactor);
                            allLevelLimitMultiplyFactor[currLevel].Add(limMulFactor);
                            allCoverLevelDeds[currLevel].Add(ded);
                            allCoverLevelLimits[currLevel].Add(lim);
                            allCoverLevelShares[currLevel].Add(share);                            
                        }
                    } //PerRisk
                    else
                    {
                        allLevelDerivedNodesPerBuildingList[currLevel].Add(currNode);
                        allLevelDerivedNodesBuildingList[currLevel].Add(0);
                        allLevelAggSubjectMultiplyFactor[currLevel].Add(subMulFactor);
                        allLevelLimitMultiplyFactor[currLevel].Add(limMulFactor);
                        allCoverLevelDeds[currLevel].Add(ded);
                        allCoverLevelLimits[currLevel].Add(lim);
                        allCoverLevelShares[currLevel].Add(share);
        
                        int tempC = coverNodeLongList.Count();
                        for (int j = 0; j < tempC; j++)
                        {
                            if (coverGraphPCMap.ContainsKey(currNode) && coverGraphPCMap[currNode].Contains(coverNodeLongList[j]))
                            {
                                allLevelChildrenIndexMap[currLevel - 1].Add(j);
                                if (coverNodeLongList[j].IsPerRisk)
                                    allCoverLevelFactorsIndex[currLevel-1].Add(indexMapper.GetMappedFactorIndex(coverNodeLongList[j].GetFirstRITEcharID(),coverNodeBuildingLongList[j]));                                
                                else
                                    allCoverLevelFactorsIndex[currLevel - 1].Add(-1);                                
                            }
                        }
                        allLevelChildrenPartitionMap[currLevel - 1].Add(allLevelChildrenIndexMap[currLevel - 1].Count());

                        if (currLevel == level)
                            allCoverLevelFactorsIndex[level].Add(-1); //top summed node, so factor = 1

                        coverNodeLongList.Add(currNode);
                        coverNodeBuildingLongList.Add(0);
                        
                    } //not PerRisk                                        
                }  //foreach node

                List<float> subFactor = allLevelAggSubjectMultiplyFactor[currLevel].Distinct().ToList();
                List<float> limitFactor = allLevelLimitMultiplyFactor[currLevel].Distinct().ToList();
                if (subFactor.Count() > 1)
                    allLevelAggSubjectFuncType.Add(currLevel, FunctionType.Mix);
                else if (subFactor[0] == 0.5)
                    allLevelAggSubjectFuncType.Add(currLevel, FunctionType.Min);
                else if (subFactor[0] == -0.5)
                    allLevelAggSubjectFuncType.Add(currLevel, FunctionType.Max);
                else if (subFactor[0] == 0)
                    allLevelAggSubjectFuncType.Add(currLevel, FunctionType.Sum);

                if (limitFactor.Count() > 1)
                    allLevelLimitFuncType.Add(currLevel, FunctionType.Mix);
                else if (limitFactor[0] == 1)
                    allLevelLimitFuncType.Add(currLevel, FunctionType.Regular);
                else if (limitFactor[0] == 0)
                    allLevelLimitFuncType.Add(currLevel, FunctionType.Min);
                else if (limitFactor[0] == 0.5)
                    allLevelLimitFuncType.Add(currLevel, FunctionType.Constant);                    
            }//for each level
        }

        #region delete later
        //public void FormLeafCoverLevel_withTermNode(Dictionary<CoverNode, HashSet<GraphNode>> leafCoverNodeChildrenMap, Dictionary<int, TermNode[]> allLevelNodesPerBuildingList, Dictionary<int, int[]> allLevelBuildingsList, Dictionary<int, int[]> allLevelAtomicRiteIndexList)
        //{
        //    //Dictionary<CoverNode, List<Tuple<int, bool, int>>> leafCoverLevelInfo = new Dictionary<CoverNode, List<Tuple<int, bool, int>>>();
        //    List<int> leafCoverLevelInfo = new List<int>();
        //    List<bool> leafCoverIsTermInfo = new List<bool>();
        //    List<int> leafCoverIndexInfo = new List<int>();
        //    List<int> leafCoverPartition = new List<int>();
        //    List<int> leafCoverRitesInfo = new List<int>();
        //    List<int> leafCoverRitesPartition = new List<int>();

        //    int numOfTermLevel = allLevelBuildingsList.Keys.ToArray().Max() + 1;

        //    foreach (CoverNode cNode in leafCoverNodeChildrenMap.Keys)
        //    {
        //        PrimarySubject s = cNode.Subject as PrimarySubject;
        //        int numBuildings = s.Schedule.ScheduleList.First().ActNumOfBldgs;
        //        HashSet<int> allRites;
        //        HashSet<int> childrenRites;


        //        //Certainly, PerRisk and nonPerRisk scenarios can be combinded to reduce the amount of codes, 
        //        //but for clarity, just separate them for easy reading
        //        if (cNode.IsPerRisk)
        //        {
        //            allRites = new HashSet<int>();
        //            childrenRites = new HashSet<int>();

        //            //need to expand for perBuilding                                   
        //            for (int building = 0; building < numBuildings; building++)
        //            {
        //                bool found = false;
        //                foreach (GraphNode node in leafCoverNodeChildrenMap[cNode])
        //                {
        //                    //assume all children should have the same num of buildings
        //                    for (int level = 0; level < numOfTermLevel; level++)
        //                    {
        //                        for (int iInLevel = 0; iInLevel < allLevelNodesPerBuildingList[level].Count(); iInLevel++)
        //                        {
        //                            if (allLevelNodesPerBuildingList[level][iInLevel] == node && allLevelBuildingsList[level][iInLevel] == building)
        //                            {
        //                                leafCoverLevelInfo.Add(level);
        //                                leafCoverIsTermInfo.Add(true);
        //                                leafCoverIndexInfo.Add(iInLevel);
        //                                found = true;
        //                                goto nextBuilding1;
        //                            }
        //                        }
        //                    }
        //                    childrenRites.UnionWith(GetAtomicRitesIndexForSubject(node.Subject, building));
        //                }

        //                nextBuilding1:
        //                if (found == false)
        //                    throw new InvalidOperationException("Cover is Per Building, but its children term is not");

        //                //find rites info
        //                allRites.UnionWith(GetAtomicRitesIndexForSubject(cNode.Subject, building));
        //                allRites.ExceptWith(childrenRites);

        //                foreach (int riteIndex in allRites)
        //                {
        //                    found = false;
        //                    //loop through all atomic levels,
        //                    for (int level = 1; level < numOfTermLevel + 1; level++)
        //                    {
        //                        for (int rInLevel = 0; rInLevel < allLevelAtomicRiteIndexList[level].Count(); rInLevel++)
        //                        {
        //                            if (allLevelAtomicRiteIndexList[level][rInLevel] == riteIndex)
        //                            {
        //                                leafCoverLevelInfo.Add(level);
        //                                leafCoverIsTermInfo.Add(false);
        //                                leafCoverIndexInfo.Add(rInLevel);
        //                                found = true;                                        
        //                            }
        //                        }
        //                    }
        //                    if (found == false)
        //                    {
        //                        leafCoverRitesInfo.Add(riteIndex);
        //                    }
        //                }
        //            }  //for each building
        //        }  //cNode PerRisk
        //        else
        //        {
        //            allRites = new HashSet<int>();
        //            childrenRites = new HashSet<int>();

        //            bool found = false;

        //            foreach (GraphNode node in leafCoverNodeChildrenMap[cNode])
        //            {
        //                PrimarySubject cSubject = node.Subject as PrimarySubject;
        //                if (node.IsPerRisk)                                                    
        //                    numBuildings = cSubject.Schedule.ActNumOfBldgs;
        //                else
        //                    numBuildings = 1;

        //                for (int building = 0; building < numBuildings; building++)
        //                {
        //                    //assume all children should have the same num of buildings
        //                    for (int level = 0; level < numOfTermLevel; level++)
        //                    {
        //                        for (int iInLevel = 0; iInLevel < allLevelNodesPerBuildingList[level].Count(); iInLevel++)
        //                        {
        //                            if (allLevelNodesPerBuildingList[level][iInLevel] == node && allLevelBuildingsList[level][iInLevel] == building)
        //                            {
        //                                leafCoverLevelInfo.Add(level);
        //                                leafCoverIsTermInfo.Add(true);
        //                                leafCoverIndexInfo.Add(iInLevel);
        //                                found = true;
        //                                goto nextBuilding2;
        //                            }
        //                        }
        //                    }
        //                nextBuilding2:
        //                    childrenRites.UnionWith(GetAtomicRitesIndexForSubject(node.Subject, building));


        //                    if (found == false)
        //                        throw new InvalidOperationException("Cover is Per Building, but its children term is not");

        //                    //find rites info
        //                    allRites.UnionWith(GetAtomicRitesIndexForSubject(cNode.Subject, building));
        //                    allRites.ExceptWith(childrenRites);

        //                    foreach (int riteIndex in allRites)
        //                    {
        //                        found = false;
        //                        //loop through all atomic levels,
        //                        for (int level = 1; level < numOfTermLevel + 1; level++)
        //                        {
        //                            for (int rInLevel = 0; rInLevel < allLevelAtomicRiteIndexList[level].Count(); rInLevel++)
        //                            {
        //                                if (allLevelAtomicRiteIndexList[level][rInLevel] == riteIndex)
        //                                {
        //                                    leafCoverLevelInfo.Add(level);
        //                                    leafCoverIsTermInfo.Add(false);
        //                                    leafCoverIndexInfo.Add(rInLevel);
        //                                    found = true;
        //                                }
        //                            }
        //                        }
        //                        if (found == false)
        //                        {
        //                            leafCoverRitesInfo.Add(riteIndex);
        //                        }
        //                    }
        //                }
        //            }  //for each building
        //        } //cNode nonPerRisk
        //        leafCoverPartition.Add(leafCoverLevelInfo.Count());
        //        leafCoverRitesPartition.Add(leafCoverRitesInfo.Count());                
        //    } //foreach cNode
        //}

       

        //public IExecutableMatrixGraph NonOverlapMatrixGraphBuilder(List<TermNode> InitialTermNodes, List<CoverNode> IntitialCoverNodes)
        //{
        //    IBuildableMatrixGraph graph = new AutoMatrixGraph();
        //    ContractInfo contractinfo = new ContractInfo(!Contract.Declarations.GroundUpSublimits, Contract.Declarations.MinimumAbsorbingDed);
        //    graph.SetContractInfo(contractinfo);

        //    NonOverlapNodeBuilder(InitialTermNodes, IntitialCoverNodes);
        //    int numOfLevels = allLevelNodesList.Keys.Count();

        //    allLevelAggSubjectFuncType = new Dictionary<int, FunctionType>();
        //    allLevelLimitFuncType = new Dictionary<int, FunctionType>();
        //    allLevelAggSubjectMultiplyFactor = new Dictionary<int, List<float>>();
        //    allLevelLimitMultiplyFactor = new Dictionary<int, List<float>>();
 
        //    AtomicRITELevel lowestAtomicRiteLevel;

        //    Dictionary<int, bool> allLevelIsMaxDed;
        //    Dictionary<int, bool> allLevelIsPercentDed;
        //    Dictionary<int, bool> allLevelIsFranchiseDed;

        //    Dictionary<int, float[]> allLevelMinDeds;
        //    Dictionary<int, float[]> allLevelMaxDeds;
        //    Dictionary<int, float[]> allLevelLimits;
        //    Dictionary<int, bool[]> allLevelMinFranchise;
        //    Dictionary<int, bool[]> allLevelMaxFranchise;

        //    if (numOfLevels != 0)
        //    {
        //        // ExpandNodesForMultiBuildings(allLevelNodesList, allLevelNodePartitionList,
        //        //out allLevelNodesPerBuildingList, out allLevelBuildingsList, out allLevelNodePerBuildindgPartitionList);
        //        ExpandNodesForMultiBuildings();
        //        Console.WriteLine("end at expand for building " + DateTime.Now.ToString("h:mm:ss.fff tt"));

        //        //these are all output (kept locally), will write to graph object (global variable) at the end
        //        allLevelPartitionList = new Dictionary<int, int[]>();

        //        allLevelAtomicRiteIndexList = new Dictionary<int, int[]>();
        //        allLevelAtomicRITEPartitionList = new Dictionary<int, int[]>();

        //        allLevelIsMaxDed = new Dictionary<int, bool>();
        //        allLevelIsPercentDed = new Dictionary<int, bool>();
        //        allLevelIsFranchiseDed = new Dictionary<int, bool>();

        //        allLevelMinDeds = new Dictionary<int, float[]>();
        //        allLevelMaxDeds = new Dictionary<int, float[]>();
        //        allLevelLimits = new Dictionary<int, float[]>();
        //        allLevelMinFranchise = new Dictionary<int, bool[]>();
        //        allLevelMaxFranchise = new Dictionary<int, bool[]>();

        //        Console.WriteLine("graph start at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));

        //        //find the levels
        //        int numOfTermLevels = allLevelNodesList.Keys.ToArray().Max() + 1;

        //        //Top level: Level 0
        //        bool isMaxDed = false;
        //        bool isPercentDed = false;
        //        bool isFranchiseDed = false;

        //        List<float> minDedList = new List<float>();
        //        List<float> maxDedList = new List<float>();
        //        List<float> limitList = new List<float>();
        //        List<bool> minFranchiseList = new List<bool>();
        //        List<bool> maxFranchiseList = new List<bool>();

        //        List<int> thislevelPartitionList = new List<int>();

        //        float minDed;
        //        float maxDed;
        //        bool minFranchised;
        //        bool maxFranchised;
        //        bool maxDedFlag;

        //        int level = 0;
        //        int n = 0;

        //        for (level = 0; level < numOfTermLevels; level++)
        //        {
        //            isMaxDed = false;
        //            isPercentDed = false;
        //            isFranchiseDed = false;

        //            minDedList = new List<float>();
        //            maxDedList = new List<float>();
        //            limitList = new List<float>();
        //            minFranchiseList = new List<bool>();
        //            maxFranchiseList = new List<bool>();

        //            for (n = 0; n < allLevelNodesPerBuildingList[level].Count(); n++)
        //            {
        //                TermNode tNode = allLevelNodesPerBuildingList[level][n];
        //                int bNum = allLevelBuildingsList[level][n];

        //                int origNumBuildings = tNode.GetNumOfBuildingsOrigin();
        //                double[] multiArr = GenerateMultiplierArr(origNumBuildings);

        //                tNode.Deductibles.GetMaxDed(out maxDed, out maxDedFlag, out maxFranchised);
        //                tNode.Deductibles.GetMinDed(out minDed, out minFranchised);

        //                if (maxDedFlag)
        //                    isMaxDed = true;
        //                if (maxFranchised == true || minFranchised == true)
        //                    isFranchiseDed = true;

        //                minDedList.Add(minDed * (float)multiArr[bNum]);
        //                maxDedList.Add(maxDed * (float)multiArr[bNum]);
        //                minFranchiseList.Add(minFranchised);
        //                maxFranchiseList.Add(maxFranchised);
        //                limitList.Add((float)tNode.Limits.GetLimit() * (float)multiArr[bNum]);
        //            }

        //            allLevelPartitionList.Add(level, allLevelNodePerBuildindgPartitionList[level]);
        //            allLevelIsMaxDed.Add(level, isMaxDed);
        //            allLevelIsPercentDed.Add(level, isPercentDed);
        //            allLevelIsFranchiseDed.Add(level, isFranchiseDed);
        //            allLevelMinDeds.Add(level, minDedList.ToArray());
        //            allLevelMaxDeds.Add(level, maxDedList.ToArray());
        //            allLevelMaxFranchise.Add(level, maxFranchiseList.ToArray());
        //            allLevelMinFranchise.Add(level, minFranchiseList.ToArray());
        //            allLevelLimits.Add(level, limitList.ToArray());
        //        }

        //        Console.WriteLine("before atomic rite " + DateTime.Now.ToString("h:mm:ss.fff tt"));

        //        //-------------------------------------------------
        //        //then add AtomicRites, start from lowest level
        //        //this lowest level is (numOfTermLevel + 1) and are all AtomicRiteLevel            
        //        List<int> thisLevelAtomicRITEsIndex = new List<int>();
        //        List<int> thisLevelAtomicRitePartitionList = new List<int>();

        //        level = numOfTermLevels;  //bottom level, the level without any nodes 

        //        for (n = 0; n < allLevelNodesPerBuildingList[level - 1].Count(); n++)
        //        {
        //            TermNode tNode = allLevelNodesPerBuildingList[level - 1][n];
        //            if (tNode.IsPerRisk)
        //            {
        //                thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(tNode.Subject, allLevelBuildingsList[level - 1][n]).ToList());
        //                thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
        //            }  //perRisk
        //            else
        //            {
        //                for (int ii = 0; ii < tNode.GetNumOfBuildingsActual(); ii++)
        //                {
        //                    //thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject, tNode.GetNumOfBuildings()).ToList());
        //                    thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(tNode.Subject, ii).ToList());
        //                }
        //                thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
        //            }
        //        }
        //        allLevelAtomicRiteIndexList.Add(numOfTermLevels, thisLevelAtomicRITEsIndex.ToArray());
        //        allLevelAtomicRITEPartitionList.Add(numOfTermLevels, thisLevelAtomicRitePartitionList.ToArray());

        //        Console.WriteLine(" atomic end lowest level at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));
        //        //then for each upper level
        //        for (level = numOfTermLevels - 1; level > 0; level--)
        //        {
        //            thisLevelAtomicRITEsIndex = new List<int>();
        //            thisLevelAtomicRitePartitionList = new List<int>();
        //            int tempHolder = 0;

        //            for (n = 0; n < allLevelNodesPerBuildingList[level - 1].Count(); n++)
        //            {
        //                TermNode tNode = allLevelNodesPerBuildingList[level - 1][n];

        //                if (tNode.IsPerRisk)
        //                {
        //                    HashSet<int> allRites = GetAtomicRitesIndexForSubject(tNode.Subject, allLevelBuildingsList[level - 1][n]);
        //                    HashSet<int> childrenRites = new HashSet<int>();

        //                    for (int k = tempHolder; k <= allLevelNodePerBuildindgPartitionList[level][n] - 1; k++)
        //                    {
        //                        childrenRites.UnionWith(GetAtomicRitesIndexForSubject(allLevelNodesPerBuildingList[level][k].Subject, allLevelBuildingsList[level][k]));
        //                    }

        //                    //find Residual atomicRite
        //                    allRites.ExceptWith(childrenRites);
        //                    thisLevelAtomicRITEsIndex.AddRange(allRites.ToList());
        //                    thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());

        //                    tempHolder = allLevelNodePerBuildindgPartitionList[level][n];
        //                }
        //                else if (!tNode.IsPerRisk)
        //                {
        //                    //tempHolder = 0;
        //                    HashSet<int> allRites = new HashSet<int>();
        //                    //do special
        //                    for (int ii = 0; ii < tNode.GetNumOfBuildingsActual(); ii++)
        //                    {
        //                        allRites.UnionWith(GetAtomicRitesIndexForSubject(tNode.Subject, ii));
        //                    }

        //                    HashSet<int> childrenRites = new HashSet<int>();

        //                    for (int k = tempHolder; k <= allLevelNodePartitionList[level][n] - 1; k++)
        //                    {
        //                        childrenRites.UnionWith(GetAtomicRitesIndexForSubject(allLevelNodesList[level][k].Subject, allLevelBuildingsList[level][k]));
        //                        //tempHolder = allLevelNodePartitionList[level][n];
        //                    }
        //                    //find Residual atomicRite                                                          

        //                    allRites.ExceptWith(childrenRites);
        //                    thisLevelAtomicRITEsIndex.AddRange(allRites.ToList());
        //                    thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
        //                    tempHolder = allLevelNodePerBuildindgPartitionList[level][n];
        //                }
        //            }
        //            allLevelAtomicRiteIndexList.Add(level, thisLevelAtomicRITEsIndex.ToArray());
        //            allLevelAtomicRITEPartitionList.Add(level, thisLevelAtomicRitePartitionList.ToArray());
        //            Console.WriteLine(" atomic end at level : " + level + DateTime.Now.ToString("h:mm:ss.fff tt"));
        //        }

        //        //level 0: the top level should have no AtomicRiteLevel
        //        //TODO: we need default empty Level infor.
        //        allLevelAtomicRiteIndexList.Add(0, new List<int>().ToArray());
        //        allLevelAtomicRITEPartitionList.Add(0, new int[1]);

        //        if (IsOverlapByAtomicRites(allLevelAtomicRiteIndexList))
        //            throw new InvalidOperationException("Overlap detected by AtomicRITE");

        //        lowestAtomicRiteLevel = new AtomicRITELevel(allLevelAtomicRITEPartitionList[numOfTermLevels], allLevelAtomicRiteIndexList[numOfTermLevels].Count(),
        //                                                                   allLevelAtomicRiteIndexList[numOfTermLevels]);
        //        graph.SetLowestARITELevel(lowestAtomicRiteLevel);

        //        for (int i = 0; i < numOfTermLevels; i++)
        //        {
        //            //TermLevel thisTermLevel = new TermLevel(allLevelNodesList[i].Length, allLevelAtomicRiteIndexList[i].Length, allLevelIsMaxDed[i], allLevelIsPercentDed[i],
        //            //                                         allLevelMinDeds[i], allLevelMaxDeds[i], allLevelLimits[i], allLevelPartitionList[i], allLevelAtomicRITEPartitionList[i], allLevelAtomicRiteIndexList[i]);
        //            TermLevel thisTermLevel = new TermLevel(allLevelMinDeds[i].Length, allLevelAtomicRiteIndexList[i].Length, allLevelIsMaxDed[i], allLevelIsPercentDed[i],
        //                                                     allLevelMinDeds[i], allLevelMaxDeds[i], allLevelLimits[i], allLevelPartitionList[i],
        //                                                     allLevelAtomicRITEPartitionList[i], allLevelAtomicRiteIndexList[i], allLevelIsFranchiseDed[i], allLevelMinFranchise[i], allLevelMaxFranchise[i]);

        //            graph.AddTermLevel(i, thisTermLevel);
        //        }
        //    }


        //    Console.WriteLine("before cover graph: " + DateTime.Now.ToString(("h:mm:ss.fff tt")));
        //    #region Used for term only Matrix prototype to handle no term cases
        //    //else
        //    //{
        //    //    numOfLevels = 1;

        //    //    Subject topCoverSubject = Contract.GetAllCoverSubjects()[0];
        //    //    HashSet<AtomicRITE> Arites = topCoverSubject.GetAtomicRites();
        //    //    int NumOfArites = Arites.Count();
        //    //    int[] partitions = new int[]{NumOfArites};

        //    //    List<int> thisLevelAtomicRITEsIndex = new List<int>();

        //    //    for (int ii = 0; ii < NumOfArites; ii++)
        //    //    {
        //    //        //thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject, tNode.GetNumOfBuildings()).ToList());
        //    //        thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(topCoverSubject, ii).ToList());
        //    //    }

        //    //    lowestAtomicRiteLevel = new AtomicRITELevel(partitions, NumOfArites, thisLevelAtomicRITEsIndex.ToArray());

        //    //    graph.SetLowestARITELevel(lowestAtomicRiteLevel);
        //    //}
        //    #endregion

        //    //set leaf cover level
        //    FormLeafCoverLevel();
        //    Console.WriteLine("done leafcover: " + DateTime.Now.ToString(("h:mm:ss.fff tt")));
        //    //then for upper Derived Cover Levels
        //    FormDerivedCoverLevels();
        //    Console.WriteLine("done derive cover: " + DateTime.Now.ToString(("h:mm:ss.fff tt")));
        //    int totalCoverLevels = allLevelDerivedNodesBuildingList.Keys.ToList().Max() + 1;

        //    CoverLevel thisCover;
        //    LowestCoverLevel lowestCoverLevel;

        //    lowestCoverLevel = new LowestCoverLevel(allLeafCoverResidualRitesInfo.Count(), allLeafCoverRitesInfo.Count(), allLeafCoverResidualRitesPartition.ToArray(),
        //                            allLeafCoverRitesPartition.ToArray(), allLeafCoverResidualRitesInfo.ToArray(), allLeafCoverLevelInfo.ToArray(), allLeafCoverRitesInfo.ToArray());

        //    graph.SetLowestCoverLevel(lowestCoverLevel);

        //    for (int level = 0; level < totalCoverLevels; level++)
        //    {
        //        int numNode = allLevelDerivedNodesPerBuildingList[level].Count();
        //        float[] share = new float[numNode];
        //        float[] lim = new float[numNode];
        //        float[] ded = new float[numNode];

        //        for (int i = 0; i < numNode; i++)
        //        {
        //            share[i] = (float)allLevelDerivedNodesPerBuildingList[level][i].Cover.ProRata.Amount;
        //            if (allLevelDerivedNodesPerBuildingList[level][i].Cover.Unlimited)
        //                lim[i] = float.MaxValue;
        //            else
        //                lim[i] = (float)allLevelDerivedNodesPerBuildingList[level][i].Cover.LimitAmount;
        //            ded[i] = (float)allLevelDerivedNodesPerBuildingList[level][i].Cover.AttPointAmount;
        //        }

        //        int[] mapPartition;
        //        int[] childrenMap;
        //        if (level < totalCoverLevels - 1)
        //        {
        //            mapPartition = allLevelChildrenPartitionMap[level].ToArray();
        //            childrenMap = allLevelChildrenIndexMap[level].ToArray();
        //        }
        //        else
        //        {
        //            mapPartition = new int[1];
        //            mapPartition[0] = numNode;
        //            childrenMap = new int[numNode];
        //            for (int i = 0; i < numNode; i++)
        //            {
        //                int k = coverNodeLongList.Count() - (numNode - i);
        //                childrenMap[i] = k;
        //            }
        //        }
        //        if (level == 0)
        //        {
        //            thisCover = new CoverLevel(allLevelLimitFuncType[level], allLevelLimitMultiplyFactor[level].ToArray(), numNode, share, lim, ded, mapPartition, childrenMap, leafAlsoTopCoverIndexList.ToArray());
        //            graph.AddLeafCoverLevel(numOfLevels + 2 + level, thisCover);
        //        }
        //        else
        //        {
        //            thisCover = new DerivedCoverLevel(allLevelAggSubjectFuncType[level], allLevelLimitFuncType[level], allLevelAggSubjectMultiplyFactor[level].ToArray(), allLevelLimitMultiplyFactor[level].ToArray(), numNode, share, lim, ded, mapPartition, childrenMap);
        //            graph.AddDerivedCoverLevel(numOfLevels + 2 + level, (DerivedCoverLevel)thisCover);
        //        }
        //    }

        //    Console.WriteLine(" graph end at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));
            
        //    return (IExecutableMatrixGraph)graph;
        //}

        //-------------------------------------------------------------------------------
        //public void NonOverlapNodeBuilder(List<TermNode> InitialTermNodes, List<CoverNode> InitialCoverNodes)        
        //{ 
        //    //output
        //    allLevelNodesList = new Dictionary<int, TermNode[]>();
        //    allLevelNodePartitionList = new Dictionary<int, int[]>();

        //    //Do Node Tree
        //    NodeTree nodeTree = new NodeTree(InitialTermNodes, InitialCoverNodes);
        //    nodeTree.Run();
        //    Dictionary<TermNode, HashSet<TermNode>> TermCPMap = nodeTree.TermChildParentsMap;
        //    Dictionary<TermNode, HashSet<TermNode>> TermPCMap = nodeTree.TermParentChildrenMap;
        //    //Dictionary<CoverNode, HashSet<TermNode>> leafCoverNodeChildrenMap = nodeTree.LeafCoverNodeChildrenMap;
        //    coverGraphCPMap = nodeTree.CoverNodeChildParentsMap; //This could have overlap
        //    coverGraphPCMap = nodeTree.CoverNodeParentChildrenMap;
        //    allLeafCoverNodes = nodeTree.FinalLeafCoverNodes;
        //    allDerivedCoverNodes = nodeTree.FinalDerivedCoverNodes;

        //    //BuildCoverGraph(coverGraphCPMap);

        //    foreach (TermNode gn in nodeTree.FinalTermNodes)
        //    {
        //        //evaluate percent ded and limit
        //        if (gn is TermNode)
        //        {
        //            TermNode tNode = gn as TermNode;
        //            foreach (Deductible dedObj in tNode.Deductibles.GetDedList())
        //            {
        //                if (dedObj.DedType == TermValueType.PercentCovered)
        //                {
        //                    dedObj.UpdatePercentDed((float)tNode.GetTIV());
        //                }
        //            }
        //            foreach (Limit limObj in tNode.Limits.GetLimList())
        //            {
        //                if (limObj.LimType == TermValueType.PercentCovered)
        //                {
        //                    limObj.UpdatePercentLimit((float)tNode.GetTIV());
        //                }
        //            }
        //        }
        //    }

        //    //check if there is overlap among the Nodes
        //    //overlap logic: among a node's all parents, if the parents are not parent-child of each other, then overlap.
        //    if (IsOverlappedByTermNodes(TermCPMap))
        //        throw new InvalidOperationException("Overlap detected by nodes");

        //    //if not overlap, then we can use the following logic            
        //    Dictionary<int, HashSet<GraphNode>> nodesByLevel = new Dictionary<int, HashSet<GraphNode>>();

        //    //find the levels
        //    int numOfNodeLevels = 0;
        //    nodesByLevel = SetLevels(TermCPMap, out numOfNodeLevels);

        //    if (nodesByLevel.Keys.Count() != 0)
        //    {
        //        GraphNode[] thisLevelNodeArr = new GraphNode[nodesByLevel[0].Count()];
        //        int[] levelNodePartition = new int[1];

        //        int j = 0;
        //        foreach (GraphNode node in nodesByLevel[0])
        //        {
        //            TermNode tNode = (TermNode)node;
        //            thisLevelNodeArr[j] = tNode;
        //            j++;
        //        }

        //        levelNodePartition[0] = thisLevelNodeArr.Count();

        //        allLevelNodesList.Add(0, thisLevelNodeArr);
        //        allLevelNodePartitionList.Add(0, levelNodePartition);

        //        int preLevelSize;
        //        //for each lower level      
        //        for (int i = 1; i < numOfNodeLevels; i++)
        //        {
        //            GraphNode[] preLevelNodeArr = allLevelNodesList[i - 1];
        //            preLevelSize = preLevelNodeArr.Length;

        //            levelNodePartition = new int[preLevelSize];

        //            List<GraphNode> thisLevelNodes = new List<GraphNode>();

        //            for (int k = 0; k < preLevelSize; k++) //for each of the parent node
        //            {
        //                GraphNode pNode = preLevelNodeArr[k];
        //                TermNode ptNode = pNode as TermNode;
        //                HashSet<GraphNode> childrenSet = new HashSet<GraphNode>();
        //                List<GraphNode> childrenList = new List<GraphNode>();
        //                if (TermPCMap.ContainsKey(pNode))
        //                {
        //                    childrenSet.UnionWith(TermPCMap[pNode]);
        //                    childrenSet.IntersectWith(nodesByLevel[i]);
        //                    childrenList = childrenSet.ToList();
        //                    thisLevelNodes.AddRange(childrenList);
        //                }
        //                levelNodePartition[k] = thisLevelNodes.Count();
        //            }
        //            allLevelNodesList.Add(i, thisLevelNodes.ToArray());
        //            allLevelNodePartitionList.Add(i, levelNodePartition);
        //        }  //done with TermLevelInfor                    
        //    }
        //}

        //public IExecutableMatrixGraph NonOverlapBuild(List<TermNode> InitialTermNodes, List<CoverNode> InitialCoverNodes)
        //{
        //    //transfer the TermNode to GraphNode
        //    List<GraphNode> InitialGraphNodes = new List<GraphNode>();

        //    for (int i = 0; i < InitialTermNodes.Count(); i++)
        //    {
        //        GraphNode gNode = InitialTermNodes[i] as GraphNode;
        //        InitialGraphNodes.Add(gNode);
        //    }

        //    //these are all output, will write to graph object at the end
        //    Dictionary<int, GraphNode[]> allLevelNodesList = new Dictionary<int, GraphNode[]>();
        //    Dictionary<int, int[]> allLevelNodePartitionList = new Dictionary<int, int[]>();
        //    Dictionary<int, int[]> allLevelPartitionList = new Dictionary<int, int[]>();

        //    Dictionary<int, int[]> allLevelAtomicRiteIndexList = new Dictionary<int, int[]>();
        //    Dictionary<int, int[]> allLevelAtomicRITEPartitionList = new Dictionary<int, int[]>();

        //    Dictionary<int, bool> allLevelIsMaxDed = new Dictionary<int, bool>();
        //    Dictionary<int, bool> allLevelIsPercentDed = new Dictionary<int, bool>();
        //    Dictionary<int, bool> allLevelIsFranchiseDed = new Dictionary<int, bool>();

        //    Dictionary<int, float[]> allLevelMinDeds = new Dictionary<int, float[]>();
        //    Dictionary<int, float[]> allLevelMaxDeds = new Dictionary<int, float[]>();
        //    Dictionary<int, float[]> allLevelLimits = new Dictionary<int, float[]>();
        //    Dictionary<int, bool[]> allLevelMinFranchise = new Dictionary<int, bool[]>();
        //    Dictionary<int, bool[]> allLevelMaxFranchise = new Dictionary<int, bool[]>();

        //    IBuildableMatrixGraph graph = new AutoMatrixGraph();
        //    ContractInfo contractinfo = new ContractInfo(!Contract.Declarations.GroundUpSublimits, Contract.Declarations.MinimumAbsorbingDed);
        //    graph.SetContractInfo(contractinfo);

        //    Console.WriteLine("graph start at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));

        //    //Do Node Tree
        //    NodeTree nodeTree = new NodeTree(InitialGraphNodes, InitialCoverNodes);
        //    nodeTree.Run();
        //    Dictionary<GraphNode, HashSet<GraphNode>> CPMap = nodeTree.TermChildParentsMap;
        //    Dictionary<GraphNode, HashSet<GraphNode>> PCMap = nodeTree.TermParentChildrenMap;

        //    //check if there is overlap in among the Nodes
        //    //overlap logic: among a node's all parents, if the parents are not parent-child of each other, then overlap.
        //    if (IsOverlappedByTermNodes(CPMap))
        //        throw new InvalidOperationException("Overlap detected by nodes");

        //    //if not overlap, then we can use the following logic            
        //    Dictionary<int, HashSet<GraphNode>> nodesByLevel = new Dictionary<int, HashSet<GraphNode>>();

        //    //find the levels
        //    int numOfNodeLevels = 0;
        //    nodesByLevel = SetLevels(CPMap, out numOfNodeLevels);

        //    //Top level: Level 0
        //    bool isMaxDed = false;
        //    bool isPercentDed = false;
        //    bool isFranchiseDed = false;

        //    List<float> minDedList = new List<float>();
        //    List<float> maxDedList = new List<float>();
        //    List<float> limitList = new List<float>();
        //    List<bool> minFranchiseList = new List<bool>();
        //    List<bool> maxFranchiseList = new List<bool>();

        //    AtomicRITELevel lowestAtomicRiteLevel;
        //    if (nodesByLevel.Keys.Count != 0)
        //    {
        //        GraphNode[] thisLevelNodeArr = new GraphNode[nodesByLevel[0].Count()];
        //        int[] levelNodePartition = new int[1];
        //        List<int> levelPartitionList = new List<int>();

        //        float minDed;
        //        float maxDed;
        //        bool minFranchised;
        //        bool maxFranchised;
        //        bool maxDedFlag;

        //        int j = 0;
        //        CoverNode coverNode;
        //        foreach (GraphNode node in nodesByLevel[0])
        //        {
        //            if (node is TermNode)
        //            {
        //                TermNode tNode = node as TermNode;
        //                int origNumBuildings = tNode.GetNumOfBuildingsOrigin();
        //                int numBuildings = tNode.GetNumOfBuildings();
        //                double[] multiArr = GenerateMultiplierArr(origNumBuildings);

        //                tNode.Deductibles.GetMaxDed(out maxDed, out maxDedFlag, out maxFranchised);
        //                tNode.Deductibles.GetMinDed(out minDed, out minFranchised);

        //                if (maxDed > 0)
        //                    isMaxDed = true;
        //                if (maxFranchised == true || minFranchised == true)
        //                    isFranchiseDed = true;

        //                for (int i = 0; i < numBuildings; i++)
        //                {
        //                    minDedList.Add(minDed * (float)multiArr[i]);
        //                    maxDedList.Add(maxDed * (float)multiArr[i]);
        //                    minFranchiseList.Add(minFranchised);
        //                    maxFranchiseList.Add(maxFranchised);
        //                    limitList.Add((float)tNode.Limits.GetLimit() * (float)multiArr[i]);
        //                }
        //            }
        //            else
        //            {
        //                coverNode = node as CoverNode;
        //            }
        //            thisLevelNodeArr[j] = node;
        //            j++;
        //        }

        //        levelNodePartition[0] = thisLevelNodeArr.Count();
        //        levelPartitionList.Add(minDedList.Count());

        //        allLevelNodesList.Add(0, thisLevelNodeArr);
        //        allLevelPartitionList.Add(0, levelPartitionList.ToArray());
        //        allLevelNodePartitionList.Add(0, levelNodePartition);
        //        allLevelIsMaxDed.Add(0, isMaxDed);
        //        allLevelIsPercentDed.Add(0, isPercentDed);
        //        allLevelIsFranchiseDed.Add(0, isFranchiseDed);
        //        allLevelMinDeds.Add(0, minDedList.ToArray());
        //        allLevelMaxDeds.Add(0, maxDedList.ToArray());
        //        allLevelMaxFranchise.Add(0, maxFranchiseList.ToArray());
        //        allLevelMinFranchise.Add(0, minFranchiseList.ToArray());
        //        allLevelLimits.Add(0, limitList.ToArray());

        //        int preLevelSize;
        //        //for each lower level      
        //        for (int i = 1; i < numOfNodeLevels; i++)
        //        {
        //            isMaxDed = false;
        //            isPercentDed = false;
        //            isFranchiseDed = false;
        //            GraphNode[] preLevelNodeArr = allLevelNodesList[i - 1];
        //            preLevelSize = preLevelNodeArr.Length;

        //            levelNodePartition = new int[preLevelSize];
        //            //buildingNum = new int[preLevelSize];
        //            List<GraphNode> thisLevelNodes = new List<GraphNode>();

        //            minDedList = new List<float>();
        //            maxDedList = new List<float>();
        //            maxFranchiseList = new List<bool>();
        //            minFranchiseList = new List<bool>();
        //            limitList = new List<float>();
        //            levelPartitionList = new List<int>();

        //            float minD;
        //            float maxD;
        //            float limit;

        //            for (int k = 0; k < preLevelSize; k++) //for each of the parent node
        //            {
        //                GraphNode pNode = preLevelNodeArr[k];
        //                TermNode ptNode = pNode as TermNode;
        //                HashSet<GraphNode> childrenSet = new HashSet<GraphNode>();
        //                List<GraphNode> childrenList = new List<GraphNode>();
        //                if (PCMap.ContainsKey(pNode))
        //                {
        //                    childrenSet.UnionWith(PCMap[pNode]);
        //                    childrenSet.IntersectWith(nodesByLevel[i]);
        //                    childrenList = childrenSet.ToList();
        //                    thisLevelNodes.AddRange(childrenList);
        //                }
        //                levelNodePartition[k] = thisLevelNodes.Count();

        //                if (ptNode.IsPerRisk) //assume all chidlren have the same number of buildings
        //                {
        //                    int origNumBuildings = ptNode.GetNumOfBuildingsOrigin();
        //                    int numBuildings = ptNode.GetNumOfBuildings();
        //                    double[] multiArr = GenerateMultiplierArr(origNumBuildings);

        //                    for (int kk = 0; kk < numBuildings; kk++)
        //                    {
        //                        foreach (GraphNode node in childrenList)
        //                        {
        //                            TermNode tNode = node as TermNode;
        //                            tNode.Deductibles.GetMaxDed(out maxDed, out maxDedFlag, out maxFranchised);
        //                            tNode.Deductibles.GetMinDed(out minDed, out minFranchised);

        //                            if (maxDed > 0)
        //                                isMaxDed = true;
        //                            if (maxFranchised == true || minFranchised == true)
        //                                isFranchiseDed = true;

        //                            minDedList.Add(minDed * (float)multiArr[kk]);
        //                            maxDedList.Add(maxDed * (float)multiArr[kk]);
        //                            minFranchiseList.Add(minFranchised);
        //                            maxFranchiseList.Add(maxFranchised);
        //                            limitList.Add((float)tNode.Limits.GetLimit() * (float)multiArr[kk]);
        //                        }
        //                        levelPartitionList.Add(minDedList.Count());
        //                    }
        //                }
        //                else //pNode is not PerRisk
        //                {
        //                    foreach (GraphNode node in childrenList)
        //                    {
        //                        TermNode tNode = node as TermNode;
        //                        tNode.Deductibles.GetMaxDed(out maxDed, out maxDedFlag, out maxFranchised);
        //                        tNode.Deductibles.GetMinDed(out minDed, out minFranchised);

        //                        if (maxDed > 0)
        //                            isMaxDed = true;
        //                        if (maxFranchised == true || minFranchised == true)
        //                            isFranchiseDed = true;

        //                        limit = (float)tNode.Limits.GetLimit();

        //                        if (tNode.IsPerRisk)
        //                        {
        //                            int origNumBuildings = ptNode.GetNumOfBuildingsOrigin();
        //                            int numBuildings = ptNode.GetNumOfBuildings();
        //                            double[] multiArr = GenerateMultiplierArr(origNumBuildings);

        //                            for (int kk = 0; kk < tNode.GetNumOfBuildings(); kk++)
        //                            {
        //                                minDedList.Add(minDed * (float)multiArr[kk]);
        //                                maxDedList.Add(maxDed * (float)multiArr[kk]);
        //                                minFranchiseList.Add(minFranchised);
        //                                maxFranchiseList.Add(maxFranchised);
        //                                limitList.Add(limit * (float)multiArr[kk]);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            minDedList.Add(minDed);
        //                            maxDedList.Add(maxDed);
        //                            minFranchiseList.Add(minFranchised);
        //                            maxFranchiseList.Add(maxFranchised);
        //                            limitList.Add(limit);
        //                        }
        //                    }
        //                    levelPartitionList.Add(minDedList.Count());
        //                }  //pNode is not PerRisk
        //            }
        //            allLevelNodesList.Add(i, thisLevelNodes.ToArray());
        //            allLevelNodePartitionList.Add(i, levelNodePartition);
        //            allLevelPartitionList.Add(i, levelPartitionList.ToArray());
        //            allLevelMinDeds.Add(i, minDedList.ToArray());
        //            allLevelMaxDeds.Add(i, maxDedList.ToArray());
        //            allLevelMinFranchise.Add(i, minFranchiseList.ToArray());
        //            allLevelMaxFranchise.Add(i, maxFranchiseList.ToArray());
        //            allLevelLimits.Add(i, limitList.ToArray());
        //            allLevelIsMaxDed.Add(i, isMaxDed);
        //            allLevelIsFranchiseDed.Add(i, isFranchiseDed);

        //            allLevelIsPercentDed.Add(i, isPercentDed);
        //        }  //done with TermLevelInfor


        //        //-------------------------------------------------
        //        //then add AtomicRites, start from lowest level
        //        //this lowest level is (numOfTermLevel + 1) and are all AtomicRiteLevel            
        //        List<int> thisLevelAtomicRITEsIndex = new List<int>();
        //        int level = numOfNodeLevels;  //bottom level, the level without any nodes
        //        //int partitionNum = 0;

        //        GraphNode[] preLevelNodes = allLevelNodesList[numOfNodeLevels - 1];
        //        preLevelSize = preLevelNodes.Length;

        //        //int[] thisLevelAtomicRitePartition;
        //        List<int> thisLevelAtomicRitePartitionList = new List<int>();

        //        int partIndex = 0;

        //        for (int i = 0; i < preLevelSize; i++)
        //        {
        //            GraphNode node = allLevelNodesList[level - 1][i];
        //            if (node is TermNode)
        //            {
        //                TermNode tNode = node as TermNode;

        //                if (tNode.IsPerRisk && tNode.GetNumOfBuildings() > 1)
        //                {
        //                    for (int ii = 0; ii < tNode.GetNumOfBuildings(); ii++)
        //                    {
        //                        //do special
        //                        thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject, ii).ToList());
        //                        thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
        //                        partIndex++;
        //                    }
        //                }
        //                else
        //                {
        //                    for (int ii = 0; ii < tNode.GetNumOfBuildingsActual(); ii++)
        //                    {
        //                        //thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject, tNode.GetNumOfBuildings()).ToList());
        //                        thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject, ii).ToList());
        //                    }
        //                    thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
        //                    partIndex++;
        //                }
        //            }
        //        }
        //        allLevelAtomicRiteIndexList.Add(numOfNodeLevels, thisLevelAtomicRITEsIndex.ToArray());
        //        allLevelAtomicRITEPartitionList.Add(numOfNodeLevels, thisLevelAtomicRitePartitionList.ToArray());

        //        //then for each upper level
        //        for (level = numOfNodeLevels - 1; level > 0; level--)
        //        {
        //            partIndex = 0;
        //            preLevelNodes = allLevelNodesList[level - 1];
        //            preLevelSize = preLevelNodes.Length;
        //            thisLevelAtomicRITEsIndex = new List<int>();
        //            thisLevelAtomicRitePartitionList = new List<int>();

        //            for (int i = 0; i < preLevelSize; i++)
        //            {
        //                GraphNode node = preLevelNodes[i];
        //                if (node is TermNode)
        //                {
        //                    TermNode tNode = node as TermNode;
        //                    if (tNode.IsPerRisk && tNode.GetNumOfBuildings() > 1)
        //                    {
        //                        //do special
        //                        for (int ii = 0; ii < tNode.GetNumOfBuildings(); ii++)
        //                        {
        //                            HashSet<int> allRites = GetAtomicRitesIndexForSubject(node.Subject, ii);
        //                            HashSet<int> childrenRites = new HashSet<int>();

        //                            int tempHolder = 0;
        //                            for (int k = tempHolder; k <= allLevelNodePartitionList[level][i] - 1; k++)
        //                            {
        //                                childrenRites.UnionWith(GetAtomicRitesIndexForSubject(allLevelNodesList[level][k].Subject, ii));
        //                                tempHolder = allLevelNodePartitionList[level][i];
        //                            }
        //                            //find Residual atomicRite
        //                            allRites.ExceptWith(childrenRites);
        //                            thisLevelAtomicRITEsIndex.AddRange(allRites.ToList());
        //                            thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
        //                            partIndex++;
        //                        }
        //                    }
        //                    else if (!tNode.IsPerRisk && tNode.GetNumOfBuildingsActual() > 1)
        //                    {
        //                        //do special
        //                        for (int ii = 0; ii < tNode.GetNumOfBuildingsActual(); ii++)
        //                        {
        //                            HashSet<int> allRites = GetAtomicRitesIndexForSubject(node.Subject, ii);
        //                            HashSet<int> childrenRites = new HashSet<int>();

        //                            int tempHolder = 0;
        //                            for (int k = tempHolder; k <= allLevelNodePartitionList[level][i] - 1; k++)
        //                            {
        //                                childrenRites.UnionWith(GetAtomicRitesIndexForSubject(allLevelNodesList[level][k].Subject, ii));
        //                                tempHolder = allLevelNodePartitionList[level][i];
        //                            }
        //                            //find Residual atomicRite
        //                            allRites.ExceptWith(childrenRites);
        //                            thisLevelAtomicRITEsIndex.AddRange(allRites.ToList());
        //                        }
        //                        thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
        //                        partIndex++;
        //                    }
        //                    else
        //                    {
        //                        for (int ii = 0; ii < tNode.GetNumOfBuildings(); ii++)
        //                        {
        //                            HashSet<int> allRites = GetAtomicRitesIndexForSubject(node.Subject, ii);
        //                            HashSet<int> childrenRites = new HashSet<int>();

        //                            int tempHolder = 0;
        //                            for (int k = tempHolder; k <= allLevelNodePartitionList[level][i] - 1; k++)
        //                            {
        //                                childrenRites.UnionWith(GetAtomicRitesIndexForSubject(allLevelNodesList[level][k].Subject, ii));
        //                                tempHolder = allLevelNodePartitionList[level][i];
        //                            }
        //                            //find Residual atomicRite
        //                            allRites.ExceptWith(childrenRites);
        //                            thisLevelAtomicRITEsIndex.AddRange(allRites.ToList());
        //                        }
        //                        thisLevelAtomicRitePartitionList.Add(thisLevelAtomicRITEsIndex.Count());
        //                        partIndex++;
        //                    }
        //                }
        //            }

        //            allLevelAtomicRiteIndexList.Add(level, thisLevelAtomicRITEsIndex.ToArray());
        //            allLevelAtomicRITEPartitionList.Add(level, thisLevelAtomicRitePartitionList.ToArray());
        //        }

        //        //level 0: the top level should have no AtomicRiteLevel
        //        //TODO: we need default empty Level infor.
        //        allLevelAtomicRiteIndexList.Add(0, new List<int>().ToArray());
        //        allLevelAtomicRITEPartitionList.Add(0, new int[1]);

        //        if (IsOverlapByAtomicRites(allLevelAtomicRiteIndexList))
        //            throw new InvalidOperationException("Overlap detected by AtomicRITE");

        //        lowestAtomicRiteLevel = new AtomicRITELevel(allLevelAtomicRITEPartitionList[numOfNodeLevels], allLevelAtomicRiteIndexList[numOfNodeLevels].Count(),
        //                                                                       allLevelAtomicRiteIndexList[numOfNodeLevels]);

        //        graph.SetLowestARITELevel(lowestAtomicRiteLevel);

        //        for (int i = 0; i < numOfNodeLevels; i++)
        //        {
        //            //TermLevel thisTermLevel = new TermLevel(allLevelNodesList[i].Length, allLevelAtomicRiteIndexList[i].Length, allLevelIsMaxDed[i], allLevelIsPercentDed[i],
        //            //                                         allLevelMinDeds[i], allLevelMaxDeds[i], allLevelLimits[i], allLevelPartitionList[i], allLevelAtomicRITEPartitionList[i], allLevelAtomicRiteIndexList[i]);
        //            TermLevel thisTermLevel = new TermLevel(allLevelMinDeds[i].Length, allLevelAtomicRiteIndexList[i].Length, allLevelIsMaxDed[i], allLevelIsPercentDed[i],
        //                                                     allLevelMinDeds[i], allLevelMaxDeds[i], allLevelLimits[i], allLevelPartitionList[i],
        //                                                     allLevelAtomicRITEPartitionList[i], allLevelAtomicRiteIndexList[i], allLevelIsFranchiseDed[i], allLevelMinFranchise[i], allLevelMaxFranchise[i]);

        //            graph.AddTermLevel(i, thisTermLevel);
        //        }
        //    }
        //    else
        //    {
        //        numOfNodeLevels = 1;

        //        Subject topCoverSubject = Contract.GetAllCoverSubjects()[0];
        //        HashSet<AtomicRITE> Arites = topCoverSubject.GetAtomicRites();
        //        int NumOfArites = Arites.Count();
        //        int[] partitions = new int[] { NumOfArites };

        //        List<int> thisLevelAtomicRITEsIndex = new List<int>();

        //        for (int ii = 0; ii < NumOfArites; ii++)
        //        {
        //            //thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject, tNode.GetNumOfBuildings()).ToList());
        //            thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(topCoverSubject, ii).ToList());
        //        }

        //        lowestAtomicRiteLevel = new AtomicRITELevel(partitions, NumOfArites, thisLevelAtomicRITEsIndex.ToArray());

        //        graph.SetLowestARITELevel(lowestAtomicRiteLevel);
        //    }

        //    Console.WriteLine(" graph end at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));
        //    return (IExecutableMatrixGraph)graph;
        //}


        //public Dictionary<int, HashSet<GraphNode>> SetLevels(Dictionary<GraphNode, HashSet<GraphNode>> CPMap, out int numOfNodeLevels)
        //{
        //    Dictionary<int, HashSet<GraphNode>> nodesByLevel = new Dictionary<int, HashSet<GraphNode>>();
        //    numOfNodeLevels = 0;

        //    foreach (GraphNode node in CPMap.Keys)
        //    {
        //        int numOfParents = CPMap[node].Count();

        //        if (nodesByLevel.ContainsKey(numOfParents))
        //            nodesByLevel[numOfParents].Add(node);
        //        else
        //        {
        //            numOfNodeLevels++;
        //            nodesByLevel.Add(numOfParents, new HashSet<GraphNode> { node });
        //        }
        //    }
        //    return nodesByLevel;
        //}

        //public MatrixGraph GeneralBuild(List<GraphNode> IntitialGraphNodes, List<CoverNode> InitialCoverNodes)  //for all cases
        //{
        //    AutoMatrixGraph graph = new AutoMatrixGraph();

        //    Console.WriteLine(" graph start at: " + DateTime.Now.ToString("h:mm:ss.fff tt"));

        //    //Do Node Tree
        //    NodeTree nodeTree = new NodeTree(IntitialGraphNodes, InitialCoverNodes);
        //    nodeTree.Run();

        //    Dictionary<GraphNode, HashSet<GraphNode>> CPMap = nodeTree.TermChildParentsMap;
        //    Dictionary<GraphNode, HashSet<GraphNode>> PCMap = nodeTree.TermParentChildrenMap;
        //    Dictionary<int, GraphNode[]> nodesByLevel = new Dictionary<int, GraphNode[]>();

        //    int numOfChildren = 0;
        //    List<GraphNode> tempThisLevelNodeHolder = new List<GraphNode>();

        //    foreach (GraphNode node in CPMap.Keys)
        //    {
        //        if (CPMap[node].Count() == 0)
        //        {
        //            tempThisLevelNodeHolder.Add(node);
        //            numOfChildren += PCMap[node].Count();
        //        }
        //    }
        //    nodesByLevel.Add(0, tempThisLevelNodeHolder.ToArray());

        //    int partitionNum = 0;
        //    int numOfLevel = 0;

        //    while (numOfChildren > 0)
        //    {
        //        //re-initialize
        //        partitionNum = 0;
        //        numOfChildren = 0;
        //        tempThisLevelNodeHolder = new List<GraphNode>();
        //        GraphNode[] preLevelNodeArr = nodesByLevel[numOfLevel];
        //        int[] thisLevelPartition = new int[preLevelNodeArr.Length];

        //        for (int i = 0; i < preLevelNodeArr.Length; i++) //foreach preLevel Node
        //        {
        //            HashSet<GraphNode> cSet = PCMap[preLevelNodeArr[i]];
        //            foreach (GraphNode child in cSet)
        //            {
        //                //check if this child should be the immedinate child is to check if any of the child's parent is in the children's list
        //                HashSet<GraphNode> pSet = CPMap[child];
        //                if (!pSet.Overlaps(cSet))  //then immedinate child
        //                {
        //                    tempThisLevelNodeHolder.Add(child);
        //                    partitionNum++;
        //                    numOfChildren += PCMap[child].Count();
        //                }
        //            }
        //            thisLevelPartition[i] = partitionNum;
        //        }
        //        if (tempThisLevelNodeHolder.Count() > 0)
        //        {
        //            numOfLevel++;
        //            nodesByLevel.Add(numOfLevel, tempThisLevelNodeHolder.ToArray());
        //        }
        //    } //while loop

        //    return graph;
        //}

        //public bool IsOverlappedByTermNodes(Dictionary<GraphNode, HashSet<GraphNode>> CPMap)
        //{
        //    foreach (GraphNode node in CPMap.Keys)
        //    {
        //        List<GraphNode> pList = CPMap[node].ToList();
        //        for (int i = 0; i < pList.Count(); i++)
        //        {
        //            GraphNode node1 = pList[i];
        //            for (int j = i + 1; j < pList.Count(); j++)
        //            {
        //                GraphNode node2 = pList[j];
        //                if (!CPMap[node1].Contains(node2) && !CPMap[node2].Contains(node1))
        //                    return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        #endregion

        public Dictionary<int, HashSet<TermNode>> SetRootTermLevel(Dictionary<TermNode, HashSet<TermNode>> CPMap)
        {
            //this CPMap is Transitively resuced
            Dictionary<int, HashSet<TermNode>> nodesByLevel = new Dictionary<int, HashSet<TermNode>>();
            
            foreach (TermNode node in CPMap.Keys)
            {
                int numOfParents = CPMap[node].Count();

                if (numOfParents == 0)
                {
                    if (nodesByLevel.ContainsKey(numOfParents))
                        nodesByLevel[numOfParents].Add(node);
                    else
                    {                        
                        nodesByLevel.Add(numOfParents, new HashSet<TermNode> { node });
                    }
                }
            }
            return nodesByLevel;
        }


        public bool IsOverlapByAtomicRites(Dictionary<int, int[]> allLevelAtomicRiteIndexList)
        {
            HashSet<int> allRites = new HashSet<int>();
            int counter = 0;

            foreach (int level in allLevelAtomicRiteIndexList.Keys)
            {
                HashSet<int> tempRites = new HashSet<int>(allLevelAtomicRiteIndexList[level]);
                counter += tempRites.Count();
                allRites.UnionWith(tempRites);
                if (allRites.Count() < counter)  //means there are duplicate RITEs
                    return true;
            }
            return false;
        }

        public HashSet<int> GetAtomicRitesIndexForSubject(Subject sub, int numBuildings, out Dictionary<int, int> allRitesFactorsIndexDict)
        {
            allRitesFactorsIndexDict = new Dictionary<int, int>();
            HashSet<int> RiteIndex = new HashSet<int>();
            HashSet<AtomicRITE> tempSet = sub.GetAtomicRites();
            
            foreach (AtomicRITE aRite in tempSet)
            {
                CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
                //Modifyied by Sunny to produce only first building loss
                int riteIndex = indexMapper.GetMappedIndex(cRite.RITCharacterisiticID, numBuildings, cRite.SubPeril);
                //int riteFactorsIndex = indexMapper.GetMappedFactorIndex(cRite.RITE.ExposureID, numBuildings); //TODO: should be RiteId??
                int riteFactorsIndex = indexMapper.GetMappedFactorIndex(cRite.RITCharacterisiticID, numBuildings); //TODO: should be RiteId??
                RiteIndex.Add(riteIndex);
                allRitesFactorsIndexDict.Add(riteIndex, riteFactorsIndex);
            }
            return RiteIndex;
        }

        public int GetAtomicRitesIndexForSubject(CoverageAtomicRITE cRite, int numBuildings, out Dictionary<int, int> allRitesFactorsIndexDict)
        {
            allRitesFactorsIndexDict = new Dictionary<int, int>();
            int riteIndex = indexMapper.GetMappedIndex(cRite.RITCharacterisiticID, numBuildings, cRite.SubPeril);
            int riteFactorsIndex = indexMapper.GetMappedFactorIndex(cRite.RITCharacterisiticID, numBuildings); //TODO: should be RiteId??
            allRitesFactorsIndexDict.Add(riteIndex, riteFactorsIndex);
            return riteIndex;
        }

        public int GetAtomicRitesIndexForSubject(CoverageAtomicRITE cRite, int numBuildings)
        {
            return indexMapper.GetMappedIndex(cRite.RITCharacterisiticID, numBuildings, cRite.SubPeril);
        }

        public static double[] GenerateMultiplierArr(int numOfBldgs)
        {
            int MaxNumOfBldgs = 250;
            double[] multiplierArr;
            if (numOfBldgs <= MaxNumOfBldgs)
            {
                multiplierArr = new double[numOfBldgs];
                for (int i = 1; i <= numOfBldgs; i++)
                {
                    multiplierArr[i - 1] = 1;
                }
            }
            else
            {
                multiplierArr = new double[MaxNumOfBldgs];
                Double temp = (double)(numOfBldgs / MaxNumOfBldgs);
                int n = (int)(Math.Floor(temp));
                int m = numOfBldgs - n * MaxNumOfBldgs;
                for (int i = 1; i <= m; i++)
                {
                    multiplierArr[i - 1] = n + 1;
                }
                for (int i = m + 1; i <= MaxNumOfBldgs; i++)
                {
                    multiplierArr[i - 1] = n;
                }
            }
            return multiplierArr;
        }

        public static double[] GenerateMultiplierArr_new(int numOfBldgs)
        {
            int MaxNumOfBldgs = 250;
            double[] multiplierArr;
            if (numOfBldgs <= MaxNumOfBldgs)
            {
                multiplierArr = new double[numOfBldgs];
                for (int i = 1; i <= numOfBldgs; i++)
                {
                    multiplierArr[i - 1] = 1;
                }
            }
            else
            {
                multiplierArr = new double[MaxNumOfBldgs];
                Double temp = (double)(numOfBldgs / MaxNumOfBldgs);

                for (int i = 1; i <= MaxNumOfBldgs; i++)
                {
                    multiplierArr[i - 1] = temp;
                }

            }
            return multiplierArr;
        }
    }

    //public class ARiteLocationTuple
    //{
    //    public int Level { get; set; }
    //    public int index { get; set; }

    //    public ARiteLocationTuple(int whichLevel, int whichIndex)
    //    {
    //        Level = whichLevel;
    //        index = whichIndex;
    //    }
    //}
}
