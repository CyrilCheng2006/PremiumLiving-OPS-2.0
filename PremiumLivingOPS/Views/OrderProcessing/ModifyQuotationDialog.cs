using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.OrderProcessing
{
    /// <summary>
    /// Modify Quotation — inline-rendered dialog.
    ///
    /// MVC contract (View layer):
    ///   • Receives a pre-loaded QuotationEntity (with Items) from QuotationForm.
    ///   • All Quotation header fields are read-only; only Quotation Items may be
    ///     added or deleted.
    ///   • On Confirm: calls _ctrl.SaveModifiedQuotation() with the updated item list.
    ///
    /// Layout:
    ///   – pnlHeader      Top  80   — dark navy, title "Modify Quotation — {ID}" + status badge
    ///   – pnlQuoteInfo   Top  220  — read-only 4-col: header fields (non-editable)
    ///   – pnlLineLabel   Top  50   — "QUOTATION ITEMS" bar + [＋ Add Item] button
    ///   – dgvItems       Fill      — editable item list with [✕] delete column
    ///   – pnlTotalRow    Bottom 50 — live-computed Total Amount
    ///   – pnlFooter      Bottom 80 — [✔ Save Changes] (210×60)  [Cancel] (210×60)
    ///
    /// Size: 2500 × 1200, StartPosition CenterParent.
    /// </summary>
    public partial class ModifyQuotationDialog : Form
    {
        private readonly OrderProcessingController _ctrl;
        private readonly QuotationEntity           _q;

        // Working copy of items — mutated by Add / Delete
        private readonly List<QuotationItemEntity> _items;

        // Controls that need cross-method access
        private DataGridView _dgvItems;
        private Label        _lblTotal;

        // ── Constructor
        public ModifyQuotationDialog(QuotationEntity q, OrderProcessingController ctrl)
        {
            _q    = q    ?? throw new ArgumentNullException(nameof(q));
            _ctrl = ctrl ?? throw new ArgumentNullException(nameof(ctrl));
            _items = new List<QuotationItemEntity>(q.Items ?? new List<QuotationItemEntity>());

            InitializeComponent();
            BuildDialog();
        }

        // ──────────────────────────────────────────────────────────────────
        //  Full inline build
        // ──────────────────────────────────────────────────────────────────
        private void BuildDialog()
        {
            // ── Form
            this.Text            = $"Modify Quotation  —  {_q.QuotationID}";
            this.Size            = new Size(2500, 1200);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.BackColor       = Color.White;
            this.Font            = new Font("Segoe UI", 13f);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;

            // ── Header (dark navy)
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor       = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Modify Quotation  —  {_q.QuotationID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            var (scBg, scFg) = GetStatusColor(_q.QuotationStatus);
            tblHeader.Controls.Add(new Label
            {
                Text      = _q.QuotationStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = scFg, BackColor = scBg,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Quotation header info (read-only, 4-col)
            var pnlQuoteInfo = new Panel
            {
                Dock    = DockStyle.Top, Height = 220,
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

            AddReadRow(tblQ, 0, "Quotation ID:",  _q.QuotationID,                     "Customer:",         _q.CustomerName);
            AddReadRow(tblQ, 1, "Total Amount:",  $"HK$ {_q.TotalAmount:N2}",         "Deposit Required:", $"HK$ {_q.DepositRequired:N2}");
            AddReadRow(tblQ, 2, "Lead Time:",     _q.LeadTimeEstimated ?? "—",        "Expiry Date:",      _q.ExpiryDate.ToString("yyyy-MM-dd"));
            pnlQuoteInfo.Controls.Add(tblQ);

            // ── QUOTATION ITEMS label bar (with Add Item button)
            var pnlLineLabel = new Panel
            {
                Dock      = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(239, 246, 255),
                Padding   = new Padding(28, 0, 16, 0)
            };
            pnlLineLabel.Paint += PaintBottomBorder;

            var tblLineBar = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor       = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblLineBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
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
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Fill,
                Cursor    = Cursors.Hand
            };
            btnAddItem.FlatAppearance.BorderSize         = 0;
            btnAddItem.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            btnAddItem.Click += BtnAddItem_Click;
            tblLineBar.Controls.Add(btnAddItem, 1, 0);
            pnlLineLabel.Controls.Add(tblLineBar);

            // ── Items DataGridView (editable: only Delete column action)
            _dgvItems = new DataGridView
            {
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                ReadOnly              = false,
                RowHeadersVisible     = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(221, 227, 236),
                Font                  = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate           = { Height = 46 },
                Dock                  = DockStyle.Fill,
                ColumnHeadersHeight   = 40,
                EnableHeadersVisualStyles = false,
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
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cProduct",   HeaderText = "PRODUCT",    FillWeight = 30, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",       HeaderText = "QTY",        FillWeight = 10, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUnit",      HeaderText = "UNIT",       FillWeight = 10, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cUnitPrice", HeaderText = "UNIT PRICE", FillWeight = 14, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cDiscount",  HeaderText = "DISCOUNT %", FillWeight = 12, ReadOnly = true });
            _dgvItems.Columns.Add(new DataGridViewTextBoxColumn { Name = "cSubtotal",  HeaderText = "SUBTOTAL",   FillWeight = 14, ReadOnly = true });

            var colDelete = new DataGridViewButtonColumn
            {
                Name           = "cDelete",
                HeaderText     = "",
                Text           = "\u2715  Delete",
                UseColumnTextForButtonValue = true,
                FillWeight     = 12,
                FlatStyle      = FlatStyle.Flat
            };
            _dgvItems.Columns.Add(colDelete);

            _dgvItems.CellClick    += DgvItems_CellClick;
            _dgvItems.CellPainting += DgvItems_CellPainting;

            RebuildItemGrid();

            // ── Total row
            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding   = new Padding(0, 0, 28, 0)
            };
            _lblTotal = new Label
            {
                Text      = FormatTotal(),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = false
            };
            pnlTotalRow.Controls.Add(_lblTotal);

            // ── Footer  [✔ Save Changes] (210×60)   [Cancel] (210×60)
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            pnlFooter.Paint += PaintTopBorder;

            var btnSave = new Button
            {
                Text      = "\u2714  Save Changes",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize         = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            btnSave.Click   += BtnSave_Click;
            btnCancel.Click += (o, ev) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnCancel);

            this.Controls.Add(_dgvItems);
            this.Controls.Add(pnlTotalRow);
            this.Controls.Add(pnlLineLabel);
            this.Controls.Add(pnlQuoteInfo);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Grid helpers
        // ──────────────────────────────────────────────────────────────────
        private void RebuildItemGrid()
        {
            _dgvItems.Rows.Clear();
            foreach (var item in _items)
                _dgvItems.Rows.Add(
                    item.ItemID,
                    item.ProductName,
                    item.Quantity,
                    item.Unit,
                    $"HK$ {item.UnitPrice:N2}",
                    $"{item.DiscountPercent:N1}%",
                    $"HK$ {item.Subtotal:N2}");

            RefreshTotal();
        }

        private void RefreshTotal()
        {
            if (_lblTotal != null)
                _lblTotal.Text = FormatTotal();
        }

        private string FormatTotal()
        {
            double total = 0;
            foreach (var i in _items) total += i.Subtotal;
            return $"Total Amount:   HK$ {total:N2}";
        }

        // ──────────────────────────────────────────────────────────────────
        //  Delete column — cell click
        // ──────────────────────────────────────────────────────────────────
        private void DgvItems_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_dgvItems.Columns[e.ColumnIndex].Name != "cDelete") return;

            var confirm = MessageBox.Show(
                "Remove this item from the quotation?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _items.RemoveAt(e.RowIndex);
                RebuildItemGrid();
            }
        }

        private void DgvItems_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_dgvItems.Columns[e.ColumnIndex].Name != "cDelete") return;

            e.Paint(e.ClipBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            using var brush = new SolidBrush(Color.FromArgb(254, 226, 226));
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
        //  Add Item — inline mini-dialog
        //  Uses ProductLookup (existing type) — no ItemLookup needed.
        // ──────────────────────────────────────────────────────────────────
        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            // GetAvailableItemsForQuotation returns List<ProductLookup>
            var availableItems = _ctrl.GetAvailableItemsForQuotation(_q.CustomerID)
                                 ?? new System.Collections.Generic.List<ProductLookup>();

            using var addDlg = new Form
            {
                Text            = "Add Quotation Item",
                Size            = new Size(900, 520),
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

            var pnlF = new Panel
            {
                Dock      = DockStyle.Fill,
                Padding   = new Padding(28, 16, 28, 8),
                BackColor = Color.White
            };
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            for (int r = 0; r < 5; r++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            // ComboBox using ProductLookup.DisplayText
            var cboItem = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Dock          = DockStyle.Fill
            };
            cboItem.Items.Add(new ProductComboItem("-- Select Item --", "", 0d, "pcs"));
            foreach (var p in availableItems)
                cboItem.Items.Add(new ProductComboItem(p.ItemName, p.ItemID, p.SalesPrice, "pcs"));
            cboItem.SelectedIndex = 0;

            var numQty = new NumericUpDown
            {
                Minimum = 1, Maximum = 9999, Value = 1,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            var numDiscount = new NumericUpDown
            {
                Minimum = 0, Maximum = 100, Value = 0, DecimalPlaces = 1,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            var lblUnitPrice = new Label
            {
                Text      = "HK$ 0.00",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblSubtotal = new Label
            {
                Text      = "HK$ 0.00",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Action recompute = () =>
            {
                var sel = cboItem.SelectedItem as ProductComboItem;
                if (sel == null || string.IsNullOrEmpty(sel.ItemID))
                { lblUnitPrice.Text = "HK$ 0.00"; lblSubtotal.Text = "HK$ 0.00"; return; }
                double price    = sel.UnitPrice;
                double qty      = (double)numQty.Value;
                double disc     = (double)numDiscount.Value;
                double subtotal = price * qty * (1 - disc / 100.0);
                lblUnitPrice.Text = $"HK$ {price:N2}";
                lblSubtotal.Text  = $"HK$ {subtotal:N2}";
            };

            cboItem.SelectedIndexChanged += (s, ev) => recompute();
            numQty.ValueChanged          += (s, ev) => recompute();
            numDiscount.ValueChanged     += (s, ev) => recompute();

            tbl.Controls.Add(MakeFieldLabel("Item *"),      0, 0); tbl.Controls.Add(cboItem,      1, 0);
            tbl.Controls.Add(MakeFieldLabel("Quantity *"),  0, 1); tbl.Controls.Add(numQty,       1, 1);
            tbl.Controls.Add(MakeFieldLabel("Discount %"),  0, 2); tbl.Controls.Add(numDiscount,  1, 2);
            tbl.Controls.Add(MakeFieldLabel("Unit Price"),  0, 3); tbl.Controls.Add(lblUnitPrice, 1, 3);
            tbl.Controls.Add(MakeFieldLabel("Subtotal"),    0, 4); tbl.Controls.Add(lblSubtotal,  1, 4);
            pnlF.Controls.Add(tbl);

            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            pnlFoot.Paint += PaintTopBorder;

            var btnAdd = new Button
            {
                Text      = "\u2714  Add Item",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize         = 0;
            btnAdd.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);

            var btnCancelAdd = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(210, 60),
                Dock      = DockStyle.Right,
                Cursor    = Cursors.Hand
            };
            btnCancelAdd.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancelAdd.FlatAppearance.BorderSize         = 1;
            btnCancelAdd.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            btnAdd.Click += (s, ev) =>
            {
                var sel = cboItem.SelectedItem as ProductComboItem;
                if (sel == null || string.IsNullOrEmpty(sel.ItemID))
                {
                    MessageBox.Show("Please select an item.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Build entity — do NOT assign Subtotal (computed property)
                _items.Add(new QuotationItemEntity
                {
                    QuotationID     = _q.QuotationID,
                    ItemID          = sel.ItemID,
                    ProductName     = sel.ItemName,
                    Quantity        = (int)numQty.Value,
                    Unit            = sel.Unit,
                    UnitPrice       = sel.UnitPrice,
                    DiscountPercent = (double)numDiscount.Value
                    // Subtotal is a computed property: Quantity * UnitPrice * (1 - DiscountPercent/100)
                    // Do NOT assign it — the getter calculates it automatically.
                });

                RebuildItemGrid();
                addDlg.DialogResult = DialogResult.OK;
                addDlg.Close();
            };
            btnCancelAdd.Click += (s, ev) => { addDlg.DialogResult = DialogResult.Cancel; addDlg.Close(); };

            pnlFoot.Controls.Add(btnAdd);
            pnlFoot.Controls.Add(btnCancelAdd);

            addDlg.Controls.Add(pnlF);
            addDlg.Controls.Add(pnlH);
            addDlg.Controls.Add(pnlFoot);
            addDlg.ShowDialog(this);
        }

        // ──────────────────────────────────────────────────────────────────
        //  Save
        // ──────────────────────────────────────────────────────────────────
        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_items.Count == 0)
            {
                MessageBox.Show(
                    "A quotation must have at least one item.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool ok = _ctrl.SaveModifiedQuotation(_q.QuotationID, _items);
                if (ok)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                    MessageBox.Show("Failed to save changes. Please try again.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ──────────────────────────────────────────────────────────────────
        //  Helpers
        // ──────────────────────────────────────────────────────────────────
        private static (Color bg, Color fg) GetStatusColor(string status)
            => status switch
            {
                "Pending"   => (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)),
                "Converted" => (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)),
                "Rejected"  => (Color.FromArgb(254, 226, 226), Color.FromArgb(153,  27,  27)),
                _           => (Color.FromArgb(80, 80, 80),    Color.White)
            };

        private static void AddReadRow(TableLayoutPanel tbl, int row,
            string keyL, string valL, string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKey(keyL),        0, row);
            tbl.Controls.Add(MakeLabelVal(valL ?? "—"), 1, row);
            tbl.Controls.Add(MakeLabelKey(keyR),        2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "—"), 3, row);
        }

        private static Label MakeLabelKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false
        };
        private static Label MakeLabelVal(string text) => new Label
        {
            Text         = text ?? "—",
            Font         = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        private static Label MakeFieldLabel(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false,
            Padding   = new Padding(0, 0, 8, 0)
        };
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

        private void InitializeComponent() { this.SuspendLayout(); this.ResumeLayout(false); }

        // ── Inner helper type (replaces removed ItemLookup / ItemComboItem)
        private class ProductComboItem
        {
            public string ItemName  { get; }
            public string ItemID    { get; }
            public double UnitPrice { get; }
            public string Unit      { get; }
            public ProductComboItem(string name, string id, double price, string unit)
            { ItemName = name; ItemID = id; UnitPrice = price; Unit = unit; }
            public override string ToString() => ItemName;
        }
    }
}
