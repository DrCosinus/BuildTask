using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildTask
{
    class CommandLine
    {
        internal enum NeedValue
        {
            NoValue,
            OneValue,
        }

        private class FlagDefinition
        {
            internal string name;
            internal NeedValue needValue;
            uint valueCount = 0;

            public override string ToString()
            {
                return $"-{name}{ (needValue==NeedValue.OneValue?"(one value)":"(no value)") }";
            }
        }

        const string keyGroupName = "key";
        const string valueGroupName = "value";
        static string wordRegEx(string _word) => $"(?<{ _word }>[a-zA-z0-9_][a-zA-z0-9_-]*)";
        Regex regexDefinition = new Regex( "^[-/]" + wordRegEx(keyGroupName) + "(?:[:=]" + wordRegEx(valueGroupName) + ")?$", RegexOptions.Compiled | RegexOptions.Singleline);
        Regex regexAnyValue = new Regex("^[^-/].+$", RegexOptions.Compiled | RegexOptions.Singleline);
        Regex regexNotADefinition = new Regex("^[^-/:=][^:=]*$", RegexOptions.Compiled | RegexOptions.Singleline);

        Dictionary<string, string> definitions_ = new Dictionary<string, string>(); // TO DO: support multiple values for a flag
        public IEnumerable<string> Files { get; private set; } = null;
        List<FlagDefinition> flags_ = new List<FlagDefinition>();

        internal void RegisterFlag(string _flagName, NeedValue _flagNeedValue = NeedValue.NoValue)
        {
            if (flags_.Any(f => f.name == _flagName))
            {
                Log.WriteLine($"Flag \"{ _flagName }\" already registred!");
                return;
            }
            flags_.Add(new FlagDefinition { name = _flagName, needValue = _flagNeedValue });
        }

        private bool Define(string _flag, string _value)
        {
            if (definitions_.ContainsKey(_flag))
            {
                Log.WriteLine($@"Commandline flag ""{ _flag }"" already defined");
            }
            definitions_.Add(_flag, _value);
            return true;
        }

        // [-\/](?<key>[^ :=]+)(?:[ :=](?<value>[^ ]+))?(?:\s|$)
        // [-\/](?:(?<key>fofo|compiler|force)(?:[ :=](?<value>[^-](?:(?!\s|$).)*)))|(?<key0>fuck)
        public void Parse(string[] commandline_args)
        {
            string all_args = string.Join(" ", commandline_args);
            string OneValueFlags = string.Join("|", flags_.Where(f => f.needValue == NeedValue.OneValue).Select(f => f.name));
            string NoValueFlags = string.Join("|", flags_.Where(f => f.needValue == NeedValue.NoValue).Select(f => f.name));

            var NoValueRegex = new Regex($@"[/-](?<{ keyGroupName }>{ NoValueFlags })(?:\s|$)"); // todo: manage double quotes
            var OneValueRegex = new Regex($@"[/-](?<{ keyGroupName }>{ OneValueFlags })[ :=](?<{ valueGroupName }>(?:((?!\s|$).)+))(?:\s|$)"); // todo: manage double quotes
            var noval = NoValueRegex.Matches(all_args);
            var oneval = OneValueRegex.Matches(all_args);
            foreach (var m in noval.Enumerate())
            {
                definitions_.Add(m.Groups[keyGroupName].Value, "");
            }

            foreach (var m in oneval.Enumerate())
            {
                definitions_.Add(m.Groups[keyGroupName].Value, m.Groups[valueGroupName].Value);
            }

            Files = OneValueRegex.Replace(NoValueRegex.Replace(all_args, ""), "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool IsPresent(string _flag)
        {
            return definitions_.Count(kv => kv.Key==_flag) > 0;
        }

        public bool TryGet(string _flag, out string _value)
        {
            return definitions_.TryGetValue(_flag, out _value);
        }

        public bool TryGet(string _flag, out int _value)
        {
            string str;
            if (definitions_.TryGetValue(_flag, out str))
            {
                return int.TryParse(str, out _value);
            }
            else
            {
                _value = default(int);
                return false;
            }
        }

    }
}
