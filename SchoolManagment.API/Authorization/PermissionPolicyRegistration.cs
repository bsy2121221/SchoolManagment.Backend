using Microsoft.AspNetCore.Authorization;
using SchoolManagment.Models.Common;

namespace SchoolManagment.API.Authorization
{
    public static class PermissionPolicyRegistration
    {
        /// <summary>
        /// Registers one policy per (module, action) pair -- 15 modules x 4
        /// actions. Registered eagerly rather than through a policy provider
        /// because the set is small, fixed, and known at startup, and because a
        /// typo in a module name then fails at startup instead of silently
        /// denying at runtime.
        /// </summary>
        public static void AddPermissionPolicies(this AuthorizationOptions options)
        {
            var actions = new[]
            {
                Constants.PermissionActions.View,
                Constants.PermissionActions.Create,
                Constants.PermissionActions.Edit,
                Constants.PermissionActions.Delete
            };

            foreach (var module in Constants.Modules.All)
            {
                foreach (var action in actions)
                {
                    options.AddPolicy(
                        PermissionRequirement.PolicyName(module, action),
                        policy => policy
                            .RequireAuthenticatedUser()
                            .AddRequirements(new PermissionRequirement(module, action)));
                }
            }
        }
    }
}
