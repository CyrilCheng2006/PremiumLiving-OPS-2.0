using PremiumLivingOPS.Models.Entities;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the Invoice List + Record Payment popup dialog
    /// (Account Receivable module).
    /// </summary>
    public class InvoiceListViewModel
    {
        public UserBarViewModel          UserBar      { get; set; }
        public string[]                  AllowedMenus { get; set; }
        public List<InvoiceDetailEntity> Invoices     { get; set; } = new List<InvoiceDetailEntity>();
    }
}
