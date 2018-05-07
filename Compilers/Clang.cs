using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask.Compilers
{
    class Clang : AbstractCompiler
    {
        protected override string ExecutableName => "clang";
        private string GenerateCompilationParametersString()
        {
            List<string> parameters = new List<string>();
            if (CppVersion.HasValue)
            {
                switch (CppVersion)
                {
                    case ECppVersion.Cpp11: parameters.Add("-std=c++11"); break;
                    case ECppVersion.Cpp14: parameters.Add("-std=c++14"); break;
                    case ECppVersion.Cpp17: parameters.Add("-std=c++17"); break;
                    case ECppVersion.Cpp20: parameters.Add("-std=c++2a"); break;
                    default: goto case ECppVersion.Cpp17;
                }
            }
            switch (WarningLevel.GetValueOrDefault(EWarningLevel.High))
            {
                case EWarningLevel.None: break;
                case EWarningLevel.Low: parameters.Add("-Wall"); break;
                case EWarningLevel.MediumLow: parameters.Add("-Wall -pedantic"); break;
                case EWarningLevel.MediumHigh: parameters.Add("-Wall -pedantic"); break;
                case EWarningLevel.High: parameters.Add("-Wall -pedantic -Wextra"); break;
                case EWarningLevel.Max: parameters.Add("-Wall -pedantic -Wextra"); break;
                default: goto case EWarningLevel.Low;
            }
            switch (DebugLevel.GetValueOrDefault(EDebugLevel.NonDebug))
            {
                case EDebugLevel.Debug:
                    parameters.Add("-DDEBUG=1");
                    parameters.Add("-O0");
                    break;
                case EDebugLevel.NonDebug:
                    parameters.Add("-DDEBUG=0");
                    parameters.Add("-DNDEBUG");
                    parameters.Add("-O3");
                    break;
            }
            if (WarningAsErrors)
            {
                parameters.Add("-Werror");
            }
            parameters.AddRange(SourceFilePaths);
            parameters.Add($"-o {OutputFilepath}");
            parameters.Add("-Xclang -flto-visibility-public-std");
            return string.Join(" ", parameters);
        }

        protected override string CompilationParameters
        {
            get => GenerateCompilationParametersString();
        }
    }
}
