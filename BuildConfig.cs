using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildTask
{
    class Project
    {
        public string[] Sourcefiles;
        public string[] Dependencies;
    }
    class Config
    {
        public Project[] Projects;
    }
    class BuildConfig
    {
        public BuildConfig(string _filename)
        {
            string definitions = "";
            using (var reader = new StreamReader(_filename))
            {
                definitions = reader.ReadToEnd();
            }
            Parse(definitions);
        }

        private Config Parse(string definitions)
        {
            definitions = RemoveAllComments(definitions);
            var re = new Regex(@"project\(<project_name>(\w)+\)");
            var matched = re.Matches(definitions);
            return new Config
            {
                Projects = new Project[]
                {
                    new Project
                    {
                        Sourcefiles = new string[] { "enum.cpp" },
                        Dependencies = new string[] { "tdd/simple_tests.hpp" }
                    }
                }
            };

        }

        private string RemoveAllComments(string definitions)
        {
            var reBlock = new Regex(@"/\*((?!\*/).)*\*/", RegexOptions.Multiline);
            definitions = reBlock.Replace(definitions, "");
            return definitions;
        }
    }
}
