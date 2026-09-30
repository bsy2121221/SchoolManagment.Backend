using SchoolManagment.Models.Common;
using SchoolManagment.Models.Entities;
using System.Security.Claims;

namespace SchoolManagment.Auth.Helpers
{
    /// <summary>
    /// Reads the packed "perm" claims that JwtHelper writes -- one per module, in
    /// the form "Users:VCED".
    ///
    /// Anything unparseable is skipped rather than throwing: a malformed claim
    /// then grants nothing, which is the safe direction. A module with no claim
    /// is denied, so a token minted before permissions existed authorises nothing
    /// that RequiresPermission guards.
    /// </summary>
    public static class PermissionClaims
    {
        public static List<RolePermission> Read(ClaimsPrincipal? principal)
        {
            var permissions = new List<RolePermission>();
            if (principal == null)
                return permissions;

            foreach (var claim in principal.FindAll(Constants.JwtClaims.Permission))
            {
                var parsed = Parse(claim.Value);
                if (parsed != null)
                    permissions.Add(parsed);
            }

            return permissions;
        }

        public static bool Has(ClaimsPrincipal? principal, string module, string action)
        {
            if (principal == null || string.IsNullOrWhiteSpace(module))
                return false;

            foreach (var claim in principal.FindAll(Constants.JwtClaims.Permission))
            {
                var separator = claim.Value.IndexOf(Constants.PermissionClaim.Separator);
                if (separator <= 0)
                    continue;

                var claimModule = claim.Value.AsSpan(0, separator);
                if (!claimModule.Equals(module.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    continue;

                var flags = claim.Value.AsSpan(separator + 1);
                var wanted = FlagFor(action);
                return wanted != null && flags.IndexOf(wanted.Value) >= 0;
            }

            return false;
        }

        private static RolePermission? Parse(string value)
        {
            var separator = value.IndexOf(Constants.PermissionClaim.Separator);
            if (separator <= 0)
                return null;

            var flags = value[(separator + 1)..];

            return new RolePermission
            {
                ModuleName = value[..separator],
                CanView = flags.Contains(Constants.PermissionClaim.View),
                CanCreate = flags.Contains(Constants.PermissionClaim.Create),
                CanEdit = flags.Contains(Constants.PermissionClaim.Edit),
                CanDelete = flags.Contains(Constants.PermissionClaim.Delete),
                IsActive = true
            };
        }

        private static char? FlagFor(string action)
        {
            if (Matches(action, Constants.PermissionActions.View)) return Constants.PermissionClaim.View;
            if (Matches(action, Constants.PermissionActions.Create)) return Constants.PermissionClaim.Create;
            if (Matches(action, Constants.PermissionActions.Edit)) return Constants.PermissionClaim.Edit;
            if (Matches(action, Constants.PermissionActions.Delete)) return Constants.PermissionClaim.Delete;

            // An action nobody defined denies, rather than falling through to
            // whichever flag happened to sort first.
            return null;
        }

        private static bool Matches(string action, string expected) =>
            string.Equals(action, expected, StringComparison.OrdinalIgnoreCase);
    }
}
