using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public static class MSBuildRunner
    {
        public static bool RunCMake(string sourceDir, string buildDir)
        {
            string cmake = "cmake";
            string arch = BuildConfig.Architecture == "x64" ? "x64" : "Win32";

            var psi = new ProcessStartInfo
            {
                FileName = cmake,
                Arguments = $"-S \"{sourceDir}\" -B \"{buildDir}\" -A {arch}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = sourceDir
            };

            using (var process = Process.Start(psi))
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.OutputDataReceived += (sender, e) =>
                {
                    Logger.Info($"CMake: {e.Data}");
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    Logger.Info($"CMake: {e.Data}");
                };
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Logger.Error($"CMake failed.");
                    return false;
                }
            }

            return true;
        }

        public static bool BuildSolution(string solutionPath, string configuration, int maxCpuCount)
        {
            var msbuild = "msbuild";
            var cpuCountArg = maxCpuCount > 1 ? $"-maxcpucount:{maxCpuCount}" : "";

            // Handle wildcard in solution path
            if (solutionPath.Contains("*"))
            {
                var dir = Path.GetDirectoryName(solutionPath);
                var pattern = Path.GetFileName(solutionPath);
                var files = Directory.GetFiles(dir, pattern);
                if (files.Length > 0)
                {
                    solutionPath = files[0];
                }
            }

            var arguments = $"\"{solutionPath}\" /p:Configuration={configuration} /p:Platform={BuildConfig.Architecture} {cpuCountArg} /verbosity:minimal";

            Logger.Info($"Running MSBuild: {msbuild} {arguments}");

            var psi = new ProcessStartInfo
            {
                FileName = msbuild,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
            };

            using (var process = Process.Start(psi))
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.OutputDataReceived += (sender, e) =>
                {
                    Logger.Info($"MSBuild: {e.Data}");
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    Logger.Info($"MSBuild: {e.Data}");
                };
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Logger.Error($"MSBuild failed.");
                    return false;
                }
            }

            return true;
        }

        public static bool BuildProject(string projectPath, string configuration, int maxCpuCount)
        {
            return BuildSolution(projectPath, configuration, maxCpuCount);
        }
    }
}
