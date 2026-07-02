using System;
using System.Collections.Generic;

namespace PremiumLivingOPS.Models.Entities
{
    // ----------------------------------------------------------------
    // DTOs used by the Schedule Shipment wizard
    // (ScheduleShipmentDialog <-> LogisticsProcessingController <-> LogisticsProcessingRepo)
    // Placed in Models.Entities so all three MVC layers can reference them
    // without any layer depending on a higher layer.
    // ----------------------------------------------------------------

    /// <summary>
    /// Lightweight order summary shown in Step-1 order picker.
    /// </summary>
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

    /// <summary>
    /// One OrderLine row enriched with already-shipped qty.
    /// Shown in Step-2 scheduling grid.
    /// </summary>
    public class OrderLineDetail
    {
        public string ItemID             { get; set; }
        public string ItemName           { get; set; }
        public int    Quantity           { get; set; }
        public int    QtyAlreadyShipped  { get; set; }
    }

    /// <summary>
    /// One ShipmentLine within a CreateShipmentRequest.
    /// </summary>
    public class ShipmentLineRequest
    {
        public string ItemID  { get; set; }
        public int    QtyShip { get; set; }
        public int    Remain  { get; set; }
    }

    /// <summary>
    /// Request object passed from ScheduleShipmentDialog to
    /// LogisticsProcessingController.CreateScheduledShipment().
    /// </summary>
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
}
