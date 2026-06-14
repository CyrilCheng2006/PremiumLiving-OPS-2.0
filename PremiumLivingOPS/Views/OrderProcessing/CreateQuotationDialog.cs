using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
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
    ///   – pnlHeader      Top  80   — dark navy, title + “Pending” badge (260px)
    ///   – pnlQuoteInfo   Top  220  — 4-col header fields
    ///   – pnlLineLabel   Top  50   — “QUOTATION ITEMS” bar + [＋ Add Item] button
    ///   – dgvItems       Fill      — ITEM ID | PRODUCT | QTY | UNIT PRICE | SUBTOTAL | DELETE
    ///   – pnlTotalRow    Bottom 50 — live Total Amount
    ///   – pnlFooter      Bottom 80 — [✔ Create 210×60]  [Cancel 210×60]
    ///
    /// Customer field: Label showing picked value + [Pick…] button
    ///   → opens SearchPickerDialog (“CustomerID  –  CustomerName”), same as CreateOrderForm.
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

        // Customer picker state
        private string _selectedCustomerId   = "";
        private string _selectedCustomerName = "";
        private Label  _lblCustomerPicked;

        private DateTimePicker _dtpExpiry;
        private NumericUpDown  _nudDeposit;
        private TextBox        _txtLeadTime;

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

            // ── Header
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

            // ── Quote Info (editable fields)
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
            tblQ.Controls.Add(MakeLabelKey("Quotation ID:"),        0, 0);
            tblQ.Controls.Add(MakeLabelVal(_quotationId),           1, 0);
            tblQ.Controls.Add(MakeLabelKey("Sales Staff:"),         2, 0);
            tblQ.Controls.Add(MakeLabelVal(_salesStaffName ?? "—"), 3, 0);

            // Row 1: Customer (picker) | Expiry Date
            tblQ.Controls.Add(MakeLabelKey("Customer *:"),    0, 1);
            tblQ.Controls.Add(BuildCustomerPickerCell(),       1, 1);
            tblQ.Controls.Add(MakeLabelKey("Expiry Date *:"), 2, 1);
            _dtpExpiry = new DateTimePicker
            {
                Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 12f),
                MinDate = DateTime.Today.AddDays(1),
                Value   = DateTime.Today.AddDays(30)
            };
            tblQ.Controls.Add(_dtpExpiry, 3, 1);

            // Row 2: Deposit Required (optional, nullable) | Lead Time
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

            // ── QUOTATION ITEMS bar
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

            // ── Items DataGridView
            // Columns: ITEM ID | PRODUCT | QTY | UNIT PRICE | SUBTOTAL | DELETE
            // Unit and Discount omitted — no backing in schema.sql.
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
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cProduct",   HeaderText = "PRODUCT",    FillWeight = 36, ReadOnly = true });
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

            // ── Footer  [✔ Create 210×60]  [Cancel 210×60]
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            pnlFooter.Paint += PaintTopBorder;
            var btnCreate = new Button
            {
                Text      = "\u2714  Create",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right, Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCreate.Click += BtnCreate_Click;
            btnCancel.Click += (o, ev) => { DialogResult = DialogResult.Cancel; Close(); };
            pnlFooter.Controls.Add(btnCreate);
            pnlFooter.Controls.Add(btnCancel);

            // ── Assemble
            Controls.Add(_dgvItems);
            Controls.Add(pnlTotalRow);
            Controls.Add(pnlLineLabel);
            Controls.Add(pnlQuoteInfo);
            Controls.Add(pnlHeader);
            Controls.Add(pnlFooter);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Customer picker cell  (Label + [Pick…] button in a TableLayoutPanel)
        // ──────────────────────────────────────────────────────────────────────
        private Control BuildCustomerPickerCell()
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _lblCustomerPicked = new Label
            {
                Text      = "(None selected)",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };

            var btnPick = new Button
            {
                Text      = "Pick\u2026",
                Font      = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Fill,
                Cursor    = Cursors.Hand
            };
            btnPick.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnPick.FlatAppearance.BorderSize         = 1;
            btnPick.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnPick.Click += BtnPickCustomer_Click;

            tbl.Controls.Add(_lblCustomerPicked, 0, 0);
            tbl.Controls.Add(btnPick,            1, 0);
            return tbl;
        }

        // ── Customer picker: opens SearchPickerDialog (mirrors CreateOrderForm) ──
        private void BtnPickCustomer_Click(object sender, EventArgs e)
        {
            var items = _customers
                .Select(c => new SearchPickerDialog.PickerItem
                {
                    Id      = c.CustomerID,
                    Display = $"{c.CustomerID}  –  {c.CustomerName}"
                }).ToList();

            using var dlg = new SearchPickerDialog("Select Customer", items);
            if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedItem == null) return;

            _selectedCustomerId   = dlg.SelectedItem.Id;
            _selectedCustomerName = dlg.SelectedItem.Display;

            _lblCustomerPicked.Text      = dlg.SelectedItem.Display;
            _lblCustomerPicked.ForeColor = Color.FromArgb(15, 31, 53);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Grid helpers
        // ──────────────────────────────────────────────────────────────────────
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

        // ──────────────────────────────────────────────────────────────────────
        //  Add Item dialog (mirrors ModifyQuotationDialog.BtnAddItem_Click)
        // ──────────────────────────────────────────────────────────────────────
        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            var availableItems = _ctrl.GetAvailableItemsForQuotation(_selectedCustomerId)
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

            var pnlH = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(19, 35, 61) };
            pnlH.Controls.Add(new Label
            {
                Text      = "Add Quotation Item",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0), AutoSize = false
            });

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

            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 14, 24, 8), BackColor = Color.White };
            var tblBody = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tblBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tblBody.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 14, 0), BackColor = Color.Transparent };
            var lblItemCaption = new Label
            {
                Text = "Item", Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Top, Height = 40,
                TextAlign = ContentAlignment.BottomLeft
            };
            var txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Top, Height = 36,
                BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "\uD83D\uDD0E  Search by ID or name..."
            };
            var lstItems = new ListBox
            {
                Font = new Font("Segoe UI", 11f),
                Dock = DockStyle.Fill, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false
            };

            var productItems = availableItems
                .Select(p => new ProductComboItem(p.ItemName, p.ItemID, p.SalesPrice)).ToList();

            void FilterList(string keyword)
            {
                lstItems.BeginUpdate(); lstItems.Items.Clear();
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

            var numQty = new NumericUpDown { Minimum = 1, Maximum = 9999, Value = 1, Font = new Font("Segoe UI", 13f), Dock = DockStyle.Fill };
            var lblUnitPriceVal = new Label { Text = "HK$ 0.00", Font = new Font("Segoe UI", 13f), ForeColor = Color.FromArgb(15, 31, 53), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var lblSubtotalVal  = new Label { Text = "HK$ 0.00", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = Color.FromArgb(5, 150, 105), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

            Action recompute = () =>
            {
                var sel = lstItems.SelectedItem as ProductComboItem;
                if (sel == null) { lblUnitPriceVal.Text = "HK$ 0.00"; lblSubtotalVal.Text = "HK$ 0.00"; return; }
                lblUnitPriceVal.Text = $"HK$ {sel.UnitPrice:N2}";
                lblSubtotalVal.Text  = $"HK$ {sel.UnitPrice * (double)numQty.Value:N2}";
            };
            lstItems.SelectedIndexChanged += (s, ev) => recompute();
            numQty.ValueChanged           += (s, ev) => recompute();

            tblRight.Controls.Add(MakeFieldLabel("Quantity *"),  0, 0); tblRight.Controls.Add(numQty,          1, 0);
            tblRight.Controls.Add(MakeFieldLabel("Unit Price"),  0, 1); tblRight.Controls.Add(lblUnitPriceVal, 1, 1);
            tblRight.Controls.Add(MakeFieldLabel("Subtotal"),    0, 2); tblRight.Controls.Add(lblSubtotalVal,  1, 2);

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
                {
                    existing.Quantity += addQty;
                }
                else
                {
                    // Unit and DiscountPercent removed — no backing columns in schema.sql.
                    _items.Add(new QuotationItemEntity
                    {
                        QuotationID = _quotationId,
                        ItemID      = sel.ItemID,
                        ProductName = sel.ItemName,
                        Quantity    = addQty,
                        UnitPrice   = sel.UnitPrice
                    });
                }
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

        // ──────────────────────────────────────────────────────────────────────
        //  Save
        // ──────────────────────────────────────────────────────────────────────
        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedCustomerId))
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

            double total = _items.Sum(i => i.Subtotal);

            // DepositRequired is double? (nullable) — schema DEFAULT NULL.
            // If the user left it at 0, store null (no deposit required).
            double? deposit = _nudDeposit.Value > 0 ? (double?)_nudDeposit.Value : null;

            var quotation = new QuotationEntity
            {
                QuotationID       = _quotationId,
                CustomerID        = _selectedCustomerId,
                CustomerName      = _selectedCustomerName,
                ExpiryDate        = _dtpExpiry.Value.Date,
                TotalAmount       = total,
                DepositRequired   = deposit,
                LeadTimeEstimated = _txtLeadTime.Text.Trim(),
                TermsandCondition = string.Empty,
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

        // ──────────────────────────────────────────────────────────────────────
        //  Paint / Label helpers
        // ──────────────────────────────────────────────────────────────────────
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

        // ──────────────────────────────────────────────────────────────────────
        //  Inner class — Add Item sub-dialog
        // ──────────────────────────────────────────────────────────────────────
        private class ProductComboItem
        {
            public string ItemName  { get; }
            public string ItemID    { get; }
            public double UnitPrice { get; }
            public ProductComboItem(string name, string id, double price)
            { ItemName = name; ItemID = id; UnitPrice = price; }
            public override string ToString() => $"{ItemID}  —  {ItemName}";
        }
    }
}
