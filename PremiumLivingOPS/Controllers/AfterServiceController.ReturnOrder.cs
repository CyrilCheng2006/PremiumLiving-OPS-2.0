using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Partial controller — Return Order create / picker / ID-generation methods.
    /// All four CS0111 duplicates have been removed from the main controller file.
    /// </summary>
    public partial class AfterServiceController
    {
        // ── Picker data sources ────────────────────────────────────────────

        /// <summary>
        /// Returns orders eligible for return (Delivered / Completed / Partially Delivered).
        /// Used to populate the Order ID Picker in Create Return Order.
        /// </summary>
        public List<OrderEntity> GetOrdersForReturnPicker(string keyword = null)
            => _repo.GetOrdersForReturnPicker(keyword);

        /// <summary>
        /// Returns staff list with Department and Role columns.
        /// Used to populate the Staff Picker in Create Return Order.
        /// </summary>
        public List<(string StaffID, string StaffName, string Department, string StaffRole)>
            GetStaffListForPicker()
            => _repo.GetStaffListForPicker();

        // ── Return ID generation ─────────────────────────────────────────

        /// <summary>
        /// Returns existing ReturnIDs that start with the given prefix.
        /// Called by CreateReturnOrderDialog to compute the next daily sequence
        /// number for the RTN-YYYYMMDD-XXXX format.
        /// </summary>
        public List<string> GetReturnIdsByPrefix(string prefix)
            => _repo.GetReturnIdsByPrefix(prefix);

        /// <summary>
        /// Generates the next ReturnID in RTN-YYYYMMDD-XXXX format.
        /// </summary>
        public string GenerateReturnId()
        {
            string prefix   = "RTN-" + DateTime.Today.ToString("yyyyMMdd") + "-";
            var    existing = GetReturnIdsByPrefix(prefix);
            int    next     = 1;
            foreach (var id in existing)
            {
                if (id.Length >= prefix.Length + 4 &&
                    int.TryParse(id.Substring(prefix.Length, 4), out int seq) &&
                    seq >= next)
                    next = seq + 1;
            }
            return $"{prefix}{next:D4}";
        }

        // ── Persist ───────────────────────────────────────────────────

        /// <summary>
        /// Saves a new ReturnOrder. Generates RTN-YYYYMMDD-XXXX ID automatically
        /// and defaults status to "Pending" when not supplied.
        /// </summary>
        public bool CreateReturnOrder(ReturnOrderEntity entity)
        {
            if (string.IsNullOrWhiteSpace(entity.ReturnID))
                entity.ReturnID = GenerateReturnId();
            if (string.IsNullOrWhiteSpace(entity.ReturnStatus))
                entity.ReturnStatus = "Pending";
            return _repo.CreateReturnOrder(entity);
        }
    }
}
