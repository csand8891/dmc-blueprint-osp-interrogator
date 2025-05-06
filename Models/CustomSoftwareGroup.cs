namespace DmcBlueprint.Models
{
    public class CustomSoftwareGroup
    {
        public string GroupName { get; set; }
        public List<string> FilePaths { get; set; }

        public CustomSoftwareGroup()
        {
            FilePaths = new List<string>();
        }
    }
}