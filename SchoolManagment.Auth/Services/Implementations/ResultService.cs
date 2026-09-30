using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Results;

namespace SchoolManagment.Auth.Services.Implementations
{
    /// <summary>
    /// Results service.
    ///
    /// <c>IHttpContextAccessor</c> and its private <c>GetCurrentUserId()</c> are gone, the same
    /// removal made in Teachers, Attendance and Examinations before it. Phase 10's note claimed
    /// Examinations was the last service holding a second reading of the claims; this one was
    /// holding it too, and the reason it was missed is worth recording — the grep was for
    /// <c>IHttpContextAccessor</c> in services whose repository had failed the parameter audit, and
    /// this repository passed. The actor and the role both come from <see cref="ITenantContext"/>.
    /// </summary>
    public class ResultService : IResultService
    {
        private readonly IResultRepository _resultRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly ITenantContext _tenantContext;

        private const string NoSchoolContext = "No school context on this token.";

        public ResultService(
            IResultRepository resultRepository,
            ITeacherRepository teacherRepository,
            ITenantContext tenantContext)
        {
            _resultRepository = resultRepository;
            _teacherRepository = teacherRepository;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Whether this caller enters marks as an administrator rather than as a subject teacher.
        /// Administrators hold no <c>TeacherSubjectAssignments</c> rows, so the procedure's
        /// assignment check can only ever refuse them; a SuperAdmin who has switched into a school
        /// is in the same position.
        /// </summary>
        private bool ActsAsAdmin =>
            _tenantContext.IsSuperAdmin ||
            string.Equals(_tenantContext.Role, Constants.Roles.Admin, StringComparison.OrdinalIgnoreCase);

        public async Task<ProcResult> AddOrUpdateResultAsync(ResultEntryDTO result)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new ProcResult(false, NoSchoolContext);
            }

            return await _resultRepository.AddOrUpdateResultAsync(
                _tenantContext.SchoolId.Value,
                result.StudentId,
                result.ExaminationId,
                result.ObtainedMarks,
                result.Grade,
                result.Remarks);
        }

        public async Task<(ProcResult Result, BulkGradeEntryResponseDTO Counts)> BulkGradeEntryAsync(
            BulkGradeEntryDTO bulkEntry)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (new ProcResult(false, NoSchoolContext), new BulkGradeEntryResponseDTO());
            }

            return await _resultRepository.BulkGradeEntryAsync(
                _tenantContext.SchoolId.Value,
                bulkEntry.ExaminationId,
                bulkEntry.GradeEntries);
        }

        public async Task<List<ResultDTO>> GetStudentResultsAsync(int studentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ResultDTO>();
            }

            return await _resultRepository.GetStudentResultsAsync(
                _tenantContext.SchoolId.Value, studentId);
        }

        public async Task<(bool Authorised, List<StudentForGradeEntryDTO> Students)>
            GetStudentsForGradeEntryAsync(int subjectId, int classId, int? examinationId = null)
        {
            if (!_tenantContext.SchoolId.HasValue || !_tenantContext.UserId.HasValue)
            {
                return (false, new List<StudentForGradeEntryDTO>());
            }

            var schoolId = _tenantContext.SchoolId.Value;
            var actsAsAdmin = ActsAsAdmin;

            /* An admin needs no teacher row, and looking one up would fail for most of them.
               A teacher without one cannot be checked against the assignment table at all, which
               is a refusal rather than an empty roll. */
            var teacherId = 0;

            if (!actsAsAdmin)
            {
                var resolved = await _teacherRepository.GetTeacherIdByUserIdAsync(
                    schoolId, _tenantContext.UserId.Value);

                if (!resolved.HasValue)
                {
                    return (false, new List<StudentForGradeEntryDTO>());
                }

                teacherId = resolved.Value;
            }

            return await _resultRepository.GetStudentsForGradeEntryAsync(
                schoolId, teacherId, subjectId, classId, examinationId, actsAsAdmin);
        }
    }
}
