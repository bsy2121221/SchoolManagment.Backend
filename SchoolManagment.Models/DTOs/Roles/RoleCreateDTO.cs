using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Roles
{
    public class RoleCreateDTO
    {
        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
        public string RoleName { get; set; } = string.Empty;

        /// <summary>
        /// Optional. sp_CreateRole derives it from the name when omitted, using
        /// the same sanitiser that builds school codes.
        /// </summary>
        [StringLength(20, ErrorMessage = "Role code cannot exceed 20 characters")]
        [RegularExpression(@"^[A-Za-z0-9_]+$",
            ErrorMessage = "Role code may contain only letters, numbers and underscores")]
        public string? RoleCode { get; set; }

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }

        /// <summary>
        /// Optional starting grid. Sent to sp_SaveRolePermission one module at a
        /// time after the role is created; omit it and the role starts with no
        /// access at all.
        /// </summary>
        public List<RolePermissionSaveDTO> Permissions { get; set; } = new();
    }
}
