using System;

namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>Maps to the Transaction table in the database schema.</summary>
    public class TransactionEntity
    {
        public string   TransactionID   { get; set; }
        public string   InvoiceID       { get; set; }
        public string   PurInvoiceID    { get; set; }
        public string   ReturnID        { get; set; }
        public double   Amount          { get; set; }
        public DateTime TransactionDate { get; set; }
        /// <summary>Deposit | Installment | Full | Refund</summary>
        public string   TransactionType { get; set; }
    }
}
