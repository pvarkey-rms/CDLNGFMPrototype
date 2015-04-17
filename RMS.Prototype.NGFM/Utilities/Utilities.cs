using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

using ProtoBuf;

using Rms.Analytics.DataService.Zip;
using Rms.DataServices.DataObjects;
using Rms.DataServices.DataObjects.CDL;

using RMS.ContractObjectModel;

namespace RMS.Prototype.NGFM
{
    public abstract class AppConfig : IDisposable
    {
        public static AppConfig Change(string path)
        {
            return new ChangeAppConfig(path);
        }

        public abstract void Dispose();

        private class ChangeAppConfig : AppConfig
        {
            private readonly string oldConfig =
                AppDomain.CurrentDomain.GetData("APP_CONFIG_FILE").ToString();

            private bool disposedValue;

            public ChangeAppConfig(string path)
            {
                AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", path);
                //AppDomain.CurrentDomain.SetData("APPBASE", );
                ResetConfigMechanism();
            }

            public override void Dispose()
            {
                if (!disposedValue)
                {
                    AppDomain.CurrentDomain.SetData("APP_CONFIG_FILE", oldConfig);
                    ResetConfigMechanism();


                    disposedValue = true;
                }
                GC.SuppressFinalize(this);
            }

            private static void ResetConfigMechanism()
            {
                typeof(ConfigurationManager)
                    .GetField("s_initState", BindingFlags.NonPublic |
                                             BindingFlags.Static)
                    .SetValue(null, 0);

                typeof(ConfigurationManager)
                    .GetField("s_configSystem", BindingFlags.NonPublic |
                                                BindingFlags.Static)
                    .SetValue(null, null);

                typeof(ConfigurationManager)
                    .Assembly.GetTypes()
                    .Where(x => x.FullName ==
                                "System.Configuration.ClientConfigPaths")
                    .First()
                    .GetField("s_current", BindingFlags.NonPublic |
                                           BindingFlags.Static)
                    .SetValue(null, null);
            }
        }
    }
    
    public static class Utilities
    {
        #region Protobuf Serialization/Deserialization

        public static string[] GetExtantFiles(string[] files)
        {
            var fs = new List<string>();
            if (files != null)
                foreach (string f in files)
                    if (File.Exists(f))
                        fs.Add(f);
            return fs.ToArray();
        }

        public static List<ContractExposure> DeserializeContractExposures(string file)
        {
            List<ContractExposure> ContractExposures = null;
            try
            {
                byte[] data = File.ReadAllBytes(file);
                using (MemoryStream ms = new MemoryStream(data))
                {
                    ms.Position = 0;
                    ContractExposures = ProtoBuf.Serializer.DeserializeItems<ContractExposure>(ms, ProtoBuf.PrefixStyle.Base128, 0).ToList();
                    return ContractExposures;
                }
            }
            catch (ProtoBuf.ProtoException p)
            {
                //Log.Fatal("Error Reading a protobuf file" + p.Message);
                Console.WriteLine("Error Reading a protobuf file" + p.Message);
            }
            return ContractExposures;
        }

        public static List<ContractExposure> DeserializeContractExposures(string[] files)
        {
            List<ContractExposure> ContractExposures = new List<ContractExposure>();
            foreach (string file in GetExtantFiles(files))
            {
                var ContractExposuresFromFile = DeserializeContractExposures(file);
                if (ContractExposuresFromFile!=null)
                    ContractExposures = ContractExposures.Concat(ContractExposuresFromFile).ToList();
            }
            return ContractExposures;
        }

        public static PartitionData DeserializePartitionData(string[] files, CancellationTokenSource ct)
        {
            List<PartitionData> PDs = new List<PartitionData>();
            foreach (var f in GetExtantFiles(files))
            {
                if (ct.IsCancellationRequested)
                    return null;
                var pd = DeserializePartitionData(f);
                if(pd != null)
                    PDs.Add(pd);
            }
            return (PDs.Count > 0) ? PartitionData.Merge(PDs) : null;
        }

        public static void Serialize(PartitionData pData, string file)
        {
            using (var wc = new WebClient().OpenWrite(file))
            {
                try
                {
                    ProtoBuf.Serializer.Serialize<PartitionData>(wc, pData);
                }
                catch (ProtoBuf.ProtoException p)
                {
                    //Log.Fatal("Error Writing of protobuf to file" + p.Message);
                    Console.WriteLine("Error Writing of protobuf to file \"{0}\": {1}", file, p.Message);
                }
            }
        }

        public static PartitionData DeserializePartitionData(string file)
        {
            PartitionData result = null;
            using (var wc = new WebClient().OpenRead(file))
            {
                try
                {
                    result = ProtoBuf.Serializer.Deserialize<PartitionData>(wc);
                }
                catch (ProtoBuf.ProtoException p)
                {
                    //Log.Fatal("Error Reading a protobuf file" + p.Message);
                    Console.WriteLine("Error Reading a protobuf file \"{0}\": {1}", file, p.Message);
                }
            }
            return result;
        }

        public static string AsString(this Subschedule subschedule)
        {
            var sb = new StringBuilder();
            if (subschedule != null)
            {
                sb.Append(" Type : ");
                    sb.Append(subschedule.Type.ToString());
                sb.Append("; Ids :   ");
                if (subschedule.Ids != null)
                    sb.Append(string.Join(",", subschedule.Ids.Select(e => e.ToString()).ToArray<string>()));
                else if (subschedule.CompressedIds != null)
                    sb.Append(string.Join(",", subschedule.CompressedIds.Enumerable().Select(e => e.ToString()).ToArray<string>()));
                sb.Append(";");
            }
            return sb.ToString();
        }

        #endregion Protobuf Serialization/Deserialization

        #region Damage Ratios: IN/OUT

        static public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> ReadDamageRatiosFromDATFile(string fName)
        {
            #region output
            // format of output:
            //  key1: subperil
            //      key2: sample id
            //          key3: rite characteristic (or loccvg) id
            //              value: Tuple of (timestamp, sampled damage ratio)
            var res = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();
            #endregion

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fName, FileMode.Open)))
                {
                    int numSubperils = reader.ReadInt32();

                    for (int i = 0; i < numSubperils; i++)
                    {
                        string subperil = reader.ReadString();
                        if (!res.ContainsKey(subperil))
                            res.Add(subperil, new Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>());
                        else
                            throw new Exception("Subperil should not exist.");

                        int numSamples = reader.ReadInt32();

                        for (int j = 0; j < numSamples; j++)
                        {
                            int sampleId = reader.ReadInt32();
                            if (!res[subperil].ContainsKey(sampleId))
                                res[subperil].Add(sampleId, new Dictionary<long, Tuple<double, uint, List<float>>>());
                            else
                                throw new Exception("Sample Id should not exist.");

                            int numRiteChars = reader.ReadInt32();

                            for (int k = 0; k < numRiteChars; k++)
                            {
                                long riteId = reader.ReadInt64();
                                if (!res[subperil][sampleId].ContainsKey(riteId))
                                {
                                    uint timestamp = reader.ReadUInt32();
                                    int numDrs = reader.ReadInt32();
                                    List<float> drs = new List<float>(numDrs);
                                    for (int m = 0; m < numDrs; m++)
                                    {
                                        drs.Add(BitConverter.ToSingle(reader.ReadBytes(4), 0));
                                    }
                                    res[subperil][sampleId].Add(riteId, new Tuple<double, uint, List<float>>(1.0, timestamp, drs));                                   
                                }
                                else 
                                    throw new Exception("Rite characteristic id should not exist.");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return res;
        }
        
        static public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> ReadUncertOutput(string fName)
        {
            #region output
            // format of output:
            //  key1: subperil
            //      key2: sample id
            //          key3: rite characteristic (or loccvg) id
            //              value: Tuple of (timestamp, sampled damage ratio)
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> res = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();
            #endregion

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(fName, FileMode.Open)))
                {
                    int numSubperils = reader.ReadInt32();

                    for (int i = 0; i < numSubperils; i++)
                    {
                        string subperil = reader.ReadString();
                        if (res.ContainsKey(subperil))
                        {
                            throw new Exception("Subperil should not exist.");
                        }

                        res[subperil] = new Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>();

                        int numSamples = reader.ReadInt32();

                        for (int j = 0; j < numSubperils; j++)
                        {
                            int sampleId = reader.ReadInt32();
                            if (res[subperil].ContainsKey(sampleId))
                            {
                                throw new Exception("Sample Id should not exist.");
                            }
                            res[subperil][sampleId] = new Dictionary<long, Tuple<double, uint, List<float>>>();

                            int numRiteChars = reader.ReadInt32();

                            for (int k = 0; k < numRiteChars; k++)
                            {
                                long ritecharId = reader.ReadInt64();
                                if (res[subperil][sampleId].ContainsKey(ritecharId))
                                {
                                    throw new Exception("Rite characteristic id should not exist.");
                                }

                                uint timestamp = reader.ReadUInt32();

                                List<float> drs = new List<float>();
                                drs.Add(BitConverter.ToSingle(reader.ReadBytes(4), 0));

                                res[subperil][sampleId][ritecharId] = new Tuple<double, uint, List<float>>(1.0, timestamp, drs);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }

            return res;
        }

        static public void ReadWriteDamageRatios(string infilepath, string outfilepath)
        {

            var data = ReadDamageRatiosFromCSVFile(infilepath);
            if(null != data)
                WriteDamageRatiosToDATFile(outfilepath, data);
        }

        static public Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> ReadDamageRatiosFromCSVFile(string infilepath)
        {
            var data = new Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>>();

            try
            {
                using (StreamReader sr = new StreamReader(infilepath))
                {
                    string str;
                    while ((str = sr.ReadLine()) != null)
                    {
                        str = str.Trim();
                        if (str.Count() == 0)
                            continue;
                        string[] arr = str.Split(',');
                        int i = 0;
                        string peril = arr[i++];
                        int sampleId = int.Parse(arr[i++]);
                        long riteId = long.Parse(arr[i++]);
                        uint timestamp = uint.Parse(arr[i++]);
                        int numMultiBuildingSamples = int.Parse(arr[i++]);

                        var drs = new List<float>((int)numMultiBuildingSamples);
                        int j = i;
                        for (; j < i + numMultiBuildingSamples; j++)
                            drs.Add((j < arr.Length) ? float.Parse(arr[j]) : 0.0f);

                        double factor = float.Parse(arr[j]);

                        if (!data.ContainsKey(peril))
                            data.Add(peril, new Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>());
                        if (!data[peril].ContainsKey(sampleId))
                            data[peril].Add(sampleId, new Dictionary<long, Tuple<double, uint, List<float>>>());
                        if (!data[peril][sampleId].ContainsKey(riteId))
                            data[peril][sampleId].Add(riteId, new Tuple<double, uint, List<float>>(factor, timestamp, drs));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return data;
        }

        static public void WriteDamageRatiosToFile(string filePath, Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> damageRatiosPerSubPeril)
        {
            if (null != damageRatiosPerSubPeril && !string.IsNullOrEmpty(filePath) && Utilities.CreateDirectoryRecursively(Path.GetDirectoryName(filePath)))
            {
                FileInfo fi = new FileInfo(filePath);
                if (fi.Extension.ToLower().Equals(".txt") || fi.Extension.ToLower().Equals(".csv"))
                    Utilities.WriteDamageRatiosToCSVFile(filePath, damageRatiosPerSubPeril);
                else if (fi.Extension.ToLower().Equals(".dat"))
                    Utilities.WriteDamageRatiosToDATFile(filePath, damageRatiosPerSubPeril);
                else
                    Utilities.WriteDamageRatiosToDATFile(filePath, damageRatiosPerSubPeril);
            }
        }

        static public void WriteDamageRatiosToDATFile(string fName, 
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> dr) 
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(fName, FileMode.Create)))
            {
                writer.Write(dr.Count); // write # subperils

                foreach (var kv1 in dr.Where(elem => elem.Value != null))
                {
                    string peril = kv1.Key;
                    writer.Write(peril);                // write subperil
                    writer.Write(kv1.Value.Count); // write number of samples

                    foreach (var kv2 in kv1.Value.Where(elem => elem.Value != null))
                    {
                        int sampleId = kv2.Key;
                        writer.Write(sampleId);             // write sample id
                        writer.Write(kv2.Value.Count);   // write # rite chars

                        foreach (var kv3 in kv2.Value.Where(elem => elem.Value != null))
                        {
                            long riteId = kv3.Key;
                            writer.Write(riteId);   // write rite char id

                            uint time = kv3.Value.Item2;
                            writer.Write(time);     // write time stamp

                            List<float> drs = kv3.Value.Item3;
                            writer.Write(drs.Count);       // write sampled dr length
                            foreach (float d in drs)
                                writer.Write(d);
                        }
                    }
                }
            }
        }
        
        static public void WriteDamageRatiosToCSVFile(string fName,
            Dictionary<string, Dictionary<int, Dictionary<long, Tuple<double, uint, List<float>>>>> dr) 
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fName, true))
            {
                //example:
                //EQ,0,3253,1,3,0.0348366951942444,0.04258971,0.089412578963

                foreach (var kv1 in dr.Where(elem => elem.Value != null))
                {
                    string peril = kv1.Key;
                    foreach (var kv2 in kv1.Value.Where(elem => elem.Value != null))
                    {
                        int sampleId = kv2.Key;
                        foreach (var kv3 in kv2.Value.Where(elem => elem.Value != null))
                        {
                            long riteId = kv3.Key;
                            uint timestamp = kv3.Value.Item2;
                            int numMultiBuildingSamples = kv3.Value.Item3.Count();
                            string drs = string.Join(",", kv3.Value.Item3);
                            file.WriteLine(string.Format("{0},{1},{2},{3},{4},{5}", peril, sampleId, riteId, timestamp, numMultiBuildingSamples, drs));
                        }
                    }
                }
            }
        }

        static public void WriteResultsToCSVFile(string fName,
            Dictionary<long, ResultPosition> Results)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(fName, true))
            {
                //example:
                //EQ,0,3253,1,3,0.0348366951942444,0.04258971,0.089412578963

                foreach (var ContractResultPositionKVP in Results)
                {
                    long ContractId = ContractResultPositionKVP.Key;
                    
                    Dictionary<long, Dictionary<string, double>> RITEAllocation = 
                        ContractResultPositionKVP.Value.RITEAllocation;

                    foreach (var RITEAllocationKVP in RITEAllocation)
                    {
                        long RITEId = RITEAllocationKVP.Key;

                        foreach (var GRLossByCOLKVP in RITEAllocationKVP.Value)
                        {
                            string COL = GRLossByCOLKVP.Key;
                            double GRLoss = GRLossByCOLKVP.Value;
                            file.WriteLine(string.Format("{0},{1},{2},{3}", ContractId, RITEId, COL, GRLoss));
                        }
                    }
                }
            }
        }


        #endregion

        #region Time Series Operations
        static public SortedDictionary<DateTime, double> UnionTimeSeries(SortedDictionary<DateTime, double> A, SortedDictionary<DateTime, double> B)
        { // A + B

            if (null == A || A.Count() == 0)
                return B;
            else if (null == B || B.Count() == 0)
                return A;

            var result = new SortedDictionary<DateTime, double>(A);
            foreach (var p in B)
            {
                if (!result.ContainsKey(p.Key))
                    result.Add(p.Key, p.Value);
                else
                    result[p.Key] += p.Value;
            }
            return result;
        }

        static public SortedDictionary<DateTime, double> SubtractTimeSeries(SortedDictionary<DateTime, double> A, SortedDictionary<DateTime, double> B)
        { // A - B

            if (null == B || B.Count() == 0)
                return A;

            var result = new SortedDictionary<DateTime, double>();
            foreach (var p in A)
            {
                if (!B.ContainsKey(p.Key) && p.Value > 0)
                    result.Add(p.Key, p.Value);
                else if (p.Value > B[p.Key])
                    result.Add(p.Key, p.Value - B[p.Key]);
            }
            return result;
        }

        // Create map: Position -> TimeSeries
        public static Dictionary<string, SortedDictionary<DateTime, double>> CreatePositionToTimeAllocation(
            Dictionary<string, HashSet<long>> PositionToContractId,
            Dictionary<long, SortedDictionary<DateTime, double>> ContractIdToTimeAllocation)
        {
            // position title -> Time -> PayOut
            var positionToTimeAllocation = new Dictionary<string, SortedDictionary<DateTime, double>>();
            if (null != PositionToContractId && null != ContractIdToTimeAllocation)
            {
                positionToTimeAllocation =
                        PositionToContractId.Where(pos => null != pos.Value)
                        .ToDictionary(pos => pos.Key, pos =>
                        new SortedDictionary<DateTime, double>(
                        ContractIdToTimeAllocation.Where(kv => null != kv.Value && pos.Value.Contains(kv.Key))
                            .Select(kv => kv.Value)
                            .Aggregate(new SortedDictionary<DateTime, double>(), (a, b) => { return UnionTimeSeries(a, b); }
                            )));

            }
            return positionToTimeAllocation;
        }

        // Create map: Position -> TimeSeries
        public static Dictionary<string, HashSet<long>> AdjustPositions(
            Dictionary<string, HashSet<long>> PositionToContractId,
            Dictionary<string, bool> PositionToOperation)
        {
            //Example:
            // (+) A -> {2, 4, 5}  |    A -> {4}
            // (+) B -> {2}        |    B -> {}
            // (-) C -> {2, 3, 5}  |=>  C -> {3}
            // (-) D -> {5, 2}     |    D -> {5}
            // (-) E -> {2}        |    E -> {2}

            var result = PositionToContractId.Where(elem => null != elem.Value && PositionToOperation.ContainsKey(elem.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

            bool flag = true;
            do
            {
                flag = SubtractTheNextCommonID(ref result, PositionToOperation);
            }
            while (flag);

            return result;
        }

        public static bool SubtractTheNextCommonID(
            ref Dictionary<string, HashSet<long>> PositionToContractId,
            Dictionary<string, bool> PositionToOperation)
        {
            foreach (var kv1 in PositionToContractId.Where(elem => PositionToOperation[elem.Key]))
            {
                foreach (long id1 in kv1.Value)
                {
                    foreach (var kv2 in PositionToContractId.Where(elem => !PositionToOperation[elem.Key]))
                    {
                        foreach (long id2 in kv2.Value)
                        {
                            if (id1 == id2)
                            {
                                PositionToContractId[kv1.Key].Remove(id1);
                                PositionToContractId[kv2.Key].Remove(id2);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        //Union or set-difference of time series: if operation is true then union, otherwise set-difference
        public static SortedDictionary<DateTime, double> ApplyUnionOrSetDifferenceForPositionTimeSeries(
            Dictionary<string, SortedDictionary<DateTime, double>> PositionToTimeAllocation,
            Dictionary<string, bool> PositionToOperation)
        { 
            if (null != PositionToTimeAllocation && null != PositionToOperation)
            {
                var temp = PositionToTimeAllocation.Where(elem => null != elem.Value && PositionToOperation.ContainsKey(elem.Key));

                var plus = temp.Where(elem => PositionToOperation[elem.Key])
                    .Aggregate(new SortedDictionary<DateTime, double>(), (a, b) => { return UnionTimeSeries(a, b.Value); });

                var minus = temp.Where(elem => !PositionToOperation[elem.Key])
                    .Aggregate(new SortedDictionary<DateTime, double>(), (a, b) => { return UnionTimeSeries(a, b.Value); });

                return SubtractTimeSeries(plus, minus);
            }
            return null;
        }
        #endregion

        public static bool CreateDirectoryRecursively(string path)
        {
            string[] pathParts = path.Split('\\','/');

            string path2 = pathParts[0] + "\\";
            for (int i = 1; i < pathParts.Length; i++)
            {
                path2 = Path.Combine(path2, pathParts[i]);

                if (!Directory.Exists(path2))
                    Directory.CreateDirectory(path2);
            }
            return Directory.Exists(path);
        }

        public static bool CompareDictionaries(Dictionary<string, HashSet<long>> dic1, Dictionary<string, HashSet<long>> dic2)
        {
            try
            {
                if (dic1 == null && dic2 == null)
                    return true;
                else if(dic1 == null || dic2 == null)
                    throw new Exception("dic1 == null OR dic2 == null");

                var keys1 = dic1.Keys;
                var keys2 = dic2.Keys;

                foreach (var k1 in keys1)
                    if (!keys2.Contains(k1)) { throw new Exception(string.Format("!keys2.Contains({0})",k1)); }

                foreach (var k2 in keys2)
                    if (!keys1.Contains(k2)) { throw new Exception(string.Format("!keys1.Contains({0})", k2)); }

                foreach (var kv in dic1)
                    foreach (long id1 in kv.Value)
                        if (!dic2[kv.Key].Contains(id1)) { throw new Exception(string.Format("!dic2[{0}].Contains({1})", kv.Key, id1)); }

                foreach (var kv in dic2)
                    foreach (long id2 in kv.Value)
                        if (!dic1[kv.Key].Contains(id2)) { throw new Exception(string.Format("!dic1[{0}].Contains({1})", kv.Key, id2)); }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

    }
}
