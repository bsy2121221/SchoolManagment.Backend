using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Examinations;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Examination management endpoints.
    ///
    /// Two guards on every action, as elsewhere: the role policy is the coarse check and
    /// <c>[RequiresPermission]</c> the fine one. The permission attributes are new -- this was
    /// the second controller found naming no module at all (Attendance was the first), so
    /// <c>Examinations</c> was a column in the seeded grid that nothing read. An administrator
    /// taking <c>Examinations:Create</c> away from the Teacher role saw no change in behaviour,
    /// which is the grid lying.
    ///
    /// The single POST creates and updates, because <c>sp_CreateOrUpdateExamination</c> is an
    /// upsert matched on (ExamName, ExamType, ClassId, SubjectId). That has a consequence worth
    /// stating plainly, since no part of the URL shape hints at it: <b>those four fields cannot
    /// be edited.</b> Posting an existing exam under a new name creates a second exam and leaves
    /// the first standing with its marks. Only the date, the two mark totals and the duration are
    /// ever updated.
    ///
    /// Gating one upsert means gating on one action, and <c>Create</c> is what POST carries
    /// everywhere else here. So a custom role granted <c>Edit</c> but not <c>Create</c> cannot
    /// reschedule an exam -- the same gap recorded for Attendance, and for the same reason.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExaminationsController : ControllerBase
    {
        private readonly IExaminationService _examinationService;

        public ExaminationsController(IExaminationService examinationService)
        {
            _examinationService = examinationService;
        }

        /// <summary>
        /// Create or update an examination. Requires create access to the Examinations module.
        ///
        /// Matched on name, type, class and subject: a request whose four key fields match an
        /// active exam updates that exam's date, marks and duration, and any other request
        /// inserts. The one refusal beyond a bad class or subject is lowering
        /// <c>MaxMarks</c> below a score already entered, which would leave marks on the report
        /// card that no longer make sense.
        /// </summary>
        /// <param name="request">Examination details</param>
        /// <returns>The examination's ID</returns>
        /// <response code="201">Examination created</response>
        /// <response code="200">Examination updated</response>
        /// <response code="400">Invalid request or the operation was refused</response>
        [HttpPost]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Examinations, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<object>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> CreateOrUpdateExamination([FromBody] ExaminationCreateDTO request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => new ErrorDetail("", e.ErrorMessage))
                    .ToList();
                return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
            }

            // Kept ahead of the round trip even though the procedure and the CK_Examinations_Marks
            // constraint both enforce it: the two Range attributes cannot express a relationship
            // between fields, and this is the one validation message worth wording ourselves.
            if (request.PassingMarks > request.MaxMarks)
            {
                return BadRequest(ApiResponse.FailureResult("Passing marks cannot exceed maximum marks"));
            }

            var (result, examinationId, wasCreated) =
                await _examinationService.CreateOrUpdateExaminationAsync(request);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            // `wasCreated` is the procedure's own Operation column. It used to be inferred by
            // re-reading the examination and treating "found" as an update -- which meant 201
            // Created was returned only when the row just written could not be read back, and a
            // real create answered 200. One fewer round trip, and the right status code.
            if (wasCreated)
            {
                return CreatedAtAction(
                    nameof(GetExaminationById),
                    new { examinationId },
                    ApiResponse<object>.SuccessResponse(
                        new { ExaminationId = examinationId },
                        "Examination created successfully"
                    )
                );
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                new { ExaminationId = examinationId },
                "Examination updated successfully"
            ));
        }

        /// <summary>
        /// Get all examinations, most recent first, with an optional class filter. Requires view
        /// access to the Examinations module.
        ///
        /// Unpaged, and deliberately so at this layer -- the procedure returns every active exam
        /// for the school. There is no subject filter: an exam is scoped to a class and a
        /// subject, but only the class is a parameter.
        /// </summary>
        /// <param name="classId">Filter by class ID (optional)</param>
        /// <returns>List of examinations</returns>
        /// <response code="200">Examinations retrieved successfully</response>
        [HttpGet]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Examinations, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<ExaminationDTO>>), 200)]
        public async Task<IActionResult> GetExaminations([FromQuery] int? classId = null)
        {
            var examinations = await _examinationService.GetExaminationsAsync(classId);

            return Ok(ApiResponse<List<ExaminationDTO>>.SuccessResponse(
                examinations,
                "Examinations retrieved successfully"
            ));
        }

        /// <summary>
        /// Get one examination. Requires view access to the Examinations module.
        ///
        /// Soft-deleted examinations answer 404 here, which is what the mark-sheet screen relies
        /// on: <c>GET {id}/results</c> cannot tell "no such exam" from "a class with no students",
        /// because both are an empty set, so this endpoint is the guard for that one.
        /// </summary>
        /// <param name="examinationId">Examination ID</param>
        /// <returns>Examination details</returns>
        /// <response code="200">Examination found</response>
        /// <response code="404">Examination not found</response>
        [HttpGet("{examinationId}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Examinations, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<ExaminationDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetExaminationById(int examinationId)
        {
            var examination = await _examinationService.GetExaminationByIdAsync(examinationId);

            if (examination == null)
            {
                return NotFound(ApiResponse.FailureResult("Examination not found"));
            }

            return Ok(ApiResponse<ExaminationDTO>.SuccessResponse(
                examination,
                "Examination retrieved successfully"
            ));
        }

        /// <summary>
        /// Delete an examination. Requires delete access to the Examinations module.
        ///
        /// A soft delete that takes the marks with it: <c>sp_DeleteExamination</c> deactivates
        /// every result for the exam in the same transaction, so the exam does not vanish from
        /// the lists while its scores remain on report cards. <c>ExaminationDTO.ResultsEntered</c>
        /// is how a caller can say how many marks that is before asking.
        /// </summary>
        /// <param name="examinationId">Examination ID</param>
        /// <returns>Delete result</returns>
        /// <response code="200">Examination deleted successfully</response>
        /// <response code="400">Delete refused, with the reason</response>
        [HttpDelete("{examinationId}")]
        [Authorize(Roles = Constants.Roles.Admin)]
        [RequiresPermission(Constants.Modules.Examinations, Constants.PermissionActions.Delete)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> DeleteExamination(int examinationId)
        {
            var result = await _examinationService.DeleteExaminationAsync(examinationId);

            // The procedure says why -- "Examination not found in this school" -- and this used
            // to answer a flat "Failed to delete examination", which reads as a fault rather
            // than as a stale list.
            if (!result.Success)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse.SuccessResult("Examination deleted successfully"));
        }

        /// <summary>
        /// The mark sheet for one examination, with class rankings. Requires view access to the
        /// Examinations module.
        ///
        /// This is a read, not an entry screen -- marks are written through ResultsController.
        /// It returns the whole class rather than the students who have marks, so an unmarked
        /// student appears with a null score; see the remarks on
        /// <c>ExaminationResultsDTO.IsPass</c> for the one field that must not be read alone.
        /// </summary>
        /// <param name="examinationId">Examination ID</param>
        /// <returns>Complete mark sheet with student rankings</returns>
        /// <response code="200">Results retrieved successfully</response>
        [HttpGet("{examinationId}/results")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOrTeacher)]
        [RequiresPermission(Constants.Modules.Examinations, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<ExaminationResultsDTO>>), 200)]
        public async Task<IActionResult> GetExaminationResults(int examinationId)
        {
            var results = await _examinationService.GetExaminationResultsAsync(examinationId);

            return Ok(ApiResponse<List<ExaminationResultsDTO>>.SuccessResponse(
                results,
                "Examination results retrieved successfully"
            ));
        }
    }
}
