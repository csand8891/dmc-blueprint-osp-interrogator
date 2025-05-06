namespace DmcBlueprint.Models
{
    public class SoftwareDataManagementCard
    {
        public MachineIdentifier MachineDetails { get; set; }
        public DistributorAndCustomerInfo DistributorAndCustomerDetails { get; set; }
        public RevisionAndCustomizationInfo RevisionAndCustomization { get; set; }
        public DvdMediaVersionData DvdMediaVersions { get; set; }
        public SoftwareVersionIdentifier AdditionalSoftwareDvdVersions { get; set; }
        public List<SoftwarePackage> SoftwarePackageComposition { get; set; }
        public List<CustomSoftwareGroup> CustomSoftwareComposition { get; set; }
        public List<SpecCodeSection> NcSpecCodes { get; set; }
        public List<SpecCodeSection> PlcSpecCodes { get; set; }

        public SoftwareDataManagementCard()
        {
            SoftwarePackageComposition = new List<SoftwarePackage>();
            CustomSoftwareComposition = new List<CustomSoftwareGroup>();
            NcSpecCodes = new List<SpecCodeSection>();
            PlcSpecCodes = new List<SpecCodeSection>();
        }
    }

}