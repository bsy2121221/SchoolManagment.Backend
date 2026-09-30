namespace SchoolManagment.Models.Entities
{
    /// <summary>
    /// dbo.RolePermissions -- one row per (RoleId, ModuleName).
    ///
    /// A role with no row for a module has no access to it: absence is denial,
    /// so the grid fails closed. CK_RolePermissions_ViewImplied additionally
    /// forbids granting Create/Edit/Delete without View.
    ///
    /// ModuleName is drawn from Constants.Modules.
    /// </summary>
    public class RolePermission
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;

        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Joined by sp_GetRolePermissions for display.</summary>
        public string? RoleName { get; set; }
    }
}
