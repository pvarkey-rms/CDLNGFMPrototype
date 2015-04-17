using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rms.Utilities
{
    public static partial class TypeUtilities
    {
        /// <summary>
        /// From http://stackoverflow.com/a/13727044/3465697
        /// Gets a all Type instances matching the specified class name with just non-namespace qualified class name.
        /// </summary>
        /// <param name="className">Name of the class sought.</param>
        /// <returns>Types that have the class name specified. They may not be in the same namespace.</returns>
        public static Type[] GetTypesByName(string className)
        {
            List<Type> returnVal = new List<Type>();

            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assemblyTypes = a.GetTypes();
                for (int j = 0; j < assemblyTypes.Length; j++)
                {
                    if (assemblyTypes[j].Name == className)
                    {
                        returnVal.Add(assemblyTypes[j]);
                    }
                }
            }

            return returnVal.ToArray();
        }

        // Utilitiy to convert class name string to namespace-qualified name
        // TODO: make namespace prefix customizable (e.g. for CLR primitives) and update http://stackoverflow.com/a/25613838/3465697
        public static Type GetTypeByName(string ClassName, string TypesNamespacePrefix = "RMS.ContractObjectModel.", Dictionary<string, Type> ConcreteTypeMap = null)
        {
            if ((ConcreteTypeMap != null) && (ConcreteTypeMap.ContainsKey(ClassName)))
                return ConcreteTypeMap[ClassName];

            if ((ConcreteTypeMap != null) && (ConcreteTypeMap.ContainsKey(TypesNamespacePrefix + ClassName)))
                return ConcreteTypeMap[TypesNamespacePrefix + ClassName];

            try
            {
                if (Type.GetType(ClassName) != null)
                    return Type.GetType(ClassName);
            }
            catch { }

            try
            {
                if (Type.GetType(TypesNamespacePrefix + ClassName) != null)
                    return Type.GetType(TypesNamespacePrefix + ClassName);
            }
            catch { }

            Stack<int> GenericCounterStack = new Stack<int>();
            Stack<int> GenericStartIndexStack = new Stack<int>();
            Dictionary<int, int> GenericCountMapByStartIndex = new Dictionary<int, int>();
            int Count = 1;
            int GenericStartIndex = -1;
            int PreviousHighestGenericIndex = -1;

            foreach (char c in ClassName)
            {
                if (c.Equals('<'))
                {
                    if (GenericStartIndex != -1)
                    {
                        GenericCounterStack.Push(Count);
                        GenericStartIndexStack.Push(GenericStartIndex);
                    }
                    Count = 1;
                    GenericStartIndex = PreviousHighestGenericIndex + 1;
                    PreviousHighestGenericIndex = Math.Max(GenericStartIndex, PreviousHighestGenericIndex);
                }
                else if (c.Equals(','))
                {
                    Count++;
                }
                else if (c.Equals('>'))
                {
                    GenericCountMapByStartIndex[GenericStartIndex] = Count;
                    if (GenericCounterStack.Count != 0)
                        Count = GenericCounterStack.Pop();
                    if (GenericStartIndexStack.Count != 0)
                        GenericStartIndex = GenericStartIndexStack.Pop();
                }
            }

            ClassName = ClassName.Replace("<" + TypesNamespacePrefix, "<");

            StringBuilder FullyQualifiedClassName = new StringBuilder(TypesNamespacePrefix);

            GenericStartIndex = 0;
            foreach (char c in ClassName)
            {
                if (c.Equals('<'))
                {
                    FullyQualifiedClassName.Append("`" + GenericCountMapByStartIndex[GenericStartIndex].ToString()
                        + "[" + TypesNamespacePrefix);
                    GenericStartIndex++;
                }
                else if (c.Equals(','))
                {
                    FullyQualifiedClassName.Append("," + TypesNamespacePrefix);
                }
                else if (c.Equals('>'))
                {
                    FullyQualifiedClassName.Append("]");
                }
                else
                    FullyQualifiedClassName.Append(c);
            }

            ClassName = FullyQualifiedClassName.ToString();

            return Type.GetType(ClassName);
        }

    }
}
