using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Auth;
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
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
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
                pill.Click  += clickHandler;
                tlp.Click   += clickHandler;
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

            btnGenReplySlip.Enabled = hasDN;
            if (hasRS)
            {
                if (!_rsBtnIsView)
                {
                    btnGenReplySlip.Text = "\U0001F441  View Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, BlueNorm, BlueHover, BlueDown);
                    _rsBtnIsView = true;
                }
            }
            else
            {
                if (_rsBtnIsView)
                {
                    btnGenReplySlip.Text = "\U0001F9FE  Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, GreenNorm, GreenHover, GreenDown);
                    _rsBtnIsView = false;
                }
            }
        }

        private static void ApplyBtnColour(Button btn, Color norm, Color hover, Color down)
        {
            btn.BackColor = norm;
            btn.FlatAppearance.MouseOverBackColor = hover;
            btn.FlatAppearance.MouseDownBackColor = down;
        }

        // ── Button click handlers
        private void btnSearch_Click(object sender, EventArgs e)    => RefreshGrid();
        private void btnReset_Click(object sender, EventArgs e)     => ResetFilters();
        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        private void chkDateFrom_CheckedChanged(object sender, EventArgs e)
            => dtpDateFrom.Enabled = chkDateFrom.Checked;

        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            ShowViewDetailDialog(_selectedDetail);
        }

        private void btnGenDeliveryNote_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            if (_dnBtnIsView)
                ShowViewDeliveryNoteDialog(_selectedDetail);
            else
                ShowGenerateDeliveryNoteDialog(_selectedDetail);
        }

        private void btnGenReplySlip_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            if (_rsBtnIsView)
                ShowViewReplySlipDialog(_selectedDetail);
            else
                ShowGenerateReplySlipDialog(_selectedDetail);
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            using var dlg = new ModifyShipmentDialog(_ctrl, _selectedDetail);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        private void btnScheduleShipment_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            using var dlg = new ScheduleShipmentDialog(_ctrl);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                RefreshGrid();
        }

        private void OnTopNavMenuItemClicked(object sender, string tag)
        {
            // Navigation handled by AppShell
        }

        // ── Dialog builders
        private void ShowViewDetailDialog(ShipmentDetailVM detail)
        {
            var s     = detail.Shipment;
            var lines = detail.Lines ?? new List<ShipmentLineEntity>();

            var dlg = new Form
            {
                Text = $"Shipment Details \u2014 {s.ShipmentID}",
                Size = new Size(2500, 1000),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblH = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text = $"Shipment Details  \u2014  {s.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblH.Controls.Add(new Label
            {
                Text = s.ShipmentStatus,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70), BackColor = Color.FromArgb(209, 250, 229),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlHeader.Controls.Add(tblH);

            // Info rows
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 280,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = Build4ColTlp(5);
            AddInfoRow(tblInfo, 0, "Shipment ID:",  s.ShipmentID,                         "Order ID:",        s.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",     s.CustomerName,                        "Tracking No.:",    s.TrackingNumber ?? "\u2014");
            AddInfoRow(tblInfo, 2, "Ship Date:",    s.ShipDate.ToString("yyyy-MM-dd"),     "Delivery Method:", s.DeliveryMethod);
            AddInfoRow(tblInfo, 3, "Status:",       s.ShipmentStatus,                      "Ship Type:",       s.ShipmentType);
            // Address spans full width (multi-line)
            tblInfo.Controls.Add(MakeLabelKey("Address:"),                      0, 4);
            tblInfo.Controls.Add(MakeLabelValMultiLine(s.ShippingAddress ?? "\u2014"), 1, 4);
            tblInfo.SetColumnSpan(tblInfo.GetControlFromPosition(1, 4), 3);
            pnlInfo.Controls.Add(tblInfo);

            // Items section label
            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");

            // Items grid
            var dgv = BuildItemsGrid();
            foreach (var ln in lines)
                dgv.Rows.Add(ln.ShipmentLineID, ln.ItemID, ln.ItemName, ln.QtyShipped, ln.QtyOutstanding?.ToString() ?? "\u2014");

            // Total row
            var pnlTotal = BuildTotalRow(lines.Count, (double)s.TotalAmount);

            // Footer
            var pnlFooter = BuildCloseFooter(dlg);

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotal);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);

            dlg.ShowDialog(this);
        }

        private void ShowViewDeliveryNoteDialog(ShipmentDetailVM detail)
        {
            var s   = detail.Shipment;
            var dn  = detail.DeliveryNote;
            var lines = detail.Lines ?? new List<ShipmentLineEntity>();

            var dlg = new Form
            {
                Text = $"Delivery Note \u2014 {dn?.DeliveryID}",
                Size = new Size(2500, 1100),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblH = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text = $"Delivery Note \u2014 {dn?.DeliveryID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblH.Controls.Add(new Label
            {
                Text = s.ShipmentStatus,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70), BackColor = Color.FromArgb(209, 250, 229),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlHeader.Controls.Add(tblH);

            // Shipment info
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = Build4ColTlp(4);
            AddInfoRow(tblInfo, 0, "Shipment ID:",  s.ShipmentID,                         "Order ID:",        s.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",     s.CustomerName,                        "Tracking No.:",    s.TrackingNumber ?? "\u2014");
            AddInfoRow(tblInfo, 2, "Ship Date:",    s.ShipDate.ToString("yyyy-MM-dd"),     "Delivery Method:", s.DeliveryMethod);
            AddInfoRow(tblInfo, 3, "Status:",       s.ShipmentStatus,                      "Ship Type:",       s.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            // DN title bar
            var pnlDNTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 253, 244), Padding = new Padding(28, 0, 16, 0)
            };
            pnlDNTitle.Paint += PaintBottomBorderStatic;
            pnlDNTitle.Controls.Add(new Label
            {
                Text      = "\U0001F4C4  Delivery Note Details",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // DN body — 18/32/18/32 column split, multi-line Ship Address
            // Height matches Reply Slip body (380px) so the Ship Address row
            // has the same vertical space to wrap long text.
            var pnlDNBody = new Panel
            {
                Dock = DockStyle.Top, Height = 380,
                BackColor = Color.FromArgb(249, 254, 251), Padding = new Padding(28, 12, 28, 12)
            };
            pnlDNBody.Paint += PaintBottomBorderStatic;
            var tblDN = Build4ColTlp(3, 18f, 18f);
            AddInfoRow(tblDN, 0, "Delivery ID:", dn?.DeliveryID, "Delivery Date:", dn?.DeliveryDate.ToString("yyyy-MM-dd"));
            tblDN.Controls.Add(MakeLabelKey("Ship Address:"),                       0, 1);
            tblDN.Controls.Add(MakeLabelValMultiLine(s.ShippingAddress ?? "\u2014"), 1, 1);
            tblDN.Controls.Add(MakeLabelKey("Ship To:"),                             2, 1);
            tblDN.Controls.Add(MakeLabelVal(s.CustomerName),                        3, 1);
            AddInfoRow(tblDN, 2, "Total Amount:", $"HK$ {s.TotalAmount:N2}", "Ship Type:", s.ShipmentType);
            pnlDNBody.Controls.Add(tblDN);

            // Items
            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");
            var dgv          = BuildItemsGrid();
            foreach (var ln in lines)
                dgv.Rows.Add(ln.ShipmentLineID, ln.ItemID, ln.ItemName, ln.QtyShipped, ln.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotal  = BuildTotalRow(lines.Count, (double)s.TotalAmount);
            var pnlFooter = BuildCloseFooter(dlg);

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotal);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlDNBody);
            dlg.Controls.Add(pnlDNTitle);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);

            dlg.ShowDialog(this);
        }

        private void ShowViewReplySlipDialog(ShipmentDetailVM detail)
        {
            var s  = detail.Shipment;
            var rs = detail.ReplySlip;
            var lines = detail.Lines ?? new List<ShipmentLineEntity>();

            var dlg = new Form
            {
                Text = $"Reply Slip \u2014 {rs?.SlipID}",
                Size = new Size(2500, 1100),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblH = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text = $"Reply Slip \u2014 {rs?.SlipID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblH.Controls.Add(new Label
            {
                Text = s.ShipmentStatus,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70), BackColor = Color.FromArgb(209, 250, 229),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlHeader.Controls.Add(tblH);

            // Shipment info
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = Build4ColTlp(4);
            AddInfoRow(tblInfo, 0, "Shipment ID:",  s.ShipmentID,                         "Order ID:",        s.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",     s.CustomerName,                        "Tracking No.:",    s.TrackingNumber ?? "\u2014");
            AddInfoRow(tblInfo, 2, "Ship Date:",    s.ShipDate.ToString("yyyy-MM-dd"),     "Delivery Method:", s.DeliveryMethod);
            AddInfoRow(tblInfo, 3, "Status:",       s.ShipmentStatus,                      "Ship Type:",       s.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            // RS title bar
            var pnlSlipTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(239, 246, 255), Padding = new Padding(28, 0, 16, 0)
            };
            pnlSlipTitle.Paint += PaintBottomBorderStatic;
            pnlSlipTitle.Controls.Add(new Label
            {
                Text      = "\U0001F9FE  Reply Slip Details",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // RS body — 18/32/18/32 column split, multi-line Ship Address
            var pnlSlipBody = new Panel
            {
                Dock = DockStyle.Top, Height = 380,
                BackColor = Color.FromArgb(249, 251, 255), Padding = new Padding(28, 12, 28, 12)
            };
            pnlSlipBody.Paint += PaintBottomBorderStatic;
            var tblSlip = Build4ColTlp(3, 18f, 18f);
            AddInfoRowStatic(tblSlip, 0, "Reply Slip ID:", rs?.SlipID, "Received Date:", rs?.ReceivedDate.ToString("yyyy-MM-dd"));
            tblSlip.Controls.Add(MakeLabelKeyStatic("Ship Address:"),                             0, 1);
            tblSlip.Controls.Add(MakeLabelValMultiLineStatic(s.ShippingAddress ?? "\u2014"),      1, 1);
            tblSlip.Controls.Add(MakeLabelKeyStatic("Recipient:"),                                2, 1);
            tblSlip.Controls.Add(MakeLabelValStatic(rs?.ActualRecipient ?? "\u2014"),            3, 1);
            AddInfoRowStatic(tblSlip, 2, "Remark:", rs?.RecipientRemark ?? "\u2014", "Total Amount:", $"HK$ {s.TotalAmount:N2}");
            pnlSlipBody.Controls.Add(tblSlip);

            // Items
            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");
            var dgv          = BuildItemsGrid();
            foreach (var ln in lines)
                dgv.Rows.Add(ln.ShipmentLineID, ln.ItemID, ln.ItemName, ln.QtyShipped, ln.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotal  = BuildTotalRow(lines.Count, (double)s.TotalAmount);
            var pnlFooter = BuildCloseFooter(dlg);

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotal);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlSlipBody);
            dlg.Controls.Add(pnlSlipTitle);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);

            dlg.ShowDialog(this);
        }

        private void ShowGenerateDeliveryNoteDialog(ShipmentDetailVM detail)
        {
            var s     = detail.Shipment;
            var lines = detail.Lines ?? new List<ShipmentLineEntity>();

            var dlg = new Form
            {
                Text = $"Generate Delivery Note \u2014 {s.ShipmentID}",
                Size = new Size(2500, 1000),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblH = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text = $"Generate Delivery Note  \u2014  {s.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblH.Controls.Add(new Label
            {
                Text = s.ShipmentStatus,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70), BackColor = Color.FromArgb(209, 250, 229),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlHeader.Controls.Add(tblH);

            // Info rows
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = Build4ColTlp(4);
            AddInfoRowStatic(tblInfo, 0, "Shipment ID:",  s.ShipmentID,                         "Order ID:",        s.OrderID);
            AddInfoRowStatic(tblInfo, 1, "Customer:",     s.CustomerName,                        "Tracking No.:",    s.TrackingNumber ?? "\u2014");
            AddInfoRowStatic(tblInfo, 2, "Ship Date:",    s.ShipDate.ToString("yyyy-MM-dd"),     "Delivery Method:", s.DeliveryMethod);
            AddInfoRowStatic(tblInfo, 3, "Status:",       s.ShipmentStatus,                      "Ship Type:",       s.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            // Items
            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");
            var dgv          = BuildItemsGrid();
            foreach (var ln in lines)
                dgv.Rows.Add(ln.ShipmentLineID, ln.ItemID, ln.ItemName, ln.QtyShipped, ln.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotal = BuildTotalRow(lines.Count, (double)s.TotalAmount);

            // Footer with Generate button
            const int BtnH = 60;
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 90,
                BackColor = Color.White, Padding = new Padding(28, 15, 28, 15)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnGen = new Button
            {
                Text      = "\u2714  Generate Delivery Note",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat, Size = new Size(260, BtnH), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnGen.FlatAppearance.BorderSize         = 0;
            btnGen.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 131, 58);
            btnGen.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 100, 40);

            var btnClose2 = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(160, BtnH), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose2.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose2.FlatAppearance.BorderSize         = 1;
            btnClose2.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose2.Click += (_, __) => dlg.Close();

            const int Gap2 = 16;
            pnlFooter.SizeChanged += (o, ev) =>
            {
                int top2   = (pnlFooter.ClientSize.Height - BtnH) / 2;
                int rEdge2 = pnlFooter.ClientSize.Width - 28;
                btnGen.Location    = new Point(rEdge2 - 260,                top2);
                btnClose2.Location = new Point(rEdge2 - 260 - Gap2 - 160,  top2);
            };
            btnGen.Location    = new Point(2500 - 28 - 260,               (90 - BtnH) / 2);
            btnClose2.Location = new Point(2500 - 28 - 260 - Gap2 - 160, (90 - BtnH) / 2);

            btnGen.Click += (_, __) =>
            {
                try
                {
                    string dnId = _ctrl.GenerateDeliveryNote(s.ShipmentID);
                    MessageBox.Show($"Delivery Note {dnId} generated successfully.",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    RefreshGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to generate Delivery Note:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnlFooter.Controls.Add(btnGen);
            pnlFooter.Controls.Add(btnClose2);

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotal);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);

            dlg.ShowDialog(this);
        }

        private void ShowGenerateReplySlipDialog(ShipmentDetailVM detail)
        {
            var s     = detail.Shipment;
            var dn    = detail.DeliveryNote;
            var rs    = detail.ReplySlip;
            var lines = detail.Lines ?? new List<ShipmentLineEntity>();

            bool rsExists = rs != null;

            var dlg = new Form
            {
                Text = "Generate Reply Slip",
                Size = new Size(2500, 1100),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            // Header
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblH = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text = $"Generate Reply Slip  \u2014  {s.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblH.Controls.Add(new Label
            {
                Text = s.ShipmentStatus,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70), BackColor = Color.FromArgb(209, 250, 229),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlHeader.Controls.Add(tblH);

            // Shipment info
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = Build4ColTlp(4);
            AddInfoRowStatic(tblInfo, 0, "Shipment ID:",  s.ShipmentID,                         "Order ID:",        s.OrderID);
            AddInfoRowStatic(tblInfo, 1, "Customer:",     s.CustomerName,                        "Tracking No.:",    s.TrackingNumber ?? "\u2014");
            AddInfoRowStatic(tblInfo, 2, "Ship Date:",    s.ShipDate.ToString("yyyy-MM-dd"),     "Delivery Method:", s.DeliveryMethod);
            AddInfoRowStatic(tblInfo, 3, "Status:",       s.ShipmentStatus,                      "Ship Type:",       s.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            // RS blue title bar
            var pnlSlipTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(239, 246, 255), Padding = new Padding(28, 0, 16, 0)
            };
            pnlSlipTitle.Paint += PaintBottomBorderStatic;
            pnlSlipTitle.Controls.Add(new Label
            {
                Text      = "\U0001F9FE  Reply Slip Preview",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // RS body — 18/32/18/32 column split, multi-line Ship Address
            var pnlSlipBody = new Panel
            {
                Dock = DockStyle.Top, Height = 380,
                BackColor = Color.FromArgb(249, 251, 255), Padding = new Padding(28, 12, 28, 12)
            };
            pnlSlipBody.Paint += PaintBottomBorderStatic;
            var tblSlip = Build4ColTlp(3, 18f, 18f);
            AddInfoRowStatic(tblSlip, 0, "Delivery ID:", dn?.DeliveryID, "Delivery Date:", dn?.DeliveryDate.ToString("yyyy-MM-dd"));
            tblSlip.Controls.Add(MakeLabelKeyStatic("Ship Address:"),                             0, 1);
            tblSlip.Controls.Add(MakeLabelValMultiLineStatic(s.ShippingAddress ?? "\u2014"),      1, 1);
            tblSlip.Controls.Add(MakeLabelKeyStatic("Ship To:"),                                  2, 1);
            tblSlip.Controls.Add(MakeLabelValStatic(s.CustomerName),                             3, 1);
            AddInfoRowStatic(tblSlip, 2, "Total Amount:", $"HK$ {s.TotalAmount:N2}", "Ship Type:", s.ShipmentType);
            pnlSlipBody.Controls.Add(tblSlip);

            // Warning bar
            var pnlWarn = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = rsExists ? 48 : 0,
                BackColor = Color.FromArgb(255, 251, 235),
                Padding = new Padding(28, 0, 16, 0),
                Visible = rsExists
            };
            pnlWarn.Paint += PaintBottomBorderStatic;
            if (rsExists)
                pnlWarn.Controls.Add(new Label
                {
                    Text      = $"\u26A0  A Reply Slip already exists ({rs.SlipID}). Generating again will overwrite it.",
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(146, 64, 14),
                    Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
                });

            // Items
            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");
            var dgv          = BuildItemsGrid();
            foreach (var ln in lines)
                dgv.Rows.Add(ln.ShipmentLineID, ln.ItemID, ln.ItemName, ln.QtyShipped, ln.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotal = BuildTotalRow(lines.Count, (double)s.TotalAmount);

            // Footer with Generate button
            const int BtnH = 60;
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 90,
                BackColor = Color.White, Padding = new Padding(28, 15, 28, 15)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnGen = new Button
            {
                Text      = "\u2714  Generate Reply Slip",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Size = new Size(240, BtnH), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnGen.FlatAppearance.BorderSize         = 0;
            btnGen.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            btnGen.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 60, 155);

            var btnClose2 = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(160, BtnH), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose2.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose2.FlatAppearance.BorderSize         = 1;
            btnClose2.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose2.Click += (_, __) => dlg.Close();

            const int Gap2 = 16;
            pnlFooter.SizeChanged += (o, ev) =>
            {
                int top2   = (pnlFooter.ClientSize.Height - BtnH) / 2;
                int rEdge2 = pnlFooter.ClientSize.Width - 28;
                btnGen.Location    = new Point(rEdge2 - 240,                top2);
                btnClose2.Location = new Point(rEdge2 - 240 - Gap2 - 160,  top2);
            };
            btnGen.Location    = new Point(2500 - 28 - 240,                (90 - BtnH) / 2);
            btnClose2.Location = new Point(2500 - 28 - 240 - Gap2 - 160,  (90 - BtnH) / 2);

            btnGen.Click += (_, __) =>
            {
                try
                {
                    // Collect actualRecipient (required) and remark (optional) via inline dialog
                    using var inputDlg = new Form
                    {
                        Text = "Generate Reply Slip", Size = new Size(480, 220),
                        StartPosition = FormStartPosition.CenterParent,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        MaximizeBox = false, MinimizeBox = false, BackColor = Color.White
                    };
                    var lblR = new Label { Text = "Actual Recipient:", Left = 24, Top = 24, Width = 160, AutoSize = false, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
                    var txtR = new TextBox { Left = 188, Top = 24, Width = 252, Text = s.CustomerName };
                    var lblM = new Label { Text = "Remark (optional):", Left = 24, Top = 64, Width = 160, AutoSize = false, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
                    var txtM = new TextBox { Left = 188, Top = 64, Width = 252 };
                    var btnOk  = new Button { Text = "Generate", Left = 268, Top = 112, Width = 100, Height = 36, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(47,111,237), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                    var btnCx  = new Button { Text = "Cancel",   Left = 376, Top = 112, Width = 80,  Height = 36, DialogResult = DialogResult.Cancel, FlatStyle = FlatStyle.Flat };
                    btnOk.FlatAppearance.BorderSize = 0;
                    inputDlg.Controls.AddRange(new Control[]{ lblR, txtR, lblM, txtM, btnOk, btnCx });
                    inputDlg.AcceptButton = btnOk; inputDlg.CancelButton = btnCx;
                    if (inputDlg.ShowDialog(dlg) != DialogResult.OK) return;
                    if (string.IsNullOrWhiteSpace(txtR.Text)) { MessageBox.Show("Actual Recipient is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    string rsId = _ctrl.GenerateReplySlip(s.ShipmentID, txtR.Text.Trim(), txtM.Text.Trim());
                    MessageBox.Show($"Reply Slip {rsId} generated successfully.",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    RefreshGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to generate Reply Slip:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnlFooter.Controls.Add(btnGen);
            pnlFooter.Controls.Add(btnClose2);

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotal);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlWarn);
            dlg.Controls.Add(pnlSlipBody);
            dlg.Controls.Add(pnlSlipTitle);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);

            dlg.ShowDialog(this);
        }

        // ── UI builder helpers ──────────────────────────────────────────────────

        private static Panel BuildSectionLabel(string title)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(28, 0, 0, 0)
            };
            pnl.Paint += PaintBottomBorderStatic;
            pnl.Controls.Add(new Label
            {
                Text = title, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });
            return pnl;
        }

        private static DataGridView BuildItemsGrid()
        {
            var dgv = new DataGridView
            {
                Dock = DockStyle.Top, Height = 300,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                GridColor = Color.FromArgb(226, 232, 240),
                RowHeadersVisible = false, AllowUserToAddRows = false,
                ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 36, RowTemplate = { Height = 36 },
                Font = new Font("Segoe UI", 12f),
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(29, 78, 216),
                    Padding = new Padding(4, 0, 4, 0)
                },
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(248, 250, 252), ForeColor = Color.FromArgb(100, 116, 139),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(248, 250, 252),
                    SelectionForeColor = Color.FromArgb(100, 116, 139),
                    Padding = new Padding(4, 0, 4, 0), Alignment = DataGridViewContentAlignment.MiddleLeft
                }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LINE ID",     FillWeight = 16, Name = "colLineID"  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM ID",     FillWeight = 14, Name = "colItemID"  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM NAME",   FillWeight = 34, Name = "colItemNm"  });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY SHIPPED", FillWeight = 18, Name = "colQtyShip" });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OUTSTANDING", FillWeight = 18, Name = "colQtyOut"  });
            return dgv;
        }

        private static Panel BuildTotalRow(int lineCount, double totalAmount)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top, Height = 44, BackColor = Color.White,
                Padding = new Padding(28, 0, 28, 0)
            };
            pnl.Paint += PaintTopBorderStatic;
            var lbl = new Label
            {
                Text = $"  {lineCount} line(s)     Total: HK$ {totalAmount:N2}",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoSize = false
            };
            pnl.Controls.Add(lbl);
            return pnl;
        }

        private static Panel BuildCloseFooter(Form owner)
        {
            const int BtnH = 60;
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 90,
                BackColor = Color.White, Padding = new Padding(28, 15, 28, 15)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(160, BtnH), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (_, __) => owner.Close();

            pnlFooter.SizeChanged += (o, ev) =>
            {
                int top2  = (pnlFooter.ClientSize.Height - BtnH) / 2;
                btnClose.Location = new Point(pnlFooter.ClientSize.Width - 28 - 160, top2);
            };
            btnClose.Location = new Point(2500 - 28 - 160, (90 - BtnH) / 2);

            pnlFooter.Controls.Add(btnClose);
            return pnlFooter;
        }

        /// <summary>
        /// Build a 4-column TableLayoutPanel with percent-based column widths.
        /// Column layout: Key-L (col0%) | Val-L (valW%) | Key-R (col2%) | Val-R (valW%)
        /// where valW = (100 - col0 - col2) / 2.
        ///
        /// Default (col0=16, col2=16): 16% | 34% | 16% | 34%
        /// DN/RS body (col0=18, col2=18): 18% | 32% | 18% | 32%
        /// </summary>
        private static TableLayoutPanel Build4ColTlp(int rows, float col0 = 16f, float col2 = 16f, float unused = 0f)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = rows,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(0)
            };
            float valW = (100f - col0 - col2) / 2f;
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, col0));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, valW));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, col2));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, valW));
            for (int i = 0; i < rows; i++)
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
            return tlp;
        }

        private static Label MakeLabelKey(string text) =>
            new Label
            {
                Text = text, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };

        private static Label MakeLabelVal(string text) =>
            new Label
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            };

        private static Label MakeLabelValMultiLine(string text) =>
            new Label
            {
                Text = text, Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft,
                AutoSize = false, AutoEllipsis = false
            };

        private static void AddInfoRow(TableLayoutPanel tbl, int row,
            string keyL, string valL, string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKey(keyL),             0, row);
            tbl.Controls.Add(MakeLabelVal(valL ?? "\u2014"),  1, row);
            tbl.Controls.Add(MakeLabelKey(keyR),             2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "\u2014"),  3, row);
        }

        // ── Static variants (used in dialogs that don't need instance context) ──

        private static Label MakeLabelKeyStatic(string text) => MakeLabelKey(text);
        private static Label MakeLabelValStatic(string text) => MakeLabelVal(text);
        private static Label MakeLabelValMultiLineStatic(string text) => MakeLabelValMultiLine(text);

        private static void AddInfoRowStatic(TableLayoutPanel tbl, int row,
            string keyL, string valL, string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKeyStatic(keyL),             0, row);
            tbl.Controls.Add(MakeLabelValStatic(valL ?? "\u2014"),  1, row);
            tbl.Controls.Add(MakeLabelKeyStatic(keyR),             2, row);
            tbl.Controls.Add(MakeLabelValStatic(valR ?? "\u2014"),  3, row);
        }

        // ── Designer.cs wired instance event handlers ──────────────────────────
        private void PaintCardBorder(object sender, PaintEventArgs e)
        {
            var ctl  = (Control)sender;
            using var pen  = new Pen(Color.FromArgb(221, 227, 236), 1);
            var rect = new Rectangle(0, 0, ctl.Width - 1, ctl.Height - 1);
            using var path = RoundedRect(rect, 8);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawPath(pen, path);
        }

        private void dgvShipments_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            if (dgvShipments.Columns[e.ColumnIndex].Name != "colStatus") return;

            string status = e.Value.ToString();
            if (StatusColors.TryGetValue(status, out var colors))
            {
                e.CellStyle.BackColor          = colors.bg;
                e.CellStyle.ForeColor          = colors.fg;
                e.CellStyle.SelectionBackColor = colors.bg;
                e.CellStyle.SelectionForeColor = colors.fg;
                e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
            }
            else
            {
                e.CellStyle.ForeColor = Color.FromArgb(15, 31, 53);
            }
            e.FormattingApplied = true;
        }

        private void dgvShipments_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _selectedDetail == null) return;
            ShowViewDetailDialog(_selectedDetail);
        }

        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
