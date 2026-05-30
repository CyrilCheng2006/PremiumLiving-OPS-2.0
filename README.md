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
PremiumLiving-OPS-2.0/
├── PremiumLivingOPS.sln                                    ← Done
│
├── Database/
│   ├── schema.sql                                          ← Done   ← Full database schema (DDL)
│   └── sample_data.sql                                     ← Done   ← Seed data with 13 business scenarios
│
├── PremiumLivingOPS/                                                ← Visual Studio Project Root
│   ├── PremiumLivingOPS.csproj                             ← Done
│   ├── Program.cs                                          ← Done
│   │
│   ├── Models/
│   │   ├── Entities/                                                ← Entity & ViewModel Classes
│   │   │   ├── Staff.cs                                    ← Done
│   │   │   ├── OrderProcessingViewModel.cs                 ← Done   ← Entities + ViewModels for Order Processing
│   │   │   │                                                           (OrderEntity, OrderLineEntity,
│   │   │   │                                                            QuotationEntity, CustomerEntity,
│   │   │   │                                                            ProductLookup, ViewOrderViewModel,
│   │   │   │                                                            QuotationViewModel,
│   │   │   │                                                            CreateOrderViewModel,
│   │   │   │                                                            ModifyOrderViewModel)
│   │   │   ├── Customer.cs
│   │   │   ├── Product.cs
│   │   │   ├── RawMaterial.cs
│   │   │   ├── Order.cs
│   │   │   ├── OrderItem.cs
│   │   │   ├── Quotation.cs
│   │   │   ├── Invoice.cs
│   │   │   ├── Shipment.cs
│   │   │   ├── DeliveryNote.cs
│   │   │   ├── ReplySlip.cs
│   │   │   ├── Complaint.cs
│   │   │   ├── ReturnOrder.cs
│   │   │   ├── Supplier.cs
│   │   │   ├── SupplierReceipt.cs
│   │   │   ├── PurchaseInvoice.cs
│   │   │   ├── WarehouseTransfer.cs
│   │   │   └── AuditLog.cs
│   │   │
│   │   └── DAL/                                                     ← Repository Classes (MySQL)
│   │       ├── DatabaseHelper.cs                           ← Done   ← MySQL connection manager
│   │       ├── OrderProcessingRepo.cs                      ← Done   ← All SQL for Order Processing module
│   │       │                                                           (GetAllOrders, GetOrdersByStatus,
│   │       │                                                            GetOrderById, GetOrderLines,
│   │       │                                                            GetAllQuotations, GetPendingQuotations,
│   │       │                                                            GetAllCustomers, GetAllProducts,
│   │       │                                                            CreateOrder, CreateOrderLine,
│   │       │                                                            UpdateOrder, UpdateOrderStatus,
│   │       │                                                            ReplaceOrderLines,
│   │       │                                                            UpdateQuotationStatus)
│   │       ├── StaffRepo.cs                                ← Done
│   │       ├── CustomerRepo.cs
│   │       ├── ProductRepo.cs
│   │       ├── RawMaterialRepo.cs
│   │       ├── OrderRepo.cs
│   │       ├── QuotationRepo.cs
│   │       ├── InvoiceRepo.cs
│   │       ├── ShipmentRepo.cs
│   │       ├── ComplaintRepo.cs
│   │       ├── ReturnOrderRepo.cs
│   │       ├── SupplierRepo.cs
│   │       └── AuditLogRepo.cs
│   │
│   ├── Controllers/                                                  ← Business Logic (no UI dependencies)
│   │   ├── SessionManager.cs                               ← Done   ← Current user session state
│   │   ├── NavAccessPolicy.cs                              ← Done   ← Role-based menu access rules
│   │   ├── DashboardController.cs                          ← Done
│   │   └── OrderProcessingController.cs                    ← Done   ← All business logic for Order Processing
│   │                                                                   (GetViewOrderVM, GetOrderLines,
│   │                                                                    GetQuotationVM, UpdateQuotationStatus,
│   │                                                                    GetCreateOrderVM, SubmitCreateOrder,
│   │                                                                    GetModifyOrderVM, SubmitModifyOrder,
│   │                                                                    CancelOrder)
│   │
│   └── Views/                                                        ← Windows Forms (UI only)
│       │
│       ├── Shared/                                         ← Done   ← Reusable chrome — used by ALL pages
│       │   ├── AppShell.cs                                 ← Done   ← Hosts TopNavBar + UserBar (116 px)
│       │   │                                                           TableLayoutPanel layout (no overlap)
│       │   │                                                           Exposes: SetUser / SetVisibleMenus /
│       │   │                                                           SetBreadcrumb / SetPopupContainer
│       │   │                                                           MenuItemClicked / LogoutClicked events
│       │   ├── TopNavBar.cs                                ← Done   ← Apple-style top nav (44 px)
│       │   │                                                           Mega-menu dropdown per role
│       │   ├── UserInfoLabel.cs                            ← Done   ← User name + department display
│       │   ├── Palette.cs                                  ← Done   ← Centralised colour constants
│       │   │                                                           (BgPage, BgCard, Primary, Danger,
│       │   │                                                            Success, TextMain, TextMuted,
│       │   │                                                            BorderColor)
│       │   └── FormNavigator.cs                            ← Done   ← TopNavBar routing — maps
│       │                                                               (menuLabel, subItem) → target Form
│       │
│       ├── Auth/
│       │   └── LoginForm.cs                                ← Done
│       │
│       ├── Dashboard/
│       │   ├── DashboardForm.cs                            ← Done   ← Consumes AppShell
│       │   └── DashboardForm.Designer.cs                   ← Done
│       │
│       ├── OrderProcessing/                                ← Done   ← All 4 tabs complete
│       │   ├── ViewOrderForm.cs                            ← Done   ← Tab 1: View & filter orders
│       │   ├── ViewOrderForm.Designer.cs                   ← Done     DataGridView + status filter
│       │   │                                                           + drill-down order lines panel
│       │   ├── QuotationForm.cs                            ← Done   ← Tab 2: List & update quotations
│       │   ├── QuotationForm.Designer.cs                   ← Done     Status filter + Change Status
│       │   ├── CreateOrderForm.cs                          ← Done   ← Tab 3: Create new order
│       │   ├── CreateOrderForm.Designer.cs                 ← Done     Order Header card + Order Lines card
│       │   │                                                           Real-time Grand Total calculation
│       │   ├── ModifyOrderForm.cs                          ← Done   ← Tab 4: Edit Order + Cancel Order
│       │   └── ModifyOrderForm.Designer.cs                 ← Done     Progressive disclosure (load first)
│       │                                                               Business rules: Delivered/Completed
│       │                                                               orders cannot be cancelled
│       │
│       ├── Logistics/
│       │   ├── ShipmentListForm.cs
│       │   ├── ScheduleShipmentForm.cs
│       │   ├── DeliveryNoteForm.cs
│       │   ├── SupplierReceiptForm.cs
│       │   └── PurchaseInvoiceForm.cs
│       │
│       ├── Inventory/
│       │   ├── InventoryListForm.cs
│       │   ├── InwardGoodsForm.cs
│       │   └── WarehouseTransferForm.cs
│       │
│       ├── AfterService/
│       │   ├── CreateInvoiceForm.cs
│       │   ├── ComplaintListForm.cs
│       │   ├── ReturnOrderListForm.cs
│       │   ├── AccountReceivableForm.cs
│       │   └── AccountPayableForm.cs
│       │
│       ├── MasterData/
│       │   ├── SupplierListForm.cs
│       │   └── CustomerListForm.cs
│       │
│       └── SystemSecurity/
│           ├── StaffListForm.cs
│           └── AuditLogForm.cs
│
└── README.md
```

### Views/Shared — Chrome Architecture

All pages include `AppShell` as their top chrome. `AppShell` encapsulates `TopNavBar` and `UserBar` so they never need to be re-implemented per form.

```
AppShell (Panel, Dock.Top, 116 px)
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
// 1. Declare
private AppShell _shell;

// 2. Initialise (in constructor or InitializeComponent)
_shell = new AppShell();
_shell.MenuItemClicked += OnMenuItemClicked;
_shell.LogoutClicked   += OnLogoutClicked;
_shell.SetPopupContainer(pnlMain);
Controls.Add(_shell);

// 3. Bind data (after loading ViewModel)
_shell.SetUser(vm.DisplayName, vm.Department);
_shell.SetVisibleMenus(vm.AllowedMenus);
_shell.SetBreadcrumb("Order Processing");
```

### Order Processing — MVC Data Flow

```
 View (*.cs / *.Designer.cs)
   │  calls
   ▼
 Controller (OrderProcessingController.cs)
   │  reads session      │  calls
   ▼                     ▼
 SessionManager    OrderProcessingRepo.cs
 NavAccessPolicy       │  executes SQL via
                       ▼
                  DatabaseHelper  ──►  MySQL Database
                                        (schema.sql tables:
                                         Order, OrderLine,
                                         Quotation, Customer,
                                         Product, Item, Staff)
```

---

## 📱 Application Modules & Pages

```
Dashboard
├── 1. Order Processing Management            ← Done
│   ├── View & Search Order                   ← Done   (ViewOrderForm)
│   ├── Quotation                             ← Done   (QuotationForm)
│   ├── Create Order                          ← Done   (CreateOrderForm)
│   └── Modify Order                          ← Done   (ModifyOrderForm)
│       ├── Cancel Order                      ← Done
│       └── Edit Order                        ← Done
│
├── 2. Production Processing Management       [Prototype 2]
│   ├── Search Raw Material Request
│   │   └── Modify Request (Edit / Delete)
│   └── Create Raw Material Request
│
├── 3. Logistics Processing Management
│   ├── View & Search Shipment
│   │   ├── Schedule Shipment
│   │   ├── Modify Shipment (Edit / Delete)
│   │   └── Generate Delivery Notes & Reply Slip
│   └── Handling Goods Received
│       ├── Upload Supplier Receipt
│       └── Record Purchase Invoice
│
├── 4. Inventory Control Management
│   └── View & Search Product / Raw Material
│       ├── Add New Item
│       ├── Modify Item (Edit / Delete)
│       ├── Record Inward Goods
│       └── Record Warehouse Item Transfer
│
├── 5. Raw Material Management                [Prototype 2]
│   ├── Create Procurement
│   └── Search & List Procurement
│
├── 6. After-service Management
│   ├── Create Invoice
│   ├── Complaint List (Create / Edit / Delete)
│   ├── Return Order List (Create / Edit)
│   ├── Account Receivable
│   └── Account Payable
│
├── 7. Master Data Maintenance
│   ├── Supplier List (Add / Edit)
│   └── Customer List (Add / Edit)
│
├── 8. System Security & Control
│   ├── Staff List (Add / Edit / Delete)
│   └── Log List
│
└── 9. Statistical Reports                    [Prototype 2]
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

> ⚠️ **Note:** Passwords in the seed data are plain text for development purposes only. Production builds must hash passwords (e.g., BCrypt).

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
- [x] **Phase 1** — Shared Chrome: `AppShell`, `TopNavBar`, `UserBar`, `Palette`, `FormNavigator`
- [x] **Phase 2** — Dashboard
- [x] **Phase 3** — Order Processing Management (all 4 tabs: View Order, Quotation, Create Order, Modify Order)
- [ ] **Phase 4** — Inventory & Procurement Module
- [ ] **Phase 5** — Logistics Module (Shipment, Delivery, Return)
- [ ] **Phase 6** — After-service Module (Invoice, Complaint, Return Order, Finance)
- [ ] **Phase 7** — Master Data & System Security

### Prototype 2
- [ ] **Phase 8** — Production Processing Management
- [ ] **Phase 9** — Raw Material Management (Procurement)
- [ ] **Phase 10** — Statistical Reports

---

*Last updated: 2026-05-30*
