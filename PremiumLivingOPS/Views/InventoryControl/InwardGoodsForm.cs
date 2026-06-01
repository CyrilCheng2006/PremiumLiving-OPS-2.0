using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.InventoryControl
{
    /// <summary>
    /// Record Inward Goods dialog.
    /// Pre-selects the item if opened from a specific row in ViewProductForm /
    /// ViewRawMaterialForm, or allows free selection if itemId is null.
    /// </summary>
    public class InwardGoodsForm : Form
    {
        private readonly InventoryControlController _ctrl = new InventoryControlController();
        private readonly string _preSelectedItemId;

        private ComboBox      cboItem;
        private ComboBox      cboWarehouse;
        private NumericUpDown nudQty;
        private Label         lblCurrentStock;
        private Button        btnConfirm, btnCancel;

        // ── Layout constants ─────────────────────────────────────────────
        private const int RowH     = 72;
        private const int RowGap   = 16;
        private const int LabelW   = 240;
        private const int BtnW     = 160;
        private const int BtnH     = 46;
        private const int CardPadH = 40;
        private const int CardPadV = 32;

        private Panel _outerCard;
        private Panel _scroll;

        public InwardGoodsForm(string preSelectedItemId = null)
        {
            _preSelectedItemId = preSelectedItemId;
            InitLayout();
        }

        private void InitLayout()
        {
            Text            = "Record Inward Goods";
            Size            = new Size(1200, 800);
            MinimumSize     = new Size(900, 700);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);

            // ── Header ──────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text      = "Record Inward Goods",
                Font      = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(36, 0, 0, 0)
            });

            // ── Footer ──────────────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 80,
                BackColor = Color.White,
                Padding   = new Padding(0, 16, 36, 16)
            };
            pnlFoot.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            btnCancel  = MakeBtn("Cancel",   Color.White,                  Color.FromArgb(15, 31, 53));
            btnConfirm = MakeBtn("Confirm",   Color.FromArgb(22, 163, 74), Color.White);
            btnCancel.Click  += (s, e) => Close();
            btnConfirm.Click += BtnConfirm_Click;

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            flow.Controls.AddRange(new Control[] { btnCancel, btnConfirm });
            pnlFoot.Controls.Add(flow);

            // ── Scroll body ──────────────────────────────────────────────
            _scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(240, 244, 249),
                AutoScroll = true,
                Padding    = new Padding(40, 28, 40, 16)
            };

            var (outerCard, innerCard) = CardPanel.Create(outerHeight: 100, outerPadding: new Padding(0));
            _outerCard = outerCard;
            innerCard.Padding = new Padding(CardPadH, CardPadV, CardPadH, CardPadV);

            cboItem      = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            cboWarehouse = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12f) };
            nudQty       = new NumericUpDown { Minimum = 1, Maximum = 99999, Value = 1, Font = new Font("Segoe UI", 12f) };
            lblCurrentStock = new Label
            {
                Text      = "Current Stock: —",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(70, 85, 110),
                Height    = 40,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            cboItem.SelectedIndexChanged      += (s, e) => UpdateCurrentStock();
            cboWarehouse.SelectedIndexChanged += (s, e) => UpdateCurrentStock();

            var fieldRows = new[]
            {
                FieldRow("Item *",             cboItem),
                FieldRow("Warehouse *",        cboWarehouse),
                FieldRow("Quantity Received *", nudQty)
            };

            int y = 0;
            foreach (var row in fieldRows)
            {
                row.Location = new Point(0, y);
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerCard.Controls.Add(row);
                y += RowH + RowGap;
            }

            // Stock info row
            var stockRow = new Panel { Height = 44, BackColor = Color.Transparent };
            var stockTlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            stockTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelW));
            stockTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            stockTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            stockTlp.Controls.Add(new Label { BackColor = Color.Transparent }, 0, 0);
            stockTlp.Controls.Add(lblCurrentStock, 1, 0);
            stockRow.Controls.Add(stockTlp);
            stockRow.Location = new Point(0, y);
            stockRow.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            innerCard.Controls.Add(stockRow);
            y += 44 + RowGap;

            int cardContentH = y - RowGap + CardPadV * 2;
            outerCard.Height  = cardContentH + 16;
            innerCard.Height  = cardContentH;

            _scroll.Controls.Add(outerCard);
            Controls.Add(_scroll);
            Controls.Add(pnlFoot);
            Controls.Add(pnlHeader);

            Load          += (s, e) => ResizeCard();
            _scroll.Resize += (s, e) => ResizeCard();

            LoadDropdowns();
        }

        private void ResizeCard()
        {
            if (_outerCard == null || _scroll == null) return;
            int w = _scroll.ClientSize.Width - _scroll.Padding.Horizontal;
            if (w < 100) return;
            _outerCard.Width = w;
        }

        private void LoadDropdowns()
        {
            var vm = _ctrl.GetInwardGoodsVM();

            cboItem.Items.Clear();
            foreach (var item in vm.Items)
                cboItem.Items.Add(new ItemComboItem(item.ItemID, item.ItemName));

            cboWarehouse.Items.Clear();
            foreach (var w in vm.Warehouses)
                cboWarehouse.Items.Add(new WarehouseComboItem(w.WarehouseID, w.WarehouseLocation));
            if (cboWarehouse.Items.Count > 0) cboWarehouse.SelectedIndex = 0;

            if (!string.IsNullOrEmpty(_preSelectedItemId))
            {
                for (int i = 0; i < cboItem.Items.Count; i++)
                {
                    if (cboItem.Items[i] is ItemComboItem ic && ic.Id == _preSelectedItemId)
                    { cboItem.SelectedIndex = i; break; }
                }
            }
            else if (cboItem.Items.Count > 0)
                cboItem.SelectedIndex = 0;
        }

        private void UpdateCurrentStock()
        {
            if (!(cboItem.SelectedItem is ItemComboItem ic) ||
                !(cboWarehouse.SelectedItem is WarehouseComboItem wh))
            { lblCurrentStock.Text = "Current Stock: —"; return; }

            var breakdown = _ctrl.GetWarehouseItemsByItem(ic.Id);
            int stock = 0;
            foreach (var wi in breakdown)
                if (wi.WarehouseID == wh.Id) { stock = wi.Quantity; break; }
            lblCurrentStock.Text = $"Current Stock in this warehouse: {stock}";
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (!(cboItem.SelectedItem is ItemComboItem ic))
            { MessageBox.Show("Please select an item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!(cboWarehouse.SelectedItem is WarehouseComboItem wh))
            { MessageBox.Show("Please select a warehouse.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            int qty = (int)nudQty.Value;
            try
            {
                _ctrl.SubmitInwardGoods(ic.Id, wh.Id, qty);
                MessageBox.Show($"{qty} unit(s) of '{ic.Name}' recorded to {wh.Name}.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── UI helpers ────────────────────────────────────────────────
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
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 85, 110),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            };

            var inputWrapper = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 12, 0, 12)
            };
            input.Dock = DockStyle.Fill;
            inputWrapper.Controls.Add(input);

            tlp.Controls.Add(lbl,          0, 0);
            tlp.Controls.Add(inputWrapper, 1, 0);
            row.Controls.Add(tlp);
            return row;
        }

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
                Margin    = new Padding(10, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        private sealed class ItemComboItem
        {
            public string Id   { get; }
            public string Name { get; }
            public ItemComboItem(string id, string name) { Id = id; Name = name; }
            public override string ToString() => $"{Name} ({Id})";
        }

        private sealed class WarehouseComboItem
        {
            public string Id   { get; }
            public string Name { get; }
            public WarehouseComboItem(string id, string name) { Id = id; Name = name; }
            public override string ToString() => $"{Id}  {Name}";
        }
    }
}
