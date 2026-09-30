namespace SchoolManagment.Models.DTOs.Dashboard
{
    /// <summary>
    /// The school-wide figures from sp_GetDashboardStats.
    ///
    /// Every figure is nullable, and null means "withheld", not "zero". The dashboard
    /// blanks out the numbers the caller's permission grid does not cover -- a teacher
    /// holding Reports:View but no Fees row gets the head counts and null money. A
    /// zero would be a lie a UI cannot detect, so the distinction is carried in the
    /// type; <see cref="DashboardDTO.Omitted"/> says which fields were dropped and
    /// why.
    ///
    /// Column names match the procedure so Dapper maps this directly. The counts come
    /// from the Students / Teachers / Parents tables joined to an active user, not
    /// from Users.Role, so a soft-deleted student stops counting the moment their row
    /// is deactivated rather than when their login is.
    /// </summary>
    public class SchoolStatsDTO
    {
        public int? TotalStudents { get; set; }
        public int? TotalTeachers { get; set; }
        public int? TotalParents { get; set; }
        public int? TotalClasses { get; set; }
        public int? TotalSubjects { get; set; }

        /// <summary>Attendance rows marked present for today's date.</summary>
        public int? TodayPresent { get; set; }

        public int? TodayAbsent { get; set; }

        /// <summary>
        /// Present as a share of the marks taken today, not of the roll: a class
        /// nobody has registered yet contributes to neither side. Null when attendance
        /// has not been marked at all today, because 0% would read as a school-wide
        /// absence.
        /// </summary>
        public decimal? TodayAttendancePercentage { get; set; }

        /// <summary>
        /// Fees past their due date that are still not covered by completed payments.
        /// Failed and refunded payments do not count as money received.
        /// </summary>
        public int? OverdueFees { get; set; }

        /// <summary>Total still owed across all active fees, whether due yet or not.</summary>
        public decimal? FeesOutstandingAmount { get; set; }

        public decimal? FeesCollectedThisMonth { get; set; }
    }
}
