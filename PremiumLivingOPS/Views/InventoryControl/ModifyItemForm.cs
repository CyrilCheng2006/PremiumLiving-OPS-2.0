using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.InventoryControl
{
    /// <summary>
    /// Modify Item dialog — supports both Product and Raw Material.
    /// Visual language matches AddItemForm / ViewRawMaterialForm:
    ///   · Dark navy header
    ///   · Three CardPanel white-card sections floating on grey background
    ///   · FieldRow with shaded label column + bottom divider
    ///   · Footer: Close | Delete Item | Save Changes
    /// MVC: all data access goes through InventoryControlController.
    /// </summary>
    public class ModifyItemForm : Form
    {
        // ── Mode ──────────────────────────────────────────────────────────────
        public enum ItemMode { Product, RawMaterial }
        private readonly ItemMode _mode;
        private readonly string   _itemId;
        private readonly InventoryControlController _ctrl =
            new InventoryControlController();

        // ── Input controls ────────────────────────────────────────────────────
        private TextBox      _txtItemId, _txtItemName, _txtItemDesc, _txtPrice;
        private ComboBox     _cboCategory;
        private DataGridView _dgvWarehouses;
        private Button       _btnSave, _btnDelete, _btnClose;

        // ── Layout constants ──────────────────────────────────────────────────
        private const int RowH      = 64;   // FieldRow height (matches ViewRawMaterialForm)
        private const int LabelW    = 260;  // label column width
        private const int BtnW      = 210;
        private const int BtnH      = 60;
        private const int ScrollPad = 56;   // scroll area horizontal padding
        private const int CardGap   = 24;   // vertical gap between cards

        // Warehouse DGV metrics
        private const int WhHdrH = 44;
        private const int WhRowH = 44;

        // outer card panels (needed for ResizeCards)
        private Panel _outerCard1, _outerCard2, _outerCard3;
        private Panel _scroll;

        // ════════════════════════════════════════════════════════════════════
        public ModifyItemForm(ItemMode mode, string itemId)
        {
            _mode   = mode;
            _itemId = itemId;
            InitLayout();
            LoadData();
        }

        // ════════════════════════════════════════════════════════════════════
        //  InitLayout
        // ════════════════════════════════════════════════════════════════════
        private void InitLayout()
        {
            string title = _mode == ItemMode.Product
                ? "Modify Product"
                : "Modify Raw Material";

            Text            = title;
            Size            = new Size(1600, 1100);
            MinimumSize     = new Size(1200, 900);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);

            // ── Header ────────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(48, 0, 0, 0),
                BackColor = Color.Transparent
            });

            // ── Footer ────────────────────────────────────────────────────────
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

            _btnClose  = MakeBtn("Close",        Color.White,                  Color.FromArgb(15, 31, 53));
            _btnDelete = MakeBtn("Delete Item",   Color.FromArgb(220, 38,  38), Color.White);
            _btnSave   = MakeBtn("Save Changes",  Color.FromArgb(22,  163, 74), Color.White);

            _btnClose.Click  += (s, e) => Close();
            _btnDelete.Click += BtnDelete_Click;
            _btnSave.Click   += BtnSave_Click;

            var footFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent
            };
            footFlow.Controls.AddRange(new Control[] { _btnClose, _btnDelete, _btnSave });
            pnlFoot.Controls.Add(footFlow);

            // ── Scroll area ───────────────────────────────────────────────────
            _scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(240, 244, 249),
                AutoScroll = true,
                Padding    = new Padding(ScrollPad, 40, ScrollPad, 24)
            };

            // ── Build the three cards ─────────────────────────────────────────
            BuildCard1();   // Item Information
            BuildCard2();   // Material / Product Details
            BuildCard3();   // Warehouse Breakdown

            // Stack cards (Location-based; Anchor keeps width responsive)
            _outerCard1.Location = new Point(0, 0);
            _outerCard2.Location = new Point(0, _outerCard1.Height + CardGap);
            _outerCard3.Location = new Point(0, _outerCard1.Height + CardGap
                                                + _outerCard2.Height + CardGap);

            foreach (var c in new[] { _outerCard1, _outerCard2, _outerCard3 })
            {
                c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _scroll.Controls.Add(c);
            }

            Controls.Add(_scroll);
            Controls.Add(pnlFoot);
            Controls.Add(pnlHeader);

            Load           += (s, e) => ResizeCards();
            _scroll.Resize += (s, e) => ResizeCards();
        }

        // ════════════════════════════════════════════════════════════════════
        //  Card builders
        // ════════════════════════════════════════════════════════════════════

        /// Card 1 — Item Information  (3 rows: ID · Name · Description)
        private void BuildCard1()
        {
            _txtItemId   = MakeTxt(readOnly: true);
            _txtItemName = MakeTxt();
            _txtItemDesc = MakeTxt();

            var rows = new List<(string label, Control input, bool last)>
            {
                ("Item ID",      _txtItemId,   false),
                ("Item Name *",  _txtItemName, false),
                ("Description",  _txtItemDesc, true)
            };

            BuildCard(rows, "Item Information",
                out _outerCard1, sectionColor: Color.FromArgb(47, 111, 237));
        }

        /// Card 2 — Material Details  (2 rows: Category/Type · Price)
        private void BuildCard2()
        {
            _cboCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 13f)
            };

            string catLabel, priceLabel, sectionTitle;
            if (_mode == ItemMode.Product)
            {
                catLabel     = "Category *";
                priceLabel   = "Sales Price (HK$) *";
                sectionTitle = "Product Details";
                foreach (var c in new[] { "Sofa", "Bed", "Table", "Chair", "Cabinet" })
                    _cboCategory.Items.Add(c);
            }
            else
            {
                catLabel     = "Material Type *";
                priceLabel   = "Purchase Price (HK$) *";
                sectionTitle = "Material Details";
                foreach (var c in new[] { "Wood", "Metal", "Fabric", "Foam", "Glass", "Paint" })
                    _cboCategory.Items.Add(c);
            }

            _txtPrice = MakeTxt();

            var rows = new List<(string label, Control input, bool last)>
            {
                (catLabel,   _cboCategory, false),
                (priceLabel, _txtPrice,    true)
            };

            BuildCard(rows, sectionTitle,
                out _outerCard2, sectionColor: Color.FromArgb(47, 111, 237));
        }

        /// Card 3 — Warehouse Breakdown  (DGV, no FieldRows)
        private void BuildCard3()
        {
            const int SecH = 50;

            _dgvWarehouses = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(237, 241, 247),
                Font                  = new Font("Segoe UI", 12f),
                ColumnHeadersHeight   = WhHdrH,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect
            };
            _dgvWarehouses.RowTemplate.Height = WhRowH;
            _dgvWarehouses.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(248, 250, 252);
            _dgvWarehouses.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 12f, FontStyle.Bold);
            _dgvWarehouses.ColumnHeadersDefaultCellStyle.ForeColor  = Color.FromArgb(70, 85, 110);
            _dgvWarehouses.ColumnHeadersBorderStyle                  = DataGridViewHeaderBorderStyle.Single;
            _dgvWarehouses.DefaultCellStyle.BackColor                = Color.White;
            _dgvWarehouses.DefaultCellStyle.SelectionBackColor       = Color.FromArgb(219, 234, 254);
            _dgvWarehouses.DefaultCellStyle.SelectionForeColor       = Color.FromArgb(15, 31, 53);
            _dgvWarehouses.DefaultCellStyle.Padding                  = new Padding(12, 0, 12, 0);
            _dgvWarehouses.EnableHeadersVisualStyles                 = false;

            _dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colWhId",    HeaderText = "Warehouse ID",  AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells });
            _dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colWhLoc",   HeaderText = "Location",      AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colQty",     HeaderText = "Stock Qty",     Width = 130 });
            _dgvWarehouses.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "colReorder", HeaderText = "Reorder Level", Width = 150 });

            // Initial card height: section header + DGV header + 1 placeholder row + 22 (CardPanel border)
            int card3H = SecH + WhHdrH + WhRowH + 22;

            var (outer, inner) = CardPanel.Create(
                outerHeight:  card3H,
                outerPadding: new Padding(20, 8, 20, 16));
            inner.Padding = new Padding(0);

            // Section header
            var secHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = SecH,
                BackColor = Color.White,
                Padding   = new Padding(20, 0, 20, 0)
            };
            secHeader.Controls.Add(new Label
            {
                Text      = "Warehouse Breakdown",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            secHeader.Paint += (s, pe) =>
            {
                var p = (Panel)s;
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 20, p.Height - 1, p.Width - 20, p.Height - 1);
            };

            inner.Controls.Add(_dgvWarehouses);
            inner.Controls.Add(secHeader);   // DockStyle.Top — drawn above DGV

            _outerCard3 = outer;
        }

        /// Generic helper: builds a titled CardPanel from a list of FieldRows.
        private void BuildCard(
            List<(string label, Control input, bool last)> rows,
            string sectionTitle,
            out Panel outerCard,
            Color sectionColor)
        {
            const int SecH = 50;

            int totalRowsH = rows.Count * RowH;
            int cardH      = SecH + totalRowsH + 22;   // 22 = CardPanel border overhead

            var (outer, inner) = CardPanel.Create(
                outerHeight:  cardH,
                outerPadding: new Padding(20, 8, 20, 8));
            inner.Padding = new Padding(0);

            // Section title header
            var secHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = SecH,
                BackColor = Color.White,
                Padding   = new Padding(20, 0, 20, 0)
            };
            secHeader.Controls.Add(new Label
            {
                Text      = sectionTitle,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = sectionColor,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            secHeader.Paint += (s, pe) =>
            {
                var p = (Panel)s;
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 20, p.Height - 1, p.Width - 20, p.Height - 1);
            };

            // Row container (DockStyle.Fill — sits below section header)
            var rowContainer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White
            };

            int y = 0;
            foreach (var (lbl, input, last) in rows)
            {
                var row = MakeFieldRow(lbl, input, last);
                row.Location = new Point(0, y);
                row.Anchor   = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                rowContainer.Controls.Add(row);
                y += RowH;
            }

            // Keep rows full-width when container resizes
            rowContainer.Resize += (s, _) =>
            {
                int w = ((Panel)s).Width;
                foreach (Control c in ((Panel)s).Controls) c.Width = w;
            };

            inner.Controls.Add(rowContainer);
            inner.Controls.Add(secHeader);   // DockStyle.Top — rendered first

            outerCard = outer;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Data loading  (MVC: all reads via controller)
        // ════════════════════════════════════════════════════════════════════
        private void LoadData()
        {
            _dgvWarehouses.Rows.Clear();

            if (_mode == ItemMode.Product)
            {
                var vm = _ctrl.GetModifyProductVM(_itemId);
                if (vm?.Product == null)
                {
                    MessageBox.Show("Item not found.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close(); return;
                }

                var p = vm.Product;
                _txtItemId.Text   = p.ItemID;
                _txtItemName.Text = p.ItemName;
                _txtItemDesc.Text = p.ItemDescription ?? string.Empty;
                _txtPrice.Text    = p.SalesPrice.ToString("F2");

                int idx = _cboCategory.FindStringExact(p.Category);
                if (idx >= 0) _cboCategory.SelectedIndex = idx;

                foreach (var wi in vm.WarehouseBreakdown)
                    _dgvWarehouses.Rows.Add(wi.WarehouseID, wi.WarehouseName,
                                            wi.Quantity, wi.ReorderLevel);
            }
            else
            {
                var vm = _ctrl.GetModifyRawMaterialVM(_itemId);
                if (vm?.Material == null)
                {
                    MessageBox.Show("Item not found.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close(); return;
                }

                var m = vm.Material;
                _txtItemId.Text   = m.MaterialID;
                _txtItemName.Text = m.MaterialName;
                _txtItemDesc.Text = m.ItemDescription ?? string.Empty;
                _txtPrice.Text    = m.UnitCost.ToString("F2");

                int idx = _cboCategory.FindStringExact(m.Category);
                if (idx >= 0) _cboCategory.SelectedIndex = idx;

                foreach (var wi in vm.WarehouseBreakdown)
                    _dgvWarehouses.Rows.Add(wi.WarehouseID, wi.WarehouseName,
                                            wi.Quantity, wi.ReorderLevel);
            }

            // Resize Card 3 to fit actual row count
            RefreshCard3Height();
        }

        /// Adjusts Card 3 outer height to fit actual warehouse rows loaded.
        private void RefreshCard3Height()
        {
            if (_outerCard3 == null) return;
            const int SecH = 50;
            int rows       = Math.Max(1, _dgvWarehouses.Rows.Count);
            _outerCard3.Height = SecH + WhHdrH + rows * WhRowH + 22;

            // Re-stack cards after height change
            _outerCard2.Location = new Point(0, _outerCard1.Height + CardGap);
            _outerCard3.Location = new Point(0, _outerCard1.Height + CardGap
                                                + _outerCard2.Height + CardGap);
        }

        // ════════════════════════════════════════════════════════════════════
        //  Save / Delete  (MVC: all writes via controller)
        // ════════════════════════════════════════════════════════════════════
        private void BtnSave_Click(object sender, EventArgs e)
        {
            string name     = _txtItemName.Text.Trim();
            string desc     = _txtItemDesc.Text.Trim();
            string category = _cboCategory.SelectedItem?.ToString();
            string priceStr = _txtPrice.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category))
            {
                MessageBox.Show("Item Name and Category are required.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!double.TryParse(priceStr, out double price) || price < 0)
            {
                MessageBox.Show("Price must be a valid non-negative number.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_mode == ItemMode.Product)
                    _ctrl.SubmitUpdateProduct(_itemId, name,
                        string.IsNullOrEmpty(desc) ? null : desc, category, price);
                else
                    _ctrl.SubmitUpdateRawMaterial(_itemId, name,
                        string.IsNullOrEmpty(desc) ? null : desc, category, price);

                MessageBox.Show("Item updated successfully.",
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

                MessageBox.Show("Item deleted successfully.",
                    "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Responsive resize
        // ════════════════════════════════════════════════════════════════════
        private void ResizeCards()
        {
            if (_scroll == null) return;
            int w = _scroll.ClientSize.Width - _scroll.Padding.Horizontal;
            if (w < 100) return;
            if (_outerCard1 != null) _outerCard1.Width = w;
            if (_outerCard2 != null)
            {
                _outerCard2.Width    = w;
                _outerCard2.Location = new Point(0, _outerCard1.Height + CardGap);
            }
            if (_outerCard3 != null)
            {
                _outerCard3.Width    = w;
                _outerCard3.Location = new Point(0, _outerCard1.Height + CardGap
                                                    + _outerCard2.Height + CardGap);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  UI helpers
        // ════════════════════════════════════════════════════════════════════

        /// FieldRow: shaded label column + white input + bottom divider.
        /// Matches ViewRawMaterialForm FieldRow visual language exactly.
        private static Panel MakeFieldRow(string labelText, Control input, bool lastRow)
        {
            var row = new Panel { Height = RowH, BackColor = Color.White };

            if (!lastRow)
            {
                row.Paint += (s, pe) =>
                {
                    var p = (Panel)s;
                    using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                    pe.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
                };
            }

            var tlp = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                BackColor       = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelW));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var lbl = new Label
            {
                Text      = labelText,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 85, 110),
                BackColor = Color.FromArgb(248, 250, 252),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Padding   = new Padding(20, 0, 8, 0)
            };

            var inputWrapper = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.White,
                Padding   = new Padding(20, 10, 20, 10)
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
