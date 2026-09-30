using Dapper;
using Microsoft.Data.SqlClient;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Results;
using System.Data;
using System.Text.Json;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    /// <summary>
    /// Results data access. All four call sites matched <c>sys.parameters</c> and, per §8's Phase 7
    /// lesson, the class was grepped for inline <c>SELECT</c> as well: there was none. The defects
    /// were the two the Attendance repository had, in the same place and for the same reasons.
    ///
    /// <c>@GradeEntries</c> was passed as an anonymous object, so Dapper inferred
    /// <c>DbType.String</c> with its default size of 4000. A class of sixty with remarks
    /// serialises past that, SQL Server truncates silently, and the procedure's
    /// <c>ISJSON</c> guard then rejects the whole batch with "GradeEntries is not valid JSON" —
    /// an error naming the payload rather than its length. It is now <c>size: -1</c>.
    ///
    /// The serializer options are pinned. <c>OPENJSON</c> matches the literal paths
    /// <c>'$.StudentId'</c>, <c>'$.ObtainedMarks'</c>, <c>'$.Grade'</c> and <c>'$.Remarks'</c>
    /// case-sensitively regardless of database collation, and the old code relied on
    /// <c>JsonSerializer.Serialize</c>'s default of leaving property names alone. A camelCase
    /// policy arriving later — from a shared options instance, or a change of default — would not
    /// throw: every path would match nothing, the procedure's NULL filter would discard every row,
    /// and it would report <c>Success</c> with nothing written.
    /// </summary>
    public class ResultRepository : IResultRepository
    {
        private readonly IDbContext _dbContext;

        /// <summary>
        /// PascalCase, matching the <c>OPENJSON ... WITH</c> paths in <c>sp_BulkGradeEntry</c>
        /// exactly. A null naming policy means "use the property names as declared"; it is set
        /// explicitly rather than left to the default because the failure mode of getting it wrong
        /// is a silent no-op, not an exception.
        /// </summary>
        private static readonly JsonSerializerOptions GradeEntryJsonOptions = new()
        {
            PropertyNamingPolicy = null,
        };

        public ResultRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>The single-<c>Result</c> row every write procedure here returns.</summary>
        private sealed class ResultRow
        {
            public string? Result { get; set; }
        }

        /// <summary>
        /// <c>sp_BulkGradeEntry</c>'s row: the refusal channel and the two counts together, read
        /// into one type rather than into the response DTO, so <c>Result</c> has somewhere to go
        /// without the DTO having to carry it.
        /// </summary>
        private sealed class BulkGradeRow
        {
            public string? Result { get; set; }
            public int EntriesSaved { get; set; }
            public int EntriesSkipped { get; set; }
        }

        public async Task<ProcResult> AddOrUpdateResultAsync(
            int schoolId,
            int studentId,
            int examinationId,
            int obtainedMarks,
            string? grade = null,
            string? remarks = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@ExaminationId", examinationId, DbType.Int32);
            parameters.Add("@ObtainedMarks", obtainedMarks, DbType.Int32);
            parameters.Add("@Grade", grade, DbType.String, size: 5);
            parameters.Add("@Remarks", remarks, DbType.String, size: 255);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "sp_AddOrUpdateResult",
                parameters,
                commandType: CommandType.StoredProcedure);

            return ProcResult.From(row?.Result);
        }

        public async Task<(ProcResult Result, BulkGradeEntryResponseDTO Counts)> BulkGradeEntryAsync(
            int schoolId,
            int examinationId,
            List<GradeEntryRecordDTO> gradeEntries)
        {
            using var connection = _dbContext.CreateConnection();

            var gradeEntriesJson = JsonSerializer.Serialize(gradeEntries, GradeEntryJsonOptions);

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ExaminationId", examinationId, DbType.Int32);
            // size: -1 is NVARCHAR(MAX). Without it Dapper sends NVARCHAR(4000) and a full class
            // with remarks is cut mid-token.
            parameters.Add("@GradeEntries", gradeEntriesJson, DbType.String, size: -1);

            var row = await connection.QueryFirstOrDefaultAsync<BulkGradeRow>(
                "sp_BulkGradeEntry",
                parameters,
                commandType: CommandType.StoredProcedure);

            var result = ProcResult.From(row?.Result);

            // The counts are returned whether or not the procedure succeeded. On a refusal they
            // are both zero, which is true and is what the caller should report.
            return (result, new BulkGradeEntryResponseDTO
            {
                EntriesSaved = row?.EntriesSaved ?? 0,
                EntriesSkipped = row?.EntriesSkipped ?? 0,
            });
        }

        public async Task<List<ResultDTO>> GetStudentResultsAsync(int schoolId, int studentId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);

            var results = await connection.QueryAsync<ResultDTO>(
                "sp_GetStudentResults",
                parameters,
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }

        /// <summary>
        /// The text <c>sp_GetStudentsForGradeEntry</c> raises when a teacher holds no assignment for
        /// the subject and class. Matched here, beside the procedure, rather than in the controller.
        /// </summary>
        private const string NotAuthorisedFragment = "not authorized";

        public async Task<(bool Authorised, List<StudentForGradeEntryDTO> Students)>
            GetStudentsForGradeEntryAsync(
                int schoolId,
                int teacherId,
                int subjectId,
                int classId,
                int? examinationId = null,
                bool isAdmin = false)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@TeacherId", teacherId, DbType.Int32);
            parameters.Add("@SubjectId", subjectId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@ExaminationId", examinationId, DbType.Int32);
            parameters.Add("@IsAdmin", isAdmin, DbType.Boolean);

            try
            {
                var students = await connection.QueryAsync<StudentForGradeEntryDTO>(
                    "sp_GetStudentsForGradeEntry",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return (true, students.ToList());
            }
            catch (SqlException ex) when (
                ex.Message.Contains(NotAuthorisedFragment, StringComparison.OrdinalIgnoreCase))
            {
                // Only this one message is a refusal. Every other SqlException is a fault and is
                // left to propagate, so a broken procedure cannot masquerade as a permission
                // problem.
                return (false, new List<StudentForGradeEntryDTO>());
            }
        }
    }
}
