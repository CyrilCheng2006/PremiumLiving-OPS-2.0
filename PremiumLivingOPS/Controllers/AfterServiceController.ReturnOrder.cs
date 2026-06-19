using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Partial controller — Return Order create/picker methods.
    /// </summary>
    public partial class AfterServiceController
    {
        /// <summary>
        /// Returns orders eligible for return (Delivered / Completed).
        /// Used to populate the Order ID Picker in Create Return Order dialog.
        /// </summary>
        public List<OrderEntity> GetOrdersForReturnPicker(string keyword = null)
            => _repo.GetOrdersForReturnPicker(keyword);

        /// <summary>
        /// Returns all staff (StaffID, StaffName, Department, StaffRole).
        /// Used to populate the Handed By Picker in Create Return Order dialog.
        /// </summary>
        public List<(string StaffID, string StaffName, string Department, string StaffRole)>
            GetStaffListForPicker()
            => _repo.GetStaffListForPicker();

        /// <summary>Generates the next ReturnID (RET-YYYYMMDD-NNNN).</summary>
        public string GenerateReturnId()
            => _repo.GenerateReturnId();

        /// <summary>
        /// Persists a new ReturnOrder to the database.
        /// HandedBy is UI-only (not stored — schema has no HandedBy column).
        /// </summary>
        public bool CreateReturnOrder(ReturnOrderEntity entity)
            => _repo.CreateReturnOrder(entity);
    }
}
