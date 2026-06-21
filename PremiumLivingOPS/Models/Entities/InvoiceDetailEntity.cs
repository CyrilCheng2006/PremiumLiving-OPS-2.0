using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Extends Invoice data with a flat CustomerName (JOIN result)
    /// and the list of Transaction rows linked to this Invoice.
    /// </summary>
    public class InvoiceDetailEntity
    {
        public string   InvoiceID        { get; set; }
        public string   OrderID          { get; set; }
        public string   CustomerName     { get; set; }
        public DateTime InvoiceDate      { get; set; }
        public double   DepositAmount    { get; set; }
        public double   PaidAmount       { get; set; }
        public double   RemainingBalance { get; set; }
        public double   TotalAmount      { get; set; }
        public string   PaymentStatus    { get; set; }
        public DateTime DueDate          { get; set; }
        public bool     IsOverdue        { get; set; }

        public List<TransactionEntity> Transactions { get; set; } = new List<TransactionEntity>();
    }
}
