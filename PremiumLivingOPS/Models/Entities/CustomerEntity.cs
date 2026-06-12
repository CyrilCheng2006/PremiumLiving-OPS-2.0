namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Entity representing a row in the Customer table.
    /// Schema: CustomerID, CustomerName, ContactPhone, ContactEmail
    /// </summary>
    public class CustomerEntity
    {
        public string CustomerID   { get; set; }
        public string CustomerName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
    }
}
