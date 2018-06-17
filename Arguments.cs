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
        public EWarningLevel? WarningLevel;
        public ECppVersion? StandardCpp;
        public bool ForceCompilation = false;
        public bool WarningsAreErrors = false;
    }
}
