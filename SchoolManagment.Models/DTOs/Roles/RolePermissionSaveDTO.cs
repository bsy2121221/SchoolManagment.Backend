using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Roles
{
    /// <summary>
    /// Upsert payload for one (role, module) cell. sp_SaveRolePermission inserts
    /// or updates, so the caller does not have to know whether the row exists.
    /// </summary>
    public class RolePermissionSaveDTO
    {
        [Required(ErrorMessage = "Module name is required")]
        [StringLength(50, ErrorMessage = "Module name cannot exceed 50 characters")]
        public string ModuleName { get; set; } = string.Empty;

        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
