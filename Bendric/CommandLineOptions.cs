using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric
{
    public class CommandLineOptions
    {
        public string ScriptPath { get; set; }
        public string Command { get; set; }
        public string Configuration { get; set; }
        public string Architecture { get; set; }
        public int MaxCpuCount { get; set; }
        public string InstallDirectory { get; set; }
        public bool NoLogo { get; set; }
        public List<string> Defines { get; set; }
        public bool ShowHelp { get; set; }

        public CommandLineOptions()
        {
            ScriptPath = "";
            Command = "";
            Configuration = "Debug";
            Architecture = "x64";
            MaxCpuCount = 1;
            InstallDirectory = "";
            NoLogo = false;
            Defines = new List<string>();
            ShowHelp = false;
        }

        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();

            if ((args.Length == 0) || 
                (args[0].ToLower() == "-help") ||
                (args[0].ToLower() == "-h") ||
                (args[0].ToLower() == "--help"))
            {
                options.ShowHelp = true;
                return options;
            }

            int i = 0;

            // First argument is the command
            options.Command = args[i++].ToLower();

            // Special case for clean command - may not have a script path
            if (options.Command == "clean")
            {
                if (i < args.Length && !args[i].StartsWith("-"))
                {
                    options.ScriptPath = args[i++];
                }
                else
                {
                    options.ScriptPath = FindBuildScript(Directory.GetCurrentDirectory());
                }
                return options;
            }

            // For other commands, get the script path
            if (i < args.Length && !args[i].StartsWith("-"))
            {
                options.ScriptPath = args[i++];
            }
            else
            {
                options.ScriptPath = FindBuildScript(Directory.GetCurrentDirectory());
            }

            // Parse options
            while (i < args.Length)
            {
                var arg = args[i];

                switch (arg.ToLower())
                {
                    case "-config":
                    case "-c":
                        if (i + 1 < args.Length)
                        {
                            options.Configuration = args[++i];
                        }
                        break;

                    case "-arch":
                    case "-a":
                        if (i + 1 < args.Length)
                        {
                            options.Architecture = args[++i];
                        }
                        break;

                    case "-m":
                    case "-maxcpucount":
                        if (i + 1 < args.Length)
                        {
                            if (int.TryParse(args[++i], out int cpuCount))
                            {
                                options.MaxCpuCount = cpuCount;
                            }
                        }
                        break;

                    case "-install":
                    case "-i":
                        if (i + 1 < args.Length)
                        {
                            options.InstallDirectory = args[++i];
                        }
                        break;

                    case "-nologo":
                        options.NoLogo = true;
                        break;

                    case "-define":
                    case "-d":
                        if (i + 1 < args.Length)
                        {
                            var defines = args[++i];
                            options.Defines.AddRange(defines.Split(';', ','));
                        }
                        break;

                    default:
                        Console.WriteLine($"Warning: Unknown option '{arg}'");
                        break;
                }

                i++;
            }

            return options;
        }

        private static string FindBuildScript(string directory)
        {
            var possibleNames = new[]
            {
                "Build.bendric",
                "build.bendric",
                "Bendric.build",
                "bendric.build",
                "Bendric",
                "bendric",
            };

            foreach (var name in possibleNames)
            {
                var path = Path.Combine(directory, name);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return "";
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Bendric - C/C++ Build System");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  bendric build <script> [options]    Build the project");
            Console.WriteLine("  bendric generate <script> [options] Generate project files only");
            Console.WriteLine("  bendric clean [script]              Clean build artifacts");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -config <config>       Build configuration (Debug/Release), default: Debug");
            Console.WriteLine("  -arch <arch>           Target architecture (x64/Win32), default: x64");
            Console.WriteLine("  -m <count>             Maximum CPU count for parallel build, default: 1");
            Console.WriteLine("  -install <dir>         Install directory for built artifacts");
            Console.WriteLine("  -define <defs>         Semicolon-separated preprocessor definitions");
            Console.WriteLine("  -nologo                Suppress logo output");
            Console.WriteLine("  -help                  Show this help message");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  bendric build .\\Build.bendric -config Release -arch x64 -m 4 -install .\\bin");
            Console.WriteLine("  bendric generate .\\Build.bendric -config Debug");
            Console.WriteLine("  bendric clean");
        }
    }
}
