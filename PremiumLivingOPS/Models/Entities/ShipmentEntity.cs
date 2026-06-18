using System;

namespace PremiumLivingOPS.Models.Entities
{
    public class ShipmentEntity
    {
        public string    ShipmentID      { get; set; }
        public string    OrderID         { get; set; }
        public string    TrackingNumber  { get; set; }  // DB col: TrackingNumber
        public DateTime  ShipDate        { get; set; }
        public string    DeliveryMethod  { get; set; }
        public string    ShipmentStatus  { get; set; }
        public string    ShipmentType    { get; set; }
        public double    TotalAmount     { get; set; }

        // Joined from Order
        public string    CustomerName    { get; set; }
        public string    ShippingAddress { get; set; }  // comes from o.ShippingAddress (Order table)
        public DateTime? DeliveryDate    { get; set; }  // comes from o.DeliveryDate   (Order table)
    }

    public class ShipmentLineEntity
    {
        public string ShipmentLineID { get; set; }
        public string ShipmentID     { get; set; }
        public string OrderID        { get; set; }
        public string ItemID         { get; set; }
        public string ItemName       { get; set; }
        public int    QtyShipped     { get; set; }
        public int?   QtyOutstanding { get; set; }
    }

    public class DeliveryNoteEntity
    {
        public string   DeliveryID      { get; set; }
        public string   ShipmentID      { get; set; }
        public DateTime DeliveryDate    { get; set; }
        public int?     OutstandingQty  { get; set; }
        public string   ShippingAddress { get; set; }
        public string   ShipToName      { get; set; }
    }

    public class ReplySlipEntity
    {
        public string   SlipID            { get; set; }
        public string   DeliveryID        { get; set; }
        public string   ActualRecipient   { get; set; }
        public DateTime ReceivedDate      { get; set; }
        public string   RecipientRemark   { get; set; }
    }
}
