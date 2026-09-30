using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Classes;
using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClassesController : ControllerBase
    {
        private readonly IClassService _classService;

        public ClassesController(IClassService classService)
        {
            _classService = classService;
        }

        /// <summary>
        /// Create a new class (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        public async Task<IActionResult> CreateClass([FromBody] ClassCreateDTO request)
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
            // actually hit is the duplicate grade and section -- and because that check is
            // not filtered on IsActive, it can be a *soft-deleted* class holding 10-A,
            // which no amount of looking at the class list would explain.
            var (result, classId) = await _classService.CreateClassAsync(request);
            if (!result.Success || classId is null)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return CreatedAtAction(
                nameof(GetClassById),
                new { classId = classId.Value },
                ApiResponse<object>.SuccessResponse(
                    new { classId = classId.Value }, "Class created successfully")
            );
        }

        /// <summary>
        /// Get all classes (paginated, with filters) - Admin/Teacher
        /// </summary>
        [HttpGet]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        public async Task<IActionResult> GetAllClasses(
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

            var result = await _classService.GetAllClassesAsync(grade, isActive, page, pageSize);

            return Ok(ApiResponse<PaginatedResponse<ClassDTO>>.SuccessResponse(result, "Classes retrieved successfully"));
        }

        /// <summary>
        /// Get class by ID - Admin/Teacher
        /// </summary>
        [HttpGet("{classId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        public async Task<IActionResult> GetClassById(int classId)
        {
            var classDto = await _classService.GetClassByIdAsync(classId);
            if (classDto == null)
            {
                return NotFound(ApiResponse.FailureResult("Class not found"));
            }

            return Ok(ApiResponse<ClassDTO>.SuccessResponse(classDto, "Class retrieved successfully"));
        }

        /// <summary>
        /// Get class details with students and stats - Admin/Teacher
        /// </summary>
        [HttpGet("{classId}/details")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        public async Task<IActionResult> GetClassDetails(int classId)
        {
            var details = await _classService.GetClassDetailsAsync(classId);
            if (details == null)
            {
                return NotFound(ApiResponse.FailureResult("Class not found"));
            }

            return Ok(ApiResponse<ClassDetailsDTO>.SuccessResponse(details, "Class details retrieved successfully"));
        }

        /// <summary>
        /// Update class - Admin only
        /// </summary>
        [HttpPut("{classId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateClass(int classId, [FromBody] ClassUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _classService.UpdateClassAsync(classId, request);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Class updated successfully"));
        }

        /// <summary>
        /// Update class status - Admin only
        /// </summary>
        [HttpPut("{classId}/status")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateClassStatus(int classId, [FromBody] ClassStatusUpdateDTO request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.FailureResult("Invalid request"));
            }

            var result = await _classService.UpdateClassStatusAsync(classId, request.IsActive);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult($"Class {(request.IsActive ? "activated" : "deactivated")} successfully"));
        }

        /// <summary>
        /// Delete class - Admin only
        /// </summary>
        [HttpDelete("{classId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteClass(int classId)
        {
            // sp_DeleteClass refuses on any of three conditions -- enrolled students,
            // recorded attendance, active examinations -- and each needs a different
            // action from the user. "Failed to delete class" told them none of it.
            var result = await _classService.DeleteClassAsync(classId);
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Class deleted successfully"));
        }

        /// <summary>
        /// Get class students - Admin/Teacher
        /// </summary>
        [HttpGet("{classId}/students")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        public async Task<IActionResult> GetClassStudents(int classId)
        {
            var students = await _classService.GetClassStudentsAsync(classId);

            return Ok(ApiResponse<List<StudentDTO>>.SuccessResponse(students, "Class students retrieved successfully"));
        }

        /// <summary>
        /// Get class timetable - Admin/Teacher/Student (in class)
        /// </summary>
        [HttpGet("{classId}/timetable")]
        public async Task<IActionResult> GetClassTimetable(int classId, [FromQuery] int? dayOfWeek = null)
        {
            var timetable = await _classService.GetClassTimetableAsync(classId, dayOfWeek);
            if (timetable == null)
            {
                return NotFound(ApiResponse.FailureResult("Class timetable not found"));
            }

            return Ok(ApiResponse<ClassTimetableDTO>.SuccessResponse(timetable, "Class timetable retrieved successfully"));
        }
    }
}
