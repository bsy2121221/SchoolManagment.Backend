using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Results;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface IResultRepository
    {
        Task<ProcResult> AddOrUpdateResultAsync(
            int schoolId,
            int studentId,
            int examinationId,
            int obtainedMarks,
            string? grade = null,
            string? remarks = null
        );

        /// <summary>
        /// The procedure's verdict and its counts as two values, because they are two things: a
        /// batch can succeed having skipped rows, and it can be refused outright. The response DTO
        /// no longer carries a <c>Result</c> string for the verdict to hide inside.
        /// </summary>
        Task<(ProcResult Result, BulkGradeEntryResponseDTO Counts)> BulkGradeEntryAsync(
            int schoolId,
            int examinationId,
            List<GradeEntryRecordDTO> gradeEntries
        );

        Task<List<ResultDTO>> GetStudentResultsAsync(int schoolId, int studentId);

        /// <summary>
        /// The class roll for one examination, with any marks already entered.
        ///
        /// <paramref name="isAdmin"/> bypasses the procedure's <c>TeacherSubjectAssignments</c>
        /// check, which an administrator has no rows in and so could never satisfy.
        ///
        /// <c>Authorised</c> is false when a teacher is not assigned to this subject and class. The
        /// procedure signals that with <c>RAISERROR</c>, and translating the resulting
        /// <c>SqlException</c> here rather than in the controller keeps the message-matching next to
        /// the SQL that produced the message — the controller used to do it, which meant an
        /// unrelated database error whose text happened to contain "not authorized" would have been
        /// reported as a 403. An empty roll is <c>Authorised = true</c> with no students, which is a
        /// different thing and now distinguishable.
        /// </summary>
        Task<(bool Authorised, List<StudentForGradeEntryDTO> Students)> GetStudentsForGradeEntryAsync(
            int schoolId,
            int teacherId,
            int subjectId,
            int classId,
            int? examinationId = null,
            bool isAdmin = false
        );
    }
}
