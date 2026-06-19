namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Entity mapping the Complaint table.
    /// StaffID is stored on insert; StaffName is populated on SELECT via JOIN.
    /// </summary>
    public class ComplaintEntity
    {
        public string ComplaintID          { get; set; }
        public string OrderID              { get; set; }   // nullable
        public string StaffID              { get; set; }   // FK — used on INSERT
        public string StaffName            { get; set; }   // resolved via JOIN — used on SELECT
        public string ComplaintDescription { get; set; }   // nullable
        public string ComplaintStatus      { get; set; }   // Pending | Processing | Escalated | Completed
    }
}
