using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Logistics Processing — Handling Goods Received
    /// MVC: all DB access via LogisticsProcessingController.
    /// </summary>
    public partial class HandlingGoodsReceivedForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private HandlingGoodsReceivedVM _vm;
        private int _activeGridIndex = 0;

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusTheme
            = new Dictionary<string, (Color, Color)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Sent"]               = (FromHex("#FEF3C7"), FromHex("#92400E")),
            ["Partially Received"] = (FromHex("#DBEAFE"), FromHex("#1D4ED8")),
            ["Received"]           = (FromHex("#E0F2FE"), FromHex("#0360AA")),
            ["Completed"]          = (FromHex("#D1FAE5"), FromHex("#065F46")),
            ["Cancelled"]          = (FromHex("#F3F4F6"), FromHex("#6B7280")),
            ["Partial"]            = (FromHex("#FEF3C7"), FromHex("#92400E")),
            ["Full"]               = (FromHex("#D1FAE5"), FromHex("#065F46"))
        };

        public HandlingGoodsReceivedForm()
        {
            InitializeComponent();
            this.Load += HandlingGoodsReceivedForm_Load;
        }

        private static Color FromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16));
        }

        // ━━━ Load ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void HandlingGoodsReceivedForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += BtnLogout_Click;
            RefreshGrids();
            SwitchToGrid(0);
        }

        // ━━━ AppShell Navigation ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to log out?",
                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            // fix CS0200: CurrentUser is read-only; use SessionManager.Clear() instead
            SessionManager.Clear();
            // fix CS0234: LoginForm is in Views.Auth, not Views.Login
            var login = new Auth.LoginForm();
            login.Show();
            this.Hide();
            login.FormClosed += (s, _) => this.Close();
        }

        private void OnTopNavMenuItemClicked(object sender, string menuItem)
        {
            FormNavigator.NavigateTo(this, menuItem);
        }

        // ━━━ Grid Tab Switcher ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        internal void SwitchToGrid(int index)
        {
            _activeGridIndex = index;
            var tabs = new[] { btnTabReceipts, btnTabPO, btnTabInvoices };

            for (int i = 0; i < tabs.Length; i++)
            {
                bool active = (i == index);
                tabs[i].ForeColor = active
                    ? Color.FromArgb(47, 111, 237)
                    : Color.FromArgb(98, 112, 135);
                tabs[i].Font = active
                    ? new Font("Segoe UI", 12f, FontStyle.Bold)
                    : new Font("Segoe UI", 12f);
                tabs[i].Invalidate();
                if (tabs[i].Tag is Panel card)
                    card.Visible = active;
            }

            if (!_tabPaintWired)
            {
                btnTabReceipts.Paint += PaintTabUnderline;
                btnTabPO.Paint       += PaintTabUnderline;
                btnTabInvoices.Paint += PaintTabUnderline;
                _tabPaintWired = true;
            }

            UpdateActionButtons();
        }

        private bool _tabPaintWired = false;

        private void PaintTabUnderline(object sender, PaintEventArgs e)
        {
            var btn = (Button)sender;
            bool isActive = btn.ForeColor == Color.FromArgb(47, 111, 237);
            if (!isActive) return;
            using var pen = new Pen(Color.FromArgb(47, 111, 237), 3f);
            int y = btn.Height - 2;
            e.Graphics.DrawLine(pen, 0, y, btn.Width, y);
        }

        private void UpdateActionButtons()
        {
            switch (_activeGridIndex)
            {
                case 0:
                    bool hasRcpt = dgvReceipts.SelectedRows.Count > 0;
                    btnViewReceiptLines.Enabled = hasRcpt;
                    btnUploadReceipt.Enabled    = true;
                    if (hasRcpt)
                    {
                        var r = dgvReceipts.SelectedRows[0].Tag as GoodsReceivedEntity;
                        btnViewPODetail.Enabled  = r?.PurchaseID != null;
                        btnRecordInvoice.Enabled = r?.PurchaseID != null;
                    }
                    else
                    {
                        btnViewPODetail.Enabled  = false;
                        btnRecordInvoice.Enabled = false;
                    }
                    break;

                case 1:
                    bool hasPO = dgvPO.SelectedRows.Count > 0;
                    btnViewPODetail.Enabled     = hasPO;
                    btnRecordInvoice.Enabled    = hasPO;
                    btnViewReceiptLines.Enabled = false;
                    btnUploadReceipt.Enabled    = false;
                    break;

                case 2:
                    btnViewPODetail.Enabled     = false;
                    btnRecordInvoice.Enabled    = false;
                    btnViewReceiptLines.Enabled = false;
                    btnUploadReceipt.Enabled    = false;
                    break;
            }
        }
    }
}
