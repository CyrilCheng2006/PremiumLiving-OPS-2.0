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
    /// KPI bar and dialogs follow ShowDetailDialog visual standard.
    /// </summary>
    public partial class ViewShipmentForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private List<ShipmentEntity> _currentShipments = new List<ShipmentEntity>();
        private ShipmentDetailVM     _selectedDetail;

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
            string status = _selectedDetail?.Shipment?.ShipmentStatus ?? string.Empty;
            btnViewDetail.Enabled       = hasRow;
            btnModify.Enabled           = hasRow;
            btnGenDeliveryNote.Enabled  = hasRow && _selectedDetail?.DeliveryNote == null;
            btnGenReplySlip.Enabled     = hasRow
                && _selectedDetail?.DeliveryNote != null
                && _selectedDetail?.ReplySlip    == null;
            btnScheduleShipment.Enabled = hasRow && status != "Completed";
        }

        // ── Action handlers
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            ShowDetailDialog(_selectedDetail);
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            if (_selectedDetail == null) return;
            using var dlg = new ModifyShipmentDialog(_ctrl, _selectedDetail);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            { _selectedDetail = null; RefreshGrid(); }
        }

        private void btnScheduleShipment_Click(object sender, EventArgs e)
        {
            using var dlg = new ScheduleShipmentDialog(_ctrl);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            { _selectedDetail = null; RefreshGrid(); }
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
            using var dlg = new GenerateDeliveryNoteForm(_selectedDetail);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _selectedDetail = _ctrl.GetShipmentDetail(_selectedDetail.Shipment.ShipmentID);
                UpdateActionButtons();
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

        // ── ShowDetailDialog (read-only, ShowDetailDialog standard)
        private void ShowDetailDialog(ShipmentDetailVM s)
        {
            var ship = s.Shipment;
            using var dlg = new Form
            {
                Text            = $"Shipment Detail \u2014 {ship.ShipmentID}",
                Size            = new Size(2500, 1100),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false
            };

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHeader.Controls.Add(new Label
            {
                Text      = $"Shipment Details  \u2014  {ship.ShipmentID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            StatusColors.TryGetValue(ship.ShipmentStatus ?? string.Empty, out var sc);
            tblHeader.Controls.Add(new Label
            {
                Text      = ship.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Padding = new Padding(8, 4, 8, 4)
            }, 1, 0);
            pnlHeader.Controls.Add(tblHeader);

            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 400,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += (sender, e) =>
            {
                using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
                e.Graphics.DrawLine(pen, 28, ((Panel)sender).Height - 1,
                                    ((Panel)sender).Width - 28, ((Panel)sender).Height - 1);
            };
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 5; r++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 14f));
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));
            var leftFields = new[]
            {
                ("Shipment ID",   ship.ShipmentID),
                ("Order ID",      ship.OrderID),
                ("Ship Date",     ship.ShipDate.ToString("yyyy-MM-dd")),
                ("Delivery Date", ship.DeliveryDate.HasValue
                                    ? ship.DeliveryDate.Value.ToString("yyyy-MM-dd")
                                    : ship.ShipDate.ToString("yyyy-MM-dd")),
                ("Tracking No.",  string.IsNullOrWhiteSpace(ship.TrackingNumber) ? "\u2014" : ship.TrackingNumber),
                ("Address",       ship.ShippingAddress ?? "\u2014"),
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(
                    i == 5 ? MakeLabelValMultiLine(leftFields[i].Item2 ?? "\u2014")
                           : MakeLabelVal(leftFields[i].Item2 ?? "\u2014"),
                    1, i);
            }
            var rightFields = new[]
            {
                ("Customer",        ship.CustomerName   ?? "\u2014"),
                ("Delivery Method", ship.DeliveryMethod ?? "\u2014"),
                ("Shipment Type",   ship.ShipmentType   ?? "\u2014"),
                ("Status",          ship.ShipmentStatus ?? "\u2014"),
                ("Total Amount",    $"HK$ {ship.TotalAmount:N2}"),
                ("",                ""),
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[i].Item2 ?? "\u2014"), 3, i);
            }
            pnlInfo.Controls.Add(tblInfo);

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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LINE ID",     FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM ID",     FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM NAME",   FillWeight = 34 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY SHIPPED", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OUTSTANDING", FillWeight = 18 });
            foreach (var line in s.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotalRow = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White };
            pnlTotalRow.Paint += PaintTopBorderStatic;
            var tblTotals = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTotals.Controls.Add(new Label
            {
                Text = $"Shipment Lines:   {s.Lines.Count}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(28, 0, 0, 0)
            }, 0, 0);
            tblTotals.Controls.Add(new Label
            {
                Text = $"Total Amount:   HK$ {ship.TotalAmount:N2}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotals);

            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 86,
                BackColor = Color.White, Padding = new Padding(28, 14, 28, 14)
            };
            pnlFooter.Paint += PaintTopBorderStatic;
            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(150, 56), Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(2500 - 28 - 150 - 16, 14)
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (_, __) => dlg.Close();
            pnlFooter.Controls.Add(btnClose);

            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlHeader);
            dlg.ShowDialog(this);
        }

        // ── Generate Reply Slip dialog — rebuilt to ShowDetailDialog standard
        private void ShowGenerateReplySlipDialog(ShipmentDetailVM s)
        {
            var ship = s.Shipment;
            using var dlg = new Form
            {
                Text            = $"Generate Reply Slip  \u2014  {ship.ShipmentID}",
                Size            = new Size(2500, 1100),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false
            };

            // ── Header
            var pnlH = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tblH = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text      = $"Generate Reply Slip  \u2014  {ship.ShipmentID}",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);
            StatusColors.TryGetValue(ship.ShipmentStatus ?? string.Empty, out var sc);
            tblH.Controls.Add(new Label
            {
                Text      = ship.ShipmentStatus ?? "Unknown",
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sc.fg != default ? sc.fg : Color.White,
                BackColor = sc.bg != default ? sc.bg : Color.FromArgb(80, 80, 80),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize  = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnlH.Controls.Add(tblH);

            // ── Info Panel (4-col, 4-row) — same layout as ShowDetailDialog
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 4; r++) tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            AddInfoRow(tblInfo, 0, "Shipment ID:",    ship.ShipmentID,                       "Order ID:",        ship.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",       ship.CustomerName,                     "Delivery Note:",   s.DeliveryNote?.DeliveryID ?? "\u2014");
            AddInfoRow(tblInfo, 2, "Ship Date:",      ship.ShipDate.ToString("yyyy-MM-dd"),  "Delivery Method:", ship.DeliveryMethod);
            AddInfoRow(tblInfo, 3, "Tracking No.:",   ship.TrackingNumber ?? "\u2014",           "Ship Type:",       ship.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            // ── Reply Slip input title bar
            var pnlSlipTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 253, 244), Padding = new Padding(28, 0, 16, 0)
            };
            pnlSlipTitle.Paint += PaintBottomBorderStatic;
            pnlSlipTitle.Controls.Add(new Label
            {
                Text      = "\u270D  Reply Slip  —  Required Fields",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            // ── Reply Slip input body (2-col: label + field)
            var pnlSlipBody = new Panel
            {
                Dock = DockStyle.Top, Height = 160,
                BackColor = Color.FromArgb(249, 254, 251), Padding = new Padding(28, 18, 28, 12)
            };
            pnlSlipBody.Paint += PaintBottomBorderStatic;
            var tblSlip = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblSlip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblSlip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblSlip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblSlip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblSlip.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            tblSlip.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            var txtRecip = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                PlaceholderText = "Full name of recipient",
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };
            var txtRemark = new TextBox
            {
                Font = new Font("Segoe UI", 12f),
                PlaceholderText = "Optional remark",
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill
            };

            tblSlip.Controls.Add(MakeLabelKey("Actual Recipient *:"), 0, 0);
            tblSlip.Controls.Add(txtRecip,                            1, 0);
            tblSlip.Controls.Add(MakeLabelKey("Remark:"),             2, 0);
            tblSlip.Controls.Add(txtRemark,                           3, 0);
            // Row 1: Ship Address (read-only, spans across for reference)
            tblSlip.Controls.Add(MakeLabelKey("Ship Address:"),                                     0, 1);
            tblSlip.Controls.Add(MakeLabelValMultiLine(ship.ShippingAddress ?? "\u2014"),           1, 1);
            tblSlip.Controls.Add(MakeLabelKey("Total Amount:"),                                     2, 1);
            tblSlip.Controls.Add(MakeLabelVal($"HK$ {ship.TotalAmount:N2}"),                       3, 1);
            pnlSlipBody.Controls.Add(tblSlip);

            // ── Section label
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

            // ── Items DataGridView (same spec as ShowDetailDialog)
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LINE ID",     FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM ID",     FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM NAME",   FillWeight = 34 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY SHIPPED", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OUTSTANDING", FillWeight = 18 });
            foreach (var line in s.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "\u2014");

            // ── Total row (left: Lines Count / right: Total Amount in blue)
            var pnlTotalRow = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White };
            pnlTotalRow.Paint += PaintTopBorderStatic;
            var tblTotals = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblTotals.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTotals.Controls.Add(new Label
            {
                Text = $"Shipment Lines:   {s.Lines.Count}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(28, 0, 0, 0)
            }, 0, 0);
            tblTotals.Controls.Add(new Label
            {
                Text = $"Total Amount:   HK$ {ship.TotalAmount:N2}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnlTotalRow.Controls.Add(tblTotals);

            // ── Footer — [✔ Generate Reply Slip]  [Cancel]
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 86,
                BackColor = Color.White, Padding = new Padding(28, 14, 28, 14)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnGen = new Button
            {
                Text      = "\u2714  Generate Reply Slip",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(22, 163, 74),
                FlatStyle = FlatStyle.Flat, Size = new Size(240, 56), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnGen.FlatAppearance.BorderSize         = 0;
            btnGen.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 131, 58);

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(160, 56), Cursor = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);

            // Dynamic right-anchored positioning (SizeChanged standard)
            pnlFooter.SizeChanged += (o, ev) =>
            {
                int top   = (pnlFooter.ClientSize.Height - 56) / 2;
                int rEdge = pnlFooter.ClientSize.Width - 28;
                btnGen.Location    = new Point(rEdge - 240,            top);
                btnCancel.Location = new Point(rEdge - 240 - 16 - 160, top);
            };
            btnGen.Location    = new Point(2500 - 28 - 240,                 (86 - 56) / 2);
            btnCancel.Location = new Point(2500 - 28 - 240 - 16 - 160,     (86 - 56) / 2);

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
                    string slipId = _ctrl.GenerateReplySlip(ship.ShipmentID, recip, remark);
                    MessageBox.Show($"Reply Slip {slipId} generated.", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _selectedDetail = _ctrl.GetShipmentDetail(ship.ShipmentID);
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
            btnCancel.Click += (_, __) => dlg.Close();

            pnlFooter.Controls.Add(btnGen);
            pnlFooter.Controls.Add(btnCancel);

            // Assemble: Fill → Bottom (reverse) → Top (reverse)
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlSlipBody);
            dlg.Controls.Add(pnlSlipTitle);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlH);
            dlg.ShowDialog(this);
        }

        // ── Grid cell formatting
        private void dgvShipments_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var col = dgvShipments.Columns[e.ColumnIndex];
            if (col.Name == "colStatus" && e.Value != null)
            {
                if (StatusColors.TryGetValue(e.Value.ToString(), out var colSc))
                {
                    e.CellStyle.BackColor          = colSc.bg;
                    e.CellStyle.ForeColor          = colSc.fg;
                    e.CellStyle.SelectionBackColor = colSc.bg;
                    e.CellStyle.SelectionForeColor = colSc.fg;
                    e.CellStyle.Font               = new Font("Segoe UI", 11f, FontStyle.Bold);
                    e.CellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;
                    e.FormattingApplied            = true;
                }
            }
        }

        // ── UI helpers
        private static Label MakeLabelKey(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(98, 112, 135),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 0, 8, 0), AutoEllipsis = false
        };

        private static Label MakeLabelVal(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
        };

        private static Label MakeLabelValMultiLine(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft,
            AutoEllipsis = false, AutoSize = false, Padding = new Padding(0, 8, 8, 4)
        };

        private static void AddInfoRow(
            TableLayoutPanel tbl, int row,
            string keyL, string valL, string keyR, string valR)
        {
            tbl.Controls.Add(MakeLabelKey(keyL),          0, row);
            tbl.Controls.Add(MakeLabelVal(valL ?? "—"),   1, row);
            tbl.Controls.Add(MakeLabelKey(keyR),          2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "—"),   3, row);
        }

        private static void PaintBottomBorderStatic(object s, PaintEventArgs e)
        {
            var p = (Panel)s;
            using var pen = new Pen(Color.FromArgb(221, 227, 236), 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
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
            path.CloseFigure();
            return path;
        }

        // ── Navigation / session
        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to log out?",
                                "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            { SessionManager.Clear(); Application.Restart(); }
        }
    }
}
