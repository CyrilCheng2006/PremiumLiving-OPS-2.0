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
    /// Shows Receipt records (joined with PurchaseOrder / Supplier / Item / Warehouse)
    /// and a summary panel of Purchase Orders below.
    /// Follows MVC + CardPanel three-layer nesting.
    /// </summary>
    public partial class HandlingGoodsReceivedForm : Form
    {
        // ── Fields ─────────────────────────────────────────────────────
        private readonly LogisticsProcessingController _controller = new LogisticsProcessingController();
        private HandlingGoodsReceivedVM _vm;

        // ── Constructor ─────────────────────────────────────────────────
        public HandlingGoodsReceivedForm()
        {
            InitializeComponent();
            Load += HandlingGoodsReceivedForm_Load;
        }

        // ── Load ─────────────────────────────────────────────────────────
        private void HandlingGoodsReceivedForm_Load(object sender, EventArgs e)
        {
            RefreshData();
        }

        // ── RefreshData ─────────────────────────────────────────────────
        private void RefreshData(string status = null, string keyword = null, DateTime? from = null)
        {
            try
            {
                _vm = _controller.GetHandlingGoodsReceivedVM(status, keyword, from);

                // User bar
                lblUserName.Text   = _vm.UserBar.DisplayName;
                lblDepartment.Text = _vm.UserBar.Department;

                // Nav bar
                topNavBar.SetAllowedMenus(_vm.AllowedMenus);

                // Bind receipts grid
                BindReceiptsGrid(_vm.Receipts);

                // Bind purchase orders grid
                BindPurchaseOrdersGrid(_vm.PurchaseOrders);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading goods received:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── BindReceiptsGrid ───────────────────────────────────────────
        private void BindReceiptsGrid(System.Collections.Generic.List<GoodsReceivedEntity> data)
        {
            dgvReceipts.Rows.Clear();
            foreach (var r in data)
            {
                int i = dgvReceipts.Rows.Add(
                    r.ReceiptID,
                    r.PurchaseID,
                    r.SupplierName,
                    r.RawMaterialItemID,
                    r.ItemName,
                    r.QtyReceived,
                    r.OutstandingQty?.ToString() ?? "0",
                    r.ReceiptDate.ToString("yyyy-MM-dd"),
                    r.WarehouseLocation,
                    r.PurchaseStatus,
                    r.UnitPrice.ToString("C"));

                // Status badge colour
                var cell = dgvReceipts.Rows[i].Cells["colRStatus"];
                switch (r.PurchaseStatus)
                {
                    case "Received":
                    case "Completed":          cell.Style.ForeColor = Color.FromArgb(67, 122, 34); break;
                    case "Partially Received": cell.Style.ForeColor = Color.FromArgb(0, 100, 148); break;
                    case "Sent":               cell.Style.ForeColor = Color.FromArgb(154, 66, 25); break;
                    default:                   cell.Style.ForeColor = Color.FromArgb(122, 121, 116); break;
                }
            }

            lblReceiptCount.Text = $"{data.Count} receipt(s)";
        }

        // ── BindPurchaseOrdersGrid ─────────────────────────────────────
        private void BindPurchaseOrdersGrid(System.Collections.Generic.List<PurchaseOrderEntity> data)
        {
            dgvPO.Rows.Clear();
            foreach (var po in data)
            {
                int i = dgvPO.Rows.Add(
                    po.PurchaseID,
                    po.SupplierName,
                    po.OrderDate.ToString("yyyy-MM-dd"),
                    po.POTotalAmount.ToString("C"),
                    po.PurchaseStatus);

                var cell = dgvPO.Rows[i].Cells["colPOStatus"];
                switch (po.PurchaseStatus)
                {
                    case "Received":
                    case "Completed":          cell.Style.ForeColor = Color.FromArgb(67, 122, 34); break;
                    case "Partially Received": cell.Style.ForeColor = Color.FromArgb(0, 100, 148); break;
                    case "Sent":               cell.Style.ForeColor = Color.FromArgb(154, 66, 25); break;
                    default:                   cell.Style.ForeColor = Color.FromArgb(122, 121, 116); break;
                }
            }
        }

        // ── Filter / Search ───────────────────────────────────────────────
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

        // ── Navigation (TopNavBar event) ──────────────────────────────
        private void topNavBar_MenuItemClicked(object sender, MenuItemClickedEventArgs e)
        {
            FormNavigator.NavigateTo(this, e.MenuLabel, e.SubItem);
        }
    }
}
