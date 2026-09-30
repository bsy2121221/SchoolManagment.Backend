using Microsoft.AspNetCore.Authorization;

namespace SchoolManagment.API.Authorization
{
    /// <summary>
    /// "the caller may <see cref="Action"/> in <see cref="Module"/>".
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public PermissionRequirement(string module, string action)
        {
            Module = module;
            Action = action;
        }

        /// <summary>A name from Constants.Modules.</summary>
        public string Module { get; }

        /// <summary>A name from Constants.PermissionActions.</summary>
        public string Action { get; }

        /// <summary>
        /// The policy name a requirement is registered under. Built rather than
        /// listed so a new module needs no change here.
        /// </summary>
        public static string PolicyName(string module, string action) => $"Permission:{module}:{action}";
    }
}
