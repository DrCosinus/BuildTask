using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BuildTask
{
    public static class CollectionExtensions
    {
        public static IEnumerable<Match> Enumerate(this MatchCollection matchCollection)
        {
            var enumerator = matchCollection.GetEnumerator();
            while(enumerator.MoveNext())
            {
                yield return enumerator.Current as Match;
            }
        }
    }
}
