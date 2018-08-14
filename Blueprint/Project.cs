using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildTask.Blueprint
{
    internal class Project
    {
        public string FullFolderPath;
        public string FullFilePath;
        public string Name;
        public EOutputType? OutputType = null;
        public List<string> Dependencies = null;
        public List<string> Sources = null;
        public List<string> Headers = null;
        public List<string> Libs = null;
        public string Output = null;
        public EWarningLevel? WarningLevel = null;

        public override string ToString()
        {
            return $@"Project""{ Name }""";
        }

        internal string ResolveOutput(Dictionary<string, string> _variables)
        {
            string result = Output.ReplaceVariable("project_name", Name);
            foreach (var kv in _variables)
            {
                result = result.ReplaceVariable(kv.Key, kv.Value);
            }

            return result;
        }

        internal IEnumerable<string> ResolvedSourceFullPaths => Sources?.Select(filename => Directory.GetFiles(FullFolderPath, filename) as IEnumerable<string>)
        .Aggregate((s1, s2) => { var sum = new List<string>(s1); sum.AddRange(s2); return sum as IEnumerable<string>; });
        internal IEnumerable<string> ResolvedSourceRelativePaths => ResolvedSourceFullPaths.Select(s => FileUtility.MakeRelative(FullFolderPath, s));
        internal IEnumerable<string> ResolvedHeaderFullPaths => Headers?.Select(filename => Directory.GetFiles(FullFolderPath, filename) as IEnumerable<string>)
        .Aggregate((s1, s2) => { var sum = new List<string>(s1); sum.AddRange(s2); return sum as IEnumerable<string>; });

        internal void DebugTest()
        {
            Log.WriteLine($"{Name}:");
            // to do: transform \ in / in the pattern
            var filenamePattern = $"[^{ Path.GetInvalidFileNameChars().Aggregate("", (str, c) => $"{str}{c.ToString()}") }]*";
            var pathPattern = $"[^{ Path.GetInvalidPathChars().Aggregate("", (str, c) => $"{str}{c.ToString()}") }]+";
            var regex = new Regex($@"^{FullFolderPath}(\\{pathPattern})*\\{filenamePattern}\.hpp");
            var filepaths0 = Directory.GetFiles(FullFolderPath, "*.*", SearchOption.AllDirectories);
            var filepaths1 = Directory.GetFiles(FullFolderPath);
            var filepaths = Directory.GetFiles(FullFolderPath, "*.*", SearchOption.AllDirectories).Where( fullpath => regex.IsMatch(fullpath) ).ToList();
            foreach(var filepath in filepaths)
            {
                Log.WriteLine($"- {filepath}");
            }
        }
    }

}
