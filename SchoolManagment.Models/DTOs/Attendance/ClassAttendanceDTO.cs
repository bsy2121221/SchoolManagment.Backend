namespace SchoolManagment.Models.DTOs.Attendance
{
    /// <summary>
    /// One row per student on a class roll for a single date, from
    /// <c>sp_GetClassAttendance</c>.
    ///
    /// The register, not the attendance table: the procedure is driven from
    /// <c>Students</c> with a LEFT JOIN to <c>Attendance</c>, so every enrolled student
    /// appears whether or not anyone has marked them. The nullable members below are all
    /// that join -- they are null together, and together they mean "not marked yet".
    /// </summary>
    public class ClassAttendanceDTO
    {
        /// <summary>The <c>Attendance</c> row id, or null when there is no row yet.</summary>
        public int? Id { get; set; }

        public int StudentId { get; set; }

        /// <summary>
        /// Present, absent, or -- when null -- undecided. The third state exists only here,
        /// on the way out: <c>Attendance.IsPresent</c> is <c>NOT NULL</c>, so there is no way
        /// to store "no decision" and no way to send one back. Once a student is marked they
        /// can be corrected but not returned to unmarked.
        /// </summary>
        public bool? IsPresent { get; set; }

        public string? Remarks { get; set; }
        public DateTime? MarkedAt { get; set; }

        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Nullable because <c>Students.RollNumber</c> is. The procedure orders by
        /// <c>TRY_CONVERT(INT, RollNumber)</c> then the string, so unnumbered students sort
        /// together at the top rather than being dropped.
        /// </summary>
        public string? RollNumber { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
