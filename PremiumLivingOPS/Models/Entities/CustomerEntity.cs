namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Entity representing a row in the Customer table.
    /// Schema columns: CustomerID, CustomerName, ContactPhone, ContactEmail
    ///
    /// Alias properties EmailAddress and PhoneNumber are provided for
    /// backward-compatibility with MasterDataRepo and CustomerListForm
    /// which were authored against the older column naming convention.
    /// Both sets of properties share the same backing fields.
    /// </summary>
    public class CustomerEntity
    {
        // ── Canonical properties (match schema + OrderProcessingRepo) ─────────
        public string CustomerID   { get; set; }
        public string CustomerName { get; set; }
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }

        // ── Backward-compat aliases (used by MasterDataRepo + CustomerListForm) ─
        /// <summary>Alias for ContactEmail — used by MasterDataRepo / CustomerListForm.</summary>
        public string EmailAddress
        {
            get { return ContactEmail; }
            set { ContactEmail = value; }
        }

        /// <summary>Alias for ContactPhone — used by MasterDataRepo / CustomerListForm.</summary>
        public string PhoneNumber
        {
            get { return ContactPhone; }
            set { ContactPhone = value; }
        }
    }
}
