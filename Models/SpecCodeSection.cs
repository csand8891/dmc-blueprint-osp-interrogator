using System.Collections.Generic;
namespace DmcBlueprint.Models
{
    public class SpecCodeSection
    {
        public string? SectionTitle { get; set; }
        public List<string> HexCodes { get; set; }
        public List<SpecFeature> SpecCodes { get; set; }    

        public SpecCodeSection()
        {
            HexCodes = new List<string>();
            SpecCodes = new List<SpecFeature>();
        }
    }
}