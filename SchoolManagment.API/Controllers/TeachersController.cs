using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Teachers;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Teacher management endpoints
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeachersController : ControllerBase
    {
        private readonly ITeacherService _teacherService;

        public TeachersController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        /// <summary>
        /// Register a new teacher
        /// </summary>
        /// <param name="request">Teacher registration details</param>
        /// <returns>Registration result with credentials</returns>
        /// <response code="201">Teacher registered successfully</response>
        /// <response code="400">Invalid request or registration failed</response>
        [HttpPost]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<TeacherRegistrationResponseDTO>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> RegisterTeacher([FromBody] TeacherRegistrationDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            // The procedure names its own refusal -- a duplicate email, a username
            // collision -- and each is something the admin can act on, so the message is
            // the response.
            var (result, teacher) = await _teacherService.RegisterTeacherAsync(request);

            if (!result.Success || teacher == null)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return CreatedAtAction(
                nameof(GetTeacherProfile),
                new { userId = teacher.UserId },
                ApiResponse<TeacherRegistrationResponseDTO>.SuccessResponse(teacher, "Teacher registered successfully")
            );
        }

        /// <summary>
        /// Get all teachers with optional subject filter
        /// </summary>
        /// <param name="subjectId">Filter by subject ID (optional)</param>
        /// <returns>List of teachers</returns>
        /// <response code="200">Teachers retrieved successfully</response>
        [HttpGet]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<List<TeacherDTO>>), 200)]
        public async Task<IActionResult> GetAllTeachers([FromQuery] int? subjectId = null)
        {
            var teachers = await _teacherService.GetAllTeachersAsync(subjectId);

            return Ok(ApiResponse<List<TeacherDTO>>.SuccessResponse(teachers, "Teachers retrieved successfully"));
        }

        /// <summary>
        /// Get teacher profile with statistics
        /// </summary>
        /// <param name="userId">User ID of the teacher</param>
        /// <returns>Teacher profile with stats</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">Teacher not found</response>
        [HttpGet("profile/{userId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<TeacherProfileDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetTeacherProfile(int userId)
        {
            var profile = await _teacherService.GetTeacherProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(ApiResponse.FailureResult("Teacher profile not found"));
            }

            return Ok(ApiResponse<TeacherProfileDTO>.SuccessResponse(profile, "Teacher profile retrieved successfully"));
        }

        /// <summary>
        /// Get current teacher's own profile
        /// </summary>
        /// <returns>Teacher profile with stats</returns>
        /// <response code="200">Profile retrieved successfully</response>
        /// <response code="404">Teacher not found</response>
        [HttpGet("my-profile")]
        [Authorize(Roles = Constants.Roles.Teacher)]
        [ProducesResponseType(typeof(ApiResponse<TeacherProfileDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var profile = await _teacherService.GetTeacherProfileAsync(userId);

            if (profile == null)
            {
                return NotFound(ApiResponse.FailureResult("Teacher profile not found"));
            }

            return Ok(ApiResponse<TeacherProfileDTO>.SuccessResponse(profile, "Profile retrieved successfully"));
        }

        /// <summary>
        /// Update teacher details (Admin)
        /// </summary>
        /// <param name="teacherId">Teacher ID (Teachers table ID, not User ID)</param>
        /// <param name="request">Updated teacher details</param>
        /// <returns>Update result</returns>
        /// <response code="200">Teacher updated successfully</response>
        /// <response code="400">Invalid request or update failed</response>
        [HttpPut("{teacherId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> UpdateTeacher(int teacherId, [FromBody] TeacherUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _teacherService.UpdateTeacherAsync(teacherId, request);

            if (!result.Success)
            {
                // "Teacher not found in this school" and "Email already exists" are
                // different problems; this used to answer both with "Failed to update
                // teacher".
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Teacher updated successfully"));
        }

        /// <summary>
        /// Update own profile (Teacher self-service)
        /// </summary>
        /// <param name="request">Updated profile details</param>
        /// <returns>Update result</returns>
        /// <response code="200">Profile updated successfully</response>
        /// <response code="400">Invalid request or update failed</response>
        [HttpPut("my-profile")]
        [Authorize(Roles = Constants.Roles.Teacher)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] TeacherUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _teacherService.UpdateTeacherProfileAsync(request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Profile updated successfully"));
        }

        /// <summary>
        /// Delete teacher (soft delete)
        /// </summary>
        /// <param name="teacherId">Teacher ID (Teachers table ID, not User ID)</param>
        /// <returns>Delete result</returns>
        /// <response code="200">Teacher deleted successfully</response>
        /// <response code="400">Delete failed</response>
        [HttpDelete("{teacherId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> DeleteTeacher(int teacherId)
        {
            var result = await _teacherService.DeleteTeacherAsync(teacherId);

            if (!result.Success)
            {
                // Two refusals with different remedies: reassign the classes first, or
                // accept that a teacher with attendance history cannot be removed at all.
                // Both used to arrive as "Failed to delete teacher".
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Teacher deleted successfully"));
        }

        /// <summary>
        /// Replace the teacher's subject set.
        ///
        /// Not additive: sp_AssignSubjectsToTeacher deactivates every link absent from the
        /// list, so an empty list clears them all. TeacherSubjectAssignmentDTO carries no
        /// [MinLength(1)], which is what makes "teaches nothing" expressible here -- unlike
        /// the student equivalent, where it is not.
        /// </summary>
        /// <param name="teacherId">Teacher ID (Teachers table ID)</param>
        /// <param name="request">The complete list of subject IDs the teacher should hold</param>
        /// <returns>Assignment result with the surviving count</returns>
        /// <response code="200">Subjects assigned successfully</response>
        /// <response code="400">Invalid request or assignment failed</response>
        [HttpPost("{teacherId}/subjects")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> AssignSubjectsToTeacher(
            int teacherId,
            [FromBody] TeacherSubjectAssignmentDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Invalid request"));
            }

            var (result, subjectsAssigned) = await _teacherService.AssignSubjectsToTeacherAsync(teacherId, request.SubjectIds);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                new { SubjectsAssigned = subjectsAssigned },
                subjectsAssigned == 0
                    ? "All subjects removed from this teacher"
                    : $"{subjectsAssigned} subject(s) assigned successfully"
            ));
        }

        /// <summary>
        /// Get teacher's assigned subjects
        /// </summary>
        /// <param name="teacherId">Teacher ID (Teachers table ID)</param>
        /// <returns>List of assigned subjects</returns>
        /// <response code="200">Subjects retrieved successfully</response>
        [HttpGet("{teacherId}/subjects")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<List<TeacherSubjectDTO>>), 200)]
        public async Task<IActionResult> GetTeacherSubjects(int teacherId)
        {
            var subjects = await _teacherService.GetTeacherSubjectsAsync(teacherId);

            return Ok(ApiResponse<List<TeacherSubjectDTO>>.SuccessResponse(subjects, "Subjects retrieved successfully"));
        }

        /// <summary>
        /// Get teacher's assigned classes
        /// </summary>
        /// <param name="teacherId">Teacher ID (Teachers table ID)</param>
        /// <returns>List of assigned classes</returns>
        /// <response code="200">Classes retrieved successfully</response>
        [HttpGet("{teacherId}/classes")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<List<TeacherClassDTO>>), 200)]
        public async Task<IActionResult> GetTeacherClasses(int teacherId)
        {
            var classes = await _teacherService.GetTeacherClassesAsync(teacherId);

            return Ok(ApiResponse<List<TeacherClassDTO>>.SuccessResponse(classes, "Classes retrieved successfully"));
        }

        /// <summary>
        /// Get teacher's subject-class assignments
        /// </summary>
        /// <param name="teacherId">Teacher ID (Teachers table ID)</param>
        /// <returns>List of subject-class assignments</returns>
        /// <response code="200">Assignments retrieved successfully</response>
        [HttpGet("{teacherId}/subject-assignments")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [ProducesResponseType(typeof(ApiResponse<List<TeacherSubjectClassDTO>>), 200)]
        public async Task<IActionResult> GetTeacherSubjectAssignments(int teacherId)
        {
            var assignments = await _teacherService.GetTeacherSubjectAssignmentsAsync(teacherId);

            return Ok(ApiResponse<List<TeacherSubjectClassDTO>>.SuccessResponse(
                assignments,
                "Subject assignments retrieved successfully"
            ));
        }

        /// <summary>
        /// Assign teacher to teach a specific subject in a specific class
        /// </summary>
        /// <param name="request">Assignment details (teacherId, subjectId, classId)</param>
        /// <returns>Assignment result</returns>
        /// <response code="200">Assignment created successfully</response>
        /// <response code="400">Invalid request or assignment failed</response>
        [HttpPost("subject-class-assignments")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> AssignTeacherToSubjectClass(
            [FromBody] TeacherClassSubjectAssignmentDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var (result, assignmentId) = await _teacherService.AssignTeacherToSubjectClassAsync(
                request.TeacherId,
                request.SubjectId,
                request.ClassId,
                request.IsActive
            );

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                new { AssignmentId = assignmentId },
                request.IsActive
                    ? "Teacher assigned to subject-class successfully"
                    : "Assignment withdrawn successfully"
            ));
        }
    }
}
