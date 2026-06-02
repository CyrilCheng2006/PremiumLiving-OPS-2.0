using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Logistics Processing – Handling Goods Received page.
    /// MVC: Controller handles all DB access; this form is pure View.
    /// </summary>
    public partial class HandlingGoodsReceivedForm : Form
    {
        private readonly LogisticsProcessingController _controller = new LogisticsProcessingController();
        private HandlingGoodsReceivedVM _vm;

        public HandlingGoodsReceivedForm()
        {
            InitializeComponent();
            Load += HandlingGoodsReceivedForm_Load;
        }

        private void HandlingGoodsReceivedForm_Load(object sender, EventArgs e) => RefreshData();

        private void RefreshData(string status = null, string keyword = null, DateTime? from = null)
        {
            try
            {
                _vm = _controller.GetHandlingGoodsReceivedVM(status, keyword, from);
                userInfoLabel.UserName   = _vm.UserBar.DisplayName;
                userInfoLabel.Department = _vm.UserBar.Department;
                topNavBar.SetVisibleMenus(_vm.AllowedMenus);
                BindReceiptsGrid(_vm.Receipts);
                BindPurchaseOrdersGrid(_vm.PurchaseOrders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading goods received:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindReceiptsGrid(System.Collections.Generic.List<GoodsReceivedEntity> data)
        {
            dgvReceipts.Rows.Clear();
            foreach (var r in data)
            {
                int i = dgvReceipts.Rows.Add(
                    r.ReceiptID, r.PurchaseID, r.SupplierName,
                    r.RawMaterialItemID, r.ItemName,
                    r.QtyReceived, r.OutstandingQty?.ToString() ?? "0",
                    r.ReceiptDate.ToString("yyyy-MM-dd"),
                    r.WarehouseLocation, r.PurchaseStatus,
                    r.UnitPrice.ToString("C"));

                var cell = dgvReceipts.Rows[i].Cells["colRStatus"];
                switch (r.PurchaseStatus)
                {
                    case "Received":
                    case "Completed":          cell.Style.ForeColor = Color.FromArgb(67, 122, 34);  break;
                    case "Partially Received": cell.Style.ForeColor = Color.FromArgb(0, 100, 148);  break;
                    case "Sent":               cell.Style.ForeColor = Color.FromArgb(154, 66, 25);  break;
                    default:                   cell.Style.ForeColor = Color.FromArgb(122, 121, 116); break;
                }
            }
            lblReceiptCount.Text = $"{data.Count} receipt(s)";
        }

        private void BindPurchaseOrdersGrid(System.Collections.Generic.List<PurchaseOrderEntity> data)
        {
            dgvPO.Rows.Clear();
            foreach (var po in data)
            {
                int i = dgvPO.Rows.Add(
                    po.PurchaseID, po.SupplierName,
                    po.OrderDate.ToString("yyyy-MM-dd"),
                    po.POTotalAmount.ToString("C"), po.PurchaseStatus);

                var cell = dgvPO.Rows[i].Cells["colPOStatus"];
                switch (po.PurchaseStatus)
                {
                    case "Received":
                    case "Completed":          cell.Style.ForeColor = Color.FromArgb(67, 122, 34);  break;
                    case "Partially Received": cell.Style.ForeColor = Color.FromArgb(0, 100, 148);  break;
                    case "Sent":               cell.Style.ForeColor = Color.FromArgb(154, 66, 25);  break;
                    default:                   cell.Style.ForeColor = Color.FromArgb(122, 121, 116); break;
                }
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string status  = cmbStatusFilter.SelectedIndex > 0 ? cmbStatusFilter.SelectedItem.ToString() : null;
            string keyword = txtSearch.Text.Trim();
            DateTime? from = dtpFrom.Checked ? dtpFrom.Value.Date : (DateTime?)null;
            RefreshData(status, string.IsNullOrEmpty(keyword) ? null : keyword, from);
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            cmbStatusFilter.SelectedIndex = 0;
            txtSearch.Clear();
            dtpFrom.Checked = false;
            RefreshData();
        }

        private void TopNavBar_MenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);
    }
}
