using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Attendance;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IAttendanceService
    {
        Task<ProcResult> MarkAttendanceAsync(AttendanceMarkDTO attendance);

        Task<(ProcResult Result, AttendanceBulkMarkResponseDTO? Counts)> MarkAttendanceBulkAsync(
            AttendanceBulkMarkDTO bulkAttendance
        );

        Task<List<ClassAttendanceDTO>> GetClassAttendanceAsync(int classId, DateTime attendanceDate);

        Task<List<StudentAttendanceDTO>> GetStudentAttendanceAsync(
            int studentId,
            DateTime? startDate = null,
            DateTime? endDate = null
        );

        Task<List<AttendanceSummaryDTO>> GetAttendanceSummaryAsync(
            int? classId = null,
            DateTime? startDate = null,
            DateTime? endDate = null
        );

        Task<List<DailyAttendanceReportDTO>> GetDailyAttendanceReportAsync(
            int? classId = null,
            DateTime? startDate = null,
            DateTime? endDate = null
        );
    }
}
