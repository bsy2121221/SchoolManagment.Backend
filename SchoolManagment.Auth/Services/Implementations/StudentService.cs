using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class StudentService : IStudentService
    {
        /// <summary>
        /// What every write returns when the caller's token carries no school. A
        /// SuperAdmin who has not switched into a school is the usual case, and
        /// "Failed to update student" told them nothing about how to fix it.
        /// </summary>
        private static readonly ProcResult NoSchoolInScope = new(
            false,
            "No school is in scope for this request. Switch into a school before managing its students."
        );

        private readonly IStudentRepository _studentRepository;
        private readonly ITenantContext _tenantContext;

        public StudentService(IStudentRepository studentRepository, ITenantContext tenantContext)
        {
            _studentRepository = studentRepository;
            _tenantContext = tenantContext;
        }

        /// <summary>Who the procedures record in the audit trail.</summary>
        private int? ActorUserId => _tenantContext.UserId;

        public async Task<(ProcResult Result, StudentRegistrationResponseDTO? Student)> RegisterStudentAsync(
            StudentRegistrationDTO student
        )
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (NoSchoolInScope, null);
            }

            // Hash password if provided, otherwise use default
            var passwordHash = !string.IsNullOrEmpty(student.Password)
                ? PasswordHelper.HashPassword(student.Password)
                : PasswordHelper.HashPassword("Temp@123");

            return await _studentRepository.RegisterStudentAsync(
                _tenantContext.SchoolId.Value,
                student,
                passwordHash,
                ActorUserId
            );
        }

        public async Task<PaginatedResponse<StudentDTO>> GetAllStudentsAsync(
            int? classId = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20
        )
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new PaginatedResponse<StudentDTO>(new List<StudentDTO>(), 0, page, pageSize);
            }

            var (students, totalCount) = await _studentRepository.GetAllStudentsAsync(
                _tenantContext.SchoolId.Value,
                classId,
                isActive,
                searchTerm,
                page,
                pageSize
            );

            return new PaginatedResponse<StudentDTO>(students, totalCount, page, pageSize);
        }

        public async Task<StudentDTO?> GetStudentByIdAsync(int studentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _studentRepository.GetStudentByIdAsync(_tenantContext.SchoolId.Value, studentId);
        }

        public async Task<StudentProfileDTO?> GetStudentProfileAsync(int studentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _studentRepository.GetStudentProfileAsync(_tenantContext.SchoolId.Value, studentId);
        }

        public async Task<ProcResult> UpdateStudentAsync(int studentId, StudentUpdateDTO student)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _studentRepository.UpdateStudentAsync(
                _tenantContext.SchoolId.Value,
                studentId,
                student,
                ActorUserId
            );
        }

        public async Task<ProcResult> DeleteStudentAsync(int studentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _studentRepository.DeleteStudentAsync(
                _tenantContext.SchoolId.Value,
                studentId,
                ActorUserId
            );
        }

        public async Task<ProcResult> PromoteStudentAsync(int studentId, StudentPromoteDTO promotion)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _studentRepository.PromoteStudentAsync(
                _tenantContext.SchoolId.Value,
                studentId,
                promotion.NewClassId,
                promotion.AcademicYear,
                ActorUserId
            );
        }

        public async Task<ProcResult> AssignSubjectsToStudentAsync(
            int studentId,
            StudentSubjectAssignmentDTO assignment
        )
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _studentRepository.AssignSubjectsToStudentAsync(
                _tenantContext.SchoolId.Value,
                studentId,
                assignment.SubjectIds
            );
        }

        public async Task<List<SubjectDTO>> GetStudentSubjectsAsync(int studentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<SubjectDTO>();
            }

            return await _studentRepository.GetStudentSubjectsAsync(_tenantContext.SchoolId.Value, studentId);
        }

        public async Task<ProcResult> RemoveStudentSubjectAsync(int studentId, int subjectId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _studentRepository.RemoveStudentSubjectAsync(
                _tenantContext.SchoolId.Value,
                studentId,
                subjectId
            );
        }

        public async Task<List<StudentDTO>> GetStudentsByClassAsync(int classId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<StudentDTO>();
            }

            return await _studentRepository.GetStudentsByClassAsync(_tenantContext.SchoolId.Value, classId);
        }
    }
}
