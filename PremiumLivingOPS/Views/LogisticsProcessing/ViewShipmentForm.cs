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
    /// • KPI pills + five action buttons:
    ///     View Details | Modify | Generate Delivery Note | Generate Reply Slip | Schedule Shipment
    /// • Schedule Shipment (210×60, purple):
    ///     – Opens ScheduleShipmentDialog (standalone, 1040×560).
    ///     – Enabled only when a row is selected and status ≠ Completed.
    ///     – On DialogResult.OK: refreshes grid.
    /// • Generate Delivery Note:
    ///     – Opens GenerateDeliveryNoteForm (standalone dialog, 1200×780).
    ///     – Blocked if a Delivery Note already exists for the shipment.
    ///     – On success: refreshes _selectedDetail and opens ShowDeliveryDocDialog.
    /// • Generate Reply Slip:
    ///     – Requires an existing Delivery Note.
    ///     – Blocked if a Reply Slip already exists for that Delivery Note.
    ///     – Full dialog (1400×880) matching ShowDetailDialog visual language.
    /// • ShowDetailDialog is the single entry point for both view AND modify:
    ///     – Edit Shipment: update Status + ActualRecipient + optional Remark.
    ///     – Delete Shipment: permanently removes shipment + child records.
    ///
    /// Grid columns (7 — ShipmentType and DeliveryMethod removed from grid;
    /// both fields remain fully visible inside ShowDetailDialog info panel):
    ///   ShipmentID | OrderID | CustomerName | TrackingNumber |
    ///   ShipDate   | ShipmentStatus | TotalAmount
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Load
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ViewShipmentForm_Load(object sender, EventArgs e)
        {
            _shell.MenuItemClicked += OnTopNavMenuItemClicked;
            _shell.LogoutClicked   += btnLogout_Click;
            RefreshGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid refresh
        //  Rows.Add passes exactly 7 values — one per Designer column:
        //    col 0  colShipmentID  — Shipment.ShipmentID
        //    col 1  colOrderID     — Shipment.OrderID
        //    col 2  colCustomer    — JOIN Customer.CustomerName
        //    col 3  colTracking    — Shipment.TrackingNumber
        //    col 4  colShipDate    — Shipment.ShipDate
        //    col 5  colStatus      — Shipment.ShipmentStatus
        //    col 6  colAmount      — Shipment.TotalAmount
        //  (ShipmentType and DeliveryMethod are shown in ShowDetailDialog only)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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
            _shell.SetBreadcrumb("Logistics Processing  \u203A  View Shipment");

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  KPI pills
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Selection + action button state
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void dgvShipments_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvShipments.SelectedRows.Count == 0)
            {
                _selectedDetail = null;
                UpdateActionButtons();
                return;
            }
            string id = dgvShipments.SelectedRows[0].Cells["colShipmentID"].Value?.ToString();
            _selectedDetail = string.IsNullOrEmpty(id) ? null : _ctrl.GetShipmentDetail(id);
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            bool hasRow = _selectedDetail != null;
            string status = _selectedDetail?.ShipmentStatus ?? "";

            btnViewDetail.Enabled      = hasRow;
            btnModify.Enabled          = hasRow;
            btnGenDeliveryNote.Enabled = hasRow && _selectedDetail?.DeliveryNote == null;
            btnGenReplySlip.Enabled    = hasRow
                && _selectedDetail?.DeliveryNote != null
                && _selectedDetail?.ReplySlip    == null;

            // Schedule Shipment: enabled when a row is selected and not yet Completed
            btnScheduleShipment.Enabled = hasRow && status != "Completed";
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Action handlers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            ShowDetailDialog(_selectedDetail);
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            ShowDetailDialog(_selectedDetail, editMode: true);
        }

        /// <summary>
        /// Opens ScheduleShipmentDialog for the selected shipment.
        /// On DialogResult.OK the grid is refreshed.
        /// </summary>
        private void btnScheduleShipment_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;

            var entity = _currentShipments.Find(
                s => s.ShipmentID == _selectedDetail.ShipmentID);
            if (entity == null) return;

            using var dlg = new ScheduleShipmentDialog(_ctrl, entity);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _selectedDetail = null;
                RefreshGrid();
            }
        }

        private void btnGenDeliveryNote_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            if (_selectedDetail.DeliveryNote != null)
            {
                MessageBox.Show(
                    $"A Delivery Note ({_selectedDetail.DeliveryNote.DeliveryID}) already exists.",
                    "Cannot Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string dnId = _ctrl.GenerateDeliveryNote(_selectedDetail.ShipmentID);
                MessageBox.Show($"Delivery Note {dnId} generated successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _selectedDetail = _ctrl.GetShipmentDetail(_selectedDetail.ShipmentID);
                UpdateActionButtons();
                ShowDeliveryDocDialog(_selectedDetail);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGenReplySlip_Click(object sender, EventArgs e)
        {
            if (_selectedDetail?.DeliveryNote == null)
            {
                MessageBox.Show("Please generate a Delivery Note first.",
                    "Cannot Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_selectedDetail.ReplySlip != null)
            {
                MessageBox.Show(
                    $"A Reply Slip ({_selectedDetail.ReplySlip.SlipID}) already exists.",
                    "Cannot Generate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowGenerateReplySlipDialog(_selectedDetail);
        }

        private void dgvShipments_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _selectedDetail == null) return;
            ShowDetailDialog(_selectedDetail);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Detail dialog (View / Modify / Delete)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowDetailDialog(ShipmentDetailVM s, bool editMode = false)
        {
            bool needsRefresh = false;

            var dlg = new Form
            {
                Text            = $"Shipment Details  —  {s.ShipmentID}",
                Size            = new Size(1200, 780),
                MinimumSize     = new Size(1000, 660),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                Font            = new Font("Segoe UI", 13f)
            };

            // ── Header ────────────────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top, Height = 80,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text = $"Shipment Details  —  {s.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            StatusColors.TryGetValue(s.ShipmentStatus ?? "", out var sc);
            var lblStatusBadge = new Label
            {
                Text      = s.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Padding = new Padding(8, 4, 8, 4)
            };
            tblHeader.Controls.Add(lblStatusBadge, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info panel (outer grey → white card → 4-col TLP) ──────────
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 14, 24, 10)
            };
            for (int c = 0; c < 4; c++)
                tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent,  33f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent,  33f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent,  34f));

            void AddInfoRow(int row, string k1, string v1, string k2, string v2,
                            string k3, string v3, string k4, string v4)
            {
                tblInfo.Controls.Add(MakeLabelKey(k1), 0, row); tblInfo.Controls.Add(MakeLabelVal(v1), 1, row);
                tblInfo.Controls.Add(MakeLabelKey(k2), 2, row); tblInfo.Controls.Add(MakeLabelVal(v2), 3, row);
            }

            AddInfoRow(0,
                "Shipment ID",   s.ShipmentID,
                "Order ID",      s.OrderID,
                "", "", "", "");
            AddInfoRow(1,
                "Customer",      s.CustomerName  ?? "—",
                "Ship Date",     s.ShipDate.ToString("yyyy-MM-dd"),
                "", "", "", "");
            AddInfoRow(2,
                "Delivery",      s.DeliveryMethod ?? "—",
                "Type",          s.ShipmentType   ?? "—",
                "", "", "", "");

            var pnlInfoWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlInfoWhite.Paint += PaintCardBorder;
            pnlInfoWhite.Controls.Add(tblInfo);
            var pnlInfoOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 160,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 8, 20, 0)
            };
            pnlInfoOuter.Controls.Add(pnlInfoWhite);
            var pnlInfo = pnlInfoOuter;

            // ── Delivery Note section (shown if exists) ───────────────────
            Panel pnlDN = null;
            if (s.DeliveryNote != null)
            {
                var dn = s.DeliveryNote;
                var tblDN = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                    Padding = new Padding(24, 10, 24, 10)
                };
                for (int c = 0; c < 4; c++)
                    tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
                tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
                tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

                AddDetailRow(tblDN, 0,
                    "Delivery Note", dn.DeliveryID,
                    "Delivery Date", dn.DeliveryDate.ToString("yyyy-MM-dd"));
                AddDetailRow(tblDN, 1,
                    "Ship-to",       dn.ShipToName      ?? "—",
                    "Address",       dn.ShippingAddress ?? "—");

                var pnlDNWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                pnlDNWhite.Paint += PaintCardBorder;
                pnlDNWhite.Controls.Add(tblDN);
                pnlDN = new Panel
                {
                    Dock = DockStyle.Top, Height = 110,
                    BackColor = Color.FromArgb(240, 244, 249),
                    Padding = new Padding(20, 4, 20, 0)
                };
                pnlDN.Controls.Add(pnlDNWhite);
            }

            // ── Edit section ─────────────────────────────────────────────
            bool editExpanded = editMode;
            var pnlEditTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 8, 20, 0)
            };
            var lblEditToggle = new Label
            {
                Text = editExpanded ? "▼  Edit Shipment" : "▶  Edit Shipment",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            pnlEditTitle.Controls.Add(lblEditToggle);

            var pnlEditBody = new Panel
            {
                Dock = DockStyle.Top, Height = editExpanded ? 130 : 0,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 4, 20, 8)
            };

            var tblEdit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(18, 12, 18, 12)
            };
            for (int c = 0; c < 4; c++)
                tblEdit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            tblEdit.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            tblEdit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            tblEdit.Controls.Add(MakeLabelKey("New Status"),      0, 0);
            tblEdit.Controls.Add(MakeLabelKey("Actual Recipient"), 1, 0);
            tblEdit.Controls.Add(MakeLabelKey("Remark"),           2, 0);

            var cboStatusEdit = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Dock = DockStyle.Fill
            };
            cboStatusEdit.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            int sIdx = cboStatusEdit.FindStringExact(s.ShipmentStatus);
            cboStatusEdit.SelectedIndex = sIdx >= 0 ? sIdx : 0;

            var txtRecipEdit = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, PlaceholderText = "Actual recipient name"
            };
            if (s.ReplySlip != null) txtRecipEdit.Text = s.ReplySlip.ActualRecipient ?? "";

            var txtRemarkEdit = new TextBox
            {
                Font = new Font("Segoe UI", 12f), BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill, PlaceholderText = "Optional"
            };
            if (s.ReplySlip != null) txtRemarkEdit.Text = s.ReplySlip.RecipientRemark ?? "";

            tblEdit.Controls.Add(cboStatusEdit,  0, 1);
            tblEdit.Controls.Add(txtRecipEdit,   1, 1);
            tblEdit.Controls.Add(txtRemarkEdit,  2, 1);

            var pnlEditWhite = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlEditWhite.Paint += PaintCardBorder;
            pnlEditWhite.Controls.Add(tblEdit);
            pnlEditBody.Controls.Add(pnlEditWhite);

            void toggleEdit(object o, EventArgs ev)
            {
                editExpanded = !editExpanded;
                pnlEditBody.Height  = editExpanded ? 130 : 0;
                lblEditToggle.Text = editExpanded ? "▼  Edit Shipment" : "▶  Edit Shipment";
            }
            lblEditToggle.Click += toggleEdit;

            // ── Line items grid ────────────────────────────────────────────
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 36,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 10, 20, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text = "LINE ITEMS",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });

            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 40 },
                Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40,
                EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0),
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor          = Color.White,
                    ForeColor          = Color.FromArgb(15, 31, 53),
                    SelectionBackColor = Color.FromArgb(219, 234, 254),
                    SelectionForeColor = Color.FromArgb(15, 31, 53),
                    Padding            = new Padding(12, 6, 12, 6)
                }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LINE ID",    FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM ID",    FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM NAME",  FillWeight = 30 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY SHIPPED",FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OUTSTANDING",FillWeight = 18 });
            foreach (var line in s.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "—");

            var pnlTotalRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 44,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(12, 0, 20, 0)
            };
            pnlTotalRow.Controls.Add(new Label
            {
                Text = $"TOTAL AMOUNT:   HK$ {s.TotalAmount:N2}",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight
            });

            // ── Footer ────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 12, 28, 12)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnSave = new Button
            {
                Text      = "\u2714  Save Changes",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 200, Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);
            btnSave.Margin = new Padding(0, 0, 8, 0);

            var btnDelete = new Button
            {
                Text      = "\u2716  Delete Shipment",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(220, 38, 38),
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 200, Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 28, 28);
            btnDelete.Margin = new Padding(0, 0, 8, 0);

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            btnSave.Click += (o, ev) =>
            {
                if (!editExpanded) { toggleEdit(o, ev); return; }
                string newStatus    = cboStatusEdit.SelectedItem?.ToString();
                string newRecipient = txtRecipEdit.Text.Trim();
                string newRemark    = txtRemarkEdit.Text.Trim();
                try
                {
                    _ctrl.UpdateShipment(s.ShipmentID, newStatus, newRecipient, newRemark);
                    needsRefresh = true;

                    StatusColors.TryGetValue(newStatus ?? "", out var nc);
                    lblStatusBadge.Text      = newStatus ?? "Unknown";
                    lblStatusBadge.ForeColor = nc.fg != default ? nc.fg : Color.White;
                    lblStatusBadge.BackColor = nc.bg != default ? nc.bg : Color.FromArgb(80, 80, 80);

                    MessageBox.Show("Shipment updated successfully.",
                        "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save changes:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnDelete.Click += (o, ev) =>
            {
                var confirm = MessageBox.Show(
                    $"Permanently delete Shipment {s.ShipmentID} and all related records?\nThis cannot be undone.",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
                try
                {
                    _ctrl.DeleteShipment(s.ShipmentID);
                    needsRefresh = true;
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete shipment:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnClose.Click += (o, ev) => dlg.Close();

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnDelete);
            pnlFooter.Controls.Add(btnClose);

            // ── Assemble (Bottom first, Fill, then Top stack) ─────────────────
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlEditBody);
            dlg.Controls.Add(pnlEditTitle);
            if (pnlDN != null) dlg.Controls.Add(pnlDN);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFooter);

            dlg.ShowDialog(this);

            if (needsRefresh)
            {
                _selectedDetail = null;
                RefreshGrid();
            }
        }

        // ── Label factories ────────────────────────────────────────────────────
        private static Label MakeLabelKey(string text, Color? fg = null) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = fg ?? Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize  = false,
            Padding   = new Padding(0, 0, 8, 0)
        };

        private static Label MakeLabelVal(string text, Color? fg = null) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = fg ?? Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoSize     = false,
            AutoEllipsis = true
        };

        private static Label MakeLabelValMultiLine(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            AutoSize  = false,
            Padding   = new Padding(0, 6, 0, 0)
        };

        // ── Border painters ────────────────────────────────────────────────────
        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
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
            e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
        }

        // ── AddDetailRow helper (used in Reply Slip dialog) ────────────────────
        private static void AddDetailRow(TableLayoutPanel tbl, int row,
            string key1, string val1, string key2, string val2)
        {
            tbl.Controls.Add(MakeLabelKey(key1), 0, row);
            tbl.Controls.Add(MakeLabelVal(val1 ?? "\u2014"), 1, row);
            tbl.Controls.Add(MakeLabelKey(key2), 2, row);
            tbl.Controls.Add(MakeLabelVal(val2 ?? "\u2014"), 3, row);
        }

        // ── ShowDeliveryDocDialog (stub — called after DN/RS generation) ───────
        private void ShowDeliveryDocDialog(ShipmentDetailVM vm) { /* extend as needed */ }

        // ── ShowGenerateReplySlipDialog ────────────────────────────────────────
        private void ShowGenerateReplySlipDialog(ShipmentDetailVM s)
        {
            // Implemented inline to stay consistent with ShowDetailDialog pattern
            using var dlg = new Form
            {
                Text            = $"Generate Reply Slip  —  {s.ShipmentID}",
                Size            = new Size(640, 380),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                Font            = new Font("Segoe UI", 13f)
            };

            var tblMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 20, 24, 20)
            };
            tblMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute,  36f));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute,  70f));
            tblMain.RowStyles.Add(new RowStyle(SizeType.Absolute,  70f));

            tblMain.Controls.Add(new Label
            {
                Text = $"Delivery Note: {s.DeliveryNote?.DeliveryID}",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            var pnlRecip = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            pnlRecip.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            pnlRecip.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            pnlRecip.Controls.Add(MakeLabelKey("Actual Recipient *"), 0, 0);
            var txtRecip = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRecip.Controls.Add(txtRecip, 0, 1);
            tblMain.Controls.Add(pnlRecip, 0, 1);

            var pnlRemark = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            pnlRemark.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
            pnlRemark.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            pnlRemark.Controls.Add(MakeLabelKey("Remark (Optional)"), 0, 0);
            var txtRemark = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlRemark.Controls.Add(txtRemark, 0, 1);
            tblMain.Controls.Add(pnlRemark, 0, 2);

            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 72,
                BackColor = Color.White, Padding = new Padding(0, 10, 16, 10)
            };
            pnlFoot.Paint += PaintTopBorderStatic;
            var btnGen = new Button
            {
                Text = "\u2714  Generate",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right,
                Width = 180, Cursor = Cursors.Hand
            };
            btnGen.FlatAppearance.BorderSize = 0;
            btnGen.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 131, 58);
            var btnCancelRS = new Button
            {
                Text = "Cancel", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right,
                Width = 120, Cursor = Cursors.Hand
            };
            btnCancelRS.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancelRS.FlatAppearance.BorderSize         = 1;
            btnCancelRS.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnGen.Click += (o, ev) =>
            {
                string recip  = txtRecip.Text.Trim();
                string remark = txtRemark.Text.Trim();
                if (string.IsNullOrEmpty(recip))
                {
                    MessageBox.Show("Actual Recipient is required.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRecip.Focus(); return;
                }
                try
                {
                    string slipId = _ctrl.GenerateReplySlip(s.ShipmentID, recip, remark);
                    MessageBox.Show($"Reply Slip {slipId} generated.",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    _selectedDetail = _ctrl.GetShipmentDetail(s.ShipmentID);
                    UpdateActionButtons();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            btnCancelRS.Click += (o, ev) => dlg.Close();
            pnlFoot.Controls.Add(btnGen);
            pnlFoot.Controls.Add(btnCancelRS);

            dlg.Controls.Add(tblMain);
            dlg.Controls.Add(pnlFoot);
            dlg.ShowDialog(this);
        }

        // ── Rounded rectangle helper (used in KPI pills) ──────────────────────
        private static GraphicsPath RoundedRect(System.Drawing.Rectangle r, int radius)
        {
            var path = new GraphicsPath(); int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure(); return path;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Navigation / session
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?",
                                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SessionManager.Clear();
                Application.Restart();
            }
        }
    }
}
