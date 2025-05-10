namespace DmcBlueprint.Models
{
    public class MachineIdentifier
    {
        public string? OspType { get; set;}
        public List<string> MachineType { get; set;}
        public string? SoftwareProductionNumber { get; set; }
        public string? ProjectNumber { get; set; }
        public DateTime SoftwareProductionDate { get; set; }    
        
        // Parameterless constructor
        public MachineIdentifier()
        {
            MachineType = new List<string>();
        }
    }
}