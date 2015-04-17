using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMS.Prototype.NGFM.CLI
{
    public static partial class IOUtilities
    {
        /// <summary>
        /// Gets all files in directory.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <returns>List{System.String}.</returns>
        public static List<string> GetAllFilesinDirectory(string directory)
        {
            List<string> listofProtobufs = new List<string>();
            foreach (string file in Directory.GetFiles(directory))
            {
                if (file.Contains("rites"))
                {
                    listofProtobufs.Add(file);
                }
            }
            return listofProtobufs;
        }

        public static List<string> GetAllFilesinDirectory(string dir, string fileNameTemplate)
        {
            if (Directory.Exists(dir))
                return FilterFiles(Directory.GetFiles(dir).ToList(), fileNameTemplate);
            return new List<string>();
        }

        public static List<string> FilterFiles(List<string> files, string fileNameTemplate)
        {
            Regex expr = new Regex(string.Format(fileNameTemplate + "$", @"(\d+)"));
            return files.Where(e => expr.IsMatch(e)).ToList();
        }

        public static int FileMatch(string fileName, string fileNameTemplate)
        {
            Regex expr = new Regex(string.Format(fileNameTemplate + "$", @"(\d+)"));
            int id = -1;
            Match m = expr.Match(fileName);
            return (m.Success && Int32.TryParse(m.Groups[1].Value, out id)) ? id : -1;
        }

        public static Dictionary<string, int> ReadCSVLineGetValueToIdxMap(string csvLine)
        {
            string[] columns = csvLine.Split(',');
            Dictionary<string, int> idxHeaderFieldMap = new Dictionary<string, int>();
            for (int i = 0; i < columns.Count(); i++)
            {
                idxHeaderFieldMap.Add(columns[i].Trim(), i);
            }
            return idxHeaderFieldMap;
        }

        public static string CreateSubdirectory(params string[] paths)
        {
            string dir = Path.Combine(paths);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static float GetaAvailablePhysicalMemoryMB()
        {
            PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes", String.Empty);
            return ramCounter.NextValue();
        }
    }
}
