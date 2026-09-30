using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Examinations;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface IExaminationRepository
    {
        /// <summary>
        /// One upsert, matched on (ExamName, ExamType, ClassId, SubjectId) by
        /// <c>sp_CreateOrUpdateExamination</c> -- not on an id, which the procedure never takes.
        /// </summary>
        /// <returns>
        /// The outcome, the id (the existing one on an update, 0 on failure), and whether a row
        /// was inserted. <c>WasCreated</c> comes from the procedure's <c>Operation</c> column
        /// rather than from a follow-up read, so the caller can answer 201 or 200 honestly.
        /// </returns>
        Task<(ProcResult Result, int ExaminationId, bool WasCreated)> CreateOrUpdateExaminationAsync(
            int schoolId,
            ExaminationCreateDTO examination,
            int? createdBy = null
        );

        Task<List<ExaminationDTO>> GetExaminationsAsync(int schoolId, int? classId = null);

        /// <param name="includeInactive">
        /// Soft-deleted examinations are hidden by default. Pass true to read one anyway --
        /// for an audit trail, or to tell a stale link that the exam was deleted rather than
        /// answering 404 as though it never existed.
        /// </param>
        Task<ExaminationDTO?> GetExaminationByIdAsync(
            int schoolId,
            int examinationId,
            bool includeInactive = false
        );

        /// <summary>
        /// Soft delete. Returns the procedure's reason rather than a bool: its one refusal is
        /// an examination that is not this school's or is already deleted, and "Failed to
        /// delete examination" does not distinguish those from a fault.
        /// </summary>
        Task<ProcResult> DeleteExaminationAsync(int schoolId, int examinationId);

        Task<List<ExaminationResultsDTO>> GetExaminationResultsAsync(int schoolId, int examinationId);
    }
}
