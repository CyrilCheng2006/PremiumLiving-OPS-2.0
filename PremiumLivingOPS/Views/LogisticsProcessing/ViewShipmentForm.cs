using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Models.ViewModels;
using PremiumLivingOPS.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    /// <summary>
    /// View Shipment — main list form for Logistics Processing.
    ///
    /// Button state machine (per selected row):
    /// ──────────────────────────────────────────────
    /// btnGenDeliveryNote:
    ///   • No row selected         → disabled, green  "📄  Delivery Note"
    ///   • Row, DN == null          → enabled,  green  "📄  Delivery Note"   (Generate)
    ///   • Row, DN != null          → enabled,  blue   "👁  View Del. Note" (View)
    ///
    /// btnGenReplySlip:
    ///   • No row selected             → disabled, green  "🧾  Reply Slip"
    ///   • Row, DN == null             → disabled, green  "🧾  Reply Slip"   (need DN first)
    ///   • Row, DN != null, RS == null → enabled,  green  "🧾  Reply Slip"   (Generate)
    ///   • Row, DN != null, RS != null  → enabled,  blue   "👁  View Reply Slip" (View)
    /// </summary>
    public partial class ViewShipmentForm : Form
    {
        private readonly LogisticsProcessingController _ctrl =
            new LogisticsProcessingController();

        private List<ShipmentEntity> _shipments = new List<ShipmentEntity>();

        // ── Button colour constants (Green / Blue)
        private static readonly Color GreenNorm  = Color.FromArgb( 22, 163,  74);
        private static readonly Color GreenHover = Color.FromArgb( 16, 131,  58);
        private static readonly Color GreenDown  = Color.FromArgb( 10, 100,  40);
        private static readonly Color BlueNorm   = Color.FromArgb( 37, 99,  235);
        private static readonly Color BlueHover  = Color.FromArgb( 29,  78, 216);
        private static readonly Color BlueDown   = Color.FromArgb( 21,  56, 180);

        public ViewShipmentForm()
        {
            InitializeComponent();
            this.Load += ViewShipmentForm_Load;
        }

        private void ViewShipmentForm_Load(object sender, EventArgs e) => RefreshGrid();

        private void OnTopNavMenuItemClicked(string menuLabel, string subItem)
            => FormNavigator.NavigateTo(this, menuLabel, subItem);

        private void btnLogout_Click(object sender, EventArgs e)
        { SessionManager.Clear(); Application.Restart(); }

        // ── Grid refresh ────────────────────────────────────────────────────────
        private void RefreshGrid()
        {
            string statusSel = cboStatus.SelectedItem?.ToString();
            string statusFilter = (string.IsNullOrEmpty(statusSel) || statusSel == "All")
                ? null : statusSel;
            string keyword = txtSearch.Text.Trim();

            var vm = _ctrl.GetShipmentListVM(
                statusFilter,
                string.IsNullOrEmpty(keyword) ? null : keyword);

            _shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
            _shell.SetVisibleMenus(vm.AllowedMenus);
            _shell.SetBreadcrumb("Logistics  ›  View Shipment");

            _shipments = vm.Shipments;
            dgvShipments.Rows.Clear();
            foreach (var s in _shipments)
                dgvShipments.Rows.Add(
                    s.ShipmentID, s.OrderID,
                    s.ShipDate.ToString("yyyy-MM-dd"),
                    s.CustomerName,
                    s.ShipmentType, s.DeliveryMethod,
                    s.ShipmentStatus,
                    s.TrackingNumber ?? "—");
        }

        private void ResetSearch()
        {
            txtSearch.Text         = string.Empty;
            cboStatus.SelectedIndex = 0;
            RefreshGrid();
        }

        // ── Selection → update button states ────────────────────────────────────
        private void dgvShipments_SelectionChanged(object sender, EventArgs e)
        {
            bool hasRow = dgvShipments.SelectedRows.Count > 0
                       && dgvShipments.SelectedRows[0].Index >= 0;

            btnViewDetail.Enabled      = hasRow;
            btnModify.Enabled          = hasRow;
            btnScheduleShipment.Enabled = hasRow;

            if (!hasRow)
            {
                btnGenDeliveryNote.Enabled = false;
                btnGenReplySlip.Enabled    = false;
                return;
            }

            int idx = dgvShipments.SelectedRows[0].Index;
            if (idx < 0 || idx >= _shipments.Count) return;
            var ship = _shipments[idx];
            var detail = _ctrl.GetShipmentDetailVM(ship.ShipmentID);

            // ── Delivery Note button
            btnGenDeliveryNote.Enabled = true;
            if (detail.DeliveryNote != null)
            {
                btnGenDeliveryNote.Text = "\U0001F441  View Del. Note";
                ApplyBtnColour(btnGenDeliveryNote, BlueNorm, BlueHover, BlueDown);
            }
            else
            {
                btnGenDeliveryNote.Text = "\U0001F4C4  Delivery Note";
                ApplyBtnColour(btnGenDeliveryNote, GreenNorm, GreenHover, GreenDown);
            }

            // ── Reply Slip button
            if (detail.DeliveryNote == null)
            {
                btnGenReplySlip.Enabled = false;
                btnGenReplySlip.Text = "\U0001F9FE  Reply Slip";
                ApplyBtnColour(btnGenReplySlip, GreenNorm, GreenHover, GreenDown);
            }
            else if (detail.ReplySlip == null)
            {
                btnGenReplySlip.Enabled = true;
                if (ship.ShipmentStatus == "Completed")
                {
                    btnGenReplySlip.Text = "\U0001F441  View Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, BlueNorm, BlueHover, BlueDown);
                }
                else
                {
                    btnGenReplySlip.Text = "\U0001F9FE  Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, GreenNorm, GreenHover, GreenDown);
                }
            }
            else
            {
                btnGenReplySlip.Enabled = true;
                if (detail.ReplySlip != null)
                {
                    btnGenReplySlip.Text = "\U0001F441  View Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, BlueNorm, BlueHover, BlueDown);
                }
                else
                {
                    btnGenReplySlip.Text = "\U0001F9FE  Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, GreenNorm, GreenHover, GreenDown);
                }
                if (detail.ReplySlip == null)
                {
                    btnGenReplySlip.Enabled = false;
                    btnGenReplySlip.Text = "\U0001F9FE  Reply Slip";
                    ApplyBtnColour(btnGenReplySlip, GreenNorm, GreenHover, GreenDown);
                }
            }
        }

        private static void ApplyBtnColour(Button btn, Color norm, Color hover, Color down)
        {
            btn.BackColor = norm;
            btn.FlatAppearance.MouseOverBackColor = hover;
            btn.FlatAppearance.MouseDownBackColor = down;
        }

        // ── Button clicks ────────────────────────────────────────────────────────
        private void btnViewDetail_Click(object sender, EventArgs e)
        {
            int idx = dgvShipments.SelectedRows[0].Index;
            if (idx < 0 || idx >= _shipments.Count) return;
            ShowDetailDialog(_ctrl.GetShipmentDetailVM(_shipments[idx].ShipmentID));
        }

        private void btnModify_Click(object sender, EventArgs e)
        {
            int idx = dgvShipments.SelectedRows[0].Index;
            if (idx < 0 || idx >= _shipments.Count) return;
            var ship = _shipments[idx];
            using var dlg = new ModifyShipmentDialog(ship.ShipmentID);
            if (dlg.ShowDialog(this) == DialogResult.OK) RefreshGrid();
        }

        private void btnScheduleShipment_Click(object sender, EventArgs e)
        {
            int idx = dgvShipments.SelectedRows[0].Index;
            if (idx < 0 || idx >= _shipments.Count) return;
            using var dlg = new ScheduleShipmentDialog(_shipments[idx].ShipmentID);
            if (dlg.ShowDialog(this) == DialogResult.OK) RefreshGrid();
        }

        private void btnGenDeliveryNote_Click(object sender, EventArgs e)
        {
            int idx = dgvShipments.SelectedRows[0].Index;
            if (idx < 0 || idx >= _shipments.Count) return;
            var detail = _ctrl.GetShipmentDetailVM(_shipments[idx].ShipmentID);

            if (detail.DeliveryNote != null)
                ShowViewDeliveryNoteDialog(detail);
            else
            {
                using var form = new GenerateDeliveryNoteForm(detail);
                if (form.ShowDialog(this) == DialogResult.OK) RefreshGrid();
            }
        }

        private void btnGenReplySlip_Click(object sender, EventArgs e)
        {
            int idx = dgvShipments.SelectedRows[0].Index;
            if (idx < 0 || idx >= _shipments.Count) return;
            var detail = _ctrl.GetShipmentDetailVM(_shipments[idx].ShipmentID);

            if (detail.ReplySlip != null)
                ShowViewReplySlipDialog(detail);
            else
                ShowGenerateReplySlipDialog(detail);
        }

        // ────────────────────────────────────────────────────────────────────────
        //  Show Detail Dialog (Shipment Detail)
        // ────────────────────────────────────────────────────────────────────────
        private void ShowDetailDialog(ShipmentDetailVM s)
        {
            var ship = s.Shipment;

            using var dlg = new Form
            {
                Text            = $"Shipment Detail  \u2014  {ship.ShipmentID}",
                Size            = new Size(2500, 1100),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false
            };

            var pnlH = BuildNavyHeader(
                $"Shipment Detail  \u2014  {ship.ShipmentID}",
                ship.ShipmentStatus);

            // ── Info panel (2-col, 6 rows)
            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 340,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;

            var tblInfo = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tblInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            for (int r = 0; r < 6; r++)
                tblInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6f));

            // Build left/right field lists
            var leftFields = new (string, string)[]
            {
                ("Shipment ID:",    ship.ShipmentID),
                ("Customer:",       ship.CustomerName),
                ("Ship Date:",      ship.ShipDate.ToString("yyyy-MM-dd")),
                ("Ship Type:",      ship.ShipmentType),
                ("Tracking No.:",   ship.TrackingNumber ?? "\u2014"),
                ("Address",         ship.ShippingAddress ?? "\u2014"),
            };
            var rightFields = new (string, string)[]
            {
                ("Order ID:",        ship.OrderID),
                ("Status:",          ship.ShipmentStatus),
                ("Delivery Method:", ship.DeliveryMethod),
                ("Total Amount:",    $"HK$ {ship.TotalAmount:N2}"),
                ("Notes:",           ship.Notes ?? "\u2014"),
                ("",                 ""),
            };

            for (int i = 0; i < 6; i++)
            {
                tblInfo.Controls.Add(MakeLabelKey(leftFields[i].Item1),  0, i);
                tblInfo.Controls.Add(
                    i == 5 ? MakeLabelValMultiLine(leftFields[i].Item2 ?? "\u2014")
                           : MakeLabelVal(leftFields[i].Item2 ?? "\u2014"), 1, i);
                tblInfo.Controls.Add(MakeLabelKey(rightFields[i].Item1), 2, i);
                tblInfo.Controls.Add(MakeLabelVal(rightFields[i].Item2 ?? "\u2014"), 3, i);
            }

            pnlInfo.Controls.Add(tblInfo);

            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");
            var dgv          = BuildItemsGrid();
            foreach (var line in s.Lines ?? new List<ShipmentLineEntity>())
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotalRow = BuildTotalRow(s.Lines?.Count ?? 0, ship.TotalAmount);
            var pnlFooter   = BuildDocFooter(dlg, null, null);

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlH);
            dlg.ShowDialog(this);
        }

        // ────────────────────────────────────────────────────────────────────────
        //  View Delivery Note dialog (read-only)
        // ────────────────────────────────────────────────────────────────────────
        private void ShowViewDeliveryNoteDialog(ShipmentDetailVM s)
        {
            var ship = s.Shipment;
            var dn   = s.DeliveryNote;
            if (dn == null) return;

            int outQty = 0;
            foreach (var line in s.Lines ?? new List<ShipmentLineEntity>())
                outQty += line.QtyOutstanding ?? 0;

            using var dlg = new Form
            {
                Text            = $"Delivery Note  \u2014  {dn.DeliveryID}",
                Size            = new Size(2500, 1100),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false
            };

            var pnlH = BuildNavyHeader(
                $"Delivery Note  \u2014  {dn.DeliveryID}",
                ship.ShipmentStatus);

            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = Build4ColTlp(4);
            AddInfoRow(tblInfo, 0, "Shipment ID:",  ship.ShipmentID,                      "Order ID:",        ship.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",     ship.CustomerName,                    "Tracking No.:",    ship.TrackingNumber ?? "\u2014");
            AddInfoRow(tblInfo, 2, "Ship Date:",    ship.ShipDate.ToString("yyyy-MM-dd"), "Delivery Method:", ship.DeliveryMethod);
            AddInfoRow(tblInfo, 3, "Status:",       ship.ShipmentStatus,                  "Ship Type:",       ship.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            var pnlDNTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 253, 244), Padding = new Padding(28, 0, 16, 0)
            };
            pnlDNTitle.Paint += PaintBottomBorderStatic;
            pnlDNTitle.Controls.Add(new Label
            {
                Text      = $"\u2709  Delivery Note  \u2014  {dn.DeliveryID}  (Date: {dn.DeliveryDate:yyyy-MM-dd})",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            var pnlDNBody = new Panel
            {
                Dock = DockStyle.Top, Height = 180,
                BackColor = Color.FromArgb(249, 254, 251), Padding = new Padding(28, 12, 28, 12)
            };
            pnlDNBody.Paint += PaintBottomBorderStatic;
            var tblDN = Build4ColTlp(3, 28f, 44f, 28f);
            AddInfoRow(tblDN, 0, "Delivery Date:",  dn.DeliveryDate.ToString("yyyy-MM-dd"), "Ship To:",       ship.CustomerName);
            tblDN.Controls.Add(MakeLabelKey("Ship Address:"),                               0, 1);
            tblDN.Controls.Add(MakeLabelValMultiLine(ship.ShippingAddress ?? "\u2014"),     1, 1);
            tblDN.Controls.Add(MakeLabelKey("Outstanding Qty:"),                            2, 1);
            tblDN.Controls.Add(MakeLabelVal(outQty.ToString()),                             3, 1);
            AddInfoRow(tblDN, 2, "Delivery Method:", ship.DeliveryMethod, "Shipment Type:", ship.ShipmentType);
            pnlDNBody.Controls.Add(tblDN);

            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");
            var dgv          = BuildItemsGrid();
            foreach (var line in s.Lines ?? new List<ShipmentLineEntity>())
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotalRow = BuildTotalRow(s.Lines?.Count ?? 0, ship.TotalAmount);

            EventHandler pdfHandler = (_, __) =>
            {
                using var sfd = new SaveFileDialog
                {
                    Title           = "Export Delivery Note as PDF",
                    Filter          = "PDF Files (*.pdf)|*.pdf",
                    FileName        = $"DeliveryNote_{dn.DeliveryID}_{DateTime.Now:yyyyMMdd}.pdf",
                    DefaultExt      = "pdf",
                    OverwritePrompt = true
                };
                if (sfd.ShowDialog(dlg) != DialogResult.OK) return;
                try
                {
                    PdfExporter.ExportDeliveryNote(s, sfd.FileName);
                    MessageBox.Show($"PDF saved:\n{sfd.FileName}",
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var pnlFooter = BuildDocFooter(dlg, "\U0001F4C4  Export PDF", pdfHandler);

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlDNBody);
            dlg.Controls.Add(pnlDNTitle);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlH);
            dlg.ShowDialog(this);
        }

        // ────────────────────────────────────────────────────────────────────────
        //  View Reply Slip dialog (read-only)
        // ────────────────────────────────────────────────────────────────────────
        private void ShowViewReplySlipDialog(ShipmentDetailVM s)
        {
            var ship = s.Shipment;
            var rs   = s.ReplySlip;
            if (rs == null) return;

            using var dlg = new Form
            {
                Text            = $"Reply Slip  \u2014  {rs.SlipID}",
                Size            = new Size(2500, 1100),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                Font            = new Font("Segoe UI", 13f),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false, MinimizeBox = false
            };

            var pnlH = BuildNavyHeader(
                $"Reply Slip  \u2014  {rs.SlipID}",
                ship.ShipmentStatus);

            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = Build4ColTlp(4);
            AddInfoRow(tblInfo, 0, "Shipment ID:",  ship.ShipmentID,                      "Order ID:",        ship.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",     ship.CustomerName,                    "Delivery Note:",   s.DeliveryNote?.DeliveryID ?? "\u2014");
            AddInfoRow(tblInfo, 2, "Ship Date:",    ship.ShipDate.ToString("yyyy-MM-dd"), "Delivery Method:", ship.DeliveryMethod);
            AddInfoRow(tblInfo, 3, "Tracking No.:", ship.TrackingNumber ?? "\u2014",       "Ship Type:",       ship.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            var pnlSlipTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 253, 244), Padding = new Padding(28, 0, 16, 0)
            };
            pnlSlipTitle.Paint += PaintBottomBorderStatic;
            pnlSlipTitle.Controls.Add(new Label
            {
                Text      = $"\U0001F9FE  Reply Slip  \u2014  {rs.SlipID}  (Received: {rs.ReceivedDate:yyyy-MM-dd})",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            var pnlSlipBody = new Panel
            {
                Dock = DockStyle.Top, Height = 180,
                BackColor = Color.FromArgb(249, 254, 251), Padding = new Padding(28, 12, 28, 12)
            };
            pnlSlipBody.Paint += PaintBottomBorderStatic;
            var tblSlip = Build4ColTlp(2, 50f, 50f);
            AddInfoRow(tblSlip, 0,
                "Actual Recipient:", rs.ActualRecipient  ?? "\u2014",
                "Remark:",           rs.RecipientRemark  ?? "\u2014");
            tblSlip.Controls.Add(MakeLabelKey("Ship Address:"),                              0, 1);
            tblSlip.Controls.Add(MakeLabelValMultiLine(ship.ShippingAddress ?? "\u2014"),    1, 1);
            tblSlip.Controls.Add(MakeLabelKey("Total Amount:"),                              2, 1);
            tblSlip.Controls.Add(MakeLabelVal($"HK$ {ship.TotalAmount:N2}"),                3, 1);
            pnlSlipBody.Controls.Add(tblSlip);

            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");
            var dgv          = BuildItemsGrid();
            foreach (var line in s.Lines ?? new List<ShipmentLineEntity>())
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotalRow = BuildTotalRow(s.Lines?.Count ?? 0, ship.TotalAmount);

            EventHandler pdfHandler = (_, __) =>
            {
                using var sfd = new SaveFileDialog
                {
                    Title           = "Export Reply Slip as PDF",
                    Filter          = "PDF Files (*.pdf)|*.pdf",
                    FileName        = $"ReplySlip_{rs.SlipID}_{DateTime.Now:yyyyMMdd}.pdf",
                    DefaultExt      = "pdf",
                    OverwritePrompt = true
                };
                if (sfd.ShowDialog(dlg) != DialogResult.OK) return;
                try
                {
                    PdfExporter.ExportReplySlip(s, sfd.FileName);
                    MessageBox.Show($"PDF saved:\n{sfd.FileName}",
                        "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var pnlFooter = BuildDocFooter(dlg, "\U0001F9FE  Export PDF", pdfHandler);

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlSlipBody);
            dlg.Controls.Add(pnlSlipTitle);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlH);
            dlg.ShowDialog(this);
        }

        // ────────────────────────────────────────────────────────────────────────
        //  Generate Reply Slip dialog
        // ────────────────────────────────────────────────────────────────────────
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

            var pnlH = BuildNavyHeader(
                $"Generate Reply Slip  \u2014  {ship.ShipmentID}",
                ship.ShipmentStatus);

            var pnlInfo = new Panel
            {
                Dock = DockStyle.Top, Height = 220,
                Padding = new Padding(28, 18, 28, 8), BackColor = Color.White
            };
            pnlInfo.Paint += PaintBottomBorderStatic;
            var tblInfo = Build4ColTlp(4);
            AddInfoRow(tblInfo, 0, "Shipment ID:",  ship.ShipmentID,                      "Order ID:",        ship.OrderID);
            AddInfoRow(tblInfo, 1, "Customer:",     ship.CustomerName,                    "Delivery Note:",   s.DeliveryNote?.DeliveryID ?? "\u2014");
            AddInfoRow(tblInfo, 2, "Ship Date:",    ship.ShipDate.ToString("yyyy-MM-dd"), "Delivery Method:", ship.DeliveryMethod);
            AddInfoRow(tblInfo, 3, "Tracking No.:", ship.TrackingNumber ?? "\u2014",       "Ship Type:",       ship.ShipmentType);
            pnlInfo.Controls.Add(tblInfo);

            var pnlSlipTitle = new Panel
            {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(240, 253, 244), Padding = new Padding(28, 0, 16, 0)
            };
            pnlSlipTitle.Paint += PaintBottomBorderStatic;
            pnlSlipTitle.Controls.Add(new Label
            {
                Text      = "\u270D  Reply Slip  \u2014  Required Fields",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(6, 95, 70),
                Dock      = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            });

            var pnlSlipBody = new Panel
            {
                Dock = DockStyle.Top, Height = 160,
                BackColor = Color.FromArgb(249, 254, 251), Padding = new Padding(28, 18, 28, 12)
            };
            pnlSlipBody.Paint += PaintBottomBorderStatic;
            var tblSlip = Build4ColTlp(2, 50f, 50f);

            var txtRecip = new TextBox
            {
                Font = new Font("Segoe UI", 12f), PlaceholderText = "Full name of recipient",
                BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill
            };
            var txtRemark = new TextBox
            {
                Font = new Font("Segoe UI", 12f), PlaceholderText = "Optional remark",
                BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill
            };
            tblSlip.Controls.Add(MakeLabelKey("Actual Recipient *:"), 0, 0);
            tblSlip.Controls.Add(txtRecip,                            1, 0);
            tblSlip.Controls.Add(MakeLabelKey("Remark:"),             2, 0);
            tblSlip.Controls.Add(txtRemark,                           3, 0);
            tblSlip.Controls.Add(MakeLabelKey("Ship Address:"),                              0, 1);
            tblSlip.Controls.Add(MakeLabelValMultiLine(ship.ShippingAddress ?? "\u2014"),    1, 1);
            tblSlip.Controls.Add(MakeLabelKey("Total Amount:"),                              2, 1);
            tblSlip.Controls.Add(MakeLabelVal($"HK$ {ship.TotalAmount:N2}"),                3, 1);
            pnlSlipBody.Controls.Add(tblSlip);

            var pnlLineLabel = BuildSectionLabel("SHIPMENT ITEMS");
            var dgv          = BuildItemsGrid();
            foreach (var line in s.Lines)
                dgv.Rows.Add(line.ShipmentLineID, line.ItemID, line.ItemName,
                             line.QtyShipped, line.QtyOutstanding?.ToString() ?? "\u2014");

            var pnlTotalRow = BuildTotalRow(s.Lines.Count, ship.TotalAmount);

            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 90,
                BackColor = Color.White, Padding = new Padding(28, 15, 28, 15)
            };
            pnlFooter.Paint += PaintTopBorderStatic;

            var btnGen = new Button
            {
                Text      = "\u2714  Confirm Generate",
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = GreenNorm,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(220, 60), Cursor = Cursors.Hand
            };
            btnGen.FlatAppearance.BorderSize         = 0;
            btnGen.FlatAppearance.MouseOverBackColor = GreenHover;
            btnGen.FlatAppearance.MouseDownBackColor = GreenDown;

            var btnCancel = new Button
            {
                Text      = "Cancel",
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(160, 60), Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize         = 1;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnCancel.Click += (_, __) => dlg.Close();

            pnlFooter.Controls.Add(btnGen);
            pnlFooter.Controls.Add(btnCancel);
            pnlFooter.Resize += (_, __) =>
            {
                int top   = (pnlFooter.Height - 60) / 2;
                int right = pnlFooter.Width - 28;
                btnGen.Location    = new Point(right - 220,       top);
                btnCancel.Location = new Point(right - 220 - 16 - 160, top);
            };

            btnGen.Click += (_, __) =>
            {
                string recip  = txtRecip.Text.Trim();
                string remark = txtRemark.Text.Trim();
                if (string.IsNullOrEmpty(recip))
                {
                    MessageBox.Show("Actual Recipient is required.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    string slipId = _ctrl.GenerateReplySlip(
                        s.DeliveryNote.DeliveryID, recip,
                        string.IsNullOrEmpty(remark) ? null : remark);
                    MessageBox.Show($"Reply Slip {slipId} generated successfully.",
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to generate Reply Slip:\n{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.Add(pnlFooter);
            dlg.Controls.Add(pnlTotalRow);
            dlg.Controls.Add(dgv);
            dlg.Controls.Add(pnlLineLabel);
            dlg.Controls.Add(pnlSlipBody);
            dlg.Controls.Add(pnlSlipTitle);
            dlg.Controls.Add(pnlInfo);
            dlg.Controls.Add(pnlH);
            dlg.ShowDialog(this);
            if (dlg.DialogResult == DialogResult.OK) RefreshGrid();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  KPI refresh
        // ════════════════════════════════════════════════════════════════════════
        private void RefreshKpi()
        {
            pnlKpi.Controls.Clear();

            var allShipments = _ctrl.GetShipmentListVM().Shipments;

            int total      = allShipments.Count;
            int inTransit  = 0;
            int pending    = 0;
            int completed  = 0;

            foreach (var s in allShipments)
            {
                if (s.ShipmentStatus == "In Transit") inTransit++;
                if (s.ShipmentStatus == "Pending")    pending++;
                if (s.ShipmentStatus == "Completed")  completed++;
            }

            AddKpiPill($"Total: {total}",         Color.FromArgb(219, 234, 254), Color.FromArgb(29,  78, 216));
            AddKpiPill($"In Transit: {inTransit}", Color.FromArgb(219, 234, 254), Color.FromArgb(29,  78, 216));
            AddKpiPill($"Pending: {pending}",      Color.FromArgb(254, 243, 199), Color.FromArgb(146, 64,  14));
            AddKpiPill($"Completed: {completed}",  Color.FromArgb(220, 252, 231), Color.FromArgb(22, 101,  52));
        }

        private void AddKpiPill(string text, Color bg, Color fg)
        {
            const int PH = 36, PW = 170, PR = 18;
            var lbl = new Label
            {
                Text = text, AutoSize = false,
                Size = new Size(PW, PH),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = fg, BackColor = bg,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 8, 0)
            };
            lbl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(new Rectangle(0, 0, lbl.Width, lbl.Height), PR);
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var fgBrush = new SolidBrush(fg);
                g.DrawString(text, lbl.Font, fgBrush, new RectangleF(0, 0, lbl.Width, lbl.Height), sf);
            };
            pnlKpi.Controls.Add(lbl);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Shared builder helpers
        // ════════════════════════════════════════════════════════════════════════

        private static Panel BuildNavyHeader(string title, string statusText)
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(19, 35, 61) };
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding = new Padding(24, 0, 24, 0)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 264f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tbl.Controls.Add(new Label
            {
                Text = title, Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.White, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, AutoSize = false
            }, 0, 0);

            Color sbg = Color.FromArgb(80, 80, 80), sfg = Color.White;
            if (statusText == "Pending")    { sbg = Color.FromArgb(254, 243, 199); sfg = Color.FromArgb(146, 64, 14); }
            if (statusText == "In Transit") { sbg = Color.FromArgb(219, 234, 254); sfg = Color.FromArgb( 29, 78,216); }
            if (statusText == "Completed")  { sbg = Color.FromArgb(209, 250, 229); sfg = Color.FromArgb(  6, 95, 70); }

            tbl.Controls.Add(new Label
            {
                Text = statusText, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = sfg, BackColor = sbg,
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false, Margin = new Padding(0, 14, 0, 14)
            }, 1, 0);
            pnl.Controls.Add(tbl);
            return pnl;
        }

        private static TableLayoutPanel Build4ColTlp(int rows,
            params float[] rowHeightPercents)
        {
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 4, RowCount = rows,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            if (rowHeightPercents.Length == rows)
            {
                foreach (float p in rowHeightPercents)
                    tbl.RowStyles.Add(new RowStyle(SizeType.Percent, p));
            }
            else
            {
                float each = 100f / rows;
                for (int i = 0; i < rows; i++)
                    tbl.RowStyles.Add(new RowStyle(SizeType.Percent, each));
            }
            return tbl;
        }

        private static Panel BuildSectionLabel(string text)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(246, 249, 255), Padding = new Padding(28, 0, 0, 0)
            };
            pnl.Controls.Add(new Label
            {
                Text = text, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            });
            pnl.Paint += PaintBottomBorderStatic;
            return pnl;
        }

        private static DataGridView BuildItemsGrid()
        {
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
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Padding = new Padding(12, 0, 0, 0)
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
            return dgv;
        }

        private static Panel BuildTotalRow(int lineCount, double totalAmount)
        {
            var pnl = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.White };
            pnl.Paint += PaintTopBorderStatic;
            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
                BackColor = Color.Transparent, CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tbl.Controls.Add(new Label
            {
                Text = $"Shipment Lines:   {lineCount}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(28, 0, 0, 0)
            }, 0, 0);
            tbl.Controls.Add(new Label
            {
                Text = $"Total Amount:   HK$ {totalAmount:N2}",
                Dock = DockStyle.Fill, AutoSize = false,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(47, 111, 237),
                TextAlign = ContentAlignment.MiddleRight, Padding = new Padding(0, 0, 28, 0)
            }, 1, 0);
            pnl.Controls.Add(tbl);
            return pnl;
        }

        private static Panel BuildDocFooter(Form dlg, string exportBtnText, EventHandler exportHandler)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Bottom, Height = 86,
                BackColor = Color.White, Padding = new Padding(28, 13, 28, 13)
            };
            pnl.Paint += PaintTopBorderStatic;

            var btnClose = new Button
            {
                Text = "Close", Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53), BackColor = Color.White,
                FlatStyle = FlatStyle.Flat, Size = new Size(160, 60), Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderColor        = Color.FromArgb(221, 227, 236);
            btnClose.FlatAppearance.BorderSize         = 1;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 244, 249);
            btnClose.Click += (_, __) => dlg.Close();

            pnl.Controls.Add(btnClose);

            if (!string.IsNullOrEmpty(exportBtnText) && exportHandler != null)
            {
                var btnExport = new Button
                {
                    Text = exportBtnText,
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.White, BackColor = Color.FromArgb(37, 99, 235),
                    FlatStyle = FlatStyle.Flat, Size = new Size(220, 60), Cursor = Cursors.Hand
                };
                btnExport.FlatAppearance.BorderSize         = 0;
                btnExport.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 216);
                btnExport.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 56, 180);
                btnExport.Click += exportHandler;
                pnl.Controls.Add(btnExport);

                pnl.Resize += (_, __) =>
                {
                    int top   = (pnl.Height - 60) / 2;
                    int right = pnl.Width - 28;
                    btnExport.Location = new Point(right - 220,            top);
                    btnClose.Location  = new Point(right - 220 - 16 - 160, top);
                };
            }
            else
            {
                pnl.Resize += (_, __) =>
                {
                    btnClose.Location = new Point(
                        pnl.Width - 28 - 160,
                        (pnl.Height - 60) / 2);
                };
            }

            return pnl;
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Label / cell helpers
        // ════════════════════════════════════════════════════════════════════════

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
            tbl.Controls.Add(MakeLabelVal(valL ?? "\u2014"),  1, row);
            tbl.Controls.Add(MakeLabelKey(keyR),          2, row);
            tbl.Controls.Add(MakeLabelVal(valR ?? "\u2014"),  3, row);
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
        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
