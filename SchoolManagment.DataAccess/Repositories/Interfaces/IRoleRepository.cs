using SchoolManagment.Models.DTOs.Roles;
using SchoolManagment.Models.Entities;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    /// <summary>
    /// Roles are global -- there is no SchoolId anywhere in this interface. A
    /// school does not own its roles; the platform does.
    ///
    /// The write methods return the procedure's own Result string alongside the
    /// success flag, because the procedures explain their refusals ("system role
    /// cannot be deleted", "12 users still hold this role") and dropping that
    /// text would leave the API with nothing to say.
    /// </summary>
    public interface IRoleRepository
    {
        Task<List<RoleDTO>> GetRolesAsync(bool includeInactive = false);

        /// <summary>Role plus its permission grid (sp_GetRoleById, two sets).</summary>
        Task<RoleDTO?> GetRoleByIdAsync(int roleId);

        /// <summary>Pass null for every role's grid at once.</summary>
        Task<List<RolePermissionDTO>> GetRolePermissionsAsync(int? roleId = null);

        /// <summary>The effective grid for one user, as baked into their token.</summary>
        Task<List<RolePermission>> GetUserPermissionsAsync(int userId);

        Task<(bool Success, string Message, int? RoleId)> CreateRoleAsync(RoleCreateDTO role, int? createdBy);

        Task<(bool Success, string Message)> UpdateRoleAsync(int roleId, RoleUpdateDTO role, int? modifiedBy);

        Task<(bool Success, string Message)> DeleteRoleAsync(int roleId, int? deletedBy);

        Task<(bool Success, string Message)> SaveRolePermissionAsync(
            int roleId, RolePermissionSaveDTO permission, int? modifiedBy);

        Task<(bool Success, string Message)> DeleteRolePermissionAsync(
            int roleId, string moduleName, int? deletedBy);
    }
}
