using System;
using System.Collections.Generic;

// ============================================================
//  FILE: Models/Entities/ProductionProcessingViewModels.cs
// ============================================================

namespace PremiumLivingOPS.Models.Entities
{
    public class MaterialRequestLineEntity
    {
        public string RequestID         { get; set; }
        public string RawMaterialItemID { get; set; }
        public string RawMaterialName   { get; set; }
        public string MaterialType      { get; set; }
        public string WarehouseItemID   { get; set; }
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public int    RequestedQty      { get; set; }
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }
    }

    public class MaterialRequestBatchEntity
    {
        private string _batchPrefix;

        // Always expose only MRQ-YYMMDD-NNN in main grid.
        public string BatchPrefix
        {
            get
            {
                if (string.IsNullOrEmpty(_batchPrefix)) return _batchPrefix;
                return _batchPrefix.Length > 14 ? _batchPrefix.Substring(0, 14) : _batchPrefix;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _batchPrefix = value;
                    return;
                }
                _batchPrefix = value.Length > 14 ? value.Substring(0, 14) : value;
            }
        }

        public string OrderID           { get; set; }
        public string UrgencyLevel      { get; set; }
        public string TriggerType       { get; set; }
        public int    TotalLines        { get; set; }
        public int    TotalRequestedQty { get; set; }
        public string WarehouseLocation { get; set; }
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }
        public bool   IsLinkedToPO      { get; set; }
    }

    public class MaterialRequestBatchDetailEntity
    {
        public string   BatchPrefix     { get; set; }
        public string   OrderID         { get; set; }
        public string   UrgencyLevel    { get; set; }
        public string   TriggerType     { get; set; }
        public int      TotalLines      { get; set; }
        public List<MaterialRequestLineEntity> Lines { get; set; } = new List<MaterialRequestLineEntity>();
        public string   PurchaseID      { get; set; }
        public string   PurchaseStatus  { get; set; }
        public decimal? POTotalAmount   { get; set; }
    }

    public class MaterialRequestEntity
    {
        public string RequestID         { get; set; }
        public string OrderID           { get; set; }
        public string RawMaterialItemID { get; set; }
        public string RawMaterialName   { get; set; }
        public string MaterialType      { get; set; }
        public string WarehouseItemID   { get; set; }
        public int    RequestedQty      { get; set; }
        public string UrgencyLevel      { get; set; }
        public string TriggerType       { get; set; }
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }
        public bool   IsLinkedToPO      { get; set; }
    }

    public class MaterialRequestDetailEntity
    {
        public string   RequestID         { get; set; }
        public string   OrderID           { get; set; }
        public string   RawMaterialItemID { get; set; }
        public string   RawMaterialName   { get; set; }
        public string   MaterialType      { get; set; }
        public string   WarehouseItemID   { get; set; }
        public string   WarehouseID       { get; set; }
        public string   WarehouseLocation { get; set; }
        public int      RequestedQty      { get; set; }
        public string   UrgencyLevel      { get; set; }
        public string   TriggerType       { get; set; }
        public int      CurrentStock      { get; set; }
        public int      ReorderLevel      { get; set; }
        public string   PurchaseID        { get; set; }
        public string   PurchaseStatus    { get; set; }
        public decimal? POTotalAmount     { get; set; }
    }

    public class RawMaterialLookup
    {
        public string  ItemID        { get; set; }
        public string  ItemName      { get; set; }
        public string  MaterialType  { get; set; }
        public decimal PurchasePrice { get; set; }
        public override string ToString() =>
            $"{ItemID}  —  {ItemName}  ({MaterialType})";
    }

    public class WarehouseItemLookup
    {
        public string WarehouseItemID   { get; set; }
        public string WarehouseID       { get; set; }
        public string WarehouseLocation { get; set; }
        public string ItemID            { get; set; }
        public int    CurrentStock      { get; set; }
        public int    ReorderLevel      { get; set; }
        public override string ToString() =>
            $"{WarehouseID}  —  {WarehouseLocation}  (Stock: {CurrentStock})";
    }

    public class OrderLookup
    {
        public string OrderID     { get; set; }
        public string CustomerID  { get; set; }
        public string OrderStatus { get; set; }
        public override string ToString() =>
            $"{OrderID}  ({OrderStatus})";
    }
}

namespace PremiumLivingOPS.Models.ViewModels
{
    using PremiumLivingOPS.Models.Entities;

    public class SearchMaterialRequestViewModel
    {
        public UserBarViewModel UserBar { get; set; }
        public string[] AllowedMenus { get; set; }
        public System.Collections.Generic.List<MaterialRequestBatchEntity> Batches { get; set; }
        public System.Collections.Generic.List<MaterialRequestEntity> Requests { get; set; }
    }

    public class CreateMaterialRequestViewModel
    {
        public UserBarViewModel UserBar { get; set; }
        public string[] AllowedMenus { get; set; }
        public System.Collections.Generic.List<RawMaterialLookup> RawMaterials { get; set; }
        public System.Collections.Generic.List<WarehouseItemLookup> WarehouseItems { get; set; }
        public System.Collections.Generic.List<OrderLookup> Orders { get; set; }
        public string NextRequestID { get; set; }
    }
}
