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
    ///
    /// NOTE on ShipmentDetailVM:
    ///   ShipmentDetailVM does NOT expose shipment fields directly.
    ///   All ShipmentEntity properties are accessed via  vm.Shipment.<prop>
    ///   (e.g. vm.Shipment.ShipmentID, vm.Shipment.ShipmentStatus).
    ///   DeliveryNote and ReplySlip are direct properties of ShipmentDetailVM.
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
        //  NOTE: s here is ShipmentEntity from _currentShipments — direct props are correct.
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
            foreach (var s in _currentShipments)   // s is ShipmentEntity — direct props OK
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
                    BackColor = Color.Transparent,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, NumColW));
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

                tlp.Controls.Add(new Label
                {
                    Text = count, Font = new Font("Segoe UI", 20f, FontStyle.Bold),
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
            // Use null-safe chain: ShipmentDetailVM.Shipment may be null if detail load failed
            string status = _selectedDetail?.Shipment?.ShipmentStatus ?? "";

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
        /// Passes ShipmentEntity (not ShipmentDetailVM) because ScheduleShipmentDialog
        /// only needs the raw entity for read-only display + the ScheduleShipment call.
        /// On DialogResult.OK the grid is refreshed.
        /// </summary>
        private void btnScheduleShipment_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;

            var entity = _currentShipments.Find(
                s => s.ShipmentID == _selectedDetail.Shipment.ShipmentID);
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
                string dnId = _ctrl.GenerateDeliveryNote(_selectedDetail.Shipment.ShipmentID);
                MessageBox.Show($"Delivery Note {dnId} generated successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _selectedDetail = _ctrl.GetShipmentDetail(_selectedDetail.Shipment.ShipmentID);
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
        //  IMPORTANT: parameter s is ShipmentDetailVM.
        //  All ShipmentEntity fields accessed via s.Shipment.<prop>
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowDetailDialog(ShipmentDetailVM s, bool editMode = false)
        {
            bool needsRefresh = false;

            var dlg = new Form
            {
                Text            = $"Shipment Details  —  {s.Shipment.ShipmentID}",
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
                Text = $"Shipment Details  —  {s.Shipment.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            StatusColors.TryGetValue(s.Shipment.ShipmentStatus ?? "", out var sc);
            var lblStatusBadge = new Label
            {
                Text      = s.Shipment.ShipmentStatus ?? "Unknown",
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
                "Shipment ID",   s.Shipment.ShipmentID,
                "Order ID",      s.Shipment.OrderID,
                "", "", "", "");
            AddInfoRow(1,
                "Customer",      s.Shipment.CustomerName  ?? "\u2014",
                "Ship Date",     s.Shipment.ShipDate.ToString("yyyy-MM-dd"),
                "", "", "", "");
            AddInfoRow(2,
                "Delivery",      s.Shipment.DeliveryMethod ?? "\u2014",
                "Type",          s.Shipment.ShipmentType   ?? "\u2014",
                "", "", "", "");

            var pnlInfoCard  = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlInfoCard.Paint += PaintCardBorder;
            pnlInfoCard.Controls.Add(tblInfo);

            var pnlInfoOuter = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 14, 20, 8)
            };
            pnlInfoOuter.Controls.Add(pnlInfoCard);

            // ── Lines grid ────────────────────────────────────────────────
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
                RowTemplate = { Height = 44 },
                Dock = DockStyle.Fill,
                ColumnHeadersHeight = 42,
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
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotalRow = new Panel
            {
                Dock = DockStyle.Bottom, Height = 44,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(12, 0, 20, 0)
            };
            pnlTotalRow.Controls.Add(new Label
            {
                Text = $"TOTAL AMOUNT:   HK$ {s.Shipment.TotalAmount:N2}",
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
            btnSave.Visible = editMode;

            var btnDelete = new Button
            {
                Text      = "\uD83D\uDDD1  Delete",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 160, Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = Color.FromArgb(153, 27, 27);
            btnDelete.Visible = editMode;

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
            btnClose.Click += (_, __) => dlg.Close();

            // ── Edit controls ─────────────────────────────────────────────
            var cboStatusEdit = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Width = 200
            };
            cboStatusEdit.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            int sIdx = cboStatusEdit.FindStringExact(s.Shipment.ShipmentStatus);
            cboStatusEdit.SelectedIndex = sIdx >= 0 ? sIdx : 0;
            cboStatusEdit.Visible = editMode;

            var txtRecipient = new TextBox
            {
                Font = new Font("Segoe UI", 12f), Width = 260,
                PlaceholderText = "Actual recipient name",
                BorderStyle = BorderStyle.FixedSingle
            };
            txtRecipient.Visible = editMode;

            var txtRemark = new TextBox
            {
                Font = new Font("Segoe UI", 12f), Width = 320,
                PlaceholderText = "Optional remark",
                BorderStyle = BorderStyle.FixedSingle
            };
            txtRemark.Visible = editMode;

            if (editMode)
            {
                var pnlEditRow = new Panel
                {
                    Dock = DockStyle.Top, Height = 60,
                    BackColor = Color.FromArgb(246, 249, 255),
                    Padding = new Padding(20, 10, 20, 10)
                };
                pnlEditRow.Controls.Add(txtRemark);
                pnlEditRow.Controls.Add(txtRecipient);
                pnlEditRow.Controls.Add(cboStatusEdit);
                var tblFooter2 = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                    BackColor = Color.Transparent
                };
                tblFooter2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
                tblFooter2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
                tblFooter2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tblFooter2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tblFooter2.Controls.Add(MakeEditCell("New Status",    cboStatusEdit), 0, 0);
                tblFooter2.Controls.Add(MakeEditCell("Recipient",     txtRecipient),  1, 0);
                tblFooter2.Controls.Add(MakeEditCell("Remark",        txtRemark),     2, 0);
                pnlEditRow.Controls.Clear();
                pnlEditRow.Controls.Add(tblFooter2);
                dlg.Controls.Add(pnlEditRow);
            }

            btnSave.Click += (_, __) =>
            {
                string newStatus    = cboStatusEdit.SelectedItem?.ToString();
                string newRecipient = txtRecipient.Text.Trim();
                string newRemark    = txtRemark.Text.Trim();
                try
                {
                    _ctrl.UpdateShipment(s.Shipment.ShipmentID, newStatus, newRecipient, newRemark);
                    needsRefresh = true;
                    MessageBox.Show("Shipment updated.", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Save failed:\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnDelete.Click += (_, __) =>
            {
                var confirm = MessageBox.Show(
                    $"Permanently delete Shipment {s.Shipment.ShipmentID} and all related records?\nThis cannot be undone.",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
                try
                {
                    _ctrl.DeleteShipment(s.Shipment.ShipmentID);
                    needsRefresh = true;
                    MessageBox.Show("Shipment deleted.", "Deleted",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Delete failed:\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnlFooter.Controls.Add(btnSave);
            pnlFooter.Controls.Add(btnDelete);
            pnlFooter.Controls.Add(btnClose);

            // ── Grid card wrapper ─────────────────────────────────────────
            var pnlGridInner = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGridInner.Paint += PaintCardBorder;
            pnlGridInner.Controls.Add(pnlTotalRow);
            pnlGridInner.Controls.Add(dgv);

            var pnlGridOuter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 8, 20, 0)
            };
            pnlGridOuter.Controls.Add(pnlGridInner);

            // ── Assemble dlg ──────────────────────────────────────────────
            dlg.Controls.Add(pnlGridOuter);
            dlg.Controls.Add(pnlInfoOuter);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlHeader);

            dlg.FormClosed += (_, __) => { if (needsRefresh) RefreshGrid(); };
            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Delivery doc stub
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowDeliveryDocDialog(ShipmentDetailVM vm) { /* extend as needed */ }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Generate Reply Slip dialog
        //  IMPORTANT: parameter s is ShipmentDetailVM.
        //  All ShipmentEntity fields accessed via s.Shipment.<prop>
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowGenerateReplySlipDialog(ShipmentDetailVM s)
        {
            var dlg = new Form
            {
                Text            = $"Generate Reply Slip  —  {s.Shipment.ShipmentID}",
                Size            = new Size(1400, 880),
                MinimumSize     = new Size(1100, 700),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                Font            = new Font("Segoe UI", 13f)
            };

            // ── Header ────────────────────────────────────────────────────
            var pnlH = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlH.Controls.Add(new Label
            {
                Text = $"Generate Reply Slip  —  {s.Shipment.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0)
            });

            // ── Form card ─────────────────────────────────────────────────
            var tblForm = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(28, 18, 28, 14)
            };
            tblForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblForm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            for (int r = 0; r < 4; r++)
                tblForm.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            // Row 0: read-only
            tblForm.Controls.Add(MakeEditCell("Shipment ID",     MakeStaticLabel(s.Shipment.ShipmentID)), 0, 0);
            tblForm.Controls.Add(MakeEditCell("Delivery Note",   MakeStaticLabel(s.DeliveryNote?.DeliveryID ?? "\u2014")), 1, 0);

            // Row 1: read-only
            tblForm.Controls.Add(MakeEditCell("Customer",        MakeStaticLabel(s.Shipment.CustomerName ?? "\u2014")), 0, 1);
            tblForm.Controls.Add(MakeEditCell("Ship Date",       MakeStaticLabel(s.Shipment.ShipDate.ToString("yyyy-MM-dd"))), 1, 1);

            // Row 2: editable — Actual Recipient
            var txtRecip = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                PlaceholderText = "Full name of recipient",
                BorderStyle = BorderStyle.FixedSingle
            };
            tblForm.Controls.Add(MakeEditCell("Actual Recipient *", txtRecip), 0, 2);

            // Row 3: editable — Remark
            var txtRemark = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                PlaceholderText = "Optional remark",
                BorderStyle = BorderStyle.FixedSingle
            };
            tblForm.Controls.Add(MakeEditCell("Remark", txtRemark), 1, 2);

            var pnlFormCard  = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlFormCard.Paint += PaintCardBorder;
            pnlFormCard.Controls.Add(tblForm);

            var pnlFormOuter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 14, 20, 8)
            };
            pnlFormOuter.Controls.Add(pnlFormCard);

            // ── Footer ────────────────────────────────────────────────────
            var pnlF = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 12, 28, 12)
            };
            pnlF.Paint += PaintTopBorderStatic;

            var btnGen = new Button
            {
                Text = "\u2714  Generate Reply Slip",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 240, Cursor = Cursors.Hand
            };
            btnGen.FlatAppearance.BorderSize = 0;
            btnGen.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 131, 58);

            var btnCancelSlip = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Dock = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnCancelSlip.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancelSlip.FlatAppearance.BorderSize         = 1;
            btnCancelSlip.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancelSlip.Click += (_, __) => dlg.Close();

            btnGen.Click += (_, __) =>
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
                    string slipId = _ctrl.GenerateReplySlip(s.Shipment.ShipmentID, recip, remark);
                    MessageBox.Show($"Reply Slip {slipId} generated.", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _selectedDetail = _ctrl.GetShipmentDetail(s.Shipment.ShipmentID);
                    UpdateActionButtons();
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed:\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnlF.Controls.Add(btnGen);
            pnlF.Controls.Add(btnCancelSlip);

            dlg.Controls.Add(pnlFormOuter);
            dlg.Controls.Add(pnlF);
            dlg.Controls.Add(pnlH);
            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid cell formatting — colour-codes the Status column
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void dgvShipments_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvShipments.Columns[e.ColumnIndex];
            if (col.Name == "colStatus" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (StatusColors.TryGetValue(status, out var sc))
                {
                    e.CellStyle.BackColor  = sc.bg;
                    e.CellStyle.ForeColor  = sc.fg;
                    e.CellStyle.Font       = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.FormattingApplied    = true;
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  UI helpers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static Label MakeLabelKey(string text) => new Label
        {
            Text      = text,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Padding   = new Padding(0, 0, 0, 2)
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoSize     = false,
            AutoEllipsis = true
        };

        private static Label MakeStaticLabel(string text) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.MiddleLeft,
            AutoSize     = false,
            AutoEllipsis = true
        };

        private static Panel MakeEditCell(string caption, Control ctrl)
        {
            var cell = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 0, 14, 0) };
            var lbl  = new Label
            {
                Text = caption, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Top, Height = 28,
                TextAlign = ContentAlignment.BottomLeft, Padding = new Padding(0, 0, 0, 2)
            };
            ctrl.Dock = DockStyle.Fill;
            cell.Controls.Add(ctrl);
            cell.Controls.Add(lbl);
            return cell;
        }

        private static void PaintCardBorder(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            var rc = ((Control)s).ClientRectangle;
            rc.Width--; rc.Height--;
            e.Graphics.DrawRectangle(pen, rc);
        }

        private static void PaintTopBorderStatic(object s, PaintEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, ((Control)s).Width, 0);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
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
