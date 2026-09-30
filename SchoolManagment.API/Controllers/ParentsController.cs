using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Parents;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Parent management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ParentsController : ControllerBase
    {
        private readonly IParentService _parentService;

        public ParentsController(IParentService parentService)
        {
            _parentService = parentService;
        }

        /// <summary>
        /// Register a new parent
        /// </summary>
        /// <param name="request">Parent registration details</param>
        /// <returns>Registration result with credentials</returns>
        /// <response code="201">Parent registered successfully</response>
        /// <response code="400">Invalid request or registration failed</response>
        [HttpPost]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<ParentRegistrationResponseDTO>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> RegisterParent([FromBody] ParentRegistrationDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            // The procedure names its own refusal -- a duplicate email, a username collision,
            // a deactivated school -- and each is something the admin can act on, so the
            // message is the response.
            var (result, parent) = await _parentService.RegisterParentAsync(request);

            if (!result.Success || parent == null)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return CreatedAtAction(
                nameof(GetParentById),
                new { parentId = parent.ParentId },
                ApiResponse<ParentRegistrationResponseDTO>.SuccessResponse(parent, "Parent registered successfully")
            );
        }

        /// <summary>
        /// Get all parents
        /// </summary>
        /// <param name="includeInactive">Include deactivated parents (optional)</param>
        /// <returns>List of parents</returns>
        /// <response code="200">Parents retrieved successfully</response>
        [HttpGet]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<List<ParentDTO>>), 200)]
        public async Task<IActionResult> GetAllParents([FromQuery] bool includeInactive = false)
        {
            var parents = await _parentService.GetAllParentsAsync(includeInactive);

            return Ok(ApiResponse<List<ParentDTO>>.SuccessResponse(
                parents,
                "Parents retrieved successfully"
            ));
        }

        /// <summary>
        /// Get parent by ID
        /// </summary>
        /// <param name="parentId">Parent ID</param>
        /// <returns>Parent details</returns>
        /// <response code="200">Parent found</response>
        /// <response code="404">Parent not found</response>
        [HttpGet("{parentId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<ParentDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetParentById(int parentId)
        {
            var parent = await _parentService.GetParentByIdAsync(parentId);

            if (parent == null)
            {
                return NotFound(ApiResponse.FailureResult("Parent not found"));
            }

            return Ok(ApiResponse<ParentDTO>.SuccessResponse(
                parent,
                "Parent retrieved successfully"
            ));
        }

        /// <summary>
        /// Get parent profile with children
        /// </summary>
        /// <param name="userId">User ID of the parent</param>
        /// <returns>Parent profile with children details</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">Parent not found</response>
        // Was a bare [Authorize], which let any signed-in account in the school read any
        // parent's phone number and home address -- including a student's. Narrowed to match
        // the teacher equivalent; a parent reads their own record through my-profile below,
        // which is where they are actually sent.
        [HttpGet("profile/{userId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<ParentProfileDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetParentProfile(int userId)
        {
            var profile = await _parentService.GetParentProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(ApiResponse.FailureResult("Parent profile not found"));
            }

            return Ok(ApiResponse<ParentProfileDTO>.SuccessResponse(
                profile,
                "Parent profile retrieved successfully"
            ));
        }

        /// <summary>
        /// Get current parent's own profile
        /// </summary>
        /// <returns>Parent profile with children</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">Parent not found</response>
        [HttpGet("my-profile")]
        [Authorize(Roles = Constants.Roles.Parent)]
        [ProducesResponseType(typeof(ApiResponse<ParentProfileDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var profile = await _parentService.GetParentProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(ApiResponse.FailureResult("Parent profile not found"));
            }

            return Ok(ApiResponse<ParentProfileDTO>.SuccessResponse(
                profile,
                "Profile retrieved successfully"
            ));
        }

        /// <summary>
        /// Update parent details
        /// </summary>
        /// <param name="parentId">Parent ID</param>
        /// <param name="request">Updated parent details</param>
        /// <returns>Update result</returns>
        /// <response code="200">Parent updated successfully</response>
        /// <response code="400">Invalid request or update failed</response>
        [HttpPut("{parentId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> UpdateParent(int parentId, [FromBody] ParentUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            // "Parent not found in this school" and "Email already exists" have different
            // remedies, so the procedure's sentence is the response rather than a generic
            // "Failed to update parent".
            var result = await _parentService.UpdateParentAsync(parentId, request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Parent updated successfully"));
        }

        /// <summary>
        /// Delete parent (soft delete)
        /// </summary>
        /// <param name="parentId">Parent ID</param>
        /// <returns>Delete result</returns>
        /// <response code="200">Parent deleted successfully</response>
        /// <response code="400">Delete failed</response>
        [HttpDelete("{parentId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> DeleteParent(int parentId)
        {
            // A refusal here is usually "has recorded fee payments", which the admin can
            // only act on if they are told.
            var result = await _parentService.DeleteParentAsync(parentId);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Parent deleted successfully"));
        }

        /// <summary>
        /// Link a parent to a student
        /// </summary>
        /// <param name="request">Link details (studentId, parentId, relationship)</param>
        /// <returns>Link result</returns>
        /// <response code="200">Parent linked to student successfully</response>
        /// <response code="400">Invalid request or linking failed</response>
        [HttpPost("link")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> LinkStudentParent([FromBody] LinkStudentParentDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _parentService.LinkStudentParentAsync(request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Parent linked to student successfully"));
        }

        /// <summary>
        /// Unlink a parent from a student
        /// </summary>
        /// <param name="request">Unlink details (studentId, parentId)</param>
        /// <returns>Unlink result</returns>
        /// <response code="200">Parent unlinked from student successfully</response>
        /// <response code="400">Unlinking failed</response>
        [HttpPost("unlink")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> UnlinkStudentParent([FromBody] UnlinkStudentParentDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _parentService.UnlinkStudentParentAsync(request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Parent unlinked from student successfully"));
        }

        /// <summary>
        /// Get all parents for a specific student
        /// </summary>
        /// <param name="studentId">Student ID</param>
        /// <returns>List of parents</returns>
        /// <response code="200">Parents retrieved successfully</response>
        [HttpGet("student/{studentId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<List<ParentDTO>>), 200)]
        public async Task<IActionResult> GetParentsByStudent(int studentId)
        {
            var parents = await _parentService.GetParentsByStudentAsync(studentId);

            return Ok(ApiResponse<List<ParentDTO>>.SuccessResponse(
                parents,
                "Parents retrieved successfully"
            ));
        }

        /// <summary>
        /// Get all children for a specific parent
        /// </summary>
        /// <param name="parentId">Parent ID</param>
        /// <returns>List of children</returns>
        /// <response code="200">Children retrieved successfully</response>
        // Also narrowed from a bare [Authorize]: this is keyed on Parents.Id, so a signed-in
        // parent could walk the ids and read other families' children. A parent's own
        // children arrive with my-profile.
        [HttpGet("{parentId}/children")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<List<ParentChildDTO>>), 200)]
        public async Task<IActionResult> GetChildrenByParent(int parentId)
        {
            var children = await _parentService.GetChildrenByParentAsync(parentId);

            return Ok(ApiResponse<List<ParentChildDTO>>.SuccessResponse(
                children,
                "Children retrieved successfully"
            ));
        }
    }
}
