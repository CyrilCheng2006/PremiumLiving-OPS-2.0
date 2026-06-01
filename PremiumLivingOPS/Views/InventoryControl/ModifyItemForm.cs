using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
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
        private readonly string _itemId;

        private TextBox      txtItemId, txtItemName, txtItemDesc, txtPrice;
        private ComboBox     cboCategory;
        private DataGridView dgvWarehouses;
        private Button       btnSave, btnDelete, btnClose;

        // ── Layout constants ─────────────────────────────────────────────
        private const int RowH     = 84;
        private const int RowGap   = 20;
        private const int LabelW   = 300;
        private const int BtnW     = 180;
        private const int BtnH     = 52;
        private const int CardPadH = 56;
        private const int CardPadV = 40;

        private Panel _outerFields;
        private Panel _outerGrid;
        private Panel _scroll;

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
            Size            = new Size(1600, 1100);
            MinimumSize     = new Size(1100, 800);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);

            // ── Header ────────────────────────────────────────────────────
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

            // ── Footer ────────────────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 90,
                BackColor = Color.White,
                Padding   = new Padding(0, 20, 48, 20)
            };
            pnlFoot.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            btnClose  = MakeBtn("Close",        Color.White,                  Color.FromArgb(15, 31, 53));
            btnDelete = MakeBtn("Delete Item",   Color.FromArgb(220, 38, 38), Color.White);
            btnSave   = MakeBtn("Save Changes",  Color.FromArgb(22, 163, 74), Color.White);
            btnClose.Click  += (s, e) => Close();
            btnDelete.Click += BtnDelete_Click;
            btnSave.Click   += BtnSave_Click;

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            flow.Controls.AddRange(new Control[] { btnClose, btnDelete, btnSave });
            pnlFoot.Controls.Add(flow);

            // ── Scroll body ────────────────────────────────────────────────
            _scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(240, 244, 249),
                AutoScroll = true,
                Padding    = new Padding(56, 40, 56, 24)
            };

            // ── Field card ─────────────────────────────────────────────────
            var (outerFields, innerFields) = CardPanel.Create(outerHeight: 100, outerPadding: new Padding(0));
            _outerFields = outerFields;
            innerFields.Padding = new Padding(CardPadH, CardPadV, CardPadH, CardPadV);

            txtItemId   = MakeTxt(readOnly: true);
            txtItemName = MakeTxt();
            txtItemDesc = MakeTxt();
            txtPrice    = MakeTxt();
            cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 13f) };

            if (_mode == ItemMode.Product)
                foreach (var c in new[] { "Sofa", "Bed", "Table", "Chair", "Cabinet" })
                    cboCategory.Items.Add(c);
            else
                foreach (var c in new[] { "Wood", "Metal", "Fabric", "Foam", "Glass", "Paint" })
                    cboCategory.Items.Add(c);

            string priceLabel = _mode == ItemMode.Product ? "Sales Price (HK$)" : "Unit Cost (HK$)";

            var fieldRows = new[]
            {
                FieldRow("Item ID",         txtItemId),
                FieldRow("Item Name *",     txtItemName),
                FieldRow("Description",      txtItemDesc),
                FieldRow("Category *",       cboCategory),
                FieldRow(priceLabel + " *",  txtPrice)
            };

            int y = 0;
            foreach (var row in fieldRows)
            {
                row.Location = new Point(0, y);
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                innerFields.Controls.Add(row);
                y += RowH + RowGap;
            }
            int fieldsContentH = y - RowGap + CardPadV * 2;
            outerFields.Height  = fieldsContentH + 16;
            innerFields.Height  = fieldsContentH;

            // ── Warehouse stock card ───────────────────────────────────────
            var (outerGrid, innerGrid) = CardPanel.Create(outerHeight: 320, outerPadding: new Padding(0));
            _outerGrid = outerGrid;
            innerGrid.Padding = new Padding(CardPadH, CardPadV, CardPadH, CardPadV);

            innerGrid.Controls.Add(new Label
            {
                Text      = "Warehouse Stock Breakdown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(19, 35, 61),
                Dock      = DockStyle.Top,
                Height    = 52
            });

            dgvWarehouses = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(230, 235, 245),
                Font                  = new Font("Segoe UI", 13f),
                ColumnHeadersHeight   = 52,
                RowTemplate           = { Height = 56 }
            };
            dgvWarehouses.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 13f, FontStyle.Bold);
            dgvWarehouses.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(240, 244, 249);
            dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Warehouse",   Name = "colWH",  AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Location",    Name = "colLoc", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Qty on Hand", Name = "colQty", Width = 160 });
            innerGrid.Controls.Add(dgvWarehouses);

            outerFields.Location = new Point(0, 0);
            outerFields.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            outerGrid.Location   = new Point(0, outerFields.Height + 32);
            outerGrid.Anchor     = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _scroll.Controls.Add(outerGrid);
            _scroll.Controls.Add(outerFields);

            Controls.Add(_scroll);
            Controls.Add(pnlFoot);
            Controls.Add(pnlHeader);

            Load          += (s, e) => ResizeCards();
            _scroll.Resize += (s, e) => ResizeCards();
        }

        private void ResizeCards()
        {
            if (_scroll == null) return;
            int w = _scroll.ClientSize.Width - _scroll.Padding.Horizontal;
            if (w < 100) return;
            if (_outerFields != null) _outerFields.Width = w;
            if (_outerGrid   != null)
            {
                _outerGrid.Width    = w;
                _outerGrid.Location = new Point(0, _outerFields.Height + 32);
            }
        }

        // ── LoadData ─────────────────────────────────────────────
        private void LoadData()
        {
            dgvWarehouses.Rows.Clear();

            if (_mode == ItemMode.Product)
            {
                var vm = _ctrl.GetModifyProductVM(_itemId);
                if (vm?.Product == null) { MessageBox.Show("Item not found.", "Error"); Close(); return; }

                var p = vm.Product;
                txtItemId.Text   = p.ItemID;
                txtItemName.Text = p.ItemName;
                txtItemDesc.Text = p.ItemDescription ?? "";
                txtPrice.Text    = p.SalesPrice.ToString("F2");

                int catIdx = cboCategory.FindStringExact(p.Category);
                if (catIdx >= 0) cboCategory.SelectedIndex = catIdx;

                foreach (var wi in vm.WarehouseBreakdown)
                    dgvWarehouses.Rows.Add(wi.WarehouseID, wi.WarehouseName, wi.Quantity);
            }
            else
            {
                var vm = _ctrl.GetModifyRawMaterialVM(_itemId);
                if (vm?.Material == null) { MessageBox.Show("Item not found.", "Error"); Close(); return; }

                var m = vm.Material;
                // RawMaterialEntity: MaterialID, MaterialName, ItemDescription, Category (=MaterialType), UnitCost
                txtItemId.Text   = m.MaterialID;
                txtItemName.Text = m.MaterialName;
                txtItemDesc.Text = m.ItemDescription ?? "";
                txtPrice.Text    = m.UnitCost.ToString("F2");

                int catIdx = cboCategory.FindStringExact(m.Category);
                if (catIdx >= 0) cboCategory.SelectedIndex = catIdx;

                foreach (var wi in vm.WarehouseBreakdown)
                    dgvWarehouses.Rows.Add(wi.WarehouseID, wi.WarehouseName, wi.Quantity);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name     = txtItemName.Text.Trim();
            string desc     = txtItemDesc.Text.Trim();
            string category = cboCategory.SelectedItem?.ToString();
            string priceStr = txtPrice.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category))
            { MessageBox.Show("Name and category are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            if (!double.TryParse(priceStr, out double price) || price < 0)
            { MessageBox.Show("Price must be a valid non-negative number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                if (_mode == ItemMode.Product)
                    _ctrl.SubmitUpdateProduct(_itemId, name,
                        string.IsNullOrEmpty(desc) ? null : desc, category, price);
                else
                    _ctrl.SubmitUpdateRawMaterial(_itemId, name,
                        string.IsNullOrEmpty(desc) ? null : desc, category, price);

                MessageBox.Show("Item updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                $"Are you sure you want to delete item '{_itemId}'?\nThis action cannot be undone.",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            try
            {
                if (_mode == ItemMode.Product)
                    _ctrl.DeleteProduct(_itemId);
                else
                    _ctrl.DeleteRawMaterial(_itemId);

                MessageBox.Show("Item deleted.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── UI helpers ─────────────────────────────────────────────────────
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

        private static TextBox MakeTxt(bool readOnly = false) => new TextBox
        {
            Font      = new Font("Segoe UI", 13f),
            Dock      = DockStyle.Fill,
            BackColor = readOnly ? Color.FromArgb(245, 247, 250) : Color.White,
            ReadOnly  = readOnly
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
    }
}
