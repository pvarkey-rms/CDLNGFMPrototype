using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeTransform
{

    public class BinCode : IComparable
    {
        #region variables

        private Dictionary<int, int> field = null;
        private int m = 8 * sizeof(int);//number of bits per element in field

        #endregion

        #region Constructors

        public BinCode()
        {
            if (null == field)
                field = new Dictionary<int, int>();
        }
        public BinCode(int idx)
            : this()
        {
            int i = idx / m;
            if (!this.field.ContainsKey(i))
                this.field.Add(i, 0);

            this.field[i] |= (1 << (idx % m));
        }
        public BinCode(BinCode hc)
        {
            this.field = (hc != null && hc.field != null) ? hc.field.Where(e => e.Value != 0).ToDictionary(kv => kv.Key, kv => kv.Value) : new Dictionary<int, int>();
        }

        #endregion

        #region private

        private BinCode Union(BinCode hc2)
        {
            BinCode hc = new BinCode(this);
            if (hc2 != null && hc2.field != null)
            {
                foreach (var kv in hc2.field.Where(e => e.Value != 0))
                {
                    if (!hc.field.ContainsKey(kv.Key))
                        hc.field.Add(kv.Key, kv.Value);
                    else
                        hc.field[kv.Key] |= kv.Value;
                }
            }
            return hc;
        }
        private BinCode Subtract(BinCode hc2)
        {
            // x  y   x&!y
            // 0  0    0
            // 0  1    0
            // 1  0    1
            // 1  1    0

            var hc = new BinCode(this);
            if (hc2 != null && hc2.field != null)
            {
                foreach (var kv in hc2.field.Where(e => e.Value != 0))
                {
                    if (hc.field.ContainsKey(kv.Key))
                    {
                        hc.field[kv.Key] &= ~(kv.Value);

                        if (hc.field[kv.Key] == 0)
                            hc.field.Remove(kv.Key);
                    }
                }
            }
            return hc;
        }

        #endregion

        #region API

        public int CompareTo(object obj)
        {
            if (null == obj || obj.GetType() != typeof(BinCode))
                return 1;

            BinCode hc = (BinCode)obj;

            if (this.field == null && hc.field == null)
                return 0;
            else if (this.field == null || hc.field == null)
                return 1;
            else
            {
                foreach (var kv in this.field.Where(e => e.Value != 0))
                    if (!hc.field.ContainsKey(kv.Key) || (int)(kv.Value ^ hc.field[kv.Key]) > 0)
                        return 1;

                foreach (var kv in hc.field.Where(e => e.Value != 0))
                    if (!this.field.ContainsKey(kv.Key) || (int)(kv.Value ^ this.field[kv.Key]) > 0)
                        return 1;
            }

            return 0;
        }
        public bool IsEmpty()
        {
            return (this.field != null) ? this.field.Count(e => e.Value != 0) == 0 : true;
        }

        public static BinCode operator +(BinCode hc1, BinCode hc2)
        {
            if (hc1 != null && hc2 != null)
                return hc1.Union(hc2);
            else if (hc1 != null)
                return new BinCode(hc1);
            else if (hc2 != null)
                return new BinCode(hc2);
            else
                return new BinCode();
        }
        public static BinCode operator +(BinCode hc1, int elem)
        {
            return hc1 + new BinCode(elem);
        }
        public static BinCode operator -(BinCode hc1, BinCode hc2)
        {
            if (hc1 != null && hc2 != null)
                return hc1.Subtract(hc2);
            else if (hc1 != null)
                return new BinCode(hc1);
            else
                return new BinCode();
        }
        public static BinCode operator !(BinCode hc2)
        {
            var hc = new BinCode();
            int mask = 0xFF;

            if (hc2 != null && hc2.field != null)
                hc.field = hc2.field.Where(e => (int)(e.Value ^ mask) != 0).ToDictionary(kv => kv.Key, kv => kv.Value ^ mask);

            return hc;
        }

        public HashSet<int> GetElements()
        {
            var res = new HashSet<int>();

            if (this.field != null)
            {
                foreach (var kv in this.field)
                {
                    int k = kv.Key * m;
                    for (int j = 0; j < m; j++)
                        if ((kv.Value & (1 << j)) != 0)
                            res.Add(k + j);
                }
            }

            return res;
        }

        #endregion
    }
}
