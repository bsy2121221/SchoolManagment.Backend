using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        /// <summary>
        /// Register a new student (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Constants.Roles.Admin)]
        public async Task<IActionResult> RegisterStudent([FromBody] StudentRegistrationDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            // The procedure names its own refusal -- a full class, a class in
            // another school, a duplicate email -- and each of those is something
            // the admin can act on, so the message is the response.
            var (result, student) = await _studentService.RegisterStudentAsync(request);
            if (!result.Success || student == null)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return CreatedAtAction(
                nameof(GetStudentById),
                new { studentId = student.StudentId },
                ApiResponse<StudentRegistrationResponseDTO>.SuccessResponse(student, "Student registered successfully")
            );
        }

        /// <summary>
        /// Get all students (paginated, with filters) - Admin/Teacher
        /// </summary>
        [HttpGet]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        public async Task<IActionResult> GetAllStudents(
            [FromQuery] int? classId = null,
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

            var result = await _studentService.GetAllStudentsAsync(classId, isActive, searchTerm, page, pageSize);

            return Ok(ApiResponse<PaginatedResponse<StudentDTO>>.SuccessResponse(result, "Students retrieved successfully"));
        }

        /// <summary>
        /// Get student by ID - Admin/Teacher/Self
        /// </summary>
        [HttpGet("{studentId}")]
        public async Task<IActionResult> GetStudentById(int studentId)
        {
            var student = await _studentService.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                return NotFound(ApiResponse.FailureResult("Student not found"));
            }

            return Ok(ApiResponse<StudentDTO>.SuccessResponse(student, "Student retrieved successfully"));
        }

        /// <summary>
        /// Get student profile with stats - Admin/Teacher/Self
        /// </summary>
        [HttpGet("{studentId}/profile")]
        public async Task<IActionResult> GetStudentProfile(int studentId)
        {
            var profile = await _studentService.GetStudentProfileAsync(studentId);
            if (profile == null)
            {
                return NotFound(ApiResponse.FailureResult("Student profile not found"));
            }

            return Ok(ApiResponse<StudentProfileDTO>.SuccessResponse(profile, "Student profile retrieved successfully"));
        }

        /// <summary>
        /// Update student details - Admin only
        /// </summary>
        [HttpPut("{studentId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        public async Task<IActionResult> UpdateStudent(int studentId, [FromBody] StudentUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _studentService.UpdateStudentAsync(studentId, request);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Student updated successfully"));
        }

        /// <summary>
        /// Delete student - Admin only
        /// </summary>
        [HttpDelete("{studentId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        public async Task<IActionResult> DeleteStudent(int studentId)
        {
            // "Cannot delete student with attendance, result or fee history.
            // Deactivate the account instead." is the common answer here, and it
            // tells the admin what to do next -- so it must not be swallowed.
            var result = await _studentService.DeleteStudentAsync(studentId);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Student deleted successfully"));
        }

        /// <summary>
        /// Promote student to next class - Admin only
        /// </summary>
        [HttpPut("{studentId}/promote")]
        [Authorize(Roles = Constants.Roles.Admin)]
        public async Task<IActionResult> PromoteStudent(int studentId, [FromBody] StudentPromoteDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Invalid request"));
            }

            var result = await _studentService.PromoteStudentAsync(studentId, request);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Student promoted successfully"));
        }

        /// <summary>
        /// Assign subjects to student - Admin only
        /// </summary>
        [HttpPost("{studentId}/subjects")]
        [Authorize(Roles = Constants.Roles.Admin)]
        public async Task<IActionResult> AssignSubjectsToStudent(int studentId, [FromBody] StudentSubjectAssignmentDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Invalid request"));
            }

            var result = await _studentService.AssignSubjectsToStudentAsync(studentId, request);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Subjects assigned successfully"));
        }

        /// <summary>
        /// Get student's subjects - Admin/Teacher/Self
        /// </summary>
        [HttpGet("{studentId}/subjects")]
        public async Task<IActionResult> GetStudentSubjects(int studentId)
        {
            var subjects = await _studentService.GetStudentSubjectsAsync(studentId);

            return Ok(ApiResponse<List<SubjectDTO>>.SuccessResponse(subjects, "Student subjects retrieved successfully"));
        }

        /// <summary>
        /// Remove subject from student - Admin only
        /// </summary>
        [HttpDelete("{studentId}/subjects/{subjectId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        public async Task<IActionResult> RemoveStudentSubject(int studentId, int subjectId)
        {
            var result = await _studentService.RemoveStudentSubjectAsync(studentId, subjectId);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Subject removed successfully"));
        }

        /// <summary>
        /// Get students by class ID - Admin/Teacher
        /// </summary>
        [HttpGet("by-class/{classId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        public async Task<IActionResult> GetStudentsByClass(int classId)
        {
            var students = await _studentService.GetStudentsByClassAsync(classId);

            return Ok(ApiResponse<List<StudentDTO>>.SuccessResponse(students, "Class students retrieved successfully"));
        }
    }
}
