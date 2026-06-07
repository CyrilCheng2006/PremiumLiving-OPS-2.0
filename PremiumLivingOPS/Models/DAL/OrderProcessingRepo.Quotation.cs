using MySql.Data.MySqlClient;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.DAL
{
    /// <summary>
    /// Partial class — Quotation-specific DB operations.
    /// Extends OrderProcessingRepo with GetQuotationById and GetQuotationItems.
    /// </summary>
    public partial class OrderProcessingRepo
    {
        /// <summary>
        /// Returns a single QuotationEntity by QuotationID, including SalesStaffName,
        /// IssuedDate, and Notes joined from the Quotation table and Staff table.
        /// Returns null if not found.
        /// </summary>
        public QuotationEntity GetQuotationById(string quotationId)
        {
            const string sql = @"
                SELECT
                    q.QuotationID,
                    q.CustomerID,
                    c.CustomerName,
                    q.IssuedDate,
                    q.ExpiryDate,
                    q.TotalAmount,
                    q.DepositRequired,
                    q.LeadTimeEstimated,
                    q.TermsandCondition,
                    q.QuotationStatus,
                    q.Notes,
                    CONCAT(s.FirstName, ' ', s.LastName) AS SalesStaffName
                FROM Quotation q
                LEFT JOIN Customer  c ON c.CustomerID = q.CustomerID
                LEFT JOIN Staff     s ON s.StaffID    = q.SalesID
                WHERE q.QuotationID = @qid
                LIMIT 1";

            using var conn = new MySqlConnection(DbConfig.ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@qid", quotationId);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new QuotationEntity
            {
                QuotationID       = r["QuotationID"].ToString(),
                CustomerID        = r["CustomerID"].ToString(),
                CustomerName      = r["CustomerName"].ToString(),
                IssuedDate        = r["IssuedDate"]   == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["IssuedDate"]),
                ExpiryDate        = r["ExpiryDate"]   == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["ExpiryDate"]),
                TotalAmount       = r["TotalAmount"]   == DBNull.Value ? 0 : Convert.ToDouble(r["TotalAmount"]),
                DepositRequired   = r["DepositRequired"] == DBNull.Value ? 0 : Convert.ToDouble(r["DepositRequired"]),
                LeadTimeEstimated = r["LeadTimeEstimated"].ToString(),
                TermsandCondition = r["TermsandCondition"].ToString(),
                QuotationStatus   = r["QuotationStatus"].ToString(),
                Notes             = r["Notes"].ToString(),
                SalesStaffName    = r["SalesStaffName"].ToString()
            };
        }

        /// <summary>
        /// Returns all line items belonging to a Quotation.
        /// Maps to the QuotationItem table.
        /// </summary>
        public List<QuotationItemEntity> GetQuotationItems(string quotationId)
        {
            const string sql = @"
                SELECT
                    qi.QuotationID,
                    p.ItemName      AS ProductName,
                    qi.Quantity,
                    qi.Unit,
                    qi.UnitPrice,
                    qi.DiscountPercent,
                    qi.ItemNote
                FROM QuotationItem qi
                LEFT JOIN Product p ON p.ItemID = qi.ItemID
                WHERE qi.QuotationID = @qid
                ORDER BY qi.LineNo";

            var list = new List<QuotationItemEntity>();
            using var conn = new MySqlConnection(DbConfig.ConnectionString);
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@qid", quotationId);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new QuotationItemEntity
                {
                    QuotationID     = r["QuotationID"].ToString(),
                    ProductName     = r["ProductName"].ToString(),
                    Quantity        = r["Quantity"]       == DBNull.Value ? 0 : Convert.ToInt32(r["Quantity"]),
                    Unit            = r["Unit"].ToString(),
                    UnitPrice       = r["UnitPrice"]      == DBNull.Value ? 0 : Convert.ToDouble(r["UnitPrice"]),
                    DiscountPercent = r["DiscountPercent"] == DBNull.Value ? 0 : Convert.ToDouble(r["DiscountPercent"]),
                    ItemNote        = r["ItemNote"].ToString()
                });
            }
            return list;
        }
    }
}
