namespace SchoolManagment.Models.DTOs.Roles
{
    /// <summary>
    /// One module's four flags for one role. A module with no row is denied
    /// outright, so a module missing from a role's list is not the same as one
    /// present with all four flags false -- though both deny.
    /// </summary>
    public class RolePermissionDTO
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public string ModuleName { get; set; } = string.Empty;

        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        public bool IsActive { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
