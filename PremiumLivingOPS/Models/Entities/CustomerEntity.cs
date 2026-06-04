namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Entity representing a row in the Customer table.
    /// Schema: CustomerID, CustomerName, EmailAddress, PhoneNumber
    /// </summary>
    public class CustomerEntity
    {
        public string CustomerID   { get; set; }
        public string CustomerName { get; set; }
        public string EmailAddress { get; set; }
        public string PhoneNumber  { get; set; }
    }
}
