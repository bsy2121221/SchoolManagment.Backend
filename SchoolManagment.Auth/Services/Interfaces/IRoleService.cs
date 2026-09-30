using SchoolManagment.Models.DTOs.Roles;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleDTO>> GetRolesAsync(bool includeInactive = false);
        Task<RoleDTO?> GetRoleByIdAsync(int roleId);
        Task<List<RolePermissionDTO>> GetRolePermissionsAsync(int roleId);

        /// <summary>The signed-in user's own grid, read live from the database
        /// rather than from their token -- for the UI to refresh what it shows
        /// after an admin changes a role mid-session.</summary>
        Task<List<RolePermissionDTO>> GetMyPermissionsAsync();

        Task<(bool Success, string Message, int? RoleId)> CreateRoleAsync(RoleCreateDTO role);
        Task<(bool Success, string Message)> UpdateRoleAsync(int roleId, RoleUpdateDTO role);
        Task<(bool Success, string Message)> DeleteRoleAsync(int roleId);

        Task<(bool Success, string Message)> SaveRolePermissionAsync(
            int roleId, RolePermissionSaveDTO permission);

        /// <summary>Saves several modules for one role, stopping at the first
        /// refusal so a half-applied grid is reported rather than hidden.</summary>
        Task<(bool Success, string Message)> SaveRolePermissionsAsync(
            int roleId, IEnumerable<RolePermissionSaveDTO> permissions);

        Task<(bool Success, string Message)> DeleteRolePermissionAsync(int roleId, string moduleName);

        /// <summary>The module names a permission may be granted on.</summary>
        IReadOnlyList<string> GetModules();
    }
}
