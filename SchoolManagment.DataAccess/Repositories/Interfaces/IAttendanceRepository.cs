using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Attendance;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface IAttendanceRepository
    {
        /// <summary>
        /// One student, one date. <c>sp_MarkAttendance</c> is a MERGE, so this both
        /// creates and corrects; its one refusal is a student who is not enrolled in the
        /// class given, which is worth passing on verbatim.
        /// </summary>
        Task<ProcResult> MarkAttendanceAsync(
            int schoolId,
            int studentId,
            int classId,
            DateTime attendanceDate,
            bool isPresent,
            string? remarks,
            int markedBy
        );

        /// <summary>
        /// A whole register in one transaction. The counts are only meaningful on success,
        /// so they are nullable and arrive beside the outcome rather than inside it.
        /// </summary>
        Task<(ProcResult Result, AttendanceBulkMarkResponseDTO? Counts)> MarkAttendanceBulkAsync(
            int schoolId,
            int classId,
            DateTime attendanceDate,
            List<AttendanceRecordDTO> records,
            int markedBy
        );

        Task<List<ClassAttendanceDTO>> GetClassAttendanceAsync(
            int schoolId,
            int classId,
            DateTime attendanceDate
        );

        Task<List<StudentAttendanceDTO>> GetStudentAttendanceAsync(
            int schoolId,
            int studentId,
            DateTime? startDate = null,
            DateTime? endDate = null
        );

        Task<List<AttendanceSummaryDTO>> GetAttendanceSummaryAsync(
            int schoolId,
            int? classId = null,
            DateTime? startDate = null,
            DateTime? endDate = null
        );

        Task<List<DailyAttendanceReportDTO>> GetDailyAttendanceReportAsync(
            int schoolId,
            int? classId = null,
            DateTime? startDate = null,
            DateTime? endDate = null
        );
    }
}
