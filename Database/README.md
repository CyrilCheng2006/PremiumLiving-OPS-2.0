# Database Setup

## Requirements
- MySQL 8.0+

## Setup Instructions

1. Run `schema.sql` first to create the database and all tables
2. Run `simple_data_updated-2.sql` to populate sample data

```bash
mysql -u root -p < schema.sql
mysql -u root -p PremiumLivingFurniture < simple_data_updated-2.sql
```

## Table Count: 26 tables

### Execution Order (schema.sql)
Tables are created in dependency order:
1. Supplier, Staff, Customer, Item, TransferForm
2. RawMaterial, Product, Warehouse, Address
3. Quotation, WarehouseItem
4. Order, TransferForm_WarehouseItem
5. MaterialRequest, Invoice, OrderLine
6. Shipment, ReturnOrder, Complaint, Log
7. PurchaseOrder, DeliveryNote, ShipmentLine
8. PurchaseInvoice, PurchaseOrderLine, ReplySlip
9. ReturnOrderItem, Receipt, Transaction
