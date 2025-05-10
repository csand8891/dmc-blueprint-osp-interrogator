namespace DmcBlueprint.Models
{
    public class SpecFeature
    {
        public string? Name { get; set; }
        public bool IsEnabled { get; set; }
        public int Number { get; set; }
        public int Bit { get; set; }
        

        public SpecFeature(string name, bool isEnabled, int number, int bit)
        {
            Name = name.Trim();
            IsEnabled = isEnabled;
            Number = number;
            Bit = bit;
        }
    }
}