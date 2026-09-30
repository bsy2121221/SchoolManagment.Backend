using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Subjects;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface ISubjectRepository
    {
        /// <summary>
        /// Creates a subject, or reactivates the soft-deleted one already holding the code.
        /// The id is null on every failure, and the result carries the procedure's reason --
        /// which is usually the duplicate code, and worth repeating to the user verbatim.
        /// </summary>
        Task<(ProcResult Result, int? SubjectId)> CreateSubjectAsync(int schoolId, SubjectCreateDTO subject);

        Task<(List<SubjectDTO> Subjects, int TotalCount)> GetAllSubjectsAsync(
            int schoolId,
            string? grade = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20
        );

        Task<SubjectDTO?> GetSubjectByIdAsync(int schoolId, int subjectId);

        /// <summary>Active subjects for one grade. Unpaged: it backs pickers, not a list screen.</summary>
        Task<List<SubjectDTO>> GetSubjectsByGradeAsync(int schoolId, string grade);

        Task<ProcResult> UpdateSubjectAsync(int schoolId, int subjectId, SubjectUpdateDTO subject);
        Task<ProcResult> UpdateSubjectStatusAsync(int schoolId, int subjectId, bool isActive);
        Task<ProcResult> DeleteSubjectAsync(int schoolId, int subjectId);
    }
}
