using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public enum BuildTool
    {
        MSBuild,
        CMake,
        Copy,
        HeaderOnly
    }

    public static class BuildConfig
    {
        public static string Configuration { get; set; } = "Debug";
        public static string Architecture { get; set; } = "x64";
        public static int MaxCpuCount { get; set; } = 1;
        public static string InstallDirectory { get; set; } = "";
        public static bool ShouldInstall { get; set; } = false;
        public static string[] Defines { get; set; } = new string[0];

        public static void SetConfig(string config, string arch, int cpuCount, string installDir, string[] defines)
        {
            Configuration = config ?? "Debug";
            Architecture = arch ?? "x64";
            MaxCpuCount = cpuCount > 0 ? cpuCount : 1;
            InstallDirectory = installDir ?? "";
            ShouldInstall = !string.IsNullOrEmpty(InstallDirectory);
            Defines = defines ?? new string[0];
        }
    }
}
