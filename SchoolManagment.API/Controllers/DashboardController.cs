using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Dashboard;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// The landing page, in one request.
    ///
    /// One endpoint rather than one per role. A per-role set -- /admin, /teacher,
    /// /student -- would put the same decision in two places, the route the client
    /// chooses and the guard on the action, and the two can disagree. Here the token
    /// decides, and a client that renders whichever sections came back cannot ask for
    /// somebody else's screen.
    ///
    /// Nothing here accepts a user, student, teacher or school id. Identity comes from
    /// the token, so the only dashboard reachable is the caller's own, in the school
    /// their token names.
    ///
    /// Sections are trimmed to the caller's permission grid rather than refused: a
    /// dashboard that 403s because one figure on it is out of reach is a dashboard
    /// nobody can render. Withheld figures come back null with an entry in
    /// <see cref="DashboardDTO.Omitted"/> naming the field and the permission it wanted,
    /// which is how a client tells "withheld" from "zero".
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Everything the signed-in user's landing page shows.
        ///
        /// Which sections arrive depends on who is asking. A platform administrator who
        /// has not switched into a school gets <c>platform</c>; one who has gets the
        /// school's figures instead. <c>school</c> goes to anyone holding Reports:View.
        /// <c>teacher</c>, <c>student</c> and <c>parent</c> follow the role, because "my
        /// own timetable" is not something the permission grid can express. Recent
        /// activity is always the caller's own rows.
        ///
        /// Always 200 for an authenticated caller, even when every section is null --
        /// an account in an incomplete state, such as a Student-role login whose student
        /// record has been deactivated, still needs a page to land on, and
        /// <c>omitted</c> explains the blanks.
        ///
        /// No role guard and no [RequiresPermission]. Every section inside is gated on
        /// its own, and a guard here would only decide who may be told they have nothing
        /// to see.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<DashboardDTO>), 200)]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _dashboardService.GetDashboardAsync();

            return Ok(ApiResponse<DashboardDTO>.SuccessResponse(
                dashboard, "Dashboard retrieved successfully"));
        }

        /// <summary>
        /// Just the school-wide counts, for a widget that polls without dragging
        /// schedules and audit rows along behind it.
        ///
        /// Guarded on Reports:View, not on the modules the figures come from. A student
        /// holds Attendance:View so they can see their own register; reading that as
        /// leave to see the school's would hand every pupil the whole roll's numbers.
        /// The seeded grid gives Reports to SuperAdmin, Admin and Teacher, which is the
        /// population school-wide aggregates belong to -- and this is the first endpoint
        /// in the project to use that module, which until now was a permission nothing
        /// checked.
        ///
        /// Figures within are still trimmed per module, by the same code that trims them
        /// in the full dashboard, so the two can never disagree about what a role sees.
        /// </summary>
        /// <response code="400">
        /// No school is in scope. Only reachable for a platform administrator who has
        /// not switched into one; there is no school to count, which is a different
        /// thing from a school that counts zero.
        /// </response>
        [HttpGet("stats")]
        [RequiresPermission(Constants.Modules.Reports, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<SchoolStatsDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _dashboardService.GetSchoolStatsAsync();

            if (stats == null)
            {
                return BadRequest(ApiResponse.FailureResult(
                    "No school is in scope. Switch into a school first: POST /api/Schools/{id}/switch"));
            }

            return Ok(ApiResponse<SchoolStatsDTO>.SuccessResponse(
                stats, "School statistics retrieved successfully"));
        }

        /// <summary>
        /// The caller's own recent audit rows, newest first -- logins, record changes,
        /// the trail behind "what did I just do".
        ///
        /// Authorisation is [Authorize] alone, with no reference to the Reports module.
        /// These are the caller's own rows and nobody else's: the user id comes from the
        /// token, so there is no parameter through which another person's history could
        /// be requested. Reading the audit log at large is a different endpoint, and does
        /// not exist yet.
        /// </summary>
        /// <param name="top">
        /// How many rows, 1 to 500. Out-of-range values fall back to the procedure's
        /// default of ten rather than failing -- a client asking for 0 or 10000 wants a
        /// list, not a validation error, and silently returning nothing would look like
        /// an empty log.
        /// </param>
        [HttpGet("activities")]
        [ProducesResponseType(typeof(ApiResponse<List<ActivityDTO>>), 200)]
        public async Task<IActionResult> GetActivities([FromQuery] int top = 10)
        {
            var activities = await _dashboardService.GetMyActivitiesAsync(top);

            return Ok(ApiResponse<List<ActivityDTO>>.SuccessResponse(
                activities, "Activities retrieved successfully"));
        }
    }
}
