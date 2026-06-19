using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Controllers
{
    /// <summary>
    /// Partial controller — Return Order create/picker methods.
    /// </summary>
    public partial class AfterServiceController
    {
        public List<OrderEntity> GetOrdersForReturnPicker(string keyword = null)
            => _repo.GetOrdersForReturnPicker(keyword);

        public List<(string StaffID, string StaffName, string Department, string StaffRole)>
            GetStaffListForPicker()
            => _repo.GetStaffListForPicker();

        public string GenerateReturnId()
            => _repo.GenerateReturnId();

        public bool CreateReturnOrder(ReturnOrderEntity entity)
            => _repo.CreateReturnOrder(entity);
    }
}
