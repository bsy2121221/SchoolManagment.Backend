using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Subjects;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface ISubjectService
    {
        Task<(ProcResult Result, int? SubjectId)> CreateSubjectAsync(SubjectCreateDTO subject);

        Task<PaginatedResponse<SubjectDTO>> GetAllSubjectsAsync(
            string? grade = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20
        );

        Task<SubjectDTO?> GetSubjectByIdAsync(int subjectId);
        Task<List<SubjectDTO>> GetSubjectsByGradeAsync(string grade);
        Task<ProcResult> UpdateSubjectAsync(int subjectId, SubjectUpdateDTO subject);
        Task<ProcResult> UpdateSubjectStatusAsync(int subjectId, bool isActive);
        Task<ProcResult> DeleteSubjectAsync(int subjectId);
    }
}
