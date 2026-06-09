using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// Logistics Processing — View Shipment
    ///
    /// MVC contract
    /// ─────────────────────────────────────────────────────────────────
    /// • All DB access delegated to LogisticsProcessingController (zero SQL here).
    /// • AppShell wired in Load — identical to ViewOrderForm.ViewOrderForm_Load.
    /// • CardPanel three-layer nesting: grey outer → white card → content.
    /// • KPI pills + two action buttons mirror ViewOrderForm layout exactly.
    /// • ShowDetailDialog fully rewritten to match ViewOrderForm.ShowDetailDialog:
    ///     – Size 2500 × 1100, Percent-based column widths, multiline address labels.
    /// </summary>
    public partial class ViewShipmentForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private List<ShipmentEntity> _currentShipments = new List<ShipmentEntity>();
        private ShipmentDetailVM     _selectedDetail;

        // ── Status colour palette (matches schema ENUM values) ────────────
        private static readonly Dictionary<string, (Color bg, Color fg)> StatusColors =
            new Dictionary<string, (Color, Color)>
            {
                { "Pending",    (Color.FromArgb(254, 243, 199), Color.FromArgb(146,  64,  14)) },
                { "In Transit", (Color.FromArgb(219, 234, 254), Color.FromArgb( 29,  78, 216)) },
                { "Completed",  (Color.FromArgb(209, 250, 229), Color.FromArgb(  6,  95,  70)) },
            };

        public ViewShipmentForm()
        {
            InitializeComponent();
            this.Load += ViewShipmentForm_Load;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Load
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ViewShipmentForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            RefreshGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid refresh
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void RefreshGrid()
        {
            string shipNo    = txtSearchShipmentNo.Text.Trim();
            string customer  = txtSearchCustomer.Text.Trim();
            string statusSel = cboStatus.SelectedItem?.ToString();

            string statusFilter = (statusSel == "All" || string.IsNullOrEmpty(statusSel))
                                  ? null : statusSel;
            DateTime? dateFrom = chkDateFrom.Checked ? (DateTime?)dtpDateFrom.Value.Date : null;

            string keyword = !string.IsNullOrEmpty(shipNo)   ? shipNo
                           : !string.IsNullOrEmpty(customer) ? customer
                           : null;

            var vm = _ctrl.GetViewShipmentVM(statusFilter, keyword, dateFrom);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Logistics Processing  ›  View Shipment");

            _currentShipments = vm.Shipments;
            _selectedDetail   = null;

            dgvShipments.Rows.Clear();
            foreach (var s in _currentShipments)
                dgvShipments.Rows.Add(
                    s.ShipmentID,
                    s.OrderID,
                    s.CustomerName,
                    s.TrackingNumber,
                    s.ShipDate.ToString("yyyy-MM-dd"),
                    s.ShipmentStatus,
                    s.ShipmentType,
                    s.DeliveryMethod,
                    $"HK$ {s.TotalAmount:N2}");

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  KPI pills
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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

            const int PillW   = 290;
            const int PillH   = 60;
            const int Gap     = 8;
            const int NumColW = 80;

            foreach (var (label, count, fg, bg, filterItem) in pills)
            {
                var pill = new Panel
                {
                    BackColor = bg, Size = new Size(PillW, PillH),
                    Margin = new Padding(0, 0, Gap, 0), Cursor = Cursors.Hand
                };
                pill.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path  = RoundedRect(((Panel)s).ClientRectangle, 8);
                    using var brush = new SolidBrush(((Panel)s).BackColor);
                    e.Graphics.FillPath(brush, path);
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
                EventHandler clickHandler = (s, e) =>
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Action button state
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void UpdateActionButtons()
        {
            bool sel = dgvShipments.SelectedRows.Count > 0;
            btnViewDetail.Enabled      = sel;
            btnGenDeliveryNote.Enabled = sel;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid events
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void dgvShipments_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvShipments.SelectedRows.Count == 0) { _selectedDetail = null; UpdateActionButtons(); return; }
            string id = dgvShipments.SelectedRows[0].Cells["colShipmentID"].Value?.ToString();
            if (string.IsNullOrEmpty(id)) return;
            try   { _selectedDetail = _ctrl.GetShipmentDetail(id); }
            catch { _selectedDetail = null; }
            UpdateActionButtons();
        }

        private void dgvShipments_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvShipments.Columns[e.ColumnIndex].Name != "colStatus" || e.Value == null) return;
            e.FormattingApplied = true;
            if (StatusColors.TryGetValue(e.Value.ToString(), out var c))
            {
                e.CellStyle.ForeColor = c.fg; e.CellStyle.BackColor = c.bg;
                e.CellStyle.SelectionForeColor = c.fg; e.CellStyle.SelectionBackColor = c.bg;
                e.CellStyle.Font      = new Font("Segoe UI", 11f, FontStyle.Bold);
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }
        }

        private void dgvShipments_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) ShowDetailDialog();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Selected shipment ID helper
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private string SelectedShipmentId()
        {
            if (dgvShipments.SelectedRows.Count == 0) return null;
            return dgvShipments.SelectedRows[0].Cells["colShipmentID"].Value?.ToString();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  View Detail button
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnViewDetail_Click(object sender, EventArgs e) => ShowDetailDialog();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  SHIPMENT DETAIL DIALOG
        //  Fully rewritten to match ViewOrderForm.ShowDetailDialog:
        //    • Size 2500 × 1100  (same as ViewOrderForm)
        //    • pnlInfo uses Percent column widths (15 / 35 / 15 / 35)
        //    • Row 3 (shipping address) uses MakeLabelValMultiLine — top-aligned, wraps
        //    • DeliveryNote strip uses same Percent column widths
        //    • All fixed-pixel key columns removed
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowDetailDialog()
        {
            if (_selectedDetail?.Shipment == null)
            {
                MessageBox.Show("Please select a shipment first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var s = _selectedDetail.Shipment;
            StatusColors.TryGetValue(s.ShipmentStatus ?? "", out var sc);

            using var dlg = new Form
            {
                Text            = $"Shipment Detail — {s.ShipmentID}",
                Size            = new Size(2500, 1100),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── Header ────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text = $"Shipment Details  —  {s.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = s.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info panel  (mirrors ViewOrderForm pnlInfo exactly)
            // 4 columns: Key(15%) | Value(35%) | Key(15%) | Value(35%)
            // 5 rows: rows 0-2 & 4 single-line (15% each); row 3 address row (40%)
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 340,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += (o, ev) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                ev.Graphics.DrawLine(pen, 28, ((Panel)o).Height - 1, ((Panel)o).Width - 28, ((Panel)o).Height - 1);
            };

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 5,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));  // left Key
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));  // left Value
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));  // right Key
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));  // right Value

            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 15f)); // row 0
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 15f)); // row 1
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 15f)); // row 2
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 40f)); // row 3 — shipping address
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 15f)); // row 4

            // Left column: Shipment ID | Customer | Ship Date | Shipping Address | Total Amount
            var leftFields = new (string Key, string Val, bool multiLine)[]
            {
                ("Shipment ID",       s.ShipmentID,                            false),
                ("Customer",          s.CustomerName,                          false),
                ("Ship Date",         s.ShipDate.ToString("yyyy-MM-dd"),       false),
                ("Shipping Address",  s.ShippingAddress,                       true ),
                ("Total Amount",      $"HK$ {s.TotalAmount:N2}",               false),
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Key), 0, i);
                tblInfo.Controls.Add(
                    leftFields[i].multiLine
                        ? MakeLabelValMultiLine(leftFields[i].Val)
                        : MakeLabelVal(leftFields[i].Val),
                    1, i);
            }

            // Right column: Order ID | Tracking No. | Ship Type | (address span) | Delivery Method
            var rightFields = new (string Key, string Val, bool multiLine)[]
            {
                ("Order ID",          s.OrderID,          false),
                ("Tracking No.",      s.TrackingNumber,   false),
                ("Ship Type",         s.ShipmentType,     false),
                ("Delivery Method",   s.DeliveryMethod,   false),  // row 3 right — single line
                ("Status",            s.ShipmentStatus,   false),
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Key), 2, i);
                tblInfo.Controls.Add(
                    rightFields[i].multiLine
                        ? MakeLabelValMultiLine(rightFields[i].Val)
                        : MakeLabelVal(rightFields[i].Val),
                    3, i);
            }
            pnlInfo.Controls.Add(tblInfo);

            // ── DeliveryNote strip (conditional, mirrors ViewOrderForm discount bar) ──
            Panel pnlDN = null;
            if (_selectedDetail.DeliveryNote != null)
            {
                var dn = _selectedDetail.DeliveryNote;
                var rs = _selectedDetail.ReplySlip;

                // Height: 1 row = 60px, 2 rows (with reply slip) = 110px
                pnlDN = new Panel
                {
                    Dock      = DockStyle.Top,
                    Height    = rs != null ? 110 : 60,
                    BackColor = rs != null
                                ? Color.FromArgb(209, 250, 229)
                                : Color.FromArgb(254, 243, 199),
                    Padding   = new Padding(28, 0, 28, 0)
                };
                pnlDN.Paint += PaintBottomBorderStatic;

                Color dnFg = rs != null ? Color.FromArgb(6, 95, 70) : Color.FromArgb(146, 64, 14);

                int dnRows = rs != null ? 2 : 1;
                var tblDN = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 6, RowCount = dnRows,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                // 6 columns: K(12%) | V(21.3%) | K(12%) | V(21.3%) | K(12%) | V(21.4%)
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.3f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.3f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.4f));
                for (int r = 0; r < dnRows; r++)
                    tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / dnRows));

                // Row 0: Delivery ID | Delivery Date | Outstanding Qty
                tblDN.Controls.Add(MakeLabelKey("Delivery Note:",  dnFg), 0, 0);
                tblDN.Controls.Add(MakeLabelVal(dn.DeliveryID,     dnFg), 1, 0);
                tblDN.Controls.Add(MakeLabelKey("Delivery Date:",  dnFg), 2, 0);
                tblDN.Controls.Add(MakeLabelVal(dn.DeliveryDate.ToString("yyyy-MM-dd"), dnFg), 3, 0);
                tblDN.Controls.Add(MakeLabelKey("Outstanding Qty:",dnFg), 4, 0);
                tblDN.Controls.Add(MakeLabelVal((dn.OutstandingQty ?? 0).ToString(), dnFg), 5, 0);

                // Row 1 (reply slip, optional)
                if (rs != null)
                {
                    tblDN.Controls.Add(MakeLabelKey("Reply Slip:",    dnFg), 0, 1);
                    tblDN.Controls.Add(MakeLabelVal(rs.SlipID,        dnFg), 1, 1);
                    tblDN.Controls.Add(MakeLabelKey("Received By:",   dnFg), 2, 1);
                    tblDN.Controls.Add(MakeLabelVal(rs.ActualRecipient, dnFg), 3, 1);
                    tblDN.Controls.Add(MakeLabelKey("Received Date:", dnFg), 4, 1);
                    tblDN.Controls.Add(MakeLabelVal(rs.ReceivedDate.ToString("yyyy-MM-dd"), dnFg), 5, 1);
                }

                pnlDN.Controls.Add(tblDN);
            }

            // ── SHIPMENT ITEMS label bar ───────────────────────────────────
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text = "SHIPMENT ITEMS",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            // ── Items grid (identical spec to ViewOrderForm) ───────────────
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLID",  HeaderText = "LINE ID",          FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem", HeaderText = "ITEM ID",          FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName", HeaderText = "ITEM NAME",        FillWeight = 42 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",  HeaderText = "QTY SHIPPED",      FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cOut",  HeaderText = "QTY OUTSTANDING",  FillWeight = 13 });
            foreach (var line in _selectedDetail.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding ?? 0);

            // ── Total row (mirrors ViewOrderForm Grand Total row) ──────────
            var pnlTotalRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(28, 0, 28, 0)
            };
            pnlTotalRow.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {s.TotalAmount:N2}",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = false
            });

            // ── Footer ────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            pnlFooter.Paint += PaintTopBorderStatic;
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (o, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            // ── Assemble (DockStyle.Top added bottom-to-top) ───────────────
            dlg.Controls.Add(dgv);           // Fill — expands to remaining space
            dlg.Controls.Add(pnlTotalRow);   // Bottom
            dlg.Controls.Add(pnlLineLabel);  // Top (last added = nearest to Fill)
            if (pnlDN != null)
                dlg.Controls.Add(pnlDN);    // Top
            dlg.Controls.Add(pnlInfo);       // Top
            dlg.Controls.Add(pnlHeader);     // Top (first)
            dlg.Controls.Add(pnlFooter);     // Bottom
            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Delivery Note / Reply Slip document dialog
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnGenDeliveryNote_Click(object sender, EventArgs e)
        {
            if (_selectedDetail?.Shipment == null)
            {
                MessageBox.Show("Please select a shipment first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_selectedDetail.DeliveryNote == null)
            {
                MessageBox.Show(
                    "No Delivery Note is linked to this shipment yet.\n" +
                    "A Delivery Note is issued when the shipment is dispatched.",
                    "No Delivery Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ShowDeliveryDocDialog(_selectedDetail);
        }

        private void ShowDeliveryDocDialog(ShipmentDetailVM d)
        {
            var s  = d.Shipment;
            var dn = d.DeliveryNote;
            var rs = d.ReplySlip;

            bool received = rs != null;

            Color headerBg = received ? Color.FromArgb(5, 95, 70) : Color.FromArgb(19, 35, 61);
            string badge   = received ? "RECEIVED" : "PENDING";
            Color badgeFg  = received ? Color.FromArgb(6, 95, 70)    : Color.FromArgb(146, 64, 14);
            Color badgeBg  = received ? Color.FromArgb(209, 250, 229) : Color.FromArgb(254, 243, 199);

            int dlgH = received ? 820 : 660;

            using var dlg = new Form
            {
                Text = $"Delivery Note — {dn.DeliveryID}",
                Size = new Size(1060, dlgH),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White, Font = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            // Header
            var pnlH = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = headerBg };
            var tblH = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text = $"Delivery Note  —  {dn.DeliveryID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblH.Controls.Add(new Label
            {
                Text = badge, Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = badgeFg, BackColor = badgeBg,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlH.Controls.Add(tblH);

            // Delivery Note info
            var pnlDNInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 195,
                Padding = new Padding(28, 14, 28, 8), BackColor = Color.White
            };
            pnlDNInfo.Paint += PaintBottomBorderStatic;

            var tblDN = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 4; r++) tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            AddDetailRow(tblDN, 0, "Delivery ID:",     dn.DeliveryID,                       "Shipment ID:",   s.ShipmentID);
            AddDetailRow(tblDN, 1, "Ship To:",         dn.ShipToName,                       "Delivery Date:", dn.DeliveryDate.ToString("yyyy-MM-dd"));
            AddDetailRow(tblDN, 2, "Ship Address:",    dn.ShippingAddress,                  "Tracking No.:",  s.TrackingNumber);
            AddDetailRow(tblDN, 3, "Outstanding Qty:", (dn.OutstandingQty ?? 0).ToString(), "Method:",        s.DeliveryMethod);
            pnlDNInfo.Controls.Add(tblDN);

            // Reply Slip section (optional)
            Panel pnlRS = null;
            if (received)
            {
                pnlRS = new Panel
                {
                    Dock = DockStyle.Top, Height = 120,
                    BackColor = Color.FromArgb(240, 253, 244),
                    Padding = new Padding(28, 10, 28, 8)
                };
                pnlRS.Paint += PaintBottomBorderStatic;

                pnlRS.Controls.Add(new Label
                {
                    Text = "REPLY SLIP — RECEIPT CONFIRMATION",
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(6, 95, 70),
                    Dock = DockStyle.Top, Height = 30,
                    TextAlign = ContentAlignment.MiddleLeft
                });

                var tblRS = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
                tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
                tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
                tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
                tblRS.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
                tblRS.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

                Color rsFg = Color.FromArgb(6, 95, 70);
                AddDetailRow(tblRS, 0, "Slip ID:",   rs.SlipID,           "Received Date:", rs.ReceivedDate.ToString("yyyy-MM-dd"), rsFg);
                AddDetailRow(tblRS, 1, "Recipient:", rs.ActualRecipient,  "Remark:",        rs.RecipientRemark ?? "(No remark)",   rsFg);
                pnlRS.Controls.Add(tblRS);
            }

            // Lines
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text = "SHIPMENT ITEMS",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236), Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding = new Padding(12, 6, 12, 6)
                }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLID",  HeaderText = "LINE ID",         FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem", HeaderText = "ITEM ID",         FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName", HeaderText = "ITEM NAME",       FillWeight = 42 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",  HeaderText = "QTY SHIPPED",     FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cOut",  HeaderText = "QTY OUTSTANDING", FillWeight = 13 });
            foreach (var line in d.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding ?? 0);

            var pnlTotalRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(0, 0, 28, 0)
            };
            pnlTotalRow.Controls.Add(new Label
            {
                Text = $"Total Amount:   HK$ {s.TotalAmount:N2}",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight
            });

            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 10, 28, 10)
            };
            pnlFooter.Paint += PaintTopBorderStatic;
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (o, ev) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            if (pnlRS != null) dlg.Controls.Add(pnlRS);
            dlg.Controls.Add(pnlDNInfo);
            dlg.Controls.Add(pnlH);
            dlg.Controls.Add(pnlFooter);
            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Nav / Logout
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            SessionManager.Clear();
            Application.Restart();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Static dialog helpers  (identical signature to ViewOrderForm)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static void AddDetailRow(
            TableLayoutPanel tbl, int row,
            string key1, string val1, string key2, string val2,
            Color? fg = null)
        {
            tbl.Controls.Add(MakeLabelKey(key1, fg), 0, row);
            tbl.Controls.Add(MakeLabelVal(val1, fg), 1, row);
            tbl.Controls.Add(MakeLabelKey(key2, fg), 2, row);
            tbl.Controls.Add(MakeLabelVal(val2, fg), 3, row);
        }

        // Bold grey key label — no ellipsis (matches ViewOrderForm.MakeLabelKey)
        private static Label MakeLabelKey(string text, Color? fg = null) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor    = fg ?? Color.FromArgb(98, 112, 135),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            Padding      = new Padding(0, 0, 8, 0),
            AutoEllipsis = false
        };

        // Single-line value label (matches ViewOrderForm.MakeLabelVal)
        private static Label MakeLabelVal(string text, Color? fg = null) => new Label
        {
            Text         = text ?? "—",
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = fg ?? Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        // Multi-line value label for address fields — top-aligned, wraps
        // (matches ViewOrderForm.MakeLabelValMultiLine)
        private static Label MakeLabelValMultiLine(string text) => new Label
        {
            Text         = text ?? "—",
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.TopLeft,
            AutoEllipsis = false,
            AutoSize     = false,
            Padding      = new Padding(0, 8, 8, 4)
        };

        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
