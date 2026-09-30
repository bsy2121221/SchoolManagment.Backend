using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Teachers;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class TeacherRepository : ITeacherRepository
    {
        private readonly IDbContext _dbContext;

        public TeacherRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(ProcResult Result, TeacherRegistrationResponseDTO? Teacher)> RegisterTeacherAsync(
            int schoolId,
            TeacherRegistrationDTO teacher,
            string passwordHash,
            int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FirstName", teacher.FirstName, DbType.String);
            parameters.Add("@LastName", teacher.LastName, DbType.String);
            parameters.Add("@Email", teacher.Email, DbType.String);
            parameters.Add("@PhoneNumber", teacher.PhoneNumber, DbType.String);
            parameters.Add("@Address", teacher.Address, DbType.String);
            parameters.Add("@Subject", teacher.Subject, DbType.String);
            parameters.Add("@Qualification", teacher.Qualification, DbType.String);
            parameters.Add("@Experience", teacher.Experience, DbType.Int32);
            parameters.Add("@Salary", teacher.Salary, DbType.Decimal);
            parameters.Add("@SubjectIds", JoinIds(teacher.SubjectIds), DbType.String);
            parameters.Add("@PasswordHash", passwordHash, DbType.String);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<RegisterTeacherRow>(
                "dbo.sp_RegisterTeacher",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var result = ProcResult.From(row?.Result);

            // Every failure branch of the procedure selects the id columns as NULL, so
            // there is nothing to hand back on one. The reason -- a duplicate email, an
            // inactive school -- rides in the ProcResult.
            if (!result.Success || row is null)
            {
                return (result, null);
            }

            return (
                result,
                new TeacherRegistrationResponseDTO
                {
                    UserId = row.UserId ?? 0,
                    TeacherRecordId = row.TeacherRecordId ?? 0,
                    Username = row.Username ?? string.Empty,
                    EmployeeId = row.EmployeeId ?? string.Empty
                }
            );
        }

        public async Task<List<TeacherDTO>> GetAllTeachersAsync(int schoolId, int? subjectId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@SubjectId", subjectId, DbType.Int32);

            var teachers = await connection.QueryAsync<TeacherDTO>(
                "dbo.sp_GetTeachersWithDetails",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return teachers.ToList();
        }

        public async Task<TeacherProfileDTO?> GetTeacherProfileAsync(int schoolId, int userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<TeacherProfileDTO>(
                "dbo.sp_GetTeacherProfile",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<TeacherProfileStatsDTO?> GetTeacherProfileStatsAsync(int schoolId, int userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<TeacherProfileStatsDTO>(
                "dbo.sp_GetTeacherProfileStats",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<ProcResult> UpdateTeacherAsync(
            int schoolId,
            int teacherId,
            TeacherUpdateDTO teacher,
            int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@TeacherId", teacherId, DbType.Int32);
            parameters.Add("@FirstName", teacher.FirstName, DbType.String);
            parameters.Add("@LastName", teacher.LastName, DbType.String);
            parameters.Add("@Email", teacher.Email, DbType.String);
            parameters.Add("@PhoneNumber", teacher.PhoneNumber, DbType.String);
            parameters.Add("@Address", teacher.Address, DbType.String);
            parameters.Add("@Subject", teacher.Subject, DbType.String);
            parameters.Add("@Qualification", teacher.Qualification, DbType.String);
            parameters.Add("@Experience", teacher.Experience, DbType.Int32);
            parameters.Add("@Salary", teacher.Salary, DbType.Decimal);

            // Null leaves the subject links alone; a list replaces them wholesale, because
            // sp_UpdateTeacherDetails forwards it to sp_AssignSubjectsToTeacher. An empty
            // list from the caller is therefore sent as NULL rather than as "", which the
            // split would read as "assign nothing" and use to clear every link.
            parameters.Add("@SubjectIds", JoinIds(teacher.SubjectIds), DbType.String);

            // The procedure has always declared @ModifiedBy; this repository never passed
            // it, so every admin edit was attributed to nobody.
            parameters.Add("@ModifiedBy", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateTeacherDetails", parameters);
        }

        public async Task<ProcResult> UpdateTeacherProfileAsync(int schoolId, int userId, TeacherUpdateDTO teacher)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@FirstName", teacher.FirstName, DbType.String);
            parameters.Add("@LastName", teacher.LastName, DbType.String);
            parameters.Add("@Email", teacher.Email, DbType.String);
            parameters.Add("@PhoneNumber", teacher.PhoneNumber, DbType.String);
            parameters.Add("@Address", teacher.Address, DbType.String);
            parameters.Add("@Subject", teacher.Subject, DbType.String);
            parameters.Add("@Qualification", teacher.Qualification, DbType.String);
            parameters.Add("@Experience", teacher.Experience, DbType.Int32);

            // Salary and SubjectIds are deliberately absent: sp_UpdateTeacherProfile takes
            // neither, so a teacher cannot raise their own pay or hand themselves a subject.
            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateTeacherProfile", parameters);
        }

        public async Task<ProcResult> DeleteTeacherAsync(int schoolId, int teacherId, int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@TeacherId", teacherId, DbType.Int32);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_DeleteTeacher", parameters);
        }

        public async Task<(ProcResult Result, int SubjectsAssigned)> AssignSubjectsToTeacherAsync(
            int schoolId,
            int teacherId,
            List<int> subjectIds)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@TeacherId", teacherId, DbType.Int32);
            parameters.Add("@SubjectIds", string.Join(",", subjectIds), DbType.String);

            var row = await connection.QueryFirstOrDefaultAsync<AssignSubjectsRow>(
                "dbo.sp_AssignSubjectsToTeacher",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return (ProcResult.From(row?.Result), row?.SubjectsAssigned ?? 0);
        }

        public async Task<List<TeacherSubjectDTO>> GetTeacherSubjectsAsync(int schoolId, int teacherId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@TeacherId", teacherId, DbType.Int32);

            var subjects = await connection.QueryAsync<TeacherSubjectDTO>(
                "dbo.sp_GetTeacherSubjects",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return subjects.ToList();
        }

        public async Task<List<TeacherClassDTO>> GetTeacherClassesAsync(int schoolId, int teacherId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@TeacherId", teacherId, DbType.Int32);

            var classes = await connection.QueryAsync<TeacherClassDTO>(
                "dbo.sp_GetTeacherClasses",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return classes.ToList();
        }

        public async Task<List<TeacherSubjectClassDTO>> GetTeacherSubjectAssignmentsAsync(int schoolId, int teacherId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@TeacherId", teacherId, DbType.Int32);

            var assignments = await connection.QueryAsync<TeacherSubjectClassDTO>(
                "dbo.sp_GetTeacherSubjectAssignments",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return assignments.ToList();
        }

        public async Task<(ProcResult Result, int? AssignmentId)> AssignTeacherToSubjectClassAsync(
            int schoolId,
            int teacherId,
            int subjectId,
            int classId,
            bool isActive = true)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@TeacherId", teacherId, DbType.Int32);
            parameters.Add("@SubjectId", subjectId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);

            // The procedure is a MERGE on (Teacher, Subject, Class), so passing false is
            // how an assignment is withdrawn. There is no delete path.
            parameters.Add("@IsActive", isActive, DbType.Boolean);

            var row = await connection.QueryFirstOrDefaultAsync<AssignSubjectClassRow>(
                "dbo.sp_AssignTeacherToSubjectClass",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return (ProcResult.From(row?.Result), row?.AssignmentId);
        }

        public async Task<int?> GetTeacherIdByUserIdAsync(int schoolId, int userId)
        {
            using var connection = _dbContext.CreateConnection();

            const string sql = @"SELECT Id
                                 FROM dbo.Teachers
                                 WHERE SchoolId = @SchoolId AND UserId = @UserId AND IsActive = 1;";

            return await connection.QueryFirstOrDefaultAsync<int?>(
                sql,
                new { SchoolId = schoolId, UserId = userId }
            );
        }

        /// <summary>
        /// Comma-separates the ids for the procedures that take them as a string, and
        /// returns null for an absent or empty list so the receiving procedure takes its
        /// "leave the links alone" branch rather than its "replace with nothing" one.
        /// </summary>
        private static string? JoinIds(List<int>? ids) =>
            ids is { Count: > 0 } ? string.Join(",", ids) : null;

        /// <summary>
        /// Runs a procedure whose whole output is one <c>Result</c> column and reports
        /// what it said.
        ///
        /// These three writes did compare against the right token, so unlike Classes and
        /// Subjects they were never wrong about success. What they did was throw the
        /// sentence away: sp_DeleteTeacher refuses with "Cannot delete teacher who is
        /// assigned to active classes" or "...who has historical attendance data", two
        /// conditions with different remedies, and the API answered both with "Failed to
        /// delete teacher".
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
        /// sp_RegisterTeacher's single row. Read into this rather than onto the response
        /// DTO so that the outcome column has somewhere to land.
        /// </summary>
        private sealed class RegisterTeacherRow
        {
            public string? Result { get; set; }
            public int? UserId { get; set; }
            public int? TeacherRecordId { get; set; }
            public string? Username { get; set; }
            public string? EmployeeId { get; set; }
        }

        /// <summary>sp_AssignSubjectsToTeacher's row: the outcome and how many links survived.</summary>
        private sealed class AssignSubjectsRow
        {
            public string? Result { get; set; }
            public int? SubjectsAssigned { get; set; }
        }

        /// <summary>sp_AssignTeacherToSubjectClass's row: the outcome and the merged row's id.</summary>
        private sealed class AssignSubjectClassRow
        {
            public string? Result { get; set; }
            public int? AssignmentId { get; set; }
        }
    }
}
