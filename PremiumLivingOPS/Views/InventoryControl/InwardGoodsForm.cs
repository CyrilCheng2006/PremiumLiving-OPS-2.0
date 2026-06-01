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
    /// Record Inward Goods dialog — adds received stock to a WarehouseItem row.
    /// Pre-selects the item if opened from a specific row in ViewProductForm /
    /// ViewRawMaterialForm, or allows free selection if opened from the action bar.
    /// </summary>
    public class InwardGoodsForm : Form
    {
        private readonly InventoryControlController _ctrl = new InventoryControlController();
        private readonly string _preSelectedItemId;   // may be null

        private ComboBox       cboItem;
        private ComboBox       cboWarehouse;
        private NumericUpDown  nudQty;
        private Label          lblCurrentStock;
        private Button         btnConfirm, btnCancel;

        public InwardGoodsForm(string preSelectedItemId = null)
        {
            _preSelectedItemId = preSelectedItemId;
            InitLayout();
        }

        private void InitLayout()
        {
            Text            = "Record Inward Goods";
            Size            = new Size(580, 480);
            MinimumSize     = new Size(520, 420);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 11f);

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label { Text = "Record Inward Goods", Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0) });

            // Footer
            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = Color.White, Padding = new Padding(0, 12, 24, 12) };
            pnlFoot.Paint += (s, e) => { using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); };

            btnCancel  = MakeBtn("Cancel",  Color.White,                  Color.FromArgb(15, 31, 53));
            btnConfirm = MakeBtn("Confirm",  Color.FromArgb(22, 163, 74), Color.White);
            btnCancel.Click  += (s, e) => Close();
            btnConfirm.Click += BtnConfirm_Click;

            var flow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent };
            flow.Controls.AddRange(new Control[] { btnCancel, btnConfirm });
            pnlFoot.Controls.Add(flow);

            // Body card
            var scroll = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249), AutoScroll = true, Padding = new Padding(20, 14, 20, 8) };
            var (outerCard, innerCard) = CardPanel.Create(310, new Padding(0));
            innerCard.Padding = new Padding(24, 20, 24, 20);

            cboItem      = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11f) };
            cboWarehouse = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11f) };
            nudQty       = new NumericUpDown { Minimum = 1, Maximum = 99999, Value = 1, Font = new Font("Segoe UI", 11f) };
            lblCurrentStock = new Label { Text = "Current Stock: —", Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(70, 85, 110), Height = 24, AutoSize = false };

            cboItem.SelectedIndexChanged      += CboItem_Changed;
            cboWarehouse.SelectedIndexChanged += CboWarehouse_Changed;

            var fields = new[] {
                FieldRow("Item *",           cboItem),
                FieldRow("Warehouse *",      cboWarehouse),
                FieldRow("Quantity Received *", nudQty)
            };

            int y = 20;
            foreach (var row in fields)
            {
                row.Location = new Point(0, y);
                row.Width    = innerCard.Width - 48;
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(row);
                y += row.Height + 10;
            }

            lblCurrentStock.Location = new Point(180, y + 4);
            lblCurrentStock.Width    = innerCard.Width - 204;
            lblCurrentStock.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            innerCard.Controls.Add(lblCurrentStock);

            scroll.Controls.Add(outerCard);

            Controls.Add(scroll);
            Controls.Add(pnlFoot);
            Controls.Add(pnlHeader);

            LoadDropdowns();
        }

        private void LoadDropdowns()
        {
            var vm = _ctrl.GetInwardGoodsVM();

            cboItem.Items.Clear();
            foreach (var item in vm.Items) cboItem.Items.Add(item);

            cboWarehouse.Items.Clear();
            foreach (var w in vm.Warehouses) cboWarehouse.Items.Add(new WarehouseComboItem(w.WarehouseID, w.WarehouseLocation));
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;

            // Pre-select item if specified
            if (!string.IsNullOrEmpty(_preSelectedItemId))
            {
                for (int i = 0; i < cboItem.Items.Count; i++)
                {
                    if (cboItem.Items[i] is ItemLookup il && il.ItemID == _preSelectedItemId)
                    { cboItem.SelectedIndex = i; break; }
                }
            }
            else if (cboItem.Items.Count > 0) cboItem.SelectedIndex = 0;
        }

        private void CboItem_Changed(object sender, EventArgs e) => UpdateCurrentStock();
        private void CboWarehouse_Changed(object sender, EventArgs e) => UpdateCurrentStock();

        private void UpdateCurrentStock()
        {
            if (!(cboItem.SelectedItem is ItemLookup il) || !(cboWarehouse.SelectedItem is WarehouseComboItem wh))
            { lblCurrentStock.Text = "Current Stock: —"; return; }

            var breakdown = _ctrl.GetWarehouseItemsByItem(il.ItemID);
            int stock = 0;
            foreach (var wi in breakdown)
                if (wi.WarehouseID == wh.Id) { stock = wi.Quantity; break; }
            lblCurrentStock.Text = $"Current Stock in this warehouse: {stock}";
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (!(cboItem.SelectedItem is ItemLookup il))
            { MessageBox.Show("Please select an item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!(cboWarehouse.SelectedItem is WarehouseComboItem wh))
            { MessageBox.Show("Please select a warehouse.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            int qty = (int)nudQty.Value;
            try
            {
                _ctrl.SubmitInwardGoods(il.ItemID, wh.Id, qty);
                MessageBox.Show($"{qty} unit(s) of '{il.ItemName}' recorded to {wh.Name}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── helpers ──────────────────────────────────────────────────────────
        private static Panel FieldRow(string label, Control input)
        {
            var row = new Panel { Height = 52, BackColor = Color.Transparent };
            var lbl = new Label { Text = label, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(70, 85, 110), AutoSize = false, Size = new Size(180, 52), TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Left };
            input.Dock = DockStyle.Fill;
            row.Controls.Add(input);
            row.Controls.Add(lbl);
            return row;
        }

        private static Button MakeBtn(string text, Color bg, Color fg)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 11f), BackColor = bg, ForeColor = fg, FlatStyle = FlatStyle.Flat, Width = 130, Height = 40, Margin = new Padding(6, 0, 0, 0), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220); b.FlatAppearance.BorderSize = 1;
            return b;
        }

        private class WarehouseComboItem
        {
            public string Id   { get; }
            public string Name { get; }
            public WarehouseComboItem(string id, string name) { Id = id; Name = name; }
            public override string ToString() => $"{Id}  {name}";
            private readonly string name;
            public WarehouseComboItem(string id, string name2) { Id = id; Name = name2; name = name2; }
        }
    }
}
