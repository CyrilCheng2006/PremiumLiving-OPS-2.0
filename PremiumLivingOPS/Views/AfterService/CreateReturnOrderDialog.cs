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
    /// Order ID and Handed By are selected via searchable popup pickers.
    /// </summary>
    public class CreateReturnOrderDialog : Form
    {
        private readonly AfterServiceController _ctrl;

        private List<OrderEntity> _orderList;
        private List<(string StaffID, string StaffName, string Department, string StaffRole)> _staffList;

        private string _selectedOrderID;
        private string _selectedStaffID;
        private string _selectedStaffName;

        private TextBox       txtReturnID;
        private TextBox       txtOrderID;
        private Button        btnPickOrder;
        private TextBox       txtCustomer;
        private TextBox       txtHandedBy;
        private Button        btnPickStaff;
        private TextBox       txtReason;
        private TextBox       txtRefundAmount;
        private DateTimePicker dtpReturnDate;
        private ComboBox      cmbStatus;
        private Button        btnSave;
        private Button        btnCancel;

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
            Size            = new Size(640, 560);
            MinimumSize     = new Size(560, 500);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(243, 244, 246);
            Font            = new Font("Segoe UI", 9.5f);

            var outerCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            Controls.Add(outerCard);

            var lblTitle = new Label
            {
                Text      = "Create Return Order",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize  = true,
                Location  = new Point(20, 16)
            };
            outerCard.Controls.Add(lblTitle);

            var innerCard = new CardPanel
            {
                Location = new Point(20, 52),
                Size     = new Size(572, 400),
                Padding  = new Padding(16)
            };
            outerCard.Controls.Add(innerCard);

            const int labelX = 10;
            const int fieldX = 170;
            const int fieldW = 260;
            const int btnW   = 90;
            const int rowH   = 42;
            int y = 12;

            Label MakeLabel(string text, int top) => new Label
            {
                Text      = text,
                AutoSize  = true,
                Location  = new Point(labelX, top + 4),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            // ── Return ID (auto-generated, read-only) ──
            innerCard.Controls.Add(MakeLabel("Return ID:", y));
            txtReturnID = new TextBox
            {
                Location  = new Point(fieldX, y),
                Size      = new Size(fieldW + btnW + 4, 26),
                ReadOnly  = true,
                BackColor = Color.FromArgb(235, 238, 242),
                Text      = _ctrl.GenerateReturnId()
            };
            innerCard.Controls.Add(txtReturnID);
            y += rowH;

            // ── Order ID (picker) ──
            innerCard.Controls.Add(MakeLabel("Order ID: *", y));
            txtOrderID = new TextBox
            {
                Location        = new Point(fieldX, y),
                Size            = new Size(fieldW, 26),
                ReadOnly        = true,
                PlaceholderText = "(click Browse)",
                BackColor       = Color.FromArgb(235, 238, 242)
            };
            innerCard.Controls.Add(txtOrderID);

            btnPickOrder = new Button
            {
                Text      = "Browse…",
                Size      = new Size(btnW, 26),
                Location  = new Point(fieldX + fieldW + 4, y),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f)
            };
            btnPickOrder.FlatAppearance.BorderSize = 0;
            btnPickOrder.Click += BtnPickOrder_Click;
            innerCard.Controls.Add(btnPickOrder);
            y += rowH;

            // ── Customer (auto-filled) ──
            innerCard.Controls.Add(MakeLabel("Customer:", y));
            txtCustomer = new TextBox
            {
                Location  = new Point(fieldX, y),
                Size      = new Size(fieldW + btnW + 4, 26),
                ReadOnly  = true,
                BackColor = Color.FromArgb(235, 238, 242)
            };
            innerCard.Controls.Add(txtCustomer);
            y += rowH;

            // ── Handed By (picker) ──
            innerCard.Controls.Add(MakeLabel("Handed By: *", y));
            txtHandedBy = new TextBox
            {
                Location        = new Point(fieldX, y),
                Size            = new Size(fieldW, 26),
                ReadOnly        = true,
                PlaceholderText = "(click Browse)",
                BackColor       = Color.FromArgb(235, 238, 242)
            };
            innerCard.Controls.Add(txtHandedBy);

            btnPickStaff = new Button
            {
                Text      = "Browse…",
                Size      = new Size(btnW, 26),
                Location  = new Point(fieldX + fieldW + 4, y),
                BackColor = Color.FromArgb(59, 130, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 8.5f)
            };
            btnPickStaff.FlatAppearance.BorderSize = 0;
            btnPickStaff.Click += BtnPickStaff_Click;
            innerCard.Controls.Add(btnPickStaff);
            y += rowH;

            // ── Return Date ──
            innerCard.Controls.Add(MakeLabel("Return Date: *", y));
            dtpReturnDate = new DateTimePicker
            {
                Location = new Point(fieldX, y),
                Size     = new Size(fieldW + btnW + 4, 26),
                Format   = DateTimePickerFormat.Short,
                Value    = DateTime.Today
            };
            innerCard.Controls.Add(dtpReturnDate);
            y += rowH;

            // ── Refund Amount ──
            innerCard.Controls.Add(MakeLabel("Refund Amount: *", y));
            txtRefundAmount = new TextBox
            {
                Location        = new Point(fieldX, y),
                Size            = new Size(fieldW + btnW + 4, 26),
                PlaceholderText = "0.00"
            };
            innerCard.Controls.Add(txtRefundAmount);
            y += rowH;

            // ── Status ──
            innerCard.Controls.Add(MakeLabel("Status: *", y));
            cmbStatus = new ComboBox
            {
                Location      = new Point(fieldX, y),
                Size          = new Size(fieldW + btnW + 4, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatus.Items.AddRange(new[] { "Pending", "Processing", "Refunded", "Rejected" });
            cmbStatus.SelectedIndex = 0;
            innerCard.Controls.Add(cmbStatus);
            y += rowH;

            // ── Reason ──
            innerCard.Controls.Add(MakeLabel("Reason:", y));
            txtReason = new TextBox
            {
                Location   = new Point(fieldX, y),
                Size       = new Size(fieldW + btnW + 4, 52),
                Multiline  = true,
                ScrollBars = ScrollBars.Vertical
            };
            innerCard.Controls.Add(txtReason);

            // ── action buttons ──
            btnSave = new Button
            {
                Text      = "Save",
                Size      = new Size(100, 36),
                Location  = new Point(384, 474),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            outerCard.Controls.Add(btnSave);

            btnCancel = new Button
            {
                Text      = "Cancel",
                Size      = new Size(100, 36),
                Location  = new Point(492, 474),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            outerCard.Controls.Add(btnCancel);
        }

        private void BtnPickOrder_Click(object sender, EventArgs e)
        {
            using (var picker = new OrderPickerForm(_orderList))
            {
                if (picker.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedOrderID = picker.SelectedOrderID;
                    txtOrderID.Text  = picker.SelectedOrderID;
                    txtCustomer.Text = picker.SelectedCustomer;
                    if (string.IsNullOrWhiteSpace(txtRefundAmount.Text))
                        txtRefundAmount.Text = picker.SelectedGrandTotal.ToString("N2");
                }
            }
        }

        private void BtnPickStaff_Click(object sender, EventArgs e)
        {
            using (var picker = new StaffPickerForm(_staffList))
            {
                if (picker.ShowDialog(this) == DialogResult.OK)
                {
                    _selectedStaffID   = picker.SelectedStaffID;
                    _selectedStaffName = picker.SelectedStaffName;
                    txtHandedBy.Text   = $"{picker.SelectedStaffName} ({picker.SelectedStaffID})";
                }
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
            if (!double.TryParse(txtRefundAmount.Text, out double refund) || refund < 0)
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

            bool ok = _ctrl.CreateReturnOrder(entity);
            if (ok)
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
