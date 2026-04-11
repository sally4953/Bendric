using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public enum ProjectType
    {
        Executable,
        StaticLibrary,
        DynamicLibrary
    }

    public enum RunVerb
    {
        Build = 0,
        Generate = 1,
        Clean = 2
    }

    public enum CxxVersion
    {
        Default = 0,
        StdCxx14,
        StdCxx17,
        StdCxx20,
        Latest
    }

    public class Project
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Version Version { get; set; }
        public ProjectType Type { get; set; }
        public CxxVersion CxxVersion { get; set; }
        public AdvList<string> Headers { get; set; }
        public AdvList<string> Sources { get; set; }
        public AdvList<string> SharedHeaders { get; set; }
        public AdvList<Dependency> Dependencies { get; set; }
        public AdvList<string> Defines { get; set; }
        public AdvList<string> IncludeDirectories { get; set; }
        public AdvList<string> LibraryDirectories { get; set; }
        public Guid Guid { get; set; }

        private string _workingDirectory;

        public Project()
        {
            Name = "";
            Description = "";
            Version = new Version(1, 0, 0);
            Type = ProjectType.Executable;
            CxxVersion = CxxVersion.Default;
            Headers = new AdvList<string>();
            Sources = new AdvList<string>();
            SharedHeaders = new AdvList<string>();
            Dependencies = new AdvList<Dependency>();
            Defines = new AdvList<string>();
            Guid = System.Guid.NewGuid();
            IncludeDirectories = new AdvList<string>();
            LibraryDirectories = new AdvList<string>();
            _workingDirectory = Directory.GetCurrentDirectory();
        }

        public void SetWorkingDirectory(string workingDirectory)
        {
            _workingDirectory = workingDirectory;
        }

        public void Build()
        {
            Logger.Info($"Building project: {Name}");

            // Resolve dependencies
            ResolveDependencies();

            // Generate project files
            Generate();

            // Build with MSBuild
            string solutionPath = Path.Combine(_workingDirectory, $"{Name}.sln");
            if (!MSBuildRunner.BuildSolution(solutionPath, BuildConfig.Configuration, BuildConfig.MaxCpuCount))
            {
                Logger.Error($"Build failed for {Name}");
                return;
            }

            Logger.Info($"Build succeeded: {Name}");

            // Install if requested
            if (BuildConfig.ShouldInstall)
            {
                Install();
            }
        }

        public void Generate()
        {
            Logger.Info($"Generating project files for: {Name}");

            // Resolve dependencies first
            ResolveDependencies();

            // Generate .vcxproj
            string vcxprojPath = Path.Combine(_workingDirectory, $"{Name}.vcxproj");
            VcxprojGenerator vcxprojGen = new VcxprojGenerator(this, vcxprojPath);
            vcxprojGen.Generate();

            // Generate .vcxproj.filters for solution explorer tree structure
            string filtersPath = Path.Combine(_workingDirectory, $"{Name}.vcxproj.filters");
            VcxprojFiltersGenerator filtersGen = new VcxprojFiltersGenerator(this, filtersPath, _workingDirectory);
            filtersGen.Generate();

            // Generate .sln
            string slnPath = Path.Combine(_workingDirectory, $"{Name}.sln");
            SolutionGenerator slnGen = new SolutionGenerator(new List<Project> { this }, slnPath, Name);
            slnGen.Generate();

            Logger.Info($"Project files generated for: {Name}");
        }

        public void Clean()
        {
            Logger.Info($"Cleaning project: {Name}");

            // Delete generated files
            string[] filesToDelete = new[]
            {
                Path.Combine(_workingDirectory, $"{Name}.vcxproj"),
                Path.Combine(_workingDirectory, $"{Name}.vcxproj.filters"),
                Path.Combine(_workingDirectory, $"{Name}.vcxproj.user"),
                Path.Combine(_workingDirectory, $"{Name}.sln"),
            };

            foreach (var file in filesToDelete)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Logger.Info($"Deleted: {file}");
                }
            }

            // Delete build directory
            string buildDir = Path.Combine(_workingDirectory, "build");
            if (Directory.Exists(buildDir))
            {
                Directory.Delete(buildDir, true);
                Logger.Info($"Deleted: {buildDir}");
            }

            // Delete Dependencies folder if it exists
            string depsDir = Path.Combine(_workingDirectory, "Dependencies");
            if (Directory.Exists(depsDir))
            {
                Directory.Delete(depsDir, true);
                Logger.Info($"Deleted: {depsDir}");
            }

            Logger.Info($"Clean completed for: {Name}");
        }

        public void Install()
        {
            if (string.IsNullOrEmpty(BuildConfig.InstallDirectory))
            {
                Logger.Warn("Install directory not specified");
                return;
            }

            Logger.Info($"Installing project: {Name}");

            string installDir = BuildConfig.InstallDirectory;
            string binDir = Path.Combine(installDir, "bin");
            string libDir = Path.Combine(installDir, "lib");
            string includeDir = Path.Combine(installDir, "include");

            // Create directories
            Directory.CreateDirectory(binDir);
            Directory.CreateDirectory(libDir);
            Directory.CreateDirectory(includeDir);

            // Copy binaries
            string buildOutputDir = Path.Combine(_workingDirectory, "build", BuildConfig.Configuration, BuildConfig.Architecture);
            if (Directory.Exists(buildOutputDir))
            {
                foreach (var file in Directory.GetFiles(buildOutputDir))
                {
                    string fileName = Path.GetFileName(file);
                    string ext = Path.GetExtension(file).ToLower();

                    if (ext == ".exe" || ext == ".dll")
                    {
                        string dest = Path.Combine(binDir, fileName);
                        File.Copy(file, dest, true);
                        Logger.Info($"Installed: {dest}");
                    }
                    else if (ext == ".lib")
                    {
                        string dest = Path.Combine(libDir, fileName);
                        File.Copy(file, dest, true);
                        Logger.Info($"Installed: {dest}");
                    }
                }
            }

            // Copy shared headers
            foreach (var header in SharedHeaders)
            {
                string sourcePath = Path.Combine(_workingDirectory, header);
                if (File.Exists(sourcePath))
                {
                    string dest = Path.Combine(includeDir, Path.GetFileName(header));
                    File.Copy(sourcePath, dest, true);
                    Logger.Info($"Installed: {dest}");
                }
                else
                {
                    Logger.Warn($"Shared header not found: {sourcePath}");
                }
            }

            // Copy dependency DLLs
            foreach (var dep in Dependencies)
            {
                foreach (var dll in dep.Dlls)
                {
                    if (File.Exists(dll))
                    {
                        string dest = Path.Combine(binDir, Path.GetFileName(dll));
                        File.Copy(dll, dest, true);
                        Logger.Info($"Installed dependency DLL: {dest}");
                    }
                }
            }

            Logger.Info($"Install completed for: {Name}");
        }

        private void ResolveDependencies()
        {
            Logger.Info($"Resolving {Dependencies.Count} dependencies...");

            foreach (var dep in Dependencies)
            {
                Logger.Info($"Resolving dependency: {dep.Name}");
                if (!dep.Resolve(_workingDirectory))
                {
                    Logger.Error($"Failed to resolve dependency: {dep.Name}");
                }
            }
        }

        /// <summary>
        /// Adds multiple dependencies at once
        /// </summary>
        public void AddDependencies(params Dependency[] dependencies)
        {
            foreach (var dep in dependencies)
            {
                Dependencies.Add(dep);
            }
        }
    }
}
