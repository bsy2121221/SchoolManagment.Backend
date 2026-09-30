using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Results
{
    public class BulkGradeEntryDTO
    {
        [Required(ErrorMessage = "Examination ID is required")]
        public int ExaminationId { get; set; }

        [Required(ErrorMessage = "At least one grade entry is required")]
        [MinLength(1, ErrorMessage = "At least one grade entry is required")]
        public List<GradeEntryRecordDTO> GradeEntries { get; set; } = new();
    }

    public class GradeEntryRecordDTO
    {
        [Required(ErrorMessage = "Student ID is required")]
        public int StudentId { get; set; }

        /// <summary>
        /// The mark. Required, and deliberately not nullable: an entry with no mark is not a
        /// request to clear the mark, it is a row the caller should not have sent. There is no
        /// way to *remove* a result through this API — see <c>sp_BulkGradeEntry</c>, whose MERGE
        /// has no DELETE branch — so omitting a student is how you leave them unmarked.
        /// </summary>
        [Required(ErrorMessage = "Obtained marks is required")]
        [Range(0, 1000, ErrorMessage = "Obtained marks must be between 0 and 1000")]
        public int ObtainedMarks { get; set; }

        /// <summary>
        /// Leave this null. <c>sp_BulkGradeEntry</c> reads it as
        /// <c>COALESCE(NULLIF(Grade, ''), fn_CalculateGrade(...))</c>, and that function applies
        /// *this school's* thresholds from its own Settings rows. Sending a letter therefore
        /// overrides the school's grading scale for that one mark, which is how a report card ends
        /// up with two students on the same percentage holding different grades. The only reason
        /// the field exists is to let a caller record a grade awarded outside the scale.
        /// </summary>
        [StringLength(5, ErrorMessage = "Grade cannot exceed 5 characters")]
        public string? Grade { get; set; }

        [StringLength(255, ErrorMessage = "Remarks cannot exceed 255 characters")]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// What a bulk submission actually did.
    ///
    /// The <c>Result</c> string this used to carry is gone, as Teachers', Parents' and
    /// Attendance's were before it: the procedure's success-or-refusal now travels as a
    /// <c>ProcResult</c> and the response DTO carries only the counts. Keeping both meant two
    /// channels for the same answer, and callers picked whichever one they happened to read.
    ///
    /// <see cref="EntriesSaved"/> + <see cref="EntriesSkipped"/> now always equals the number of
    /// entries sent. It did not before: an entry with a missing mark was filtered out before the
    /// requested count was taken, so it appeared in neither total and the caller was told the
    /// batch had succeeded.
    /// </summary>
    public class BulkGradeEntryResponseDTO
    {
        /// <summary>Rows inserted or updated.</summary>
        public int EntriesSaved { get; set; }

        /// <summary>
        /// Rows sent and not saved — the student is not in the examination's class, the mark falls
        /// outside 0..MaxMarks, or the entry had no student id or no mark. The procedure does not
        /// say which, so a caller reporting this can only report the count.
        /// </summary>
        public int EntriesSkipped { get; set; }
    }
}
