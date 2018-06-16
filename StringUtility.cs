using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    public static class StringUtility
    {
        public static string ReplaceVariable(this string _str, string _variable, string _value)
        {
            return _str.Replace($"$({_variable})", _value);
        }
    }
}
