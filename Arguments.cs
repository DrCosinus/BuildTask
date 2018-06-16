using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    internal class Arguments
    {
        public WriteCounter<ECompiler> Compiler;
        public WriteCounter<EDebugLevel> DebugLevel;
        public string OutputFilename;
        public EWarningLevel? WarningLevel;
        public ECppVersion? StandardCpp;
        public List<string> SourceFilenames = new List<string>();
        public bool ForceCompilation = false;
        public bool WarningsAreErrors = false;
    }
}
