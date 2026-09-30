using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Auth;

namespace SchoolManagment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITenantContext _tenantContext;

        public AuthController(IAuthService authService, ITenantContext tenantContext)
        {
            _authService = authService;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// User login with username and password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => new ErrorDetail("", e.ErrorMessage))
                        .ToList();
                    return BadRequest(ApiResponse.FailureResult(Constants.ValidationMessages.Required, errors));
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var result = await _authService.LoginAsync(request, ipAddress);

                if (result == null)
                {
                    return Unauthorized(ApiResponse.FailureResult(Constants.ValidationMessages.InvalidCredentials));
                }

                return Ok(ApiResponse<LoginResponseDTO>.SuccessResponse(result, "Login successful"));
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
                // For simplicity, we are just returning a generic error message here.
                return StatusCode(500, ApiResponse.FailureResult("An unexpected error occurred. Please try again later."));
            }
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Refresh token is required"));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress);

            if (result == null)
            {
                return Unauthorized(ApiResponse.FailureResult(Constants.ValidationMessages.InvalidToken));
            }

            return Ok(ApiResponse<LoginResponseDTO>.SuccessResponse(result, "Token refreshed successfully"));
        }

        /// <summary>
        /// Logout user by revoking refresh token
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Refresh token is required"));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await _authService.LogoutAsync(request.RefreshToken, ipAddress);

            if (!result)
            {
                return BadRequest(ApiResponse.FailureResult("Logout failed"));
            }

            return Ok(ApiResponse.SuccessResult("Logout successful"));
        }

        /// <summary>
        /// Change password for logged-in user
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var userId = _tenantContext.UserId;
            if (!userId.HasValue)
            {
                return Unauthorized(ApiResponse.FailureResult(Constants.ValidationMessages.Unauthorized));
            }

            var result = await _authService.ChangePasswordAsync(userId.Value, request);

            if (!result)
            {
                return BadRequest(ApiResponse.FailureResult("Password change failed. Please check your current password."));
            }

            return Ok(ApiResponse.SuccessResult("Password changed successfully"));
        }

        /// <summary>
        /// Reset password for a user (Admin/SuperAdmin only)
        /// </summary>
        [HttpPost("reset-password")]
        [Authorize(Roles = $"{Constants.Roles.Admin},{Constants.Roles.SuperAdmin}")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var performedByUserId = _tenantContext.UserId;
            if (!performedByUserId.HasValue)
            {
                return Unauthorized(ApiResponse.FailureResult(Constants.ValidationMessages.Unauthorized));
            }

            // A SuperAdmin resets across the platform, so their scope is null. An
            // admin's own school is passed instead, which is what confines the
            // reset -- without it, one school's admin could reset another's.
            var schoolId = _tenantContext.IsSuperAdmin ? null : _tenantContext.SchoolId;

            var (success, message) = await _authService.ResetPasswordAsync(
                request, performedByUserId.Value, schoolId);

            if (!success)
            {
                return BadRequest(ApiResponse.FailureResult(message));
            }

            return Ok(ApiResponse.SuccessResult("Password reset successfully"));
        }

        /// <summary>
        /// Get current user info from token, including the permission grid the
        /// token carries -- so a client can decide what to show without asking
        /// again.
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userInfo = new
            {
                UserId = _tenantContext.UserId,
                Username = _tenantContext.Username,
                Role = _tenantContext.Role,
                RoleId = _tenantContext.RoleId,
                SchoolId = _tenantContext.SchoolId,
                SchoolCode = _tenantContext.SchoolCode,
                IsSuperAdmin = _tenantContext.IsSuperAdmin,
                Permissions = _tenantContext.Permissions.Select(p => new
                {
                    p.ModuleName,
                    p.CanView,
                    p.CanCreate,
                    p.CanEdit,
                    p.CanDelete
                })
            };

            return Ok(ApiResponse<object>.SuccessResponse(userInfo, "User information retrieved"));
        }
    }
}
