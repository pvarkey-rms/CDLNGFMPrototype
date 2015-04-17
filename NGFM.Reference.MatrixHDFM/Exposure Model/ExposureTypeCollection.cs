using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NGFM.Reference.MatrixHDFM
{
    public class ExposureTypeCollection: IEnumerable<ExposureType>
    {
        private HashSet<ExposureType> collection;
        public HashSet<ExposureType> Collection { get { return collection; } }
        private static Dictionary<ExposureType, ExposureType> Mapping;
        private HashSet<ExposureType> mappedcollection;
        public HashSet<ExposureType> MappedTypes
        {
            get
            {
                if (mappedcollection == null)
                {
                    mappedcollection = GetMappedExpTypes();
                    return mappedcollection;
                }
                else
                    return mappedcollection;
            }
        }
        public int Count { get { return collection.Count; } }

        public ExposureTypeCollection(HashSet<ExposureType> expTypes)
        {
            collection = expTypes;
        }

        static ExposureTypeCollection()
        {
            BuildExpTypeMapping();
        }

        public static ExposureTypeCollection BuildFromString(string inputstring)
        {
            if (inputstring == "Loss")
                inputstring = "Building, Contents, BI";
            Type ExpType = typeof(ExposureType);
            IEnumerable<string> stringList = inputstring.Split(',').Select(str => str.Trim());
            IEnumerable<ExposureType> expList = stringList.Select(str => Parse(str));

            return new ExposureTypeCollection(new HashSet<ExposureType>(expList));
        }

        private static ExposureType Parse(string str)
        {       
            //switch (str)
            //{
            //    case "CoverageA":
            //        return ExposureType.Building;
            //    case "CoverageB":
            //        return ExposureType.Building;
            //    case "CoverageC":
            //        return ExposureType.Contents;
            //    case "CoverageD":
            //        return ExposureType.BI;
            //    default:
            //        Type ExpType = typeof(ExposureType);
            //        return (ExposureType)Enum.Parse(ExpType, str);
            //}
            Type ExpType = typeof(ExposureType);
            return (ExposureType)Enum.Parse(ExpType, str);
        }

        public static ExposureTypeCollection GetDefaultExposureTypes()
        {
            return new ExposureTypeCollection(new HashSet<ExposureType>() { ExposureType.Building, ExposureType.Contents, ExposureType.BI });
        }

        public bool Contains(ExposureType type)
        {
            return collection.Contains(type);
        }

        public HashSet<ExposureType> GetMappedExpTypes()
        {
            return new HashSet<ExposureType>(this.collection.Select(type => GetMappedType(type)));
        }

        //Comparison Methods

        public bool LargerOrEqualThan(ExposureTypeCollection other, bool IsLocCvg = false)
        {
            if (IsLocCvg)
                return this.collection.First() == other.collection.First();

            return other.All(expType => this.collection.Contains(expType)
                                        || this.collection.Contains(ExposureTypeCollection.GetMappedType(expType)));
        }

        public bool LargerThan(ExposureTypeCollection other, bool IsLocCvg = false)
        {
            if (IsLocCvg)
                return false;

            return other.All(expType => this.collection.Contains(expType)
                                        || this.collection.Contains(ExposureTypeCollection.GetMappedType(expType)))
                   && !this.Equals(other);
        }

        //ExposureTypeMapping
        public static void BuildExpTypeMapping()
        {
            Mapping = new Dictionary<ExposureType, ExposureType>();
            Mapping.Add(ExposureType.Building, ExposureType.Building);
            Mapping.Add(ExposureType.Contents, ExposureType.Contents);
            Mapping.Add(ExposureType.BI, ExposureType.BI);
            Mapping.Add(ExposureType.CoverageA, ExposureType.Building);
            Mapping.Add(ExposureType.CoverageB, ExposureType.Building);
            Mapping.Add(ExposureType.CoverageC, ExposureType.Contents);
            Mapping.Add(ExposureType.CoverageD, ExposureType.BI);
        }

        public static ExposureType GetMappedType(ExposureType type)
        {
            if (Mapping.ContainsKey(type))
                return Mapping[type];
            else
                throw new ArgumentOutOfRangeException("Cannot find mapped Exposure Type for " + type.ToString() + ". Please update mapping in ExposureTYpeCollection class"); 
        }
        
        //Override IEquatable

        public bool Equals(ExposureTypeCollection other)
        {
            if (other == null)
                return false;

            if (this.collection.SetEquals(other.collection))
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            ExposureTypeCollection expColObj = obj as ExposureTypeCollection;
            if (expColObj == null)
                return false;
            else
                return Equals(expColObj);
        }

        public override int GetHashCode()
        {
            int code = 0;

            foreach (ExposureType expType in collection)
            {
                code = code + 31 * expType.GetHashCode();
            }

            return code;

        }

        public override string ToString()
        {
            return String.Join(",", collection.Select(COL => COL.ToString()));
        }

        //override IEnumerable to allow FOR EACH

        public IEnumerator<ExposureType> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            // Lets call the generic version here
            return this.GetEnumerator();
        }

    }

    public enum ExposureType
    {
        Building = 1,
        Contents = 2,
        BI = 3,
        CoverageA = 4,
        CoverageB = 5,
        CoverageC = 6,
        CoverageD = 7,
    }
}
