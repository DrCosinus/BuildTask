﻿using System.IO;
using System.Web.Script.Serialization;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace BuildTask
{
    class BlueprintManager
    {
        static JavaScriptSerializer serializer = new JavaScriptSerializer();

        List<string> importedBlueprintFullPaths = new List<string>();
        Dictionary<string, Project> projects = new Dictionary<string, Project>();
        readonly string baseDirectory;

        public BlueprintManager()
        {
            baseDirectory = Directory.GetCurrentDirectory();
        }

        public Project GetProject(string _projectName)
        {
            if (projects.TryGetValue(_projectName, out var project))
            {
                return project;
            }
            return null;
        }

        public Project GetProjectByFullpath(string _fullpath)
        {
            return projects.Select(kv => kv.Value).FirstOrDefault(pj => string.Compare(pj.FullFilePath, _fullpath, true) == 0);
        }

        internal bool Import(string _path)
        {
            var fullpath = Path.GetFullPath(_path);
            if (!File.Exists(fullpath))
            {
                fullpath = Path.GetFullPath(Path.Combine(baseDirectory, _path));
                if (!File.Exists(fullpath))
                {
                    Log.WriteLine($@"ERROR: Can not find blueprint ""{_path}""");
                    return false;
                }
            }
            // Fix slashes and turn to lower case
            var lowercase_fullpath = fullpath.ToLower();

            if (importedBlueprintFullPaths.Contains(lowercase_fullpath))
            {
                // Log.WriteLine($@"Skip import of ""{ _path }"" (already imported).");
                return true;
            }
            importedBlueprintFullPaths.Add(lowercase_fullpath);

            // Log.WriteLine($"Importing { fullpath }...");
            string cwd = Path.GetDirectoryName(fullpath);
            var filename = Path.GetFileName(_path);
            using (new ScopedWorkingDirectory(cwd))
            {
                var blueprint = ReadBlueprint(filename);
                if (blueprint != null)
                {
                    if (blueprint.Import != null)
                    {
                        foreach (var import in blueprint.Import)
                        {
                            // Log.PushIndent();
                            if (!Import(import))
                            {
                                Log.WriteLine($@"ERROR: Something got wrong during the import of the blueprint ""{filename}""");
                                return false;
                            }
                            // Log.PopIndent();
                        }
                    }
                    var project = blueprint;
                    if (blueprint.Project != null)
                    {
                        if (blueprint.Project.Name != null)
                        {
                            // Log.WriteLine($@"Found project ""{ blueprint.Project.Name }"" key.");
                            if (projects.Count(pair => pair.Key == blueprint.Project.Name) == 0)
                            {
                                blueprint.Project.FullFolderPath = cwd;
                                blueprint.Project.FullFilePath = fullpath;
                                projects.Add(blueprint.Project.Name, blueprint.Project);
                            }
                            else
                            {
                                Log.WriteLine("@@ A project with the same name already exist!");
                            }
                        }
                        else
                        {
                            Log.WriteLine($@"## Can not find project name.");
                        }
                    }
                }
                else
                {
                    Log.WriteLine($@"## Fail to read blueprint ""{ fullpath }""!");
                }
            }
            return true;
        }

        internal void DumpProjectNames()
        {
            Log.WriteLine($"{ projects.Count() } project found.");
            foreach (var pair in projects)
            {
                Log.WriteLine($@"- ""{ pair.Key }""");
            }
        }

        private BluePrint ReadBlueprint(string _path)
        {
            BluePrint blueprint = null;
            using (var stream = new StreamReader(_path))
            {
                var json = stream.ReadToEnd();
                blueprint = serializer.Deserialize<BluePrint>(json);
            }
            return blueprint;
        }

        class BluePrint
        {
            public List<string> Import = null;
            public Project Project = null;
        }

        internal class Project
        {
            public string FullFolderPath;
            public string FullFilePath;
            public string Name;
            public List<string> Dependencies = null;
            public List<string> Sources = null;
            public List<string> Headers = null;
            public List<string> Libs = null;
            public string Output = null;
            public EWarningLevel? WarningLevel = null;

            public override string ToString()
            {
                return $@"Project""{ Name }""";
            }

            internal string ResolveOutput(Dictionary<string, string> _variables)
            {
                string result = Output.ReplaceVariable("project_name", Name);
                foreach (var kv in _variables)
                {
                    result = result.ReplaceVariable(kv.Key, kv.Value);
                }

                return result;
            }

            internal IEnumerable<string> ResolvedSourceFullPaths => Sources?.Select(filename => Directory.GetFiles(FullFolderPath, filename) as IEnumerable<string>)
            .Aggregate((s1, s2) => { var sum = new List<string>(s1); sum.AddRange(s2); return sum as IEnumerable<string>; });
            internal IEnumerable<string> ResolvedSourceRelativePaths => ResolvedSourceFullPaths.Select(s => FileUtility.MakeRelative(FullFolderPath, s));
            internal IEnumerable<string> ResolvedHeaderFullPaths => Headers?.Select(filename => Directory.GetFiles(FullFolderPath, filename) as IEnumerable<string>)
            .Aggregate((s1, s2) => { var sum = new List<string>(s1); sum.AddRange(s2); return sum as IEnumerable<string>; });
        }

        internal IEnumerable<Project> Touch(IEnumerable<string> paths)
        {
            List<Project> coverage = new List<Project>();
            var projs = projects.Select(kv => kv.Value);

            foreach (var path in paths)
            {
                var fullpath = Path.GetFullPath(path);
                // the projects which include this file
                var new_projects_to_build = projs.Where(proj =>
                {
                    return
                        ((proj.ResolvedSourceFullPaths?.Count(filename => filename == fullpath)).GetValueOrDefault(0) != 0)
                        || ((proj.ResolvedHeaderFullPaths?.Count(filename => filename == fullpath)).GetValueOrDefault(0) != 0);
                }).ToList();
                if (new_projects_to_build.Count != 0)
                {
                    coverage.AddRange(new_projects_to_build);
                    projs = projs.Except(new_projects_to_build).ToArray();

                    bool finish = false;
                    while (!finish)
                    {
                        // now we need the project which depends on
                        var depending_projects = projs.Where(proj =>
                        {
                            if (proj.Dependencies == null)
                                return false;
                            return proj.Dependencies.Intersect(new_projects_to_build.Select(pj => pj.Name)).Count() != 0;
                        }).ToList();

                        if (depending_projects.Count != 0)
                        {
                            coverage.AddRange(depending_projects);
                            projs = projs.Except(depending_projects).ToArray();
                            new_projects_to_build = depending_projects;
                        }
                        else
                        {
                            finish = true;
                        }
                    }
                }
            }
            // filtering project without output
            return coverage.Where(pj => pj.Output != null);
        }
    }
}