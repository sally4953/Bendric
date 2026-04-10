using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public static class GitHelper
    {
        public static bool Clone(string url, string targetDirectory, string branch = "main")
        {
            if (!IsGitAvailable())
            {
                Logger.Error("Git is not available in PATH");
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"clone --branch {branch} --single-branch {url} \"{targetDirectory}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                void a(object sender, DataReceivedEventArgs e)
                {
                    Logger.Info($"Git: {e.Data}");
                }
                process.ErrorDataReceived += a;
                process.OutputDataReceived += a;
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Logger.Error($"Git clone failed.");
                    return false;
                }
            }

            return true;
        }

        public static bool IsGitAvailable()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetGitPath()
        {
            var paths = Environment.GetEnvironmentVariable("PATH").Split(';');
            foreach (string path in paths)
            {
                string gitExe = Path.Combine(path, "git.exe");
                if (File.Exists(gitExe))
                {
                    return gitExe;
                }
            }
            return "git";
        }
    }
}
