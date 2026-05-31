using PremiumLivingOPS.Views.Shared;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.InventoryControl
{
    partial class ViewRawMaterialForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // ── Form ───────────────────────────────────────────────────────────
            Name                = "ViewRawMaterialForm";
            Text                = "Inventory Control — View Raw Material";
            WindowState         = FormWindowState.Maximized;
            BackColor           = Color.FromArgb(240, 242, 245);
            Font                = new Font("Segoe UI", 11f);
            AutoScaleMode       = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7f, 15f);

            // ── Root panel ─────────────────────────────────────────────────────
            pnlRoot = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 242, 245)
            };

            // ── AppShell ───────────────────────────────────────────────────────
            _shell = new AppShell { Dock = DockStyle.Top };

            // ── Scrollable content area ────────────────────────────────────────
            pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(240, 242, 245),
                Padding    = new Padding(20, 16, 20, 16)
            };

            // ── Tab strip ──────────────────────────────────────────────────────
            pnlTabStrip = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.White
            };
            pnlTabStrip.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };

            btnTabProduct = new Button
            {
                Text      = "Products",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = 160,
                Height    = 44,
                Dock      = DockStyle.Left,
                Cursor    = Cursors.Hand
            };
            btnTabProduct.FlatAppearance.BorderSize = 0;

            btnTabRawMaterial = new Button
            {
                Text      = "Raw Materials",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = 160,
                Height    = 44,
                Dock      = DockStyle.Left,
                Cursor    = Cursors.Hand
            };
            btnTabRawMaterial.FlatAppearance.BorderSize = 0;

            pnlTabStrip.Controls.Add(btnTabRawMaterial);
            pnlTabStrip.Controls.Add(btnTabProduct);

            // ── KPI card ───────────────────────────────────────────────────────
            var (kpiOuter, kpiInner) = CardPanel.Create(outerHeight: 100);
            pnlKpi = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            kpiInner.Controls.Add(pnlKpi);

            // ── Search / filter card ───────────────────────────────────────────
            var (searchOuter, searchInner) = CardPanel.Create(outerHeight: 72);
            var searchFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(8, 8, 0, 0)
            };

            txtSearch = new TextBox
            {
                PlaceholderText = "Search material ID, name or category…",
                Width           = 300,
                Height          = 34,
                Font            = new Font("Segoe UI", 11f),
                Margin          = new Padding(0, 0, 10, 0)
            };

            var lblCat = new Label
            {
                Text      = "Category:",
                AutoSize  = true,
                Font      = new Font("Segoe UI", 11f),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin    = new Padding(0, 4, 6, 0)
            };

            cboCategory = new ComboBox
            {
                Width         = 170,
                Height        = 34,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f),
                Margin        = new Padding(0, 0, 10, 0)
            };

            var lblSt = new Label
            {
                Text      = "Status:",
                AutoSize  = true,
                Font      = new Font("Segoe UI", 11f),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin    = new Padding(0, 4, 6, 0)
            };

            cboStatus = new ComboBox
            {
                Width         = 170,
                Height        = 34,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f),
                Margin        = new Padding(0, 0, 10, 0)
            };
            cboStatus.Items.AddRange(new object[] { "All", "In Stock", "Low Stock", "Out of Stock" });
            cboStatus.SelectedIndex = 0;

            btnSearch = MakePrimaryBtn("Search");
            btnReset  = MakeOutlineBtn("Reset");
            btnSearch.Margin = new Padding(0, 0, 8, 0);

            searchFlow.Controls.Add(txtSearch);
            searchFlow.Controls.Add(lblCat);
            searchFlow.Controls.Add(cboCategory);
            searchFlow.Controls.Add(lblSt);
            searchFlow.Controls.Add(cboStatus);
            searchFlow.Controls.Add(btnSearch);
            searchFlow.Controls.Add(btnReset);
            searchInner.Controls.Add(searchFlow);

            // ── Table card (Fill) ───────────────────────────────────────────────
            var (tableOuter, tableInner) = CardPanel.CreateFill();

            dgvMaterials = new DataGridView
            {
                Dock                      = DockStyle.Fill,
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                RowHeadersVisible         = false,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor           = Color.White,
                BorderStyle               = BorderStyle.None,
                GridColor                 = Color.FromArgb(221, 227, 236),
                Font                      = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle           = DataGridViewCellBorderStyle.SingleHorizontal,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight       = 40,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };
            dgvMaterials.RowTemplate.Height = 44;

            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterialID",   HeaderText = "MATERIAL ID",   FillWeight = 14 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaterialName", HeaderText = "MATERIAL NAME", FillWeight = 28 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory",     HeaderText = "CATEGORY",      FillWeight = 16 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",         HeaderText = "UNIT",          FillWeight = 10 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnitCost",     HeaderText = "UNIT COST",     FillWeight = 12 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStockQty",     HeaderText = "STOCK QTY",     FillWeight = 10 });
            dgvMaterials.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",       HeaderText = "STATUS",        FillWeight = 10 });

            // Footer inside table card
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 56,
                BackColor = Color.White,
                Padding   = new Padding(12, 10, 12, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            btnViewDetail = MakePrimaryBtn("View Detail");
            btnViewDetail.Enabled = false;
            btnViewDetail.Dock    = DockStyle.Right;
            btnViewDetail.Width   = 140;
            pnlFooter.Controls.Add(btnViewDetail);

            tableInner.Controls.Add(dgvMaterials);
            tableInner.Controls.Add(pnlFooter);

            // ── Assemble pnlScroll ─────────────────────────────────────────────
            pnlScroll.Controls.Add(tableOuter);   // Fill — first
            pnlScroll.Controls.Add(searchOuter);  // Top
            pnlScroll.Controls.Add(kpiOuter);     // Top
            pnlScroll.Controls.Add(pnlTabStrip);  // Top

            // ── Assemble root ──────────────────────────────────────────────────
            pnlRoot.Controls.Add(pnlScroll);  // Fill
            pnlRoot.Controls.Add(_shell);     // Top

            Controls.Add(pnlRoot);
            ResumeLayout(false);
        }

        // ── Button factories ───────────────────────────────────────────────────
        private static Button MakePrimaryBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Height    = 34,
                AutoSize  = true,
                Padding   = new Padding(16, 0, 16, 0),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static Button MakeOutlineBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Height    = 34,
                AutoSize  = true,
                Padding   = new Padding(16, 0, 16, 0),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        // ── Field declarations ─────────────────────────────────────────────────
        private Panel            pnlRoot;
        private AppShell         _shell;
        private Panel            pnlScroll;
        private Panel            pnlTabStrip;
        private Button           btnTabProduct;
        private Button           btnTabRawMaterial;
        internal Panel           pnlKpi;
        private TextBox          txtSearch;
        private ComboBox         cboCategory;
        private ComboBox         cboStatus;
        private Button           btnSearch;
        private Button           btnReset;
        private DataGridView     dgvMaterials;
        private Button           btnViewDetail;
    }
}
