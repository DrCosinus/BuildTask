using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    public class FileUtility
    {
        // remove "folder/.." pattern
        // remove "." pattern
        private static string[] SplitAndDustPath(string _path)
        {
            var splits = _path.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            splits = splits.Where(s => s != ".").ToArray();
            while (true)
            {
                var ix = splits.Select((s, i) => new { str = s, idx = i }).Where(o => o.idx != 0 && o.str == "..").Select(o => o.idx).FirstOrDefault();
                if (ix == 0)
                    break;
                splits = splits.Where((s, i) => i != ix - 1 && i != ix).ToArray();
            }
            return splits;
        }

        public static string MakeRelative(string _referencePath, string _path)
        {
            var ref_splits = SplitAndDustPath(_referencePath);

            var path_splits = SplitAndDustPath(_path);

            int count = 0;
            int n = Math.Min(ref_splits.Count(), path_splits.Count());
            for (int i = 0; i < n; ++i)
            {
                if (string.Compare(ref_splits[i], path_splits[i], true)!=0)
                    break;
                ++count;
            }
            return ref_splits.Skip(count).Select(x => "..").Concat(path_splits.Skip(count)).DefaultIfEmpty("").Aggregate((s1, s2) => s1 + Path.DirectorySeparatorChar + s2);
        }

        //Assert.AreEqual(FileUtility.MakeRelative(@"A\B/C", @"A/B\C"), "");
        //Assert.AreEqual(FileUtility.MakeRelative(@"A\B/../C", @"A\C\E"), "E");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C/D/E", "A/B/X/Y"), @"..\..\..\X\Y");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C", "A/B/C/Z"), @"Z");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C/Z", "A/B/C"), @"..");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C/Z", "A/B/C/"), @"..");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C/Z/", "A/B/C/"), @"..");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C/Z/", "A/B/C"), @"..");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C", "."), @"..\..\..");
        //Assert.AreEqual(FileUtility.MakeRelative(".", "A/B/C" ), @"A\B\C");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C/Z", "A/B/C.exe"), @"..\..\C.exe");
        //Assert.AreEqual(FileUtility.MakeRelative("", "A/B/C"), @"A\B\C");
        //Assert.AreEqual(FileUtility.MakeRelative("A/B/C", ""), @"..\..\..");
    }
}
