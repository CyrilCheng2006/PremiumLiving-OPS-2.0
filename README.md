# 🪑 Premium Living Furniture Co. Ltd. — OPS System 2.0

> **Order Processing & Stock Recording System**  
> A centralized desktop management system for Premium Living Furniture Co. Ltd.

---

## 📌 Project Overview

Premium Living Furniture Co. Ltd. is transitioning from a fragmented, email-based operation to a **centralized computerized system** that integrates all departments into a single platform.

This system resolves core operational issues including:
- Non-trackable material requests causing production delays
- Fragmented customer complaint handling with no central tracking
- Manual order modification via email causing delays
- Decentralized inventory and delivery visibility

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| **Language** | C# (.NET) |
| **UI Framework** | Windows Forms |
| **Database** | MySQL |
| **IDE** | Visual Studio 2026 |
| **Architecture** | MVC (Model-View-Controller) |
| **Version Control** | GitHub |

---

## 🏗️ MVC Architecture

```
PremiumLiving-OPS-2.0/
├── Models/
│   ├── Entities/          # Entity Classes (Step 1) — map to DB tables
│   └── DAL/               # Repository Classes (Step 2) — CRUD operations
├── Views/                 # Windows Forms (Step 3) — UI per module
│   ├── Auth/
│   ├── Dashboard/
│   ├── OrderProcessing/
│   ├── Logistics/
│   ├── Inventory/
│   ├── AfterService/
│   ├── MasterData/
│   └── SystemSecurity/
├── Database/
│   ├── schema.sql         # Database schema (CREATE TABLE)
│   └── simple_data_updated-2.sql  # Sample data (INSERT)
└── README.md
```

---

## 🗄️ Database Schema

**Database:** `PremiumLivingFurniture`

### Core Tables

| Table | Description |
|---|---|
| `Staff` | System users with roles & departments |
| `Customer` | Customer records |
| `Address` | Customer delivery/billing addresses |
| `Supplier` | Supplier information |
| `Item` | Base item registry (products & raw materials) |
| `Product` | Finished goods (extends Item) |
| `RawMaterial` | Raw materials (extends Item) |
| `Warehouse` | Warehouse locations |
| `WarehouseItem` | Stock levels per item per warehouse |

### Order & Sales

| Table | Description |
|---|---|
| `Quotation` | Sales quotations |
| `Order` | Customer orders |
| `OrderLine` | Order line items |
| `Invoice` | Sales invoices |
| `Transaction` | Payment transactions |

### Logistics

| Table | Description |
|---|---|
| `Shipment` | Shipment records |
| `ShipmentLine` | Shipment line items |
| `DeliveryNote` | Delivery notes |
| `ReplySlip` | Customer receipt confirmation |
| `ReturnOrder` | Return/refund requests |
| `ReturnOrderItem` | Return order line items |

### Inventory & Procurement

| Table | Description |
|---|---|
| `MaterialRequest` | Raw material requests from Production |
| `PurchaseOrder` | Purchase orders to suppliers |
| `PurchaseOrderLine` | PO line items |
| `PurchaseInvoice` | Supplier invoices |
| `Receipt` | Goods received records |
| `TransferForm` | Warehouse transfer forms |
| `TransferForm_WarehouseItem` | Transfer form line items |

### System

| Table | Description |
|---|---|
| `Complaint` | Customer complaints |
| `Log` | System audit logs |

---

## 👥 User Roles & Departments

| Role | Department | Access Level |
|---|---|---|
| `Administrator` | IT | Full system access, user management |
| `Manager` | All | Module management + approval |
| `Clerk` | Sales / Inventory | CRUD on assigned modules |
| `Staff` | Logistics / Production | Operational tasks |
| `Deliverer` | Logistics | Shipment & delivery tasks |

---

## 📦 Module Structure (Prototype 1)

| # | Module | Key Functions |
|---|---|---|
| 1 | Order Processing | View/Search Orders, Quotation, Create Order |
| 3 | Logistics Processing | Shipment management, Delivery Notes, Goods Received |
| 4 | Inventory Control | Product & Raw Material management, Inward Goods, Transfer |
| 6 | After-Service | Invoice, Complaints, Return Orders, AR/AP |
| 7 | Master Data | Supplier & Customer management |
| 8 | System Security | Staff management, Audit Logs |

> **Prototype 2** will include: Order Modification, Production Management, Raw Material Procurement, Statistical Reports.

---

## ⚙️ Database Setup

1. Install MySQL and run the scripts in order:
```sql
-- Step 1: Create schema
source Database/schema.sql

-- Step 2: Insert sample data
source Database/simple_data_updated-2.sql
```

2. Update connection string in `Models/DAL/DatabaseHelper.cs`:
```csharp
string host = "127.0.0.1";
string db = "PremiumLivingFurniture";
string userId = "root";
string password = "your_password";
```

---

## 🚀 Getting Started

1. Clone the repository
```bash
git clone https://github.com/CyrilCheng2006/PremiumLiving-OPS-2.0.git
```
2. Open `PremiumLivingOPS.sln` in Visual Studio 2026
3. Restore NuGet packages (`MySql.Data`)
4. Set up database (see above)
5. Build and run

---

## 📄 Reference Documents

- `Reference.docx` — System Analysis and Design Report
- `MVC_implementation.pdf` — MVC Architecture Guidelines

---

*Developed as part of ITP4915M — System Development Project*  
*Hong Kong Institute of Information Technology*
