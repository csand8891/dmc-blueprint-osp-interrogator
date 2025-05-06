namespace DmcBlueprint.Models
{
    public class DistributorAndCustomerInfo
    {
        public ContactEntry Distributor { get; set; }
        public ContactEntry EndCustomer { get; set; }

        public DistributorAndCustomerInfo()
        {
            Distributor = new ContactEntry();
            EndCustomer = new ContactEntry();
        }
    }
}