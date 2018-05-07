using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    interface ICompiler
    {
        ECppVersion? CppVersion { set; }
        EWarningLevel? WarningLevel { set; }
        EDebugLevel? DebugLevel { set; }
        bool WarningAsErrors { set; }
        List<string> SourceFilePaths { set; }
        string OutputFilepath { set; }
        string IntermediaryFileFolderName { set; }

        int Run();
    }
}
