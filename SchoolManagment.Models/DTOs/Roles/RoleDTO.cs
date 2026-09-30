namespace SchoolManagment.Models.DTOs.Roles
{
    public class RoleDTO
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>System roles cannot be renamed or deleted.</summary>
        public bool IsSystemRole { get; set; }

        public bool IsActive { get; set; }

        /// <summary>How many users currently hold this role. Returned by
        /// sp_GetRoles so the UI can warn before a delete.</summary>
        public int UserCount { get; set; }

        /// <summary>Modules with an active permission row. Returned by the list
        /// endpoint only; the single-role endpoint sends the grid itself.</summary>
        public int ModuleCount { get; set; }

        public int? CreatedBy { get; set; }
        public string? CreatedByUsername { get; set; }
        public int? ModifiedBy { get; set; }
        public string? ModifiedByUsername { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>Populated for the single-role endpoint, empty for the list.</summary>
        public List<RolePermissionDTO> Permissions { get; set; } = new();
    }
}
