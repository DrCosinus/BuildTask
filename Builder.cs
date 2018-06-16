using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// -clang -msvc
// -debug -ndebug
// -output <output_filepath>
// -force
// -warnings_are_errors

namespace BuildTask
{
    internal class Builder
    {
        bool IsFlag(string arg)
        {
            return arg[0] == '-' || arg[0] == '/';
        }

        bool AreArgumentValids(Arguments arguments)
        {
            bool error = false;
            if (!arguments.Compiler.HasValue) { error = true; Log.WriteLine("No compiler specified! Please use -clang or -msvc"); }
            if (string.IsNullOrEmpty(arguments.OutputFilename)) { error = true; Log.WriteLine("No output filename specified! Please use -output <filepath>"); }
            if (arguments.SourceFilenames.Count == 0) { error = true; Log.WriteLine("No source filename specified! You must specify at least one source filename."); }
            return !error;
        }

        private bool ParseArgumentsForCompiler(CommandLine _commandline, Arguments _args)
        {
            if (_commandline.IsPresent("clang"))
            {
                _args.Compiler.Value = ECompiler.Clang6_0;
            }
            if (_commandline.IsPresent("msvc"))
            {
                _args.Compiler.Value = ECompiler.MSVC19;
            }
            if (_commandline.TryGet("compiler", out string compiler_name))
            {
                switch (compiler_name)
                {
                    case "msvc": _args.Compiler.Value = ECompiler.MSVC19; break;
                    case "clang": _args.Compiler.Value = ECompiler.Clang6_0; break;
                    default: Log.WriteLine($@"Unknown compiler ""{compiler_name}""!"); return false;
                }
            }
            if (_args.Compiler.WasCrashed)
            {
                Log.WriteLine("Compiler defined multiple times!");
                return false;
            }
            return true;
        }

        private bool ParseArgumentsForOptimizationLevel(CommandLine _commandline, Arguments _args)
        {
            if (_commandline.IsPresent("debug"))
            {
                _args.DebugLevel.Value = EDebugLevel.Debug;
            }
            if (_commandline.IsPresent("ndebug"))
            {
                _args.DebugLevel.Value = EDebugLevel.NonDebug;
            }
            if (_commandline.TryGet("optimization", out string optimization_name))
            {
                switch (optimization_name)
                {
                    case "debug": _args.DebugLevel.Value = EDebugLevel.Debug; break;
                    case "ndebug": _args.DebugLevel.Value = EDebugLevel.NonDebug; break;
                    default: Log.WriteLine($@"Unknown optimization level ""{optimization_name}""!"); return false;
                }
            }
            if (_args.DebugLevel.WasCrashed)
            {
                Log.WriteLine("Optimization level defined multiple times!");
                return false;
            }
            return true;
        }

        private bool ParseArgumentsForWarningLevel(CommandLine _commandline, Arguments _args)
        {
            if (_commandline.TryGet("warning_level", out string warning_level_name))
            {
                if (Enum.TryParse(warning_level_name, true, out EWarningLevel wl))
                {
                    _args.WarningLevel = wl;
                    return true;
                }
                else
                {
                    Log.WriteLine($@"Unknown warning level ""{warning_level_name}""!");
                    return false;
                }
            }
            // no warning_level is not an error
            return true;
        }

        private void AssignDefaultValuesToUnsetOptionalArguments(Arguments _args)
        {
            if (!_args.Compiler.HasValue)
            {
                _args.Compiler.Value = ECompiler.Clang6_0;
            }
            if (!_args.WarningLevel.HasValue)
            {
                _args.WarningLevel = EWarningLevel.High;
            }
            if (!_args.StandardCpp.HasValue)
            {
                _args.StandardCpp = ECppVersion.Cpp17;
            }
        }

        internal int Run(string[] commandline_args)
        {
            var commandLine = new CommandLine();
            commandLine.RegisterFlag("clang", CommandLine.NeedValue.NoValue); // obsolete
            commandLine.RegisterFlag("msvc", CommandLine.NeedValue.NoValue); // obsolete
            commandLine.RegisterFlag("debug", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("ndebug", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("force", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("warnings_are_errors", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("output", CommandLine.NeedValue.OneValue);
            commandLine.RegisterFlag("compiler", CommandLine.NeedValue.OneValue);
            commandLine.RegisterFlag("warning_level", CommandLine.NeedValue.OneValue);
            commandLine.RegisterFlag("blueprint", CommandLine.NeedValue.OneValue);

            commandLine.Parse(commandline_args);

            Arguments args = new Arguments();
            bool arg_ok = true;
            arg_ok &= ParseArgumentsForCompiler(commandLine, args);
            arg_ok &= ParseArgumentsForOptimizationLevel(commandLine, args);
            arg_ok &= ParseArgumentsForWarningLevel(commandLine, args);

            // warning as error
            args.WarningsAreErrors = commandLine.IsPresent("warnings_are_errors");

            // force compilation
            args.ForceCompilation = commandLine.IsPresent("force");

            // output
            if (commandLine.TryGet("output", out string output_filename))
            {
                args.OutputFilename = output_filename;
            }
            else
            {
                Log.WriteLine("Output file not defined!");
                arg_ok = false;
            }

            var missing_files = commandLine.Files.Where(s => !File.Exists(s)).ToArray();
            if (missing_files.Length==0)
            {
                args.SourceFilenames = commandLine.Files.ToList();
            }
            else
            {
                Log.WriteLine($"Missing source files: { string.Join(", ", missing_files.Select(s => $@"""{s}""")) }.");
                arg_ok = false;
            }

            arg_ok &= AreArgumentValids(args);
            if (!arg_ok)
            {
                Log.WriteLine("Bad arguments!");
                return 1;
            }

            AssignDefaultValuesToUnsetOptionalArguments(args);

            if (commandLine.TryGet("blueprint", out string blueprint_filename))
            {
                var blueprintManager = new BlueprintManager();
                blueprintManager.Import(blueprint_filename);

                Log.WriteLine($@"Modification of ""{ string.Join(", ", commandLine.Files) }"" will induce the compilation of following projects:");
                var project_to_compile = blueprintManager.Touch(commandLine.Files);

                Dictionary<string, string> variables = new Dictionary<string, string>
                {
                    { "compiler_name", CompilerFactory.GetShortName(args.Compiler.Value) },
                    { "optimization", args.DebugLevel.Value==EDebugLevel.Debug ? "d" : "r" },
                    { "target", "win64" }
                };
                foreach (var pj in project_to_compile)
                {
                    Log.WriteLine($@"Project ""{ pj.Name }"" blueprint:");
                    Log.PushIndent();
                    Log.WriteLine($@"- OutputFile: ""{ pj.ResolveOutput(variables) }"", { (string.IsNullOrEmpty(args.OutputFilename) ? "" : $@"(overidden by ""{ args.OutputFilename }"")") }");
                    Log.WriteLine($@"- SourcesFile: { string.Join(", ", pj.Sources.Select(s => $@"""{s}""")) }");
                    Log.PopIndent();
                }
            }

            // header only => try to compile associated tests source file
            if (args.SourceFilenames.Count == 1 && args.SourceFilenames[0].EndsWith(".h"))
            {
                args.SourceFilenames[0] = args.SourceFilenames[0].Replace(".h", "_tests.cpp");
            }

            var most_recent_source_file_time = DateTime.MinValue;
            bool error = false;
            foreach (var filename in args.SourceFilenames)
            {
                var sourceFileInfo = new FileInfo(filename);
                if (sourceFileInfo.Exists)
                {
                    if (most_recent_source_file_time < sourceFileInfo.LastWriteTime)
                        most_recent_source_file_time = sourceFileInfo.LastWriteTime;
                }
                else
                {
                    Log.WriteLine($"Fail to find the source file \"{filename}\"!");
                    error = true;
                }
            }
            if (error)
            {
                Log.WriteLine("Bad source files!");
                return 1;
            }
            Log.WriteLine($"Most recent source file time: {most_recent_source_file_time}");

            if (!args.ForceCompilation)
            {
                var outputFileInfo = new FileInfo(args.OutputFilename);
                if (outputFileInfo.Exists)
                {
                    Log.WriteLine($"Output file time: {outputFileInfo.LastWriteTime}");
                    if (outputFileInfo.LastWriteTime > most_recent_source_file_time)
                    {
                        Log.WriteLine("No update needed.");
                        return 0;
                    }
                }
            }

            var compilo = CompilerFactory.Create(args.Compiler.Value);

            compilo.IntermediaryFileFolderName = "obj";
            compilo.CppVersion = args.StandardCpp;
            compilo.WarningLevel = args.WarningLevel;
            compilo.DebugLevel = args.DebugLevel.Value;
            compilo.OutputFilepath = args.OutputFilename;
            compilo.WarningAsErrors = args.WarningsAreErrors;
            compilo.SourceFilePaths = args.SourceFilenames;
            var exitCode = compilo.Run();

            return exitCode;
        }
    }

}
