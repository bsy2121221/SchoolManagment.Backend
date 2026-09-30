namespace SchoolManagment.Models.DTOs.Attendance
{
    /// <summary>
    /// One row per active student, from <c>sp_GetAttendanceSummary</c>.
    ///
    /// Driven from <c>Students</c> rather than from <c>Attendance</c>, which is what makes a
    /// student with an untouched register appear at all -- with a null percentage rather than
    /// a zero. Those are different facts and the type keeps them apart.
    /// </summary>
    public class AttendanceSummaryDTO
    {
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Nullable because <c>Students.RollNumber</c> is: a student admitted but not yet
        /// given a roll number has none.
        /// </summary>
        public string? RollNumber { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// The student's class, or null when they are not in one.
        ///
        /// This was <c>int</c>, and that was a fault rather than a style choice.
        /// <c>Students.ClassId</c> is nullable and the procedure reads the class through a
        /// <c>LEFT JOIN</c>, so <c>c.Id</c> comes back NULL for an admitted-but-unplaced
        /// student -- and Dapper throws when it has to put NULL into a non-nullable value
        /// type. One student without a class therefore failed the entire summary for the
        /// school with a <c>DataException</c>, not with a missing row.
        ///
        /// Callers need it to mark attendance, so a null here means "this student cannot be
        /// marked until they are placed in a class", which is worth showing rather than
        /// hiding behind a 0.
        /// </summary>
        public int? ClassId { get; set; }

        /// <summary>Null alongside <see cref="ClassId"/>, from the same LEFT JOIN.</summary>
        public string? ClassName { get; set; }

        /// <inheritdoc cref="ClassName"/>
        public string? Grade { get; set; }

        /// <inheritdoc cref="ClassName"/>
        public string? Section { get; set; }

        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }

        /// <summary>
        /// Days with a record either way, which is the denominator of
        /// <see cref="AttendancePercentage"/> -- not the number of days the school was open.
        /// A register nobody opened is not counted as an absence.
        /// </summary>
        public int TotalDays { get; set; }

        /// <summary>
        /// Present as a percentage of <see cref="TotalDays"/>, or null when that is zero.
        /// Null is "nothing recorded in this window"; 0 is "marked absent every recorded
        /// day". Rendering the first as the second is the mistake this nullability exists
        /// to prevent.
        /// </summary>
        public decimal? AttendancePercentage { get; set; }
    }
}
