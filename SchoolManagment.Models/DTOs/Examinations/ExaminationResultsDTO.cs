namespace SchoolManagment.Models.DTOs.Examinations
{
    /// <summary>
    /// One row of the mark sheet from <c>sp_GetExaminationResults</c>.
    ///
    /// The procedure is driven from <c>Students</c> with a LEFT JOIN to <c>Results</c>, so this
    /// is the whole class rather than the students who have marks: an unmarked student appears
    /// with <see cref="ObtainedMarks"/>, <see cref="Percentage"/>, <see cref="Grade"/> and
    /// <see cref="Remarks"/> all null. That is the useful behaviour -- the sheet doubles as the
    /// list of who is still outstanding -- but it puts a trap in
    /// <see cref="IsPass"/>, documented there.
    /// </summary>
    public class ExaminationResultsDTO
    {
        /// <summary><c>Students.Id</c>, which is what the results endpoints take.</summary>
        public int StudentId { get; set; }

        /// <summary>The human-readable admission number, <c>Students.StudentId</c>. NOT NULL.</summary>
        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Nullable, because <c>Students.RollNumber</c> is <c>NVARCHAR(10) NULL</c> -- a student
        /// can be enrolled before a roll number is assigned. The procedure orders by
        /// <c>TRY_CONVERT(INT, RollNumber)</c>, so those rows sort together at the top of the
        /// roll-number tiebreak rather than being dropped.
        /// </summary>
        public string? RollNumber { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>Null when this student has no active result for the exam.</summary>
        public int? ObtainedMarks { get; set; }

        /// <summary>
        /// Both come from the examination row rather than the result, so every row on one
        /// sheet carries the same pair. Non-nullable: the columns are <c>NOT NULL</c>, and an
        /// examination that does not exist yields no rows at all rather than rows with nulls.
        /// </summary>
        public int MaxMarks { get; set; }

        public int PassingMarks { get; set; }

        /// <summary>Null exactly when <see cref="ObtainedMarks"/> is.</summary>
        public decimal? Percentage { get; set; }

        /// <summary>The letter grade, as entered. Optional even on a marked result.</summary>
        public string? Grade { get; set; }

        /// <summary>
        /// Read this together with <see cref="ObtainedMarks"/>, never on its own.
        ///
        /// The procedure computes <c>CASE WHEN r.ObtainedMarks &gt;= @PassingMarks THEN 1 ELSE
        /// 0 END</c>, and <c>NULL &gt;= 40</c> is unknown, so it falls to the ELSE. An
        /// <em>unmarked</em> student therefore arrives with <c>IsPass = false</c>, which a
        /// display that trusts this field alone will render as "Fail" -- a student nobody has
        /// marked yet shown as having failed. The fix belongs in the reader: when
        /// <see cref="ObtainedMarks"/> is null there is no pass or fail to report.
        /// </summary>
        public bool IsPass { get; set; }

        public string? Remarks { get; set; }

        /// <summary>
        /// <c>RANK()</c>, so ties share a position and the next position skips -- two students
        /// at rank 1 are followed by rank 3, not rank 2. <c>long</c> because <c>RANK()</c>
        /// returns <c>BIGINT</c>.
        ///
        /// Unmarked students are sorted last deliberately (the window function orders on
        /// "is null" first), but they all tie on that last rank, so the number is a position
        /// in the marked cohort and not a standing among peers.
        /// </summary>
        public long ClassRank { get; set; }
    }
}
