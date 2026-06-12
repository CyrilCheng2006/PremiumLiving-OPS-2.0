using System;

namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Maps to the Staff table in the database.
    /// Schema: Staff (StaffID, StaffName, Role, ContactPhone, ContactEmail, HireDate, IsActive)
    /// </summary>
    public class Staff
    {
        public string   StaffID      { get; set; }   // PK  e.g. STF-0001
        public string   StaffName    { get; set; }
        public string   Role         { get; set; }
        public string   ContactPhone { get; set; }
        public string   ContactEmail { get; set; }
        public DateTime HireDate     { get; set; }
        public bool     IsActive     { get; set; }

        // ── convenience aliases (backward-compat for code that used old property names) ──
        /// <summary>Alias for StaffID — kept for backward compatibility.</summary>
        public string   StaffId      => StaffID;
        /// <summary>Alias for StaffName — kept for backward compatibility.</summary>
        public string   FullName     => StaffName;

        /// <summary>Display text for ComboBox / pickers.</summary>
        public string   DisplayText  => $"{StaffID}  \u2013  {StaffName}  ({Role})";
    }
}
