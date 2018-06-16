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
        // https://docs.microsoft.com/en-us/visualstudio/extensibility/breaking-changes-2017
        // https://stackoverflow.com/questions/42916299/access-visual-studio-2017s-private-registry-hive
        // https://stackoverflow.com/questions/2917309/regloadappkey-working-fine-on-32-bit-os-failing-on-64-bit-os-even-if-both-proc

        // we need a windows kit abstraction

        enum millesim
        {
            VS_Community_2015
            , VS_Professional_2015
            , VS_Community_2017
            , VS_Professional_2017
            , VS_Enterprise_2017
            , VS_Community_Preview2017
            , VS_Professional_Preview2017
            , VS_Enterprise_Preview2017
        };
        enum toolChain { x86, x64 };
        enum target { x86, x64, arm };

        // Environment.Is64BitOperatingSystem => Wow6432Node

        // ## VISUAL STUDIO 14 -> 2015
        // =>   C:\Program Files (x86)\Microsoft Visual Studio 14.0\VC
        //      register base: \HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\14.0 + "VC"
        //
        // should contain
        //      bin\amd64\      for building with 64bit toolchain for 64bit     HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\VC\19.0\x64\x64\Compiler
        //      bin\amd64_x86\  for building with 64bit toolchain for 32bit
        //      bin\amd64_arm\  for building with 64bit toolchain for arm       HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio\VC\19.0\x64\arm\Compiler
        //      bin\x86_amd64\  for building with 32bit toolchain for 64bit
        //      bin\            for building with 32bit toolchain for 32bit
        //      bin\x86_arm\    for building with 32bit toolchain for arm
        //      lib\            for building for 32bit
        //      lib\amd64\      for building for 64bit
        //      include\        includes
        //
        // INCLUDE =
        //      C:\Program Files(x86)\Microsoft Visual Studio 14.0\VC\INCLUDE;
        //      C:\Program Files(x86)\Microsoft Visual Studio 14.0\VC\ATLMFC\INCLUDE;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\ucrt;
        //      C:\Program Files(x86)\Windows Kits\NETFXSDK\4.6.1\include\um;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\shared;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\um;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\winrt;
        //
        //  LIB =
        //      C:\Program Files(x86)\Microsoft Visual Studio 14.0\VC\LIB;
        //      C:\Program Files(x86)\Microsoft Visual Studio 14.0\VC\ATLMFC\LIB;
        //      C:\Program Files(x86)\Windows Kits\10\lib\10.0.17134.0\ucrt\x86;
        //      C:\Program Files(x86)\Windows Kits\NETFXSDK\4.6.1\lib\um\x86;
        //      C:\Program Files(x86)\Windows Kits\10\lib\10.0.17134.0\um\x86;
        //
        //  LIBPATH =
        //      C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319;
        //      C:\Program Files(x86)\Microsoft Visual Studio 14.0\VC\LIB;
        //      C:\Program Files(x86)\Microsoft Visual Studio 14.0\VC\ATLMFC\LIB;
        //      C:\Program Files(x86)\Windows Kits\10\UnionMetadata;
        //      C:\Program Files(x86)\Windows Kits\10\References;
        //      \Microsoft.VCLibs\14.0\References\CommonConfiguration\neutral;

        // ## VISUAL STUDIO 15 -> 2017
        // =>   Professional => C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\VC\Tools\MSVC
        // =>   Community => C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC
        // =>   Professional Preview => C:\Program Files (x86)\Microsoft Visual Studio\Preview\Professional\VC\Tools\MSVC
        // =>   Community Preview => C:\Program Files (x86)\Microsoft Visual Studio\Preview\Community\VC\Tools\MSVC
        // should contain
        //      14.xx.yyyyy\bin\Hostx64\x64\    for building with 64bit toolchain for 64bit
        //      14.xx.yyyyy\bin\Hostx64\x86\    for building with 64bit toolchain for 32bit
        //      14.xx.yyyyy\bin\Hostx86\x64\    for building with 32bit toolchain for 64bit
        //      14.xx.yyyyy\bin\Hostx86\x86\    for building with 32bit toolchain for 32bit
        //      14.xx.yyyyy\bin\Hostx86\arm\    for building with 32bit toolchain for arm32bit
        //      14.xx.yyyyy\bin\Hostx86\arm64\  for building with 32bit toolchain for arm64bit
        //      14.xx.yyyyy\lib\x64\            for building for 64bit
        //      14.xx.yyyyy\lib\x86\            for building for 32bit
        //      14.xx.yyyyy\include\            includes
        //
        // INCLUDE =
        //      C:\Program Files(x86)\Microsoft Visual Studio\2017\Professional\VC\Tools\MSVC\14.14.26428\ATLMFC\include;
        //      C:\Program Files(x86)\Microsoft Visual Studio\2017\Professional\VC\Tools\MSVC\14.14.26428\include;
        //      C:\Program Files(x86)\Windows Kits\NETFXSDK\4.6.1\include\um;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\ucrt;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\shared;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\um;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\winrt;
        //      C:\Program Files(x86)\Windows Kits\10\include\10.0.17134.0\cppwinrt
        // LIB =
        //      C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\VC\Tools\MSVC\14.14.26428\ATLMFC\lib\x86;
        //      C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\VC\Tools\MSVC\14.14.26428\lib\x86;
        //      C:\Program Files (x86)\Windows Kits\NETFXSDK\4.6.1\lib\um\x86;
        //      C:\Program Files (x86)\Windows Kits\10\lib\10.0.17134.0\ucrt\x86;
        //      C:\Program Files (x86)\Windows Kits\10\lib\10.0.17134.0\um\x86;
        //
        // LIBPATH =
        //      C:\Program Files(x86)\Microsoft Visual Studio\2017\Professional\VC\Tools\MSVC\14.14.26428\ATLMFC\lib\x86;
        //      C:\Program Files(x86)\Microsoft Visual Studio\2017\Professional\VC\Tools\MSVC\14.14.26428\lib\x86;
        //      C:\Program Files(x86)\Microsoft Visual Studio\2017\Professional\VC\Tools\MSVC\14.14.26428\lib\x86\store\references;
        //      C:\Program Files(x86)\Windows Kits\10\UnionMetadata\10.0.17134.0;
        //      C:\Program Files(x86)\Windows Kits\10\References\10.0.17134.0;
        //      C:\WINDOWS\Microsoft.NET\Framework\v4.0.30319;

        public MSVC()
        {
            DiscoverVisualStudio();

            var drives = DriveInfo.GetDrives().Select(d => d.Name);
            string[] vintages = { "Preview", "2017" };
            string[] flavors = { "Professional", "Community" };

            foreach (var drive in drives)
            {
                foreach (var vintage in vintages)
                {
                    foreach (var flavor in flavors)
                    {
                        string candidate = $@"{drive}Program Files (x86)\Microsoft Visual Studio\{vintage}\{flavor}\VC\Tools\MSVC";
                        if (Directory.Exists(candidate))
                        {
                            var sub_folders = Directory.GetDirectories(candidate);
                            var filtered = sub_folders.Where(path =>
                               Directory.Exists(Path.Combine(path, @"bin\HostX64\x64"))
                               && Directory.Exists(Path.Combine(path, @"include"))
                               && Directory.Exists(Path.Combine(path, @"lib\x64"))
                                );
                            // todo: should consider to accept more than one version
                            //      interactive mode: manual selection
                            //      automatic mode: most recent version
                            if (filtered.Count() == 1)
                            {
                                MSVCPath = filtered.First();
                                Log.WriteLine($"Found MSVC \"{ MSVCPath }\"...");
                                return;
                            }
                        }
                    }
                }
            }
            throw new Exception("Can not find MSVC!");
        }

        class VisualStudioInfo
        {
            public string CompilerPath;
            public string IncludesPath;
        }

        private void DiscoverVisualStudio()
        {
            var vs2017 = DiscoverModernVisualStudio("15.0"); // VS 2017
            if (vs2017!=null)
            {
                Log.WriteLine("Compiler: " + vs2017.CompilerPath);
                Log.WriteLine("Include: " + vs2017.IncludesPath);

                // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\ -> KitsRoot10
                string WindowsKit10 = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots", "KitsRoot10", "") as string;
                Log.WriteLine("Windows Kit 10: " + WindowsKit10);
            }
        }

        // from Visual Studio 2017, and future releases
        private VisualStudioInfo DiscoverModernVisualStudio(string vsWantedVersion)
        {
            var query = new SetupConfiguration();
            var query2 = (ISetupConfiguration2)query;
            var e = query2.EnumAllInstances();

            //var helper = (ISetupHelper)query;

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
            // Log.WriteLine($"InstanceId: {instance2.GetInstanceId()} ({(state == InstanceState.Complete ? "Complete" : "Incomplete")})");

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

                                //Print(appKey);

                                var compiler_path = compilerKey.GetValue("Compiler") as string;
                                var folder = Path.GetDirectoryName(compiler_path);
                                while (folder!=null && !Directory.Exists(Path.Combine(folder, "include")))
                                {
                                    folder = Directory.GetParent(folder)?.FullName;
                                }
                                var include_path = Path.Combine(folder, "include");

                                return new VisualStudioInfo { CompilerPath = compiler_path, IncludesPath = include_path };
                                //Print(appKey.OpenSubKey($@"Software\Microsoft\VisualStudio\15.0_{ instance2.GetInstanceId() }"));
                            }
                        }
                    }
                }
            }
            return null;
                // RegistryKey.FromHandle()
/*
            var installationVersion = instance.GetInstallationVersion();
            var version = helper.ParseVersion(installationVersion);

            Log.WriteLine($"InstallationVersion: {installationVersion} ({version})");

            if ((state & InstanceState.Local) == InstanceState.Local)
            {
                Log.WriteLine($"InstallationPath: {instance2.GetInstallationPath()}");
            }

            var catalog = instance as ISetupInstanceCatalog;
            if (catalog != null)
            {
                Log.WriteLine($"IsPrerelease: {catalog.IsPrerelease()}");
            }

            Log.WriteLine($"EnginePath: \"{instance2.GetEnginePath()}\"");
            Log.WriteLine($"Description: \"{instance2.GetDescription()}\"");
            Log.WriteLine($"DisplayName: \"{instance2.GetDisplayName()}\"");
            Log.WriteLine($"EnginePath: \"{instance2.GetEnginePath()}\"");
            Log.WriteLine($"InstallationName: \"{instance2.GetInstallationName()}\"");
            Log.WriteLine($"InstallationPath: \"{instance2.GetInstallationPath()}\"");
            Log.WriteLine($"InstallationVersion: \"{instance2.GetInstallationVersion()}\"");
            Log.WriteLine($"InstanceId: \"{instance2.GetInstanceId()}\"");
            // Print("Packages:", instance2.GetPackages());
            Print("Product:", instance2.GetProduct());
            Log.WriteLine($"ProductPath: \"{instance2.GetProductPath()}\"");
            Print("Properties:", instance2.GetProperties());
            Log.WriteLine($"ResolvePath(\"VC\"): \"{instance2.ResolvePath("VC")}\"");
*/
            Log.WriteLine();
        }

        private static void Print(RegistryKey key)
        {
            foreach(var subkeyName in key.GetSubKeyNames())
            {
                var subkey = key.OpenSubKey(subkeyName);
                var subpath = subkey.Name;
                Log.WriteLine(subpath);
                Log.PushIndent();
                Print(subkey);
                Log.PopIndent();
            }

            foreach (var valueName in key.GetValueNames())
            {
                Log.WriteLine($"- { (string.IsNullOrEmpty(valueName)?"(default value)":valueName) } ({Enum.GetName(typeof(RegistryValueKind), key.GetValueKind(valueName))}) = \"{ key.GetValue(valueName) }\"");
            }
        }

        private static void Print(string intro, ISetupPropertyStore properties)
        {
            Log.WriteLine(intro);
            Log.WriteLine("{");
            Log.PushIndent();
            foreach (var propertyName in properties.GetNames())
            {
                Log.WriteLine($"{propertyName}= \"{properties.GetValue(propertyName)}\"");
            }
            Log.PopIndent();
            Log.WriteLine("}");
        }

        private static void Print(string intro, ISetupPackageReference[] packages)
        {
            Log.WriteLine(intro);
            Log.WriteLine("{");
            Log.PushIndent();
            foreach (var package in packages)
            {
                Print("Package:", package);
            }
            Log.PopIndent();
            Log.WriteLine("}");
        }

        private static void Print(string intro, ISetupPackageReference package)
        {
            Log.WriteLine(intro);
            Log.WriteLine("{");
            Log.PushIndent();
            Log.WriteLine($"Branch: \"{package.GetBranch()}\"");
            Log.WriteLine($"Chip: \"{package.GetChip()}\"");
            Log.WriteLine($"Id: \"{package.GetId()}\"");
            Log.WriteLine($"IsExtension: \"{package.GetIsExtension()}\"");
            Log.WriteLine($"IsLanguage: \"{package.GetLanguage()}\"");
            Log.WriteLine($"Type: \"{package.GetType()}\"");
            Log.WriteLine($"UniqueId: \"{package.GetUniqueId()}\"");
            Log.WriteLine($"Version: \"{package.GetVersion()}\"");
            Log.PopIndent();
            Log.WriteLine("}");
        }


        private static string MSVCPath;
        private static string WindowsKitPath => @"C:\Program Files (x86)\Windows Kits"; // should be deduced HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10
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
                    case ECppVersion.Cpp11: parameters.Add("/std:c++11"); break; // unsupported flag
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
                case EWarningLevel.Few: parameters.Add("/W2"); break;
                case EWarningLevel.Medium: parameters.Add("/W3"); break;
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
