using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TreeTransform
{
    public class BinNode
    {
        public int idx;
        public BinCode val;
        public BinCode parents;

        public BinNode(int index)
        {
            idx = index;
            val = new BinCode(idx);
            parents = new BinCode();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int depth = 4;

            #region Example 1:

            var binList = BuildExample(depth, true);
            int count = binList.Count;

            // Algorithm
            binList = GraphBuildingOptimization(binList);

            // Create simple parent tree
            var parentTree = CreateParentTree(binList);

            // Create children tree
            var childrenTree = CreateChildrenTree(parentTree);

            // Print result:
            List<int> roots = parentTree.Where(e => e.Value == null || e.Value.Count == 0).Select(e => e.Key).ToList();
            PrintChildrenTree(childrenTree, roots);

            #endregion

            #region Example 2:

            for (depth = 5; depth < 13; depth++)
            {
                binList = BuildExample(depth);
                count = binList.Count;

                var sw = new Stopwatch();
                sw.Start();
                // Algorithm
                binList = GraphBuildingOptimization(binList);
                sw.Stop();
                Console.WriteLine("Count Nodes: {0}, Elapsed time: {1}", count, sw.Elapsed);
            }

            //Count Nodes: 43    , Elapsed time: 00:00:00.0015934
            //Count Nodes: 165   , Elapsed time: 00:00:00.0007876
            //Count Nodes: 434   , Elapsed time: 00:00:00.0030638
            //Count Nodes: 1287  , Elapsed time: 00:00:00.0179743
            //Count Nodes: 3298  , Elapsed time: 00:00:00.0904313
            //Count Nodes: 7018  , Elapsed time: 00:00:00.3259734
            //Count Nodes: 14353 , Elapsed time: 00:00:01.2424547
            //Count Nodes: 28309 , Elapsed time: 00:00:04.4174325
            //Count Nodes: 59361 , Elapsed time: 00:00:18.5240162
            //Count Nodes: 196556, Elapsed time: 00:03:28.7357814

            #endregion

        }

        public static List<BinNode> BuildExample(int depth, bool show = false)
        {
            var inputDict = new Dictionary<int, BinNode>();
            var parents = new List<int>();
            var r = new Random(999);
            RecursiveBuildExample(ref inputDict, parents, depth, r, show);
            return inputDict.Values.ToList();
        }

        public static void RecursiveBuildExample(ref Dictionary<int, BinNode> inputDict, List<int> parents, int depth, Random r, bool show)
        {
            int idx = inputDict.Count + 1;
            var binNode = new BinNode(idx);
            inputDict.Add(idx, binNode);

            if (show)
                Console.WriteLine("[{0}]->parents:({1})",idx,string.Join(",",parents.ToArray()));

            foreach (int p in parents)
                binNode.parents = binNode.parents + p;

            if (parents.Count < depth)
            {
                var nextParents = new List<int>(parents);
                nextParents.Add(idx);

                for (int k = 0; k < r.Next(2, 4); k++)
                    RecursiveBuildExample(ref inputDict, nextParents, depth, r, show);
            }
        }

        public static List<BinNode> GraphBuildingOptimization(List<BinNode> input)
        {
            var output = new List<BinNode>();
            var mask = new BinCode();
            int count = Int32.MaxValue;

            while (input.Count > 0 && input.Count < count)
            {
                var level = new List<BinNode>();
                var input2 = new List<BinNode>();
                var mask2 = new BinCode();
                count = input.Count;

                foreach (var elem in input)
                {
                    var parents2 = elem.parents - mask;
                    if (parents2.IsEmpty())
                    {
                        level.Add(elem);
                        mask2 += elem.val;
                    }
                    else
                    {
                        elem.parents = parents2;
                        input2.Add(elem);
                    }
                }

                output.AddRange(level);
                input = input2;
                mask = mask2;
            }

            return output;
        }

        public static Dictionary<int, List<int>> CreateParentTree(List<BinNode> binList)
        {
            var parentTree = new Dictionary<int, List<int>>();

            foreach (var node in binList)
            {
                parentTree.Add(node.idx, node.parents.GetElements().ToList());
            }

            return parentTree;
        }

        public static Dictionary<int, List<int>> CreateChildrenTree(Dictionary<int, List<int>> parentTree)
        {
            var childrenTree = new Dictionary<int, List<int>>();

            foreach (var kv in parentTree)
            {
                foreach (int p in kv.Value)
                {
                    if (!childrenTree.ContainsKey(p))
                        childrenTree.Add(p, new List<int>());

                    childrenTree[p].Add(kv.Key);
                }
            }

            return childrenTree;
        }

        public static void PrintChildrenTree(Dictionary<int, List<int>> childrenTree, List<int> roots)
        {
            foreach (int r in roots)
            {
                Console.WriteLine("Tree:");
                PrintRecursive(r, childrenTree, "  ");
            }
        }
        public static void PrintRecursive(int nodeIdx, Dictionary<int, List<int>> childrenTree, string shift = "")
        {
            Console.WriteLine("{0}{1}", shift, nodeIdx);

            if (childrenTree.ContainsKey(nodeIdx) && childrenTree[nodeIdx] != null)
                foreach (int ch in childrenTree[nodeIdx])
                    PrintRecursive(ch, childrenTree, "  " + shift);

        }
    }
}
