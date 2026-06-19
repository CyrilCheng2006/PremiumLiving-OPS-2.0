using PremiumLivingOPS.Models.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// Popup order picker — rendered to match ComplaintListForm.ShowOrderPicker baseline.
    /// Structure: dark header (19,35,61) → white search bar (bottom-bordered)
    ///            → fill ListBox (Order ID only) → white footer with Select / Clear / Cancel.
    /// </summary>
    public class OrderPickerForm : Form
    {
        public string SelectedOrderID    { get; private set; }
        public string SelectedCustomer   { get; private set; }
        public double SelectedGrandTotal { get; private set; }

        private readonly List<OrderEntity> _allOrders;
        private ListBox  _lst;
        private TextBox  _txtSearch;

        public OrderPickerForm(List<OrderEntity> orders)
        {
            _allOrders = orders ?? new List<OrderEntity>();
            InitUI();
            Populate(string.Empty);
        }

        private void InitUI()
        {
            Text            = "Select Order ID";
            Size            = new Size(700, 560);
            MinimumSize     = new Size(500, 400);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);

            // ── Header (dark navy) ─────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(19, 35, 61) };
            hdr.Controls.Add(new Label
            {
                Text      = "\uD83D\uDD0D  Select Order ID",
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(20, 0, 0, 0)
            });

            // ── Search bar (white, bottom border) ───────────────────
            var pnlSearch = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Color.White,
                Padding   = new Padding(16, 10, 16, 10)
            };
            PaintBottomBorder(pnlSearch);
            _txtSearch = new TextBox
            {
                Dock            = DockStyle.Fill,
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                PlaceholderText = "Type to search Order ID..."
            };
            _txtSearch.TextChanged += (_, __) => Populate(_txtSearch.Text.Trim());
            pnlSearch.Controls.Add(_txtSearch);

            // ── ListBox (fill, Order ID only) ─────────────────────
            _lst = new ListBox
            {
                Dock          = DockStyle.Fill,
                Font          = new Font("Segoe UI", 12f),
                BorderStyle   = BorderStyle.None,
                ItemHeight    = 36,
                BackColor     = Color.White,
                SelectionMode = SelectionMode.One
            };
            _lst.DoubleClick += (_, __) => Confirm();

            // ── Footer (white, top border) ──────────────────────
            var foot = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 72,
                BackColor = Color.White,
                Padding   = new Padding(0, 12, 20, 12)
            };
            foot.Paint += (s, pe) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                pe.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
            };

            var btnSelect = new Button
            {
                Text      = "\u2714  Select",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                BackColor = Color.FromArgb(47, 111, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Width     = 160,
                Height    = 48,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 10, 0)
            };
            btnSelect.FlatAppearance.BorderSize = 0;
            btnSelect.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 216);

            var btnClear = new Button
            {
                Text      = "Clear (Optional)",
                Font      = new Font("Segoe UI", 12f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Width     = 180,
                Height    = 48,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 10, 0)
            };
            btnClear.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnClear.FlatAppearance.BorderSize  = 1;
            btnClear.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 31, 53),
                FlatStyle = FlatStyle.Flat,
                Width     = 120,
                Height    = 48,
                Cursor    = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize  = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            btnSelect.Click += (_, __) =>
            {
                if (_lst.SelectedItem != null) Confirm();
                else MessageBox.Show("Please select an Order ID.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            btnClear.Click += (_, __) =>
            {
                SelectedOrderID    = string.Empty;
                SelectedCustomer   = string.Empty;
                SelectedGrandTotal = 0;
                DialogResult = DialogResult.OK;
            };
            btnCancel.Click += (_, __) => Close();

            var footFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Right,
                AutoSize      = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent
            };
            footFlow.Controls.Add(btnSelect);
            footFlow.Controls.Add(btnClear);
            footFlow.Controls.Add(btnCancel);
            foot.Controls.Add(footFlow);

            // ── Body ─────────────────────────────────────────
            var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            body.Controls.Add(_lst);

            Controls.Add(body);
            Controls.Add(foot);
            Controls.Add(pnlSearch);
            Controls.Add(hdr);
        }

        /// <summary>
        /// Populates the ListBox with Order IDs only.
        /// Search matches against OrderID (and CustomerName internally for convenience,
        /// but the displayed text is Order ID only).
        /// </summary>
        private void Populate(string kw)
        {
            _lst.BeginUpdate();
            _lst.Items.Clear();
            foreach (var o in _allOrders)
            {
                bool match = string.IsNullOrEmpty(kw)
                    || o.OrderID.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
                if (match)
                    _lst.Items.Add(new OrderListItem
                    {
                        OrderID    = o.OrderID,
                        Customer   = o.CustomerName,
                        GrandTotal = o.GrandTotal
                    });
            }
            _lst.EndUpdate();
        }

        private void Confirm()
        {
            if (_lst.SelectedItem is OrderListItem item)
            {
                SelectedOrderID    = item.OrderID;
                SelectedCustomer   = item.Customer;
                SelectedGrandTotal = item.GrandTotal;
                DialogResult = DialogResult.OK;
            }
        }

        private static void PaintBottomBorder(Panel p)
        {
            p.Paint += (s, e) =>
            {
                using var pen = new System.Drawing.Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 0, ((Panel)s).Height - 1, ((Panel)s).Width, ((Panel)s).Height - 1);
            };
        }

        // ── Inner helper for ListBox items ──────────────────────
        private class OrderListItem
        {
            public string OrderID    { get; set; }
            public string Customer   { get; set; }
            public double GrandTotal { get; set; }

            // Only Order ID is shown in the ListBox
            public override string ToString() => OrderID;
        }
    }
}
