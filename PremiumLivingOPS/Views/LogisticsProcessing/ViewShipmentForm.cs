using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Services;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Logistics Processing — View Shipment
    ///
    /// KPI bar button toggle rules
    /// ──────────────────────────────────────────────
    /// btnGenDeliveryNote:
    ///   • No row selected         → disabled, green  "📄  Delivery Note"
    ///   • Row, DN == null          → enabled,  green  "📄  Delivery Note"   (Generate)
    ///   • Row, DN != null          → enabled,  blue   "👁  View Del. Note" (View)
    ///
    /// btnGenReplySlip:
    ///   • No row selected             → disabled, green  "🧾  Reply Slip"
    ///   • Row, DN == null              → disabled, green  "🧾  Reply Slip"
    ///   • Row, DN != null, RS == null  → enabled,  green  "🧾  Reply Slip"     (Generate)
    ///   • Row, DN != null, RS != null  → enabled,  blue   "👁  View Reply Slip" (View)
    /// </summary>
    public partial class ViewShipmentForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private List<ShipmentEntity> _currentShipments = new List<ShipmentEntity>();
        private ShipmentDetailVM     _selectedDetail;

        private bool _dnBtnIsView = false;
        private bool _rsBtnIsView = false;

        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
            };

        private static readonly Color GreenNorm  = Color.FromArgb( 22, 163,  74);
        private static readonly Color GreenHover = Color.FromArgb( 16, 131,  58);
        private static readonly Color GreenDown  = Color.FromArgb( 10, 100,  40);
        private static readonly Color BlueNorm   = Color.FromArgb( 47, 111, 237);
        private static readonly Color BlueHover  = Color.FromArgb( 26,  77, 192);
        private static readonly Color BlueDown   = Color.FromArgb( 21,  60, 155);

        public ViewShipmentForm()
        {
            InitializeComponent();
            this.Load += ViewShipmentForm_Load;
        }

        private void ViewShipmentForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            RefreshGrid();
        }

        // ── Grid refresh
        private void RefreshGrid()
        {
            string shipNo    = txtSearchShipmentNo.Text.Trim();
            string customer  = txtSearchCustomer.Text.Trim();
            string statusSel = cboStatus.SelectedItem?.ToString();
            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel)) ? null : statusSel;
            DateTime? dateFrom = chkDateFrom.Checked ? (DateTime?)dtpDateFrom.Value.Date : null;
            string keyword = !string.IsNullOrEmpty(shipNo) ? shipNo
                           : !string.IsNullOrEmpty(customer) ? customer : null;

            var vm = _ctrl.GetViewShipmentVM(statusFilter, keyword, dateFrom);
            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Logistics Processing  \u203A  View Shipment");

            _currentShipments = vm.Shipments;
            _selectedDetail   = null;

            dgvShipments.Rows.Clear();
            foreach (var s in _currentShipments)
                dgvShipments.Rows.Add(
                    s.ShipmentID, s.OrderID, s.CustomerName,
                    s.ShipDate.ToString("yyyy-MM-dd"),
                    s.ShipmentStatus, $"HK$ {s.TotalAmount:N2}");

            RefreshKpi();
            UpdateActionButtons();
        }

        private void ResetFilters()
        {
            txtSearchShipmentNo.Text = string.Empty;
            txtSearchCustomer.Text   = string.Empty;
            cboStatus.SelectedIndex  = 0;
            chkDateFrom.Checked      = false;
            dtpDateFrom.Value        = DateTime.Today.AddMonths(-1);
            dtpDateFrom.Enabled      = false;
            RefreshGrid();
        }

        // ── KPI pills
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();
            var all = _ctrl.GetViewShipmentVM().Shipments;
            int total     = all.Count;
            int pending   = all.FindAll(s => s.ShipmentStatus == "Pending").Count;
            int inTransit = all.FindAll(s => s.ShipmentStatus == "In Transit").Count;
            int completed = all.FindAll(s => s.ShipmentStatus == "Completed").Count;

            var pills = new[]
            {
                ("Total",      total.ToString(),     Color.FromArgb( 47, 111, 237), Color.FromArgb(219, 234, 254), "All"),
                ("Pending",    pending.ToString(),   Color.FromArgb(146,  64,  14), Color.FromArgb(254, 243, 199), "Pending"),
                ("In Transit", inTransit.ToString(), Color.FromArgb( 29,  78, 216), Color.FromArgb(219, 234, 254), "In Transit"),
                ("Completed",  completed.ToString(), Color.FromArgb(  6,  95,  70), Color.FromArgb(209, 250, 229), "Completed"),
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0), AutoScroll = false
            };
            const int PillW = 290, PillH = 60, Gap = 8, NumColW = 80;

            foreach (var (label, count, fg, bg, filterItem) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg, Size = new Size(PillW, PillH),
                    Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand
                };
                pill.Paint += (s, ev) =>
                {
                    ev.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    ev.Graphics.FillPath(brush, path);
                };
                var tlp = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(10, 0, 8, 0)
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tlp.Controls.Add(new Label
                {
                    Text = count, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
                }, 0, 0);
                tlp.Controls.Add(new Label
                {
                    Text = label, Font = new Font("Segoe UI", 12f),
                    ForeColor = fg, BackColor = Color.Transparent,
                    Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                }, 1, 0);
                string localFilterItem = filterItem;
                EventHandler clickHandler = (s, ev) =>
                {
                    int idx = cboStatus.FindStringExact(localFilterItem);
                    if (idx >= 0) cboStatus.SelectedIndex = idx;
                    RefreshGrid();
                };
                pill.Click += clickHandler; tlp.Click += clickHandler;
                foreach (Control c in tlp.Controls) c.Click += clickHandler;
                pill.Controls.Add(tlp);
                flow.Controls.Add(pill);
            }
            pnlKpi.Controls.Add(flow);
        }

        // ── Selection + action button state
        private void dgvShipments_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvShipments.SelectedRows.Count == 0)
            { _selectedDetail = null; UpdateActionButtons(); return; }
            string id = dgvShipments.SelectedRows[0].Cells["colShipmentID"].Value?.ToString();
            _selectedDetail = string.IsNullOrEmpty(id) ? null : _ctrl.GetShipmentDetail(id);
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            bool hasRow = _selectedDetail != null;
            bool hasDN  = hasRow && _selectedDetail.DeliveryNote != null;
            bool hasRS  = hasRow && _selectedDetail.ReplySlip    != null;
            string status = _selectedDetail?.Shipment?.ShipmentStatus ?? string.Empty;

            btnViewDetail.Enabled       = hasRow;
            btnModify.Enabled           = hasRow;
            btnScheduleShipment.Enabled = hasRow && status != "Completed";

            btnGenDeliveryNote.Enabled = hasRow;
            if (hasDN)
            {
                if (!_dnBtnIsView)
                {
                    btnGenDeliveryNote.Text = "\U0001F441  View Del. Note";
                    ApplyBtnColour(btnGenDeliveryNote, BlueNorm, BlueHover, BlueDown);
                    _dnBtnIsView = true;
                }
            }
            else
            {
                if (_dnBtnIsView)
                {
                    btnGenDeliveryNote.Text = "\U0001F4C4  Delivery Note";
                    ApplyBtnColour(btnGenDeliveryNote, GreenNorm, GreenHover, GreenDown);
                    _dnBtnIsView = false;
                }
            }

            if (!hasRow)
            {
                btnGenReplySlip.Enabled = false;
                if (_rsBtnIsView)
                {
                    btnGenReplySlip.Text = "\U0001F9FE  Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, GreenNorm, GreenHover, GreenDown);
                    _rsBtnIsView = false;
                }
            }
            else if (hasRS)
            {
                btnGenReplySlip.Enabled = true;
                if (!_rsBtnIsView)
                {
                    btnGenReplySlip.Text = "\U0001F441  View Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, BlueNorm, BlueHover, BlueDown);
                    _rsBtnIsView = true;
                }
            }
            else if (hasDN)
            {
                btnGenReplySlip.Enabled = true;
                if (_rsBtnIsView)
                {
                    btnGenReplySlip.Text = "\U0001F9FE  Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, GreenNorm, GreenHover, GreenDown);
                    _rsBtnIsView = false;
                }
            }
            else
            {
                btnGenReplySlip.Enabled = false;
                if (_rsBtnIsView)
  