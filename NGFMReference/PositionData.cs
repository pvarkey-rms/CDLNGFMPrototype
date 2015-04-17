using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class PositionData
    {
        public Dictionary<string, HashSet<long>> posDict;

        public PositionData()
        {
            posDict = new Dictionary<string, HashSet<long>>();
        }

        public PositionData(Dictionary<string, HashSet<long>> posData)
        {
            posDict = posData;
        }

        public void Add(string position, HashSet<long> contracts)
        {
            position = position.Trim().ToUpper();
            if (posDict.ContainsKey(position))
                posDict[position].UnionWith(contracts);
            else
                posDict.Add(position, contracts);
        }

        public bool ContractsForPosition(string position, out HashSet<long> contracts)
        {
            position = position.Trim().ToUpper();
            if (posDict.TryGetValue(position, out contracts))
            {
                contracts = posDict[position];
                return true;
            }
            else
                return false;
        }

        public HashSet<long> GetDependentContracts()
        {
            HashSet<long> allContracts = new HashSet<long>();
            foreach (HashSet<long> contracts in posDict.Values)
            {
                allContracts.UnionWith(contracts);
            }

            return allContracts;
        }
    }
}
