using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Roles;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Roles and their permission grids. Roles are global -- there is no school in
    /// any of these routes, because one set of roles serves the whole platform.
    ///
    /// Reads are guarded by the Roles module's own permissions -- the seeded
    /// Admin holds Roles:View, which the Users screen's role-change dialog needs.
    ///
    /// Every write is SuperAdminOnly as well (PHASE 14). The seeded grid gives
    /// the school Admin Roles Create and Edit, and because roles are global that
    /// let one school's admin rewrite the Teacher grid of every school, or grant
    /// its own role Roles:Delete and remove another school's custom role. The
    /// permission attributes stay, so the grid can still narrow a SuperAdmin
    /// -- except that PermissionAuthorizationHandler short-circuits role 1.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize()]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        private BadRequestObjectResult ValidationFailure()
        {
            var errors = ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .SelectMany(entry => entry.Value!.Errors.Select(
                    e => new ErrorDetail(entry.Key, e.ErrorMessage)))
                .ToList();
            return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
        }

        /// <summary>
        /// The procedures report every refusal as 'Error: ...'. "not found" is a
        /// 404, a name or code already taken and a role still held are 409s, and
        /// the rest -- system-role and SuperAdmin-grid refusals -- stay 400.
        /// </summary>
        private ObjectResult WriteFailure(string message)
        {
            var body = ApiResponse.FailureResult(message);
            if (message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || message.Contains("No permission row", StringComparison.OrdinalIgnoreCase))
                return NotFound(body);
            if (message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                || message.Contains("still hold", StringComparison.OrdinalIgnoreCase))
                return Conflict(body);
            return BadRequest(body);
        }

        /// <summary>
        /// The role list, each with how many users hold it and how many modules it
        /// grants.
        /// </summary>
        [HttpGet]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.View)]
        public async Task<IActionResult> GetRoles([FromQuery] bool includeInactive = false)
        {
            var roles = await _roleService.GetRolesAsync(includeInactive);
            return Ok(ApiResponse<List<RoleDTO>>.SuccessResponse(roles, "Roles retrieved successfully"));
        }

        /// <summary>One role with its full permission grid.</summary>
        [HttpGet("{roleId}")]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.View)]
        public async Task<IActionResult> GetRoleById(int roleId)
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role == null)
            {
                return NotFound(ApiResponse.FailureResult("Role not found"));
            }

            return Ok(ApiResponse<RoleDTO>.SuccessResponse(role, "Role retrieved successfully"));
        }

        /// <summary>
        /// The module names a permission can be granted on. Served from
        /// Constants.Modules so a UI does not have to hard-code the list.
        /// </summary>
        [HttpGet("modules")]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.View)]
        public IActionResult GetModules()
        {
            var modules = _roleService.GetModules();
            return Ok(ApiResponse<IReadOnlyList<string>>.SuccessResponse(modules, "Modules retrieved successfully"));
        }

        /// <summary>
        /// The signed-in user's own grid, read live rather than from their token.
        /// Deliberately needs no Roles permission: everybody may ask what they
        /// themselves can do.
        /// </summary>
        [HttpGet("my-permissions")]
        public async Task<IActionResult> GetMyPermissions()
        {
            var permissions = await _roleService.GetMyPermissionsAsync();
            return Ok(ApiResponse<List<RolePermissionDTO>>.SuccessResponse(
                permissions, "Permissions retrieved successfully"));
        }

        /// <summary>One role's permission grid.</summary>
        [HttpGet("{roleId}/permissions")]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.View)]
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            var permissions = await _roleService.GetRolePermissionsAsync(roleId);
            return Ok(ApiResponse<List<RolePermissionDTO>>.SuccessResponse(
                permissions, "Permissions retrieved successfully"));
        }

        /// <summary>
        /// Create a custom role. Its id is allocated from 100 upward -- 1 to 5 are
        /// reserved for the system roles the database refers to by number.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.Create)]
        public async Task<IActionResult> CreateRole([FromBody] RoleCreateDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var (success, message, roleId) = await _roleService.CreateRoleAsync(request);
            if (!success && roleId != null)
            {
                // Created, but the starting grid stopped part-way. The id rides
                // along so a caller can open the role rather than retry the name,
                // which would now be a 409.
                return BadRequest(new ApiResponse<object>
                {
                    Success = false, Message = message, Data = new { RoleId = roleId }
                });
            }
            if (!success)
            {
                return WriteFailure(message);
            }

            return Ok(ApiResponse<object>.SuccessResponse(new { RoleId = roleId }, "Role created successfully"));
        }

        /// <summary>
        /// Rename or deactivate a role. The five system roles cannot be renamed;
        /// the database refuses it.
        /// </summary>
        [HttpPut("{roleId}")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.Edit)]
        public async Task<IActionResult> UpdateRole(int roleId, [FromBody] RoleUpdateDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var (success, message) = await _roleService.UpdateRoleAsync(roleId, request);
            if (!success)
            {
                return WriteFailure(message);
            }

            return Ok(ApiResponse.SuccessResult("Role updated successfully"));
        }

        /// <summary>
        /// Delete a custom role. Refused while any user still holds it, and
        /// refused outright for the system roles.
        /// </summary>
        [HttpDelete("{roleId}")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.Delete)]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            var (success, message) = await _roleService.DeleteRoleAsync(roleId);
            if (!success)
            {
                return WriteFailure(message);
            }

            return Ok(ApiResponse.SuccessResult("Role deleted successfully"));
        }

        /// <summary>
        /// Grant or revoke one module for one role. Granting create, edit or delete
        /// implies view -- the database will not store the contradiction.
        /// </summary>
        [HttpPut("{roleId}/permissions")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.Edit)]
        public async Task<IActionResult> SaveRolePermission(
            int roleId, [FromBody] RolePermissionSaveDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var (success, message) = await _roleService.SaveRolePermissionAsync(roleId, request);
            if (!success)
            {
                return WriteFailure(message);
            }

            return Ok(ApiResponse.SuccessResult("Permission saved successfully"));
        }

        /// <summary>
        /// Save several modules at once -- what a permissions screen sends when the
        /// user hits save. Stops at the first refusal and says how many were
        /// applied, because the writes are not one transaction.
        /// </summary>
        [HttpPut("{roleId}/permissions/bulk")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.Edit)]
        public async Task<IActionResult> SaveRolePermissions(
            int roleId, [FromBody] List<RolePermissionSaveDTO> request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            if (request == null || request.Count == 0)
            {
                return BadRequest(ApiResponse.FailureResult("No permissions supplied"));
            }

            var (success, message) = await _roleService.SaveRolePermissionsAsync(roleId, request);
            if (!success)
            {
                return WriteFailure(message);
            }

            return Ok(ApiResponse.SuccessResult(message));
        }

        /// <summary>
        /// Remove a module from a role entirely. Distinct from saving it with all
        /// four flags off: both deny, but only one records that somebody meant to.
        /// </summary>
        [HttpDelete("{roleId}/permissions/{moduleName}")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Roles, Constants.PermissionActions.Edit)]
        public async Task<IActionResult> DeleteRolePermission(int roleId, string moduleName)
        {
            var (success, message) = await _roleService.DeleteRolePermissionAsync(roleId, moduleName);
            if (!success)
            {
                return WriteFailure(message);
            }

            return Ok(ApiResponse.SuccessResult("Permission removed successfully"));
        }
    }
}
