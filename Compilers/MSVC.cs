using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask.Compilers
{
    class MSVC : AbstractCompiler
    {
        public MSVC()
        {
            string[] vintages = { "Preview", "2017" };
            string[] flavors = { "Professional", "Community" };
            foreach (var vintage in vintages)
            {
                foreach (var flavor in flavors)
                {
                    string candidate = $@"C:\Program Files (x86)\Microsoft Visual Studio\{vintage}\{flavor}\VC\Tools\MSVC";
                    if (Directory.Exists(candidate))
                    {
                        var sub_folders = Directory.GetDirectories($@"C:\Program Files (x86)\Microsoft Visual Studio\{vintage}\{flavor}\VC\Tools\MSVC");
                        var filtered = sub_folders.Where(path =>
                           Directory.Exists(Path.Combine(path, @"bin\HostX64\x64"))
                           && Directory.Exists(Path.Combine(path, @"include"))
                           && Directory.Exists(Path.Combine(path, @"lib\x64"))
                            );
                        if (filtered.Count() == 1)
                        {
                            Log.WriteLine($"Found MSVC \"{candidate}\"...");
                            MSVCPath = filtered.First();
                            return;
                        }
                    }
                }
            }
            throw new Exception("Can not find MSVC!");
        }
        private static string MSVCPath;
        private static string WindowsKitPath => @"C:\Program Files (x86)\Windows Kits"; // should be deduced
        private static string Windows10KitPath(string _group) => $@"{WindowsKitPath}\10\{_group}\10.0.16299.0"; // should be deduced
                                                                                                                //private static string DotNetFrameworkPath => $@"{WindowsKitPath}\NETFXSDK\4.6.1"; // should be deduced

        protected override void SetupEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("PATH", string.Join(";"
                , $@"{MSVCPath}\bin\HostX64\x64"
                , $"{Environment.GetEnvironmentVariable("PATH")}"
                ));
            Environment.SetEnvironmentVariable("INCLUDE", string.Join(";"
                //, $@"{MSVCPath}\ATLMFC\include"
                , $@"{MSVCPath}\include"
                //, $@"{DotNetFrameworkPath}\include\um"
                , $@"{Windows10KitPath("include")}\ucrt"
                , $@"{Windows10KitPath("include")}\shared"
                , $@"{Windows10KitPath("include")}\um"
                //, $@"{Windows10KitPath("include")}\winrt"
                //, $@"{Windows10KitPath}\cppwinrt"
                ));
            Environment.SetEnvironmentVariable("LIB", string.Join(";"
                //, $@"{MSVCPath}\ATLMFC\lib\x64"
                , $@"{MSVCPath}\lib\x64"
                //, $@"{DotNetFrameworkPath}\lib\um\x64"
                , $@"{Windows10KitPath("lib")}\ucrt\x64"
                , $@"{Windows10KitPath("lib")}\um\x64"
                ));
            // Environment.SetEnvironmentVariable( "LIBPATH", string.Join(";"
            //     , $@"{MSVCPath}\ATLMFC\lib\x64"
            //     , $@"{MSVCPath}\lib\x64"
            //     , $@"{MSVCPath}\lib\x86\store\references"
            //     , $@"{Windows10KitPath("UnionMetadata")}"
            //     , $@"{Windows10KitPath("References")}"
            //     , @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
            //     ));
        }
        protected override void PrepareRun()
        {
            base.PrepareRun();
            if (!string.IsNullOrEmpty(IntermediaryFileFolderName))
            {
                new DirectoryInfo(IntermediaryFileFolderName).Create();
            }
        }
        protected override string ExecutableName => "cl";
        private string GenerateCompilationParametersString()
        {
            List<string> parameters = new List<string>();
            if (CppVersion.HasValue)
            {
                switch (CppVersion)
                {
                    case ECppVersion.Cpp11: parameters.Add("/std:c++11"); break;
                    case ECppVersion.Cpp14: parameters.Add("/std:c++14"); break;
                    case ECppVersion.Cpp17: parameters.Add("/std:c++17"); break;
                    case ECppVersion.Cpp20: parameters.Add("/std:c++latest"); break;
                    default: goto case ECppVersion.Cpp17;
                }
            }
            switch (WarningLevel.GetValueOrDefault(EWarningLevel.High))
            {
                case EWarningLevel.None: parameters.Add("/W0"); break;
                case EWarningLevel.Low: parameters.Add("/W1"); break;
                case EWarningLevel.MediumLow: parameters.Add("/W2"); break;
                case EWarningLevel.MediumHigh: parameters.Add("/W3"); break;
                case EWarningLevel.High: parameters.Add("/W4"); break;
                case EWarningLevel.Max: parameters.Add("/Wall"); break;
                default: goto case EWarningLevel.Low;
            }
            switch (DebugLevel.GetValueOrDefault(EDebugLevel.NonDebug))
            {
                case EDebugLevel.Debug:
                    parameters.Add("/DDEBUG=1");
                    parameters.Add("/Zi");
                    parameters.Add("/Od");
                    break;
                case EDebugLevel.NonDebug:
                    parameters.Add("/DDEBUG=0");
                    parameters.Add("/DNDEBUG");
                    parameters.Add("/Ox");
                    break;
            }
            if (WarningAsErrors)
            {
                parameters.Add("/WX");
            }
            parameters.AddRange(SourceFilePaths);
            parameters.Add($"/Fe{OutputFilepath}");
            parameters.Add("/EHsc"); // avoid warning C4530: C++ exception handler used, but unwind semantics are not enabled. Specify /EHsc
            parameters.Add("/permissive-"); // disable soms nonconforming code to compile
                                            //parameters.Add("/Za"); // disable extensions... unfortunately some extension are required in some windows header :(
            parameters.Add("/nologo"); // disable copyright message
            if (!string.IsNullOrEmpty(IntermediaryFileFolderName))
            {
                parameters.Add($"/Fo{IntermediaryFileFolderName}/");
            }
            return string.Join(" ", parameters);
        }
        protected override string CompilationParameters
        {
            get => GenerateCompilationParametersString();
        }
    }

}
