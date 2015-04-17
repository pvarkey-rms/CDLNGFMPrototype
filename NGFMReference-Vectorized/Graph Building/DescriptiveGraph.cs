using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class DescriptiveGraph
    {
        //foundermental fields to describe an abstract graph
        public Dictionary<TermNode, HashSet<TermNode>> TermGraph  {get; private set;} //all term nodes parent-children relationship
        public Dictionary<CoverNode, HashSet<CoverNode>> CoverGraph { get; private set; } //all cover nodes parent-children relationship
        public Dictionary<CoverNode, CoverNodeRecoverableSubject> CoverGraphTermGraphMapping { get; private set; }  //TODO: not implemented
        public Boolean TermNodesIsOverlapped { get; private set; }  //this is only for term nodes overlap, NO AtomicRites overlap

        //not necessary infor, but convenient infor for translating to executible graph
        public Dictionary<TermNode, HashSet<TermNode>> TermGraphChildParentsMapping { get; private set; }
        public HashSet<CoverNode> AllLeafCoverNodes { get; private set; }
        public HashSet<CoverNode> AllDerivedCoverNodes { get; private set; }
        public Dictionary<CoverNode, HashSet<CoverNode>> CoverGraphChildParentsMapping { get; private set; }               

        public DescriptiveGraph(Dictionary<TermNode, HashSet<TermNode>> termGraph, 
                                Dictionary<CoverNode, HashSet<CoverNode>> coverGraph, 
                                Dictionary<CoverNode, CoverNodeRecoverableSubject> coverGraphTermGraphMapping,
                                Boolean termNodesIsOverlapped)
        {
            TermGraph = termGraph;
            CoverGraph = coverGraph;
            CoverGraphTermGraphMapping = coverGraphTermGraphMapping;
            TermNodesIsOverlapped = termNodesIsOverlapped;
        }

        public DescriptiveGraph()
        {
            TermGraph = new Dictionary<TermNode, HashSet<TermNode>>();
            CoverGraph = new Dictionary<CoverNode, HashSet<CoverNode>>();
            CoverGraphTermGraphMapping = new Dictionary<CoverNode, CoverNodeRecoverableSubject>();

        }

        public void SetTermGraphChildParentsMap(Dictionary<TermNode, HashSet<TermNode>> termGraphChildParentsMapping)
        {
            TermGraphChildParentsMapping = termGraphChildParentsMapping;
        }

        public void SetCoverGraphChildParentsMap(Dictionary<CoverNode, HashSet<CoverNode>> coverGraphChildParentsMapping)
        {
            CoverGraphChildParentsMapping = coverGraphChildParentsMapping;
        }


        public void SetLeafCoverNodes(HashSet<CoverNode> allLeafCoverNodes)
        {
            AllLeafCoverNodes = allLeafCoverNodes;        
        }

        public void SetDerivedCoverNodes(HashSet<CoverNode> allDerivedCoverNodes)
        {
            AllDerivedCoverNodes = allDerivedCoverNodes;
        }

        public void SetTermGraph(Dictionary<TermNode, HashSet<TermNode>> termGraph)
        {
            TermGraph = termGraph;
        }

        public void SetCoverGraph(Dictionary<CoverNode, HashSet<CoverNode>> coverGraph)
        {
            CoverGraph = coverGraph;
        }

        public void SetCoverGraphTermGraphMapping(Dictionary<CoverNode, CoverNodeRecoverableSubject> coverGraphTermGraphMapping)
        {
            CoverGraphTermGraphMapping = coverGraphTermGraphMapping;
        }

    }

    public class CoverNodeRecoverableSubject
    {
        public HashSet<AtomicRITE> ARites { get; private set; }
        public HashSet<TermNode> TNodes { get; private set; }

        public CoverNodeRecoverableSubject(HashSet<AtomicRITE> aRites, HashSet<TermNode> tNodes)
        {
            ARites = aRites;
            TNodes = tNodes;
        }
    }
}
