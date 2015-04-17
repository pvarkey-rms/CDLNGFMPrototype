using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFMReference
{
    public class COLCollection: IEquatable<COLCollection>
    {
        private HashSet<CauseOfLoss> collection;
        public HashSet<CauseOfLoss> Collection { get { return collection; } }

        public int Count { get { return collection.Count; } }

        public COLCollection(HashSet<string> COLs)
        {
            collection = new HashSet<CauseOfLoss>();
            foreach (string col in COLs)
            {
                collection.Add(new CauseOfLoss(col));
            }
        }

        public COLCollection(string inputstring)
        {
            collection = new HashSet<CauseOfLoss>();
            IEnumerable<string> stringList = inputstring.Split(',').Select(str => str.Trim());
            foreach (string col in stringList)
            {
                collection.Add(new CauseOfLoss(col));
            }
        }

        public HashSet<string> GetSubperils()
        {
            HashSet<string> subperils = new HashSet<string>();
            foreach (CauseOfLoss col in collection)
            {
                subperils.UnionWith(col.GetSubperils());
            }
            return subperils;
        }

        public bool Equals(COLCollection other)
        {
            if (other == null)
                return false;

            if (this.collection.SetEquals(other.collection) )
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            COLCollection COLObj = obj as COLCollection;
            if (COLObj == null)
                return false;
            else
                return Equals(COLObj);
        }

        public override int GetHashCode()
        {
            int code = 0;

            foreach (CauseOfLoss COL in collection)
            {
                code = code + 31 * COL.GetHashCode();
            }

            return code;

        }

        public bool LargerOrEqualThan(COLCollection other)
        {
            HashSet<string> subperils = this.GetSubperils();
            HashSet<string> othersubperils = other.GetSubperils();
            if (subperils.IsSupersetOf(othersubperils))
                return true;
            else
                return false;
        }

        public bool LargerThan(COLCollection other)
        {
            HashSet<string> subperils = this.GetSubperils();
            HashSet<string> othersubperils = other.GetSubperils();
            if (subperils.IsProperSupersetOf(othersubperils))
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            return String.Join(",", collection.Select(COL => COL.ToString()));
        }

        public void UnionWith(COLCollection other)
        {
            this.collection.UnionWith(other.collection);
        }
    }

    public class CauseOfLoss: IEquatable<CauseOfLoss>
    {
        private string COLstring;
        private static Dictionary<string, HashSet<string>> COLDict = new Dictionary<string, HashSet<string>>();

        static CauseOfLoss()
        {
            Initialize();
        }

        public CauseOfLoss(string _col)
        {
            _col = _col.Trim().ToUpper();
            if (COLDict.ContainsKey(_col))
                COLstring = _col;
            else
                throw new ArgumentOutOfRangeException("Cannot create casue of loss object with string " + _col + ". This COL has no mapping to subperils!");
        }

        public static void AddMapping(string COL, string subperil)
        {
            COL = COL.Trim().ToUpper();
            subperil = subperil.Trim().ToUpper();

            HashSet<string> subperils;
            if (COLDict.TryGetValue(COL, out subperils))
                subperils.Add(subperil);
            else
                COLDict.Add(COL, new HashSet<string>(){subperil});
        }

        public HashSet<string> GetSubperils()
        {
            HashSet<string> subperils;
            if (COLDict.TryGetValue(COLstring, out subperils))
                return subperils;
            else
                throw new InvalidOperationException("Cannot get subperils for cause of loss: " + COLstring + ". No COL exists in mapping!");
        }

        public bool Equals(CauseOfLoss other)
        {
            if (other == null)
                return false;

            if (this.COLstring == other.COLstring)
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            CauseOfLoss COLObj = obj as CauseOfLoss;
            if (COLObj == null)
                return false;
            else
                return Equals(COLObj);
        }

        public override int GetHashCode()
        {
            return COLstring.GetHashCode();
        }

        public static void Initialize()
        {
            COLDict = new Dictionary<string, HashSet<string>>();
            //AddMapping("EQ", "SH");
            //AddMapping("EQ", "FF");
            //AddMapping("EQ", "SL");
            //AddMapping("EQSH", "SH");
            //AddMapping("EQFF", "FF");
            //AddMapping("EQSL", "SL");
            //AddMapping("EQSHFF", "SH");
            //AddMapping("EQSHFF", "FF");
            AddMapping("EQ", "EQ");
            AddMapping("WS", "WS");
            AddMapping("CS", "CS");
            AddMapping("WT", "WT");
            AddMapping("FL", "FL");
            //AddMapping("EQWS", "EQ");
            //AddMapping("EQWS", "WS");
            AddMapping("CSWT", "CSWT");
            AddMapping("WI", "WI");
            AddMapping("WA", "WA");
        }

        public static bool COLExists(string col)
        {
            col = col.Trim().ToUpper();
            if (COLDict.ContainsKey(col))
                return true;
            else
                return false;
        }

        public static bool SubPerilExists(string subperil)
        {
            bool found = false;
            subperil = subperil.Trim().ToUpper();
            foreach (HashSet<string> subperilSet in COLDict.Values)
            {
                if (subperilSet.Contains(subperil))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        public static COLCollection GetDefaultCOLs()
        {
            HashSet<string> Default = new HashSet<string>();
            foreach (KeyValuePair<string, HashSet<string>> COlpair in COLDict)
            {
                Default.Add(COlpair.Key);
            }

            return new COLCollection(Default);
        }

        public override string ToString()
        {
            return COLstring;
        }

    }
}
