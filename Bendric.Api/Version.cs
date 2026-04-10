using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bendric.Api
{
    public class Version
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public string Identification { get; set; }

        public Version(int major, int minor, int patch, string identification = "")
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Identification = identification ?? "";
        }

        public static Version Make(int major, int minor, int patch, string identification = "")
        {
            return new Version(major, minor, patch, identification);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Identification))
                return $"{Major}.{Minor}.{Patch}";
            return $"{Major}.{Minor}.{Patch}-{Identification}";
        }
    }
}
