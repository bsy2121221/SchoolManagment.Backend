using SchoolManagment.Models.DTOs.Dashboard;

namespace SchoolManagment.Auth.Services.Interfaces
{
    /// <summary>
    /// Assembles a landing page for whoever is signed in.
    ///
    /// This is the one service in the project that composes others rather than owning a
    /// repository of its own. That is deliberate: the figures a dashboard shows already
    /// have canonical queries behind the schedule, results, fee and school services, and
    /// a second implementation of any of them would be a second thing to keep correct.
    /// What it does own is the three aggregate procedures in 14_Procs_Dashboard.sql that
    /// nothing else called.
    ///
    /// Every method reads the caller's identity from the token. None of them takes a
    /// user, student, teacher or school id, so there is no parameter through which one
    /// person's dashboard could be asked for by another.
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// The whole landing page in one call: the sections that apply to the caller,
        /// trimmed to what their permission grid covers.
        ///
        /// Never throws for want of data and never returns null. A user with nothing to
        /// show -- no school in scope, no role section, an empty audit log -- gets an
        /// envelope whose sections are null and whose
        /// <see cref="DashboardDTO.Omitted"/> list says why. That keeps the landing page
        /// renderable for accounts in an incomplete state, such as a Student-role login
        /// whose student record has been deactivated.
        ///
        /// Costs between two and six round trips depending on the role, against roughly
        /// a dozen HTTP requests for the same screen assembled client-side.
        /// </summary>
        Task<DashboardDTO> GetDashboardAsync();

        /// <summary>
        /// Just the school-wide figures, for a widget that refreshes on its own without
        /// re-reading schedules and audit rows.
        ///
        /// Null when no school is in scope -- a platform administrator who has not
        /// switched into one has no school to count. Trimmed by permission exactly as it
        /// is inside <see cref="GetDashboardAsync"/>, so the two never disagree.
        /// </summary>
        Task<SchoolStatsDTO?> GetSchoolStatsAsync();

        /// <summary>
        /// The caller's own recent audit rows, newest first.
        /// </summary>
        /// <param name="topCount">
        /// Clamped to 1..500 by the procedure, which substitutes its default of 10 for
        /// anything outside that range rather than failing.
        /// </param>
        Task<List<ActivityDTO>> GetMyActivitiesAsync(int topCount);
    }
}
