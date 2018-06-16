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

        bool error = false;

        private void ErrorHandler(string _message)
        {
            Log.WriteLine(_message);
            error = true;
        }

        private (bool no_error, Arguments arguments) ParseArguments(string[] Args)
        {
            error = false;
            Arguments arguments = new Arguments();
            var arg_count = Args.Length;
            for (var arg_index = 0; arg_index < arg_count; ++arg_index)
            {
                if (IsFlag(Args[arg_index]))
                {
                    string option = Args[arg_index].Substring(1);
                    switch (option)
                    {
                        case "clang": arguments.Compiler.Value = ECompiler.Clang6_0; break;
                        case "msvc": arguments.Compiler.Value = ECompiler.MSVC19; break;
                        case "debug": arguments.DebugLevel.Value = EDebugLevel.Debug; break;
                        case "ndebug": arguments.DebugLevel.Value = EDebugLevel.NonDebug; break;
                        case "force": arguments.ForceCompilation = true; break;
                        case "warnings_are_errors": arguments.WarningsAreErrors = true; break;
                        case "output":
                            ++arg_index;
                            if (arg_index >= arg_count || IsFlag(Args[arg_index]))
                                ErrorHandler("Output filename not specified in output filename option!");
                            else
                                arguments.OutputFilename.Value = Args[arg_index];
                            break;
                        default: ErrorHandler($"Unknown option \"{Args[arg_index]}\"!"); break;
                    }
                }
                else
                {
                    if (File.Exists(Args[arg_index]))
                        arguments.SourceFilenames.Add(Args[arg_index]);
                    else
                    {
                        ErrorHandler($"Can not find the source file \"{Args[arg_index]}\"!");
                    }
                }
            }
            if (arguments.Compiler.WasCrashed) ErrorHandler("Compiler defined multiple times!");
            if (arguments.DebugLevel.WasCrashed) ErrorHandler("Debug level defined multiple times!");
            if (arguments.OutputFilename.WasCrashed) ErrorHandler("Output filepath defined multiple times!");
            return (!error, arguments);
        }

        bool AreArgumentValids(Arguments arguments)
        {
            bool error = false;
            if (!arguments.Compiler.HasValue) { error = true; Log.WriteLine("No compiler specified! Please use -clang or -msvc"); }
            if (!arguments.OutputFilename.HasValue) { error = true; Log.WriteLine("No output filename specified! Please use -output <filepath>"); }
            if (arguments.SourceFilenames.Count == 0) { error = true; Log.WriteLine("No source filename specified! You must specify at least one source filename."); }
            return !error;
        }

        // remove "folder/.." pattern
        // remove "." pattern
        internal string[] SplitAndDustPath(string _path)
        {
            var splits = _path.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            splits = splits.Where(s => s != ".").ToArray();
            while (true)
            {
                var ix = splits.Select((s, i) => new { str = s, idx = i }).Where(o => o.idx != 0 && o.str == "..").Select(o => o.idx).FirstOrDefault();
                if (ix == 0)
                    break;
                splits = splits.Where((s, i) => i != ix - 1 && i != ix).ToArray();
            }
            return splits;
        }

        internal string MakeRelative(string _referencePath, string _path)
        {
            var ref_splits = SplitAndDustPath(_referencePath);

            var path_splits = SplitAndDustPath(_path);

            int count = 0;
            int n = Math.Min(ref_splits.Count(), path_splits.Count());
            for (int i = 0; i< n; ++i)
            {
                if (ref_splits[i] != path_splits[i])
                    break;
                ++count;
            }
            return ref_splits.Skip(count).Select(x => "..").Concat(path_splits.Skip(count)).Aggregate((s1, s2) => s1 + Path.DirectorySeparatorChar + s2);
        }

        internal int Run(string[] commandline_args)
        {
            //string p1 = Path.Combine(Directory.GetCurrentDirectory(), "champ/../../mops");
            //string p2 = Path.GetFullPath(p1);
            //string p3 = new Uri(Environment.CurrentDirectory+"\\").MakeRelativeUri(new Uri(p2)).ToString();
            //string p4 = MakeRelative(Environment.CurrentDirectory, p1);
            //string zz1 = MakeRelative("A/B/C/D/E", "A/B/X/Y");
            //string zz2 = MakeRelative("A/B/C", "A/B/C/Z");
            //string zz3 = MakeRelative("A/B/C/Z", "A/B/C");
            //string zzx = MakeRelative("A/B/C/Z", "A/B/C/");
            //string zzw = MakeRelative("A/B/C/Z/", "A/B/C/");
            //string zzz = MakeRelative("A/B/C/Z/", "A/B/C");
            //string zza = MakeRelative("A/B/C", ".");
            //string zzb = MakeRelative(".", "A/B/C" );
            //string zzy = MakeRelative("A/B/C/Z", "A/B/C.exe");
            //string zz4 = MakeRelative("", "A/B/C");
            //string zz5 = MakeRelative("A/B/C", "");

            var blueprintManager = new BlueprintManager();
            blueprintManager.Import("playground.blueprint.json");
            blueprintManager.DumpProjectNames();

            string touched_filename = "tdd/simple_tests.hpp";
            Log.WriteLine($@"Modification of ""{ touched_filename }"" will induce the compilation of following projects:");
            var project_to_compile = blueprintManager.Touch(touched_filename);
            foreach (var pj in blueprintManager.Touch(touched_filename))
            {
                Log.WriteLine($@"- ""{ pj.Name }""");
            }

            var commandLine = new CommandLine();
            commandLine.RegisterFlag("clang", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("msvc", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("debug", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("ndebug", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("force", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("warnings_are_errors", CommandLine.NeedValue.NoValue);
            commandLine.RegisterFlag("output", CommandLine.NeedValue.OneValue);
            commandLine.RegisterFlag("compiler", CommandLine.NeedValue.OneValue);
            commandLine.RegisterFlag("warning_level", CommandLine.NeedValue.OneValue);
            commandLine.RegisterFlag("blueprint", CommandLine.NeedValue.OneValue);

            commandLine.Parse(new string[] { "source.cpp", "-fofo", "source.h", "-compiler:msvc", "-force:no" });

            var (arg_ok, args) = ParseArguments(commandline_args);
            arg_ok &= AreArgumentValids(args);
            if (!arg_ok)
            {
                Log.WriteLine("Bad arguments!");
                return 1;
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
                var outputFileInfo = new FileInfo(args.OutputFilename.Value);
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

            var compilo = CompilerFactory.Create(args.Compiler.GetValueOrDefault(ECompiler.Clang6_0));

            compilo.IntermediaryFileFolderName = "obj";
            compilo.CppVersion = ECppVersion.Cpp17;
            compilo.WarningLevel = EWarningLevel.High;
            compilo.DebugLevel = args.DebugLevel.Value;
            compilo.OutputFilepath = args.OutputFilename.Value;
            compilo.WarningAsErrors = args.WarningsAreErrors;
            compilo.SourceFilePaths = args.SourceFilenames;
            var exitCode = compilo.Run();

            return exitCode;
        }
    }

}
