using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Bendric.Api
{
    public class VcxprojFiltersGenerator
    {
        private Project _project;
        private string _outputPath;
        private string _projectDirectory;
        private static readonly XNamespace NS = "http://schemas.microsoft.com/developer/msbuild/2003";

        public VcxprojFiltersGenerator(Project project, string outputPath, string projectDirectory)
        {
            _project = project;
            _outputPath = outputPath;
            _projectDirectory = projectDirectory;
        }

        public void Generate()
        {
            XDocument doc = new XDocument();
            var projectElement = new XElement(NS + "Project",
                new XAttribute("ToolsVersion", "4.0"));

            // Create filter groups based on directory structure
            var filterGuids = CreateFilterHierarchy(projectElement);

            // Add header files with filters
            if (_project.Headers.Count > 0 || _project.SharedHeaders.Count > 0)
            {
                var headerGroup = new XElement(NS + "ItemGroup");
                var allHeaders = _project.Headers.Union(_project.SharedHeaders).Distinct();
                foreach (var header in allHeaders)
                {
                    var filter = GetFilterForFile(header);
                    var element = new XElement(NS + "ClInclude",
                        new XAttribute("Include", header));
                    if (!string.IsNullOrEmpty(filter))
                    {
                        element.Add(new XElement(NS + "Filter", filter));
                    }
                    headerGroup.Add(element);
                }
                projectElement.Add(headerGroup);
            }

            // Add source files with filters
            if (_project.Sources.Count > 0)
            {
                var sourceGroup = new XElement(NS + "ItemGroup");
                foreach (var source in _project.Sources)
                {
                    var filter = GetFilterForFile(source);
                    var element = new XElement(NS + "ClCompile",
                        new XAttribute("Include", source));
                    if (!string.IsNullOrEmpty(filter))
                    {
                        element.Add(new XElement(NS + "Filter", filter));
                    }
                    sourceGroup.Add(element);
                }
                projectElement.Add(sourceGroup);
            }

            doc.Add(projectElement);
            doc.Save(_outputPath);

            Logger.Info($"Generated: {_outputPath}");
        }

        /// <summary>
        /// Creates the filter hierarchy based on actual directory structure
        /// </summary>
        private Dictionary<string, string> CreateFilterHierarchy(XElement parent)
        {
            var filterGuids = new Dictionary<string, string>();
            var filters = new HashSet<string>();

            // Collect all directories from source and header files
            var allFiles = _project.Headers
                .Union(_project.SharedHeaders)
                .Union(_project.Sources)
                .Distinct();

            foreach (var file in allFiles)
            {
                var directory = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(directory))
                {
                    // Add this directory and all parent directories
                    var current = directory;
                    while (!string.IsNullOrEmpty(current))
                    {
                        filters.Add(current);
                        current = Path.GetDirectoryName(current);
                    }
                }
            }

            // Sort filters by depth (parent directories first)
            var sortedFilters = filters.OrderBy(f => f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length).ToList();

            // Create filter elements
            if (sortedFilters.Count > 0)
            {
                var filterGroup = new XElement(NS + "ItemGroup");
                foreach (var filter in sortedFilters)
                {
                    var guid = Guid.NewGuid().ToString("B");
                    filterGuids[filter] = guid;

                    var filterElement = new XElement(NS + "Filter",
                        new XAttribute("Include", filter));
                    filterElement.Add(new XElement(NS + "UniqueIdentifier", guid));
                    filterGroup.Add(filterElement);
                }
                parent.Add(filterGroup);
            }

            return filterGuids;
        }

        private string GetFilterForFile(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            return directory ?? "";
        }
    }
}
