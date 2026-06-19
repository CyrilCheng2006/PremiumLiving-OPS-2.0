using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.AfterService
{
    /// <summary>
    /// Dialog for creating a new Return Order.
    /// Uses CardPanel.Create() / CardPanel.CreateFill() — CardPanel is a static class.
    /// </summary>
    public class CreateReturnOrderDialog : Form
    {
        private readonly AfterServiceController _ctrl;

        private List<OrderEntity> _orderList;
        private List<(string StaffID, string StaffName, string Department, string StaffRole)> _staffList;

        private string _selectedOrderID;
        private string _selectedStaffID;
        private string _selectedStaffName;

        private TextBox        txtReturnID;
        private TextBox        txtOrderID;
        private Button         btnPickOrder;
        private TextBox        txtCustomer;
        private TextBox        txtHandedBy;
        private Button         btnPickStaff;
        private TextBox        txtReason;
        private TextBox        txtRefundAmount;
        private DateTimePicker dtpReturnDate;
        private ComboBox       cmbStatus;
        private Button         btnSave;
        private Button         btnCancel;

        public CreateReturnOrderDialog(AfterServiceController ctrl)
        {
            _ctrl = ctrl;
            LoadPickerData();
            InitUI();
        }

        private void LoadPickerData()
        {
            _orderList = _ctrl.GetOrdersForReturnPicker();
            _staffList = _ctrl.GetStaffListForPicker();
        }

        private void InitUI()
        {
            Text            = "Create Return Order";
            Size            = new Size(640, 580);
            MinimumSize     = new Size(560, 520);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 9.5f);

            // ── outer fill panel ─────────────────────────────────────────────
            var layout = new Panel { Dock = DockStyle.Fill };
            Controls.Add(layout);

            // ── title card ───────────────────────────────────────────────────
            var (titleOuter, titleInner) = CardPanel.Create(outerHeight: 52,
                outerPadding: new Padding(16, 10, 16, 4));
            titleOuter.Dock = DockStyle.Top;
            layout.Controls.Add(titleOuter);

            var lblTitle = new Label
            {
                Text      = "Create Return Order",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize  = true,
                Location  = new Point(12, 8)
            };
            titleInner.Controls.Add(lblTitle);

            // ── action buttons card (bottom) ─────────────────────────────────
            var (btnOuter, btnInner) = CardPanel.Create(outerHeight: 58,
                outerPadding: new Padding(16, 8, 16, 8));
            btnOuter.Dock = DockStyle.Bottom;
            layout.Controls.Add(btnOuter);

            btnSave = new Button
            {
                Text      = "Save",
                Size      = new Size(100, 36),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text      = "Cancel",
                Size      = new Size(100, 36),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var btnFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents  = false
            };
            btnFlow.Controls.Add(btnCancel);
            btnFlow.Controls.Add(btnSave);
            btnInner.Controls.Add(btnFlow);

            // ── form fields card (fills remaining space) ─────────────────────
            var (formOuter, formInner) = CardPanel.CreateFill(
                outerPadding: new Padding(16, 4, 16, 4));
            layout.Controls.Add(formOuter);

            // TableLayoutPanel for the form fields
            var tlp = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 3,
                RowCount    = 8,
                Padding     = new Padding(12)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140f));  // label
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));   // field
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92f));   // browse btn
            for (int i = 0; i < 8; i++)
                tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
            formInner.Controls.Add(tlp);

            Label Lbl(string t) => new Label
            {
                Text      = t,
                Anchor    = AnchorStyles.Left | AnchorStyles.Top,
                AutoSize  = true,
                ForeColor = Color.FromArgb(60, 60, 60),
                Margin    = new Padding(0, 12, 4, 0)
            };
            TextBox ROBox(string placeholder = "") => new TextBox
            {
                Dock        = DockStyle.Fill,
                ReadOnly    = true,
                BackColor   = Color.FromArgb(235, 238, 242),
                PlaceholderText = placeholder,
                Margin      = new Padding(0, 8, 4, 0)
            };

            // Row 0 — Return ID
            txtReturnID = ROBox();
            txtReturnID.Text = _ctrl.GenerateReturnId();
            tlp.Controls.Add(Lbl("Return ID:"),  0, 0);
            tlp.Controls.Add(txtReturnID,         1, 0);
            tlp.SetColumnSpan(txtReturnID, 2);

            // Row 1 — Order ID + Browse
            txtOrderID = ROBox("(click Browse…)");
            btnPickOrder = new Button
            {
                Text      = "Browse…",
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f),
                Margin    = new Padding(0, 8, 0, 0)
            };
            btnPickOrder.FlatAppearance.BorderSize = 0;
            btnPickOrder.Click += BtnPickOrder_Click;
            tlp.Controls.Add(Lbl("Order ID: *"), 0, 1);
            tlp.Controls.Add(txtOrderID,          1, 1);
            tlp.Controls.Add(btnPickOrder,        2, 1);

            // Row 2 — Customer (auto-filled)
            txtCustomer = ROBox();
            tlp.Controls.Add(Lbl("Customer:"),   0, 2);
            tlp.Controls.Add(txtCustomer,         1, 2);
            tlp.SetColumnSpan(txtCustomer, 2);

            // Row 3 — Handed By + Browse
            txtHandedBy = ROBox("(click Browse…)");
            btnPickStaff = new Button
            {
                Text      = "Browse…",
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f),
                Margin    = new Padding(0, 8, 0, 0)
            };
            btnPickStaff.FlatAppearance.BorderSize = 0;
            btnPickStaff.Click += BtnPickStaff_Click;
            tlp.Controls.Add(Lbl("Handed By: *"), 0, 3);
            tlp.Controls.Add(txtHandedBy,          1, 3);
            tlp.Controls.Add(btnPickStaff,         2, 3);

            // Row 4 — Return Date
            dtpReturnDate = new DateTimePicker
            {
                Dock   = DockStyle.Fill,
                Format = DateTimePickerFormat.Short,
                Value  = DateTime.Today,
                Margin = new Padding(0, 8, 4, 0)
            };
            tlp.Controls.Add(Lbl("Return Date: *"), 0, 4);
            tlp.Controls.Add(dtpReturnDate,          1, 4);
            tlp.SetColumnSpan(dtpReturnDate, 2);

            // Row 5 — Refund Amount
            txtRefundAmount = new TextBox
            {
                Dock            = DockStyle.Fill,
                PlaceholderText = "0.00",
                Margin          = new Padding(0, 8, 4, 0)
            };
            tlp.Controls.Add(Lbl("Refund Amount: *"), 0, 5);
            tlp.Controls.Add(txtRefundAmount,          1, 5);
            tlp.SetColumnSpan(txtRefundAmount, 2);

            // Row 6 — Status
            cmbStatus = new ComboBox
            {
                Dock          = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin        = new Padding(0, 8, 4, 0)
            };
            cmbStatus.Items.AddRange(new[] { "Pending", "Processing", "Refunded", "Rejected" });
            cmbStatus.SelectedIndex = 0;
            tlp.Controls.Add(Lbl("Status: *"), 0, 6);
            tlp.Controls.Add(cmbStatus,         1, 6);
            tlp.SetColumnSpan(cmbStatus, 2);

            // Row 7 — Reason
            txtReason = new TextBox
            {
                Dock       = DockStyle.Fill,
                Multiline  = true,
                ScrollBars = ScrollBars.Vertical,
                Margin     = new Padding(0, 8, 4, 0)
            };
            tlp.RowStyles[7] = new RowStyle(SizeType.Absolute, 66f);
            tlp.Controls.Add(Lbl("Reason:"), 0, 7);
            tlp.Controls.Add(txtReason,       1, 7);
            tlp.SetColumnSpan(txtReason, 2);
        }

        private void BtnPickOrder_Click(object sender, EventArgs e)
        {
            using var picker = new OrderPickerForm(_orderList);
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                _selectedOrderID = picker.SelectedOrderID;
                txtOrderID.Text  = picker.SelectedOrderID;
                txtCustomer.Text = picker.SelectedCustomer;
                if (string.IsNullOrWhiteSpace(txtRefundAmount.Text))
                    txtRefundAmount.Text = picker.SelectedGrandTotal.ToString("N2");
            }
        }

        private void BtnPickStaff_Click(object sender, EventArgs e)
        {
            using var picker = new StaffPickerForm(_staffList);
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                _selectedStaffID   = picker.SelectedStaffID;
                _selectedStaffName = picker.SelectedStaffName;
                txtHandedBy.Text   = $"{picker.SelectedStaffName} ({picker.SelectedStaffID})";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_selectedOrderID))
            {
                MessageBox.Show("Please select an Order ID.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_selectedStaffID))
            {
                MessageBox.Show("Please select a staff member (Handed By).", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!double.TryParse(txtRefundAmount.Text,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture,
                    out double refund) || refund < 0)
            {
                MessageBox.Show("Please enter a valid Refund Amount.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var entity = new ReturnOrderEntity
            {
                ReturnID     = txtReturnID.Text,
                OrderID      = _selectedOrderID,
                ReturnDate   = dtpReturnDate.Value.Date,
                Reason       = txtReason.Text.Trim(),
                RefundAmount = refund,
                ReturnStatus = cmbStatus.SelectedItem?.ToString() ?? "Pending"
            };

            if (_ctrl.CreateReturnOrder(entity))
            {
                MessageBox.Show(
                    $"Return Order {entity.ReturnID} created successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Failed to create return order. Please try again.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
