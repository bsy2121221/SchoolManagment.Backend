using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Users;

namespace SchoolManagment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get all users in the school. Requires view access to the Users module.
        ///
        /// Filter by role either as a name or code ("Teacher", "TEACHER") through
        /// role, or by the exact dbo.Roles.Id through roleId. Prefer roleId: a role
        /// can be renamed, its id cannot.
        /// </summary>
        [HttpGet]
        [RequiresPermission(Constants.Modules.Users, Constants.PermissionActions.View)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? role = null,
            [FromQuery] int? roleId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            if (pageSize > Constants.Settings.MaxPageSize)
            {
                pageSize = Constants.Settings.MaxPageSize;
            }

            var result = await _userService.GetAllUsersAsync(role, roleId, isActive, searchTerm, page, pageSize);

            return Ok(ApiResponse<PaginatedResponse<UserDTO>>.SuccessResponse(result, "Users retrieved successfully"));
        }

        /// <summary>
        /// Get user by ID (Admin or Self)
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(int userId)
        {
            var canAccess = await _userService.CanAccessUser(userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse.FailureResult("User not found"));
            }

            return Ok(ApiResponse<UserDTO>.SuccessResponse(user, "User retrieved successfully"));
        }

        /// <summary>
        /// Update user profile (Admin or Self - limited fields)
        /// </summary>
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var canAccess = await _userService.CanAccessUser(userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var result = await _userService.UpdateUserAsync(userId, request);
            if (!result.Success)
            {
                // "Email already exists in this school" and "User not found in this
                // school" are different problems with different fixes; both used to
                // arrive as "Failed to update user".
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("User updated successfully"));
        }

        /// <summary>
        /// Update user active status. Requires edit access to the Users module.
        ///
        /// Sending isActive = true is the only way to reactivate an account anywhere in
        /// the API: DELETE on Students, Teachers and Parents all deactivate and none of
        /// those modules offers a way back. It restores the login, the person and the
        /// role-specific row, but not the subject enrolments or parent links the delete
        /// switched off -- those are re-established through their own modules.
        /// </summary>
        [HttpPut("{userId}/status")]
        [RequiresPermission(Constants.Modules.Users, Constants.PermissionActions.Edit)]
        public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] UserStatusUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Invalid request"));
            }

            var result = await _userService.UpdateUserStatusAsync(userId, request.IsActive);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult($"User {(request.IsActive ? "activated" : "deactivated")} successfully"));
        }

        /// <summary>
        /// Change a user's role. Requires edit access to the Users module.
        ///
        /// Refused for students, teachers and parents: their role follows from the
        /// Students / Teachers / Parents row that owns them, so moving the login
        /// alone would leave the two disagreeing.
        /// </summary>
        [HttpPut("{userId}/role")]
        [RequiresPermission(Constants.Modules.Users, Constants.PermissionActions.Edit)]
        public async Task<IActionResult> ChangeUserRole(int userId, [FromBody] UserRoleUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Invalid request"));
            }

            var result = await _userService.ChangeUserRoleAsync(userId, request.RoleId);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("User role updated successfully"));
        }

        /// <summary>
        /// Delete user. Requires delete access to the Users module.
        ///
        /// A soft delete that cascades into whichever role-specific rows the account
        /// owns, and the most talkative procedure in the database: it refuses for admins,
        /// for a teacher who still has an active class or any attendance history, and for
        /// a student with attendance, results or fees. Eight distinct refusals with eight
        /// different remedies, so the message is returned rather than summarised.
        /// </summary>
        [HttpDelete("{userId}")]
        [RequiresPermission(Constants.Modules.Users, Constants.PermissionActions.Delete)]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("User deleted successfully"));
        }

        /// <summary>
        /// Get user profile picture (Authenticated users)
        /// </summary>
        [HttpGet("{userId}/profile-picture")]
        public async Task<IActionResult> GetProfilePicture(int userId)
        {
            var picture = await _userService.GetProfilePictureAsync(userId);
            if (picture == null || picture.Data == null || picture.Data.Length == 0)
            {
                return NotFound(ApiResponse.FailureResult("Profile picture not found"));
            }

            return File(picture.Data, picture.ContentType, picture.FileName);
        }

        /// <summary>
        /// Upload user profile picture (Admin or Self)
        /// </summary>
        [HttpPost("{userId}/profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(int userId, IFormFile file)
        {
            var canAccess = await _userService.CanAccessUser(userId);
            if (!canAccess)
            {
                return Forbid();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse.FailureResult("No file provided"));
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(ApiResponse.FailureResult("Only image files (JPEG, PNG, GIF) are allowed"));
            }

            // Validate file size (max 5MB)
            const int maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest(ApiResponse.FailureResult("File size cannot exceed 5MB"));
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var pictureData = memoryStream.ToArray();

            var result = await _userService.UpdateProfilePictureAsync(
                userId,
                pictureData,
                file.FileName,
                file.ContentType
            );

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Profile picture uploaded successfully"));
        }

        /// <summary>
        /// Delete user profile picture (Admin or Self)
        /// </summary>
        [HttpDelete("{userId}/profile-picture")]
        public async Task<IActionResult> DeleteProfilePicture(int userId)
        {
            var canAccess = await _userService.CanAccessUser(userId);
            if (!canAccess)
            {
                return Forbid();
            }

            var result = await _userService.DeleteProfilePictureAsync(userId);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Profile picture deleted successfully"));
        }
    }
}
