using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.Shared
{
    /// <summary>
    /// AddOrderItemDialog — searchable product picker + qty input.
    /// Opens from "+ Add Item" in Create Order and Modify Order.
    /// Returns SelectedProduct and SelectedQty on DialogResult.OK.
    /// </summary>
    public class AddOrderItemDialog : Form
    {
        public ProductLookup SelectedProduct { get; private set; }
        public int           SelectedQty     { get; private set; } = 1;

        private TextBox       txtSearch;
        private ListBox       lstItems;
        private Label         lblSelectedName;
        private Label         lblSelectedPrice;
        private NumericUpDown nudQty;
        private Button        btnConfirm;
        private Button        btnCancel;

        private readonly List<ProductLookup> _products;
        private List<ProductLookup>          _filtered;

        public AddOrderItemDialog(List<ProductLookup> products)
        {
            _products = (products ?? new List<ProductLookup>())
                        .OrderBy(p => p.ItemID, StringComparer.Ordinal)
                        .ToList();
            _filtered = new List<ProductLookup>(_products);
            BuildUI();
            PopulateList();
        }

        private void BuildUI()
        {
            this.Text            = "Add Order Item";
            this.Size            = new Size(1400, 800);
            this.MinimumSize     = new Size(1200, 660);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = Color.FromArgb(246, 249, 255);
            this.Font            = new Font("Segoe UI", 11f);

            const int BtnW   = 290;
            const int BtnH   = 60;
            const int BtnGap = 10;

            // ── Title bar ─────────────────────────────────────────────────
            var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White };
            pnlTitle.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, pnlTitle.Height - 1, pnlTitle.Width, pnlTitle.Height - 1);
            };
            var lblTitle = new Label
            {
                Text      = "➕  Add Order Item",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0)
            };
            pnlTitle.Controls.Add(lblTitle);

            // ── Search box ────────────────────────────────────────────────
            var pnlSearch = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 52,
                BackColor = Color.Transparent,
                Padding   = new Padding(16, 10, 16, 4)
            };
            txtSearch = new TextBox
            {
                PlaceholderText = "🔍  Search by item ID or name…",
                Font            = new Font("Segoe UI", 11f),
                BorderStyle     = BorderStyle.FixedSingle,
                Dock            = DockStyle.Fill
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;
            txtSearch.KeyDown     += TxtSearch_KeyDown;
            pnlSearch.Controls.Add(txtSearch);

            // ── Item list ─────────────────────────────────────────────────
            lstItems = new ListBox
            {
                Dock           = DockStyle.Fill,
                Font           = new Font("Segoe UI", 11f),
                BorderStyle    = BorderStyle.FixedSingle,
                ItemHeight     = 32,
                BackColor      = Color.White,
                ForeColor      = Color.FromArgb(15, 31, 53),
                SelectionMode  = SelectionMode.One,
                IntegralHeight = false
            };
            lstItems.SelectedIndexChanged += LstItems_SelectedIndexChanged;
            lstItems.DoubleClick          += LstItems_DoubleClick;
            lstItems.KeyDown              += LstItems_KeyDown;

            var pnlList = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding   = new Padding(16, 0, 16, 0)
            };
            pnlList.Controls.Add(lstItems);

            // ── Selected item preview ─────────────────────────────────────
            var pnlPreview = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 60,
                BackColor = Color.White,
                Padding   = new Padding(20, 0, 20, 0)
            };
            pnlPreview.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlPreview.Width, 0);
            };
            lblSelectedPrice = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Right,
                AutoSize  = false,
                Width     = 280,
                TextAlign = ContentAlignment.MiddleRight
            };
            lblSelectedName = new Label
            {
                Text      = "No item selected",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlPreview.Controls.Add(lblSelectedName);
            pnlPreview.Controls.Add(lblSelectedPrice);

            // ── Footer ────────────────────────────────────────────────────
            int footerH = BtnH + 20;
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = footerH,
                BackColor = Color.FromArgb(246, 249, 255)
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0);
            };

            // Qty label — 160 px, left-anchored
            var lblQty = new Label
            {
                Text      = "Quantity:",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                AutoSize  = false,
                Width     = 160,
                Height    = BtnH,
                Location  = new Point(16, (footerH - BtnH) / 2),
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor    = AnchorStyles.Left | AnchorStyles.Top
            };

            nudQty = new NumericUpDown
            {
                Minimum   = 1,
                Maximum   = 9999,
                Value     = 1,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                Width     = 110,
                Height    = BtnH,
                Location  = new Point(16 + 160, (footerH - BtnH) / 2),
                TextAlign = HorizontalAlignment.Center,
                Anchor    = AnchorStyles.Left | AnchorStyles.Top
            };

            // Green Confirm — explicit Size, right-anchored
            btnConfirm = new Button
            {
                Text      = "✔  Confirm",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(34, 139, 34),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(BtnW, BtnH),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            btnConfirm.FlatAppearance.BorderSize         = 0;
            btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 111, 22);
            btnConfirm.FlatAppearance.MouseDownBackColor = Color.FromArgb(14, 85, 14);
            btnConfirm.Click += BtnConfirm_Click;

            // Red Cancel — identical Size, right-anchored
            btnCancel = new Button
            {
                Text      = "✕  Cancel",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(192, 57, 43),
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(BtnW, BtnH),
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCancel.FlatAppearance.BorderSize         = 0;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 40, 30);
            btnCancel.FlatAppearance.MouseDownBackColor = Color.FromArgb(125, 28, 20);
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Position both buttons manually so sizes are never distorted by Dock
            void LayoutBtns()
            {
                int top   = (pnlFooter.Height - BtnH) / 2;
                int right = pnlFooter.Width - 16;
                btnConfirm.Location = new Point(right - BtnW, top);
                btnCancel.Location  = new Point(right - BtnW - BtnGap - BtnW, top);
            }
            pnlFooter.Resize += (s, e) => LayoutBtns();

            pnlFooter.Controls.Add(lblQty);
            pnlFooter.Controls.Add(nudQty);
            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(btnCancel);

            // ── Assemble ──────────────────────────────────────────────────
            this.Controls.Add(pnlList);
            this.Controls.Add(pnlPreview);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlSearch);
            this.Controls.Add(pnlTitle);

            this.AcceptButton  = btnConfirm;
            this.CancelButton  = btnCancel;
            this.ActiveControl = txtSearch;

            // Run layout once form is fully loaded
            this.Load += (s, e) => LayoutBtns();
        }

        // ── List population & filtering ───────────────────────────────────
        private void PopulateList()
        {
            lstItems.Items.Clear();
            foreach (var p in _filtered)
                lstItems.Items.Add($"{p.ItemID}  –  {p.ItemName}");
            if (lstItems.Items.Count > 0) lstItems.SelectedIndex = 0;
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string kw = txtSearch.Text.Trim().ToLower();
            _filtered = string.IsNullOrEmpty(kw)
                ? new List<ProductLookup>(_products)
                : _products.Where(p =>
                    p.ItemID.ToLower().Contains(kw) ||
                    p.ItemName.ToLower().Contains(kw)).ToList();
            PopulateList();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && lstItems.Items.Count > 0)
            {
                lstItems.Focus();
                lstItems.SelectedIndex = 0;
                e.Handled = true;
            }
        }

        // ── Selection preview ─────────────────────────────────────────────
        private void LstItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = lstItems.SelectedIndex;
            if (idx < 0 || idx >= _filtered.Count)
            {
                lblSelectedName.Text      = "No item selected";
                lblSelectedName.ForeColor = Color.FromArgb(98, 112, 135);
                lblSelectedPrice.Text     = "";
                return;
            }
            var p = _filtered[idx];
            lblSelectedName.Text      = $"{p.ItemID}  –  {p.ItemName}";
            lblSelectedName.ForeColor = Color.FromArgb(15, 31, 53);
            lblSelectedPrice.Text     = $"HK$ {p.SalesPrice:N2}";
        }

        private void LstItems_DoubleClick(object sender, EventArgs e) => Confirm();

        private void LstItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { Confirm(); e.Handled = true; }
        }

        // ── Confirm ───────────────────────────────────────────────────────
        private void BtnConfirm_Click(object sender, EventArgs e) => Confirm();

        private void Confirm()
        {
            int idx = lstItems.SelectedIndex;
            if (idx < 0 || idx >= _filtered.Count)
            {
                MessageBox.Show("Please select an item from the list.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SelectedProduct   = _filtered[idx];
            SelectedQty       = (int)nudQty.Value;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
