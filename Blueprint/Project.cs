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

        //static internal string fileCharsPattern = $@"[^{ Path.GetInvalidFileNameChars().Aggregate("", (str, c) => $"{str}{c.ToString()}") }]";
        private static readonly string fileCharsPattern = $@"[^\x00-\x1f""<>|:*?\\\/]"; // easier to debug
        private static readonly string recursivePattern = $@"({fileCharsPattern}+\\)*";
        private static readonly string filePattern = $@"{fileCharsPattern}*";

        private IEnumerable<string> ResolveWilcards(IEnumerable<string> patterns)
        {
            // ** => (\\{filenameCharsPattern}+)* recursive folders
            // * => {filenameCharsPattern}*
            // . => \.
            return patterns?.
                Select(searchPattern =>
                {
                    var re = new Regex($@"^{ FullFolderPath.Replace(@"\", @"\\") }\\{ searchPattern.Replace("/", "\\").Replace(@"\", @"\\").Replace(@"**\\", "<recursivePattern>").Replace("*", filePattern).Replace("<recursivePattern>", recursivePattern).Replace(".", @"\.")}");
                    return Directory.GetFiles(FullFolderPath, "*", SearchOption.AllDirectories).Where(filepath => re.IsMatch(filepath));
                }).Aggregate((s1, s2) => { var sum = new List<string>(s1); sum.AddRange(s2); return sum as IEnumerable<string>; });
        }

        internal IEnumerable<string> ResolvedSourceFullPaths => ResolveWilcards(Sources);
        internal IEnumerable<string> ResolvedSourceRelativePaths => ResolvedSourceFullPaths.Select(s => FileUtility.MakeRelative(FullFolderPath, s));
        internal IEnumerable<string> ResolvedHeaderFullPaths => ResolveWilcards(Headers);
    }

}
