using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Students;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class StudentRepository : IStudentRepository
    {
        private readonly IDbContext _dbContext;

        public StudentRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(ProcResult Result, StudentRegistrationResponseDTO? Student)> RegisterStudentAsync(
            int schoolId,
            StudentRegistrationDTO student,
            string passwordHash,
            int? performedByUserId = null
        )
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FirstName", student.FirstName, DbType.String);
            parameters.Add("@LastName", student.LastName, DbType.String);
            parameters.Add("@Email", student.Email, DbType.String);
            parameters.Add("@PhoneNumber", student.PhoneNumber, DbType.String);
            parameters.Add("@DateOfBirth", student.DateOfBirth, DbType.Date);
            parameters.Add("@Gender", student.Gender, DbType.String);
            parameters.Add("@ClassId", student.ClassId, DbType.Int32);
            parameters.Add("@FatherName", student.FatherName, DbType.String);
            parameters.Add("@MotherName", student.MotherName, DbType.String);
            parameters.Add("@BloodGroup", student.BloodGroup, DbType.String);
            parameters.Add("@Address", student.Address, DbType.String);
            parameters.Add("@PasswordHash", passwordHash, DbType.String);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            /* Read into RegisterStudentRow, not straight onto the DTO. The
               procedure's StudentId column is the NVARCHAR(40) admission number
               while the DTO's StudentId is the int record id, so mapping by name
               put a string into an int and threw on every registration -- and
               the Result column, which is where "Class 10A is full" lives, was
               dropped on the floor. */
            var row = await connection.QueryFirstOrDefaultAsync<RegisterStudentRow>(
                "dbo.sp_RegisterStudent",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var result = ProcResult.From(row?.Result);

            if (!result.Success || row is null)
            {
                return (result, null);
            }

            return (
                result,
                new StudentRegistrationResponseDTO
                {
                    StudentId = row.StudentRecordId ?? 0,
                    UserId = row.UserId ?? 0,
                    Username = row.Username ?? string.Empty,
                    StudentIdNumber = row.StudentId ?? string.Empty,
                    RollNumber = row.RollNumber,
                    // Null for a student admitted without a class, which the
                    // procedure allows: they are enrolled, not yet placed.
                    ClassName = row.ClassName ?? string.Empty
                }
            );
        }

        public async Task<(List<StudentDTO> Students, int TotalCount)> GetAllStudentsAsync(
            int schoolId,
            int? classId = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20
        )
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@IsActive", isActive, DbType.Boolean);
            parameters.Add("@SearchTerm", searchTerm, DbType.String);
            parameters.Add("@Page", page, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);

            using var multi = await connection.QueryMultipleAsync(
                "dbo.sp_GetAllStudents",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var students = (await multi.ReadAsync<StudentDTO>()).ToList();
            var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

            return (students, totalCount);
        }

        public async Task<StudentDTO?> GetStudentByIdAsync(int schoolId, int studentId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<StudentDTO>(
                "dbo.sp_GetStudentById",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<StudentProfileDTO?> GetStudentProfileAsync(int schoolId, int studentId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);

            using var multi = await connection.QueryMultipleAsync(
                "dbo.sp_GetStudentProfile",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var studentInfo = await multi.ReadFirstOrDefaultAsync<StudentDTO>();
            if (studentInfo == null)
                return null;

            var academicStats = await multi.ReadFirstOrDefaultAsync<AcademicStatsDTO>();
            var subjects = (await multi.ReadAsync<SubjectDTO>()).ToList();
            var feeStatus = await multi.ReadFirstOrDefaultAsync<FeeStatusDTO>();

            return new StudentProfileDTO
            {
                StudentInfo = studentInfo,
                AcademicStats = academicStats,
                Subjects = subjects,
                FeeStatus = feeStatus
            };
        }

        public async Task<ProcResult> UpdateStudentAsync(
            int schoolId,
            int studentId,
            StudentUpdateDTO student,
            int? performedByUserId = null
        )
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@FirstName", student.FirstName, DbType.String);
            parameters.Add("@LastName", student.LastName, DbType.String);
            parameters.Add("@Email", student.Email, DbType.String);
            parameters.Add("@PhoneNumber", student.PhoneNumber, DbType.String);
            parameters.Add("@DateOfBirth", student.DateOfBirth, DbType.Date);
            parameters.Add("@Gender", student.Gender, DbType.String);
            parameters.Add("@FatherName", student.FatherName, DbType.String);
            parameters.Add("@MotherName", student.MotherName, DbType.String);
            parameters.Add("@BloodGroup", student.BloodGroup, DbType.String);
            parameters.Add("@Address", student.Address, DbType.String);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateStudent", parameters);
        }

        public async Task<ProcResult> DeleteStudentAsync(
            int schoolId,
            int studentId,
            int? performedByUserId = null
        )
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_DeleteStudent", parameters);
        }

        public async Task<ProcResult> PromoteStudentAsync(
            int schoolId,
            int studentId,
            int newClassId,
            int academicYear,
            int? performedByUserId = null
        )
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@NewClassId", newClassId, DbType.Int32);
            parameters.Add("@AcademicYear", academicYear, DbType.Int32);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_PromoteStudent", parameters);
        }

        public async Task<ProcResult> AssignSubjectsToStudentAsync(
            int schoolId,
            int studentId,
            List<int> subjectIds
        )
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            // The procedure takes a comma-separated list and splits it itself.
            parameters.Add("@SubjectIds", string.Join(",", subjectIds), DbType.String);

            // Returns Result, Message and SubjectsAssigned; Result is the column
            // ProcResult reads, and the count is available if a caller wants it.
            return await ExecuteReportingAsync(connection, "dbo.sp_AssignSubjectsToStudent", parameters);
        }

        public async Task<List<SubjectDTO>> GetStudentSubjectsAsync(int schoolId, int studentId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);

            var result = await connection.QueryAsync<SubjectDTO>(
                "dbo.sp_GetStudentSubjects",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<ProcResult> RemoveStudentSubjectAsync(int schoolId, int studentId, int subjectId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@SubjectId", subjectId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_RemoveStudentSubject", parameters);
        }

        public async Task<List<StudentDTO>> GetStudentsByClassAsync(int schoolId, int classId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);

            var result = await connection.QueryAsync<StudentDTO>(
                "dbo.sp_GetStudentsByClass",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        /// <summary>
        /// Runs a procedure whose whole output is one <c>Result</c> column. Five
        /// methods here used to test <c>message.Contains("successfully")</c>,
        /// which the procedures never say: the work happened and the API reported
        /// failure. See <see cref="ProcResult"/>.
        /// </summary>
        private static async Task<ProcResult> ExecuteReportingAsync(
            IDbConnection connection,
            string procedure,
            DynamicParameters parameters
        )
        {
            var message = await connection.QueryFirstOrDefaultAsync<string>(
                procedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return ProcResult.From(message);
        }

        /// <summary>
        /// sp_RegisterStudent's row, column for column. Every field is nullable
        /// because the refusal paths select NULLs alongside the reason.
        /// </summary>
        private sealed class RegisterStudentRow
        {
            public string? Result { get; set; }
            public int? UserId { get; set; }
            public int? StudentRecordId { get; set; }
            public string? Username { get; set; }

            /// <summary>The human-readable admission number, not the record id.</summary>
            public string? StudentId { get; set; }

            public string? RollNumber { get; set; }
            public string? ClassName { get; set; }
        }
    }
}
