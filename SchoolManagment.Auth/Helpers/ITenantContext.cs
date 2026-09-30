using SchoolManagment.Models.Entities;

namespace SchoolManagment.Auth.Helpers
{
    public interface ITenantContext
    {
        int? SchoolId { get; }
        string? SchoolCode { get; }
        int? UserId { get; }
        string? Username { get; }

        /// <summary>Role name, from the "role" claim.</summary>
        string? Role { get; }

        /// <summary>dbo.Roles.Id, from the "role_id" claim. Survives a rename.</summary>
        int? RoleId { get; }

        bool IsSuperAdmin { get; }

        /// <summary>
        /// The permission grid carried by the current access token. Only modules
        /// the role can actually touch appear.
        /// </summary>
        IReadOnlyList<RolePermission> Permissions { get; }

        /// <summary>
        /// <paramref name="module"/> from Constants.Modules,
        /// <paramref name="action"/> from Constants.PermissionActions. False when
        /// the module is absent from the token -- absence is denial.
        /// </summary>
        bool HasPermission(string module, string action);
    }
}
