namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Maps to the Customer table in the database.
    /// Schema: Customer (CustomerID, CustomerName, ContactPhone, ContactEmail, Address, MemberTier, JoinDate)
    /// </summary>
    public class CustomerEntity
    {
        public string CustomerID    { get; set; }   // PK  e.g. CUS-0001
        public string CustomerName  { get; set; }
        public string ContactPhone  { get; set; }
        public string ContactEmail  { get; set; }
        public string Address       { get; set; }
        public string MemberTier    { get; set; }   // Bronze / Silver / Gold / Platinum
        public string JoinDate      { get; set; }   // stored as string YYYY-MM-DD

        /// <summary>Display text for ComboBox / pickers.</summary>
        public string DisplayText   => $"{CustomerID}  \u2013  {CustomerName}";
    }
}
