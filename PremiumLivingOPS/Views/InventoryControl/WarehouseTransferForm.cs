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
    /// Record Warehouse Item Transfer dialog.
    /// User selects: source WarehouseItem (item + from-warehouse),
    /// destination warehouse, and transfer qty.
    /// Creates a TransferForm + TransferForm_WarehouseItem row and updates stock.
    /// </summary>
    public class WarehouseTransferForm : Form
    {
        private readonly InventoryControlController _ctrl = new InventoryControlController();
        private WarehouseTransferViewModel _vm;

        private ComboBox      cboFromItem;
        private ComboBox      cboFromWarehouse;
        private ComboBox      cboToWarehouse;
        private NumericUpDown nudQty;
        private Label         lblTransferId, lblAvailable;
        private Button        btnConfirm, btnCancel;

        private string _fromWarehouseItemId;
        private int    _availableQty;

        // ── Layout constants ────────────────────────────────────────────
        private const int RowH     = 68;
        private const int RowGap   = 14;
        private const int LabelW   = 220;
        private const int BtnW     = 150;
        private const int BtnH     = 44;
        private const int CardPadH = 32;
        private const int CardPadV = 28;

        public WarehouseTransferForm()
        {
            InitLayout();
            LoadDropdowns();
        }

        private void InitLayout()
        {
            Text            = "Warehouse Item Transfer";
            Size            = new Size(1200, 800);
            MinimumSize     = new Size(900, 700);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);

            // ── Header ─────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text = "Warehouse Item Transfer",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(32, 0, 0, 0)
            });

            // ── Footer ─────────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 76, BackColor = Color.White,
                Padding = new Padding(0, 14, 28, 14)
            };
            pnlFoot.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            btnCancel  = MakeBtn("Cancel",   Color.White,                  Color.FromArgb(15, 31, 53));
            btnConfirm = MakeBtn("Transfer",  Color.FromArgb(47, 111, 237), Color.White);
            btnCancel.Click  += (s, e) => Close();
            btnConfirm.Click += BtnConfirm_Click;

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent
            };
            flow.Controls.AddRange(new Control[] { btnCancel, btnConfirm });
            pnlFoot.Controls.Add(flow);

            // ── Body card ───────────────────────────────────────
            var scroll = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249),
                AutoScroll = true, Padding = new Padding(32, 20, 32, 16)
            };

            var (outerCard, innerCard) = CardPanel.Create(480, new Padding(0));
            innerCard.Padding = new Padding(CardPadH, CardPadV, CardPadH, CardPadV);

            lblTransferId = new Label
            {
                Font = new Font("Segoe UI", 12f), ForeColor = Color.FromArgb(19, 35, 61),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            cboFromItem      = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboFromWarehouse = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboToWarehouse   = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            nudQty           = new NumericUpDown { Minimum = 1, Maximum = 99999, Value = 1, Font = new Font("Segoe UI", 12f) };
            lblAvailable     = new Label
            {
                Text = "Available: —", Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(70, 85, 110), Height = 32, AutoSize = false
            };

            cboFromItem.SelectedIndexChanged      += (s, e) => RefreshFromWarehouses();
            cboFromWarehouse.SelectedIndexChanged += (s, e) => RefreshAvailable();

            var rows = new[]
            {
                FieldRow("Transfer ID",     lblTransferId),
                FieldRow("Item *",           cboFromItem),
                FieldRow("From Warehouse *",  cboFromWarehouse),
                FieldRow("To Warehouse *",    cboToWarehouse),
                FieldRow("Transfer Qty *",    nudQty)
            };

            int y = CardPadV;
            foreach (var row in rows)
            {
                row.Location = new Point(0, y);
                row.Width    = innerCard.Width - CardPadH * 2;
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(row);
                y += row.Height + RowGap;
            }

            lblAvailable.Location = new Point(LabelW, y + 6);
            lblAvailable.Width    = innerCard.Width - LabelW - CardPadH * 2;
            lblAvailable.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            innerCard.Controls.Add(lblAvailable);

            scroll.Controls.Add(outerCard);
            Controls.Add(scroll);
            Controls.Add(pnlFoot);
            Controls.Add(pnlHeader);
        }

        private void LoadDropdowns()
        {
            _vm = _ctrl.GetWarehouseTransferVM();
            lblTransferId.Text = _vm.NextTransferID;

            var seen = new HashSet<string>();
            cboFromItem.Items.Clear();
            foreach (var wi in _vm.WarehouseItems)
            {
                if (seen.Add(wi.ItemID))
                    cboFromItem.Items.Add(new ItemComboItem(wi.ItemID, wi.ItemName));
            }
            if (cboFromItem.Items.Count > 0) cboFromItem.SelectedIndex = 0;

            cboToWarehouse.Items.Clear();
            foreach (var w in _vm.Warehouses)
                cboToWarehouse.Items.Add(new WarehouseComboItem(w.WarehouseID, w.WarehouseLocation));
            if (cboToWarehouse.Items.Count > 0) cboToWarehouse.SelectedIndex = 0;
        }

        private void RefreshFromWarehouses()
        {
            cboFromWarehouse.Items.Clear();
            if (!(cboFromItem.SelectedItem is ItemComboItem ic)) return;
            foreach (var wi in _vm.WarehouseItems)
            {
                if (wi.ItemID == ic.Id && wi.Quantity > 0)
                    cboFromWarehouse.Items.Add(new WarehouseItemComboItem(wi.WarehouseItemID, wi.WarehouseID, wi.WarehouseName, wi.Quantity));
            }
            if (cboFromWarehouse.Items.Count > 0) cboFromWarehouse.SelectedIndex = 0;
            RefreshAvailable();
        }

        private void RefreshAvailable()
        {
            if (!(cboFromWarehouse.SelectedItem is WarehouseItemComboItem wi))
            { lblAvailable.Text = "Available: —"; _fromWarehouseItemId = null; _availableQty = 0; nudQty.Maximum = 1; return; }
            _fromWarehouseItemId = wi.WarehouseItemId;
            _availableQty        = wi.Qty;
            nudQty.Maximum       = _availableQty;
            lblAvailable.Text    = $"Available in source warehouse: {_availableQty}";
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_fromWarehouseItemId))
            { MessageBox.Show("Please select a source item and warehouse.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!(cboToWarehouse.SelectedItem is WarehouseComboItem toWh))
            { MessageBox.Show("Please select a destination warehouse.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (cboFromWarehouse.SelectedItem is WarehouseItemComboItem fromWi && fromWi.WarehouseId == toWh.Id)
            { MessageBox.Show("Source and destination warehouse cannot be the same.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int qty = (int)nudQty.Value;
            if (qty > _availableQty)
            { MessageBox.Show($"Transfer quantity exceeds available stock ({_availableQty}).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                _ctrl.SubmitWarehouseTransfer(_vm.NextTransferID, _fromWarehouseItemId, toWh.Id, qty);
                MessageBox.Show($"Transfer {_vm.NextTransferID} completed successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Text = label, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 85, 110), AutoSize = false,
                Size = new Size(LabelW, RowH), TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Left
            };
            input.Dock = DockStyle.Fill;
            row.Controls.Add(input);
            row.Controls.Add(lbl);
            return row;
        }

        private static Button MakeBtn(string text, Color bg, Color fg)
        {
            var b = new Button
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                BackColor = bg, ForeColor = fg, FlatStyle = FlatStyle.Flat,
                Width = BtnW, Height = BtnH, Margin = new Padding(8, 0, 0, 0), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        private class ItemComboItem
        {
            public string Id   { get; }
            public string Name { get; }
            public ItemComboItem(string id, string name) { Id = id; Name = name; }
            public override string ToString() => $"{Name} ({Id})";
        }

        private class WarehouseComboItem
        {
            public string Id   { get; }
            public string Name { get; }
            public WarehouseComboItem(string id, string name) { Id = id; Name = name; }
            public override string ToString() => $"{Id}  {Name}";
        }

        private class WarehouseItemComboItem
        {
            public string WarehouseItemId { get; }
            public string WarehouseId     { get; }
            public string Name            { get; }
            public int    Qty             { get; }
            public WarehouseItemComboItem(string wiId, string whId, string name, int qty)
            { WarehouseItemId = wiId; WarehouseId = whId; Name = name; Qty = qty; }
            public override string ToString() => $"{WarehouseId}  {Name}  (Qty: {Qty})";
        }
    }
}
