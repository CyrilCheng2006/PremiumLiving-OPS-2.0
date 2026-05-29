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

---

## Password Hashing Policy

All staff passwords are stored as **PBKDF2-HMACSHA256** hashes — never as plain text.

### Algorithm Specification

| Parameter | Value |
|-----------|-------|
| Algorithm | PBKDF2-HMACSHA256 |
| Salt length | 16 bytes (cryptographically random, unique per password) |
| Iterations | 100,000 (OWASP 2024 recommendation) |
| Output length | 32 bytes |
| Comparison | Constant-time (prevents timing attacks) |
| Stored format | `100000:saltBase64:hashBase64` |

Implementation: `PremiumLivingOPS/Models/Helpers/PasswordHelper.cs`

### How It Works

#### Login Flow
1. The system retrieves the staff record by `StaffID` only — the password is **never compared inside SQL**.
2. The plain-text password entered by the user is verified against the stored hash in C# using `PasswordHelper.Verify()`.
3. If the hashes match, login succeeds.

#### Creating / Editing a Staff Account
- The caller passes the plain-text password to `StaffRepo.Add()` or `StaffRepo.Edit()`.
- The repository hashes it via `PasswordHelper.Hash()` before writing to the database.
- The plain-text password is **never written to the database**.

### Migration Path (Existing Plain-Text Passwords)

Existing accounts whose passwords are still stored as plain text **do not need to be manually updated**.
The system handles migration automatically and transparently:

```
First login after upgrade
─────────────────────────────────────────────────
1. User submits password (e.g. "abc123")
2. System fetches stored value from DB → plain text detected
3. Plain-text comparison: "abc123" == "abc123" → correct
4. System immediately re-hashes the password and writes it back to DB
   → DB now stores: "100000:xK9s…:mP2f…"
5. Login succeeds ✅

All subsequent logins
─────────────────────────────────────────────────
1. User submits password (e.g. "abc123")
2. System fetches stored hash from DB → hash detected
3. PasswordHelper.Verify("abc123", storedHash) → correct
4. Login succeeds ✅
```

Each account is automatically upgraded on its **first successful login after the update** — no manual SQL updates or data migration scripts are required.
