using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Partial class extension of AfterServiceRepo.
    /// Contains the AP 3-Way Match verification data access method.
    ///
    /// 3-Way Match Rule:
    ///   PurchaseOrder.POTotalAmount
    ///   == SUM(Receipt.QtyReceived × PurchaseOrderLine.UnitPrice)  [Supplier Receipt total]
    ///   == PurchaseInvoice.TotalAmount
    ///   All three equal → eligible to be recorded as Account Payable.
    /// </summary>
    public partial class AfterServiceRepo
    {
        // ══════════════════════════════════════════════════════════════════════════════
        //  GetAPVerificationDetail
        //
        //  Given a PurchaseInvoice ID, loads:
        //    1. PurchaseOrder header + Supplier info
        //    2. PurchaseInvoice header
        //    3. PurchaseOrderLine rows + per-line QtyReceived (SUM from Receipt)
        //    4. Computes SupplierReceiptTotal
        //
        //  Returns null if the PurInvoiceID does not exist.
        // ══════════════════════════════════════════════════════════════════════════════
        public APVerificationDetailVM GetAPVerificationDetail(string purInvoiceId)
        {
            if (string.IsNullOrWhiteSpace(purInvoiceId)) return null;

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();

            var vm = new APVerificationDetailVM();

            // ── Step 1: PurchaseInvoice + PurchaseOrder + Supplier ──────────────────
            const string headerSql = @"
                SELECT
                    pi.PurInvoiceID,
                    pi.PurchaseID,
                    pi.TotalAmount      AS InvTotalAmount,
                    pi.PaymentStatus    AS InvPayStatus,
                    pi.ExpectedDate,
                    po.POTotalAmount,
                    po.OrderDate,
                    po.PurchaseStatus,
                    s.SupplierID,
                    s.SupplierName,
                    s.PhoneNumber       AS SupplierPhone,
                    s.SupplierAddress
                FROM PurchaseInvoice pi
                JOIN PurchaseOrder   po ON po.PurchaseID  = pi.PurchaseID
                JOIN Supplier        s  ON s.SupplierID   = po.SupplierID
                WHERE pi.PurInvoiceID = @pid
                LIMIT 1";

            using (var cmd = new MySqlCommand(headerSql, conn))
            {
                cmd.Parameters.AddWithValue("@pid", purInvoiceId);
                using var rdr = cmd.ExecuteReader();
                if (!rdr.Read()) return null;   // record not found

                vm.PurInvoiceID    = rdr.GetString("PurInvoiceID");
                vm.PurchaseID      = rdr.GetString("PurchaseID");
                vm.InvTotalAmount  = rdr.GetDouble("InvTotalAmount");
                vm.InvPayStatus    = rdr.GetString("InvPayStatus");
                vm.ExpectedDate    = rdr.GetDateTime("ExpectedDate");
                vm.POTotalAmount   = rdr.GetDouble("POTotalAmount");
                vm.OrderDate       = rdr.GetDateTime("OrderDate");
                vm.PurchaseStatus  = rdr.GetString("PurchaseStatus");
                vm.SupplierID      = rdr.GetString("SupplierID");
                vm.SupplierName    = rdr.GetString("SupplierName");
                vm.SupplierPhone   = rdr.GetString("SupplierPhone");
                vm.SupplierAddress = rdr.GetString("SupplierAddress");
            }

            // ── Step 2: PurchaseOrderLine rows + per-line QtyReceived ────────────
            const string linesSql = @"
                SELECT
                    pol.POLineID,
                    pol.RawMaterialItemID,
                    i.ItemName,
                    pol.OrderQty,
                    pol.UnitPrice,
                    COALESCE(SUM(r.QtyReceived), 0) AS QtyReceived
                FROM PurchaseOrderLine pol
                JOIN Item i ON i.ItemID = pol.RawMaterialItemID
                LEFT JOIN Receipt r ON r.POLineID = pol.POLineID
                WHERE pol.PurchaseID = @pid2
                GROUP BY pol.POLineID, pol.RawMaterialItemID, i.ItemName,
                         pol.OrderQty, pol.UnitPrice";

            using (var cmd2 = new MySqlCommand(linesSql, conn))
            {
                cmd2.Parameters.AddWithValue("@pid2", vm.PurchaseID);
                using var rdr2 = cmd2.ExecuteReader();
                while (rdr2.Read())
                {
                    vm.Lines.Add(new APVerificationLineEntity
                    {
                        POLineID          = rdr2.GetString("POLineID"),
                        RawMaterialItemID = rdr2.GetString("RawMaterialItemID"),
                        ItemName          = rdr2.GetString("ItemName"),
                        OrderQty          = rdr2.GetInt32("OrderQty"),
                        UnitPrice         = rdr2.GetDouble("UnitPrice"),
                        QtyReceived       = rdr2.GetInt32("QtyReceived"),
                    });
                }
            }

            // ── Step 3: Compute SupplierReceiptTotal ───────────────────────────
            double receiptTotal = 0.0;
            foreach (var line in vm.Lines)
                receiptTotal += line.QtyReceived * line.UnitPrice;
            vm.SupplierReceiptTotal = Math.Round(receiptTotal, 2);

            return vm;
        }
    }
}
