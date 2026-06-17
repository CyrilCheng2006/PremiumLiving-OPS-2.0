using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.ViewModels
{
    /// <summary>
    /// Represents one parsed row from the uploaded CSV file.
    /// Header expected:
    ///   PurchaseID, POLineID, QtyReceived, ReceiptDate, Outstanding_QTY
    /// ReceiptDate format: yyyy-MM-dd
    /// Outstanding_QTY is optional (leave blank = null).
    /// </summary>
    public class ReceiptImportRow
    {
        public int      RowNumber      { get; set; }   // 1-based CSV line number (excl. header)
        public string   PurchaseID     { get; set; }
        public string   POLineID       { get; set; }
        public int      QtyReceived    { get; set; }
        public DateTime ReceiptDate    { get; set; }
        public int?     OutstandingQty { get; set; }
    }

    /// <summary>
    /// Returned by Controller.ImportReceiptsFromCsv().
    /// Callers inspect SuccessCount and Errors to build a summary message.
    /// </summary>
    public class ReceiptImportResult
    {
        public int                  SuccessCount { get; set; }
        public List<string>         Errors       { get; set; } = new List<string>();
        public bool                 HasErrors    => Errors.Count > 0;
    }
}
