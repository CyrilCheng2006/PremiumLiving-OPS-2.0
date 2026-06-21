using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Invoice List + Record Payment popup dialog.
    /// Carries the full Invoice list (with Transaction history per invoice).
    /// </summary>
    public class InvoiceListViewModel
    {
        public UserBarViewModel          UserBar      { get; set; }
        public string[]                  AllowedMenus { get; set; }
        public List<InvoiceDetailEntity> Invoices     { get; set; } = new List<InvoiceDetailEntity>();
    }
}
