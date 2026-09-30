using Microsoft.AspNetCore.Authorization;

namespace SchoolManagment.API.Authorization
{
    /// <summary>
    /// Authorises an action against the caller's permission grid instead of
    /// against a hard-coded list of role names:
    ///
    ///     [RequiresPermission(Constants.Modules.Users, Constants.PermissionActions.Edit)]
    ///
    /// The grid is data, so an admin can widen or narrow access without a
    /// redeploy -- which is the point of the Roles / RolePermissions tables.
    /// A module the caller has no row for is denied.
    ///
    /// This still requires authentication: it derives from AuthorizeAttribute, so
    /// an anonymous request is challenged before the requirement is evaluated.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequiresPermissionAttribute : AuthorizeAttribute
    {
        public RequiresPermissionAttribute(string module, string action)
        {
            Module = module;
            Action = action;
            Policy = PermissionRequirement.PolicyName(module, action);
        }

        public string Module { get; }
        public string Action { get; }
    }
}
