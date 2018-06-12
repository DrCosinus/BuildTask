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
        Dictionary<string, string> definitions_ = new Dictionary<string, string>();
        List<string> files_ = new List<string>();
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
            noval.Enumerate();
            //string pending_definition = null;
            foreach (var commandline_arg in commandline_args)
            {
                //if (pending_definition!=null)
                //{
                //    var match = regexAnyValue.Match(commandline_arg);
                //    if (match.Success)
                //    {
                //        definitions_[pending_definition] = commandline_arg;
                //        pending_definition = null;
                //    }
                //    else
                //    {
                //        Log.WriteLine("Command line argument error: \"{ pending_definition }\" = \"{ commandline_arg }\"");
                //        pending_definition = null;
                //        continue;
                //    }
                //}
                var definition_match = regexDefinition.Match(commandline_arg);
                if (definition_match.Success)
                {
                    var k = definition_match.Groups[keyGroupName].Value;
                    var v = definition_match.Groups[valueGroupName].Value;
                    var flags = flags_.Where(f => f.name == k);
                    if (flags.Count()!=0)
                    {
                        var flag = flags.First();
                        if (flag.needValue == NeedValue.OneValue)
                        {
                            definitions_[definition_match.Groups[keyGroupName].Value] = v;
                        }
                        else
                        {
                            Log.WriteLine($"Flag: \"{ k }\" in command line argument should not have value\"{ commandline_arg }\"");
                        }
                    }
                    else
                    {
                        Log.WriteLine($"Unregistered flag: \"{ k }\" in command line argument \"{ commandline_arg }\"");
                    }
                }
                else
                {
                    var file_match = regexNotADefinition.Match(commandline_arg);
                    if (file_match.Success)
                    {
                        files_.Add(commandline_arg);
                    }
                    else
                    {
                        Log.WriteLine($"Command line argument error: \"{ commandline_arg }\"");
                        continue;
                    }
                }
            }
        }
        bool IsPresent(string anArgument)
        {
            return false;
        }
    }
}
