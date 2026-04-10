using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bendric.Api
{
    public class VcxprojGenerator
    {
        private Project _project;
        private string _outputPath;
        private static readonly XNamespace NS = "http://schemas.microsoft.com/developer/msbuild/2003";

        public VcxprojGenerator(Project project, string outputPath)
        {
            _project = project;
            _outputPath = outputPath;
        }

        public void Generate()
        {
            var doc = new XDocument();
            var projectElement = new XElement(NS + "Project",
                new XAttribute("DefaultTargets", "Build"));

            // Project configurations
            projectElement.Add(CreateItemGroup("ProjectConfigurations"));

            // Globals
            projectElement.Add(CreatePropertyGroup("Globals"));

            // Import default props
            projectElement.Add(new XElement(NS + "Import",
                new XAttribute("Project", "$(VCTargetsPath)\\Microsoft.Cpp.Default.props")));

            // Configuration properties
            CreateConfigurationProperties(projectElement);

            // Import Cpp props
            projectElement.Add(new XElement(NS + "Import",
                new XAttribute("Project", "$(VCTargetsPath)\\Microsoft.Cpp.props")));

            // Extension settings
            projectElement.Add(new XElement(NS + "ImportGroup",
                new XAttribute("Label", "ExtensionSettings")));

            // Shared
            projectElement.Add(new XElement(NS + "ImportGroup",
                new XAttribute("Label", "Shared")));

            // Property sheets
            projectElement.Add(CreatePropertySheets());

            // User macros
            projectElement.Add(new XElement(NS + "PropertyGroup",
                new XAttribute("Label", "UserMacros")));

            // Additional properties
            CreateAdditionalProperties(projectElement);

            // Source files
            projectElement.Add(CreateSourceFiles());

            // Import targets
            projectElement.Add(new XElement(NS + "Import",
                new XAttribute("Project", "$(VCTargetsPath)\\Microsoft.Cpp.targets")));

            doc.Add(projectElement);
            doc.Save(_outputPath);

            Logger.Info($"Generated: {_outputPath}");
        }

        private XElement CreateItemGroup(string label)
        {
            var itemGroup = new XElement(NS + "ItemGroup",
                new XAttribute("Label", label));

            var configs = new[] { "Debug", "Release" };
            var platforms = new[] { "x64", "Win32" };

            foreach (var config in configs)
            {
                foreach (var platform in platforms)
                {
                    itemGroup.Add(new XElement(NS + "ProjectConfiguration",
                        new XAttribute("Include", $"{config}|{platform}"),
                        new XElement(NS + "Configuration", config),
                        new XElement(NS + "Platform", platform)));
                }
            }

            return itemGroup;
        }

        private XElement CreatePropertyGroup(string label)
        {
            var propGroup = new XElement(NS + "PropertyGroup",
                new XAttribute("Label", label));

            if (label == "Globals")
            {
                propGroup.Add(new XElement(NS + "VCProjectVersion", "16.0"));
                propGroup.Add(new XElement(NS + "ProjectGuid", _project.Guid.ToString("B")));
                propGroup.Add(new XElement(NS + "Keyword", "Win32Proj"));
                propGroup.Add(new XElement(NS + "RootNamespace", _project.Name));
                propGroup.Add(new XElement(NS + "WindowsTargetPlatformVersion", "10.0"));
            }

            return propGroup;
        }

        private void CreateConfigurationProperties(XElement parent)
        {
            var configs = new[] { "Debug", "Release" };
            var platforms = new[] { "x64", "Win32" };

            foreach (var config in configs)
            {
                foreach (var platform in platforms)
                {
                    var condition = $"'$(Configuration)|$(Platform)'=='{config}|{platform}'";
                    var configGroup = new XElement(NS + "PropertyGroup",
                        new XAttribute("Condition", condition),
                        new XAttribute("Label", "Configuration"));

                    configGroup.Add(new XElement(NS + "ConfigurationType", GetProjectType()));
                    configGroup.Add(new XElement(NS + "UseDebugLibraries", config == "Debug" ? "true" : "false"));
                    configGroup.Add(new XElement(NS + "PlatformToolset", "v143"));
                    configGroup.Add(new XElement(NS + "CharacterSet", "Unicode"));

                    parent.Add(configGroup);
                }
            }
        }

        private XElement CreatePropertySheets()
        {
            var importGroup = new XElement(NS + "ImportGroup",
                new XAttribute("Label", "PropertySheets"));

            var configs = new[] { "Debug", "Release" };
            var platforms = new[] { "x64", "Win32" };

            foreach (var config in configs)
            {
                foreach (var platform in platforms)
                {
                    importGroup.Add(new XElement(NS + "Import",
                        new XAttribute("Project", "$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props"),
                        new XAttribute("Condition", "exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')"),
                        new XAttribute("Label", "LocalAppDataPlatform")));
                }
            }

            return importGroup;
        }

        private void CreateAdditionalProperties(XElement parent)
        {
            var configs = new[] { "Debug", "Release" };
            var platforms = new[] { "x64", "Win32" };

            foreach (var config in configs)
            {
                foreach (var platform in platforms)
                {
                    var condition = $"'$(Configuration)|$(Platform)'=='{config}|{platform}'";

                    // Output directories PropertyGroup
                    var propGroup = new XElement(NS + "PropertyGroup",
                        new XAttribute("Condition", condition));

                    propGroup.Add(new XElement(NS + "OutDir", $"$(SolutionDir)build\\$(Configuration)\\$(Platform)\\"));
                    propGroup.Add(new XElement(NS + "IntDir", $"$(SolutionDir)build\\intermediate\\$(Configuration)\\$(Platform)\\$(ProjectName)\\"));
                    propGroup.Add(new XElement(NS + "TargetName", _project.Name));

                    parent.Add(propGroup);

                    // ClCompile and Link settings ItemDefinitionGroup
                    var itemDefGroup = new XElement(NS + "ItemDefinitionGroup",
                        new XAttribute("Condition", condition));

                    var clCompile = new XElement(NS + "ClCompile");

                    // Warning level
                    clCompile.Add(new XElement(NS + "WarningLevel", "Level3"));

                    // SDL checks
                    clCompile.Add(new XElement(NS + "SDLCheck", "true"));

                    // Preprocessor definitions
                    List<string> defines = new List<string>();
                    defines.Add("WIN32");
                    defines.Add("_WINDOWS");
                    defines.Add("UNICODE");
                    defines.Add("_UNICODE");
                    if (config == "Debug")
                    {
                        defines.Add("_DEBUG");
                    }
                    else
                    {
                        defines.Add("NDEBUG");
                    }

                    // Add custom defines
                    foreach (var define in _project.Defines)
                    {
                        defines.Add(define);
                    }

                    clCompile.Add(new XElement(NS + "PreprocessorDefinitions", string.Join(";", defines) + ";%(PreprocessorDefinitions)"));

                    // Debug information format
                    if (config == "Debug")
                    {
                        clCompile.Add(new XElement(NS + "DebugInformationFormat", "ProgramDatabase"));
                        clCompile.Add(new XElement(NS + "Optimization", "Disabled"));
                    }
                    else
                    {
                        clCompile.Add(new XElement(NS + "Optimization", "MaxSpeed"));
                        clCompile.Add(new XElement(NS + "FunctionLevelLinking", "true"));
                        clCompile.Add(new XElement(NS + "IntrinsicFunctions", "true"));
                    }

                    // C++ standard
                    switch (_project.CxxVersion)
                    {
                        case CxxVersion.StdCxx14:
                            clCompile.Add(new XElement(NS + "LanguageStandard", "stdcpp14"));
                            break;
                        case CxxVersion.StdCxx17:
                            clCompile.Add(new XElement(NS + "LanguageStandard", "stdcpp17"));
                            break;
                        case CxxVersion.Latest:
                            clCompile.Add(new XElement(NS + "LanguageStandard", "latest"));
                            break;
                        case CxxVersion.Default:
                        case CxxVersion.StdCxx20:
                        default:
                            clCompile.Add(new XElement(NS + "LanguageStandard", "stdcpp20"));
                            break;
                    }

                    // Include directories
                    List<string> includeDirs = new List<string>();
                    includeDirs.Add("$(ProjectDir)");

                    // Add project's own include directories
                    foreach (var includeDir in _project.IncludeDirectories)
                    {
                        if (!string.IsNullOrEmpty(includeDir))
                        {
                            includeDirs.Add(includeDir);
                        }
                    }

                    // Add dependency include directories
                    foreach (var dep in _project.Dependencies)
                    {
                        if (!string.IsNullOrEmpty(dep.IncludeDirectory) && Directory.Exists(dep.IncludeDirectory))
                        {
                            includeDirs.Add(dep.IncludeDirectory);
                        }
                    }

                    if (includeDirs.Count > 1)
                    {
                        clCompile.Add(new XElement(NS + "AdditionalIncludeDirectories", string.Join(";", includeDirs) + ";%(AdditionalIncludeDirectories)"));
                    }

                    itemDefGroup.Add(clCompile);

                    // Linker settings
                    var link = new XElement(NS + "Link");

                    if (config == "Debug")
                    {
                        link.Add(new XElement(NS + "GenerateDebugInformation", "true"));
                    }

                    // Subsystem
                    if (_project.Type == ProjectType.Executable)
                    {
                        link.Add(new XElement(NS + "SubSystem", "Console"));
                    }

                    // Additional library directories
                    List<string> libDirs = new List<string>();

                    // Add project's own library directories
                    foreach (var libDir in _project.LibraryDirectories)
                    {
                        if (!string.IsNullOrEmpty(libDir))
                        {
                            libDirs.Add(libDir);
                        }
                    }

                    // Add dependency library directories
                    foreach (var dep in _project.Dependencies)
                    {
                        if (!string.IsNullOrEmpty(dep.LibraryDirectory) && Directory.Exists(dep.LibraryDirectory))
                        {
                            libDirs.Add(dep.LibraryDirectory);
                        }
                    }

                    if (libDirs.Count > 0)
                    {
                        link.Add(new XElement(NS + "AdditionalLibraryDirectories", string.Join(";", libDirs) + ";%(AdditionalLibraryDirectories)"));
                    }

                    // Additional dependencies
                    List<string> libs = new List<string>();
                    foreach (var dep in _project.Dependencies)
                    {
                        foreach (var lib in dep.Libraries)
                        {
                            libs.Add(lib);
                        }
                    }

                    if (libs.Count > 0)
                    {
                        link.Add(new XElement(NS + "AdditionalDependencies", string.Join(";", libs) + ";%(AdditionalDependencies)"));
                    }

                    itemDefGroup.Add(link);

                    parent.Add(itemDefGroup);
                }
            }
        }

        private XElement CreateSourceFiles()
        {
            var itemGroup = new XElement(NS + "ItemGroup");

            // Header files
            foreach (var header in _project.Headers)
            {
                itemGroup.Add(new XElement(NS + "ClInclude",
                    new XAttribute("Include", header)));
            }

            // Shared header files
            foreach (var shared in _project.SharedHeaders)
            {
                if (!_project.Headers.Contains(shared))
                {
                    itemGroup.Add(new XElement(NS + "ClInclude",
                        new XAttribute("Include", shared)));
                }
            }

            // Source files
            foreach (var source in _project.Sources)
            {
                itemGroup.Add(new XElement(NS + "ClCompile",
                    new XAttribute("Include", source)));
            }

            return itemGroup;
        }

        private string GetProjectType()
        {
            switch (_project.Type)
            {
                case ProjectType.Executable:
                    return "Application";
                case ProjectType.StaticLibrary:
                    return "StaticLibrary";
                case ProjectType.DynamicLibrary:
                    return "DynamicLibrary";
                default:
                    return "Application";
            }
        }
    }
}
