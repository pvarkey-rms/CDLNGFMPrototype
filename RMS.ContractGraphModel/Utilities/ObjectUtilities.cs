using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rms.Utilities
{
    public static partial class ObjectUtilities
    {
        public static bool ContainsAny<T>(this IEnumerable<T> container, IEnumerable<T> elements)
        {
            if (elements == null)
                throw new ArgumentNullException("elements is null");
            foreach (T element in elements)
            {
                if (container.Contains(element))
                    return true;
            }
            return false;
        }

        public static Dictionary<K, V> DeepCopy<K, V>(this Dictionary<K, V> src)
        {
            Dictionary<K, V> copy = new Dictionary<K, V>();

            for (int i = 0; i < src.Count; i++)
            {
                copy.Add((K)Activator.CreateInstance(typeof(K), new object[] { src.ElementAt(i).Key }),
                    (V)Activator.CreateInstance(typeof(V), new object[] { src.ElementAt(i).Value }));
            }

            return copy;
        }
    }
}