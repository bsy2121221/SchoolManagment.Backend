namespace SchoolManagment.Models.DTOs.Results
{
    /// <summary>
    /// One row of the grade-entry screen: a student on the examination's class roll, with
    /// whatever mark they already have.
    ///
    /// Driven from <c>Students</c> with a LEFT JOIN to <c>Results</c>, so every student on the
    /// roll appears whether or not anyone has marked them. That is the point of the screen, and
    /// it is why the three <c>Current*</c> properties are nullable.
    /// </summary>
    public class StudentForGradeEntryDTO
    {
        /// <summary>
        /// <c>Students.Id</c> — the id <c>sp_BulkGradeEntry</c> and <c>sp_AddOrUpdateResult</c>
        /// both take. Not <c>Users.Id</c>, and not the human-readable
        /// <see cref="StudentNumber"/>.
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// <c>Students.StudentId</c>, the school's own admission number. A string, and not the
        /// key for any write — see <see cref="StudentId"/>.
        /// </summary>
        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Nullable because <c>Students.RollNumber</c> is: a student can be enrolled and placed
        /// in a class before a roll number is assigned. The procedure orders by it, so those
        /// students sort first.
        /// </summary>
        public string? RollNumber { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Class details. Not nullable: Classes.Grade and Section are NOT NULL, and the join to
        // Classes is INNER.
        public string ClassName { get; set; } = string.Empty;
        public string ClassGrade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;

        // Subject details.
        public string SubjectName { get; set; } = string.Empty;

        /// <summary>Nullable because <c>Subjects.SubjectCode</c> is.</summary>
        public string? SubjectCode { get; set; }

        /// <summary>
        /// The mark already recorded, or <c>null</c> if this student has not been marked.
        ///
        /// This property used to be a non-nullable <c>int</c> fed by
        /// <c>COALESCE(r.ObtainedMarks, 0)</c>, which made "not marked" and "scored zero"
        /// the same value. On an entry grid that is actively destructive rather than merely
        /// misleading: every unmarked row renders pre-filled with 0, and a teacher who marks
        /// half a class and saves the screen writes a zero for everyone else. Read it with
        /// <see cref="HasResult"/>.
        /// </summary>
        public int? CurrentMarks { get; set; }

        /// <summary>
        /// The letter grade already recorded, or <c>null</c>. Note that it can be null even for
        /// a marked student: <c>fn_CalculateGrade</c> returns NULL when MaxMarks is zero or
        /// absent, so marks without a letter are possible.
        /// </summary>
        public string? CurrentGrade { get; set; }

        public string? CurrentRemarks { get; set; }

        /// <summary>
        /// Whether a <c>Results</c> row exists for this student and examination. The
        /// authoritative answer to "has this student been marked?" — <see cref="CurrentMarks"/>
        /// answers it too, but only because it is now nullable, and this says so without
        /// relying on that.
        /// </summary>
        public bool HasResult { get; set; }

        // Examination details. Non-nullable now that the procedure refuses to return a roll for
        // an examination that does not exist; it used to invent MaxMarks = 100 in that case.
        public int MaxMarks { get; set; }
        public int PassingMarks { get; set; }
        public int ExaminationId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
    }
}
