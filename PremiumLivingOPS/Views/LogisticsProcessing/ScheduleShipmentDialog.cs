using PremiumLivingOPS.Controllers;
using PremiumLivingOPS.Models.Entities;
using PremiumLivingOPS.Views.Shared;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PremiumLivingOPS.Views.LogisticsProcessing
{
    // ScheduleShipmentDialog
    // ---------------------------------------------------------------
    // Step 1 : Choose an Order (Processing / Partially Delivered / Pending)
    // Step 2 : For each OrderLine item decide:
    //            - Batch letter (A/B/C/D) -> each batch = one Shipment
    //            - Scheduled ship date for that batch
    //            - Qty to ship in this batch
    //            - Leave unchecked to defer scheduling
    // On Confirm -> controller creates one Shipment + ShipmentLines per batch
    // ShipmentID pattern: SHP-YYYYMMDD-<orderNumSuffix><batchLetter>
    //   e.g. SHP-20260309-0029A
    // ---------------------------------------------------------------
    public class ScheduleShipmentDialog : Form
    {
        private readonly LogisticsProcessingController _ctrl;

        // Step-1 controls
        private Panel          _pnlStep1;
        private ComboBox       _cboOrder;
        private Label          _lblOrderInfo;
        private Button         _btnNext;

        // Step-2 controls
        private Panel          _pnlStep2;
        private Label          _lblStep2OrderInfo;
        private DataGridView   _grid;
        private Button         _btnBack;
        private Button         _btnConfirm;

        // Grid column indices
        private const int COL_CHECK    = 0;
        private const int COL_ITEMID   = 1;
        private const int COL_ITEMNAME = 2;
        private const int COL_ORDERQTY = 3;
        private const int COL_SHIPPED  = 4;
        private const int COL_REMAIN   = 5;
        private const int COL_BATCH    = 6;
        private const int COL_DATE     = 7;
        private const int COL_QTYSHIP  = 8;
        private const int COL_METHOD   = 9;

        // Data
        private List<OrderSummary>    _orders     = new List<OrderSummary>();
        private List<OrderLineDetail> _lines      = new List<OrderLineDetail>();
        private OrderSummary          _selOrder;

        public ScheduleShipmentDialog(LogisticsProcessingController ctrl)
        {
            _ctrl = ctrl ?? throw new ArgumentNullException(nameof(ctrl));
            BuildUI();
            LoadOrders();
        }

        // ================================================================
        //  UI Construction
        // ================================================================
        private void BuildUI()
        {
            Text            = "Schedule Shipment";
            Size            = new Size(1800, 800);
            MinimumSize     = new Size(1400, 700);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.FromArgb(240, 244, 249);
            Font            = new Font("Segoe UI", 12f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;

            // -- Header --------------------------------------------------
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 80,
                BackColor = Color.FromArgb(19, 35, 61)
            };
            var tblH = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 1,
                BackColor       = Color.Transparent,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(28, 0, 28, 0)
            };
            tblH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tblH.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblH.Controls.Add(new Label
            {
                Text      = "Schedule Shipment",
                Font      = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.White,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false
            }, 0, 0);
            pnlHeader.Controls.Add(tblH);

            // -- Footer --------------------------------------------------
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 80,
                BackColor = Color.White,
                Padding   = new Padding(0, 12, 28, 12)
            };
            pnlFooter.Paint += PaintTopBorder;

            _btnConfirm = MakeBtn("\u2714  Confirm Schedule", Color.FromArgb(109, 40, 217),
                Color.FromArgb(91, 25, 180), Color.FromArgb(69, 17, 140), Color.White);
            _btnConfirm.Dock    = DockStyle.Right;
            _btnConfirm.Visible = false;
            _btnConfirm.Click  += BtnConfirm_Click;

            _btnNext = MakeBtn("Next  \u25b6", Color.FromArgb(29, 78, 216),
                Color.FromArgb(21, 60, 170), Color.FromArgb(14, 42, 130), Color.White);
            _btnNext.Dock   = DockStyle.Right;
            _btnNext.Click += BtnNext_Click;

            _btnBack = MakeBtn("\u25c0  Back", Color.White,
                Color.FromArgb(240, 244, 249), Color.FromArgb(220, 228, 240),
                Color.FromArgb(15, 31, 53));
            _btnBack.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            _btnBack.FlatAppearance.BorderSize  = 1;
            _btnBack.Dock    = DockStyle.Right;
            _btnBack.Visible = false;
            _btnBack.Click  += BtnBack_Click;

            var btnCancel = MakeBtn("Cancel", Color.White,
                Color.FromArgb(240, 244, 249), Color.FromArgb(220, 228, 240),
                Color.FromArgb(15, 31, 53));
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(221, 227, 236);
            btnCancel.FlatAppearance.BorderSize  = 1;
            btnCancel.Dock   = DockStyle.Right;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // Add right-to-left: Confirm, Next, Back, Cancel
            pnlFooter.Controls.Add(_btnConfirm);
            pnlFooter.Controls.Add(_btnNext);
            pnlFooter.Controls.Add(_btnBack);
            pnlFooter.Controls.Add(btnCancel);

            // -- Step 1 panel (card) -------------------------------------
            BuildStep1Panel();

            // -- Step 2 panel (card) -------------------------------------
            BuildStep2Panel();

            _pnlStep2.Visible = false;

            // Assemble
            Controls.Add(_pnlStep2);
            Controls.Add(_pnlStep1);
            Controls.Add(pnlFooter);
            Controls.Add(pnlHeader);
        }

        // ----------------------------------------------------------------
        //  Step 1: Order Selection Card
        // ----------------------------------------------------------------
        private void BuildStep1Panel()
        {
            var (outer, inner) = CardPanel.Create(
                outerHeight:  560,
                outerPadding: new Padding(20, 14, 20, 8));

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 5,
                BackColor   = Color.Transparent,
                Padding     = new Padding(32, 28, 32, 20)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));   // section label
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));   // field label
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));   // combobox
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));   // spacer
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));  // info area

            tbl.Controls.Add(new Label
            {
                Text      = "STEP 1 — SELECT ORDER",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(29, 78, 216),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            tbl.Controls.Add(new Label
            {
                Text      = "Order ID",
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(98, 112, 135),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            }, 0, 1);

            _cboOrder = new ComboBox
            {
                Dock          = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font          = new Font("Segoe UI", 13f),
                Height        = 48
            };
            _cboOrder.SelectedIndexChanged += CboOrder_Changed;
            tbl.Controls.Add(_cboOrder, 0, 2);

            tbl.Controls.Add(new Label { Dock = DockStyle.Fill }, 0, 3); // spacer

            _lblOrderInfo = new Label
            {
                Dock      = DockStyle.Fill,
                Font      = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(15, 31, 53),
                TextAlign = ContentAlignment.TopLeft,
                AutoSize  = false
            };
            tbl.Controls.Add(_lblOrderInfo, 0, 4);

            inner.Controls.Add(tbl);
            _pnlStep1 = outer;
        }

        // ----------------------------------------------------------------
        //  Step 2: Item Schedule Grid Card
        // ----------------------------------------------------------------
        private void BuildStep2Panel()
        {
            var (outer, inner) = CardPanel.Create(
                outerHeight:  560,
                outerPadding: new Padding(20, 14, 20, 8));

            var tbl = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 3,
                BackColor   = Color.Transparent,
                Padding     = new Padding(20, 16, 20, 12)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));   // section label
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));   // sub info
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));  // grid

            tbl.Controls.Add(new Label
            {
                Text      = "STEP 2 — SCHEDULE ITEMS",
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(109, 40, 217),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);

            _lblStep2OrderInfo = new Label
            {
                Dock      = DockStyle.Fill,
                Font      = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(98, 112, 135),
                TextAlign = ContentAlignment.MiddleLeft
            };
            tbl.Controls.Add(_lblStep2OrderInfo, 0, 1);

            // Hint label
            var hint = new Label
            {
                Text      = "Tip: Assign each item to a Batch (A/B/C/D) and pick a date. Leave unchecked to defer. Items in the same batch share one Shipment.",
                Font      = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(130, 140, 160),
                Dock      = DockStyle.Top,
                AutoSize  = false,
                Height    = 24
            };

            var gridWrapper = new Panel { Dock = DockStyle.Fill };
            gridWrapper.Controls.Add(BuildGrid());
            gridWrapper.Controls.Add(hint);

            tbl.Controls.Add(gridWrapper, 0, 2);

            inner.Controls.Add(tbl);
            _pnlStep2 = outer;
        }

        private DataGridView BuildGrid()
        {
            _grid = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                BackgroundColor       = Color.White,
                BorderStyle           = BorderStyle.None,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor             = Color.FromArgb(230, 234, 240),
                RowHeadersVisible     = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                Font                  = new Font("Segoe UI", 11f),
                ColumnHeadersHeight   = 40,
                RowTemplate           = { Height = 46 },
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.None
            };
            _grid.ColumnHeadersDefaultCellStyle.Font            = new Font("Segoe UI", 10f, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.BackColor       = Color.FromArgb(245, 247, 252);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor       = Color.FromArgb(60, 75, 100);
            _grid.ColumnHeadersDefaultCellStyle.Alignment       = DataGridViewContentAlignment.MiddleCenter;
            _grid.EnableHeadersVisualStyles                     = false;
            _grid.DefaultCellStyle.SelectionBackColor           = Color.FromArgb(219, 234, 254);
            _grid.DefaultCellStyle.SelectionForeColor           = Color.FromArgb(15, 31, 53);

            // COL 0: Checkbox
            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name       = "colCheck",
                HeaderText = "Schedule",
                Width      = 80,
                Resizable  = DataGridViewTriState.False
            });
            // COL 1: Item ID (read-only)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "colItemID",
                HeaderText = "Item ID",
                Width      = 140,
                ReadOnly   = true
            });
            // COL 2: Item Name (read-only)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "colItemName",
                HeaderText = "Item Name",
                Width      = 230,
                ReadOnly   = true
            });
            // COL 3: Order Qty (read-only)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name            = "colOrderQty",
                HeaderText      = "Order Qty",
                Width           = 100,
                ReadOnly        = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            // COL 4: Already Shipped (read-only)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name            = "colShipped",
                HeaderText      = "Shipped",
                Width           = 100,
                ReadOnly        = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            // COL 5: Remaining (read-only)
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name            = "colRemain",
                HeaderText      = "Remaining",
                Width           = 100,
                ReadOnly        = true,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter,
                                     ForeColor  = Color.FromArgb(146, 64, 14),
                                     Font       = new Font("Segoe UI", 11f, FontStyle.Bold) }
            });
            // COL 6: Batch (A/B/C/D)
            var cboBatch = new DataGridViewComboBoxColumn
            {
                Name            = "colBatch",
                HeaderText      = "Batch",
                Width           = 90,
                DataSource      = new string[] { "A", "B", "C", "D" },
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            };
            _grid.Columns.Add(cboBatch);
            // COL 7: Schedule Date
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name            = "colDate",
                HeaderText      = "Ship Date (YYYY-MM-DD)",
                Width           = 200,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            // COL 8: Qty to Ship
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name            = "colQtyShip",
                HeaderText      = "Qty to Ship",
                Width           = 110,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            // COL 9: Delivery Method
            var cboMethod = new DataGridViewComboBoxColumn
            {
                Name       = "colMethod",
                HeaderText = "Delivery Method",
                Width      = 160,
                DataSource = new string[] { "Courier", "SelfPickup" }
            };
            _grid.Columns.Add(cboMethod);

            _grid.CellValueChanged     += Grid_CellValueChanged;
            _grid.CurrentCellDirtyStateChanged += Grid_DirtyState;

            return _grid;
        }

        // ================================================================
        //  Data Loading
        // ================================================================
        private void LoadOrders()
        {
            try
            {
                _orders = _ctrl.GetSchedulableOrders();
                _cboOrder.Items.Clear();
                _cboOrder.Items.Add("-- Select Order --");
                foreach (var o in _orders)
                    _cboOrder.Items.Add(o.OrderID + "  |  " + o.CustomerName + "  |  " + o.OrderStatus);
                _cboOrder.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load orders:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadOrderLines(string orderId)
        {
            try
            {
                _lines = _ctrl.GetOrderLinesWithShipmentStatus(orderId);
                _grid.Rows.Clear();
                foreach (var ln in _lines)
                {
                    int remaining = ln.Quantity - ln.QtyAlreadyShipped;
                    int rowIdx = _grid.Rows.Add(
                        false,               // Schedule checkbox
                        ln.ItemID,
                        ln.ItemName,
                        ln.Quantity,
                        ln.QtyAlreadyShipped,
                        remaining,
                        "A",                 // default batch
                        DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                        remaining > 0 ? remaining.ToString() : "0",
                        "Courier"
                    );
                    // Grey out rows with no remaining qty
                    if (remaining <= 0)
                    {
                        _grid.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
                        _grid.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 180);
                        _grid.Rows[rowIdx].Cells[COL_CHECK].ReadOnly  = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load order lines:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================================================================
        //  Event Handlers
        // ================================================================
        private void CboOrder_Changed(object sender, EventArgs e)
        {
            if (_cboOrder.SelectedIndex <= 0)
            {
                _lblOrderInfo.Text = string.Empty;
                _btnNext.Enabled   = false;
                _selOrder          = null;
                return;
            }
            _selOrder = _orders[_cboOrder.SelectedIndex - 1];
            _lblOrderInfo.Text =
                "Customer       : " + _selOrder.CustomerName + "\n" +
                "Order Status   : " + _selOrder.OrderStatus  + "\n" +
                "Shipping Addr  : " + _selOrder.ShippingAddress + "\n" +
                "Contact        : " + _selOrder.ContactName   + "\n" +
                "Required Date  : " + _selOrder.DeliveryDate.ToString("yyyy-MM-dd") + "\n" +
                "Grand Total    : HKD " + _selOrder.GrandTotal.ToString("N2");
            _btnNext.Enabled = true;
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (_selOrder == null) return;
            _lblStep2OrderInfo.Text =
                "Order: " + _selOrder.OrderID +
                "   Customer: " + _selOrder.CustomerName +
                "   Ship To: " + _selOrder.ShippingAddress;
            LoadOrderLines(_selOrder.OrderID);
            _pnlStep1.Visible    = false;
            _pnlStep2.Visible    = true;
            _btnNext.Visible     = false;
            _btnBack.Visible     = true;
            _btnConfirm.Visible  = true;
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            _pnlStep2.Visible    = false;
            _pnlStep1.Visible    = true;
            _btnBack.Visible     = false;
            _btnConfirm.Visible  = false;
            _btnNext.Visible     = true;
        }

        private void Grid_DirtyState(object sender, EventArgs e)
        {
            if (_grid.IsCurrentCellDirty)
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            // When checkbox ticked, enable editing columns in that row
            if (e.ColumnIndex == COL_CHECK)
            {
                bool chk = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[COL_CHECK].Value);
                _grid.Rows[e.RowIndex].Cells[COL_BATCH].ReadOnly   = !chk;
                _grid.Rows[e.RowIndex].Cells[COL_DATE].ReadOnly    = !chk;
                _grid.Rows[e.RowIndex].Cells[COL_QTYSHIP].ReadOnly = !chk;
                _grid.Rows[e.RowIndex].Cells[COL_METHOD].ReadOnly  = !chk;
            }
        }

        // ================================================================
        //  Confirm Schedule
        // ================================================================
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            // 1. Collect checked rows
            var scheduledRows = new List<ScheduleRow>();
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                bool chk = Convert.ToBoolean(row.Cells[COL_CHECK].Value);
                if (!chk) continue;

                string itemId   = row.Cells[COL_ITEMID].Value?.ToString()  ?? string.Empty;
                string batch    = row.Cells[COL_BATCH].Value?.ToString()   ?? "A";
                string dateStr  = row.Cells[COL_DATE].Value?.ToString()    ?? string.Empty;
                string qtyStr   = row.Cells[COL_QTYSHIP].Value?.ToString() ?? "0";
                string method   = row.Cells[COL_METHOD].Value?.ToString()  ?? "Courier";
                int    remain   = Convert.ToInt32(row.Cells[COL_REMAIN].Value ?? 0);

                if (!DateTime.TryParse(dateStr, out DateTime shipDate))
                {
                    MessageBox.Show("Row " + (i + 1) + ": Invalid date '" + dateStr + "'. Use YYYY-MM-DD.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (shipDate.Date < DateTime.Today)
                {
                    MessageBox.Show("Row " + (i + 1) + ": Ship date cannot be in the past.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!int.TryParse(qtyStr, out int qty) || qty <= 0)
                {
                    MessageBox.Show("Row " + (i + 1) + ": Qty to Ship must be a positive integer.",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (qty > remain)
                {
                    MessageBox.Show("Row " + (i + 1) + ": Qty to Ship (" + qty + ") exceeds remaining qty (" + remain + ").",
                        "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                scheduledRows.Add(new ScheduleRow
                {
                    ItemID   = itemId,
                    Batch    = batch,
                    ShipDate = shipDate,
                    QtyShip  = qty,
                    Remain   = remain,
                    Method   = method
                });
            }

            if (scheduledRows.Count == 0)
            {
                MessageBox.Show("No items selected. Tick at least one item to schedule.",
                    "Nothing to Schedule", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 2. Group by Batch letter -> each group = one Shipment
            var batches = scheduledRows
                .GroupBy(r => r.Batch)
                .OrderBy(g => g.Key)
                .ToList();

            // 3. Derive order number suffix for ShipmentID
            //    e.g. ORD-20260225-0029 -> suffix = "0029"
            string ordSuffix = _selOrder.OrderID.Length >= 4
                ? _selOrder.OrderID.Substring(_selOrder.OrderID.Length - 4)
                : _selOrder.OrderID;

            // 4. Check for duplicate batch letters using existing shipments
            List<string> existingSuffixes = _ctrl.GetExistingShipmentSuffixes(_selOrder.OrderID);

            var conflicts = batches
                .Where(g => existingSuffixes.Contains(ordSuffix + g.Key))
                .Select(g => g.Key)
                .ToList();
            if (conflicts.Count > 0)
            {
                MessageBox.Show(
                    "Batch letter(s) " + string.Join(", ", conflicts) +
                    " already used by existing shipments for this order.\nPlease choose different batch letters.",
                    "Duplicate Batch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 5. Determine ShipmentType per batch
            //    Full  = all remaining qty across ALL order lines are covered by this + previous batches
            //    Partial = some items still unscheduled
            int totalRemaining   = _lines.Sum(l => l.Quantity - l.QtyAlreadyShipped);
            int totalScheduled   = scheduledRows.Sum(r => r.QtyShip);
            bool allCovered      = totalScheduled >= totalRemaining;

            // 6. Build confirmation summary
            string summary = "The following shipment batches will be created:\n\n";
            foreach (var g in batches)
            {
                string shpId = "SHP-" + g.First().ShipDate.ToString("yyyyMMdd") + "-" + ordSuffix + g.Key;
                summary += "  Batch " + g.Key + " -> " + shpId +
                           "  (" + g.Sum(r => r.QtyShip) + " unit" + (g.Sum(r => r.QtyShip) > 1 ? "s" : "") +
                           ", " + g.First().ShipDate.ToString("yyyy-MM-dd") +
                           ", " + g.First().Method + ")\n";
            }
            if (!allCovered)
                summary += "\nNote: Some items are left unscheduled (deferred).";
            summary += "\n\nProceed?";

            if (MessageBox.Show(summary, "Confirm Schedule",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // 7. Call controller
            try
            {
                foreach (var g in batches)
                {
                    var lines = g.Select(r => new ShipmentLineRequest
                    {
                        ItemID  = r.ItemID,
                        QtyShip = r.QtyShip,
                        Remain  = r.Remain - r.QtyShip
                    }).ToList();

                    _ctrl.CreateScheduledShipment(new CreateShipmentRequest
                    {
                        OrderID        = _selOrder.OrderID,
                        Batch          = g.Key,
                        OrderSuffix    = ordSuffix,
                        ShipDate       = g.First().ShipDate,
                        DeliveryMethod = g.First().Method,
                        ShipmentType   = allCovered && g.Key == batches.Last().Key ? "Full" : "Partial",
                        Lines          = lines
                    });
                }

                MessageBox.Show(
                    batches.Count + " shipment batch" + (batches.Count > 1 ? "es" : "") +
                    " scheduled successfully.",
                    "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to create shipments:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================================================================
        //  Helpers
        // ================================================================
        private static Button MakeBtn(string text, Color bg, Color hover, Color down, Color fg)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Width     = 210,
                Height    = 56,
                Cursor    = Cursors.Hand
            };
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor  = hover;
            b.FlatAppearance.MouseDownBackColor  = down;
            return b;
        }

        private static void PaintTopBorder(object s, PaintEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(221, 227, 236), 1))
                e.Graphics.DrawLine(pen, 0, 0, ((Panel)s).Width, 0);
        }

        // ================================================================
        //  Inner DTOs
        // ================================================================
        private class ScheduleRow
        {
            public string   ItemID   { get; set; }
            public string   Batch    { get; set; }
            public DateTime ShipDate { get; set; }
            public int      QtyShip  { get; set; }
            public int      Remain   { get; set; }
            public string   Method   { get; set; }
        }
    }

    // ====================================================================
    //  Request / summary DTOs  (used between View and Controller)
    // ====================================================================

    public class OrderSummary
    {
        public string   OrderID         { get; set; }
        public string   CustomerName    { get; set; }
        public string   OrderStatus     { get; set; }
        public string   ShippingAddress { get; set; }
        public string   ContactName     { get; set; }
        public DateTime DeliveryDate    { get; set; }
        public double   GrandTotal      { get; set; }
    }

    public class OrderLineDetail
    {
        public string ItemID           { get; set; }
        public string ItemName         { get; set; }
        public int    Quantity         { get; set; }
        public int    QtyAlreadyShipped { get; set; }
    }

    public class CreateShipmentRequest
    {
        public string                    OrderID        { get; set; }
        public string                    Batch          { get; set; }
        public string                    OrderSuffix    { get; set; }
        public DateTime                  ShipDate       { get; set; }
        public string                    DeliveryMethod { get; set; }
        public string                    ShipmentType   { get; set; }
        public List<ShipmentLineRequest> Lines          { get; set; }
    }

    public class ShipmentLineRequest
    {
        public string ItemID  { get; set; }
        public int    QtyShip { get; set; }
        public int    Remain  { get; set; }
    }
}
