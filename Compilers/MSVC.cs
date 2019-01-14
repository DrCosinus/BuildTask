using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
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
        public override string ShortName => "msvc";

        // https://docs.microsoft.com/en-us/visualstudio/extensibility/breaking-changes-2017
        // https://stackoverflow.com/questions/42916299/access-visual-studio-2017s-private-registry-hive
        // https://stackoverflow.com/questions/2917309/regloadappkey-working-fine-on-32-bit-os-failing-on-64-bit-os-even-if-both-proc

        // we need a windows kit abstraction

        public MSVC()
        {
            DiscoverVisualStudio();
        }

        class VisualStudioInfo
        {
            public string CompilerPath;
            public string IncludesPath;
            public string LibsPath;
        }

        private void DiscoverVisualStudio()
        {
            var vs2017 = DiscoverModernVisualStudio("15.0"); // VS 2017
            if (vs2017 != null)
            {
                visualStudioInfo = vs2017;
            }
        }

        // from Visual Studio 2017, and future releases
        private VisualStudioInfo DiscoverModernVisualStudio(string vsWantedVersion)
        {
            var query = new SetupConfiguration();
            var query2 = (ISetupConfiguration2)query;
            var e = query2.EnumAllInstances();

            int fetched;
            var instances = new ISetupInstance[1];
            do
            {
                e.Next(1, instances, out fetched);
                if (fetched > 0)
                {
                    return Parse(instances[0], vsWantedVersion);
                }
            }
            while (fetched > 0);
            return null;
        }

        private static VisualStudioInfo Parse(ISetupInstance instance, string vsWantedVersion /*, ISetupHelper helper*/)
        {
            var instance2 = (ISetupInstance2)instance;
            var state = instance2.GetState();

            //VS 2017
            var hKey = RegistryNative.RegLoadAppKey($@"{ Environment.GetEnvironmentVariable("LOCALAPPDATA") }\Microsoft\VisualStudio\{ vsWantedVersion }_{ instance2.GetInstanceId() }\privateregistry.bin");
            using (var safeRegistryHandle = new SafeRegistryHandle(new IntPtr(hKey), true))
            {
                if (safeRegistryHandle != null)
                {
                    using (var appKey = RegistryKey.FromHandle(safeRegistryHandle))
                    {
                        if (appKey != null)
                        {
                            var vcKey = appKey.OpenSubKey($@"Software\Microsoft\VisualStudio\15.0_{ instance2.GetInstanceId() }_Config\VC");
                            if (vcKey != null)
                            {
                                var t = vcKey.GetSubKeyNames();
                                var vc19Key = vcKey.OpenSubKey(vcKey.GetSubKeyNames()[0]);
                                string host = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                                string target = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                                var compilerKey = vc19Key.OpenSubKey($@"{ host }\{ target }"); // host\target

                                var compiler_path = compilerKey.GetValue("Compiler") as string;
                                var folder = Path.GetDirectoryName(compiler_path);
                                while (folder != null && !Directory.Exists(Path.Combine(folder, "include")))
                                {
                                    folder = Directory.GetParent(folder)?.FullName;
                                }
                                var include_path = Path.Combine(folder, "include");
                                var libs_path = Path.Combine(folder, $"lib\\{target}");

                                return new VisualStudioInfo { CompilerPath = Path.GetDirectoryName(compiler_path), IncludesPath = include_path, LibsPath = libs_path };
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static string LatestWindows10Kit()
        {
            var kitVersions = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots").GetSubKeyNames();
            // FIXME: for now, we return the last one...
            return kitVersions.Where(s => s.StartsWith("10.")).Last();
        }

        private VisualStudioInfo visualStudioInfo;
        private static string Windows10KitBasePath => Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots", "KitsRoot10", "") as string;
        private static string Windows10KitPath(string _group) => $@"{Windows10KitBasePath}\{_group}\{ LatestWindows10Kit() }";
        //private static string DotNetFrameworkPath => $@"{WindowsKitPath}\NETFXSDK\4.6.1"; // should be deduced

        protected override void SetupEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("PATH", string.Join(";"
                , $@"{visualStudioInfo.CompilerPath}"
                , $"{Environment.GetEnvironmentVariable("PATH")}"
                ));
            Environment.SetEnvironmentVariable("INCLUDE", string.Join(";"
                //, $@"{MSVCPath}\ATLMFC\include"
                , $@"{visualStudioInfo.IncludesPath}"
                //, $@"{DotNetFrameworkPath}\include\um"
                , $@"{Windows10KitPath("include")}\ucrt"
                , $@"{Windows10KitPath("include")}\shared"
                , $@"{Windows10KitPath("include")}\um"
                //, $@"{Windows10KitPath("include")}\winrt"
                //, $@"{Windows10KitPath}\cppwinrt"
                ));
            Environment.SetEnvironmentVariable("LIB", string.Join(";"
                //, $@"{MSVCPath}\ATLMFC\lib\x64"
                , $@"{visualStudioInfo.LibsPath}"
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

            switch (CppVersion)
            {
                case ECppVersion.Cpp11: parameters.Add("/std:c++11"); break; // unsupported flag
                case ECppVersion.Cpp14: parameters.Add("/std:c++14"); break;
                case ECppVersion.Cpp17: parameters.Add("/std:c++17"); break;
                case ECppVersion.Cpp20: parameters.Add("/std:c++latest"); break;
                default: goto case ECppVersion.Cpp17;
            }
            switch (WarningLevel)
            {
                case EWarningLevel.None: parameters.Add("/W0"); break;
                case EWarningLevel.Low: parameters.Add("/W1"); break;
                case EWarningLevel.Few: parameters.Add("/W2"); break;
                case EWarningLevel.Medium: parameters.Add("/W3"); break;
                case EWarningLevel.High: parameters.Add("/W4"); break;
                case EWarningLevel.Max: parameters.Add("/Wall"); break;
                default: goto case EWarningLevel.Low;
            }
            switch (DebugLevel)
            {
                case EDebugLevel.Debug:
                    parameters.Add("/DDEBUG=1");
                    parameters.Add("/Zi");  // enable debugging information 
                    parameters.Add("/Od");
                    // parameters.Add("/Yd");  // [DEPRECATED] put debug info in every.OBJ
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
            if (AdditionalIncludePaths != null)
            {
                foreach (var path in AdditionalIncludePaths)
                {
                    parameters.Add($"/I{path}");
                }
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

            //parameters.Add("/FC"); // fullpath in diagnotics

            if (Defines != null)
            {
                foreach (var def in Defines)
                {
                    parameters.Add($"/D{def}");
                }
            }
            if (ExtraFlags != null)
            {
                foreach (var flag in ExtraFlags)
                {
                    parameters.Add($"/{flag}");
                }
            }
            // /link must be the last flag the remaining line will be pass to the linker
            if (LibFilepaths != null)
            {
                parameters.Add($"/link");
                parameters.AddRange(LibFilepaths);
            }
            return string.Join(" ", parameters);
        }
        protected override string CompilationParameters
        {
            get => GenerateCompilationParametersString();
        }
    }

}
