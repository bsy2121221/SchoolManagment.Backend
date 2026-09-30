using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Auth;
using SchoolManagment.Models.DTOs.Schools;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Tenant administration -- the platform side of the system, and the only
    /// controller here that addresses a school by id instead of taking it from the
    /// caller's token.
    ///
    /// That makes authorisation two-layered, and both layers matter:
    ///
    ///   * The Schools module's permission grid decides whether the caller may read
    ///     or write schools at all. The seed gives an Admin view and edit but not
    ///     create or delete, which is exactly "may see and maintain my own school".
    ///   * Cross-tenant actions -- listing every school, platform statistics, the
    ///     usage report, suspending a tenant, switching into one -- additionally
    ///     require the SuperAdmin role, because Schools:View alone is held by every
    ///     school admin and would otherwise expose the whole tenant list to them.
    ///
    /// Actions that name a school and are reachable by a school admin are confined
    /// by SchoolService, which refuses any id but the caller's own. Both checks are
    /// kept: the role guard here gives the right status code, the service guard is
    /// the one that actually holds.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SchoolsController : ControllerBase
    {
        private readonly ISchoolService _schoolService;
        private readonly IAuthService _authService;
        private readonly ITenantContext _tenantContext;

        public SchoolsController(
            ISchoolService schoolService,
            IAuthService authService,
            ITenantContext tenantContext)
        {
            _schoolService = schoolService;
            _authService = authService;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Every tenant, with its headline counts. Platform administrators only.
        /// </summary>
        /// <param name="searchTerm">Matches school name, code or city</param>
        /// <param name="isActive">Filter by active status (optional)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        [HttpGet]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<SchoolListItemDTO>>), 200)]
        public async Task<IActionResult> GetSchools(
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = Constants.Settings.DefaultPageSize)
        {
            if (pageSize > Constants.Settings.MaxPageSize)
            {
                pageSize = Constants.Settings.MaxPageSize;
            }

            var result = await _schoolService.GetSchoolsAsync(searchTerm, isActive, page, pageSize);

            return Ok(ApiResponse<PaginatedResponse<SchoolListItemDTO>>.SuccessResponse(
                result, "Schools retrieved successfully"));
        }

        /// <summary>
        /// The caller's own school. This is what a school admin's settings screen
        /// reads; it needs no id, so it cannot be pointed at another tenant.
        ///
        /// 404 for a platform administrator who is not currently acting inside a
        /// school -- they have no "own school" to return.
        /// </summary>
        [HttpGet("current")]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<SchoolDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetCurrentSchool()
        {
            var school = await _schoolService.GetCurrentSchoolAsync();
            if (school == null)
            {
                return NotFound(ApiResponse.FailureResult(
                    "No school is in scope for this session"));
            }

            return Ok(ApiResponse<SchoolDTO>.SuccessResponse(school, "School retrieved successfully"));
        }

        /// <summary>Totals across every tenant. Platform administrators only.</summary>
        [HttpGet("platform-stats")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<PlatformStatsDTO>), 200)]
        public async Task<IActionResult> GetPlatformStats()
        {
            var stats = await _schoolService.GetPlatformStatsAsync();
            if (stats == null)
            {
                return NotFound(ApiResponse.FailureResult("Platform statistics are unavailable"));
            }

            return Ok(ApiResponse<PlatformStatsDTO>.SuccessResponse(
                stats, "Platform statistics retrieved successfully"));
        }

        /// <summary>
        /// Per-tenant activity over a window, for billing or capacity work. Defaults
        /// to the last 30 days. Platform administrators only.
        /// </summary>
        [HttpGet("usage-report")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<SchoolUsageReportDTO>>), 200)]
        public async Task<IActionResult> GetUsageReport(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                return BadRequest(ApiResponse.FailureResult("fromDate cannot be after toDate"));
            }

            var report = await _schoolService.GetSchoolUsageReportAsync(fromDate, toDate);

            return Ok(ApiResponse<List<SchoolUsageReportDTO>>.SuccessResponse(
                report, "Usage report retrieved successfully"));
        }

        /// <summary>
        /// Public branding for one school, by id or by code. Anonymous on purpose:
        /// the React app calls this to theme the login page before anybody has
        /// signed in, so it returns only name, logo, colour and whether the school
        /// is suspended -- never contact details or counts.
        /// </summary>
        [HttpGet("branding")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SchoolBrandingDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetBranding(
            [FromQuery] int? schoolId = null,
            [FromQuery] string? schoolCode = null)
        {
            if (!schoolId.HasValue && string.IsNullOrWhiteSpace(schoolCode))
            {
                return BadRequest(ApiResponse.FailureResult("Supply either schoolId or schoolCode"));
            }

            var branding = await _schoolService.GetBrandingAsync(schoolId, schoolCode);
            if (branding == null)
            {
                return NotFound(ApiResponse.FailureResult("School not found"));
            }

            return Ok(ApiResponse<SchoolBrandingDTO>.SuccessResponse(
                branding, "Branding retrieved successfully"));
        }

        /// <summary>
        /// Resolves a wildcard host's leading label to a tenant, returning the same
        /// public branding subset. Anonymous, for the same reason.
        /// </summary>
        [HttpGet("by-subdomain/{subdomain}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SchoolBrandingDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetBySubdomain(string subdomain)
        {
            var branding = await _schoolService.GetBySubdomainAsync(subdomain);
            if (branding == null)
            {
                return NotFound(ApiResponse.FailureResult("School not found"));
            }

            return Ok(ApiResponse<SchoolBrandingDTO>.SuccessResponse(
                branding, "School retrieved successfully"));
        }

        /// <summary>
        /// One tenant by code. Platform administrators only -- unlike the branding
        /// endpoints, this returns the full record.
        /// </summary>
        [HttpGet("by-code/{schoolCode}")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<SchoolDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetSchoolByCode(string schoolCode)
        {
            var school = await _schoolService.GetSchoolByCodeAsync(schoolCode);
            if (school == null)
            {
                return NotFound(ApiResponse.FailureResult("School not found"));
            }

            return Ok(ApiResponse<SchoolDTO>.SuccessResponse(school, "School retrieved successfully"));
        }

        /// <summary>
        /// One tenant by id. A school admin may read only their own; any other id is
        /// 404 rather than 403, so probing ids reveals nothing about which other
        /// tenants exist.
        /// </summary>
        [HttpGet("{schoolId:int}")]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<SchoolDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetSchoolById(int schoolId)
        {
            var school = await _schoolService.GetSchoolByIdAsync(schoolId);
            if (school == null)
            {
                return NotFound(ApiResponse.FailureResult("School not found"));
            }

            return Ok(ApiResponse<SchoolDTO>.SuccessResponse(school, "School retrieved successfully"));
        }

        /// <summary>
        /// Onboards a tenant. One transaction creates the school, its first admin,
        /// the number sequences, the default fee types, the default settings and --
        /// unless turned off -- a starter subject list for grades 1-12.
        ///
        /// The admin's username is generated as CODE_ADMIN and returned, because the
        /// caller has no other way to learn it. Omit adminPassword and the account
        /// gets the shared temporary password and must change it at first login,
        /// which is the normal path.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<SchoolCreateResultDTO>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> CreateSchool([FromBody] SchoolCreateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Validation failed", CollectErrors()));
            }

            var (success, message, result) = await _schoolService.CreateSchoolAsync(request);
            if (!success || result == null)
            {
                return BadRequest(ApiResponse.FailureResult(message));
            }

            return CreatedAtAction(
                nameof(GetSchoolById),
                new { schoolId = result.SchoolId },
                ApiResponse<SchoolCreateResultDTO>.SuccessResponse(result, "School created successfully")
            );
        }

        /// <summary>
        /// Partial update -- omitted fields keep their stored value. A school admin
        /// may update only their own school; SchoolCode cannot be changed at all,
        /// because it is embedded in every username and admission number already
        /// issued.
        /// </summary>
        [HttpPut("{schoolId:int}")]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.Edit)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> UpdateSchool(int schoolId, [FromBody] SchoolUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Validation failed", CollectErrors()));
            }

            // The service refuses this too; asking first is only what turns the
            // refusal into a 404 instead of a 400.
            if (!_schoolService.CanAccessSchool(schoolId))
            {
                return NotFound(ApiResponse.FailureResult("School not found"));
            }

            var (success, message) = await _schoolService.UpdateSchoolAsync(schoolId, request);
            if (!success)
            {
                return BadRequest(ApiResponse.FailureResult(message));
            }

            return Ok(ApiResponse.SuccessResult("School updated successfully"));
        }

        /// <summary>
        /// Suspends or restores a tenant. Platform administrators only -- deliberately
        /// not available to a school's own admin, who would be locking every one of
        /// their users out at once.
        ///
        /// Suspending also revokes the school's refresh tokens, so its staff are
        /// signed out rather than lingering on valid tokens.
        /// </summary>
        [HttpPut("{schoolId:int}/status")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.Edit)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> UpdateSchoolStatus(
            int schoolId, [FromBody] SchoolStatusUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Invalid request"));
            }

            var (success, message, isActive) = await _schoolService.ToggleSchoolStatusAsync(
                schoolId, request.IsActive);

            if (!success)
            {
                return BadRequest(ApiResponse.FailureResult(message));
            }

            return Ok(ApiResponse.SuccessResult(
                $"School {(isActive == true ? "activated" : "suspended")} successfully"));
        }

        /// <summary>
        /// Adds a further admin to an existing school -- the first one comes with the
        /// school itself. The username is generated as CODE_ADMIN2, CODE_ADMIN3 and
        /// so on unless one is supplied, and is returned either way.
        ///
        /// Omit the password and the account gets the shared temporary password with
        /// a forced change at first login.
        /// </summary>
        [HttpPost("{schoolId:int}/admins")]
        [RequiresPermission(Constants.Modules.Schools, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<SchoolAdminCreateResultDTO>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> CreateSchoolAdmin(
            int schoolId, [FromBody] SchoolAdminCreateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Validation failed", CollectErrors()));
            }

            if (!_schoolService.CanAccessSchool(schoolId))
            {
                return NotFound(ApiResponse.FailureResult("School not found"));
            }

            var (success, message, result) = await _schoolService.CreateSchoolAdminAsync(schoolId, request);
            if (!success || result == null)
            {
                return BadRequest(ApiResponse.FailureResult(message));
            }

            return StatusCode(201, ApiResponse<SchoolAdminCreateResultDTO>.SuccessResponse(
                result, "Administrator created successfully"));
        }

        /// <summary>
        /// Enters a school as a platform administrator: reissues the access token
        /// with that tenant's school_id on it, so anything reading the school from
        /// the token -- ITenantContext, and therefore every repository below it --
        /// sees the school instead of nothing.
        ///
        /// Two limits, both real:
        ///
        ///   * Only the access token is replaced. The refresh token is untouched and
        ///     knows nothing of the switch, so the scope lasts until the access
        ///     token expires; a client that wants to stay inside the school must
        ///     call this again after refreshing.
        ///   * Scope is not the same as access. The other modules guard their
        ///     actions with [Authorize(Roles = "Admin")] and the AdminOrTeacher /
        ///     AllSchoolUsers policies, none of which name SuperAdmin -- so a
        ///     switched platform administrator still gets 403 from Students,
        ///     Classes, Teachers and the rest. What the switch currently buys is a
        ///     correctly scoped token for the endpoints SuperAdmin may already
        ///     reach. Letting a platform administrator work inside a tenant's
        ///     modules means adding SuperAdmin to those role guards, which is a
        ///     change to the authorisation of eleven other controllers and is not
        ///     made here.
        /// </summary>
        [HttpPost("{schoolId:int}/switch")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<SchoolSessionDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public Task<IActionResult> SwitchSchool(int schoolId) => Switch(schoolId);

        /// <summary>
        /// Leaves the school again and returns to platform scope, by reissuing an
        /// access token with no school on it.
        /// </summary>
        [HttpPost("exit-switch")]
        [Authorize(Policy = Constants.AuthPolicies.SuperAdminOnly)]
        [ProducesResponseType(typeof(ApiResponse<SchoolSessionDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public Task<IActionResult> ExitSwitch() => Switch(null);

        private async Task<IActionResult> Switch(int? schoolId)
        {
            if (!_tenantContext.UserId.HasValue)
            {
                return Unauthorized(ApiResponse.FailureResult(Constants.ValidationMessages.InvalidToken));
            }

            var (success, message, session) = await _authService.SwitchSchoolAsync(
                _tenantContext.UserId.Value, schoolId);

            if (!success || session == null)
            {
                return BadRequest(ApiResponse.FailureResult(message));
            }

            return Ok(ApiResponse<SchoolSessionDTO>.SuccessResponse(session, message));
        }

        private List<ErrorDetail> CollectErrors() =>
            ModelState
                .SelectMany(entry => entry.Value!.Errors.Select(
                    error => new ErrorDetail(entry.Key, error.ErrorMessage)))
                .ToList();
    }
}
