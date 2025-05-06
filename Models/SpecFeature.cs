namespace DmcBlueprint.Models
{
    public class SpecFeature
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; }

        public SpecFeature(string name, bool isEnabled)
        {
            Name = name.Trim();
            IsEnabled = isEnabled;
        }
    }
}