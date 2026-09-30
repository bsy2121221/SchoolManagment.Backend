namespace SchoolManagment.Models.DTOs.Results
{
    /// <summary>
    /// One mark a student has been given, with the examination, subject and class it belongs to.
    /// The report card is a list of these.
    ///
    /// Unlike <c>ExaminationResultsDTO</c>, which is driven from <c>Students</c> and therefore
    /// carries a row per *unmarked* student too, this comes from <c>Results</c> with INNER joins.
    /// Every row is a real mark, so nothing here is nullable for want of a result and
    /// <see cref="IsPass"/> can be trusted on its own.
    /// </summary>
    public class ResultDTO
    {
        /// <summary><c>Results.Id</c>. Not the key for any write — the upsert matches on
        /// (StudentId, ExaminationId).</summary>
        public int Id { get; set; }

        public int ObtainedMarks { get; set; }

        /// <summary>
        /// Nullable, and not only for old data: <c>fn_CalculateGrade</c> returns NULL when the
        /// examination's MaxMarks is zero or absent, and the API lets a caller pass an explicit
        /// grade or none at all.
        /// </summary>
        public string? Grade { get; set; }

        public string? Remarks { get; set; }

        /// <summary>When the mark was first entered.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When it was last corrected, or null if it has never been. The upsert sets this on
        /// every subsequent write, so a non-null value means the mark has been revised — which is
        /// the only trace of a correction the API exposes.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        // Examination details.
        public int ExaminationId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int MaxMarks { get; set; }
        public int PassingMarks { get; set; }
        public DateTime ExamDate { get; set; }

        /// <summary>
        /// Computed by the procedure, not the client. Two reasons: the report card averages these
        /// and a client recomputing per row would round differently from
        /// <c>sp_GetExaminationResults</c> on the same mark, so the same result would read
        /// differently on the mark sheet and the report card.
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Safe to read alone here, unlike <c>ExaminationResultsDTO.IsPass</c>. That one is false
        /// for unmarked students because <c>NULL &gt;= 40</c> is unknown in SQL; this procedure
        /// has no unmarked rows, so the comparison always has two real operands.
        /// </summary>
        public bool IsPass { get; set; }

        // Subject details.
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;

        /// <summary>Nullable because <c>Subjects.SubjectCode</c> is.</summary>
        public string? SubjectCode { get; set; }

        // Class details. Non-nullable: the join is INNER and Classes.Grade/Section are NOT NULL.
        // Note this is the class the *examination* was set for, which for an older result may not
        // be the class the student is in now.
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string ClassGrade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
    }
}
