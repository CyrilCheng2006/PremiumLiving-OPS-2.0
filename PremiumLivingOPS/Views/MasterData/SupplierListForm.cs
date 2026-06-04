using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.MasterData
{
    /// <summary>
    /// View — Supplier List page (Master Data Maintenance module).
    ///
    /// MVC role: pure View. Delegates all data access to MasterDataController.
    /// UI structure follows CardPanel three-layer nested card standard:
    ///   • Card 1 — Search bar
    ///   • Card 2 — Summary KPI strip (total supplier count)
    ///   • Card 3 — DataGridView listing all suppliers
    /// </summary>
    public partial class SupplierListForm : Form
    {
        private readonly MasterDataController _ctrl = new MasterDataController();
        private List<SupplierEntity> _currentSuppliers = new List<SupplierEntity>();

        // pnlKpi is declared here; its reference is shared with Designer.cs
        private Panel pnlKpi;

        public SupplierListForm()
        {
            InitializeComponent();
            this.Load += SupplierListForm_Load;
        }

        // ── Load ─────────────────────────────────────────────────────────────
        private void SupplierListForm_Load(object sender, EventArgs e)
        {
            RefreshGrid();
        }

        // ── Data refresh ──────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string keyword = txtSearch.Text.Trim();
            var vm = _ctrl.GetSupplierListVM(string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Master Data Maintenance  ›  Supplier List");

            _currentSuppliers = vm.Suppliers;

            dgvSuppliers.Rows.Clear();
            foreach (var s in _currentSuppliers)
                dgvSuppliers.Rows.Add(
                    s.SupplierID,
                    s.SupplierName,
                    s.PhoneNumber,
                    s.SupplierAddress);

            RefreshKpi();
        }

        private void ResetFilters()
        {
            txtSearch.Text = string.Empty;
            RefreshGrid();
        }

        // ── KPI strip ────────────────────────────────────────────────────────
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            // Get unfiltered total for KPI
            var allVm = _ctrl.GetSupplierListVM();
            int total = allVm.Suppliers.Count;
            int shown = _currentSuppliers.Count;

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0),
                AutoScroll    = false
            };

            var pills = new[]
            {
                ("Total Suppliers", total.ToString(),
                 Color.FromArgb(47, 111, 237), Color.FromArgb(219, 234, 254)),
                ("Showing",         shown.ToString(),
                 Color.FromArgb(6,  95,  70),  Color.FromArgb(209, 250, 229)),
            };

            foreach (var (label, count, fg, bg) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg,
                    Size      = new Size(220, 50),
                    Margin    = new Padding(0, 0, 10, 0)
                };

                var tlp = new TableLayoutPanel
                {
                    Dock            = DockStyle.Fill,
                    ColumnCount     = 2,
                    RowCount        = 1,
                    BackColor       = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding         = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text      = count,
                    Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize  = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text      = label,
                    Font      = new Font("Segoe UI", 11f),
                    ForeColor = fg,
                    BackColor = Color.Transparent,
                    Dock      = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoSize  = false
                }, 1, 0);

                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }

            pnlKpi.Controls.Add(flow);
        }

        // ── Grid events ───────────────────────────────────────────────────────
        private void dgvSuppliers_SelectionChanged(object sender, EventArgs e) { /* reserved */ }

        private void dgvSuppliers_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            ShowDetailDialog(e.RowIndex);
        }

        // ── Detail dialog ─────────────────────────────────────────────────────
        private void ShowDetailDialog(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _currentSuppliers.Count) return;
            var s = _currentSuppliers[rowIndex];

            using var dlg = new Form
            {
                Text            = $"Supplier — {s.SupplierID}",
                Size            = new Size(640, 360),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // Header bar
            var pnlHdr = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            pnlHdr.Controls.Add(new Label
            {
                Text      = $"Supplier Details  —  {s.SupplierID}",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0)
            });

            // Body fields
            var pnlBody = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 8),
                BackColor = Color.White
            };

            var tbl = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 4,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            for (int r = 0; r < 4; r++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            var fields = new[] {
                ("Supplier ID",   s.SupplierID),
                ("Supplier Name", s.SupplierName),
                ("Phone",         s.PhoneNumber),
                ("Address",       s.SupplierAddress)
            };
            for (int i = 0; i < fields.Length; i++)
            {
                tbl.Controls.Add(MakeLblKey(fields[i].Item1), 0, i);
                tbl.Controls.Add(MakeLblVal(fields[i].Item2), 1, i);
            }
            pnlBody.Controls.Add(tbl);

            // Footer
            var pnlFtr = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 60,
                BackColor = Color.White,
                Padding   = new Padding(0, 8, 20, 8)
            };
            pnlFtr.Paint += (snd, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)snd).Width, 0);
            };
            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right,
                Width     = 130,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (snd, e) => dlg.Close();
            pnlFtr.Controls.Add(btnClose);

            dlg.Controls.Add(pnlBody);
            dlg.Controls.Add(pnlHdr);
            dlg.Controls.Add(pnlFtr);
            dlg.ShowDialog(this);
        }

        // ── Label helpers ─────────────────────────────────────────────────────
        private static Label MakeLblKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(0, 0, 8, 0)
        };
        private static Label MakeLblVal(string text) => new Label
        {
            Text        = text ?? "—",
            Font        = new Font("Segoe UI", 12f),
            ForeColor   = Color.FromArgb(15, 31, 53),
            Dock        = DockStyle.Fill,
            TextAlign   = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        // ── Navigation & Logout ───────────────────────────────────────────────
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }
    }
}
