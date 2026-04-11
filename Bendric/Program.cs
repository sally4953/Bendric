using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Bendric.Api;

namespace Bendric
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Parse command line options
            var options = CommandLineOptions.Parse(args);

            if (options.ShowHelp)
            {
                CommandLineOptions.PrintHelp();
                return;
            }

            // Set global config
            BuildConfig.SetConfig(
                options.Configuration,
                options.Architecture,
                options.MaxCpuCount,
                options.InstallDirectory,
                options.Defines.ToArray());

            // Set logger options
            Logger.SetNoLogo(options.NoLogo);

            // Print logo
            if (!options.NoLogo)
            {
                PrintLogo();
            }

            // Handle clean command without script
            if (options.Command == "clean" && string.IsNullOrEmpty(options.ScriptPath))
            {
                Logger.Info("Cleaning current directory...");
                CleanCurrentDirectory();
                return;
            }

            // Validate script path
            if (string.IsNullOrEmpty(options.ScriptPath))
            {
                Logger.Error("No build script specified and no default script found");
                return;
            }

            if (!File.Exists(options.ScriptPath))
            {
                Logger.Error($"Build script not found: {options.ScriptPath}");
                return;
            }

            var workingDirectory = Path.GetDirectoryName(Path.GetFullPath(options.ScriptPath));

            // Compile the script
            var compiler = new ScriptCompiler(options.ScriptPath);
            var assembly = compiler.Compile();

            if (assembly == null)
            {
                Logger.Error("Failed to compile build script");
                Environment.Exit(1);
                return;
            }

            // Run the script with appropriate verb
            var runner = new ScriptRunner(assembly, workingDirectory);
            var verb = GetRunVerb(options.Command);

            bool success = runner.Run(verb);

            if (!success)
            {
                Environment.Exit(1);
            }
        }

        static void PrintLogo()
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(@"
----------------------------------------------
    ____  _____ _   _ ____  ____  ___ ____
   | __ )| ____| \ | |  _ \|  _ \|_ _/ ___|
   |  _ \|  _| |  \| | | | | |_) || | |   
   | |_) | |___| |\  | |_| |  _ < | | |___
   |____/|_____|_| \_|____/|_| \_\___\____|

----------------------------------------------");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" C/C++ Build System - Version 0.2.0");
            Console.WriteLine();
            Console.ForegroundColor = originalColor;
        }

        static RunVerb GetRunVerb(string command)
        {
            switch (command.ToLower())
            {
                case "build":
                case "b":
                    return RunVerb.Build;
                case "generate":
                case "gen":
                case "g":
                    return RunVerb.Generate;
                case "clean":
                case "c":
                    return RunVerb.Clean;
                default:
                    Logger.Warn($"Unknown command '{command}', defaulting to Build");
                    return RunVerb.Build;
            }
        }

        static void CleanCurrentDirectory()
        {
            string currentDir = Directory.GetCurrentDirectory();

            // Delete common build artifacts
            string[] patterns = new[]
            {
                "*.vcxproj",
                "*.vcxproj.filters",
                "*.vcxproj.user",
                "*.sln",
            };

            foreach (var pattern in patterns)
            {
                foreach (var file in Directory.GetFiles(currentDir, pattern))
                {
                    try
                    {
                        File.Delete(file);
                        Logger.Info($"Deleted: {file}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Failed to delete {file}: {ex.Message}");
                    }
                }
            }

            // Delete build directory
            string buildDir = Path.Combine(currentDir, "build");
            if (Directory.Exists(buildDir))
            {
                try
                {
                    Directory.Delete(buildDir, true);
                    Logger.Info($"Deleted: {buildDir}");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to delete {buildDir}: {ex.Message}");
                }
            }

            // Delete Dependencies folder
            string depsDir = Path.Combine(currentDir, "Dependencies");
            if (Directory.Exists(depsDir))
            {
                try
                {
                    Directory.Delete(depsDir, true);
                    Logger.Info($"Deleted: {depsDir}");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Failed to delete {depsDir}: {ex.Message}");
                }
            }

            Logger.Info("Clean completed");
        }
    }
}
