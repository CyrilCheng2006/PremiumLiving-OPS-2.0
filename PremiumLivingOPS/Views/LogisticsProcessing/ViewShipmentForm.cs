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
    /// • KPI pills + four action buttons:
    ///     View Details | Modify | Generate Delivery Note | Generate Reply Slip
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
        //  Action button state
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void UpdateActionButtons()
        {
            bool sel = dgvShipments.SelectedRows.Count > 0;
            btnViewDetail.Enabled      = sel;
            btnModify.Enabled          = sel;
            btnGenDeliveryNote.Enabled = sel;
            btnGenReplySlip.Enabled    = sel;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Grid events
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Selected shipment ID helper
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private string SelectedShipmentId()
        {
            if (dgvShipments.SelectedRows.Count == 0) return null;
            return dgvShipments.SelectedRows[0].Cells["colShipmentID"].Value?.ToString();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  View Detail button  →  opens combined View + Modify dialog
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnViewDetail_Click(object sender, EventArgs e) => ShowDetailDialog();

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Modify button  →  same dialog, edit panel pre-expanded
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnModify_Click(object sender, EventArgs e)
        {
            string id = SelectedShipmentId();
            if (string.IsNullOrEmpty(id))
            {
                MessageBox.Show("Please select a shipment to modify.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowDetailDialog(openInEditMode: true);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Generate Delivery Note button
        //  Opens the standalone GenerateDeliveryNoteForm (1200×780).
        //  On DialogResult.OK → refresh _selectedDetail + ShowDeliveryDocDialog.
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnGenDeliveryNote_Click(object sender, EventArgs e)
        {
            if (_selectedDetail?.Shipment == null)
            {
                MessageBox.Show("Please select a shipment first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Reload fresh data so the form always reflects current DB state
            var freshDetail = _ctrl.GetShipmentDetail(_selectedDetail.Shipment.ShipmentID);
            if (freshDetail?.Shipment == null) return;

            using var frm = new GenerateDeliveryNoteForm(freshDetail);
            var result = frm.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                // Refresh cached detail so subsequent actions (Reply Slip, View Detail)
                // see the newly created Delivery Note.
                _selectedDetail = _ctrl.GetShipmentDetail(freshDetail.Shipment.ShipmentID);

                if (_selectedDetail?.DeliveryNote != null)
                    ShowDeliveryDocDialog(_selectedDetail);

                RefreshGrid();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Generate Reply Slip button
        //
        //  Full dialog matching ShowDetailDialog visual language:
        //    pnlHeader   Top  80   — green (5,95,70), DeliveryNote ID + status badge
        //    pnlDNInfo   Top  220  — 4-col TLP, all DN + Shipment fields
        //    pnlInputCard Top 180  — Reply Slip input (Recipient *, Remark, ReceivedDate)
        //    pnlLineLabel Top  40  — "SHIPMENT ITEMS" bar
        //    dgv         Fill      — shipment items grid
        //    pnlTotalRow Bottom 50 — total amount
        //    pnlFooter   Bottom 80 — [Confirm Generate] [Cancel]
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void btnGenReplySlip_Click(object sender, EventArgs e)
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
                    "A Delivery Note must be generated before creating a Reply Slip.\n" +
                    "Please click \"Generate Delivery Note\" first.",
                    "No Delivery Note", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedDetail.ReplySlip != null)
            {
                MessageBox.Show(
                    $"A Reply Slip ({_selectedDetail.ReplySlip.SlipID}) already exists for Delivery Note " +
                    $"{_selectedDetail.DeliveryNote.DeliveryID}.\n" +
                    "Use \"View Details\" to view the existing document.",
                    "Already Generated", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Reload fresh data
            var detail = _ctrl.GetShipmentDetail(_selectedDetail.Shipment.ShipmentID);
            if (detail?.Shipment == null) return;

            var s  = detail.Shipment;
            var dn = detail.DeliveryNote;

            using var dlg = new Form
            {
                Text            = $"Generate Reply Slip  —  {dn.DeliveryID}",
                Size            = new Size(1400, 880),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── Header ─────────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(5, 95, 70) };
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
                Text      = $"Generate Reply Slip  —  {dn.DeliveryID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            tblHeader.Controls.Add(new Label
            {
                Text      = "PENDING RECEIPT",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(146, 64, 14),
                BackColor = Color.FromArgb(254, 243, 199),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── DN Info panel (4-col TLP, mirrors ShowDetailDialog pnlInfo) ───
            var pnlDNInfo = new Panel
            {
                Dock    = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
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
            for (int r = 0; r < 4; r++)
                tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

            AddDetailRow(tblDN, 0, "Delivery Note:",    dn.DeliveryID,                         "Shipment ID:",    s.ShipmentID);
            AddDetailRow(tblDN, 1, "Ship To:",          dn.ShipToName,                         "Delivery Date:",  dn.DeliveryDate.ToString("yyyy-MM-dd"));
            AddDetailRow(tblDN, 2, "Ship Address:",     dn.ShippingAddress,                    "Tracking No.:",   s.TrackingNumber);
            AddDetailRow(tblDN, 3, "Outstanding Qty:",  (dn.OutstandingQty ?? 0).ToString(),   "Delivery Method:", s.DeliveryMethod);
            pnlDNInfo.Controls.Add(tblDN);

            // ── Reply Slip Input Card ──────────────────────────────────────────
            var pnlInputTitle = new Panel
            {
                Dock      = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 253, 244),
                Padding   = new Padding(28, 0, 16, 0)
            };
            pnlInputTitle.Paint += PaintBottomBorderStatic;
            pnlInputTitle.Controls.Add(new Label
            {
                Text      = "\u2709  Reply Slip  —  Receipt Confirmation",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            });

            var pnlInputBody = new Panel
            {
                Dock      = DockStyle.Top, Height = 140,
                BackColor = Color.FromArgb(249, 254, 251),
                Padding   = new Padding(28, 16, 28, 12)
            };
            pnlInputBody.Paint += PaintBottomBorderStatic;

            var lblRecipient = MakeLabelKey("Actual Recipient *");
            lblRecipient.AutoSize = true;
            lblRecipient.Dock     = DockStyle.None;
            lblRecipient.Location = new Point(0, 14);

            var txtRecipient = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Location        = new Point(180, 10),
                Size            = new Size(340, 32),
                PlaceholderText = "Full name of recipient"
            };

            var lblReceivedDate = MakeLabelKey("Received Date");
            lblReceivedDate.AutoSize = true;
            lblReceivedDate.Dock     = DockStyle.None;
            lblReceivedDate.Location = new Point(560, 14);

            var lblReceivedDateVal = MakeLabelVal(DateTime.Today.ToString("yyyy-MM-dd"));
            lblReceivedDateVal.AutoSize = false;
            lblReceivedDateVal.Dock     = DockStyle.None;
            lblReceivedDateVal.Location = new Point(720, 10);
            lblReceivedDateVal.Size     = new Size(200, 32);
            lblReceivedDateVal.Font     = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblReceivedDateVal.ForeColor = Color.FromArgb(6, 95, 70);

            var lblRemark = MakeLabelKey("Remark");
            lblRemark.AutoSize = true;
            lblRemark.Dock     = DockStyle.None;
            lblRemark.Location = new Point(0, 68);

            var txtRemark = new TextBox
            {
                Font            = new Font("Segoe UI", 12f),
                BorderStyle     = BorderStyle.FixedSingle,
                Location        = new Point(180, 64),
                Size            = new Size(740, 32),
                PlaceholderText = "e.g. Left at front desk  (optional)"
            };

            pnlInputBody.Controls.Add(lblRecipient);
            pnlInputBody.Controls.Add(txtRecipient);
            pnlInputBody.Controls.Add(lblReceivedDate);
            pnlInputBody.Controls.Add(lblReceivedDateVal);
            pnlInputBody.Controls.Add(lblRemark);
            pnlInputBody.Controls.Add(txtRemark);

            // ── SHIPMENT ITEMS label bar ──────────────────────────────────────
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text      = "SHIPMENT ITEMS",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            // ── Items grid ────────────────────────────────────────────────────
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode   = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor       = Color.FromArgb(221, 227, 236),
                Font            = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle     = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate         = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255), ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor            = Color.White, ForeColor = Color.FromArgb(15, 31, 53),
                    SelectionBackColor   = Color.FromArgb(219, 234, 254),
                    SelectionForeColor   = Color.FromArgb(15, 31, 53),
                    Padding              = new Padding(12, 6, 12, 6)
                }
            };
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cLID",  HeaderText = "LINE ID",         FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cItem", HeaderText = "ITEM ID",         FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cName", HeaderText = "ITEM NAME",       FillWeight = 42 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cQty",  HeaderText = "QTY SHIPPED",     FillWeight = 13 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "cOut",  HeaderText = "QTY OUTSTANDING", FillWeight = 13 });
            foreach (var line in detail.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding ?? 0);

            // ── Total row ─────────────────────────────────────────────────────
            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(0, 0, 28, 0)
            };
            pnlTotalRow.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {s.TotalAmount:N2}",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, AutoSize = false
            });

            // ── Footer — [Confirm Generate] [Cancel] ─────────────────────────
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 12, 28, 12)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnConfirm = new Button
            {
                Text      = "\u2714  Confirm Generate",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(5, 150, 105),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right, Width = 220, Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize         = 0;
            btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(4, 120, 87);
            btnConfirm.Margin = new Padding(0, 0, 8, 0);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            bool confirmed   = false;
            string recipient = string.Empty;
            string remark    = string.Empty;

            btnConfirm.Click += (o, ev) =>
            {
                if (string.IsNullOrWhiteSpace(txtRecipient.Text))
                {
                    MessageBox.Show("Actual Recipient is required.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRecipient.Focus();
                    return;
                }
                recipient = txtRecipient.Text.Trim();
                remark    = txtRemark.Text.Trim();
                confirmed = true;
                dlg.Close();
            };

            btnCancel.Click += (o, ev) => dlg.Close();

            pnlFooter.Controls.Add(btnConfirm);
            pnlFooter.Controls.Add(btnCancel);

            // ── Assemble (Bottom → Fill → Top in DockStyle priority order) ───
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlInputBody);
            dlg.Controls.Add(pnlInputTitle);
            dlg.Controls.Add(pnlDNInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.Controls.Add(pnlFooter);

            dlg.ShowDialog(this);

            if (!confirmed) return;

            try
            {
                _ctrl.GenerateReplySlip(
                    _selectedDetail.Shipment.ShipmentID,
                    recipient,
                    remark);

                _selectedDetail = _ctrl.GetShipmentDetail(_selectedDetail.Shipment.ShipmentID);

                MessageBox.Show(
                    $"Reply Slip {_selectedDetail.ReplySlip?.SlipID} generated successfully.",
                    "Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ShowDeliveryDocDialog(_selectedDetail);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate Reply Slip:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  SHIPMENT DETAIL + MODIFY DIALOG
        //
        //  Layout (top → bottom, DockStyle):
        //    pnlHeader          Top  80   — dark navy, shipment ID + status badge
        //    pnlInfo            Top  340  — read-only fields (4-col TLP, Percent widths)
        //    pnlDN              Top  60/110 — delivery note / reply slip strip (optional)
        //    pnlEditCard        Top  180  — ★ EDIT SHIPMENT section (collapsible)
        //    pnlLineLabel       Top  40   — "SHIPMENT ITEMS" header bar
        //    dgv                Fill      — shipment items DataGridView
        //    pnlTotalRow        Bottom 50 — total amount
        //    pnlFooter          Bottom 80 — [Save Changes] [Delete] [Close]
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowDetailDialog(bool openInEditMode = false)
        {
            if (_selectedDetail?.Shipment == null)
            {
                MessageBox.Show("Please select a shipment first.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Reload fresh data so dialog always shows current DB values
            var detail = _ctrl.GetShipmentDetail(_selectedDetail.Shipment.ShipmentID);
            if (detail?.Shipment == null) return;

            var s = detail.Shipment;
            StatusColors.TryGetValue(s.ShipmentStatus ?? "", out var sc);

            bool needsRefresh = false;

            using var dlg = new Form
            {
                Text            = $"Shipment — {s.ShipmentID}",
                Size            = new Size(2500, 1300),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false
            };

            // ── Header ─────────────────────────────────────────────────────────────
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text = $"Shipment Details  —  {s.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            var lblStatusBadge = new Label
            {
                Text      = s.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Padding = new Padding(8, 4, 8, 4)
            };
            tblHeader.Controls.Add(lblStatusBadge, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            // ── Info panel (read-only, mirrors ViewOrderForm) ──────────────────
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
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 15f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 15f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 15f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 15f));

            var leftFields = new (string Key, string Val, bool multiLine)[]
            {
                ("Shipment ID",      s.ShipmentID,                      false),
                ("Customer",         s.CustomerName,                    false),
                ("Ship Date",        s.ShipDate.ToString("yyyy-MM-dd"), false),
                ("Shipping Address", s.ShippingAddress,                 true ),
                ("Total Amount",     $"HK$ {s.TotalAmount:N2}",         false),
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

            var rightFields = new (string Key, string Val, bool multiLine)[]
            {
                ("Order ID",        s.OrderID,        false),
                ("Tracking No.",    s.TrackingNumber, false),
                ("Ship Type",       s.ShipmentType,   false),
                ("Delivery Method", s.DeliveryMethod, false),
                ("Status",          s.ShipmentStatus, false),
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

            // ── DeliveryNote strip ──────────────────────────────────────────────
            Panel pnlDN = null;
            if (detail.DeliveryNote != null)
            {
                var dn = detail.DeliveryNote;
                var rs = detail.ReplySlip;

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
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.3f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.3f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12f));
                tblDN.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 21.4f));
                for (int r = 0; r < dnRows; r++)
                    tblDN.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / dnRows));

                tblDN.Controls.Add(MakeLabelKey("Delivery Note:",   dnFg), 0, 0);
                tblDN.Controls.Add(MakeLabelVal(dn.DeliveryID,      dnFg), 1, 0);
                tblDN.Controls.Add(MakeLabelKey("Delivery Date:",   dnFg), 2, 0);
                tblDN.Controls.Add(MakeLabelVal(dn.DeliveryDate.ToString("yyyy-MM-dd"), dnFg), 3, 0);
                tblDN.Controls.Add(MakeLabelKey("Outstanding Qty:", dnFg), 4, 0);
                tblDN.Controls.Add(MakeLabelVal((dn.OutstandingQty ?? 0).ToString(), dnFg), 5, 0);

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

            // ── EDIT SHIPMENT section ───────────────────────────────────────────
            var pnlEditTitle = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.FromArgb(241, 245, 255),
                Padding   = new Padding(28, 0, 16, 0),
                Cursor    = Cursors.Hand
            };
            pnlEditTitle.Paint += PaintBottomBorderStatic;

            var lblEditToggle = new Label
            {
                Text      = openInEditMode ? "\u25BC  Edit Shipment" : "\u25BA  Edit Shipment",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Cursor    = Cursors.Hand
            };
            pnlEditTitle.Controls.Add(lblEditToggle);

            var pnlEditBody = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = openInEditMode ? 136 : 0,
                BackColor = Color.FromArgb(250, 252, 255),
                Padding   = new Padding(28, 12, 28, 12),
                Visible   = openInEditMode
            };

            var lblStatusEdit = MakeLabelKey("Status *");
            lblStatusEdit.AutoSize = true;
            lblStatusEdit.Dock     = DockStyle.None;
            lblStatusEdit.Location = new Point(0, 14);

            var cboStatusEdit = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 12f),
                Location      = new Point(160, 10),
                Size          = new Size(220, 30)
            };
            cboStatusEdit.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            int si = cboStatusEdit.FindStringExact(s.ShipmentStatus);
            cboStatusEdit.SelectedIndex = si >= 0 ? si : 0;

            var lblRecipEdit = MakeLabelKey("Actual Recipient");
            lblRecipEdit.AutoSize = true;
            lblRecipEdit.Dock     = DockStyle.None;
            lblRecipEdit.Location = new Point(420, 14);

            var txtRecipEdit = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Location    = new Point(600, 10),
                Size        = new Size(340, 30),
                Text        = detail.ReplySlip?.ActualRecipient ?? string.Empty
            };

            var lblRemarkEdit = MakeLabelKey("Remark");
            lblRemarkEdit.AutoSize = true;
            lblRemarkEdit.Dock     = DockStyle.None;
            lblRemarkEdit.Location = new Point(0, 62);

            var txtRemarkEdit = new TextBox
            {
                Font        = new Font("Segoe UI", 12f),
                BorderStyle = BorderStyle.FixedSingle,
                Location    = new Point(160, 58),
                Size        = new Size(780, 30),
                Text        = detail.ReplySlip?.RecipientRemark ?? string.Empty
            };

            pnlEditBody.Controls.Add(lblStatusEdit);
            pnlEditBody.Controls.Add(cboStatusEdit);
            pnlEditBody.Controls.Add(lblRecipEdit);
            pnlEditBody.Controls.Add(txtRecipEdit);
            pnlEditBody.Controls.Add(lblRemarkEdit);
            pnlEditBody.Controls.Add(txtRemarkEdit);

            bool editExpanded = openInEditMode;
            EventHandler toggleEdit = (o, ev) =>
            {
                editExpanded = !editExpanded;
                pnlEditBody.Visible = editExpanded;
                pnlEditBody.Height  = editExpanded ? 136 : 0;
                lblEditToggle.Text  = editExpanded
                    ? "\u25BC  Edit Shipment"
                    : "\u25BA  Edit Shipment";
            };
            pnlEditTitle.Click  += toggleEdit;
            lblEditToggle.Click += toggleEdit;

            // ── SHIPMENT ITEMS label bar ──────────────────────────────────────
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

            // ── Items grid ─────────────────────────────────────────────────────
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
            foreach (var line in detail.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding ?? 0);

            // ── Total row ─────────────────────────────────────────────────────────
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

            // ── Footer — [Save Changes] [Delete Shipment] [Close] ───────────
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 80,
                BackColor = Color.White, Padding = new Padding(0, 12, 28, 12)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnSave = new Button
            {
                Text      = "\u2714  Save Changes",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(34, 139, 34),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right, Width = 200, Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize         = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 111, 22);
            btnSave.Margin = new Padding(0, 0, 8, 0);

            var btnDelete = new Button
            {
                Text      = "\u2715  Delete Shipment",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right, Width = 200, Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize         = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = Color.FromArgb(153, 20, 20);
            btnDelete.Margin = new Padding(0, 0, 8, 0);

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            btnSave.Click += (o, ev) =>
            {
                if (!editExpanded)
                {
                    MessageBox.Show(
                        "Please expand the \"Edit Shipment\" section first.",
                        "Edit Section Collapsed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string newStatus  = cboStatusEdit.SelectedItem?.ToString() ?? string.Empty;
                string recipient  = txtRecipEdit.Text.Trim();
                string remark     = txtRemarkEdit.Text.Trim();

                if (string.IsNullOrEmpty(newStatus))
                {
                    MessageBox.Show("Please select a status.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    _ctrl.UpdateShipment(s.ShipmentID, newStatus, recipient, remark);

                    StatusColors.TryGetValue(newStatus, out var nsc);
                    lblStatusBadge.Text      = newStatus;
                    lblStatusBadge.ForeColor = nsc.fg != default ? nsc.fg : Color.White;
                    lblStatusBadge.BackColor = nsc.bg != default ? nsc.bg : Color.FromArgb(80, 80, 80);

                    MessageBox.Show(
                        $"Shipment {s.ShipmentID} updated successfully.",
                        "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    needsRefresh = true;
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
                    $"Are you sure you want to permanently delete shipment\n" +
                    $"{s.ShipmentID} ({s.CustomerName})?\n\n" +
                    "This will also delete all associated Delivery Notes and Reply Slips.",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

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

            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Controls.Add(btnDelete);
            pnlFooter.Controls.Add(btnSave);

            // ── Assemble (Bottom → Fill → Top in DockStyle priority order) ───
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

            if (needsRefresh) RefreshGrid();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Delivery Document dialog  —  shows full Delivery Note + optional Reply Slip
        //  Called after successful Generate, or directly from View Details.
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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

            // ── DN fields ─────────────────────────────────────────────────────
            var pnlDNFields = new Panel
            {
                Dock = DockStyle.Top, Height = 200,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlDNFields.Paint += PaintBottomBorderStatic;

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 3,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
            for (int r = 0; r < 3; r++) tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3f));

            AddDetailRow(tbl, 0, "Delivery Note:", dn.DeliveryID,   "Shipment ID:",    s.ShipmentID);
            AddDetailRow(tbl, 1, "Delivery Date:", dn.DeliveryDate.ToString("yyyy-MM-dd"), "Ship To:", dn.ShipToName);
            AddDetailRow(tbl, 2, "Outstanding Qty:", (dn.OutstandingQty ?? 0).ToString(), "Shipping Address:", dn.ShippingAddress);
            pnlDNFields.Controls.Add(tbl);

            // ── Reply Slip section (only if received) ─────────────────────────
            Panel pnlRS = null;
            if (received)
            {
                pnlRS = new Panel
                {
                    Dock = DockStyle.Top, Height = 160,
                    BackColor = Color.FromArgb(240, 253, 244),
                    Padding = new Padding(28, 12, 28, 8)
                };
                pnlRS.Paint += PaintBottomBorderStatic;

                pnlRS.Controls.Add(new Label
                {
                    Text = "\u2709  Reply Slip — Receipt Confirmed",
                    Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(6, 95, 70),
                    Dock = DockStyle.Top, Height = 30, AutoSize = false
                });

                var tblRS = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                    BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
                tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
                tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18f));
                tblRS.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32f));
                tblRS.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
                tblRS.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

                AddDetailRow(tblRS, 0, "Reply Slip ID:", rs.SlipID,          "Received Date:", rs.ReceivedDate.ToString("yyyy-MM-dd"));
                AddDetailRow(tblRS, 1, "Recipient:",     rs.ActualRecipient, "Remark:",        rs.RecipientRemark ?? "—");
                pnlRS.Controls.Add(tblRS);
            }

            // ── Footer ────────────────────────────────────────────────────────
            var pnlFoot = new Panel
            {
                Dock = DockStyle.Bottom, Height = 64,
                BackColor = Color.White, Padding = new Padding(0, 12, 28, 12)
            };
            pnlFoot.Paint += PaintTopBorderStatic;

            var btnClose = new Button
            {
                Text      = "Close",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock      = DockStyle.Right, Width = 140, Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (o, ev) => dlg.Close();
            pnlFoot.Controls.Add(btnClose);

            dlg.Controls.Add(pnlFoot);
            if (pnlRS != null) dlg.Controls.Add(pnlRS);
            dlg.Controls.Add(pnlDNFields);
            dlg.Controls.Add(pnlH);

            dlg.ShowDialog(this);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Shared paint helpers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static void PaintBottomBorderStatic(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        }

        private static void PaintTopBorderStatic(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            var p = (Panel)sender;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  Label factory helpers (used in inline dialogs)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static Label MakeLabelKey(string text, Color? fg = null) =>
            new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = fg ?? Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            };

        private static Label MakeLabelVal(string text, Color? fg = null) =>
            new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = fg ?? Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            };

        private static Label MakeLabelValMultiLine(string text) =>
            new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                AutoSize  = false
            };

        private static void AddDetailRow(TableLayoutPanel tbl, int row,
            string key1, string val1, string key2, string val2)
        {
            tbl.Controls.Add(MakeLabelKey(key1), 0, row);
            tbl.Controls.Add(MakeLabelVal(val1), 1, row);
            tbl.Controls.Add(MakeLabelKey(key2), 2, row);
            tbl.Controls.Add(MakeLabelVal(val2), 3, row);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.Left,       r.Top,        d, d, 180, 90);
            path.AddArc(r.Right - d,  r.Top,        d, d, 270, 90);
            path.AddArc(r.Right - d,  r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.Left,       r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
