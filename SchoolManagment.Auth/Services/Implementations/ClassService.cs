using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Classes;
using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class ClassService : IClassService
    {
        private readonly IClassRepository _classRepository;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// What a write is answered with when the token carries no school. It reaches a
        /// platform administrator who has not called POST /api/Schools/{id}/switch, and it
        /// names the fix, because "Failed to create class" for a request that never
        /// touched the database sends the reader looking at their own input.
        /// </summary>
        private static readonly ProcResult NoSchoolInScope = new(
            false,
            "No school is in scope for this request. Switch into a school before managing its classes."
        );

        public ClassService(IClassRepository classRepository, ITenantContext tenantContext)
        {
            _classRepository = classRepository;
            _tenantContext = tenantContext;
        }

        public async Task<(ProcResult Result, int? ClassId)> CreateClassAsync(ClassCreateDTO classDto)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (NoSchoolInScope, null);
            }

            return await _classRepository.CreateClassAsync(_tenantContext.SchoolId.Value, classDto);
        }

        public async Task<PaginatedResponse<ClassDTO>> GetAllClassesAsync(
            string? grade = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20
        )
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new PaginatedResponse<ClassDTO>(new List<ClassDTO>(), 0, page, pageSize);
            }

            var (classes, totalCount) = await _classRepository.GetAllClassesAsync(
                _tenantContext.SchoolId.Value,
                grade,
                isActive,
                page,
                pageSize
            );

            return new PaginatedResponse<ClassDTO>(classes, totalCount, page, pageSize);
        }

        public async Task<ClassDTO?> GetClassByIdAsync(int classId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _classRepository.GetClassByIdAsync(_tenantContext.SchoolId.Value, classId);
        }

        public async Task<ClassDetailsDTO?> GetClassDetailsAsync(int classId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _classRepository.GetClassDetailsAsync(_tenantContext.SchoolId.Value, classId);
        }

        public async Task<ProcResult> UpdateClassAsync(int classId, ClassUpdateDTO classDto)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _classRepository.UpdateClassAsync(_tenantContext.SchoolId.Value, classId, classDto);
        }

        public async Task<ProcResult> UpdateClassStatusAsync(int classId, bool isActive)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _classRepository.UpdateClassStatusAsync(_tenantContext.SchoolId.Value, classId, isActive);
        }

        public async Task<ProcResult> DeleteClassAsync(int classId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _classRepository.DeleteClassAsync(_tenantContext.SchoolId.Value, classId);
        }

        public async Task<List<StudentDTO>> GetClassStudentsAsync(int classId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<StudentDTO>();
            }

            return await _classRepository.GetClassStudentsAsync(_tenantContext.SchoolId.Value, classId);
        }

        public async Task<ClassTimetableDTO?> GetClassTimetableAsync(int classId, int? dayOfWeek = null)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _classRepository.GetClassTimetableAsync(_tenantContext.SchoolId.Value, classId, dayOfWeek);
        }
    }
}
