using System;
using System.Drawing;
using System.Windows.Forms;
using PremiumLivingOPS.Views.Shared;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    partial class CreateNewQuotationForm
    {
        private System.ComponentModel.IContainer components = null;

        private TextBox       txtQuotationId;
        private TextBox       txtSalesStaff;
        private ComboBox      cboCustomer;
        private ComboBox      cboStatus;
        private DateTimePicker dtpIssuedDate;
        private DateTimePicker dtpExpiryDate;
        private TextBox       txtDeposit;
        private TextBox       txtLeadTime;
        private TextBox       txtTerms;
        private TextBox       txtNotes;

        private ComboBox      cboProduct;
        private TextBox       txtQty;
        private TextBox       txtUnit;
        private TextBox       txtUnitPrice;
        private TextBox       txtDiscount;
        private TextBox       txtItemNote;
        private Button        btnAddLine;
        private Button        btnRemoveLine;

        private DataGridView  dgvLines;
        private Label         lblTotal;

        private Button        btnSave;
        private Button        btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text            = "Create New Quotation";
            this.Size            = new Size(1100, 820);
            this.MinimumSize     = new Size(900, 720);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Palette.BgPage;
            this.Font            = new Font("Segoe UI", 12f);
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // ── Outer table: header card | lines card | footer
            var tblOuter = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                Padding     = new Padding(18, 14, 18, 12),
                BackColor   = Color.Transparent
            };
            tblOuter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 310f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblOuter.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));

            // ════════════════════════════════════════════
            // HEADER CARD
            // ════════════════════════════════════════════
            var tblHeader = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 4,
                ColumnCount = 4,
                BackColor   = Color.Transparent,
                Padding     = new Padding(12, 8, 12, 8)
            };
            for (int i = 0; i < 4; i++)
                tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); // title row
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent,  33f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent,  33f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent,  34f));

            var lblCardTitle = new Label
            {
                Text      = "Quotation Header",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tblHeader.Controls.Add(lblCardTitle, 0, 0);
            tblHeader.SetColumnSpan(lblCardTitle, 4);

            // Row 1: QuotationID | Customer | Status | SalesStaff
            txtQuotationId = new TextBox { ReadOnly = true, BackColor = Color.FromArgb(240, 240, 240), Dock = DockStyle.Fill };
            cboCustomer    = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            cboStatus      = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
            txtSalesStaff  = new TextBox { ReadOnly = true, BackColor = Color.FromArgb(240, 240, 240), Dock = DockStyle.Fill };

            tblHeader.Controls.Add(MakeField("Quotation ID",  txtQuotationId), 0, 1);
            tblHeader.Controls.Add(MakeField("Customer",      cboCustomer),    1, 1);
            tblHeader.Controls.Add(MakeField("Status",        cboStatus),      2, 1);
            tblHeader.Controls.Add(MakeField("Sales Staff",   txtSalesStaff),  3, 1);

            // Row 2: IssuedDate | ExpiryDate | Deposit | LeadTime
            dtpIssuedDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
            dtpExpiryDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Dock = DockStyle.Fill };
            txtDeposit    = new TextBox { Dock = DockStyle.Fill };
            txtLeadTime   = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "e.g. 14 days" };

            tblHeader.Controls.Add(MakeField("Issued Date",    dtpIssuedDate), 0, 2);
            tblHeader.Controls.Add(MakeField("Expiry Date",    dtpExpiryDate), 1, 2);
            tblHeader.Controls.Add(MakeField("Deposit (HK$)", txtDeposit),     2, 2);
            tblHeader.Controls.Add(MakeField("Lead Time",      txtLeadTime),   3, 2);

            // Row 3: Terms (span 2) | Notes (span 2)
            txtTerms = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
            txtNotes = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };

            var termsCell = MakeField("Terms & Conditions", txtTerms);
            var notesCell = MakeField("Notes",              txtNotes);
            tblHeader.Controls.Add(termsCell, 0, 3);
            tblHeader.SetColumnSpan(termsCell, 2);
            tblHeader.Controls.Add(notesCell, 2, 3);
            tblHeader.SetColumnSpan(notesCell, 2);

            var headerCards = CardPanel.Create(outerHeight: 310);
            headerCards.Item2.Controls.Add(tblHeader);

            // ════════════════════════════════════════════
            // LINE ITEMS CARD
            // ════════════════════════════════════════════
            var tblLines = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                BackColor   = Color.Transparent,
                Padding     = new Padding(12, 8, 12, 8)
            };
            tblLines.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblLines.RowStyles.Add(new RowStyle(SizeType.Absolute,  36f));
            tblLines.RowStyles.Add(new RowStyle(SizeType.Absolute,  72f));
            tblLines.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lblLinesTitle = new Label
            {
                Text      = "Line Items",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Palette.TextMain,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tblLines.Controls.Add(lblLinesTitle, 0, 0);

            // Line entry row
            cboProduct  = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 280 };
            txtQty      = new TextBox  { Width = 60,  PlaceholderText = "Qty" };
            txtUnit     = new TextBox  { Width = 70,  PlaceholderText = "Unit" };
            txtUnitPrice= new TextBox  { Width = 90,  PlaceholderText = "Price" };
            txtDiscount = new TextBox  { Width = 60,  PlaceholderText = "Disc%" };
            txtItemNote = new TextBox  { Width = 140, PlaceholderText = "Note" };
            btnAddLine  = MakePrimaryBtn("+ Add",    55, 38);
            btnRemoveLine= MakeOutlineBtn("Remove",  75, 38);

            cboProduct.SelectedIndexChanged += cboProduct_SelectedIndexChanged;
            btnAddLine.Click    += btnAddLine_Click;
            btnRemoveLine.Click += btnRemoveLine_Click;

            var pnlEntry = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = false,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 4, 0, 4)
            };
            foreach (Control c in new Control[]
            {
                cboProduct, SpaceW(6), txtQty, SpaceW(4), txtUnit, SpaceW(4),
                txtUnitPrice, SpaceW(4), txtDiscount, SpaceW(4), txtItemNote,
                SpaceW(8), btnAddLine, SpaceW(4), btnRemoveLine
            })
                pnlEntry.Controls.Add(c);

            // Grid
            dgvLines = new DataGridView
            {
                ReadOnly                  = true,
                AllowUserToAddRows        = false,
                AllowUserToDeleteRows     = false,
                RowHeadersVisible         = false,
                SelectionMode             = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect               = false,
                BackgroundColor           = Color.White,
                BorderStyle               = BorderStyle.None,
                Dock                      = DockStyle.Fill,
                Font                      = new Font("Segoe UI", 11f),
                AutoSizeColumnsMode       = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight       = 38,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Palette.TextMain
                },
                RowTemplate = { Height = 38 }
            };
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colItemID",    HeaderText = "ITEM ID",    FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProduct",   HeaderText = "PRODUCT",   FillWeight = 22 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",       HeaderText = "QTY",       FillWeight = 6  });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUnit",      HeaderText = "UNIT",      FillWeight = 7  });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice",     HeaderText = "UNIT PRICE",FillWeight = 10 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDisc",      HeaderText = "DISC%",     FillWeight = 7  });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSubtotal",  HeaderText = "SUBTOTAL",  FillWeight = 12 });
            dgvLines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNote",      HeaderText = "NOTE",      FillWeight = 26 });

            lblTotal = new Label
            {
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Palette.Primary,
                Dock      = DockStyle.Bottom,
                Height    = 32,
                TextAlign = ContentAlignment.MiddleRight,
                Text      = "Total:  HK$ 0.00"
            };

            var pnlGridWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlGridWrap.Controls.Add(dgvLines);
            pnlGridWrap.Controls.Add(lblTotal);

            tblLines.Controls.Add(pnlEntry,   0, 1);
            tblLines.Controls.Add(pnlGridWrap,0, 2);

            var linesCards = CardPanel.CreateFill();
            linesCards.Item2.Controls.Add(tblLines);

            // ════════════════════════════════════════════
            // FOOTER BUTTONS
            // ════════════════════════════════════════════
            btnSave   = MakePrimaryBtn("\u2713  Save Quotation", 200, 48);
            btnCancel = MakeOutlineBtn("Cancel", 110, 48);
            btnSave.BackColor   = Color.FromArgb(5, 150, 105);
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            btnSave.Click   += btnSave_Click;
            btnCancel.Click += btnCancel_Click;

            var pnlFooter = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Resize += (s, ev) =>
            {
                int top = Math.Max(0, (pnlFooter.Height - 48) / 2);
                btnSave.Location   = new Point(pnlFooter.Width - 330, top);
                btnCancel.Location = new Point(pnlFooter.Width - 120, top);
            };

            // ── Assemble
            tblOuter.Controls.Add(headerCards.Item1, 0, 0);
            tblOuter.Controls.Add(linesCards.Item1,  0, 1);
            tblOuter.Controls.Add(pnlFooter,         0, 2);

            this.Controls.Add(tblOuter);
            this.ResumeLayout(false);
        }

        // ── Helpers

        private static Panel MakeField(string caption, Control ctrl)
        {
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                RowCount        = 2,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0, 0, 8, 0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tlp.Controls.Add(new Label
            {
                Text      = caption,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 0);
            ctrl.Dock = DockStyle.Fill;
            tlp.Controls.Add(ctrl, 0, 1);
            return tlp;
        }

        private static Button MakePrimaryBtn(string text, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Palette.Primary,
                FlatStyle = FlatStyle.Flat,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize        = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            return b;
        }

        private static Button MakeOutlineBtn(string text, int w, int h)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Palette.TextMain,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = w,
                Height    = h,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor        = Palette.BorderColor;
            b.FlatAppearance.BorderSize         = 1;
            b.FlatAppearance.MouseOverBackColor = Palette.BgPage;
            return b;
        }

        private static Panel SpaceW(int px)
            => new Panel { Width = px, Height = 1, BackColor = Color.Transparent };
    }
}
