CREATE DATABASE PremiumLivingFurniture;

USE PremiumLivingFurniture;

/*Supplier*/

CREATE TABLE Supplier (
SupplierID VARCHAR(20) NOT NULL PRIMARY KEY,
PhoneNumber VARCHAR(20) NOT NULL,
SupplierAddress VARCHAR(255) NOT NULL,
SupplierName VARCHAR(150) NOT NULL
);

/*Staff*/

CREATE TABLE Staff (
StaffID VARCHAR(20) NOT NULL PRIMARY KEY,
StaffName VARCHAR(100) NOT NULL,
StaffRole ENUM('Administrator','Manager','Clerk','Staff','Deliverer') Not NULL,
Email VARCHAR(50) NOT NULL,
StaffPassword VARCHAR(255) NOT NULL,
Department ENUM('IT','Production','Sales','Inventory', 'Finance', 'Logistics') Not NULL
);

/*Customer*/

CREATE TABLE Customer (
CustomerID VARCHAR(20) NOT NULL PRIMARY KEY,
CustomerName VARCHAR(255) NOT NULL,
EmailAddress VARCHAR(255) NOT NULL,
PhoneNumber VARCHAR(20) NOT NULL
);

/*Item*/

CREATE TABLE Item (
ItemID VARCHAR(20) NOT NULL PRIMARY KEY,
ItemName VARCHAR(100) NOT NULL,
ItemDescription VARCHAR(255)
);

/*TransferForm*/

CREATE TABLE TransferForm (
TransferID VARCHAR(20) NOT NULL PRIMARY KEY,
TransferDate DATE NOT NULL,
TransferStatus ENUM('Pending','Cancelled', 'Completed') Not NULL
);

/*RawMaterial*/

CREATE TABLE RawMaterial (
ItemID VARCHAR(20) NOT NULL PRIMARY KEY,
purchasePrice DECIMAL(10,2) NOT NULL,
MaterialType ENUM('Wood', 'Metal', 'Fabric', 'Foam', 'Glass', 'Paint') NOT NULL,
CONSTRAINT fk_rawmaterial_item FOREIGN KEY (ItemID) REFERENCES Item(ItemID)
);

/*Product*/

CREATE TABLE Product (
ItemID VARCHAR(20) NOT NULL PRIMARY KEY,
SalesPrice DECIMAL(10,2) NOT NULL,
Category ENUM('Sofa','Bed','Table','Chair','Cabinet') Not NULL,
CONSTRAINT fk_product_item FOREIGN KEY (ItemID) REFERENCES Item(ItemID)
);

/*Warehouse*/

CREATE TABLE Warehouse (
WarehouseID VARCHAR(20) NOT NULL PRIMARY KEY,
ManagerID VARCHAR(20) NOT NULL,
WarehouseLocation VARCHAR(255) NOT NULL,
ContactNumber VARCHAR(20) NOT NULL,
Capacity INTEGER NOT NULL,
CONSTRAINT fk_warehouse_manager FOREIGN KEY (ManagerID) REFERENCES Staff(StaffID)
);

/*Address*/

CREATE TABLE Address (
AddressID VARCHAR(20) NOT NULL PRIMARY KEY,
CustomerID VARCHAR(20) NOT NULL,
AddressName VARCHAR(255) NOT NULL,
AddressType ENUM('Residential','Office','Mailing') NOT NULL,
isDefault BIT NOT NULL,
CONSTRAINT fk_address_customer FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID)
);

/*Quotation*/

CREATE TABLE Quotation (
QuotationID VARCHAR(20) NOT NULL PRIMARY KEY,
CustomerID VARCHAR(20) NOT NULL,
ExpiryDate DATE NOT NULL,
TotalAmount DOUBLE(10,2) NOT NULL,
DepositRequired DOUBLE(10,2),
LeadTimeEstimated VARCHAR(255),
TermsandCondition VARCHAR(255),
QuotationStatus ENUM('Converted','Rejected','Pending') Not NULL,
CONSTRAINT fk_quotation_customer FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID)
);

/*WarehouseItem*/

CREATE TABLE WarehouseItem (
WarehouseItemID VARCHAR(20) NOT NULL PRIMARY KEY,
ItemID VARCHAR(20) NOT NULL,
WarehouseID VARCHAR(20) NOT NULL,
WarehouseItemQuantity INTEGER NOT NULL,
ReorderLevel INTEGER NOT NULL,
CONSTRAINT fk_warehouseitem_item FOREIGN KEY (ItemID) REFERENCES Item(ItemID),
CONSTRAINT fk_warehouseitem_warehouse FOREIGN KEY (WarehouseID) REFERENCES Warehouse(WarehouseID)
);

/*Order*/

CREATE TABLE `Order` (
OrderID VARCHAR(20) NOT NULL PRIMARY KEY,
QuotationID VARCHAR(20),
CustomerID VARCHAR(20) NOT NULL,
AddressID VARCHAR(20),
SalesID VARCHAR(20) NOT NULL,
IssuedTime DATE NOT NULL,
DeliveryDate DATE NOT NULL,
ShippingAddress VARCHAR(255) NOT NULL,
BillingAddress VARCHAR(255) NOT NULL,
SubTotal DOUBLE(10,2),
DiscountType ENUM('Amount','Rate'),
DiscountValue DOUBLE(10,2),
DiscountAmount DOUBLE(10,2),
GrandTotal DOUBLE(10,2) NOT NULL,
OrderContactName VARCHAR(50) NOT NULL,
OrderStatus ENUM('Processing','Partially Delivered','Delivered','Cancelled','Pending','Completed') Not NULL,
CONSTRAINT fk_order_quotation FOREIGN KEY (QuotationID) REFERENCES Quotation(QuotationID),
CONSTRAINT fk_order_customer FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID),
CONSTRAINT fk_order_address FOREIGN KEY (AddressID) REFERENCES Address(AddressID),
CONSTRAINT fk_order_staff FOREIGN KEY (SalesID) REFERENCES Staff(StaffID)
);

/*TransferForm_WarehouseItem*/

CREATE TABLE TransferForm_WarehouseItem (
TransferLineID VARCHAR(20) NOT NULL PRIMARY KEY,
TransferID VARCHAR(20) NOT NULL,
FromWarehouseItemID VARCHAR(20) NOT NULL,
ToWarehouseItemID VARCHAR(20) NOT NULL,
TransferQuantity INTEGER NOT NULL,
CONSTRAINT fk_transferline_form FOREIGN KEY (TransferID) REFERENCES TransferForm(TransferID),
CONSTRAINT fk_transferline_from_whi FOREIGN KEY (FromWarehouseItemID) REFERENCES WarehouseItem(WarehouseItemID),
CONSTRAINT fk_transferline_to_whi FOREIGN KEY (ToWarehouseItemID) REFERENCES WarehouseItem(WarehouseItemID)
);

/*MaterialRequest*/

CREATE TABLE MaterialRequest (
RequestID VARCHAR(20) NOT NULL PRIMARY KEY,
OrderID VARCHAR(20),
RawMaterialItemID VARCHAR(20) NOT NULL,
WarehouseItemID VARCHAR(20) NOT NULL,
RequestedQty INTEGER NOT NULL,
UrgencyLevel ENUM('Critical','High','Medium') Not NULL,
TriggerType ENUM('Reorder', 'OrderDemand') Not NULL,
CONSTRAINT fk_matreq_rawmaterial FOREIGN KEY (RawMaterialItemID) REFERENCES RawMaterial(ItemID),
CONSTRAINT fk_matreq_order FOREIGN KEY (OrderID) REFERENCES `Order`(OrderID),
CONSTRAINT fk_matreq_warehouseitem FOREIGN KEY (WarehouseItemID) REFERENCES WarehouseItem(WarehouseItemID)
);

/* Invoice*/

CREATE TABLE Invoice (
InvoiceID VARCHAR(20) NOT NULL PRIMARY KEY,
OrderID VARCHAR(20) NOT NULL,
InvoiceDate DATE NOT NULL,
DepositAmount DOUBLE(10,2),
PaidAmount DOUBLE(10,2) NOT NULL,
RemainingBalance DOUBLE(10,2) NOT NULL,
TotalAmount DOUBLE(10,2) NOT NULL,
PaymentStatus ENUM('Partial','Full') NOT NULL,
DueDate DATE NOT NULL,
CONSTRAINT fk_invoice_order FOREIGN KEY (OrderID) REFERENCES `Order`(OrderID)
);

/*OrderLine*/

CREATE TABLE OrderLine (
OrderID VARCHAR(20) NOT NULL,
ItemID VARCHAR(20) NOT NULL,
Quantity INTEGER NOT NULL,
Price DOUBLE(10,2) NOT NULL,
PRIMARY KEY (OrderID, ItemID),
CONSTRAINT fk_orderitem_order FOREIGN KEY (OrderID) REFERENCES `Order`(OrderID),
CONSTRAINT fk_orderitem_item FOREIGN KEY (ItemID) REFERENCES Item(ItemID)
);

/*Shipment*/

CREATE TABLE Shipment (
ShipmentID VARCHAR(20) NOT NULL PRIMARY KEY,
OrderID VARCHAR(20) NOT NULL,
TrackingNumber VARCHAR(20) NOT NULL,
ShipDate DATE NOT NULL,
DeliveryMethod ENUM('Courier', 'SelfPickup') NOT NULL,
ShipmentStatus ENUM('Pending','In Transit','Completed') NOT NULL,
ShipmentType ENUM('Partial', 'Full') NOT NULL,
TotalAmount DOUBLE(10,2) NOT NULL,
CONSTRAINT fk_shipment_order FOREIGN KEY (OrderID) REFERENCES `Order`(OrderID)
);

/*ReturnOrder*/

CREATE TABLE ReturnOrder (
ReturnID VARCHAR(20) NOT NULL PRIMARY KEY,
OrderID VARCHAR(20) NOT NULL,
ReturnDate DATE NOT NULL,
Reason VARCHAR(255),
RefundAmount DOUBLE(10,2) NOT NULL,
ReturnStatus ENUM('Pending','Approved','Processing','Rejected','Completed') NOT NULL,
CONSTRAINT fk_returnorder_order FOREIGN KEY (OrderID) REFERENCES `Order`(OrderID)
);

/*Complaint*/

CREATE TABLE Complaint (
ComplaintID VARCHAR(20) NOT NULL PRIMARY KEY,
OrderID VARCHAR(20),
StaffID VARCHAR(20) NOT NULL,
ComplaintDescription VARCHAR(255),
ComplaintStatus ENUM('Pending','Processing','Escalated','Completed') NOT NULL,
CONSTRAINT fk_complaint_order FOREIGN KEY (OrderID) REFERENCES `Order`(OrderID),
CONSTRAINT fk_complaint_staff FOREIGN KEY (StaffID) REFERENCES Staff(StaffID)
);

/*Log*/

CREATE TABLE Log (
LogID VARCHAR(36) PRIMARY KEY,
StaffID VARCHAR(20),
LogType ENUM('Login','Create','Edit','Delete') Not NULL,
TargetTable VARCHAR(50),
LogTimeStamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
OldValue TEXT,
NewValue TEXT,
CONSTRAINT fk_log_staff FOREIGN KEY (StaffID) REFERENCES Staff(StaffID)
);

/*PurchaseOrder*/

CREATE TABLE PurchaseOrder (
PurchaseID VARCHAR(20) NOT NULL PRIMARY KEY,
RequestID VARCHAR(20) NOT NULL,
SupplierID VARCHAR(20) NOT NULL,
POTotalAmount Double(10,2) NOT NULL,
OrderDate DATE NOT NULL,
PurchaseStatus ENUM('Sent','Cancelled', 'Partially Received', 'Received', 'Completed') Not NULL,
CONSTRAINT fk_purchaseorder_supplier FOREIGN KEY (SupplierID) REFERENCES Supplier(SupplierID),
CONSTRAINT fk_purchaseorder_request FOREIGN KEY (RequestID) REFERENCES MaterialRequest(RequestID)
);

/*DeliveryNote*/

CREATE TABLE DeliveryNote (
DeliveryID VARCHAR(20) NOT NULL PRIMARY KEY,
ShipmentID VARCHAR(20) NOT NULL,
DeliveryDate DATE NOT NULL,
Outstanding_qty INTEGER,
ShippingAddress VARCHAR(255) NOT NULL,
ShipToName VARCHAR(50) NOT NULL,
CONSTRAINT fk_deliverynote_shipment FOREIGN KEY (ShipmentID) REFERENCES Shipment(ShipmentID)
);

/*ShipmentLine*/

CREATE TABLE ShipmentLine (
ShipmentLineID VARCHAR(20) NOT NULL PRIMARY KEY,
ShipmentID VARCHAR(20) NOT NULL,
OrderID VARCHAR(20) NOT NULL,
ItemID VARCHAR(20) NOT NULL,
QtyShipped INTEGER NOT NULL,
QtyOutstanding INTEGER,
CONSTRAINT fk_shipmentline_shipment FOREIGN KEY (ShipmentID) REFERENCES Shipment(ShipmentID),
CONSTRAINT fk_shipmentline_order FOREIGN KEY (OrderID, ItemID) REFERENCES OrderLine(OrderID, ItemID)
);

/*PurchaseInvoice*/

CREATE TABLE PurchaseInvoice (
PurInvoiceID VARCHAR(20) NOT NULL PRIMARY KEY,
PurchaseID VARCHAR(20) NOT NULL,
TotalAmount DOUBLE(10,2) NOT NULL,
PaymentStatus ENUM('Partial', 'Full') NOT NULL,
ExpectedDate DATE NOT NULL,
CONSTRAINT fk_purchaseinvoice_purchaseorder FOREIGN KEY (PurchaseID) REFERENCES PurchaseOrder(PurchaseID)
);

/*PurchaseOrderLine*/

CREATE TABLE PurchaseOrderLine (
POLineID VARCHAR(20) NOT NULL PRIMARY KEY,
RawMaterialItemID VARCHAR(20) NOT NULL,
PurchaseID VARCHAR(20) NOT NULL,
WarehouseID VARCHAR(20) NOT NULL,
OrderQty INTEGER NOT NULL,
UnitPrice DOUBLE(10,2) NOT NULL,
CONSTRAINT fk_poline_purchase FOREIGN KEY (PurchaseID) REFERENCES PurchaseOrder(PurchaseID),
CONSTRAINT fk_poline_item FOREIGN KEY (RawMaterialItemID) REFERENCES RawMaterial(ItemID),
CONSTRAINT fk_poline_warehouse FOREIGN KEY (WarehouseID) REFERENCES Warehouse(WarehouseID)
);

/*ReplySlip*/

CREATE TABLE ReplySlip (
SlipID VARCHAR(20) NOT NULL PRIMARY KEY,
DeliveryID VARCHAR(20) NOT NULL,
actualRecipient VARCHAR(50) NOT NULL,
ReceivedDate DATE NOT NULL,
RecipientRemark VARCHAR(255),
CONSTRAINT fk_replyslip_delivery FOREIGN KEY (DeliveryID) REFERENCES DeliveryNote(DeliveryID)
);

/*ReturnOrderItem*/

CREATE TABLE ReturnOrderItem (
ReturnItemID VARCHAR(20) NOT NULL PRIMARY KEY,
ReturnID VARCHAR(20) NOT NULL,
ShipmentLineID VARCHAR(20) NOT NULL,
Quantity INTEGER NOT NULL,
CONSTRAINT fk_returnitem_return FOREIGN KEY (ReturnID) REFERENCES ReturnOrder(ReturnID),
CONSTRAINT fk_returnitem_shipmentline FOREIGN KEY (ShipmentLineID) REFERENCES ShipmentLine(ShipmentLineID)
);

/*Receipt*/

CREATE TABLE Receipt (
ReceiptID VARCHAR(20) NOT NULL PRIMARY KEY,
PurchaseID VARCHAR(20) NOT NULL,
POLineID VARCHAR(20) NOT NULL,
QtyReceived INTEGER NOT NULL,
ReceiptDate DATE NOT NULL,
Outstanding_QTY INTEGER,
CONSTRAINT fk_receipt_purchaseorder FOREIGN KEY (PurchaseID) REFERENCES PurchaseOrder(PurchaseID),
CONSTRAINT fk_receipt_poline FOREIGN KEY (POLineID) REFERENCES PurchaseOrderLine(POLineID)
);

/*Transaction*/

CREATE TABLE `Transaction` (
TransactionID VARCHAR(20) NOT NULL PRIMARY KEY,
InvoiceID VARCHAR(20),
PurInvoiceID VARCHAR(20),
ReturnID VARCHAR(20),
Amount DOUBLE(10,2) NOT NULL,
TransactionDate DATE NOT NULL,
TransactionType ENUM('Deposit','Installment','Full','Refund') NOT NULL,
CONSTRAINT fk_transaction_invoice FOREIGN KEY (InvoiceID) REFERENCES Invoice(InvoiceID),
CONSTRAINT fk_transaction_purchaseorder FOREIGN KEY (PurInvoiceID) REFERENCES PurchaseInvoice(PurInvoiceID),
CONSTRAINT fk_transaction_return FOREIGN KEY (ReturnID) REFERENCES ReturnOrder(ReturnID)
);
