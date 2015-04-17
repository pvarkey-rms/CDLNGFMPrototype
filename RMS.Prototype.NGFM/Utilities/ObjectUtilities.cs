using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rms.Utilities
{
    public static partial class ObjectUtilities
    {
        public static void AddRange<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
                else
                {
                    // handle duplicate key issue here
                }
            }
        }

        static public void Remove<K,V>(this ConcurrentDictionary<K, V> _ConcurrentDictionary, List<K> keys)
        {
            if (null != _ConcurrentDictionary && _ConcurrentDictionary.Count > 0 && null != keys && keys.Count() > 0)
            {
                V rm;
                foreach (K key in keys)
                    _ConcurrentDictionary.TryRemove(key, out rm);
            }
        }

        public static SortedDictionary<TKey, TValue> ExceptKeys<TKey, TValue>(this SortedDictionary<TKey, TValue> Original,
                                           HashSet<TKey> FilterKeys)
        {
            var Filtered = new SortedDictionary<TKey, TValue>();

            foreach (KeyValuePair<TKey, TValue> KVP in Original)
            {
                if (!FilterKeys.Contains(KVP.Key))
                    Filtered.Add(KVP.Key, KVP.Value);
            }

            return Filtered;
        }

        public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(this Dictionary<TKey, TValue> Original)
        {
            var Sorted = new SortedDictionary<TKey, TValue>();

            foreach (KeyValuePair<TKey, TValue> KVP in Original)
            {
                Sorted.Add(KVP.Key, KVP.Value);
            }

            return Sorted;
        }

        static public HashSet<T> MergeValues<K,T>(Dictionary<K, HashSet<T>> dict)
        {
            if (null != dict && dict.Count() > 0)
                return dict.Where(elem => null != elem.Value)
                    .Select(kv => kv.Value)
                    .Aggregate(new HashSet<T>(), (a, b) => { if (null != b) a.UnionWith(b); return a; });
            else return new HashSet<T>();
        }

        public static Dictionary<K, V> DeepCopy<K,V>(this Dictionary<K, V> src)
        {
            Dictionary<K, V> copy = new Dictionary<K, V>();

            for (int i = 0; i < src.Count; i++)
            {
                copy.Add((K)Activator.CreateInstance(typeof(K), new object[] { src.ElementAt(i).Key }), 
                    (V)Activator.CreateInstance(typeof(V), new object[] { src.ElementAt(i).Value }));
            }

            return copy;
        }

        static public string PrettyPrint(this TimeSpan ts)
        {
            return String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
        }

        public static string PrettyPrintHighRes(this Stopwatch sw)
        {
            TimeSpan ts = sw.Elapsed;
            long microseconds = sw.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));
            long _microseconds = microseconds - ((long)ts.TotalSeconds * 1000000);
            return String.Format("{0:00}:{1:00}:{2:00}.{3:000000}",
                ts.Hours, ts.Minutes, ts.Seconds, _microseconds);
        }

        public static long ElapsedNanoSeconds(this Stopwatch watch)
        {
            return watch.ElapsedTicks * 1000000000 / Stopwatch.Frequency;
        }

        public static long ElapsedMicroSeconds(this Stopwatch watch)
        {
            return watch.ElapsedTicks * 1000000 / Stopwatch.Frequency;
        }
    }
}
