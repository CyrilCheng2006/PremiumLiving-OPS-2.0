namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Represents a row in the Staff table.
    /// Columns aligned with schema.sql:
    ///   StaffID, StaffName, StaffRole, Department, Email, StaffPassword
    /// </summary>
    public class Staff
    {
        // ── Private fields ───────────────────────────────────────────
        private string staffId;
        private string staffName;
        private string staffRole;
        private string department;
        private string email;
        private string staffPassword;

        // ── Constructors ─────────────────────────────────────────────
        public Staff() { }

        public Staff(string staffId, string staffName, string staffRole,
                     string department, string email, string staffPassword)
        {
            this.staffId       = staffId;
            this.staffName     = staffName;
            this.staffRole     = staffRole;
            this.department    = department;
            this.email         = email;
            this.staffPassword = staffPassword;
        }

        // ── Properties ───────────────────────────────────────────────

        /// <summary>Primary key alias (uppercase D) — used by Controller layer.</summary>
        public string StaffID
        {
            get { return staffId; }
            set { staffId = value; }
        }

        /// <summary>Backward-compat alias (lowercase d).</summary>
        public string StaffId
        {
            get { return staffId; }
            set { staffId = value; }
        }

        public string StaffName
        {
            get { return staffName; }
            set { staffName = value; }
        }

        /// <summary>Maps to StaffRole column in DB (ENUM: Administrator/Manager/Clerk/Staff/Deliverer).</summary>
        public string Role
        {
            get { return staffRole; }
            set { staffRole = value; }
        }

        /// <summary>Maps to Department column in DB (ENUM: IT/Production/Sales/Inventory/Finance/Logistics).</summary>
        public string Department
        {
            get { return department; }
            set { department = value; }
        }

        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        /// <summary>Maps to StaffPassword column in DB.</summary>
        public string Password
        {
            get { return staffPassword; }
            set { staffPassword = value; }
        }

        /// <summary>Display string combining role and name.</summary>
        public string DisplayName
        {
            get { return $"{staffRole} — {staffName}"; }
        }
    }
}
