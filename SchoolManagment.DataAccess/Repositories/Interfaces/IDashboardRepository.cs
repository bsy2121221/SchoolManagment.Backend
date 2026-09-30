using SchoolManagment.Models.DTOs.Dashboard;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    /// <summary>
    /// The three reads the dashboard cannot get from an existing repository.
    ///
    /// Everything else a landing page needs already has a repository -- schedules,
    /// classes, results, fees, schools -- and the dashboard service composes those
    /// rather than reimplementing their queries. What is here is the aggregate
    /// procedures in 14_Procs_Dashboard.sql that nothing else calls, plus the identity
    /// lookup that turns a token's user id into a student row.
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// sp_GetDashboardStats. Every figure filled; the caller decides which ones the
        /// requester is allowed to keep.
        ///
        /// Null only if the procedure returns nothing at all, which it does not for a
        /// school that exists -- the counts are scalar subqueries, so an empty school
        /// yields a row of zeros rather than no row.
        /// </summary>
        Task<SchoolStatsDTO?> GetSchoolStatsAsync(int schoolId);

        /// <summary>
        /// sp_GetProfileActivities -- the user's own audit rows, newest first.
        /// </summary>
        /// <param name="schoolId">
        /// Null reads across schools, which is what a platform administrator needs:
        /// their AuditLog rows carry SchoolId NULL. Pass the school id for anyone else
        /// so a switched administrator sees the school they are in.
        /// </param>
        /// <param name="topCount">
        /// Clamped to 1..500 by the procedure, which returns its default of 10 for
        /// anything outside that rather than failing.
        /// </param>
        Task<List<ActivityDTO>> GetActivitiesAsync(int? schoolId, int userId, int topCount);

        /// <summary>
        /// The signed-in student's identity and lifetime totals, via
        /// sp_GetStudentProfileStats.
        ///
        /// Null when the user has no active Students row -- a Student-role login whose
        /// student record was soft-deleted, which is a state the schema allows. The
        /// caller must treat that as "no student section", not as an error.
        ///
        /// RecentResults and TodaySchedule are left empty; they come from the results
        /// and schedule services.
        /// </summary>
        Task<StudentSectionDTO?> GetStudentSectionAsync(int schoolId, int userId);
    }
}
