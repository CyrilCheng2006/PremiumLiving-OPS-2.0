namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    partial class GenerateDeliveryNoteForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        // ── Controls ──────────────────────────────────────────────────
        // Header
        private System.Windows.Forms.Panel     pnlHeader;
        private System.Windows.Forms.Label     lblHeader;

        // Outer grey layer (CardPanel Level 1)
        private System.Windows.Forms.Panel     pnlOuter;

        // ── Shipment Info Card ────────────────────────────────────────
        private System.Windows.Forms.Panel     pnlShipCard;          // white card
        private System.Windows.Forms.Label     lblShipCardTitle;
        private System.Windows.Forms.TableLayoutPanel tlpShipInfo;
        // Labels (captions)
        private System.Windows.Forms.Label     lblCapShipmentID;
        private System.Windows.Forms.Label     lblCapOrderID;
        private System.Windows.Forms.Label     lblCapShipDate;
        private System.Windows.Forms.Label     lblCapShipStatus;
        // Values
        private System.Windows.Forms.Label     lblShipmentID;
        private System.Windows.Forms.Label     lblOrderID;
        private System.Windows.Forms.Label     lblShipDate;
        private System.Windows.Forms.Label     lblShipStatus;

        // ── Delivery Note Preview Card ────────────────────────────────
        private System.Windows.Forms.Panel     pnlDNCard;
        private System.Windows.Forms.Label     lblDNCardTitle;
        private System.Windows.Forms.TableLayoutPanel tlpDNInfo;
        // Captions
        private System.Windows.Forms.Label     lblCapDeliveryDate;
        private System.Windows.Forms.Label     lblCapShipToName;
        private System.Windows.Forms.Label     lblCapShippingAddress;
        private System.Windows.Forms.Label     lblCapOutstandingQty;
        // Values
        private System.Windows.Forms.Label     lblDeliveryDate;
        private System.Windows.Forms.Label     lblShipToName;
        private System.Windows.Forms.Label     lblShippingAddress;
        private System.Windows.Forms.Label     lblOutstandingQty;

        // ── Already Exists Warning ────────────────────────────────────
        private System.Windows.Forms.Panel     pnlAlreadyExists;
        private System.Windows.Forms.Label     lblAlreadyExistsIcon;
        private System.Windows.Forms.Label     lblExistingDN;

        // ── Items Grid Card ───────────────────────────────────────────
        private System.Windows.Forms.Panel     pnlGridCard;
        private System.Windows.Forms.Label     lblGridCardTitle;
        private System.Windows.Forms.DataGridView dgvLines;

        // ── Footer ────────────────────────────────────────────────────
        private System.Windows.Forms.Panel     pnlFooter;
        private System.Windows.Forms.Button    btnConfirm;
        private System.Windows.Forms.Button    btnCancel;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ── Form ──────────────────────────────────────────────────
            this.Text            = "Generate Delivery Note";
            this.Size            = new System.Drawing.Size(1400, 600);
            this.MinimumSize     = new System.Drawing.Size(900, 500);
            this.StartPosition   = System.Windows.Forms.FormStartPosition.CenterParent;
            this.BackColor       = System.Drawing.Color.FromArgb(243, 244, 246);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Font            = new System.Drawing.Font("Segoe UI", 9.5f);

            // ── Header ────────────────────────────────────────────────
            pnlHeader           = new System.Windows.Forms.Panel();
            pnlHeader.Dock      = System.Windows.Forms.DockStyle.Top;
            pnlHeader.Height    = 56;
            pnlHeader.BackColor = System.Drawing.Color.FromArgb(6, 95, 70);
            pnlHeader.Padding   = new System.Windows.Forms.Padding(20, 0, 20, 0);

            lblHeader           = new System.Windows.Forms.Label();
            lblHeader.Text      = "Generate Delivery Note";
            lblHeader.Font      = new System.Drawing.Font("Segoe UI", 14f, System.Drawing.FontStyle.Bold);
            lblHeader.ForeColor = System.Drawing.Color.White;
            lblHeader.Dock      = System.Windows.Forms.DockStyle.Fill;
            lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblHeader.AutoSize  = false;

            pnlHeader.Controls.Add(lblHeader);

            // ── Footer ────────────────────────────────────────────────
            // Height = top padding (12) + button height (60) + bottom padding (12) = 84
            pnlFooter           = new System.Windows.Forms.Panel();
            pnlFooter.Dock      = System.Windows.Forms.DockStyle.Bottom;
            pnlFooter.Height    = 84;
            pnlFooter.BackColor = System.Drawing.Color.White;
            pnlFooter.Padding   = new System.Windows.Forms.Padding(20, 0, 20, 0);

            btnConfirm              = new System.Windows.Forms.Button();
            btnConfirm.Text         = "\u2714  Confirm Generate";
            btnConfirm.Font         = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            btnConfirm.Size         = new System.Drawing.Size(210, 60);
            btnConfirm.Location     = new System.Drawing.Point(pnlFooter.Width - 450, 12);
            btnConfirm.Anchor       = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnConfirm.BackColor    = System.Drawing.Color.FromArgb(6, 95, 70);
            btnConfirm.ForeColor    = System.Drawing.Color.White;
            btnConfirm.FlatStyle    = System.Windows.Forms.FlatStyle.Flat;
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Cursor       = System.Windows.Forms.Cursors.Hand;
            btnConfirm.Click       += btnConfirm_Click;

            btnCancel              = new System.Windows.Forms.Button();
            btnCancel.Text         = "\u2715  Cancel";
            btnCancel.Font         = new System.Drawing.Font("Segoe UI", 10f);
            btnCancel.Size         = new System.Drawing.Size(210, 60);
            btnCancel.Location     = new System.Drawing.Point(pnlFooter.Width - 230, 12);
            btnCancel.Anchor       = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.BackColor    = System.Drawing.Color.FromArgb(243, 244, 246);
            btnCancel.ForeColor    = System.Drawing.Color.FromArgb(55, 65, 81);
            btnCancel.FlatStyle    = System.Windows.Forms.FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(209, 213, 219);
            btnCancel.Cursor       = System.Windows.Forms.Cursors.Hand;
            btnCancel.Click       += btnCancel_Click;

            pnlFooter.Controls.AddRange(new System.Windows.Forms.Control[] { btnConfirm, btnCancel });

            // ── Outer container (grey bg, scrollable) ─────────────────
            pnlOuter            = new System.Windows.Forms.Panel();
            pnlOuter.Dock       = System.Windows.Forms.DockStyle.Fill;
            pnlOuter.BackColor  = System.Drawing.Color.FromArgb(243, 244, 246);
            pnlOuter.Padding    = new System.Windows.Forms.Padding(20, 16, 20, 16);
            pnlOuter.AutoScroll = true;

            // ── Already Exists Warning ────────────────────────────────
            pnlAlreadyExists            = new System.Windows.Forms.Panel();
            pnlAlreadyExists.Dock       = System.Windows.Forms.DockStyle.Top;
            pnlAlreadyExists.Height     = 44;
            pnlAlreadyExists.BackColor  = System.Drawing.Color.FromArgb(254, 243, 199);
            pnlAlreadyExists.Padding    = new System.Windows.Forms.Padding(14, 0, 14, 0);
            pnlAlreadyExists.Margin     = new System.Windows.Forms.Padding(0, 0, 0, 12);
            pnlAlreadyExists.Visible    = false;

            lblAlreadyExistsIcon            = new System.Windows.Forms.Label();
            lblAlreadyExistsIcon.Text       = "\u26A0";
            lblAlreadyExistsIcon.Font       = new System.Drawing.Font("Segoe UI", 13f);
            lblAlreadyExistsIcon.ForeColor  = System.Drawing.Color.FromArgb(146, 64, 14);
            lblAlreadyExistsIcon.AutoSize   = false;
            lblAlreadyExistsIcon.Size       = new System.Drawing.Size(30, 44);
            lblAlreadyExistsIcon.Location   = new System.Drawing.Point(0, 0);
            lblAlreadyExistsIcon.TextAlign  = System.Drawing.ContentAlignment.MiddleCenter;

            lblExistingDN               = new System.Windows.Forms.Label();
            lblExistingDN.Text          = "";
            lblExistingDN.Font          = new System.Drawing.Font("Segoe UI", 9.5f);
            lblExistingDN.ForeColor     = System.Drawing.Color.FromArgb(146, 64, 14);
            lblExistingDN.AutoSize      = false;
            lblExistingDN.Dock          = System.Windows.Forms.DockStyle.Fill;
            lblExistingDN.TextAlign     = System.Drawing.ContentAlignment.MiddleLeft;

            var tlpWarn = new System.Windows.Forms.TableLayoutPanel();
            tlpWarn.Dock            = System.Windows.Forms.DockStyle.Fill;
            tlpWarn.ColumnCount     = 2;
            tlpWarn.RowCount        = 1;
            tlpWarn.BackColor       = System.Drawing.Color.Transparent;
            tlpWarn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30f));
            tlpWarn.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
            tlpWarn.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
            tlpWarn.Controls.Add(lblAlreadyExistsIcon, 0, 0);
            tlpWarn.Controls.Add(lblExistingDN, 1, 0);

            pnlAlreadyExists.Controls.Add(tlpWarn);

            // ── Shipment Info Card ─────────────────────────────────────
            pnlShipCard             = new System.Windows.Forms.Panel();
            pnlShipCard.Dock        = System.Windows.Forms.DockStyle.Top;
            pnlShipCard.Height      = 130;
            pnlShipCard.BackColor   = System.Drawing.Color.White;
            pnlShipCard.Padding     = new System.Windows.Forms.Padding(16, 10, 16, 10);
            pnlShipCard.Margin      = new System.Windows.Forms.Padding(0, 0, 0, 12);

            lblShipCardTitle            = new System.Windows.Forms.Label();
            lblShipCardTitle.Text       = "Shipment Information";
            lblShipCardTitle.Font       = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
            lblShipCardTitle.ForeColor  = System.Drawing.Color.FromArgb(17, 24, 39);
            lblShipCardTitle.Dock       = System.Windows.Forms.DockStyle.Top;
            lblShipCardTitle.Height     = 28;
            lblShipCardTitle.AutoSize   = false;

            tlpShipInfo                 = new System.Windows.Forms.TableLayoutPanel();
            tlpShipInfo.Dock            = System.Windows.Forms.DockStyle.Fill;
            tlpShipInfo.ColumnCount     = 8;
            tlpShipInfo.RowCount        = 2;
            tlpShipInfo.BackColor       = System.Drawing.Color.Transparent;
            tlpShipInfo.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.None;
            for (int i = 0; i < 4; i++)
            {
                tlpShipInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120f));
                tlpShipInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25f));
            }
            tlpShipInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24f));
            tlpShipInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));

            // Captions row
            lblCapShipmentID = MakeCaption("Shipment ID");
            lblCapOrderID    = MakeCaption("Order ID");
            lblCapShipDate   = MakeCaption("Ship Date");
            lblCapShipStatus = MakeCaption("Status");
            tlpShipInfo.Controls.Add(lblCapShipmentID, 0, 0);
            tlpShipInfo.Controls.Add(lblCapOrderID,    2, 0);
            tlpShipInfo.Controls.Add(lblCapShipDate,   4, 0);
            tlpShipInfo.Controls.Add(lblCapShipStatus, 6, 0);

            // Values row
            lblShipmentID   = MakeValue("");
            lblOrderID      = MakeValue("");
            lblShipDate     = MakeValue("");
            lblShipStatus   = MakeValue("");
            tlpShipInfo.Controls.Add(lblShipmentID, 1, 1);
            tlpShipInfo.Controls.Add(lblOrderID,    3, 1);
            tlpShipInfo.Controls.Add(lblShipDate,   5, 1);
            tlpShipInfo.Controls.Add(lblShipStatus, 7, 1);

            pnlShipCard.Controls.AddRange(new System.Windows.Forms.Control[] { tlpShipInfo, lblShipCardTitle });

            // ── Delivery Note Preview Card ─────────────────────────────
            pnlDNCard               = new System.Windows.Forms.Panel();
            pnlDNCard.Dock          = System.Windows.Forms.DockStyle.Top;
            pnlDNCard.Height        = 160;
            pnlDNCard.BackColor     = System.Drawing.Color.White;
            pnlDNCard.Padding       = new System.Windows.Forms.Padding(16, 10, 16, 10);
            pnlDNCard.Margin        = new System.Windows.Forms.Padding(0, 0, 0, 12);

            lblDNCardTitle              = new System.Windows.Forms.Label();
            lblDNCardTitle.Text         = "Delivery Note Preview";
            lblDNCardTitle.Font         = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
            lblDNCardTitle.ForeColor    = System.Drawing.Color.FromArgb(17, 24, 39);
            lblDNCardTitle.Dock         = System.Windows.Forms.DockStyle.Top;
            lblDNCardTitle.Height       = 28;
            lblDNCardTitle.AutoSize     = false;

            tlpDNInfo                   = new System.Windows.Forms.TableLayoutPanel();
            tlpDNInfo.Dock              = System.Windows.Forms.DockStyle.Fill;
            tlpDNInfo.ColumnCount       = 8;
            tlpDNInfo.RowCount          = 3;
            tlpDNInfo.BackColor         = System.Drawing.Color.Transparent;
            tlpDNInfo.CellBorderStyle   = System.Windows.Forms.TableLayoutPanelCellBorderStyle.None;
            for (int i = 0; i < 4; i++)
            {
                tlpDNInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 140f));
                tlpDNInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25f));
            }
            tlpDNInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24f));
            tlpDNInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 32f));
            tlpDNInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));

            // Row 0: captions
            lblCapDeliveryDate      = MakeCaption("Delivery Date");
            lblCapShipToName        = MakeCaption("Ship To Name");
            lblCapOutstandingQty    = MakeCaption("Outstanding Qty");
            lblCapShippingAddress   = MakeCaption("Shipping Address");
            tlpDNInfo.Controls.Add(lblCapDeliveryDate,    0, 0);
            tlpDNInfo.Controls.Add(lblCapShipToName,      2, 0);
            tlpDNInfo.Controls.Add(lblCapOutstandingQty,  4, 0);
            tlpDNInfo.Controls.Add(lblCapShippingAddress, 0, 2);

            // Row 1: values (single line)
            lblDeliveryDate     = MakeValue("");
            lblShipToName       = MakeValue("");
            lblOutstandingQty   = MakeValue("");
            tlpDNInfo.Controls.Add(lblDeliveryDate,   1, 1);
            tlpDNInfo.Controls.Add(lblShipToName,     3, 1);
            tlpDNInfo.Controls.Add(lblOutstandingQty, 5, 1);

            // Row 2: address spans full width
            lblShippingAddress                  = new System.Windows.Forms.Label();
            lblShippingAddress.Text             = "";
            lblShippingAddress.Font             = new System.Drawing.Font("Segoe UI", 9.5f);
            lblShippingAddress.ForeColor        = System.Drawing.Color.FromArgb(17, 24, 39);
            lblShippingAddress.AutoSize         = false;
            lblShippingAddress.Dock             = System.Windows.Forms.DockStyle.Fill;
            lblShippingAddress.TextAlign        = System.Drawing.ContentAlignment.TopLeft;

            tlpDNInfo.SetColumnSpan(lblShippingAddress, 7);
            tlpDNInfo.Controls.Add(lblShippingAddress, 1, 2);

            pnlDNCard.Controls.AddRange(new System.Windows.Forms.Control[] { tlpDNInfo, lblDNCardTitle });

            // ── Items Grid Card ────────────────────────────────────────
            pnlGridCard             = new System.Windows.Forms.Panel();
            pnlGridCard.BackColor   = System.Drawing.Color.White;
            pnlGridCard.Dock        = System.Windows.Forms.DockStyle.Fill;
            pnlGridCard.Padding     = new System.Windows.Forms.Padding(16, 10, 16, 10);

            lblGridCardTitle            = new System.Windows.Forms.Label();
            lblGridCardTitle.Text       = "Shipment Lines";
            lblGridCardTitle.Font       = new System.Drawing.Font("Segoe UI", 11f, System.Drawing.FontStyle.Bold);
            lblGridCardTitle.ForeColor  = System.Drawing.Color.FromArgb(17, 24, 39);
            lblGridCardTitle.Dock       = System.Windows.Forms.DockStyle.Top;
            lblGridCardTitle.Height     = 28;
            lblGridCardTitle.AutoSize   = false;

            dgvLines = BuildGrid();
            dgvLines.Dock = System.Windows.Forms.DockStyle.Fill;

            pnlGridCard.Controls.AddRange(new System.Windows.Forms.Control[] { dgvLines, lblGridCardTitle });

            // ── Compose pnlOuter (bottom-up: Fill goes in last) ────────
            // Order added determines dock stack: last added = topmost.
            // We want visual order: Warning, ShipCard, DNCard, GridCard
            pnlOuter.Controls.Add(pnlGridCard);       // Fill
            pnlOuter.Controls.Add(pnlDNCard);          // Top (above Fill)
            pnlOuter.Controls.Add(pnlShipCard);        // Top
            pnlOuter.Controls.Add(pnlAlreadyExists);   // Top (topmost)

            // ── Form layout ───────────────────────────────────────────
            this.Controls.Add(pnlOuter);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);
        }

        // ── Factory helpers ───────────────────────────────────────────
        private static System.Windows.Forms.Label MakeCaption(string text)
        {
            return new System.Windows.Forms.Label
            {
                Text      = text,
                Font      = new System.Drawing.Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(107, 114, 128),
                AutoSize  = false,
                Dock      = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.BottomLeft
            };
        }

        private static System.Windows.Forms.Label MakeValue(string text)
        {
            return new System.Windows.Forms.Label
            {
                Text      = text,
                Font      = new System.Drawing.Font("Segoe UI", 10f),
                ForeColor = System.Drawing.Color.FromArgb(17, 24, 39),
                AutoSize  = false,
                Dock      = System.Windows.Forms.DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
        }

        private static System.Windows.Forms.DataGridView BuildGrid()
        {
            var dgv = new System.Windows.Forms.DataGridView();
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.ReadOnly                    = true;
            dgv.AllowUserToAddRows          = false;
            dgv.AllowUserToDeleteRows       = false;
            dgv.RowHeadersVisible           = false;
            dgv.SelectionMode               = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect                 = false;
            dgv.BackgroundColor             = System.Drawing.Color.White;
            dgv.GridColor                   = System.Drawing.Color.FromArgb(229, 231, 235);
            dgv.BorderStyle                 = System.Windows.Forms.BorderStyle.None;
            dgv.AutoSizeColumnsMode         = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgv.Font                        = new System.Drawing.Font("Segoe UI", 9.5f);

            // Header style
            dgv.ColumnHeadersDefaultCellStyle.BackColor  = System.Drawing.Color.FromArgb(249, 250, 251);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor  = System.Drawing.Color.FromArgb(107, 114, 128);
            dgv.ColumnHeadersDefaultCellStyle.Font       = new System.Drawing.Font("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            dgv.EnableHeadersVisualStyles                = false;

            // Row style
            dgv.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(219, 234, 254);
            dgv.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(29, 78, 216);
            dgv.RowTemplate.Height                  = 32;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(249, 250, 251);

            // Columns
            var cols = new[]
            {
                ("ShipmentLineID", "Line ID",        60f),
                ("ItemID",         "Item ID",         80f),
                ("ItemName",       "Item Name",      200f),
                ("QtyShipped",     "Qty Shipped",    100f),
                ("QtyOutstanding", "Qty Outstanding",120f),
            };
            foreach (var (name, header, w) in cols)
            {
                var col = new System.Windows.Forms.DataGridViewTextBoxColumn
                {
                    Name           = name,
                    HeaderText     = header,
                    FillWeight     = w,
                    SortMode       = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable
                };
                dgv.Columns.Add(col);
            }

            return dgv;
        }
    }
}
