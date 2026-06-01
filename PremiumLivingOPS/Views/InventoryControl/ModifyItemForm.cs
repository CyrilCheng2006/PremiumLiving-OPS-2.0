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
    /// Modify Item (Edit / Delete) dialog — supports both Product and Raw Material.
    /// Displays a DataGridView of per-warehouse stock breakdown.
    /// </summary>
    public class ModifyItemForm : Form
    {
        public enum ItemMode { Product, RawMaterial }
        private readonly ItemMode _mode;
        private readonly InventoryControlController _ctrl = new InventoryControlController();

        // Payload set by the parent form before opening
        private readonly string _itemId;

        private TextBox        txtItemId, txtItemName, txtItemDesc, txtPrice;
        private ComboBox       cboCategory;
        private DataGridView   dgvWarehouses;
        private Button         btnSave, btnDelete, btnClose;

        public ModifyItemForm(ItemMode mode, string itemId)
        {
            _mode   = mode;
            _itemId = itemId;
            InitLayout();
            LoadData();
        }

        private void InitLayout()
        {
            string title = _mode == ItemMode.Product ? "Modify Product" : "Modify Raw Material";
            Text            = title;
            Size            = new Size(760, 680);
            MinimumSize     = new Size(680, 580);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 11f);

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.FromArgb(19, 35, 61) };
            pnlHeader.Controls.Add(new Label
            {
                Text = title, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(24, 0, 0, 0)
            });

            // Footer
            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = Color.White, Padding = new Padding(0, 12, 24, 12) };
            pnlFoot.Paint += (s, e) => { using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1); e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0); };

            btnClose  = MakeBtn("Close",      Color.White,                  Color.FromArgb(15, 31, 53));
            btnDelete = MakeBtn("Delete Item", Color.FromArgb(220, 38, 38), Color.White);
            btnSave   = MakeBtn("Save Changes",Color.FromArgb(22, 163, 74),  Color.White);
            btnClose.Click  += (s, e) => Close();
            btnDelete.Click += BtnDelete_Click;
            btnSave.Click   += BtnSave_Click;

            var flow = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent };
            flow.Controls.AddRange(new Control[] { btnClose, btnDelete, btnSave });
            pnlFoot.Controls.Add(flow);

            // Scroll body
            var scroll = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 244, 249), AutoScroll = true, Padding = new Padding(20, 14, 20, 8) };

            // Card 1 — Master Data fields
            var (outerMaster, innerMaster) = CardPanel.Create(300, new Padding(0));
            innerMaster.Padding = new Padding(24, 16, 24, 16);

            txtItemId   = MakeTxt(); txtItemId.ReadOnly = true; txtItemId.BackColor = Color.FromArgb(245, 247, 250);
            txtItemName = MakeTxt();
            txtItemDesc = MakeTxt();
            cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11f) };
            txtPrice    = MakeTxt();

            string priceCaption = _mode == ItemMode.Product ? "Sales Price (HK$)" : "Purchase Price (HK$)";
            string catCaption   = _mode == ItemMode.Product ? "Category"           : "Material Type";

            var fieldRows = new List<Panel>
            {
                FieldRow("Item ID",     txtItemId),
                FieldRow("Item Name *", txtItemName),
                FieldRow("Description", txtItemDesc),
                FieldRow(catCaption + " *", cboCategory),
                FieldRow(priceCaption + " *", txtPrice)
            };
            int y = 16;
            foreach (var row in fieldRows)
            {
                row.Location = new Point(0, y);
                row.Width    = innerMaster.Width - 48;
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerMaster.Controls.Add(row);
                y += row.Height + 10;
            }
            outerMaster.Height = y + 36;

            // Card 2 — Warehouse Stock Breakdown
            var (outerGrid, innerGrid) = CardPanel.Create(240, new Padding(0));
            innerGrid.Padding = new Padding(16, 12, 16, 12);

            innerGrid.Controls.Add(new Label
            {
                Text = "Stock by Warehouse", Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 35, 61), Dock = DockStyle.Top, Height = 28
            });

            dgvWarehouses = BuildDgv();
            innerGrid.Controls.Add(dgvWarehouses);

            // Stack cards inside scroll
            // Must add Fill card before Top card so Fill card fills remaining space
            scroll.Controls.Add(outerGrid);
            scroll.Controls.Add(outerMaster);

            Controls.Add(scroll);
            Controls.Add(pnlFoot);
            Controls.Add(pnlHeader);

            // Populate category dropdown
            if (_mode == ItemMode.Product)
                foreach (var c in new[] { "Sofa", "Bed", "Table", "Chair", "Cabinet" }) cboCategory.Items.Add(c);
            else
                foreach (var c in new[] { "Wood", "Metal", "Fabric", "Foam", "Glass", "Paint" }) cboCategory.Items.Add(c);
        }

        private void LoadData()
        {
            if (_mode == ItemMode.Product)
            {
                var vm = _ctrl.GetModifyProductVM(_itemId);
                if (vm.Product == null) { MessageBox.Show("Item not found."); Close(); return; }
                txtItemId.Text   = vm.Product.ItemID;
                txtItemName.Text = vm.Product.ItemName;
                txtItemDesc.Text = vm.Product.ItemDescription;
                txtPrice.Text    = vm.Product.SalesPrice.ToString("0.00");
                int idx = cboCategory.FindStringExact(vm.Product.Category);
                if (idx >= 0) cboCategory.SelectedIndex = idx;
                PopulateWarehouseDgv(vm.WarehouseBreakdown);
            }
            else
            {
                var vm = _ctrl.GetModifyRawMaterialVM(_itemId);
                if (vm.Material == null) { MessageBox.Show("Item not found."); Close(); return; }
                txtItemId.Text   = vm.Material.MaterialID;
                txtItemName.Text = vm.Material.MaterialName;
                txtItemDesc.Text = vm.Material.ItemDescription;
                txtPrice.Text    = vm.Material.UnitCost.ToString("0.00");
                int idx = cboCategory.FindStringExact(vm.Material.Category);
                if (idx >= 0) cboCategory.SelectedIndex = idx;
                PopulateWarehouseDgv(vm.WarehouseBreakdown);
            }
        }

        private void PopulateWarehouseDgv(List<WarehouseItemEntity> breakdown)
        {
            dgvWarehouses.Rows.Clear();
            foreach (var wi in breakdown)
                dgvWarehouses.Rows.Add(wi.WarehouseID, wi.WarehouseName, wi.Quantity, wi.ReorderLevel);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name     = txtItemName.Text.Trim();
            string desc     = txtItemDesc.Text.Trim();
            string category = cboCategory.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category))
            { MessageBox.Show("Name and Category are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!double.TryParse(txtPrice.Text.Trim(), out double price) || price < 0)
            { MessageBox.Show("Price must be a valid non-negative number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            try
            {
                if (_mode == ItemMode.Product)
                    _ctrl.SubmitUpdateProduct(_itemId, name, string.IsNullOrEmpty(desc) ? null : desc, category, price);
                else
                    _ctrl.SubmitUpdateRawMaterial(_itemId, name, string.IsNullOrEmpty(desc) ? null : desc, category, price);
                MessageBox.Show("Changes saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Are you sure you want to delete item '{_itemId}'?\nThis action cannot be undone.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
            try
            {
                if (_mode == ItemMode.Product)  _ctrl.DeleteProduct(_itemId);
                else                             _ctrl.DeleteRawMaterial(_itemId);
                MessageBox.Show("Item deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static DataGridView BuildDgv()
        {
            var dgv = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight   = 36,
                RowTemplate           = { Height = 36 },
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                Font                  = new Font("Segoe UI", 11f)
            };
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 249);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(70, 85, 110);
            dgv.EnableHeadersVisualStyles = false;

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWhId",       HeaderText = "Warehouse ID",   FillWeight = 25 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colWhName",     HeaderText = "Location",       FillWeight = 45 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQty",        HeaderText = "Stock Qty",      FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReorder",    HeaderText = "Reorder Level",  FillWeight = 15 });
            return dgv;
        }

        private static Panel FieldRow(string label, Control input)
        {
            var row = new Panel { Height = 52, BackColor = Color.Transparent };
            var lbl = new Label { Text = label, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(70, 85, 110), AutoSize = false, Size = new Size(180, 52), TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Left };
            input.Dock = DockStyle.Fill;
            row.Controls.Add(input);
            row.Controls.Add(lbl);
            return row;
        }

        private static TextBox MakeTxt() => new TextBox { Font = new Font("Segoe UI", 11f), Dock = DockStyle.Fill, BackColor = Color.White };

        private static Button MakeBtn(string text, Color bg, Color fg)
        {
            var b = new Button { Text = text, Font = new Font("Segoe UI", 11f), BackColor = bg, ForeColor = fg, FlatStyle = FlatStyle.Flat, Width = 130, Height = 40, Margin = new Padding(6, 0, 0, 0), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(200, 207, 220); b.FlatAppearance.BorderSize = 1;
            return b;
        }
    }
}
