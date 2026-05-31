using PremiumLivingOPS.Views.Shared;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.InventoryControl
{
    partial class ViewProductForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ── Form ────────────────────────────────────────────────────────────────────
            SuspendLayout();
            Name            = "ViewProductForm";
            Text            = "Inventory Control — View Product";
            WindowState     = FormWindowState.Maximized;
            BackColor       = Color.FromArgb(240, 242, 245);
            Font            = new Font("Segoe UI", 11f);
            AutoScaleMode   = AutoScaleMode.Font;
            AutoScaleDimensions = new SizeF(7f, 15f);

            // ── Root panel ──────────────────────────────────────────────────────
            pnlRoot = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 242, 245),
                Padding   = new Padding(0)
            };

            // ── AppShell ─────────────────────────────────────────────────────────
            _shell = new AppShell { Dock = DockStyle.Top };

            // ── Scrollable content area ─────────────────────────────────────────
            pnlScroll = new Panel
            {
                Dock        = DockStyle.Fill,
                AutoScroll  = true,
                BackColor   = Color.FromArgb(240, 242, 245),
                Padding     = new Padding(20, 16, 20, 16)
            };

            // ── KPI card ─────────────────────────────────────────────────────────
            var kpiOuter = CardPanel.Create(paddingInner: 12);
            kpiOuter.Dock   = DockStyle.Top;
            kpiOuter.Height = 100;
            kpiOuter.Margin = new Padding(0, 0, 0, 12);
            pnlKpi = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            kpiOuter.Controls[0].Controls[0].Controls.Add(pnlKpi);

            // ── Search / filter card ──────────────────────────────────────────
            var searchOuter = CardPanel.Create(paddingInner: 14);
            searchOuter.Dock   = DockStyle.Top;
            searchOuter.Height = 72;
            searchOuter.Margin = new Padding(0, 0, 0, 12);
            var searchFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0)
            };

            // Keyword
            txtSearch = new TextBox
            {
                PlaceholderText = "Search item ID, name or category…",
                Width = 320, Height = 36,
                Font  = new Font("Segoe UI", 11f),
                Margin = new Padding(0, 0, 10, 0)
            };

            // Category
            var lblCat = new Label { Text = "Category:", AutoSize = true, Font = new Font("Segoe UI", 11f), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 6, 6, 0) };
            cboCategory = new ComboBox
            {
                Width         = 180, Height = 36,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f),
                Margin        = new Padding(0, 0, 10, 0)
            };

            // Status
            var lblSt = new Label { Text = "Status:", AutoSize = true, Font = new Font("Segoe UI", 11f), TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 6, 6, 0) };
            cboStatus = new ComboBox
            {
                Width         = 180, Height = 36,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 11f),
                Margin        = new Padding(0, 0, 10, 0)
            };
            cboStatus.Items.AddRange(new object[] { "All", "In Stock", "Low Stock", "Out of Stock" });
            cboStatus.SelectedIndex = 0;

            // Buttons
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
            searchOuter.Controls[0].Controls[0].Controls.Add(searchFlow);

            // ── Table card (fill) ────────────────────────────────────────────
            var tableOuter = CardPanel.CreateFill();
            tableOuter.Dock = DockStyle.Fill;

            // DataGridView
            dgvProducts = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                Font                  = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 44 },
                ColumnHeadersHeight   = 40,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor            = Color.White,
                    ForeColor            = Color.FromArgb(15, 31, 53),
                    SelectionBackColor   = Color.FromArgb(219, 234, 254),
                    SelectionForeColor   = Color.FromArgb(15, 31, 53),
                    Padding              = new Padding(12, 6, 12, 6)
                }
            };
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",    HeaderText = "ITEM ID",      FillWeight = 14 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemName",  HeaderText = "ITEM NAME",    FillWeight = 34 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory",  HeaderText = "CATEGORY",     FillWeight = 18 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice",     HeaderText = "SALES PRICE",  FillWeight = 14 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStockQty",  HeaderText = "STOCK QTY",    FillWeight = 10 });
            dgvProducts.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus",    HeaderText = "STATUS",       FillWeight = 10 });

            // Action button footer
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 56,
                BackColor = Color.White,
                Padding   = new Padding(12, 10, 12, 10)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1))
                    e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };
            btnViewDetail = MakePrimaryBtn("View Detail");
            btnViewDetail.Enabled = false;
            btnViewDetail.Dock    = DockStyle.Right;
            btnViewDetail.Width   = 140;
            pnlFooter.Controls.Add(btnViewDetail);

            var innermost = tableOuter.Controls[0].Controls[0];
            innermost.Controls.Add(dgvProducts);
            innermost.Controls.Add(pnlFooter);

            // ── Tab strip ─────────────────────────────────────────────────────────
            pnlTabStrip = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.White,
                Margin    = new Padding(0, 0, 0, 12)
            };
            pnlTabStrip.Paint += (s, e) =>
            {
                using (var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1))
                    e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };

            btnTabProduct = new Button
            {
                Text      = "Products",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = 160, Height = 44,
                Dock      = DockStyle.Left,
                Cursor    = Cursors.Hand
            };
            btnTabProduct.FlatAppearance.BorderSize  = 0;
            btnTabProduct.FlatAppearance.BorderColor = Color.Transparent;

            btnTabRawMaterial = new Button
            {
                Text      = "Raw Materials",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(98, 112, 135),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = 160, Height = 44,
                Dock      = DockStyle.Left,
                Cursor    = Cursors.Hand
            };
            btnTabRawMaterial.FlatAppearance.BorderSize  = 0;
            btnTabRawMaterial.FlatAppearance.BorderColor = Color.Transparent;

            pnlTabStrip.Controls.Add(btnTabRawMaterial);
            pnlTabStrip.Controls.Add(btnTabProduct);

            // ── Assemble scroll content (bottom-up for DockStyle.Top stacking)
            pnlScroll.Controls.Add(tableOuter);
            pnlScroll.Controls.Add(searchOuter);
            pnlScroll.Controls.Add(kpiOuter);
            pnlScroll.Controls.Add(pnlTabStrip);

            // ── Assemble root
            pnlRoot.Controls.Add(pnlScroll);
            pnlRoot.Controls.Add(_shell);
            Controls.Add(pnlRoot);

            ResumeLayout(false);
        }

        // ── Button factories (same as OrderProcessing forms) ─────────────────────
        private static Button MakePrimaryBtn(string text)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Height    = 36,
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
                Height    = 36,
                AutoSize  = true,
                Padding   = new Padding(16, 0, 16, 0),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        // ── Fields ─────────────────────────────────────────────────────────────────
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
        private DataGridView     dgvProducts;
        private Button           btnViewDetail;
    }
}
