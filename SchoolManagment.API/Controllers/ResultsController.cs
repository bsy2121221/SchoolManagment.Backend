using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Results;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Results and grade management endpoints.
    ///
    /// Every action now carries <c>[RequiresPermission]</c>. It carried none before — the third such
    /// controller after Attendance and Examinations, and the last of them, so <c>Results</c> was a
    /// column in the seeded permission grid that nothing read. An administrator who took
    /// <c>Results:Create</c> away from the Teacher role saw no change in behaviour.
    ///
    /// Two things to know about what these four endpoints can and cannot express.
    ///
    /// <b>There is no way to delete a mark.</b> Both writes are upserts whose MERGE has no DELETE
    /// branch, so a mark entered against the wrong student can be corrected but not withdrawn.
    /// Deleting the examination is the only way to remove results, and that withdraws all of them.
    ///
    /// <b>A student cannot read their own results.</b> The seeded grid grants Student and Parent
    /// <c>Results:View</c>, and <c>GET student/{studentId}</c> takes a <c>Students.Id</c> that
    /// appears on no token and is returned by no endpoint they can call. There is no
    /// <c>my-results</c>. The permission is real and unusable.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly IResultService _resultService;

        public ResultsController(IResultService resultService)
        {
            _resultService = resultService;
        }

        /// <summary>
        /// Add or update a single student result.
        /// </summary>
        /// <remarks>
        /// Leave <c>grade</c> null. The procedure derives the letter from the school's own
        /// thresholds via <c>fn_CalculateGrade</c>; supplying one overrides that school's scale for
        /// this mark alone.
        /// </remarks>
        /// <param name="request">Result entry details</param>
        /// <response code="200">Result saved successfully</response>
        /// <response code="400">Invalid request, or the procedure refused it</response>
        [HttpPost]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Results, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> AddOrUpdateResult([FromBody] ResultEntryDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var result = await _resultService.AddOrUpdateResultAsync(request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Result saved successfully"));
        }

        /// <summary>
        /// Bulk grade entry for a whole class.
        /// </summary>
        /// <remarks>
        /// Entries whose student is not in the examination's class, or whose marks fall outside
        /// 0..MaxMarks, are skipped and counted rather than aborting the batch — so a 200 here does
        /// not mean everything was written. Read <c>entriesSkipped</c>.
        ///
        /// Omitting a student is how you leave them unmarked; there is no way to clear a mark.
        /// </remarks>
        /// <param name="request">Bulk grade entry data</param>
        /// <response code="200">The batch ran. Check the counts.</response>
        /// <response code="400">Invalid request, or the procedure refused the whole batch</response>
        [HttpPost("bulk")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Results, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<BulkGradeEntryResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BulkGradeEntry([FromBody] BulkGradeEntryDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            var (result, counts) = await _resultService.BulkGradeEntryAsync(request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            // The counts travel on the success path as data, not inside the message. A caller that
            // wants to tell the user "48 saved, 2 skipped" should not have to parse prose for it.
            var message = counts.EntriesSkipped == 0
                ? $"Saved marks for {counts.EntriesSaved} student(s)."
                : $"Saved marks for {counts.EntriesSaved} student(s); {counts.EntriesSkipped} skipped.";

            return Ok(ApiResponse<BulkGradeEntryResponseDTO>.SuccessResponse(counts, message));
        }

        /// <summary>
        /// Every mark one student has been given, newest examination first.
        /// </summary>
        /// <remarks>
        /// <paramref name="studentId"/> is <c>Students.Id</c>, not <c>Users.Id</c> and not the
        /// school's admission number.
        ///
        /// Gated on <c>Results:View</c>, which is stricter than the bare <c>[Authorize]</c> this
        /// carried before — any signed-in account could read any student's full academic record by
        /// walking the ids. It is still school-wide rather than per-student: a teacher or parent
        /// holding the permission can read any student in the school, because nothing here consults
        /// <c>StudentParents</c> or the teacher's assignments. Closing that needs a procedure change.
        /// </remarks>
        /// <param name="studentId">Students.Id</param>
        /// <response code="200">Results retrieved successfully</response>
        [HttpGet("student/{studentId}")]
        [RequiresPermission(Constants.Modules.Results, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<ResultDTO>>), 200)]
        public async Task<IActionResult> GetStudentResults(int studentId)
        {
            var results = await _resultService.GetStudentResultsAsync(studentId);

            return Ok(ApiResponse<List<ResultDTO>>.SuccessResponse(
                results,
                "Student results retrieved successfully"
            ));
        }

        /// <summary>
        /// The class roll for one examination, with any marks already entered — the feed for the
        /// grade-entry screen.
        /// </summary>
        /// <remarks>
        /// Now <c>AdminOrTeacher</c>. It was <c>[Authorize(Roles = Teacher)]</c>, which locked an
        /// administrator out of the only screen the API offers for entering a class's marks even
        /// though <c>POST bulk</c> accepts their submission — they could save marks they had no way
        /// to load. A teacher is still held to their <c>TeacherSubjectAssignments</c> rows and gets
        /// a 403; an administrator bypasses that check, having no such rows to satisfy.
        ///
        /// An empty array means the roll is empty, or that no active examination matches this class
        /// and subject. It no longer means "you are not allowed" — that is the 403, and the two used
        /// to be the same response.
        /// </remarks>
        /// <param name="subjectId">Subject ID</param>
        /// <param name="classId">Class ID</param>
        /// <param name="examinationId">Examination ID. Omit for the most recent in this class and subject.</param>
        /// <response code="200">Students retrieved successfully</response>
        /// <response code="403">This teacher is not assigned to the subject and class</response>
        [HttpGet("grade-entry")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Results, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<StudentForGradeEntryDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<IActionResult> GetStudentsForGradeEntry(
            [FromQuery] int subjectId,
            [FromQuery] int classId,
            [FromQuery] int? examinationId = null)
        {
            var (authorised, students) = await _resultService.GetStudentsForGradeEntryAsync(
                subjectId, classId, examinationId);

            if (!authorised)
            {
                return StatusCode(403, ApiResponse.FailureResult(
                    "You are not assigned to enter grades for this subject and class."));
            }

            return Ok(ApiResponse<List<StudentForGradeEntryDTO>>.SuccessResponse(
                students,
                "Students for grade entry retrieved successfully"
            ));
        }
    }
}
