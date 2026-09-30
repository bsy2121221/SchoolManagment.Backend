using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Attendance;

namespace SchoolManagment.Auth.Services.Implementations
{
    /// <summary>
    /// Attendance. Every method is scoped by <c>ITenantContext.SchoolId</c> and the two
    /// writes are attributed to <c>ITenantContext.UserId</c>.
    ///
    /// This class used to read the actor out of <c>IHttpContextAccessor</c> with
    /// <c>FindFirst("UserId")</c> -- the same thing TeacherService did before Phase 6.
    /// <c>ITenantContext</c> already resolves that claim once per request and is what every
    /// other service uses, so the accessor was a second, private copy of one rule: a
    /// service that reads claims directly can disagree with the tenant scope it is
    /// enforcing, and nothing would say so.
    /// </summary>
    public class AttendanceService : IAttendanceService
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ITenantContext _tenantContext;

        public AttendanceService(
            IAttendanceRepository attendanceRepository,
            ITenantContext tenantContext)
        {
            _attendanceRepository = attendanceRepository;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// The two things every write here needs. A SuperAdmin who has not switched into a
        /// school has no <c>SchoolId</c>, which is a refusal rather than an empty result:
        /// marking attendance against no school is not a thing to let succeed.
        /// </summary>
        private bool TryGetActor(out int schoolId, out int userId, out string error)
        {
            schoolId = 0;
            userId = 0;

            if (!_tenantContext.SchoolId.HasValue)
            {
                error = "School context not found";
                return false;
            }

            if (!_tenantContext.UserId.HasValue)
            {
                error = "User context not found";
                return false;
            }

            schoolId = _tenantContext.SchoolId.Value;
            userId = _tenantContext.UserId.Value;
            error = string.Empty;
            return true;
        }

        public async Task<ProcResult> MarkAttendanceAsync(AttendanceMarkDTO attendance)
        {
            if (!TryGetActor(out var schoolId, out var markedBy, out var error))
            {
                return new ProcResult(false, error);
            }

            return await _attendanceRepository.MarkAttendanceAsync(
                schoolId,
                attendance.StudentId,
                attendance.ClassId,
                attendance.AttendanceDate,
                attendance.IsPresent,
                attendance.Remarks,
                markedBy
            );
        }

        public async Task<(ProcResult Result, AttendanceBulkMarkResponseDTO? Counts)> MarkAttendanceBulkAsync(
            AttendanceBulkMarkDTO bulkAttendance)
        {
            if (!TryGetActor(out var schoolId, out var markedBy, out var error))
            {
                return (new ProcResult(false, error), null);
            }

            return await _attendanceRepository.MarkAttendanceBulkAsync(
                schoolId,
                bulkAttendance.ClassId,
                bulkAttendance.AttendanceDate,
                bulkAttendance.Records,
                markedBy
            );
        }

        public async Task<List<ClassAttendanceDTO>> GetClassAttendanceAsync(
            int classId, DateTime attendanceDate)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ClassAttendanceDTO>();
            }

            return await _attendanceRepository.GetClassAttendanceAsync(
                _tenantContext.SchoolId.Value,
                classId,
                attendanceDate
            );
        }

        public async Task<List<StudentAttendanceDTO>> GetStudentAttendanceAsync(
            int studentId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<StudentAttendanceDTO>();
            }

            return await _attendanceRepository.GetStudentAttendanceAsync(
                _tenantContext.SchoolId.Value,
                studentId,
                startDate,
                endDate
            );
        }

        public async Task<List<AttendanceSummaryDTO>> GetAttendanceSummaryAsync(
            int? classId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<AttendanceSummaryDTO>();
            }

            return await _attendanceRepository.GetAttendanceSummaryAsync(
                _tenantContext.SchoolId.Value,
                classId,
                startDate,
                endDate
            );
        }

        public async Task<List<DailyAttendanceReportDTO>> GetDailyAttendanceReportAsync(
            int? classId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<DailyAttendanceReportDTO>();
            }

            return await _attendanceRepository.GetDailyAttendanceReportAsync(
                _tenantContext.SchoolId.Value,
                classId,
                startDate,
                endDate
            );
        }
    }
}
