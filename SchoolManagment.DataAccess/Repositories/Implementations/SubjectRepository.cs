using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Subjects;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class SubjectRepository : ISubjectRepository
    {
        private readonly IDbContext _dbContext;

        public SubjectRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(ProcResult Result, int? SubjectId)> CreateSubjectAsync(
            int schoolId, SubjectCreateDTO subject)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@SubjectName", subject.SubjectName, DbType.String);
            parameters.Add("@SubjectCode", subject.SubjectCode, DbType.String);
            parameters.Add("@Grade", subject.Grade, DbType.String);

            // sp_CreateSubject returns two columns, Result and SubjectId. This read was
            // QueryAsync<int> against that row, so Dapper took the first column -- the
            // string 'Success' -- and tried to make an int of it.
            var row = await connection.QueryFirstOrDefaultAsync<CreateSubjectRow>(
                "dbo.sp_CreateSubject",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return (ProcResult.From(row?.Result), row?.SubjectId);
        }

        public async Task<(List<SubjectDTO> Subjects, int TotalCount)> GetAllSubjectsAsync(
            int schoolId,
            string? grade = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20
        )
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Grade", grade, DbType.String);
            parameters.Add("@IsActive", isActive, DbType.Boolean);
            parameters.Add("@Page", page, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);

            using var multi = await connection.QueryMultipleAsync(
                "dbo.sp_GetAllSubjects",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var subjects = (await multi.ReadAsync<SubjectDTO>()).ToList();
            var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

            return (subjects, totalCount);
        }

        public async Task<SubjectDTO?> GetSubjectByIdAsync(int schoolId, int subjectId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@SubjectId", subjectId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<SubjectDTO>(
                "dbo.sp_GetSubjectById",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<SubjectDTO>> GetSubjectsByGradeAsync(int schoolId, string grade)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Grade", grade, DbType.String);

            // sp_GetSubjectsByGrade does not exist and never did. sp_GetSubjects takes
            // exactly these two parameters and returns that grade's live subjects, which
            // is what this method is for.
            var result = await connection.QueryAsync<SubjectDTO>(
                "dbo.sp_GetSubjects",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<ProcResult> UpdateSubjectAsync(int schoolId, int subjectId, SubjectUpdateDTO subject)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@SubjectId", subjectId, DbType.Int32);
            parameters.Add("@SubjectName", subject.SubjectName, DbType.String);
            parameters.Add("@SubjectCode", subject.SubjectCode, DbType.String);
            parameters.Add("@Grade", subject.Grade, DbType.String);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateSubject", parameters);
        }

        public async Task<ProcResult> UpdateSubjectStatusAsync(int schoolId, int subjectId, bool isActive)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@SubjectId", subjectId, DbType.Int32);
            parameters.Add("@IsActive", isActive, DbType.Boolean);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateSubjectStatus", parameters);
        }

        public async Task<ProcResult> DeleteSubjectAsync(int schoolId, int subjectId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@SubjectId", subjectId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_DeleteSubject", parameters);
        }

        /// <summary>
        /// Runs a procedure whose whole output is one <c>Result</c> column and reports what
        /// it said. All three write paths used to test that column for "successfully",
        /// which no procedure in this database has ever said -- they say 'Success' or
        /// 'Error: &lt;reason&gt;' -- so every one of them returned false on success and the
        /// API answered a completed update with "Failed to update subject".
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

        /// <summary>sp_CreateSubject's single row: the outcome, and the new id on success.</summary>
        private sealed class CreateSubjectRow
        {
            public string? Result { get; set; }

            /// <summary>Null on every failure branch -- the procedure selects it as NULL.</summary>
            public int? SubjectId { get; set; }
        }
    }
}
