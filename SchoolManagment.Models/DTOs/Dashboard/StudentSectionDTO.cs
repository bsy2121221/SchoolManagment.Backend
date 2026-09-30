using SchoolManagment.Models.DTOs.Results;
using SchoolManagment.Models.DTOs.Schedule;

namespace SchoolManagment.Models.DTOs.Dashboard
{
    /// <summary>
    /// A student's own landing page: attendance, marks, fees and today's timetable.
    ///
    /// Scoped to the signed-in student, resolved from the token's user id. No student
    /// id is accepted, so this cannot be pointed at a classmate.
    ///
    /// The counts are lifetime totals, not this term's -- sp_GetStudentProfileStats
    /// takes no date range. Treat them as "since admission".
    ///
    /// The three groups of figures are nullable because a customised role can withhold
    /// any of them: attendance needs Attendance:View, marks need Results:View, money
    /// needs Fees:View. The seeded Student role holds all three, so in a stock
    /// installation none of this is null for reasons of permission. Where null is
    /// ambiguous -- no attendance taken yet, or attendance withheld --
    /// <see cref="DashboardDTO.Omitted"/> names the fields that were withheld.
    /// </summary>
    public class StudentSectionDTO
    {
        /// <summary>dbo.Students.Id, for linking on to the by-student endpoints.</summary>
        public int StudentId { get; set; }

        public string? StudentNumber { get; set; }
        public string? RollNumber { get; set; }

        /// <summary>
        /// Null for a student who has been admitted but not yet placed in a class. That
        /// is a real state -- Students.ClassId is nullable -- and it is why
        /// <see cref="TodaySchedule"/> can be empty on a school day.
        /// </summary>
        public int? ClassId { get; set; }

        public string? ClassName { get; set; }

        public int? PresentDays { get; set; }
        public int? AbsentDays { get; set; }
        public int? TotalDays { get; set; }

        /// <summary>
        /// Present days over days marked. Null when attendance has never been taken for
        /// this student, where a 0% would read as perfect absenteeism rather than as no
        /// data.
        /// </summary>
        public decimal? AttendancePercentage { get; set; }

        public int? TotalResults { get; set; }

        /// <summary>
        /// Mean marks obtained, not a percentage: results are stored as marks out of
        /// each exam's own MaxMarks, and those maxima differ, so this average is only
        /// meaningful next to the results it came from. Null until the first result is
        /// entered.
        /// </summary>
        public double? AverageMarks { get; set; }

        public int? HighestMarks { get; set; }

        public int? TotalFees { get; set; }
        public decimal? PaidAmount { get; set; }

        /// <summary>
        /// Still owed across all active fees. Counts completed payments only, so a
        /// failed or refunded transaction does not make a fee look settled.
        /// </summary>
        public decimal? PendingAmount { get; set; }

        public int SubjectCount { get; set; }

        /// <summary>Most recent results first, capped -- the report card has the rest.</summary>
        public List<ResultDTO> RecentResults { get; set; } = new();

        /// <summary>
        /// Today's periods for the student's class. Empty when the student has no class,
        /// when the class has no timetable, or simply because it is Sunday.
        /// </summary>
        public List<ClassScheduleDTO> TodaySchedule { get; set; } = new();
    }
}
