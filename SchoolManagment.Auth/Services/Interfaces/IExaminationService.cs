using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Examinations;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IExaminationService
    {
        /// <summary>
        /// Create or update, matched on (ExamName, ExamType, ClassId, SubjectId). The actor
        /// recorded in the audit trail comes from <c>ITenantContext.UserId</c>, not from the
        /// request.
        /// </summary>
        Task<(ProcResult Result, int ExaminationId, bool WasCreated)> CreateOrUpdateExaminationAsync(
            ExaminationCreateDTO examination
        );

        Task<List<ExaminationDTO>> GetExaminationsAsync(int? classId = null);

        Task<ExaminationDTO?> GetExaminationByIdAsync(int examinationId, bool includeInactive = false);

        /// <summary>Soft delete; the exam's results are deactivated with it.</summary>
        Task<ProcResult> DeleteExaminationAsync(int examinationId);

        Task<List<ExaminationResultsDTO>> GetExaminationResultsAsync(int examinationId);
    }
}
