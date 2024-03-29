﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask.Compilers
{
    interface ICompiler
    {
        ECppVersion CppVersion { set; }
        EWarningLevel WarningLevel { set; }
        EDebugLevel DebugLevel { set; get; }
        bool WarningAsErrors { set; }
        List<string> SourceFilePaths { set; }
        List<string> LibFilepaths { set; }
        string OutputFilepath { set; }
        string IntermediaryFileFolderName { set; }
        string ShortName { get; }
        IEnumerable<string> AdditionalIncludePaths { set; }
        List<string> Defines { set; }
        List<string> ExtraFlags { set; }

        int Run();
    }
}
