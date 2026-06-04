namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Entity representing a row in the Supplier table.
    /// Schema: SupplierID, PhoneNumber, SupplierAddress, SupplierName
    /// </summary>
    public class SupplierEntity
    {
        public string SupplierID      { get; set; }
        public string SupplierName    { get; set; }
        public string PhoneNumber     { get; set; }
        public string SupplierAddress { get; set; }
    }
}
