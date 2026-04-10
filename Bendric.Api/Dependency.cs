using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public abstract class Dependency
    {
        public string Name { get; set; }
        public BuildTool BuildTool { get; set; }
        public bool IsHeaderOnly { get; set; }
        public string IncludeDirectory { get; set; }
        public string LibraryDirectory { get; set; }
        public string[] Libraries { get; set; }
        public string[] Dlls { get; set; }

        protected Dependency(string name, BuildTool buildTool)
        {
            Name = name;
            BuildTool = buildTool;
            IsHeaderOnly = (buildTool == BuildTool.HeaderOnly);
            IncludeDirectory = "";
            LibraryDirectory = "";
            Libraries = new string[0];
            Dlls = new string[0];
        }

        public abstract bool Resolve(string workingDirectory);
    }

    public class GitDependency : Dependency
    {
        public string Branch { get; set; }
        public string Url { get; set; }

        public GitDependency(string name, string branch, string url, BuildTool buildTool = BuildTool.CMake)
            : base(name, buildTool)
        {
            Name = name;
            Branch = branch;
            Url = url;
        }

        public override bool Resolve(string workingDirectory)
        {
            string depsDir = Path.Combine(workingDirectory, "Dependencies");
            string targetDir = Path.Combine(depsDir, Name);

            if (!Directory.Exists(depsDir))
            {
                Directory.CreateDirectory(depsDir);
            }

            if (!Directory.Exists(targetDir))
            {
                Logger.Info($"Cloning {Name} from {Url}...");
                if (!GitHelper.Clone(Url, targetDir, Branch))
                {
                    Logger.Error($"Failed to clone {Name}");
                    return false;
                }
            }
            else
            {
                Logger.Info($"{Name} already exists, skipping clone...");
            }

            // Set include directory
            IncludeDirectory = Path.Combine(targetDir, "include");
            if (!Directory.Exists(IncludeDirectory))
            {
                IncludeDirectory = targetDir;
            }

            // Build if needed
            if (BuildTool == BuildTool.CMake)
            {
                return BuildWithCMake(targetDir);
            }
            else if (BuildTool == BuildTool.MSBuild)
            {
                return BuildWithMSBuild(targetDir);
            }

            return true;
        }

        private bool BuildWithCMake(string sourceDir)
        {
            var buildDir = Path.Combine(sourceDir, "build");
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            Logger.Info($"Running CMake for {Name}...");
            if (!MSBuildRunner.RunCMake(sourceDir, buildDir))
            {
                Logger.Error($"CMake failed for {Name}");
                return false;
            }

            Logger.Info($"Building {Name} with MSBuild...");
            if (!MSBuildRunner.BuildSolution(Path.Combine(buildDir, "*.sln"), "Release", 4))
            {
                Logger.Error($"MSBuild failed for {Name}");
                return false;
            }

            // Find library directory
            var possibleLibDirs = new[]
            {
                Path.Combine(buildDir, "Release"),
                Path.Combine(buildDir, "lib", "Release"),
                Path.Combine(buildDir, "lib"),
            };

            foreach (var dir in possibleLibDirs)
            {
                if (Directory.Exists(dir))
                {
                    LibraryDirectory = dir;
                    break;
                }
            }

            return true;
        }

        private bool BuildWithMSBuild(string sourceDir)
        {
            string[] possibleSlnFiles = Directory.GetFiles(sourceDir, "*.sln", SearchOption.TopDirectoryOnly);
            if (possibleSlnFiles.Length == 0)
            {
                possibleSlnFiles = Directory.GetFiles(sourceDir, "*.sln", SearchOption.AllDirectories);
            }

            if (possibleSlnFiles.Length == 0)
            {
                Logger.Error($"No solution file found for {Name}");
                return false;
            }

            string slnFile = possibleSlnFiles[0];
            Logger.Info($"Building {Name} with MSBuild: {slnFile}...");

            if (!MSBuildRunner.BuildSolution(slnFile, "Release", 4))
            {
                Logger.Error($"MSBuild failed for {Name}");
                return false;
            }

            // Find library directory
            string buildDir = Path.Combine(sourceDir, "build");
            if (Directory.Exists(buildDir))
            {
                string[] possibleLibDirs = new[]
                {
                    Path.Combine(buildDir, "Release"),
                    Path.Combine(buildDir, "lib", "Release"),
                    Path.Combine(buildDir, "lib"),
                };

                foreach (var dir in possibleLibDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        LibraryDirectory = dir;
                        break;
                    }
                }
            }

            return true;
        }
    }

    public class LocalDependency : Dependency
    {
        public string LibraryPath { get; set; }
        public string DllPath { get; set; }
        public bool NoIncludeDirectory { get; set; }

        public LocalDependency(string name, BuildTool buildTool, string libraryPath, string dllPath = "", bool noIncludeDirectory = false)
            : base(name, buildTool)
        {
            Name = name;
            LibraryPath = libraryPath;
            DllPath = dllPath;
            NoIncludeDirectory = noIncludeDirectory;

            if (!string.IsNullOrEmpty(libraryPath))
            {
                Libraries = new[] { libraryPath };
            }
            if (!string.IsNullOrEmpty(dllPath))
            {
                Dlls = new[] { dllPath };
            }
        }

        public override bool Resolve(string workingDirectory)
        {
            if (BuildTool == BuildTool.Copy)
            {
                // For system libraries like kernel32.lib, opengl32.lib
                // Just validate they exist or are valid library names
                if (!string.IsNullOrEmpty(LibraryPath))
                {
                    if (File.Exists(LibraryPath))
                    {
                        LibraryDirectory = Path.GetDirectoryName(LibraryPath);
                    }
                    else
                    {
                        // Assume it's a system library that linker can find
                        LibraryDirectory = "";
                    }
                }

                if (!string.IsNullOrEmpty(DllPath) && File.Exists(DllPath))
                {
                    // DLL will be copied during install phase
                }

                return true;
            }
            else if (BuildTool == BuildTool.HeaderOnly)
            {
                if (!NoIncludeDirectory && !string.IsNullOrEmpty(IncludeDirectory))
                {
                    if (!Directory.Exists(IncludeDirectory))
                    {
                        Logger.Warn($"Include directory not found: {IncludeDirectory}");
                    }
                }
                return true;
            }
            else if (BuildTool == BuildTool.MSBuild)
            {
                // Local MSBuild project
                if (!string.IsNullOrEmpty(LibraryPath) && File.Exists(LibraryPath))
                {
                    string projectDir = Path.GetDirectoryName(LibraryPath);
                    if (!MSBuildRunner.BuildSolution(LibraryPath, "Release", 4))
                    {
                        Logger.Error($"MSBuild failed for {Name}");
                        return false;
                    }

                    // Find output directory
                    string[] possibleOutputs = new[]
                    {
                        Path.Combine(projectDir, "Release"),
                        Path.Combine(projectDir, "bin", "Release"),
                        Path.Combine(projectDir, "lib", "Release"),
                    };

                    foreach (var dir in possibleOutputs)
                    {
                        if (Directory.Exists(dir))
                        {
                            LibraryDirectory = dir;
                            break;
                        }
                    }
                }
                return true;
            }

            return true;
        }
    }
}
