using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Schedule;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// The teaching timetable: one row per lesson (teacher, subject, class, weekday, start and end,
    /// optional room), read by teacher or by class.
    ///
    /// Reworked in Phase 13. The procedures were sound; this layer was not. It is the fifth
    /// controller found carrying no <c>[RequiresPermission]</c> (every action has one now), its
    /// writes were <c>Roles = Admin</c> (so SuperAdmin, who passes every other AdminOnly gate, was
    /// refused), and every refusal was a 400, so a client could not tell "that slot is taken"
    /// from "that teacher does not exist".
    ///
    /// <b>Status codes on a write:</b> 409 when the slot clashes, with <c>Data.ConflictWith</c>
    /// set to the entry in the way and the message naming it; 404 when the entry being edited or
    /// deleted is not in this school; 400 for everything else.
    ///
    /// <b>Reads are AdminOrTeacher.</b> The seeded grid gives Student and Parent
    /// <c>Schedule:View</c>, but every read here takes a teacher or class id and nothing
    /// resolves "my class" or "my child's class", so opening them would let a student page
    /// through every class's timetable by id and still not tell them which one is theirs. Any
    /// teacher can read any teacher's week, as they can in a staff room.
    ///
    /// Times travel as <c>TimeSpan</c>, which System.Text.Json writes and reads as "HH:mm:ss".
    /// The <c>*Formatted</c> strings come from SQL <c>FORMAT</c> and follow the server's
    /// culture; clients should format the TimeSpan themselves.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        private BadRequestObjectResult ValidationFailure()
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new ErrorDetail("", e.ErrorMessage))
                .ToList();
            return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
        }

        private static bool IsNotFound(string message) =>
            message.Contains("not found", StringComparison.OrdinalIgnoreCase);

        /// <summary>Turns a refused write into 409, 404 or 400.</summary>
        private ObjectResult WriteFailure(ScheduleOperationResponseDTO result, bool entryIsTarget)
        {
            if (result.Result == "Conflict")
            {
                return Conflict(new ApiResponse<ScheduleOperationResponseDTO>
                {
                    Success = false,
                    Message = result.Message,
                    Data = result
                });
            }

            // Only "the entry you are editing is gone" is a 404. A teacher, subject or class
            // that is not found is a bad reference in the body, which is a 400.
            if (entryIsTarget && result.Message.StartsWith("Schedule entry not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(ApiResponse.FailureResult(result.Message));
            }

            return BadRequest(ApiResponse.FailureResult(result.Message));
        }

        private static bool IsValidDay(int dayOfWeek) => dayOfWeek is >= 1 and <= 7;

        /// <summary>
        /// Create a schedule entry.
        /// </summary>
        /// <response code="201">Created; Data.Id is the new entry</response>
        /// <response code="400">Invalid, or a teacher / subject / class that is not in this school</response>
        /// <response code="409">The teacher, the class or the room is already busy in that slot</response>
        [HttpPost]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Schedule, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<ScheduleOperationResponseDTO>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse<ScheduleOperationResponseDTO>), 409)]
        public async Task<IActionResult> CreateScheduleEntry([FromBody] ScheduleEntryCreateDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            if (request.EndTime <= request.StartTime)
            {
                return BadRequest(ApiResponse.FailureResult("End time must be after start time"));
            }

            var result = await _scheduleService.CreateScheduleEntryAsync(request);

            if (result.Result != "Success") return WriteFailure(result, entryIsTarget: false);

            return CreatedAtAction(
                nameof(GetTeacherSchedule),
                new { teacherId = request.TeacherId },
                ApiResponse<ScheduleOperationResponseDTO>.SuccessResponse(result, result.Message)
            );
        }

        /// <summary>
        /// Replace a schedule entry.
        /// </summary>
        /// <remarks>
        /// The id is in the route; the body is the same shape as create. It used to be a PUT to the
        /// collection with the id in the body.
        /// </remarks>
        /// <response code="200">Updated</response>
        /// <response code="400">Invalid, or a teacher / subject / class that is not in this school</response>
        /// <response code="404">No active entry with this id in this school</response>
        /// <response code="409">The new slot clashes with another entry</response>
        [HttpPut("{id:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Schedule, Constants.PermissionActions.Edit)]
        [ProducesResponseType(typeof(ApiResponse<ScheduleOperationResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse<ScheduleOperationResponseDTO>), 409)]
        public async Task<IActionResult> UpdateScheduleEntry(int id, [FromBody] ScheduleEntryCreateDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            if (request.EndTime <= request.StartTime)
            {
                return BadRequest(ApiResponse.FailureResult("End time must be after start time"));
            }

            var result = await _scheduleService.UpdateScheduleEntryAsync(id, request);

            if (result.Result != "Success") return WriteFailure(result, entryIsTarget: true);

            return Ok(ApiResponse<ScheduleOperationResponseDTO>.SuccessResponse(result, result.Message));
        }

        /// <summary>
        /// A teacher's whole week, Monday first.
        /// </summary>
        /// <param name="teacherId">Teachers.Id (not the user id)</param>
        /// <remarks>An unknown teacher and a teacher with no lessons both return an empty list.</remarks>
        [HttpGet("teacher/{teacherId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Schedule, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<ScheduleEntryDTO>>), 200)]
        public async Task<IActionResult> GetTeacherSchedule(int teacherId)
        {
            var schedule = await _scheduleService.GetTeacherScheduleAsync(teacherId);

            return Ok(ApiResponse<List<ScheduleEntryDTO>>.SuccessResponse(
                schedule,
                "Teacher schedule retrieved successfully"
            ));
        }

        /// <summary>
        /// A teacher's lessons on one weekday (1 = Monday .. 7 = Sunday).
        /// </summary>
        [HttpGet("teacher/{teacherId:int}/day/{dayOfWeek:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Schedule, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<ScheduleEntryDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetTeacherScheduleByDay(int teacherId, int dayOfWeek)
        {
            if (!IsValidDay(dayOfWeek))
            {
                return BadRequest(ApiResponse.FailureResult("Day of week must be between 1 (Monday) and 7 (Sunday)"));
            }

            var schedule = await _scheduleService.GetTeacherScheduleByDayAsync(teacherId, dayOfWeek);

            return Ok(ApiResponse<List<ScheduleEntryDTO>>.SuccessResponse(
                schedule,
                "Teacher daily schedule retrieved successfully"
            ));
        }

        /// <summary>
        /// The lesson a teacher is in now, and the next one (wrapping into next week).
        /// </summary>
        /// <remarks>
        /// "Now" is the database server's clock and time zone, not the caller's.
        /// Either may be null.
        /// </remarks>
        [HttpGet("teacher/{teacherId:int}/current-next")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Schedule, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<CurrentNextClassesResponseDTO>), 200)]
        public async Task<IActionResult> GetTeacherCurrentAndNextClasses(int teacherId)
        {
            var classes = await _scheduleService.GetTeacherCurrentAndNextClassesAsync(teacherId);

            return Ok(ApiResponse<CurrentNextClassesResponseDTO>.SuccessResponse(
                classes,
                "Current and next classes retrieved successfully"
            ));
        }

        /// <summary>
        /// Counts for a teacher's week: lessons, subjects, classes, days, first and last times.
        /// </summary>
        [HttpGet("teacher/{teacherId:int}/stats")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Schedule, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<TeacherScheduleStatsDTO>), 200)]
        public async Task<IActionResult> GetTeacherScheduleStats(int teacherId)
        {
            var stats = await _scheduleService.GetTeacherScheduleStatsAsync(teacherId);

            return Ok(ApiResponse<TeacherScheduleStatsDTO>.SuccessResponse(
                stats,
                "Teacher schedule statistics retrieved successfully"
            ));
        }

        /// <summary>
        /// A class's timetable, optionally one weekday.
        /// </summary>
        [HttpGet("class/{classId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Schedule, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<ClassScheduleDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetClassSchedule(int classId, [FromQuery] int? dayOfWeek = null)
        {
            if (dayOfWeek is int day && !IsValidDay(day))
            {
                return BadRequest(ApiResponse.FailureResult("Day of week must be between 1 (Monday) and 7 (Sunday)"));
            }

            var schedule = await _scheduleService.GetClassScheduleAsync(classId, dayOfWeek);

            return Ok(ApiResponse<List<ClassScheduleDTO>>.SuccessResponse(
                schedule,
                "Class schedule retrieved successfully"
            ));
        }

        /// <summary>
        /// Remove a schedule entry (soft delete).
        /// </summary>
        /// <response code="200">Removed</response>
        /// <response code="404">No active entry with this id in this school</response>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Schedule, Constants.PermissionActions.Delete)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> DeleteScheduleEntry(int id)
        {
            var (result, message) = await _scheduleService.DeleteScheduleEntryAsync(id);

            if (result != "Success")
            {
                return IsNotFound(message)
                    ? NotFound(ApiResponse.FailureResult(message))
                    : BadRequest(ApiResponse.FailureResult(message));
            }

            return Ok(ApiResponse.SuccessResult(message));
        }
    }
}
