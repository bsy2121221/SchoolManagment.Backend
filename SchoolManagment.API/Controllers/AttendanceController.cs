using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Attendance;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Attendance management endpoints.
    ///
    /// Two guards on every action, as elsewhere in the project: the role policy is the
    /// coarse route check and <c>[RequiresPermission]</c> is the fine one. The permission
    /// attributes are new -- this controller was the only one in the solution that named no
    /// module at all, so <c>Attendance</c> was a column in the grid that nothing read. An
    /// administrator who took <c>Attendance:Create</c> away from the Teacher role saw no
    /// change in behaviour, which is the grid lying.
    ///
    /// Marking is a MERGE, so it creates and corrects through one endpoint and is gated on
    /// <c>Create</c> -- consistent with POST everywhere else here. The consequence is worth
    /// stating: a custom role given <c>Edit</c> but not <c>Create</c> cannot correct a
    /// register, because there is no separate update to gate on <c>Edit</c>.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        /// <summary>
        /// Mark attendance for a single student. Requires create access to the Attendance module.
        ///
        /// An upsert: re-marking a student on a date the register already covers overwrites
        /// that row rather than failing. Its one refusal is a student who is not actively
        /// enrolled in the class given, which the procedure checks so that a mistyped class
        /// id cannot file a record against the wrong register.
        /// </summary>
        /// <param name="request">Attendance details</param>
        /// <returns>Mark result</returns>
        /// <response code="200">Attendance marked successfully</response>
        /// <response code="400">Invalid request or marking failed</response>
        [HttpPost]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Attendance, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> MarkAttendance([FromBody] AttendanceMarkDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _attendanceService.MarkAttendanceAsync(request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Attendance marked successfully"));
        }

        /// <summary>
        /// Mark attendance for multiple students in bulk (entire class). Requires create
        /// access to the Attendance module.
        ///
        /// One transaction for the whole register, so a failure part-way leaves it as it
        /// was rather than half-marked. Records naming a student who is not actively
        /// enrolled in the class are skipped and counted rather than aborting the
        /// submission -- so a non-zero <c>RecordsSkipped</c> is a successful save that did
        /// less than the caller asked, and the response says so rather than implying the
        /// register is complete.
        /// </summary>
        /// <param name="request">Bulk attendance data</param>
        /// <returns>Bulk mark result with counts</returns>
        /// <response code="200">Attendance marked successfully</response>
        /// <response code="400">Invalid request or marking failed</response>
        [HttpPost("bulk")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Attendance, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<AttendanceBulkMarkResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> MarkAttendanceBulk([FromBody] AttendanceBulkMarkDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var (result, counts) = await _attendanceService.MarkAttendanceBulkAsync(request);

            if (!result.Success || counts is null)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse<AttendanceBulkMarkResponseDTO>.SuccessResponse(
                counts,
                $"Attendance marked: {counts.RecordsMarked} students, {counts.RecordsSkipped} skipped"
            ));
        }

        /// <summary>
        /// Get class attendance register for a specific date. Requires view access to the
        /// Attendance module.
        ///
        /// Driven from the class roll with a LEFT JOIN, so a student with nothing marked
        /// comes back with a null <c>isPresent</c>. That third state is the point of the
        /// screen: it distinguishes "absent" from "not yet decided", which an empty list
        /// could not.
        /// </summary>
        /// <param name="classId">Class ID</param>
        /// <param name="attendanceDate">Date (defaults to today)</param>
        /// <returns>List of students with attendance status</returns>
        /// <response code="200">Attendance retrieved successfully</response>
        [HttpGet("class/{classId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Attendance, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<ClassAttendanceDTO>>), 200)]
        public async Task<IActionResult> GetClassAttendance(
            int classId,
            [FromQuery] DateTime? attendanceDate = null)
        {
            var date = attendanceDate ?? DateTime.Today;
            var attendance = await _attendanceService.GetClassAttendanceAsync(classId, date);

            return Ok(ApiResponse<List<ClassAttendanceDTO>>.SuccessResponse(
                attendance,
                "Class attendance retrieved successfully"
            ));
        }

        /// <summary>
        /// Get attendance history for a specific student. Requires view access to the
        /// Attendance module.
        /// </summary>
        /// <param name="studentId">Student ID</param>
        /// <param name="startDate">Start date (defaults to 1 month ago)</param>
        /// <param name="endDate">End date (defaults to today)</param>
        /// <returns>List of attendance records</returns>
        /// <response code="200">Attendance history retrieved successfully</response>
        [HttpGet("student/{studentId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Attendance, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<StudentAttendanceDTO>>), 200)]
        public async Task<IActionResult> GetStudentAttendance(
            int studentId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var attendance = await _attendanceService.GetStudentAttendanceAsync(
                studentId,
                startDate,
                endDate
            );

            return Ok(ApiResponse<List<StudentAttendanceDTO>>.SuccessResponse(
                attendance,
                "Student attendance history retrieved successfully"
            ));
        }

        /// <summary>
        /// Get attendance summary with percentages. Requires view access to the Attendance
        /// module.
        ///
        /// Driven from the student roll, so a student with nothing marked in the window
        /// still appears, with a null percentage rather than a zero. Those are different
        /// facts: 0% is a student who was marked absent every day, null is a register
        /// nobody opened.
        /// </summary>
        /// <param name="classId">Filter by class ID (optional)</param>
        /// <param name="startDate">Start date (defaults to 1 month ago)</param>
        /// <param name="endDate">End date (defaults to today)</param>
        /// <returns>Attendance summary with statistics</returns>
        /// <response code="200">Summary retrieved successfully</response>
        [HttpGet("summary")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Attendance, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<AttendanceSummaryDTO>>), 200)]
        public async Task<IActionResult> GetAttendanceSummary(
            [FromQuery] int? classId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var summary = await _attendanceService.GetAttendanceSummaryAsync(
                classId,
                startDate,
                endDate
            );

            return Ok(ApiResponse<List<AttendanceSummaryDTO>>.SuccessResponse(
                summary,
                "Attendance summary retrieved successfully"
            ));
        }

        /// <summary>
        /// Get daily attendance report (aggregated by date). Requires view access to the
        /// Attendance module.
        ///
        /// Driven from the Attendance rows themselves, unlike the summary above, so a date
        /// on which nobody was marked is absent from the report rather than present with
        /// zeroes. Gaps in the returned dates are therefore unopened registers.
        /// </summary>
        /// <param name="classId">Filter by class ID (optional)</param>
        /// <param name="startDate">Start date (defaults to 1 month ago)</param>
        /// <param name="endDate">End date (defaults to today)</param>
        /// <returns>Daily attendance statistics</returns>
        /// <response code="200">Report retrieved successfully</response>
        [HttpGet("daily-report")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Attendance, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<DailyAttendanceReportDTO>>), 200)]
        public async Task<IActionResult> GetDailyAttendanceReport(
            [FromQuery] int? classId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var report = await _attendanceService.GetDailyAttendanceReportAsync(
                classId,
                startDate,
                endDate
            );

            return Ok(ApiResponse<List<DailyAttendanceReportDTO>>.SuccessResponse(
                report,
                "Daily attendance report retrieved successfully"
            ));
        }
    }
}
