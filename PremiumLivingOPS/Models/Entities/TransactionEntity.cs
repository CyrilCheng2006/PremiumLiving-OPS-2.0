using System;

namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>Maps to the Transaction table in the schema.</summary>
    public class TransactionEntity
    {
        public string   TransactionID   { get; set; }
        public string   InvoiceID       { get; set; }
        public string   PurInvoiceID    { get; set; }
        public string   ReturnID        { get; set; }
        public double   Amount          { get; set; }
        public DateTime TransactionDate { get; set; }
        public string   TransactionType { get; set; }  // Deposit | Installment | Full | Refund
    }
}
