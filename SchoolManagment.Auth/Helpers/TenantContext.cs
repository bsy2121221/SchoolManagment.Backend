using Microsoft.AspNetCore.Http;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.Entities;
using System.Security.Claims;

namespace SchoolManagment.Auth.Helpers
{
    public class TenantContext : ITenantContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

        public int? SchoolId => GetInt(Constants.JwtClaims.SchoolId);

        public string? SchoolCode => Principal?.FindFirst(Constants.JwtClaims.SchoolCode)?.Value;

        public int? UserId => GetInt(Constants.JwtClaims.UserId);

        public string? Username => Principal?.FindFirst(Constants.JwtClaims.Username)?.Value;

        public string? Role => Principal?.FindFirst(Constants.JwtClaims.Role)?.Value
            ?? Principal?.FindFirst(ClaimTypes.Role)?.Value;

        public int? RoleId => GetInt(Constants.JwtClaims.RoleId);

        /// <summary>
        /// Prefers the numeric claim: the role name is display text an admin may
        /// change, the id is the contract (Constants.RoleIds.SuperAdmin). The name
        /// is still consulted so tokens issued before role_id existed keep working
        /// until they expire.
        /// </summary>
        public bool IsSuperAdmin =>
            RoleId == Constants.RoleIds.SuperAdmin
            || (RoleId == null && Role?.Equals(Constants.Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase) == true);

        public IReadOnlyList<RolePermission> Permissions => PermissionClaims.Read(Principal);

        public bool HasPermission(string module, string action) =>
            PermissionClaims.Has(Principal, module, action);

        private int? GetInt(string claimType)
        {
            var value = Principal?.FindFirst(claimType)?.Value;
            return int.TryParse(value, out int parsed) ? parsed : null;
        }
    }
}
