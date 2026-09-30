using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Subjects;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// What a write is answered with when the token carries no school. It reaches a
        /// platform administrator who has not switched into one, and it names the fix,
        /// because "Failed to create subject" for a request that never touched the
        /// database sends the reader looking at their own input.
        /// </summary>
        private static readonly ProcResult NoSchoolInScope = new(
            false,
            "No school is in scope for this request. Switch into a school before managing its subjects."
        );

        public SubjectService(ISubjectRepository subjectRepository, ITenantContext tenantContext)
        {
            _subjectRepository = subjectRepository;
            _tenantContext = tenantContext;
        }

        public async Task<(ProcResult Result, int? SubjectId)> CreateSubjectAsync(SubjectCreateDTO subject)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (NoSchoolInScope, null);
            }

            return await _subjectRepository.CreateSubjectAsync(_tenantContext.SchoolId.Value, subject);
        }

        public async Task<PaginatedResponse<SubjectDTO>> GetAllSubjectsAsync(
            string? grade = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20
        )
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new PaginatedResponse<SubjectDTO>(new List<SubjectDTO>(), 0, page, pageSize);
            }

            var (subjects, totalCount) = await _subjectRepository.GetAllSubjectsAsync(
                _tenantContext.SchoolId.Value,
                grade,
                isActive,
                page,
                pageSize
            );

            return new PaginatedResponse<SubjectDTO>(subjects, totalCount, page, pageSize);
        }

        public async Task<SubjectDTO?> GetSubjectByIdAsync(int subjectId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _subjectRepository.GetSubjectByIdAsync(_tenantContext.SchoolId.Value, subjectId);
        }

        public async Task<List<SubjectDTO>> GetSubjectsByGradeAsync(string grade)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<SubjectDTO>();
            }

            return await _subjectRepository.GetSubjectsByGradeAsync(_tenantContext.SchoolId.Value, grade);
        }

        public async Task<ProcResult> UpdateSubjectAsync(int subjectId, SubjectUpdateDTO subject)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _subjectRepository.UpdateSubjectAsync(_tenantContext.SchoolId.Value, subjectId, subject);
        }

        public async Task<ProcResult> UpdateSubjectStatusAsync(int subjectId, bool isActive)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _subjectRepository.UpdateSubjectStatusAsync(_tenantContext.SchoolId.Value, subjectId, isActive);
        }

        public async Task<ProcResult> DeleteSubjectAsync(int subjectId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _subjectRepository.DeleteSubjectAsync(_tenantContext.SchoolId.Value, subjectId);
        }
    }
}
