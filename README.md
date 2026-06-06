# 🪑 Premium Living Furniture Co. Ltd. — OPS System 2.0

> **Operations Management System** for Premium Living Furniture Co. Ltd.  
> Built with **C# (.NET) · Windows Forms · MySQL · MVC Architecture**

---

## 📌 Project Overview

The **PremiumLiving-OPS 2.0** is a Windows desktop application designed to digitalise and streamline the core business operations of Premium Living Furniture Co. Ltd. — a multi-warehouse furniture company with operations across **Hong Kong, Shenzhen, London, Tokyo, and Los Angeles**.

The system replaces manual paper-based workflows with a centralised, role-based platform covering Sales, Inventory, Production, Logistics, Finance, and IT Administration.

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Language | C# (.NET) |
| UI Framework | Windows Forms |
| Architecture | MVC (Model-View-Controller) |
| Database | MySQL |
| IDE | Visual Studio 2026 |
| Version Control | Git / GitHub |

---

## 🏗️ Project Structure

```
PremiumLiving-OPS-2.0/                          ← Repository Root
│
├── .gitignore                                  ✅
├── PremiumLivingOPS.sln                        ✅  Solution file
├── README.md                                   ✅
│
├── Database/                                   ✅  SQL scripts (no C# code)
│   ├── schema.sql                              ✅  DDL — 27 tables
│   ├── sample_data.sql                         ✅  13 business scenarios seed data
│   └── README.md                               ✅  DB setup instructions & password policy
│
└── PremiumLivingOPS/                           ✅  Visual Studio Project Root
    ├── PremiumLivingOPS.csproj                 ✅
    ├── Program.cs                              ✅  Entry point — shows LoginForm
    │
    ├── Models/
    │   │
    │   ├── Entities/                               Entity & ViewModel classes
    │   │   ├── Staff.cs                        ✅  StaffEntity + LoginViewModel
    │   │   ├── DashboardViewModel.cs           ✅  DashboardVM, UserBarVM, KpiItem,
    │   │   │                                       RecentOrderRow, ChartPoint
    │   │   ├── OrderProcessingViewModel.cs     ✅  OrderEntity, OrderLineEntity,
    │   │   │                                       QuotationEntity, CustomerEntity,
    │   │   │                                       ProductLookup, OrderDetailViewModel,
    │   │   │                                       ViewOrderViewModel,
    │   │   │                                       QuotationViewModel,
    │   │   │                                       CreateOrderViewModel,
    │   │   │                                       ModifyOrderViewModel
    │   │   ├── InventoryControlViewModel.cs    ✅  ProductEntity, RawMaterialEntity,
    │   │   │                                       ViewProductViewModel,
    │   │   │                                       ViewRawMaterialViewModel
    │   │   ├── ShipmentEntity.cs               ✅  ShipmentEntity, ShipmentLineEntity,
    │   │   │                                       DeliveryNoteEntity, ReplySlipEntity
    │   │   ├── GoodsReceivedEntity.cs          ✅  GoodsReceivedEntity,
    │   │   │                                       PurchaseOrderEntity
    │   │   ├── Customer.cs                     🔲
    │   │   ├── Product.cs                      🔲
    │   │   ├── RawMaterial.cs                  🔲
    │   │   ├── Invoice.cs                      🔲
    │   │   ├── Complaint.cs                    🔲
    │   │   ├── ReturnOrder.cs                  🔲
    │   │   ├── Supplier.cs                     🔲
    │   │   ├── PurchaseInvoice.cs              🔲
    │   │   ├── WarehouseTransfer.cs            🔲
    │   │   └── AuditLog.cs                     🔲
    │   │
    │   ├── DAL/                                    Repository classes (MySQL)
    │   │   ├── DatabaseHelper.cs               ✅  MySQL connection manager
    │   │   ├── DashboardRepo.cs                ✅  KPI queries, recent orders, charts
    │   │   ├── OrderProcessingRepo.cs          ✅  GetAllOrders, GetOrdersByStatus,
    │   │   │                                       GetOrderById, GetOrderLines,
    │   │   │                                       GetAllQuotations, GetPendingQuotations,
    │   │   │                                       GetAllCustomers, GetAllProducts,
    │   │   │                                       CreateOrder, CreateOrderLine,
    │   │   │                                       UpdateOrder, UpdateOrderStatus,
    │   │   │                                       ReplaceOrderLines, UpdateQuotationStatus
    │   │   ├── InventoryControlRepo.cs         ✅  GetAllProducts, GetProductCategories,
    │   │   │                                       GetAllRawMaterials,
    │   │   │                                       GetRawMaterialCategories
    │   │   ├── LogisticsProcessingRepo.cs      ✅  SearchShipments, GetShipmentById,
    │   │   │                                       GetShipmentLines,
    │   │   │                                       GetDeliveryNoteByShipment,
    │   │   │                                       GetReplySlipByDelivery,
    │   │   │                                       SearchReceipts,
    │   │   │                                       GetAllPurchaseOrders
    │   │   ├── StaffRepo.cs                    ✅  Login auth, staff lookup
    │   │   ├── CustomerRepo.cs                 🔲
    │   │   ├── ProductRepo.cs                  🔲
    │   │   ├── RawMaterialRepo.cs              🔲
    │   │   ├── QuotationRepo.cs                🔲
    │   │   ├── InvoiceRepo.cs                  🔲
    │   │   ├── ShipmentRepo.cs                 🔲
    │   │   ├── ComplaintRepo.cs                🔲
    │   │   ├── ReturnOrderRepo.cs              🔲
    │   │   ├── SupplierRepo.cs                 🔲
    │   │   └── AuditLogRepo.cs                 🔲
    │   │
    │   └── Helpers/                                Utility / helper classes
    │       └── PasswordHelper.cs               ✅  PBKDF2-HMACSHA256 hash + verify
    │
    ├── Controllers/                                Business logic (no UI dependencies)
    │   ├── SessionManager.cs                   ✅  Current user session state
    │   ├── NavAccessPolicy.cs                  ✅  Role-based menu access rules
    │   │                                           (see Department Navigation Access Matrix)
    │   ├── DashboardController.cs              ✅  GetDashboardVM
    │   ├── OrderProcessingController.cs        ✅  GetViewOrderVM, GetOrderLines,
    │   │                                           GetQuotationVM, UpdateQuotationStatus,
    │   │                                           GetCreateOrderVM, SubmitCreateOrder,
    │   │                                           GetModifyOrderVM, SubmitModifyOrder,
    │   │                                           CancelOrder, GetOrderDetail
    │   ├── InventoryControlController.cs       ✅  GetViewProductVM,
    │   │                                           GetProductCategories,
    │   │                                           GetViewRawMaterialVM,
    │   │                                           GetRawMaterialCategories
    │   └── LogisticsProcessingController.cs    ✅  GetViewShipmentVM, GetShipmentDetail,
    │                                               GetHandlingGoodsReceivedVM
    │
    └── Views/                                      Windows Forms (UI only, no business logic)
        │
        ├── Shared/                             ✅  Reusable chrome — used by ALL forms
        │   ├── AppShell.cs                     ✅  Hosts TopNavBar (44 px) + UserBar (72 px)
        │   │                                       Total height: 116 px, DockStyle.Top
        │   │                                       Public API:
        │   │                                         SetUser(name, dept)
        │   │                                         SetVisibleMenus(allowedMenus)
        │   │                                         SetBreadcrumb(text)
        │   │                                         SetPopupContainer(panel)
        │   │                                       Events:
        │   │                                         MenuItemClicked(menuLabel, subItem)
        │   │                                         LogoutClicked
        │   ├── TopNavBar.cs                    ✅  Apple-style horizontal nav bar (44 px)
        │   │                                       Mega-menu dropdown per role
        │   ├── UserInfoLabel.cs                ✅  User name + department display chip
        │   ├── CardPanel.cs                    ✅  Shared card factory:
        │   │                                         CardPanel.Create(outerHeight, outerPadding)
        │   │                                         CardPanel.CreateFill(outerPadding)
        │   │                                       Returns (outerPanel, innerPanel) tuple
        │   ├── Palette.cs                      ✅  Centralised colour constants
        │   │                                       (BgPage, BgCard, Primary, Danger,
        │   │                                        Success, TextMain, TextMuted, BorderColor)
        │   └── FormNavigator.cs                ✅  Routes (menuLabel, subItem) → target Form
        │
        ├── Auth/
        │   ├── LoginForm.cs                    ✅  Login logic + validation
        │   └── LoginForm.Designer.cs           ✅
        │
        ├── Dashboard/
        │   ├── DashboardForm.cs                ✅  KPI tiles, charts, recent orders
        │   └── DashboardForm.Designer.cs       ✅
        │
        ├── OrderProcessing/                    ✅  All 4 pages complete
        │   ├── ViewOrderForm.cs                ✅  Search orders (Search button triggered),
        │   ├── ViewOrderForm.Designer.cs       ✅    Reset clears all filters,
        │   │                                         DataGridView + status badge + drill-down dialog
        │   ├── QuotationForm.cs                ✅  List & approve/reject quotations
        │   ├── QuotationForm.Designer.cs       ✅
        │   ├── CreateOrderForm.cs              ✅  New order — header + line items,
        │   ├── CreateOrderForm.Designer.cs     ✅    real-time grand total calculation
        │   ├── ModifyOrderForm.cs              ✅  Edit order + Cancel Order,
        │   └── ModifyOrderForm.Designer.cs     ✅    business rule: Delivered/Completed
        │                                             orders cannot be cancelled
        │
        ├── ProductionProcessing/               🔲  Prototype 2
        │   ├── MaterialRequestListForm.cs      🔲
        │   └── CreateMaterialRequestForm.cs    🔲
        │
        ├── LogisticsProcessing/                ✅  2 pages complete
        │   ├── ViewShipmentForm.cs             ✅  Search & filter shipments,
        │   ├── ViewShipmentForm.Designer.cs    ✅    Status filter / keyword / date range,
        │   │                                         DataGridView + Shipment Detail dialog
        │   │                                         (Lines, Delivery Note, Reply Slip)
        │   ├── HandlingGoodsReceivedForm.cs    ✅  Search & filter goods receipts,
        │   └── HandlingGoodsReceivedForm.Designer.cs ✅
        │                                             KPI bar (Total PO / Pending / Completed),
        │                                             DataGridView + Receipt Detail dialog
        │
        ├── InventoryControl/                   ✅  2 pages complete
        │   ├── ViewProductForm.cs              ✅  Search & filter products,
        │   ├── ViewProductForm.Designer.cs     ✅    KPI bar (Total / In Stock /
        │   │                                         Low Stock / Out of Stock),
        │   │                                         DataGridView + Product Detail dialog
        │   │                                         View Detail button in KPI bar (right-aligned)
        │   ├── ViewRawMaterialForm.cs          ✅  Search & filter raw materials,
        │   └── ViewRawMaterialForm.Designer.cs ✅    KPI bar + DataGridView +
        │                                             Raw Material Detail dialog
        │                                             View Detail button in KPI bar (right-aligned)
        │
        ├── RawMaterial/                        🔲  Prototype 2
        │   ├── ProcurementListForm.cs          🔲
        │   └── CreateProcurementForm.cs        🔲
        │
        ├── AfterService/                       🔲
        │   ├── CreateInvoiceForm.cs            🔲
        │   ├── ComplaintListForm.cs            🔲
        │   ├── ReturnOrderListForm.cs          🔲
        │   ├── AccountReceivableForm.cs        🔲
        │   └── AccountPayableForm.cs           🔲
        │
        ├── MasterData/                         🔲
        │   ├── SupplierListForm.cs             🔲
        │   └── CustomerListForm.cs             🔲
        │
        ├── SystemControl/                      🔲
        │   ├── StaffListForm.cs                🔲
        │   └── AuditLogForm.cs                 🔲
        │
        └── StatisticalReports/                 🔲  Prototype 2
            └── ReportForm.cs                   🔲
```

---

### Views/Shared — Chrome Architecture

All forms embed `AppShell` as the top chrome. `AppShell` wraps `TopNavBar` + `UserBar` into a single reusable component so navigation never needs to be re-implemented per form.

```
AppShell (UserControl, DockStyle.Top, 116 px)
│
├── TopNavBar  (Panel, Dock.Top, 44 px)
│   └── Apple-style horizontal nav · mega-menu popup · role-based visibility
│
└── UserBar    (Panel, Dock.Top, 72 px)
    └── TableLayoutPanel  [3 columns]
        ├── Col 0 AutoSize  →  Breadcrumb Label  (left, Margin.Left = 22 px)
        ├── Col 1 100%      →  Spacer
        └── Col 2 AutoSize  →  FlowLayoutPanel
                                  ├── UserInfoLabel  (name + dept)
                                  └── Log Out Button
```

**How to use AppShell in any new Form:**
```csharp
// 1. Declare (in Designer.cs)
private AppShell _shell;

// 2. Initialise
_shell = new AppShell();
_shell.SetPopupContainer(pnlMain);   // required for mega-menu popup
pnlMain.Controls.Add(pnlContent);   // content panel first (Fill)
pnlMain.Controls.Add(_shell);       // AppShell last (Top) — stacks above content

// 3. Wire events (in Form_Load)
_shell.MenuItemClicked += (menu, sub) => FormNavigator.NavigateTo(this, menu, sub);
_shell.LogoutClicked   += (s, e) => { SessionManager.Clear(); Application.Restart(); };

// 4. Bind data (after ViewModel is loaded)
_shell.SetUser(vm.UserBar.DisplayName, vm.UserBar.Department);
_shell.SetVisibleMenus(vm.AllowedMenus);
_shell.SetBreadcrumb("Module  ›  Page Title");
```

### Views/Shared — CardPanel Factory

`CardPanel` is a shared helper used by all list/view forms to produce consistent white rounded cards.

```csharp
// Fixed-height card (e.g. Search bar, KPI bar)
var (outerPanel, innerPanel) = CardPanel.Create(
    outerHeight: 260,
    outerPadding: new Padding(20, 12, 20, 0));

// Fill-height card (e.g. data table)
var (outerPanel, innerPanel) = CardPanel.CreateFill(
    outerPadding: new Padding(20, 12, 20, 20));
```

Both methods return a `(Panel outer, Panel inner)` tuple. Add content controls to `innerPanel`; add `outerPanel` to the scroll container.

---

### Order Processing — MVC Data Flow

```
View  (*.cs / *.Designer.cs)
  │  user action (button click)
  ▼
Controller  (OrderProcessingController.cs)
  │  reads session           │  calls repo
  ▼                          ▼
SessionManager         OrderProcessingRepo.cs
NavAccessPolicy              │  executes SQL via
                             ▼
                        DatabaseHelper  ──►  MySQL Database
                                              (schema.sql tables:
                                               Order, OrderLine,
                                               Quotation, Customer,
                                               Product, Item, Staff)
  │  returns ViewModel
  ▼
View  renders data into controls
```

---

## 🔐 Department Navigation Access Matrix

This matrix defines which Top Navigation Bar items are visible to each department.
It is enforced at runtime by `NavAccessPolicy.GetAllowedMenus(department)` in
`PremiumLivingOPS/Controllers/NavAccessPolicy.cs` — **no database query is required**.

> **Legend:** `Y` = menu item is visible to this department &nbsp;|&nbsp; _(blank)_ = hidden

| Menu Item | IT | Production | Sales | Inventory | Finance | Logistics |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Dashboard | Y | Y | Y | Y | Y | Y |
| Order Processing | Y | | Y | | | |
| Production Processing | Y | Y | | | | |
| Logistics Processing | Y | | | | | Y |
| Inventory Control | Y | Y | | Y | | |
| Raw Material | Y | Y | | Y | | |
| After-Service | Y | | Y | | Y | |
| Master Data Maintenance | Y | | Y | Y | Y | Y |
| System Security & Control | Y | | | | | |
| Statistical Reports | Y | | Y | | Y | |

### Design Notes

- **IT** is the super-user department and has access to all menu items.
- **System Security & Control** is restricted to IT only.
- **Dashboard** is accessible to every department and cannot be hidden.
- To update access rules, edit **only** `NavAccessPolicy.cs`; no other file needs to change.
- The `TopNavBar` control is a pure View — it renders whatever list it receives and has
  no knowledge of departments or access rules.

---

## 📱 Application Modules & Pages

```
Dashboard
├── 1. Order Processing Management            ✅ Done
│   ├── View & Search Order                   ✅  (ViewOrderForm)
│   ├── Quotation                             ✅  (QuotationForm)
│   ├── Create Order                          ✅  (CreateOrderForm)
│   └── Modify Order                          ✅  (ModifyOrderForm)
│       ├── Cancel Order                      ✅
│       └── Edit Order                        ✅
│
├── 2. Production Processing Management       🔲 Prototype 2
│   ├── Search Raw Material Request
│   │   └── Modify Request (Edit / Delete)
│   └── Create Raw Material Request
│
├── 3. Logistics Processing Management        ✅ Partial (View only)
│   ├── View & Search Shipment                ✅  (ViewShipmentForm)
│   │   └── Shipment Detail (popup dialog)    ✅    Lines / Delivery Note / Reply Slip
│   └── Handling Goods Received               ✅  (HandlingGoodsReceivedForm)
│       ├── KPI bar                           ✅    Total PO / Pending / Completed
│       └── Receipt Detail (popup dialog)     ✅
│
├── 4. Inventory Control Management           ✅ Partial (View only)
│   ├── View & Search Product                 ✅  (ViewProductForm)
│   │   └── Product Detail (popup dialog)     ✅
│   ├── View & Search Raw Material            ✅  (ViewRawMaterialForm)
│   │   └── Raw Material Detail (popup dialog)✅
│   ├── Add New Item                          🔲
│   ├── Modify Item (Edit / Delete)           🔲
│   ├── Record Inward Goods                   🔲
│   └── Record Warehouse Item Transfer        🔲
│
├── 5. Raw Material Management                🔲 Prototype 2
│   ├── Create Procurement
│   └── Search & List Procurement
│
├── 6. After-service Management               🔲
│   ├── Create Invoice
│   ├── Complaint List (Create / Edit / Delete)
│   ├── Return Order List (Create / Edit)
│   ├── Account Receivable
│   └── Account Payable
│
├── 7. Master Data Maintenance                🔲
│   ├── Supplier List (Add / Edit)
│   └── Customer List (Add / Edit)
│
├── 8. System Security & Control              🔲  (Views/SystemControl/)
│   ├── Staff List (Add / Edit / Delete)
│   └── Log List
│
└── 9. Statistical Reports                    🔲 Prototype 2
```

---

## 🗄️ Database

### Schema Tables (27 Tables)

| Category | Tables |
|---|---|
| **Master Data** | `Staff`, `Customer`, `Address`, `Supplier`, `Item`, `Product`, `RawMaterial` |
| **Warehouse** | `Warehouse`, `WarehouseItem`, `TransferForm`, `TransferForm_WarehouseItem` |
| **Sales** | `Quotation`, `Order`, `OrderLine`, `Invoice` |
| **Logistics** | `Shipment`, `ShipmentLine`, `DeliveryNote`, `ReplySlip`, `ReturnOrder`, `ReturnOrderItem` |
| **Procurement** | `MaterialRequest`, `PurchaseOrder`, `PurchaseOrderLine`, `PurchaseInvoice`, `Receipt` |
| **Finance** | `Transaction` |
| **System** | `Complaint`, `Log` |

### Setup Instructions

```sql
-- Step 1: Create the database and schema
SOURCE Database/schema.sql;

-- Step 2: Load sample data
SOURCE Database/sample_data.sql;
```

---

## 📦 Sample Data Scenarios

The `sample_data.sql` file includes **13 realistic business scenarios** for testing:

| # | Scenario | Key Tables Involved |
|---|---|---|
| 1 | Low inventory → auto reorder (no order) | `MaterialRequest`, `PurchaseOrder`, `Receipt` |
| 2 | Warehouse stock transfer between branches | `TransferForm`, `TransferForm_WarehouseItem` |
| 3 | Chan Siu Ming — Quotation + Deposit + Full payment | `Quotation`, `Order`, `Invoice`, `Transaction` |
| 4 | Lee Wai Kwan — Deposit + Complaint | `Order`, `Complaint` |
| 5 | ABC Furniture Ltd — Partial delivery + Complaints | `Shipment`, `ShipmentLine`, `Complaint` |
| 6 | Wong Cheuk Hei — No quotation, full payment | `Order`, `Invoice`, `Shipment` |
| 7 | Sunrise Interiors — Rate discount + Deposit | `Quotation`, `Order` (DiscountType=Rate) |
| 8 | Tanaka Home Design — In transit, partial payment | `Shipment` (In Transit), `Invoice` (Partial) |
| 9 | Nordic Nest AB — Deposit paid, pending | `Order` (Pending), `Invoice` |
| 10 | Sunrise Interiors — Return order (no refund) | `ReturnOrder`, `ReturnOrderItem` |
| 11 | Chan Siu Ming — Return order + Refund | `ReturnOrder`, `Transaction` (Refund) |
| 12 | Multiple mixed single orders + complaints | `Order`, `Complaint`, `Shipment` |
| 13 | Beaumont Living SAS — Reorder triggered by order | `MaterialRequest` (OrderDemand), `PurchaseOrder` |

---

## 👥 Staff Accounts (for Testing)

| StaffID | Name | Role | Department | Password |
|---|---|---|---|---|
| S-001 | IT Admin | Administrator | IT | `admin123` |
| S-002 | Chan Ho Yuen | Manager | Production | `prod456` |
| S-003 | Lam Siu Keung | Staff | Logistics | `log789` |
| S-004 | Wong Kin Ho | Clerk | Sales | `sales321` |
| S-005 | Chan Wai Man | Manager | Inventory (HK) | `wh001` |
| S-006 | Ng Pak Hei | Manager | Finance | `fin888` |
| S-007 | Yeung Chi Wai | Deliverer | Logistics | `drv999` |
| S-008 | James Mitchell | Manager | Inventory (London) | `lon001` |
| S-009 | Yuki Tanaka | Manager | Inventory (Tokyo) | `tok001` |
| S-010 | Maria Gonzalez | Manager | Inventory (LA) | `la001` |

> ⚠️ **Note:** Passwords in the seed data are plain text for development purposes only. Production builds must hash passwords via `PasswordHelper.cs`.

---

## 🎯 Module & Use Case Mapping

| Department | Module | Key Use Cases |
|---|---|---|
| Sales | Order, Quotation, Invoice, Complaint | UC-001, UC-002, UC-003, UC-004, UC-016 |
| Inventory | Stock Management, Reorder, Reconcile | UC-005, UC-006, UC-007, UC-021 |
| Production | Material Request | UC-018 |
| Logistics | Delivery, Shipment Tracking, Return, Reply Slip | UC-008, UC-009, UC-010, UC-017 |
| Finance | Transaction Records | UC-014, UC-015 |
| IT / Admin | Staff Accounts, Audit Log | UC-011, UC-012, UC-013, UC-019 |

---

## 🚀 Development Roadmap

### Prototype 1
- [x] **Phase 0** — Database Schema + MySQL Connection (`DatabaseHelper.cs`) + Login (UC-019)
- [x] **Phase 1** — Shared Chrome: `AppShell`, `TopNavBar`, `UserBar`, `Palette`, `FormNavigator`, `CardPanel`
- [x] **Phase 2** — Dashboard
- [x] **Phase 3** — Order Processing Management (all 4 tabs: View Order, Quotation, Create Order, Modify Order)
- [x] **Phase 4** — Inventory Control — View Product & View Raw Material (read-only + detail dialog)
- [x] **Phase 5** — Logistics Processing — View Shipment & Handling Goods Received (read-only + detail dialog)
- [ ] **Phase 6** — Inventory Control — Add / Edit / Delete Item, Inward Goods, Warehouse Transfer
- [ ] **Phase 7** — After-service Module (Invoice, Complaint, Return Order, Finance)
- [ ] **Phase 8** — Master Data & System Security

### Prototype 2
- [ ] **Phase 9** — Production Processing Management
- [ ] **Phase 10** — Raw Material Management (Procurement)
- [ ] **Phase 11** — Statistical Reports

---

*Last updated: 2026-06-06*
