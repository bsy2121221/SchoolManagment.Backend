namespace SchoolManagment.Models.DTOs.Examinations
{
    /// <summary>
    /// One examination, as returned by <c>sp_GetExaminations</c> and
    /// <c>sp_GetExaminationById</c>. Both procedures select the same column list, so a list
    /// row and a single read cannot disagree.
    ///
    /// An examination is scoped to a class <em>and</em> a subject, both <c>NOT NULL</c>: there
    /// is no school-wide or class-wide exam in this schema. "Term 1 Maths for 10-A" is the unit,
    /// and "Term 1" across six subjects is six rows sharing a name.
    /// </summary>
    public class ExaminationDTO
    {
        public int Id { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public int MaxMarks { get; set; }
        public int PassingMarks { get; set; }

        /// <summary>Minutes. Nullable in the column and genuinely optional.</summary>
        public int? Duration { get; set; }

        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;

        /// <summary>
        /// Nullable because <c>Subjects.SubjectCode</c> is: rows predating
        /// <c>sp_CreateSubject</c> can lack one, though every write path now requires it.
        /// </summary>
        public string? SubjectCode { get; set; }

        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// The <em>class's</em> grade, not a letter grade. <c>Classes.Grade</c> and
        /// <c>Classes.Section</c> are both <c>NOT NULL</c>, so neither needs to be nullable
        /// here -- unlike the class columns on <c>AttendanceSummaryDTO</c>, which arrive
        /// through a LEFT JOIN.
        /// </summary>
        public string Grade { get; set; } = string.Empty;

        public string Section { get; set; } = string.Empty;
        public int SchoolId { get; set; }

        /// <summary>
        /// Active students in the exam's class -- the denominator for marking progress.
        /// Counted at read time rather than stored, so it follows transfers and admissions.
        /// </summary>
        public int StudentCount { get; set; }

        /// <summary>
        /// Active results recorded against this exam.
        ///
        /// Two jobs. It is the numerator of "12 of 30 marked", and it is what a delete
        /// confirmation needs: <c>sp_DeleteExamination</c> deactivates every result along with
        /// the exam, so this is the count of marks that disappear with it.
        ///
        /// It can exceed <see cref="StudentCount"/>, and that is not a bug: a student marked
        /// for this exam and later moved to another class still has a result, but no longer
        /// counts towards the class. Callers computing a percentage have to clamp.
        /// </summary>
        public int ResultsEntered { get; set; }
    }
}
