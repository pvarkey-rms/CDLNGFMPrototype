using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class Limit
    {
        public Value Value { get; private set; }
        public bool LimIsPerRisk { get; private set; }
        public TermValueType LimType { get; private set; }
        public bool LimitIsNetOfDed { get; private set; }
        public double Amount {get {return this.Value.Amount;}}

        public Limit()
        {

        }

        public Limit(bool _limitIsNetOfDed, Value _limit, bool _limIsPerRisk, TermValueType _limType)
        {
            LimitIsNetOfDed = _limitIsNetOfDed;
            Value = _limit;
            LimIsPerRisk = _limIsPerRisk;
            LimType = _limType;
        }

        public void SetLimFinTerms(bool _limitIsNetOfDed, Value _limit, bool _limIsPerRisk, TermValueType _limType)
        {
            LimitIsNetOfDed = _limitIsNetOfDed;
            Value = _limit;
            LimIsPerRisk = _limIsPerRisk;
            LimType = _limType;
        }
    }

    public class LimitCollection : IEnumerable<Limit>
    {
        private List<Limit> limList;
        public bool IsPerRisk
        {
            get
            {
                if (limList.Count == 0)
                    return false;
                else
                    return limList[0].LimIsPerRisk;
            }
        }

        public LimitCollection()
        {
            limList = new List<Limit>();
        }

        public LimitCollection(Limit lim)
        {
            limList = new List<Limit>();
            limList.Add(lim);
        }

        public void Add(Limit lim)
        {
            if (limList.Count == 0 || lim.LimIsPerRisk == limList[0].LimIsPerRisk)
                limList.Add(lim);
            else
                throw new InvalidOperationException("Cannot have two limits in a term's limit collection, with different Per Risk values!");
        }

        public IEnumerator<Limit> GetEnumerator()
        {
            return limList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            // Lets call the generic version here
            return this.GetEnumerator();
        }
    }
}
