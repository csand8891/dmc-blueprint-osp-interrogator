using System.Collections.Generic;

namespace DmcBlueprint.Models
{
    public class RevisionAndCustomizationInfo
    {
        public string GeneralNote { get; set; }
        public string CustomSpecVersions { get; set; }
        public List<RevisionEntry> RevisionEntries { get; set; }
        public bool IsWindowsSystemDiskMissing { get; set; }

        public RevisionAndCustomizationInfo()
        {
            RevisionEntries = new List<RevisionEntry>();
        }
    }
}