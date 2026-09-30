using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Examinations;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    /// <summary>
    /// Examinations, over the four procedures in 10_Procs_Academics.sql.
    ///
    /// This repository was outside the original audit's list of broken ones because it compared
    /// against the literal <c>"Success"</c> and got that right -- see the remarks on
    /// <see cref="ProcResult"/>. What the audit's parameter-name matching could not see is that
    /// <c>GetExaminationByIdAsync</c> did not call a procedure at all: it carried an inline
    /// SELECT with unqualified table names, the fifth such find and exactly what §8 of
    /// FRONTEND_PLAN.md tells later phases to grep for. It now calls
    /// <c>sp_GetExaminationById</c>, added for the purpose.
    ///
    /// Two other repairs. The <c>dynamic</c> row reads are typed, for the reason
    /// <c>ProfilePictureRow</c> is typed in UserRepository -- a <c>dynamic</c> member access is
    /// unchecked and was the source of this project's standing CS8602 warnings. And the delete
    /// no longer collapses the procedure's explanation into a bool.
    /// </summary>
    public class ExaminationRepository : IExaminationRepository
    {
        private readonly IDbContext _dbContext;

        public ExaminationRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(ProcResult Result, int ExaminationId, bool WasCreated)> CreateOrUpdateExaminationAsync(
            int schoolId,
            ExaminationCreateDTO examination,
            int? createdBy = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ExamName", examination.ExamName, DbType.String, size: 100);
            parameters.Add("@ExamType", examination.ExamType, DbType.String, size: 50);
            parameters.Add("@ClassId", examination.ClassId, DbType.Int32);
            parameters.Add("@SubjectId", examination.SubjectId, DbType.Int32);
            // DbType.Date, not DateTime: the column is DATE, and an exam is on a day rather
            // than at an instant. Sending a time component would be truncated silently.
            parameters.Add("@ExamDate", examination.ExamDate.Date, DbType.Date);
            parameters.Add("@MaxMarks", examination.MaxMarks, DbType.Int32);
            parameters.Add("@PassingMarks", examination.PassingMarks, DbType.Int32);
            parameters.Add("@Duration", examination.Duration, DbType.Int32);
            parameters.Add("@CreatedBy", createdBy, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<UpsertRow>(
                "dbo.sp_CreateOrUpdateExamination",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var result = ProcResult.From(row?.Result);

            // Every failure branch selects an id of 0 (or the existing id, for the
            // marks-exceed-the-new-maximum refusal) and a null Operation, so there is nothing
            // to report beyond the reason, which rides in the ProcResult.
            if (!result.Success || row is null)
            {
                return (result, 0, false);
            }

            return (
                result,
                row.ExaminationId,
                string.Equals(row.Operation, CreatedOperation, StringComparison.OrdinalIgnoreCase)
            );
        }

        public async Task<List<ExaminationDTO>> GetExaminationsAsync(int schoolId, int? classId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            // Left NULL for "every class"; the procedure's filter is
            // (@ClassId IS NULL OR e.ClassId = @ClassId).
            parameters.Add("@ClassId", classId, DbType.Int32);

            var examinations = await connection.QueryAsync<ExaminationDTO>(
                "dbo.sp_GetExaminations",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return examinations.AsList();
        }

        public async Task<ExaminationDTO?> GetExaminationByIdAsync(
            int schoolId,
            int examinationId,
            bool includeInactive = false)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ExaminationId", examinationId, DbType.Int32);
            parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

            return await connection.QueryFirstOrDefaultAsync<ExaminationDTO>(
                "dbo.sp_GetExaminationById",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<ProcResult> DeleteExaminationAsync(int schoolId, int examinationId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ExaminationId", examinationId, DbType.Int32);

            var message = await connection.QueryFirstOrDefaultAsync<string>(
                "dbo.sp_DeleteExamination",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return ProcResult.From(message);
        }

        public async Task<List<ExaminationResultsDTO>> GetExaminationResultsAsync(
            int schoolId,
            int examinationId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ExaminationId", examinationId, DbType.Int32);

            var results = await connection.QueryAsync<ExaminationResultsDTO>(
                "dbo.sp_GetExaminationResults",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return results.AsList();
        }

        /// <summary>The procedure's word for the insert branch.</summary>
        private const string CreatedOperation = "Created";

        /// <summary>
        /// sp_CreateOrUpdateExamination's single row. <c>Operation</c> is null on every failure
        /// branch, which is why <c>WasCreated</c> is only read after the ProcResult succeeds.
        /// </summary>
        private sealed class UpsertRow
        {
            public int ExaminationId { get; set; }
            public string? Result { get; set; }
            public string? Operation { get; set; }
        }
    }
}
