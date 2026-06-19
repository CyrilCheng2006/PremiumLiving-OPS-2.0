using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.InventoryControl
{
    /// <summary>
    /// Add New Item dialog — supports both Product and Raw Material.
    ///
    /// Product mode  : Item ID is auto-generated (IID-P-XXXX), ReadOnly.
    ///                 On Submit the ID is re-checked for uniqueness before
    ///                 hitting the DB; if a race-condition duplicate is found
    ///                 the user is prompted to close and reopen for a new ID.
    /// Raw Material  : Item ID is free-text editable; uniqueness is validated
    ///                 on Submit.
    /// </summary>
    public class AddItemForm : Form
    {
        public enum ItemMode { Product, RawMaterial }
        private readonly ItemMode _mode;
        private readonly InventoryControlController _ctrl = new InventoryControlController();

        private TextBox       txtItemId, txtItemName, txtItemDesc;
        private ComboBox      cboCategory;
        private TextBox       txtPrice;
        private ComboBox      cboWarehouse;
        private NumericUpDown nudInitialQty, nudReorderLevel;
        private Button        btnSubmit, btnCancel;

        private const int RowH      = 84;
        private const int RowGap    = 20;
        private const int LabelW    = 300;
        private const int BtnW      = 210;
        private const int BtnH      = 60;
        private const int CardPadH  = 56;
        private const int CardPadV  = 40;

        private Panel _outerCard;
        private Panel _scroll;

        public AddItemForm(ItemMode mode)
        {
            _mode = mode;
            InitLayout();
        }

        private void InitLayout()
        {
            string title = _mode == ItemMode.Product ? "Add New Product" : "Add New Raw Material";
            Text            = title;
            Size            = new Size(1600, 1200);
            MinimumSize     = new Size(1200, 900);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(48, 0, 0, 0)
            });

            _scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(240, 244, 249),
                AutoScroll = true,
                Padding    = new Padding(56, 40, 56, 24)
            };

            var (outerCard, innerCard) = CardPanel.Create(outerHeight: 100, outerPadding: new Padding(0));
            _outerCard = outerCard;
            innerCard.Padding = new Padding(CardPadH, CardPadV, CardPadH, CardPadV);

            var rows = BuildRows();
            int y = 0;
            foreach (var row in rows)
            {
                row.Location = new Point(0, y);
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(row);
                y += RowH + RowGap;
            }
            int cardContentH = y - RowGap + CardPadV * 2;
            outerCard.Height  = cardContentH + 16;
            innerCard.Height  = cardContentH;

            _scroll.Controls.Add(outerCard);

            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 100,
                BackColor = Color.White,
                Padding   = new Padding(0, 20, 48, 20)
            };
            pnlFoot.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            btnCancel = MakeBtn("Cancel",   Color.White,                  Color.FromArgb(15, 31, 53));
            btnSubmit = MakeBtn("Add Item",  Color.FromArgb(22, 163, 74), Color.White);
            btnCancel.Click += (s, e) => Close();
            btnSubmit.Click += BtnSubmit_Click;

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            flow.Controls.AddRange(new Control[] { btnCancel, btnSubmit });
            pnlFoot.Controls.Add(flow);

            Controls.Add(_scroll);
            Controls.Add(pnlFoot);
            Controls.Add(pnlHeader);

            Load          += AddItemForm_Load;
            _scroll.Resize += (s, e) => ResizeCard();

            LoadDropdowns();
        }

        private void AddItemForm_Load(object sender, EventArgs e)
        {
            ResizeCard();

            if (_mode == ItemMode.Product)
            {
                try
                {
                    txtItemId.Text = _ctrl.GenerateNextProductItemId();
                }
                catch
                {
                    // Fallback: let user type manually if DB unreachable
                    txtItemId.ReadOnly  = false;
                    txtItemId.BackColor = Color.White;
                    txtItemId.ForeColor = Color.FromArgb(15, 31, 53);
                    txtItemId.Text      = string.Empty;
                }
            }
        }

        private void ResizeCard()
        {
            if (_outerCard == null || _scroll == null) return;
            int w = _scroll.ClientSize.Width - _scroll.Padding.Horizontal;
            if (w < 100) return;
            _outerCard.Width = w;
        }

        private List<Panel> BuildRows()
        {
            var rows = new List<Panel>();

            txtItemId   = MakeTxt();
            txtItemName = MakeTxt();
            txtItemDesc = MakeTxt();

            if (_mode == ItemMode.Product)
            {
                txtItemId.ReadOnly  = true;
                txtItemId.BackColor = Color.FromArgb(240, 244, 249);
                txtItemId.ForeColor = Color.FromArgb(70, 85, 110);
                txtItemId.Text      = "Generating\u2026";
            }

            rows.Add(FieldRow("Item ID",       txtItemId));
            rows.Add(FieldRow("Item Name *",   txtItemName));
            rows.Add(FieldRow("Description",   txtItemDesc));

            cboCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 13f)
            };
            rows.Add(FieldRow(_mode == ItemMode.Product ? "Category *" : "Material Type *", cboCategory));

            txtPrice = MakeTxt(); txtPrice.Text = "0.00";
            rows.Add(FieldRow(_mode == ItemMode.Product
                ? "Sales Price (HK$) *"
                : "Purchase Price (HK$) *", txtPrice));

            cboWarehouse = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 13f)
            };
            rows.Add(FieldRow("Initial Warehouse *", cboWarehouse));

            nudInitialQty = new NumericUpDown
            {
                Minimum = 0, Maximum = 99999, DecimalPlaces = 0,
                Dock    = DockStyle.Fill, Font = new Font("Segoe UI", 13f)
            };
            nudReorderLevel = new NumericUpDown
            {
                Minimum = 0, Maximum = 99999, DecimalPlaces = 0,
                Value   = 10, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 13f)
            };

            rows.Add(FieldRow("Initial Qty *",   nudInitialQty));
            rows.Add(FieldRow("Reorder Level *", nudReorderLevel));

            return rows;
        }

        private void LoadDropdowns()
        {
            cboCategory.Items.Clear();
            if (_mode == ItemMode.Product)
                foreach (var c in new[] { "Sofa", "Bed", "Table", "Chair", "Cabinet" })
                    cboCategory.Items.Add(c);
            else
                foreach (var c in new[] { "Wood", "Metal", "Fabric", "Foam", "Glass", "Paint" })
                    cboCategory.Items.Add(c);
            if (cboCategory.Items.Count > 0) cboCategory.SelectedIndex = 0;

            cboWarehouse.Items.Clear();
            foreach (var w in _ctrl.GetAllWarehouses())
                cboWarehouse.Items.Add(new WarehouseComboItem(w.WarehouseID, w.WarehouseLocation));
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            string id       = txtItemId.Text.Trim();
            string name     = txtItemName.Text.Trim();
            string desc     = txtItemDesc.Text.Trim();
            string category = cboCategory.SelectedItem?.ToString();
            string priceStr = txtPrice.Text.Trim();
            var    wh       = cboWarehouse.SelectedItem as WarehouseComboItem;
            int    qty      = (int)nudInitialQty.Value;
            int    rl       = (int)nudReorderLevel.Value;

            // ── Basic required-field validation ──────────────────────────
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) ||
                string.IsNullOrEmpty(category) || wh == null)
            {
                MessageBox.Show("Please fill in all required fields (*).",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(priceStr, out double price) || price < 0)
            {
                MessageBox.Show("Price must be a valid non-negative number.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ── Duplicate Item ID check (application layer) ───────────────
            // This catches the common case fast and shows a clear message.
            // The Repo INSERT also performs a final check inside a transaction.
            try
            {
                if (_ctrl.IsItemIdExists(id))
                {
                    if (_mode == ItemMode.Product)
                    {
                        // Auto-generated ID was taken by a concurrent insert.
                        // Refresh to a new free ID and tell the user.
                        string newId = _ctrl.GenerateNextProductItemId();
                        txtItemId.Text = newId;
                        MessageBox.Show(
                            $"Item ID '{id}' was just taken by another record.\n" +
                            $"A new ID '{newId}' has been generated.\n" +
                            "Please click Add Item again to confirm.",
                            "ID Conflict — Refreshed",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Item ID '{id}' already exists in the database.\n" +
                            "Please enter a different Item ID.",
                            "Duplicate Item ID",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtItemId.Focus();
                        txtItemId.SelectAll();
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not verify Item ID: " + ex.Message,
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ── Commit to DB ──────────────────────────────────────────────
            try
            {
                if (_mode == ItemMode.Product)
                    _ctrl.SubmitAddProduct(id, name,
                        string.IsNullOrEmpty(desc) ? null : desc,
                        category, price, wh.Id, qty, rl);
                else
                    _ctrl.SubmitAddRawMaterial(id, name,
                        string.IsNullOrEmpty(desc) ? null : desc,
                        category, price, wh.Id, qty, rl);

                MessageBox.Show("Item added successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── UI helpers ────────────────────────────────────────────────────
        private static Panel FieldRow(string label, Control input)
        {
            var row = new Panel { Height = RowH, BackColor = Color.Transparent };
            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelW));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lbl = new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 85, 110),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            };

            var inputWrapper = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 14, 0, 14)
            };
            input.Dock = DockStyle.Fill;
            inputWrapper.Controls.Add(input);

            tlp.Controls.Add(lbl,          0, 0);
            tlp.Controls.Add(inputWrapper, 1, 0);
            row.Controls.Add(tlp);
            return row;
        }

        private static TextBox MakeTxt() => new TextBox
        {
            Font      = new Font("Segoe UI", 13f),
            Dock      = DockStyle.Fill,
            BackColor = Color.White
        };

        private static Button MakeBtn(string text, Color bg, Color fg)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 13f),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Width     = BtnW,
                Height    = BtnH,
                Margin    = new Padding(12, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        private class WarehouseComboItem
        {
            public string Id   { get; }
            public string Name { get; }
            public WarehouseComboItem(string id, string name) { Id = id; Name = name; }
            public override string ToString() => $"{Id}  {Name}";
        }
    }
}
