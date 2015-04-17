using System; 
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RMS.ContractObjectModel;
using RMS.ContractGraphModel;

namespace RMS.Prototype.NGFM
{
    public static class ContractGraphBuilder
    {
        public static TreatyContractGraph BuildTreatyContractGraph(Contract _Treaty)
        {
            TreatyContractGraph _TreatyGraph = new TreatyContractGraph(_Treaty);

            _TreatyGraph.AddCovers(_Treaty.Covers);

            _TreatyGraph.RebuildCoverGraph();

            return _TreatyGraph;
        }

        /// <summary>
        /// ALERT: This method may change the contents of <code>ResolvedSchedule</code> via explosion of PerRisk schedules
        /// </summary>
        /// <param name="_Contract"></param>
        /// <param name="ResolvedSchedule"></param>
        /// <returns></returns>
        public static PrimaryContractGraph BuildPrimaryContractGraph(Contract _Primary, 
            Dictionary<long, RiskItemCharacteristicIDAttributes> ExposureIDAttributeMap, 
            Dictionary<string, HashSet<long>> ResolvedSchedule)
        {
            PrimaryContractGraph _PrimaryGraph = new PrimaryContractGraph(_Primary);

            _PrimaryGraph.AddCovers(_Primary.Covers, ExposureIDAttributeMap, ResolvedSchedule);

            _PrimaryGraph.RebuildCoverGraph();

            _PrimaryGraph.AddTerms(_Primary.Sublimits, ExposureIDAttributeMap, ResolvedSchedule);

            _PrimaryGraph.AddTerms(_Primary.Deductibles, ExposureIDAttributeMap, ResolvedSchedule);

            _PrimaryGraph.RebuildTermGraph();

            // If there is no need to allocate, then try to create subject exposures for cover leafs using term roots
            _PrimaryGraph.TryCreateSubjectExposureForCoverLeafsFromTermRoots();

            return _PrimaryGraph;
        }
    }
}
