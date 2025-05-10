namespace DmcBlueprint.Models
{
    public class SoftwarePackage
    {
        // Descriptive name of software package
        public string? PackageName { get; set; }
        // Filename associated with software package
        public string? Identifier { get; set; }

        public SoftwarePackage(string packageName, string identifier)
        {
            PackageName = packageName;
            Identifier = identifier;
        }

        public SoftwarePackage() { }

        public override string ToString()
        {
            return $"{PackageName} - {Identifier}";
        }
    }
}