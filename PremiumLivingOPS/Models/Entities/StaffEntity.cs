// StaffEntity.cs — Data entity matching the Staff table in the database schema.
// Used by SystemControlRepo, SystemControlController, and SystemControlViewModels.
// NOTE: StaffListForm.cs uses the legacy 'Staff' class (with .StaffId / .Role);
//       this entity is specifically for the SystemControl MVC stack.
namespace PremiumLivingOPS.Models.Entities
{
    public class StaffEntity
    {
        public string StaffID    { get; set; }
        public string StaffName  { get; set; }
        /// <summary>Maps to the StaffRole column in the Staff table.</summary>
        public string StaffRole  { get; set; }
        public string Department { get; set; }
        public string Email      { get; set; }
        public string Password   { get; set; }
    }
}
