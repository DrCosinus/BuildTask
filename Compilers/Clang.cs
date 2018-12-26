using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask.Compilers
{
    class Clang : AbstractCompiler
    {
        public override string ShortName => "clang";

        protected override string ExecutableName => "clang";
        private string GenerateCompilationParametersString()
        {
            List<string> parameters = new List<string>();
            switch (CppVersion)
            {
                case ECppVersion.Cpp11: parameters.Add("-std=c++11"); break;
                case ECppVersion.Cpp14: parameters.Add("-std=c++14"); break;
                case ECppVersion.Cpp17: parameters.Add("-std=c++17"); break;
                case ECppVersion.Cpp20: parameters.Add("-std=c++2a"); break;
                default: goto case ECppVersion.Cpp17;
            }
            switch (WarningLevel)
            {
                case EWarningLevel.None: break;
                case EWarningLevel.Low: parameters.Add("-Wall"); break;
                case EWarningLevel.Few: parameters.Add("-Wall -pedantic"); break;
                case EWarningLevel.Medium: parameters.Add("-Wall -pedantic"); break;
                case EWarningLevel.High: parameters.Add("-Wall -pedantic -Wextra"); break;
                case EWarningLevel.Max: parameters.Add("-Wall -pedantic -Wextra -Weverything"); break; // should consider to disable the C++98, C++03 specific warnings
                default: goto case EWarningLevel.Low;
            }
            switch (DebugLevel)
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
            if (AdditionalIncludePaths != null)
            {
                foreach (var path in AdditionalIncludePaths)
                {
                    parameters.Add($"-I{path}");
                }
            }
            parameters.AddRange(SourceFilePaths);
            parameters.Add($"-o {OutputFilepath}");
            parameters.Add("-Xclang -flto-visibility-public-std");

            if (Defines != null)
            {
                foreach (var def in Defines)
                {
                    parameters.Add($"-D{def}");
                }
            }
            if (ExtraFlags != null)
            {
                foreach (var flag in ExtraFlags)
                {
                    parameters.Add($"-{flag}");
                }
            }

            if (LibFilepaths != null)
            {
                foreach (var lib in LibFilepaths)
                {
                    parameters.Add($"-Wl,{lib}");
                }
            }
            return string.Join(" ", parameters);
        }

        protected override string CompilationParameters
        {
            get => GenerateCompilationParametersString();
        }
    }
}
