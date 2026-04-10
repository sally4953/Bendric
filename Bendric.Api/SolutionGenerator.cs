using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public class SolutionGenerator
    {
        private List<Project> _projects;
        private string _outputPath;
        private string _solutionName;

        public SolutionGenerator(List<Project> projects, string outputPath, string solutionName)
        {
            _projects = projects;
            _outputPath = outputPath;
            _solutionName = solutionName;
        }

        public void Generate()
        {
            StringBuilder sb = new StringBuilder();

            // Header
            sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
            sb.AppendLine("# Visual Studio Version 17");
            sb.AppendLine("VisualStudioVersion = 17.0.31903.59");
            sb.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

            // Build a dictionary to store project GUIDs (use the project's GUID)
            var projectGuids = new Dictionary<Project, string>();
            foreach (var project in _projects)
            {
                projectGuids[project] = project.Guid.ToString("B").ToUpper();
            }

            // Projects
            foreach (var project in _projects)
            {
                string projectGuid = projectGuids[project];
                string projectFileName = $"{project.Name}.vcxproj";

                sb.AppendLine($"Project(\"{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}\") = \"{project.Name}\", \"{projectFileName}\", \"{projectGuid}\"");
                sb.AppendLine("EndProject");
            }

            // Global section
            sb.AppendLine("Global");

            // Solution configurations
            sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
            sb.AppendLine("\t\tDebug|x64 = Debug|x64");
            sb.AppendLine("\t\tDebug|Win32 = Debug|Win32");
            sb.AppendLine("\t\tRelease|x64 = Release|x64");
            sb.AppendLine("\t\tRelease|Win32 = Release|Win32");
            sb.AppendLine("\tEndGlobalSection");

            // Project configurations - use the same GUIDs as above
            sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
            foreach (var project in _projects)
            {
                var projectGuid = projectGuids[project];
                sb.AppendLine($"\t\t{projectGuid}.Debug|x64.ActiveCfg = Debug|x64");
                sb.AppendLine($"\t\t{projectGuid}.Debug|x64.Build.0 = Debug|x64");
                sb.AppendLine($"\t\t{projectGuid}.Debug|Win32.ActiveCfg = Debug|Win32");
                sb.AppendLine($"\t\t{projectGuid}.Debug|Win32.Build.0 = Debug|Win32");
                sb.AppendLine($"\t\t{projectGuid}.Release|x64.ActiveCfg = Release|x64");
                sb.AppendLine($"\t\t{projectGuid}.Release|x64.Build.0 = Release|x64");
                sb.AppendLine($"\t\t{projectGuid}.Release|Win32.ActiveCfg = Release|Win32");
                sb.AppendLine($"\t\t{projectGuid}.Release|Win32.Build.0 = Release|Win32");
            }
            sb.AppendLine("\tEndGlobalSection");

            sb.AppendLine("EndGlobal");

            File.WriteAllText(_outputPath, sb.ToString(), Encoding.UTF8);
            Logger.Info($"Generated: {_outputPath}");
        }
    }
}
