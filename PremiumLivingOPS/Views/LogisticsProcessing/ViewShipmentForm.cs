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
    /// KPI bar and View Detail dialog are redesigned to match
    /// the ViewOrderForm pattern exactly.
    /// </summary>
    public partial class ViewShipmentForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private List<ShipmentEntity> _currentShipments = new List<ShipmentEntity>();
        private ShipmentDetailVM     _selectedDetail;

        // ── Status colour palette ─────────────────────────────────────────
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
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
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
            string status = _selectedDetail?.Shipment?.ShipmentStatus ?? string.Empty;

            btnViewDetail.Enabled      = hasRow;
            btnModify.Enabled          = hasRow;
            btnGenDeliveryNote.Enabled = hasRow && _selectedDetail?.DeliveryNote == null;
            btnGenReplySlip.Enabled    = hasRow
                && _selectedDetail?.DeliveryNote != null
                && _selectedDetail?.ReplySlip    == null;
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

        private void btnScheduleShipment_Click(object sender, EventArgs e)
        {
            using var dlg = new ScheduleShipmentDialog(_ctrl);
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
        //  ShowDetailDialog
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowDetailDialog(ShipmentDetailVM s, bool editMode = false)
        {
            bool needsRefresh = false;
            var   ship        = s.Shipment;

            using var dlg = new Form
            {
                Text            = $"Shipment Detail \u2014 {ship.ShipmentID}",
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

            // ── Info panel ────────────────────────────────────────────────
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
            tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 30f));   // row 5: address multi-line

            // Left column fields
            // FIX: use ShippingAddress (correct property name from ShipmentEntity)
            var leftFields = new[]
            {
                ("Shipment ID",   ship.ShipmentID),
                ("Order ID",      ship.OrderID),
                ("Ship Date",     ship.ShipDate.ToString("yyyy-MM-dd")),
                ("Delivery Date", ship.DeliveryDate.HasValue
                                    ? ship.DeliveryDate.Value.ToString("yyyy-MM-dd")
                                    : ship.ShipDate.ToString("yyyy-MM-dd")),
                ("Tracking No.",  string.IsNullOrWhiteSpace(ship.TrackingNumber) ? "\u2014" : ship.TrackingNumber),
                ("Address",       ship.ShippingAddress ?? "\u2014"),  // was ship.DeliveryAddress — fixed
            };
            for (int i = 0; i < leftFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1), 0, i);
                tblInfo.Controls.Add(
                    i == 5 ? MakeLabelValMultiLine(leftFields[i].Item2 ?? "\u2014")
                           : MakeLabelVal(leftFields[i].Item2 ?? "\u2014"),
                    1, i);
            }

            // Right column fields
            // FIX: removed ship.ContactPerson (not in ShipmentEntity); replaced with DeliveryMethod
            var rightFields = new[]
            {
                ("Customer",        ship.CustomerName   ?? "\u2014"),
                ("Delivery Method", ship.DeliveryMethod ?? "\u2014"),
                ("Shipment Type",   ship.ShipmentType   ?? "\u2014"),
                ("Status",          ship.ShipmentStatus ?? "\u2014"),
                ("Total Amount",    $"HK$ {ship.TotalAmount:N2}"),
                ("",                ""),   // padding row
            };
            for (int i = 0; i < rightFields.Length; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[i].Item2 ?? "\u2014"), 3, i);
            }
            pnlInfo.Controls.Add(tblInfo);

            // ── Edit row (visible only in edit mode) ──────────────────────
            var cboStatusEdit = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12f), Width = 200
            };
            cboStatusEdit.Items.AddRange(new object[] { "Pending", "In Transit", "Completed" });
            int sIdx = cboStatusEdit.FindStringExact(ship.ShipmentStatus);
            cboStatusEdit.SelectedIndex = sIdx >= 0 ? sIdx : 0;

            var txtRecipient = new TextBox
            {
                Font = new Font("Segoe UI", 12f), Width = 260,
                PlaceholderText = "Actual recipient name",
                BorderStyle = BorderStyle.FixedSingle
            };

            var txtRemark = new TextBox
            {
                Font = new Font("Segoe UI", 12f), Width = 320,
                PlaceholderText = "Optional remark",
                BorderStyle = BorderStyle.FixedSingle
            };

            Panel pnlEditRow = null;
            if (editMode)
            {
                pnlEditRow = new Panel
                {
                    Dock = DockStyle.Top, Height = 60,
                    BackColor = Color.FromArgb(246, 249, 255),
                    Padding = new Padding(20, 10, 20, 10)
                };
                var tblEdit = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                    BackColor = Color.Transparent
                };
                tblEdit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
                tblEdit.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
                tblEdit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
                tblEdit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                tblEdit.Controls.Add(MakeEditCell("New Status", cboStatusEdit), 0, 0);
                tblEdit.Controls.Add(MakeEditCell("Recipient",  txtRecipient),  1, 0);
                tblEdit.Controls.Add(MakeEditCell("Remark",     txtRemark),     2, 0);
                pnlEditRow.Controls.Add(tblEdit);
            }

            // ── Section label for lines ───────────────────────────────────
            var pnlLineLabel = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255),
                Padding = new Padding(28, 0, 0, 0)
            };
            pnlLineLabel.Controls.Add(new Label
            {
                Text = "SHIPMENT ITEMS",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnlLineLabel.Paint += PaintBottomBorderStatic;

            // ── Lines grid ────────────────────────────────────────────────
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(221, 227, 236),
                Font = new Font("Segoe UI", 12f),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowTemplate = { Height = 44 }, Dock = DockStyle.Fill,
                ColumnHeadersHeight = 40, EnableHeadersVisualStyles = false,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(246, 249, 255),
                    ForeColor = Color.FromArgb(98, 112, 135),
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding   = new Padding(12, 0, 0, 0)
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
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "LINE ID",     FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM ID",     FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ITEM NAME",   FillWeight = 34 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "QTY SHIPPED", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OUTSTANDING", FillWeight = 18 });
            foreach (var line in s.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "\u2014");

            // ── Total row ─────────────────────────────────────────────────
            var pnlTotalRow = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = Color.White,
                Padding   = new Padding(0)
            };
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
                Text      = $"Shipment Lines:   {s.Lines.Count}",
                Dock      = DockStyle.Fill, AutoSize = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(28, 0, 0, 0)
            }, 0, 0);

            tblTotals.Controls.Add(new Label
            {
                Text      = $"Total Amount:   HK$ {ship.TotalAmount:N2}",
                Dock      = DockStyle.Fill, AutoSize = false,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight,
                Padding   = new Padding(0, 0, 28, 0)
            }, 1, 0);

            pnlTotalRow.Controls.Add(tblTotals);

            // ── Footer ────────────────────────────────────────────────────
            var pnlFooter = new Panel
            {
                Dock    = DockStyle.Bottom, Height = 86,
                BackColor = Color.White, Padding = new Padding(28, 14, 28, 14)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnSave = new Button
            {
                Text = "\u2714  Save Changes",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(47, 111, 237),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(200, 56), Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(2500 - 28 - 200 - 8 - 160 - 8 - 150 - 16, 14),
                Visible = editMode
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 77, 192);

            var btnDelete = new Button
            {
                Text = "\uD83D\uDDD1  Delete",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = Color.FromArgb(185, 28, 28),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 56), Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(2500 - 28 - 160 - 8 - 150 - 16, 14),
                Visible = editMode
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.FlatAppearance.MouseOverBackColor = Color.FromArgb(153, 27, 27);

            var btnClose = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 56), Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(2500 - 28 - 150 - 16, 14)
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (_, __) => dlg.Close();

            btnSave.Click += (_, __) =>
            {
                string newStatus    = cboStatusEdit.SelectedItem?.ToString();
                string newRecipient = txtRecipient.Text.Trim();
                string newRemark    = txtRemark.Text.Trim();
                try
                {
                    _ctrl.UpdateShipment(ship.ShipmentID, newStatus, newRecipient, newRemark);
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
                    $"Permanently delete Shipment {ship.ShipmentID} and all related records?\nThis cannot be undone.",
                    "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;
                try
                {
                    _ctrl.DeleteShipment(ship.ShipmentID);
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

            // ── Assemble dialog ───────────────────────────────────────────
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(pnlLineLabel);
            if (editMode && pnlEditRow != null)
                dlg.Controls.Add(pnlEditRow);
            dlg.Controls.Add(pnlInfo);
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
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void ShowGenerateReplySlipDialog(ShipmentDetailVM s)
        {
            var dlg = new Form
            {
                Text            = $"Generate Reply Slip  \u2014  {s.Shipment.ShipmentID}",
                Size            = new Size(1400, 880),
                MinimumSize     = new Size(1100, 700),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.FromArgb(240, 244, 249),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                Font            = new Font("Segoe UI", 13f)
            };

            var pnlH = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            pnlH.Controls.Add(new Label
            {
                Text = $"Generate Reply Slip  \u2014  {s.Shipment.ShipmentID}",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(24, 0, 0, 0)
            });

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

            tblForm.Controls.Add(MakeEditCell("Shipment ID",   MakeStaticLabel(s.Shipment.ShipmentID)), 0, 0);
            tblForm.Controls.Add(MakeEditCell("Delivery Note", MakeStaticLabel(s.DeliveryNote?.DeliveryID ?? "\u2014")), 1, 0);
            tblForm.Controls.Add(MakeEditCell("Customer",      MakeStaticLabel(s.Shipment.CustomerName ?? "\u2014")), 0, 1);
            tblForm.Controls.Add(MakeEditCell("Ship Date",     MakeStaticLabel(s.Shipment.ShipDate.ToString("yyyy-MM-dd"))), 1, 1);

            var txtRecip = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                PlaceholderText = "Full name of recipient",
                BorderStyle = BorderStyle.FixedSingle
            };
            tblForm.Controls.Add(MakeEditCell("Actual Recipient *", txtRecip), 0, 2);

            var txtRemarkSlip = new TextBox
            {
                Dock = DockStyle.Fill, Font = new Font("Segoe UI", 12f),
                PlaceholderText = "Optional remark",
                BorderStyle = BorderStyle.FixedSingle
            };
            tblForm.Controls.Add(MakeEditCell("Remark", txtRemarkSlip), 1, 2);

            var pnlFormCard = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlFormCard.Paint += PaintCardBorder;
            pnlFormCard.Controls.Add(tblForm);

            var pnlFormOuter = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 244, 249),
                Padding = new Padding(20, 14, 20, 8)
            };
            pnlFormOuter.Controls.Add(pnlFormCard);

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
                string remark = txtRemarkSlip.Text.Trim();
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
        //  Grid cell formatting
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  UI helpers
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
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
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        private static Label MakeLabelValMultiLine(string text) => new Label
        {
            Text         = text,
            Font         = new Font("Segoe UI", 12f),
            ForeColor    = Color.FromArgb(15, 31, 53),
            Dock         = DockStyle.Fill,
            TextAlign    = ContentAlignment.TopLeft,
            AutoEllipsis = false,
            AutoSize     = false,
            Padding      = new Padding(0, 8, 8, 4)
        };

        private static Label MakeStaticLabel(string text) => new Label
        {
            Text = text, Font = new Font("Segoe UI", 12f),
            ForeColor = Color.FromArgb(15, 31, 53),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false, AutoEllipsis = true
        };

        private static Panel MakeEditCell(string caption, Control ctrl)
        {
            var cell = new Panel
            {
                Dock = DockStyle.Fill, BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 14, 0)
            };
            var lbl = new Label
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
