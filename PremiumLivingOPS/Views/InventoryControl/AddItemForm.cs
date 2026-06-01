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
    /// Opened as a modal dialog from ViewProductForm / ViewRawMaterialForm.
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
        private Label         lblPriceCaption;

        // ──────────────────────────────────────────────────────────
        // Sizing constants — all layout changes here
        private const int RowH      = 68;   // was 52 — more breathing room
        private const int RowGap    = 14;   // was 12
        private const int LabelW    = 220;  // was 180
        private const int BtnW      = 150;  // was 130
        private const int BtnH      = 44;   // was 40
        private const int CardPadH  = 32;   // was 24 (horizontal)
        private const int CardPadV  = 28;   // was 16 (vertical)

        public AddItemForm(ItemMode mode)
        {
            _mode = mode;
            InitLayout();
        }

        private void InitLayout()
        {
            string title = _mode == ItemMode.Product ? "Add New Product" : "Add New Raw Material";
            Text            = title;
            Size            = new Size(1200, 800);
            MinimumSize     = new Size(900, 700);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);   // was 11f

            // ── Header ───────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(32, 0, 0, 0)
            });

            // ── Scroll body ────────────────────────────────────────
            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(240, 244, 249),
                AutoScroll = true,
                Padding    = new Padding(32, 20, 32, 16)
            };

            var (outerCard, innerCard) = CardPanel.Create(600, new Padding(0));
            innerCard.Padding = new Padding(CardPadH, CardPadV, CardPadH, CardPadV);

            var rows = BuildRows();
            int y = CardPadV;
            foreach (var row in rows)
            {
                row.Location = new Point(0, y);
                row.Width    = innerCard.Width - CardPadH * 2;
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(row);
                y += row.Height + RowGap;
            }
            outerCard.Height = y + CardPadV + 8;

            scroll.Controls.Add(outerCard);

            // ── Footer ───────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 76,
                BackColor = Color.White,
                Padding   = new Padding(0, 14, 28, 14)
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
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent
            };
            flow.Controls.AddRange(new Control[] { btnCancel, btnSubmit });
            pnlFoot.Controls.Add(flow);

            Controls.Add(scroll);
            Controls.Add(pnlFoot);
            Controls.Add(pnlHeader);

            LoadDropdowns();
        }

        private List<Panel> BuildRows()
        {
            var rows = new List<Panel>();

            txtItemId   = MakeTxt(); rows.Add(FieldRow("Item ID *",   txtItemId));
            txtItemName = MakeTxt(); rows.Add(FieldRow("Item Name *",  txtItemName));
            txtItemDesc = MakeTxt(); rows.Add(FieldRow("Description",   txtItemDesc));

            cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f) };
            rows.Add(FieldRow(_mode == ItemMode.Product ? "Category *" : "Material Type *", cboCategory));

            txtPrice = MakeTxt(); txtPrice.Text = "0.00";
            rows.Add(FieldRow(_mode == ItemMode.Product ? "Sales Price (HK$) *" : "Purchase Price (HK$) *", txtPrice));

            cboWarehouse = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f) };
            rows.Add(FieldRow("Initial Warehouse *", cboWarehouse));

            nudInitialQty   = new NumericUpDown { Minimum = 0, Maximum = 99999, DecimalPlaces = 0, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f) };
            nudReorderLevel = new NumericUpDown { Minimum = 0, Maximum = 99999, DecimalPlaces = 0, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f), Value = 10 };

            rows.Add(FieldRow("Initial Qty *",   nudInitialQty));
            rows.Add(FieldRow("Reorder Level *", nudReorderLevel));

            return rows;
        }

        private void LoadDropdowns()
        {
            cboCategory.Items.Clear();
            if (_mode == ItemMode.Product)
                foreach (var c in new[] { "Sofa", "Bed", "Table", "Chair", "Cabinet" }) cboCategory.Items.Add(c);
            else
                foreach (var c in new[] { "Wood", "Metal", "Fabric", "Foam", "Glass", "Paint" }) cboCategory.Items.Add(c);
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

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category) || wh == null)
            { MessageBox.Show("Please fill in all required fields (*)", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!double.TryParse(priceStr, out double price) || price < 0)
            { MessageBox.Show("Price must be a valid non-negative number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                if (_mode == ItemMode.Product)
                    _ctrl.SubmitAddProduct(id, name, string.IsNullOrEmpty(desc) ? null : desc, category, price, wh.Id, qty, rl);
                else
                    _ctrl.SubmitAddRawMaterial(id, name, string.IsNullOrEmpty(desc) ? null : desc, category, price, wh.Id, qty, rl);

                MessageBox.Show("Item added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── UI helpers ───────────────────────────────────────────
        private static Panel FieldRow(string label, Control input)
        {
            var row = new Panel { Height = RowH, BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 85, 110),
                AutoSize  = false,
                Size      = new Size(LabelW, RowH),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock      = DockStyle.Left
            };
            input.Dock = DockStyle.Fill;
            row.Controls.Add(input);
            row.Controls.Add(lbl);
            return row;
        }

        private static TextBox MakeTxt() => new TextBox
        {
            Font      = new Font("Segoe UI", 12f),
            Dock      = DockStyle.Fill,
            BackColor = Color.White
        };

        private static Button MakeBtn(string text, Color bg, Color fg)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Width     = BtnW,
                Height    = BtnH,
                Margin    = new Padding(8, 0, 0, 0),
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
