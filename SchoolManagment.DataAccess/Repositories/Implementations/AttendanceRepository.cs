using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Attendance;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    /// <summary>
    /// Attendance, over the six procedures in 09_Procs_Attendance.sql.
    ///
    /// Every call site here matched <c>sys.parameters</c> as found, and there was no
    /// inline SQL -- the two things the §8 audit and its Phase 7 correction look for. What
    /// needed repairing was the same defect Teachers had: the two writes returned a bare
    /// string and a DTO carrying its own <c>Result</c>, so the procedures' refusals had no
    /// typed channel to travel in.
    /// </summary>
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly IDbContext _dbContext;

        public AttendanceRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// The record list is passed as JSON and read by <c>OPENJSON ... WITH</c>, whose
        /// paths are the literals <c>'$.StudentId'</c>, <c>'$.IsPresent'</c> and
        /// <c>'$.Remarks'</c> -- and JSON property matching in SQL Server is
        /// case-sensitive regardless of the database collation.
        ///
        /// So the naming policy is pinned rather than left to the default. A camelCase
        /// policy arriving later would not fail: every path would match nothing,
        /// <c>StudentId</c> would come back NULL, the procedure's
        /// <c>WHERE j.StudentId IS NOT NULL</c> would discard the lot, and the register
        /// would report <c>Success</c> with zero rows marked. Silent, and only visible as
        /// attendance that quietly stopped saving.
        /// </summary>
        private static readonly JsonSerializerOptions RecordJsonOptions = new()
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        };

        public async Task<ProcResult> MarkAttendanceAsync(
            int schoolId,
            int studentId,
            int classId,
            DateTime attendanceDate,
            bool isPresent,
            string? remarks,
            int markedBy)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            // DbType.Date, not DateTime: the procedure's parameter is DATE and the unique
            // key is (SchoolId, StudentId, AttendanceDate). Sending a time component would
            // be silently truncated by SQL Server, but being explicit here keeps the
            // truncation from being the thing that makes the key work.
            parameters.Add("@AttendanceDate", attendanceDate.Date, DbType.Date);
            parameters.Add("@IsPresent", isPresent, DbType.Boolean);
            parameters.Add("@Remarks", remarks, DbType.String);
            parameters.Add("@MarkedBy", markedBy, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_MarkAttendance", parameters);
        }

        public async Task<(ProcResult Result, AttendanceBulkMarkResponseDTO? Counts)> MarkAttendanceBulkAsync(
            int schoolId,
            int classId,
            DateTime attendanceDate,
            List<AttendanceRecordDTO> records,
            int markedBy)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@AttendanceDate", attendanceDate.Date, DbType.Date);
            parameters.Add(
                "@Records",
                JsonSerializer.Serialize(records, RecordJsonOptions),
                DbType.String,
                size: -1 // NVARCHAR(MAX); a class of 60 with remarks exceeds the 4000 default.
            );
            parameters.Add("@MarkedBy", markedBy, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<BulkMarkRow>(
                "dbo.sp_MarkAttendanceBulk",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var result = ProcResult.From(row?.Result);

            // Every failure branch selects both counts as 0, so there is nothing to report
            // on one -- the reason (invalid JSON, or a class that is not this school's)
            // rides in the ProcResult.
            if (!result.Success || row is null)
            {
                return (result, null);
            }

            return (
                result,
                new AttendanceBulkMarkResponseDTO
                {
                    RecordsMarked = row.RecordsMarked,
                    RecordsSkipped = row.RecordsSkipped,
                }
            );
        }

        public async Task<List<ClassAttendanceDTO>> GetClassAttendanceAsync(
            int schoolId,
            int classId,
            DateTime attendanceDate)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@AttendanceDate", attendanceDate.Date, DbType.Date);

            var attendance = await connection.QueryAsync<ClassAttendanceDTO>(
                "dbo.sp_GetClassAttendance",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return attendance.AsList();
        }

        public async Task<List<StudentAttendanceDTO>> GetStudentAttendanceAsync(
            int schoolId,
            int studentId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            // Left NULL when the caller gave no window: the procedure then defaults to the
            // last month, which is a better answer than this layer inventing its own.
            parameters.Add("@StartDate", startDate?.Date, DbType.Date);
            parameters.Add("@EndDate", endDate?.Date, DbType.Date);

            var attendance = await connection.QueryAsync<StudentAttendanceDTO>(
                "dbo.sp_GetStudentAttendance",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return attendance.AsList();
        }

        public async Task<List<AttendanceSummaryDTO>> GetAttendanceSummaryAsync(
            int schoolId,
            int? classId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@StartDate", startDate?.Date, DbType.Date);
            parameters.Add("@EndDate", endDate?.Date, DbType.Date);

            var summary = await connection.QueryAsync<AttendanceSummaryDTO>(
                "dbo.sp_GetAttendanceSummary",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return summary.AsList();
        }

        public async Task<List<DailyAttendanceReportDTO>> GetDailyAttendanceReportAsync(
            int schoolId,
            int? classId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@StartDate", startDate?.Date, DbType.Date);
            parameters.Add("@EndDate", endDate?.Date, DbType.Date);

            var report = await connection.QueryAsync<DailyAttendanceReportDTO>(
                "dbo.sp_GetDailyAttendanceReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return report.AsList();
        }

        /// <summary>
        /// For the one write whose whole answer is the <c>Result</c> column. Mirrors the
        /// helper of the same name in UserRepository and ClassRepository.
        /// </summary>
        private static async Task<ProcResult> ExecuteReportingAsync(
            IDbConnection connection, string procedure, DynamicParameters parameters)
        {
            var message = await connection.QueryFirstOrDefaultAsync<string>(
                procedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return ProcResult.From(message);
        }

        /// <summary>
        /// sp_MarkAttendanceBulk's single row. Typed rather than <c>dynamic</c>, for the
        /// same reason ProfilePictureRow is in UserRepository: a <c>dynamic</c> member
        /// access is unchecked at compile time and was where this project's standing
        /// CS8602 warnings came from.
        /// </summary>
        private sealed class BulkMarkRow
        {
            public string? Result { get; set; }
            public int RecordsMarked { get; set; }
            public int RecordsSkipped { get; set; }
        }
    }
}
