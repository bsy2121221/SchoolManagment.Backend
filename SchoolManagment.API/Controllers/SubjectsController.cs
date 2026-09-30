using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Subjects;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Subject management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubjectsController : ControllerBase
    {
        private readonly ISubjectService _subjectService;

        public SubjectsController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        /// <summary>
        /// Create a new subject
        /// </summary>
        /// <param name="request">Subject details</param>
        /// <returns>Created subject ID</returns>
        /// <response code="201">Subject created successfully</response>
        /// <response code="400">Invalid request</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> CreateSubject([FromBody] SubjectCreateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            // The procedure's own reason, not a generic failure. The one a caller will
            // actually hit is the duplicate code -- and because codes are unique per school
            // across soft-deleted rows too, it can be a *deactivated* subject holding
            // 'MATH10', which no amount of looking at the subject list would explain.
            var (result, subjectId) = await _subjectService.CreateSubjectAsync(request);
            if (!result.Success || subjectId is null)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return CreatedAtAction(
                nameof(GetSubjectById),
                new { subjectId = subjectId.Value },
                ApiResponse<object>.SuccessResponse(
                    new { subjectId = subjectId.Value }, "Subject created successfully")
            );
        }

        /// <summary>
        /// Get all subjects with pagination and filters
        /// </summary>
        /// <param name="grade">Filter by grade (optional)</param>
        /// <param name="isActive">Filter by active status (optional)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <returns>Paginated list of subjects</returns>
        /// <response code="200">Subjects retrieved successfully</response>
        [HttpGet]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<SubjectDTO>>), 200)]
        public async Task<IActionResult> GetAllSubjects(
            [FromQuery] string? grade = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20
        )
        {
            if (pageSize > Constants.Settings.MaxPageSize)
            {
                pageSize = Constants.Settings.MaxPageSize;
            }

            var result = await _subjectService.GetAllSubjectsAsync(grade, isActive, page, pageSize);

            return Ok(ApiResponse<PaginatedResponse<SubjectDTO>>.SuccessResponse(result, "Subjects retrieved successfully"));
        }

        /// <summary>
        /// Get subject by ID
        /// </summary>
        /// <param name="subjectId">Subject ID</param>
        /// <returns>Subject details</returns>
        /// <response code="200">Subject found</response>
        /// <response code="404">Subject not found</response>
        [HttpGet("{subjectId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<SubjectDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetSubjectById(int subjectId)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null)
            {
                return NotFound(ApiResponse.FailureResult("Subject not found"));
            }

            return Ok(ApiResponse<SubjectDTO>.SuccessResponse(subject, "Subject retrieved successfully"));
        }

        /// <summary>
        /// Get subjects by grade
        /// </summary>
        /// <param name="grade">Grade (e.g., "10", "11", "12")</param>
        /// <returns>List of subjects for the specified grade</returns>
        /// <response code="200">Subjects retrieved successfully</response>
        [HttpGet("by-grade/{grade}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<List<SubjectDTO>>), 200)]
        public async Task<IActionResult> GetSubjectsByGrade(string grade)
        {
            var subjects = await _subjectService.GetSubjectsByGradeAsync(grade);

            return Ok(ApiResponse<List<SubjectDTO>>.SuccessResponse(subjects, "Subjects retrieved successfully"));
        }

        /// <summary>
        /// Update subject details
        /// </summary>
        /// <param name="subjectId">Subject ID</param>
        /// <param name="request">Updated subject details</param>
        /// <returns>Update result</returns>
        /// <response code="200">Subject updated successfully</response>
        /// <response code="400">Invalid request or update failed</response>
        [HttpPut("{subjectId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> UpdateSubject(int subjectId, [FromBody] SubjectUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _subjectService.UpdateSubjectAsync(subjectId, request);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Subject updated successfully"));
        }

        /// <summary>
        /// Update subject active status
        /// </summary>
        /// <param name="subjectId">Subject ID</param>
        /// <param name="request">Status update</param>
        /// <returns>Update result</returns>
        /// <response code="200">Status updated successfully</response>
        [HttpPut("{subjectId}/status")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        public async Task<IActionResult> UpdateSubjectStatus(int subjectId, [FromBody] SubjectStatusUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Invalid request"));
            }

            var result = await _subjectService.UpdateSubjectStatusAsync(subjectId, request.IsActive);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult($"Subject {(request.IsActive ? "activated" : "deactivated")} successfully"));
        }

        /// <summary>
        /// Delete subject
        /// </summary>
        /// <param name="subjectId">Subject ID</param>
        /// <returns>Delete result</returns>
        /// <response code="200">Subject deleted successfully</response>
        /// <response code="400">Delete failed</response>
        [HttpDelete("{subjectId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> DeleteSubject(int subjectId)
        {
            // sp_DeleteSubject refuses while the subject has active examinations, and that
            // needs the user to go and remove those first. "Failed to delete subject" told
            // them nothing they could act on.
            var result = await _subjectService.DeleteSubjectAsync(subjectId);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Subject deleted successfully"));
        }
    }
}
