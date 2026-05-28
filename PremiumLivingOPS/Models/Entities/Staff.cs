namespace PremiumLivingOPS.Models.Entities
{
    /// <summary>
    /// Represents a row in the Staff table.
    /// Phase 1 — Step 1: Entity Class
    /// </summary>
    public class Staff
    {
        // ── Primary Key ──────────────────────────────────────────────
        private string staffId;
        private string staffName;
        private string role;
        private string department;
        private string email;
        private string password;
        private string status;   // Active | Inactive

        // ── Constructors ─────────────────────────────────────────────
        public Staff() { }

        public Staff(string staffId, string staffName, string role,
                     string department, string email,
                     string password, string status)
        {
            this.staffId    = staffId;
            this.staffName  = staffName;
            this.role       = role;
            this.department = department;
            this.email      = email;
            this.password   = password;
            this.status     = status;
        }

        // ── Properties ───────────────────────────────────────────────
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

        public string Role
        {
            get { return role; }
            set { role = value; }
        }

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

        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        // ── Derived Attribute ────────────────────────────────────────
        /// <summary>Display name combining role and name, e.g. "Manager — Chan Ho Yuen"</summary>
        public string DisplayName
        {
            get { return $"{role} — {staffName}"; }
        }
    }
}
