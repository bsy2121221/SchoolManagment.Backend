using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Roles;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ITenantContext _tenantContext;

        public RoleService(IRoleRepository roleRepository, ITenantContext tenantContext)
        {
            _roleRepository = roleRepository;
            _tenantContext = tenantContext;
        }

        private int? ActorUserId => _tenantContext.UserId;

        public Task<List<RoleDTO>> GetRolesAsync(bool includeInactive = false) =>
            _roleRepository.GetRolesAsync(includeInactive);

        public Task<RoleDTO?> GetRoleByIdAsync(int roleId) =>
            _roleRepository.GetRoleByIdAsync(roleId);

        public Task<List<RolePermissionDTO>> GetRolePermissionsAsync(int roleId) =>
            _roleRepository.GetRolePermissionsAsync(roleId);

        public async Task<List<RolePermissionDTO>> GetMyPermissionsAsync()
        {
            if (!_tenantContext.UserId.HasValue)
                return new List<RolePermissionDTO>();

            var permissions = await _roleRepository.GetUserPermissionsAsync(_tenantContext.UserId.Value);
            var roleId = _tenantContext.RoleId ?? 0;

            return permissions.Select(p => new RolePermissionDTO
            {
                RoleId = roleId,
                RoleName = _tenantContext.Role,
                ModuleName = p.ModuleName,
                CanView = p.CanView,
                CanCreate = p.CanCreate,
                CanEdit = p.CanEdit,
                CanDelete = p.CanDelete,
                IsActive = true
            }).ToList();
        }

        public async Task<(bool Success, string Message, int? RoleId)> CreateRoleAsync(RoleCreateDTO role)
        {
            var unknown = FirstUnknownModule(role.Permissions.Select(p => p.ModuleName));
            if (unknown != null)
                return (false, $"'{unknown}' is not a known module.", null);

            var created = await _roleRepository.CreateRoleAsync(role, ActorUserId);
            if (!created.Success || created.RoleId == null)
                return created;

            // The starting grid is applied only after the role exists, one module
            // at a time, because sp_SaveRolePermission is the only thing that
            // enforces CK_RolePermissions_ViewImplied's intent per row.
            foreach (var permission in role.Permissions.Select(Canonical))
            {
                var saved = await _roleRepository.SaveRolePermissionAsync(
                    created.RoleId.Value, permission, ActorUserId);

                if (!saved.Success)
                {
                    // The role stays: it was created, and reporting otherwise
                    // would leave the caller thinking they can retry the name.
                    return (false,
                        $"Role created (id {created.RoleId}), but module '{permission.ModuleName}' " +
                        $"was not saved: {saved.Message}",
                        created.RoleId);
                }
            }

            return created;
        }

        public Task<(bool Success, string Message)> UpdateRoleAsync(int roleId, RoleUpdateDTO role) =>
            _roleRepository.UpdateRoleAsync(roleId, role, ActorUserId);

        public Task<(bool Success, string Message)> DeleteRoleAsync(int roleId) =>
            _roleRepository.DeleteRoleAsync(roleId, ActorUserId);

        public async Task<(bool Success, string Message)> SaveRolePermissionAsync(
            int roleId, RolePermissionSaveDTO permission)
        {
            if (!IsKnownModule(permission.ModuleName))
                return (false, $"'{permission.ModuleName}' is not a known module.");

            return await _roleRepository.SaveRolePermissionAsync(roleId, Canonical(permission), ActorUserId);
        }

        public async Task<(bool Success, string Message)> SaveRolePermissionsAsync(
            int roleId, IEnumerable<RolePermissionSaveDTO> permissions)
        {
            var list = permissions.ToList();

            if (list.Select(p => p.ModuleName?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != list.Count)
                return (false, "A module appears more than once.");

            var unknown = FirstUnknownModule(list.Select(p => p.ModuleName));
            if (unknown != null)
                return (false, $"'{unknown}' is not a known module.");

            var saved = 0;
            foreach (var permission in list)
            {
                var result = await _roleRepository.SaveRolePermissionAsync(roleId, Canonical(permission), ActorUserId);
                if (!result.Success)
                {
                    return (false,
                        $"{saved} of {list.Count} modules saved; '{permission.ModuleName}' failed: {result.Message}");
                }

                saved++;
            }

            return (true, $"{saved} module(s) saved.");
        }

        public Task<(bool Success, string Message)> DeleteRolePermissionAsync(int roleId, string moduleName) =>
            _roleRepository.DeleteRolePermissionAsync(
                roleId, CanonicalName(moduleName) ?? moduleName, ActorUserId);

        public IReadOnlyList<string> GetModules() => Constants.Modules.All;

        /// <summary>
        /// Rejected up front: the database will happily store a permission row for
        /// a module name nobody checks, and the grant would then look real in the
        /// UI while authorising nothing.
        /// </summary>
        private static bool IsKnownModule(string? moduleName) =>
            !string.IsNullOrWhiteSpace(moduleName)
            && Constants.Modules.All.Contains(moduleName.Trim(), StringComparer.OrdinalIgnoreCase);

        private static string? FirstUnknownModule(IEnumerable<string?> moduleNames) =>
            moduleNames.FirstOrDefault(m => !IsKnownModule(m));

        /// <summary>
        /// The spelling in Constants.Modules. The check above is case-insensitive,
        /// and so is the server's claim match, but the stored name is what the UI
        /// reads back -- and its module map is keyed exactly, so "students" would
        /// grant on the server while every screen hid the module.
        /// </summary>
        private static string? CanonicalName(string? moduleName) =>
            Constants.Modules.All.FirstOrDefault(
                m => string.Equals(m, moduleName?.Trim(), StringComparison.OrdinalIgnoreCase));

        private static RolePermissionSaveDTO Canonical(RolePermissionSaveDTO permission) => new()
        {
            ModuleName = CanonicalName(permission.ModuleName) ?? permission.ModuleName,
            CanView = permission.CanView,
            CanCreate = permission.CanCreate,
            CanEdit = permission.CanEdit,
            CanDelete = permission.CanDelete,
            IsActive = permission.IsActive
        };
    }
}
