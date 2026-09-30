using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Teachers;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class TeacherService : ITeacherService
    {
        /// <summary>
        /// What every write returns when the caller's token carries no school, as in
        /// StudentService. A SuperAdmin who has not switched into a school is the usual
        /// case, and "Failed to update teacher" told them nothing about how to fix it.
        /// </summary>
        private static readonly ProcResult NoSchoolInScope = new(
            false,
            "No school is in scope for this request. Switch into a school before managing its teachers."
        );

        /// <summary>
        /// Only reachable when a Teacher-role token is missing its UserId claim, which
        /// would be a token-issuing bug rather than anything the caller did wrong -- but it
        /// still has to say something other than "Failed to update profile".
        /// </summary>
        private static readonly ProcResult NoUserInScope = new(
            false,
            "Your sign-in carries no user identity, so the profile it belongs to cannot be found. Sign in again."
        );

        /// <summary>The default handed to a new teacher, changed on first sign-in.</summary>
        private const string DefaultPassword = "Temp@123";

        private readonly ITeacherRepository _teacherRepository;
        private readonly ITenantContext _tenantContext;

        public TeacherService(ITeacherRepository teacherRepository, ITenantContext tenantContext)
        {
            _teacherRepository = teacherRepository;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Who the procedures record in the audit trail. Read from the tenant context
        /// rather than from IHttpContextAccessor, which is where the other services read
        /// it and which needs no null-checking of HttpContext.
        /// </summary>
        private int? ActorUserId => _tenantContext.UserId;

        public async Task<(ProcResult Result, TeacherRegistrationResponseDTO? Teacher)> RegisterTeacherAsync(
            TeacherRegistrationDTO teacher)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (NoSchoolInScope, null);
            }

            // sp_RegisterTeacher defaults the hash itself, but only to the same value; this
            // keeps the hashing in one place rather than half here and half in SQL.
            var passwordHash = PasswordHelper.HashPassword(DefaultPassword);

            return await _teacherRepository.RegisterTeacherAsync(
                _tenantContext.SchoolId.Value,
                teacher,
                passwordHash,
                ActorUserId
            );
        }

        public async Task<List<TeacherDTO>> GetAllTeachersAsync(int? subjectId = null)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<TeacherDTO>();
            }

            return await _teacherRepository.GetAllTeachersAsync(_tenantContext.SchoolId.Value, subjectId);
        }

        public async Task<TeacherProfileDTO?> GetTeacherProfileAsync(int userId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            var profile = await _teacherRepository.GetTeacherProfileAsync(_tenantContext.SchoolId.Value, userId);

            if (profile != null)
            {
                // Two procedures rather than one because the figures are expensive and the
                // details are not; they are stitched here so the client gets one object.
                profile.Stats = await _teacherRepository.GetTeacherProfileStatsAsync(
                    _tenantContext.SchoolId.Value,
                    userId
                );
            }

            return profile;
        }

        public async Task<ProcResult> UpdateTeacherAsync(int teacherId, TeacherUpdateDTO teacher)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _teacherRepository.UpdateTeacherAsync(
                _tenantContext.SchoolId.Value,
                teacherId,
                teacher,
                ActorUserId
            );
        }

        public async Task<ProcResult> UpdateTeacherProfileAsync(TeacherUpdateDTO teacher)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            var userId = ActorUserId;
            if (!userId.HasValue)
            {
                return NoUserInScope;
            }

            return await _teacherRepository.UpdateTeacherProfileAsync(
                _tenantContext.SchoolId.Value,
                userId.Value,
                teacher
            );
        }

        public async Task<ProcResult> DeleteTeacherAsync(int teacherId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _teacherRepository.DeleteTeacherAsync(
                _tenantContext.SchoolId.Value,
                teacherId,
                ActorUserId
            );
        }

        public async Task<(ProcResult Result, int SubjectsAssigned)> AssignSubjectsToTeacherAsync(
            int teacherId,
            List<int> subjectIds)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (NoSchoolInScope, 0);
            }

            return await _teacherRepository.AssignSubjectsToTeacherAsync(
                _tenantContext.SchoolId.Value,
                teacherId,
                subjectIds
            );
        }

        public async Task<List<TeacherSubjectDTO>> GetTeacherSubjectsAsync(int teacherId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<TeacherSubjectDTO>();
            }

            return await _teacherRepository.GetTeacherSubjectsAsync(_tenantContext.SchoolId.Value, teacherId);
        }

        public async Task<List<TeacherClassDTO>> GetTeacherClassesAsync(int teacherId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<TeacherClassDTO>();
            }

            return await _teacherRepository.GetTeacherClassesAsync(_tenantContext.SchoolId.Value, teacherId);
        }

        public async Task<List<TeacherSubjectClassDTO>> GetTeacherSubjectAssignmentsAsync(int teacherId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<TeacherSubjectClassDTO>();
            }

            return await _teacherRepository.GetTeacherSubjectAssignmentsAsync(_tenantContext.SchoolId.Value, teacherId);
        }

        public async Task<(ProcResult Result, int? AssignmentId)> AssignTeacherToSubjectClassAsync(
            int teacherId,
            int subjectId,
            int classId,
            bool isActive = true)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (NoSchoolInScope, null);
            }

            return await _teacherRepository.AssignTeacherToSubjectClassAsync(
                _tenantContext.SchoolId.Value,
                teacherId,
                subjectId,
                classId,
                isActive
            );
        }
    }
}
