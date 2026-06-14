using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Create New Quotation — popup dialog (MVC View layer).
    ///
    /// Layout (mirrors ModifyQuotationDialog):
    ///   – pnlHeader      Top  80   — dark navy, title + "Pending" badge (260px)
    ///   – pnlQuoteInfo   Top  220  — 4-col editable header fields
    ///   – pnlLineLabel   Top  50   — "QUOTATION ITEMS" bar + [＋ Add Item] button
    ///   – dgvItems       Fill      — ITEM ID | PRODUCT | QTY | UNIT PRICE | SUBTOTAL | DELETE
    ///   – pnlTotalRow    Bottom 50 — live Total Amount
    ///   – pnlFooter      Bottom 80 — [✔ New]  [Cancel]  (210×60 each)
    ///
    /// Customer ComboBox displays "CustomerName  (CustomerID)".
    /// Items grid Delete column: inline CellPainting red button (same as Modify).
    /// Add Item dialog: 1350×600, left 55% search+listbox, right 45% qty/price/subtotal.
    /// </summary>
    public class CreateQuotationDialog : Form
    {
        private readonly OrderProcessingController _ctrl;
        private readonly List<CustomerEntity>      _customers;
        private readonly List<ProductLookup>       _products;
        private readonly string                    _quotationId;
        private readonly string                    _salesStaffName;
        private readonly string                    _salesStaffId;

        private readonly List<QuotationItemEntity> _items = new List<QuotationItemEntity>();

        private DataGridView  _dgvItems;
        private Label         _lblTotal;

        private ComboBox       _cboCustomer;
        private DateTimePicker _dtpExpiry;
        private NumericUpDown  _nudDeposit;
        private TextBox        _txtLeadTime;
        private TextBox        _txtTnC;

        public CreateQuotationDialog(OrderProcessingController ctrl)
        {
            _ctrl = ctrl ?? throw new ArgumentNullException(nameof(ctrl));
            var vm = _ctrl.GetCreateQuotationVM();
            _customers      = vm.Customers   ?? new List<CustomerEntity>();
            _products       = vm.Products    ?? new List<ProductLookup>();
            _quotationId    = vm.NextQuotationId;
            _salesStaffName = vm.SalesStaffName;
            _salesStaffId   = vm.SalesStaffId;
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            Text            = $"Create New Quotation  —  {_quotationId}";
            Size            = new Size(2500, 1200);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 13f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // ── Header (identical to ModifyQuotationDialog)
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Create New Quotation  —  {_quotationId}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = "Pending",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14),
                BackColor = Color.FromArgb(254, 243, 199),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Quote Info (editable fields, mirrors Modify read-only layout)
            var pnlQuoteInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 14, 28, 8), BackColor = Color.White
            };
            pnlQuoteInfo.Paint += PaintBottomBorder;
            var tblQ = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblQ.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblQ.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblQ.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblQ.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 3; r++)
                tblQ.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            // Row 0: Quotation ID (read-only) | Sales Staff (read-only)
            tblQ.Controls.Add(MakeLabelKey("Quotation ID:"),         0, 0);
            tblQ.Controls.Add(MakeLabelVal(_quotationId),            1, 0);
            tblQ.Controls.Add(MakeLabelKey("Sales Staff:"),          2, 0);
            tblQ.Controls.Add(MakeLabelVal(_salesStaffName ?? "—"),  3, 0);

            // Row 1: Customer (ComboBox shows Name + ID) | Expiry Date
            tblQ.Controls.Add(MakeLabelKey("Customer *:"), 0, 1);
            _cboCustomer = new ComboBox
            {
                Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), FlatStyle = FlatStyle.Flat
            };
            foreach (var c in _customers)
                _cboCustomer.Items.Add(new CustomerComboItem(c));
            tblQ.Controls.Add(_cboCustomer, 1, 1);
            tblQ.Controls.Add(MakeLabelKey("Expiry Date *:"), 2, 1);
            _dtpExpiry = new DateTimePicker
            {
                Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 12f),
                MinDate = DateTime.Today.AddDays(1),
                Value   = DateTime.Today.AddDays(30)
            };
            tblQ.Controls.Add(_dtpExpiry, 3, 1);

            // Row 2: Deposit Required | Lead Time
            tblQ.Controls.Add(MakeLabelKey("Deposit Required:"), 0, 2);
            _nudDeposit = new NumericUpDown
            {
                Dock = DockStyle.Fill, Minimum = 0, Maximum = 9999999,
                DecimalPlaces = 2, Font = new Font("Segoe UI", 12f), ThousandsSeparator = true
            };
            tblQ.Controls.Add(_nudDeposit, 1, 2);
            tblQ.Controls.Add(MakeLabelKey("Lead Time:"), 2, 2);
            _txtLeadTime = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                PlaceholderText = "e.g. 4–6 weeks"
            };
            tblQ.Controls.Add(_txtLeadTime, 3, 2);
            pnlQuoteInfo.Controls.Add(tblQ);

            // ── QUOTATION ITEMS bar (identical to ModifyQuotationDialog)
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(239, 246, 255), Padding = new Padding(28, 0, 16, 0)
            };
            pnlLineLabel.Paint += PaintBottomBorder;
            var tblLineBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblLineBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblLineBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
            tblLineBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblLineBar.Controls.Add(new Label
            {
                Text      = "QUOTATION ITEMS",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            var btnAddItem = new Button
            {
                Text      = "\uFF0B  Add Item",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Cursor = Cursors.Hand
            };
            btnAddItem.FlatAppearance.BorderSize = 0;
            btnAddItem.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            btnAddItem.Click += BtnAddItem_Click;
            tblLineBar.Controls.Add(btnAddItem, 1, 0);
            pnlLineLabel.Controls.Add(tblLineBar);

            // ── Items DataGridView (identical column set to ModifyQuotationDialog)
            _dgvItems = new DataGridView
            {
                AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                ReadOnly = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 46 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(239, 246, 255),
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
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItemID",    HeaderText = "ITEM ID",    FillWeight = 18, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cProduct",   HeaderText = "PRODUCT",    FillWeight = 32, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",       HeaderText = "QTY",        FillWeight = 10, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUnitPrice", HeaderText = "UNIT PRICE", FillWeight = 18, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSubtotal",  HeaderText = "SUBTOTAL",   FillWeight = 18, ReadOnly = true });
            var colDelete = new DataGridViewButtonColumn
            {
                Name = "cDelete", HeaderText = "", Text = "\u2715  Delete",
                UseColumnTextForButtonValue = true, FillWeight = 12, FlatStyle = FlatStyle.Flat
            };
            _dgvItems.Columns.Add(colDelete);
            _dgvItems.CellClick    += DgvItems_CellClick;
            _dgvItems.CellPainting += DgvItems_CellPainting;
            RebuildGrid();

            // ── Total row
            var pnlTotalRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(0, 0, 28, 0)
            };
            _lblTotal = new Label
            {
                Text      = FormatTotal(),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoSize = false
            };
            pnlTotalRow.Controls.Add(_lblTotal);

            // ── Footer  [✔ New 210×60]  [Cancel 210×60]
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            pnlFooter.Paint += PaintTopBorder;
            var btnNew = new Button
            {
                Text      = "\u2714  New",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat, Size = new Size(210, 60),
                Dock      = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(210, 60),
                Dock      = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnNew.Click    += BtnNew_Click;
            btnCancel.Click += (o, ev) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(btnNew);
            pnlFooter.Controls.Add(btnCancel);

            // ── Assemble (DockStyle.Top stacks top-down; Fill goes last in Controls.Add)
            Controls.Add(_dgvItems);
            Controls.Add(pnlTotalRow);
            Controls.Add(pnlLineLabel);
            Controls.Add(pnlQuoteInfo);
            Controls.Add(pnlHeader);
            Controls.Add(pnlFooter);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Grid helpers
        // ──────────────────────────────────────────────────────────────────
        private void RebuildGrid()
        {
            _dgvItems.Rows.Clear();
            foreach (var item in _items)
                _dgvItems.Rows.Add(
                    item.ItemID, item.ProductName, item.Quantity,
                    $"HK$ {item.UnitPrice:N2}", $"HK$ {item.Subtotal:N2}");
            RefreshTotal();
        }

        private void RefreshTotal()
        {
            if (_lblTotal != null) _lblTotal.Text = FormatTotal();
        }

        private string FormatTotal()
        {
            double total = 0;
            foreach (var i in _items) total += i.Subtotal;
            return $"Total Amount:   HK$ {total:N2}";
        }

        private void DgvItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_dgvItems.Columns[e.ColumnIndex].Name != "cDelete") return;
            if (MessageBox.Show("Remove this item from the quotation?", "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _items.RemoveAt(e.RowIndex);
                RebuildGrid();
            }
        }

        private void DgvItems_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_dgvItems.Columns[e.ColumnIndex].Name != "cDelete") return;
            e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);
            using var brush   = new SolidBrush(Color.FromArgb(254, 226, 226));
            e.Graphics.FillRectangle(brush, e.CellBounds);
            using var font    = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var fgBrush = new SolidBrush(Color.FromArgb(153, 27, 27));
            var sf = new System.Drawing.StringFormat
            {
                Alignment     = System.Drawing.StringAlignment.Center,
                LineAlignment = System.Drawing.StringAlignment.Center
            };
            e.Graphics.DrawString("\u2715  Delete", font, fgBrush, e.CellBounds, sf);
            e.Handled = true;
        }

        // ──────────────────────────────────────────────────────────────────
        //  Add Item dialog — 1350×600 (mirrors ModifyQuotationDialog exactly)
        //  Left 55%: search TextBox + filtered ListBox (ItemID — ItemName)
        //  Right 45%: Qty / Unit Price / Subtotal
        // ──────────────────────────────────────────────────────────────────
        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            // Determine CustomerID from selected ComboBox item for product lookup
            string custId = (_cboCustomer.SelectedItem as CustomerComboItem)?.CustomerID ?? string.Empty;
            var availableItems = _ctrl.GetAvailableItemsForQuotation(custId)
                                 ?? new List<ProductLookup>();

            using var addDlg = new Form
            {
                Text            = "Add Quotation Item",
                Size            = new Size(1350, 600),
                StartPosition   = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 12f),
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // Header
            var pnlH = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(19, 35, 61) };
            pnlH.Controls.Add(new Label
            {
                Text      = "Add Quotation Item",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0), AutoSize = false
            });

            // Footer
            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 70,
                BackColor = Color.White, Padding = new Padding(0, 10, 24, 10)
            };
            pnlFoot.Paint += PaintTopBorder;
            var btnAdd = new Button
            {
                Text      = "\u2714  Add", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat, Size = new Size(160, 50),
                Dock      = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            var btnCancelAdd = new Button
            {
                Text      = "Cancel", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(120, 50),
                Dock      = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnCancelAdd.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancelAdd.FlatAppearance.BorderSize         = 1;
            btnCancelAdd.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            // Body — left 55% | right 45%
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 14, 24, 8),
                BackColor = Color.White
            };
            var tblBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tblBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            // Left: caption + search + listbox
            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 14, 0), BackColor = Color.Transparent };
            var lblItemCaption = new Label
            {
                Text      = "Item",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Top, Height = 40,
                TextAlign = ContentAlignment.BottomLeft, AutoEllipsis = false
            };
            var txtSearch = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                Dock            = DockStyle.Top, Height = 36,
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "\uD83D\uDD0E  Search by ID or name..."
            };
            var lstItems = new ListBox
            {
                Font = new Font("Segoe UI", 11f),
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false
            };

            var productItems = availableItems
                .Select(p => new ProductComboItem(p.ItemName, p.ItemID, p.SalesPrice))
                .ToList();

            void FilterList(string keyword)
            {
                lstItems.BeginUpdate();
                lstItems.Items.Clear();
                string kw = (keyword ?? "").ToLowerInvariant().Trim();
                foreach (var pi in productItems)
                    if (string.IsNullOrEmpty(kw)
                        || pi.ItemID.ToLowerInvariant().Contains(kw)
                        || pi.ItemName.ToLowerInvariant().Contains(kw))
                        lstItems.Items.Add(pi);
                lstItems.EndUpdate();
            }
            FilterList("");
            txtSearch.TextChanged += (s, ev) => FilterList(txtSearch.Text);

            pnlLeft.Controls.Add(lstItems);
            pnlLeft.Controls.Add(txtSearch);
            pnlLeft.Controls.Add(lblItemCaption);

            // Right: label col Absolute 170px / value col Percent 100f / row height 76f
            var pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0), BackColor = Color.Transparent };
            var tblRight = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
            tblRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));
            tblRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));

            var numQty = new NumericUpDown
            {
                Minimum = 1, Maximum = 9999, Value = 1,
                Font = new Font("Segoe UI", 13f), Dock = DockStyle.Fill
            };
            var lblUnitPriceVal = new Label
            {
                Text      = "HK$ 0.00", Font = new Font("Segoe UI", 13f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = false
            };
            var lblSubtotalVal = new Label
            {
                Text      = "HK$ 0.00", Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(5, 150, 105),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = false
            };

            Action recompute = () =>
            {
                var sel = lstItems.SelectedItem as ProductComboItem;
                if (sel == null)
                { lblUnitPriceVal.Text = "HK$ 0.00"; lblSubtotalVal.Text = "HK$ 0.00"; return; }
                double subtotal = sel.UnitPrice * (double)numQty.Value;
                lblUnitPriceVal.Text = $"HK$ {sel.UnitPrice:N2}";
                lblSubtotalVal.Text  = $"HK$ {subtotal:N2}";
            };
            lstItems.SelectedIndexChanged += (s, ev) => recompute();
            numQty.ValueChanged           += (s, ev) => recompute();

            tblRight.Controls.Add(MakeFieldLabel("Quantity *"), 0, 0);
            tblRight.Controls.Add(numQty,                       1, 0);
            tblRight.Controls.Add(MakeFieldLabel("Unit Price"),  0, 1);
            tblRight.Controls.Add(lblUnitPriceVal,               1, 1);
            tblRight.Controls.Add(MakeFieldLabel("Subtotal"),    0, 2);
            tblRight.Controls.Add(lblSubtotalVal,                1, 2);

            pnlRight.Controls.Add(tblRight);
            tblBody.Controls.Add(pnlLeft,  0, 0);
            tblBody.Controls.Add(pnlRight, 1, 0);
            pnlBody.Controls.Add(tblBody);

            btnAdd.Click += (s, ev) =>
            {
                var sel = lstItems.SelectedItem as ProductComboItem;
                if (sel == null)
                {
                    MessageBox.Show("Please select an item from the list.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int addQty   = (int)numQty.Value;
                var existing = _items.FirstOrDefault(i => i.ItemID == sel.ItemID);
                if (existing != null)
                    existing.Quantity += addQty;
                else
                    _items.Add(new QuotationItemEntity
                    {
                        QuotationID = _quotationId, ItemID = sel.ItemID,
                        ProductName = sel.ItemName,  Quantity = addQty,
                        Unit = "", UnitPrice = sel.UnitPrice, DiscountPercent = 0
                    });
                RebuildGrid();
                addDlg.DialogResult = DialogResult.OK;
                addDlg.Close();
            };
            btnCancelAdd.Click += (s, ev) => { addDlg.DialogResult = DialogResult.Cancel; addDlg.Close(); };

            pnlFoot.Controls.Add(btnAdd);
            pnlFoot.Controls.Add(btnCancelAdd);
            addDlg.Controls.Add(pnlBody);
            addDlg.Controls.Add(pnlH);
            addDlg.Controls.Add(pnlFoot);
            addDlg.ShowDialog(this);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Save
        // ──────────────────────────────────────────────────────────────────
        private void BtnNew_Click(object sender, EventArgs e)
        {
            if (_cboCustomer.SelectedItem == null)
            {
                MessageBox.Show("Please select a Customer.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_items.Count == 0)
            {
                MessageBox.Show("Please add at least one item to the quotation.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var ci       = (CustomerComboItem)_cboCustomer.SelectedItem;
            double total = _items.Sum(i => i.Subtotal);

            var quotation = new QuotationEntity
            {
                QuotationID       = _quotationId,
                CustomerID        = ci.CustomerID,
                CustomerName      = ci.CustomerName,
                ExpiryDate        = _dtpExpiry.Value.Date,
                TotalAmount       = total,
                DepositRequired   = (double)_nudDeposit.Value,
                LeadTimeEstimated = _txtLeadTime.Text.Trim(),
                TermsandCondition = _txtTnC?.Text.Trim() ?? string.Empty,
                QuotationStatus   = "Pending"
            };

            try
            {
                bool ok = _ctrl.SaveNewQuotation(quotation, _items, _salesStaffId);
                if (ok)
                {
                    MessageBox.Show(
                        $"Quotation {_quotationId} has been created successfully.",
                        "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                    MessageBox.Show("Failed to save the quotation. Please try again.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  Paint helpers (identical to ModifyQuotationDialog)
        // ──────────────────────────────────────────────────────────────────
        private static void PaintBottomBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }
        private static void PaintTopBorder(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Label factories (identical naming to ModifyQuotationDialog)
        // ──────────────────────────────────────────────────────────────────
        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
        };
        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text ?? "—", Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };
        private static Label MakeFieldLabel(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false, Padding = new Padding(0, 0, 8, 0)
        };

        private void InitializeComponent() { SuspendLayout(); ResumeLayout(false); }

        // ──────────────────────────────────────────────────────────────────
        //  Inner classes
        // ──────────────────────────────────────────────────────────────────

        /// <summary>ComboBox item that renders "CustomerName  (CustomerID)".</summary>
        private class CustomerComboItem
        {
            public string CustomerID   { get; }
            public string CustomerName { get; }
            public CustomerComboItem(CustomerEntity c)
            { CustomerID = c.CustomerID; CustomerName = c.CustomerName; }
            public override string ToString() => $"{CustomerName}  ({CustomerID})";
        }

        /// <summary>ListBox item in the Add Item sub-dialog.</summary>
        private class ProductComboItem
        {
            public string ItemName  { get; }
            public string ItemID    { get; }
            public double UnitPrice { get; }
            public ProductComboItem(string name, string id, double price)
            { ItemName = name; ItemID = id; UnitPrice = price; }
            public override string ToString() => $"{ItemID}  \u2014  {ItemName}";
        }
    }
}
