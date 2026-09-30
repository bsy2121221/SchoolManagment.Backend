using SchoolManagment.Models.DTOs.Schedule;
using SchoolManagment.Models.DTOs.Teachers;

namespace SchoolManagment.Models.DTOs.Dashboard
{
    /// <summary>
    /// A teacher's own landing page: what they are teaching now, what is next, and the
    /// rest of today.
    ///
    /// Scoped to the signed-in teacher, resolved from the token's user id. There is no
    /// teacher id in the request, so one teacher cannot ask for another's day.
    ///
    /// The sections are the same DTOs the dedicated /api/schedule and /api/teachers
    /// endpoints return, not flattened copies of them. A field that means one thing on
    /// the schedule screen means the same thing here, and the frontend's existing types
    /// are reusable as they stand.
    /// </summary>
    public class TeacherSectionDTO
    {
        /// <summary>
        /// dbo.Teachers.Id for the signed-in user, so the UI can link on to the
        /// endpoints that are addressed by teacher id without a lookup of its own.
        /// </summary>
        public int TeacherId { get; set; }

        /// <summary>
        /// The period in progress right now, or null outside teaching hours -- which is
        /// most of the time, including every weekend, so a UI must handle null as the
        /// normal case rather than as an error.
        /// </summary>
        public CurrentNextClassDTO? CurrentClass { get; set; }

        public CurrentNextClassDTO? NextClass { get; set; }

        /// <summary>
        /// Today's periods in full, in start-time order. Empty on a day the teacher has
        /// nothing scheduled.
        /// </summary>
        public List<ScheduleEntryDTO> TodaySchedule { get; set; } = new();

        /// <summary>Weekly load: periods, subjects, earliest and latest class, minutes.</summary>
        public TeacherScheduleStatsDTO? ScheduleStats { get; set; }

        /// <summary>The classes this teacher is assigned to, each with its head count.</summary>
        public List<TeacherClassDTO> Classes { get; set; } = new();
    }
}
