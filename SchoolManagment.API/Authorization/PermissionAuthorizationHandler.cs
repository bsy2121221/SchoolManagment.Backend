using Microsoft.AspNetCore.Authorization;
using SchoolManagment.Auth.Helpers;
using SchoolManagment.Models.Common;

namespace SchoolManagment.API.Authorization
{
    /// <summary>
    /// Evaluates a <see cref="PermissionRequirement"/> against the "perm" claims
    /// in the caller's access token. No database round trip: the grid was read at
    /// login and at every refresh, so a permission change lands within one token
    /// lifetime.
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return Task.CompletedTask;   // not authenticated: fail, so the caller is challenged

            // The SuperAdmin is granted everything by the seeded grid as well, so
            // this is a short circuit rather than a second source of truth -- and
            // it keeps the platform operator able to fix a school that has
            // revoked its own admin's access.
            var roleIdClaim = context.User.FindFirst(Constants.JwtClaims.RoleId)?.Value;
            if (int.TryParse(roleIdClaim, out var roleId) && roleId == Constants.RoleIds.SuperAdmin)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (PermissionClaims.Has(context.User, requirement.Module, requirement.Action))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
