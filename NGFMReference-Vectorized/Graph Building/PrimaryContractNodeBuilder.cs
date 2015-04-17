using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HasseManager;
using NGFM.Reference.MatrixHDFM;

namespace NGFMReference
{
    public abstract class ContractNodeBuilder
    {
        protected ContractExtractor Contract;

        public List<CoverNode> GetCoverNodes()
        {
            List<CoverNode> coverNodes = new List<CoverNode>();
            List<Subject> graphSubs = Contract.GetAllCoverSubjects();

            foreach (Subject sub in graphSubs)
            {
                HashSet<Cover> covers;
                if (Contract.GetCoversForSubject(sub, out covers))
                {
                    foreach (Cover cover in covers)
                    {
                        CoverNode node = new CoverNode(sub, cover.CoverName);
                        node.Cover = cover;
                        coverNodes.Add(node);
                    }
                }
            }
            return coverNodes;
        }

        public abstract List<TermNode> GetTermNodes();
    }

    public class PrimaryContractNodeBuilder : ContractNodeBuilder
    {
        protected PrimaryContractExtractor PrimaryContract { get { return Contract as PrimaryContractExtractor; } }
        
        public PrimaryContractNodeBuilder(PrimaryContractExtractor _contract)
        {
            Contract = _contract;           
        }

        public override List<TermNode> GetTermNodes()
        {
            List<TermNode> termNodes = new List<TermNode>();
            List<PrimarySubject> graphSubs = PrimaryContract.GetAllTermSubjects();

            foreach (PrimarySubject sub in graphSubs)
            {
                TermNode node = new TermNode(sub);

                DeductibleCollection Deds;
                if (PrimaryContract.GetDeductiblesForSubject(sub, out Deds))
                {
                    node.Deductibles = Deds;
                }

                LimitCollection Lims;
                if (PrimaryContract.GetLimitsForSubject(sub, out Lims))
                {
                    node.Limits = Lims;
                }

                termNodes.Add(node);
            }
            return termNodes;
        }
    }

    public class TreatyContractNodeBuilder : ContractNodeBuilder
    {
        protected TreatyContractExtractor TreatyContract { get { return Contract as TreatyContractExtractor; } }

        public TreatyContractNodeBuilder(TreatyContractExtractor _contract)
        {
            Contract = _contract;           
        }

        public override List<TermNode> GetTermNodes()
        {
            return new List<TermNode>();
        }
    }
}
