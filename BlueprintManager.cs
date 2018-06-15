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

        public BlueprintManager()
        {
        }

        internal void Import(string _path)
        {
            var fullpath = Path.GetFullPath(_path).ToLower();
            if (importedBlueprintFullPaths.Contains(fullpath))
            {
                // Log.WriteLine($@"Skip import of ""{ _path }"" (already imported).");
                return;
            }
            importedBlueprintFullPaths.Add(fullpath);

            // Log.WriteLine($"Importing { _path }...");
            string stored_cwd = Directory.GetCurrentDirectory();
            string cwd = Path.GetDirectoryName(Path.GetFullPath(_path));
            Directory.SetCurrentDirectory(cwd);
            _path = Path.GetFileName(_path);

            var blueprint = ReadBlueprint(_path);
            if (blueprint != null)
            {
                if (blueprint.Import != null)
                {
                    foreach (var import in blueprint.Import)
                    {
                        // Log.PushIndent();
                        Import(import);
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
                            blueprint.Project.FullPath = cwd;
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

            Directory.SetCurrentDirectory(stored_cwd);
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
            public string FullPath;
            public string Name;
            public List<string> Dependencies;
            public List<string> Sources;
            public List<string> Headers;
            public string Output;
            public EWarningLevel WarningLevel;
        }

        internal IEnumerable<Project> Touch(string path)
        {
            var fullpath = Path.GetFullPath(path);
            List<Project> coverage = new List<Project>();
            var projs = projects.Select(kv => kv.Value);
            // the projects which include this file
            var new_projects_to_build = projs.Where(proj =>
            {
                return
                    ((proj.Sources?.Count(filename => Path.Combine(proj.FullPath, filename) == fullpath)).GetValueOrDefault(0) != 0)
                    || ((proj.Headers?.Count(filename => Path.Combine(proj.FullPath, filename) == fullpath)).GetValueOrDefault(0) != 0);
            }).ToList();
            if (new_projects_to_build.Count != 0)
            {
                coverage.AddRange(new_projects_to_build);

                bool finish = false;
                while (!finish)
                {
                    // now we need the project which depends on
                    var depending_projects = projs.Except(coverage).Where(proj =>
                    {
                        if (proj.Dependencies == null)
                            return false;
                        return proj.Dependencies.Intersect(new_projects_to_build.Select(pj => pj.Name)).Count() != 0;
                    }).ToList();

                    if (depending_projects.Count != 0)
                    {
                        coverage.AddRange(depending_projects);
                        new_projects_to_build = depending_projects;
                    }
                    else
                    {
                        finish = true;
                    }
                }
            }
            // filtering project without output
            return coverage.Where(pj => pj.Output != null);
        }
    }
}