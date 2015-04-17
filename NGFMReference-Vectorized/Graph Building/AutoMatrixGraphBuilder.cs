using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    //public class AutoMatrixGraphBuilder : IMatrixTermGraphBuilder
    //{
    //    private ExposureDataAdaptor expData;
    //    //private GraphBuildCache graphChache;
    //    private RITEmapper1 indexMapper;

    //    public AutoMatrixGraphBuilder(NGFMReference.ExposureDataAdaptor _expData) //, NGFMReference.GraphBuildCache _graphChache) : IMatrixTermGraphBuilder
    //    {
    //        expData = _expData;
    //        ISubPerilConfig subperilInfo = new RMSSubPerilConfig();
    //        indexMapper = new RITEmapper1(expData, subperilInfo);
    //        //graphChache = _graphChache;
    //    }

    //    public IBuildableMatrixGraph NonOverlapBuild(List<TermNode> InitialTermNodes)
    //    {
    //        //transfer the TermNode to GraphNode
    //        List<GraphNode> InitialGraphNodes = new List<GraphNode>();
    //        for (int i = 0; i < InitialTermNodes.Count(); i++)
    //        {
    //            GraphNode gNode = InitialTermNodes[i] as GraphNode;
    //            InitialGraphNodes.Add(gNode);
    //        }

    //        //these are all output, will write to graph object at the end
    //        Dictionary<int, GraphNode[]> allLevelNodesList = new Dictionary<int, GraphNode[]>();
    //        Dictionary<int, int[]> allLevelNodePartitionList = new Dictionary<int, int[]>();

    //        Dictionary<int, int[]> allLevelAtomicRiteIndexList = new Dictionary<int, int[]>();
    //        Dictionary<int, int[]> allLevelAtomicRITEPartitionList = new Dictionary<int, int[]>();                   

    //        Dictionary<int, bool> allLevelIsMaxDed = new Dictionary<int, bool>();
    //        Dictionary<int, bool> allLevelIsPercentDed = new Dictionary<int, bool>();
    //        Dictionary<int, float[]> allLevelMinDeds = new Dictionary<int, float[]>();
    //        Dictionary<int, float[]> allLevelMaxDeds = new Dictionary<int, float[]>();
    //        Dictionary<int, float[]> allLevelLimits= new Dictionary<int, float[]>();
           
    //        IBuildableMatrixGraph graph = new AutoMatrixGraph();
            
    //        Console.WriteLine("graph start at: " + DateTime.Now.ToString("h:mm:ss tt"));

    //        //Do Node Tree
    //        NodeTree nodeTree = new NodeTree(InitialGraphNodes);
    //        nodeTree.Run();
    //        Dictionary<GraphNode, HashSet<GraphNode>> CPMap = nodeTree.ChildParentsMap;
    //        Dictionary<GraphNode, HashSet<GraphNode>> PCMap = nodeTree.ParentChildrenMap;
            
    //        //check if there is overlap in among the Nodes
    //        //overlap logic: among a node's all parents, if the parents are not parent-child of each other, then overlap.
    //       if (IsOverlapByNodes(CPMap))
    //           throw new InvalidOperationException("Overlap detected by nodes");

    //        //if not overlap, then we can use the following logic            
    //        Dictionary<int, HashSet<GraphNode>> nodesByLevel = new Dictionary<int, HashSet<GraphNode>>();

    //        //find the levels
    //        int numOfNodeLevels = 0;       
    //        nodesByLevel = SetLevels(CPMap, out numOfNodeLevels);

    //        //then Do it for each level
                        
    //        //Top level: Level 0
    //        bool isMaxDed = false; //TODO: for now 
    //        bool isPercentDed = false; //TODO: for now
    //        float[] minDed = new float[nodesByLevel[0].Count()];
    //        float[] maxDed = new float[nodesByLevel[0].Count()];
    //        float[] limit = new float[nodesByLevel[0].Count()];
            
    //        GraphNode[] thisLevelNodeArr = new GraphNode[nodesByLevel[0].Count()];
    //        int[] levelPartition = new int[1];

    //        int j = 0;
    //        TermNode termNode;
    //        CoverNode coverNode;
    //        foreach (GraphNode node in nodesByLevel[0])
    //        {
    //            if (node is TermNode)
    //            {
    //                termNode = node as TermNode;
    //                minDed[j] = (float)termNode.Deductibles.GetMinDed();
    //                maxDed[j] = (float)termNode.Deductibles.GetMaxDed();
    //                limit[j] = (float)termNode.Limits.GetLimit();
    //            }
    //            else
    //            {
    //                coverNode = node as CoverNode;
    //            }
    //            thisLevelNodeArr[j] = node;
    //            j++;                                   
    //        }
    //        levelPartition[0] = nodesByLevel[0].Count();
    //        allLevelNodesList.Add(0, thisLevelNodeArr);
    //        allLevelNodePartitionList.Add(0, levelPartition);
    //        allLevelIsMaxDed.Add(0, isMaxDed);
    //        allLevelIsPercentDed.Add(0, isPercentDed);
    //        allLevelMinDeds.Add(0, minDed);
    //        allLevelMaxDeds.Add(0, maxDed);
    //        allLevelLimits.Add(0, limit);

    //        int preLevelSize;
    //        //for each lower level      
    //        for (int i = 1; i < numOfNodeLevels; i++)
    //        {                                
    //            GraphNode[] preLevelNodeArr = allLevelNodesList[i - 1];
    //            preLevelSize = preLevelNodeArr.Length;

    //            levelPartition = new int[preLevelSize];                                                           
    //            List<GraphNode> thisLevelNodes = new List<GraphNode>();

    //            for (int k = 0; k < preLevelSize; k++) //for each of the parent node
    //            { 
    //                GraphNode pNode = preLevelNodeArr[k];
    //                HashSet<GraphNode> childrenSet = new HashSet<GraphNode>();
    //                if (PCMap.ContainsKey(pNode))
    //                {
    //                    childrenSet.UnionWith(PCMap[pNode]);                        
    //                    childrenSet.IntersectWith(nodesByLevel[i]);
    //                    thisLevelNodes.AddRange(childrenSet.ToList());
    //                }              
    //                levelPartition[k] = thisLevelNodes.Count();                                          
    //            }               

    //            minDed = new float[thisLevelNodes.Count()];
    //            maxDed = new float[thisLevelNodes.Count()];
    //            limit = new float[thisLevelNodes.Count()];

    //            for (int jj = 0; jj < thisLevelNodes.Count(); jj++)
    //            { 
    //                GraphNode node = thisLevelNodes[jj];
    //                if (node is TermNode)
    //                {
    //                    termNode = node as TermNode;
    //                    minDed[jj] = (float)termNode.Deductibles.GetMinDed();
    //                    maxDed[jj] = (float)termNode.Deductibles.GetMaxDed();
    //                    limit[jj] = (float)termNode.Limits.GetLimit();
    //                }
    //                else 
    //                {
    //                    coverNode = node as CoverNode;                    
    //                }                
    //            }

    //            allLevelNodesList.Add(i, thisLevelNodes.ToArray());
    //            allLevelNodePartitionList.Add(i, levelPartition);
    //            allLevelMinDeds.Add(i, minDed);
    //            allLevelMaxDeds.Add(i, maxDed);
    //            allLevelLimits.Add(i, limit);
    //            allLevelIsMaxDed.Add(i, isMaxDed);
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

    //        int[] thisLevelAtomicRitePartition = new int[preLevelSize];

    //        for (int i = 0; i < preLevelSize; i++)
    //        {
    //            GraphNode node = allLevelNodesList[level - 1][i];
    //            thisLevelAtomicRITEsIndex.AddRange(GetAtomicRitesIndexForSubject(node.Subject).ToList());
    //            thisLevelAtomicRitePartition[i] = thisLevelAtomicRITEsIndex.Count();                
    //        }
    //        allLevelAtomicRiteIndexList.Add(numOfNodeLevels, thisLevelAtomicRITEsIndex.ToArray());
    //        allLevelAtomicRITEPartitionList.Add(numOfNodeLevels, thisLevelAtomicRitePartition);

    //        //then for each upper level
    //        for (level = numOfNodeLevels - 1; level > 0; level--)
    //        {
    //            preLevelNodes = allLevelNodesList[level - 1];
    //            preLevelSize = preLevelNodes.Length;
    //            thisLevelAtomicRITEsIndex = new List<int>();
    //            thisLevelAtomicRitePartition = new int[preLevelSize];

    //            for (int i = 0; i < preLevelSize; i++)
    //            {
    //                GraphNode node = preLevelNodes[i];
    //                HashSet<int> allRites = GetAtomicRitesIndexForSubject(node.Subject);
    //                HashSet<int> childrenRites = new HashSet<int>();

    //                int tempHolder = 0;
    //                for (int k = tempHolder; k <= allLevelNodePartitionList[level][i] - 1; k++)
    //                {
    //                    childrenRites.UnionWith(GetAtomicRitesIndexForSubject(allLevelNodesList[level][k].Subject));                                                        
    //                    tempHolder = allLevelNodePartitionList[level][i];
    //                }
    //                //find Residual atomicRite
    //                allRites.ExceptWith(childrenRites);
    //                thisLevelAtomicRITEsIndex.AddRange(allRites.ToList());
    //                thisLevelAtomicRitePartition[i] = thisLevelAtomicRITEsIndex.Count();
    //            }
    //            allLevelAtomicRiteIndexList.Add(level, thisLevelAtomicRITEsIndex.ToArray());
    //            allLevelAtomicRITEPartitionList.Add(level, thisLevelAtomicRitePartition);
    //        }

    //        //level 0: the top level should have no AtomicRiteLevel
    //        //TODO: we need default empty Level infor.
    //        allLevelAtomicRiteIndexList.Add(0, new List<int>().ToArray());
    //        allLevelAtomicRITEPartitionList.Add(0, new int[1]);

    //        if (IsOverlapByAtomicRites(allLevelAtomicRiteIndexList))
    //            throw new InvalidOperationException("Overlap detected by AtomicRITE");

    //        AtomicRITELevel lowestAtomicRiteLevel = new AtomicRITELevel(allLevelAtomicRITEPartitionList[numOfNodeLevels], allLevelAtomicRITEPartitionList[numOfNodeLevels].Count(), 
    //                                                                       allLevelAtomicRiteIndexList[numOfNodeLevels]);
    //        graph.SetLowestARITELevel(lowestAtomicRiteLevel);

    //        for (int i = 0; i < numOfNodeLevels; i++)
    //        {
    //            TermLevel thisTermLevel = new TermLevel(allLevelNodesList[i].Length, allLevelAtomicRiteIndexList[i].Length, allLevelIsMaxDed[i], allLevelIsPercentDed[i], 
    //                                                     allLevelMinDeds[i], allLevelMaxDeds[i], allLevelLimits[i], allLevelNodePartitionList[i], allLevelAtomicRITEPartitionList[i], allLevelAtomicRiteIndexList[i]);                                
    //            graph.AddTermLevel(i, thisTermLevel);
    //        }
                 
    //        Console.WriteLine(" graph end at: " + DateTime.Now.ToString("h:mm:ss tt"));
    //        return graph;
    //    }


    //    public Dictionary<int, HashSet<GraphNode>> SetLevels(Dictionary<GraphNode, HashSet<GraphNode>> CPMap, out int numOfNodeLevels)
    //    {
    //        Dictionary<int, HashSet<GraphNode>> nodesByLevel = new Dictionary<int, HashSet<GraphNode>>();
    //        numOfNodeLevels = 0; 
      
    //        foreach (GraphNode node in CPMap.Keys)
    //        {
    //            int numOfParents = CPMap[node].Count();

    //            if (nodesByLevel.ContainsKey(numOfParents))
    //                nodesByLevel[numOfParents].Add(node);
    //            else
    //            {
    //                numOfNodeLevels++;
    //                nodesByLevel.Add(numOfParents, new HashSet<GraphNode> { node });
    //            }
    //        }

    //        return nodesByLevel;
    //    }

    //    public MatrixGraph GeneralBuild(List<GraphNode> IntitialGraphNodes)  //for all cases
    //    {
    //        AutoMatrixGraph graph = new AutoMatrixGraph();

    //        Console.WriteLine(" graph start at: " + DateTime.Now.ToString("h:mm:ss tt"));

    //        //Do Node Tree
    //        NodeTree nodeTree = new NodeTree(IntitialGraphNodes);
    //        nodeTree.Run();

    //        Dictionary<GraphNode, HashSet<GraphNode>> CPMap = nodeTree.ChildParentsMap;
    //        Dictionary<GraphNode, HashSet<GraphNode>> PCMap = nodeTree.ParentChildrenMap;
    //        Dictionary<int, GraphNode[]> nodesByLevel = new Dictionary<int, GraphNode[]>();

    //        int numOfChildren = 0;
    //        List<GraphNode> tempThisLevelNodeHolder = new List<GraphNode>();

    //        foreach (GraphNode node in CPMap.Keys)
    //        {
    //            if (CPMap[node].Count() == 0)
    //            {
    //                tempThisLevelNodeHolder.Add(node);
    //                numOfChildren += PCMap[node].Count();
    //            }
    //        }
    //        nodesByLevel.Add(0, tempThisLevelNodeHolder.ToArray());

    //        int partitionNum = 0;
    //        int numOfLevel = 0;       
     
    //        while (numOfChildren > 0)
    //        {
    //            //re-initialize
    //            partitionNum = 0;
    //            numOfChildren = 0;
    //            tempThisLevelNodeHolder = new List<GraphNode>();
    //            GraphNode[] preLevelNodeArr = nodesByLevel[numOfLevel];
    //            int[] thisLevelPartition = new int[preLevelNodeArr.Length];
                                
    //            for (int i = 0; i < preLevelNodeArr.Length; i++) //foreach preLevel Node
    //            {                    
    //                HashSet<GraphNode> cSet = PCMap[preLevelNodeArr[i]];
    //                foreach (GraphNode child in cSet)
    //                {  
    //                    //check if this child should be the immedinate child is to check if any of the child's parent is in the children's list
    //                    HashSet<GraphNode> pSet = CPMap[child];
    //                    if (!pSet.Overlaps(cSet))  //then immedinate child
    //                    {
    //                        tempThisLevelNodeHolder.Add(child);
    //                        partitionNum++;
    //                        numOfChildren += PCMap[child].Count();
    //                    }                                        
    //                }
    //                thisLevelPartition[i] = partitionNum;
    //            }
    //            if (tempThisLevelNodeHolder.Count() > 0)
    //            {
    //                numOfLevel++;
    //                nodesByLevel.Add(numOfLevel, tempThisLevelNodeHolder.ToArray());
    //            }
    //        } //while loop

    //        return graph;
    //    }

    //    public bool IsOverlapByNodes(Dictionary<GraphNode, HashSet<GraphNode>> CPMap)
    //    {
    //        foreach (GraphNode node in CPMap.Keys)
    //        {
    //            List<GraphNode> pList = CPMap[node].ToList();
    //            for (int i = 0; i < pList.Count(); i++)
    //            {
    //                GraphNode node1 = pList[i];
    //                for (int j = i + 1; j < pList.Count(); j++)
    //                {
    //                    GraphNode node2 = pList[j];
    //                    if (!CPMap[node1].Contains(node2) && !CPMap[node2].Contains(node1))
    //                        return true;
    //                }
    //            }
    //        }
    //        return false;            
    //    }

    //    public bool IsOverlapByAtomicRites(Dictionary<int, int[]> allLevelAtomicRiteIndexList)
    //    {
    //        HashSet<int> allRites = new HashSet<int>();
    //        int counter = 0;

    //        foreach (int level in allLevelAtomicRiteIndexList.Keys)
    //        {
    //            HashSet<int> tempRites = new HashSet<int>(allLevelAtomicRiteIndexList[level]);
    //            counter += tempRites.Count();
    //            allRites.UnionWith(tempRites);
    //            if (allRites.Count() < counter)  //means there are duplicate RITEs
    //                return true;            
    //        }
    //        return false;
    //    }

    //    public HashSet<int> GetAtomicRitesIndexForSubject(Subject sub)
    //    {
    //        HashSet<int> RiteIndex = new HashSet<int>();
    //        foreach (AtomicRITE aRite in sub.GetAtomicRites())
    //        {
    //            CoverageAtomicRITE cRite = aRite as CoverageAtomicRITE;
    //            RiteIndex.Add(indexMapper.GetMappedIndex(cRite.RITCharacterisiticID, cRite.RITE.ActNumOfBldgs, cRite.SubPeril));
    //        }
    //        return RiteIndex;
    //    }
      
    //}
}
