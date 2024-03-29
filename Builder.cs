﻿using System;
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

            string override_outputFilename;
            // output
            if (commandLine.TryGet("output", out override_outputFilename))
            {
                override_outputFilename = Path.GetFullPath(override_outputFilename); // override is relative to workspace folder
                //Log.WriteLine($@"Override blueprint output filename by ""{ override_outputFilename }"".");
            }

            if (commandLine.Files.Count() == 0)
            {
                arg_ok = true;
                Log.WriteLine("ERROR: No source filename specified! You must specify at least one source filename.");
            }
            var missing_files = commandLine.Files.Where(s => !File.Exists(s)).ToArray();
            if (missing_files.Length!=0)
            {
                Log.WriteLine($"ERROR: Missing source files: { string.Join(", ", missing_files.Select(s => $@"""{s}""")) }.");
                arg_ok = false;
            }

            var blueprintProjectContainer = new Blueprint.ProjectContainer();

            IEnumerable<Blueprint.Project> project_to_compile = null;
            if (commandLine.TryGet("blueprint", out string blueprint_filename))
            {
                if (blueprintProjectContainer.Import(blueprint_filename))
                {
                    if (commandLine.Files.Count() == 1 && commandLine.Files.First().EndsWith(".blueprint.json"))
                    {
                        var project = blueprintProjectContainer.GetProjectByFullpath(Path.GetFullPath(commandLine.Files.First()));
                        if (project != null)
                            project_to_compile = new List<Blueprint.Project> { project };
                    }
                    else
                    {
                        project_to_compile = blueprintProjectContainer.Touch(commandLine.Files);
                    }
                    if (project_to_compile.Count() == 0)
                    {
                        if (override_outputFilename != null)
                        {
                            project_to_compile = new List<Blueprint.Project>
                            {
                                new Blueprint.Project
                                {
                                    Name = "(No Project)",
                                    Sources = commandLine.Files.ToList(),
                                    FullFolderPath = Directory.GetCurrentDirectory(),
                                    Dependencies = new List<string>()
                                }
                            };
                        }
                        else
                        {
                            if (commandLine.Files.Count() > 1)
                                Log.WriteLine($@"ERROR: No file among { string.Join(", ", commandLine.Files.Select(f => $@"""{f}""")) } belongs to a blueprint and no output defined!");
                            else
                                Log.WriteLine($@"ERROR: File ""{ commandLine.Files.First() }"" does not belong to a blueprint and no output defined!");
                            arg_ok = false;
                        }
                    }
                }
                else
                {
                    Log.WriteLine("ERROR: An error occured while reading blueprints!");
                    arg_ok = false;
                }
            }
            else
            {
                Log.WriteLine("ERROR: No blueprint specified! (not supported yet)");
                arg_ok = false;
            }

            arg_ok &= AreArgumentValids(args);
            if (!arg_ok)
            {
                Log.WriteLine("ERROR: Something went wrong!");
                return 1;
            }

            var compilo = Compilers.Factory.Create(args.Compiler.GetValueOrDefault(ECompiler.Clang6_0));

            compilo.CppVersion = args.StandardCpp.GetValueOrDefault(ECppVersion.Cpp17);
            compilo.WarningLevel = args.WarningLevel.GetValueOrDefault(EWarningLevel.High);
            compilo.DebugLevel = args.DebugLevel.GetValueOrDefault(EDebugLevel.NonDebug);
            compilo.WarningAsErrors = args.WarningsAreErrors;

            Dictionary<string, string> variables = new Dictionary<string, string>
            {
                { "compiler_name", compilo.ShortName },
                { "optimization", compilo.DebugLevel==EDebugLevel.Debug ? "d" : "r" },
                { "target", "win64" }
            };

            int exitCode = 0;
            string baseIncludePath = Directory.GetCurrentDirectory();

            foreach (var project in project_to_compile)
            {
                Log.WriteLine($@"Project ""{project.Name}"":");

                using (new ScopedWorkingDirectory(project.FullFolderPath))
                using (new ScopedLogIndent())
                {
                    string outputFilename = override_outputFilename!=null ? FileUtility.MakeRelative(project.FullFolderPath, override_outputFilename)  : project.ResolveOutput(variables);
                    var sourceFilenames = project.ResolvedSourceRelativePaths.ToList();

                    //if (!args.ForceCompilation)
                    //{
                    //    // TODO: should also check the blueprint file date and the dates of the files this project depends on
                    //    if (File.Exists(outputFilename))
                    //    {
                    //        var outputFileDate = File.GetLastWriteTime(outputFilename);
                    //        var mostRecentSourceFileDate = sourceFilenames.Select(sf => File.GetLastWriteTime(sf)).Min();
                    //        if (outputFileDate > mostRecentSourceFileDate)
                    //        {
                    //            Log.WriteLine("No changes detected. Compilation skipped.");
                    //            continue;
                    //        }
                    //        Log.WriteLine($@"output file ""{ outputFilename }"" is old. Compilation needed.");
                    //    }
                    //    else
                    //    {
                    //        Log.WriteLine($@"output file ""{ outputFilename }"" does not exit yet. Compilation needed.");
                    //    }
                    //}
                    //else
                    //{
                    //    Log.WriteLine("Forced compilation.");
                    //}
                    compilo.IntermediaryFileFolderName = FileUtility.MakeRelative(project.FullFolderPath, $@"{baseIncludePath}\obj");
                    compilo.OutputFilepath = outputFilename;
                    compilo.SourceFilePaths = sourceFilenames;
                    compilo.LibFilepaths = project.Libs;
                    compilo.Defines = project.Defines;
                    if (compilo is Compilers.MSVC)
                    {
                        compilo.ExtraFlags = project.MSVCExtra;
                    }
                    else if (compilo is Compilers.Clang)
                    {
                        compilo.ExtraFlags = project.ClangExtra;
                    }

                    List <string> additionalIncludePaths = project.Dependencies.Select(pname => blueprintProjectContainer.GetProject(pname)).Where(pj => pj != null).Select(pj => FileUtility.MakeRelative(project.FullFolderPath, pj.FullFolderPath)).ToList();
                    additionalIncludePaths.Add(".");
                    if (project.FullFolderPath != baseIncludePath)
                    {
                        string rel = FileUtility.MakeRelative(project.FullFolderPath, baseIncludePath);
                        additionalIncludePaths.Add( rel );
                        //Log.WriteLine($@"AdditionalIncludePath: ""{rel}""");
                    }
                    compilo.AdditionalIncludePaths = additionalIncludePaths;

                    exitCode = compilo.Run();

                    if (exitCode != 0)
                    {
                        Log.WriteLine($@"ERROR: Project ""{project.Name}"" compilation failed!");
                        break;
                    }
                }
            }
            Log.WriteLine("Build task completed.");
            return exitCode;
        }

        class ScopedLogIndent : IDisposable
        {
            public ScopedLogIndent()
            {
                Log.PushIndent();
            }

            public void Dispose()
            {
                Log.PopIndent();
            }
        }
    }
}
