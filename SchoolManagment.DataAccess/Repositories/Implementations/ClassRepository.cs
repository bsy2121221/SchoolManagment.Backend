using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Classes;
using SchoolManagment.Models.DTOs.Students;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class ClassRepository : IClassRepository
    {
        private readonly IDbContext _dbContext;

        public ClassRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(ProcResult Result, int? ClassId)> CreateClassAsync(
            int schoolId, ClassCreateDTO classDto)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassName", classDto.ClassName, DbType.String);
            parameters.Add("@Grade", classDto.Grade, DbType.String);
            parameters.Add("@Section", classDto.Section, DbType.String);
            parameters.Add("@ClassTeacherId", classDto.ClassTeacherId, DbType.Int32);
            parameters.Add("@MaxStudents", classDto.MaxStudents, DbType.Int32);

            // sp_CreateClass returns two columns, Result and ClassId. This read was
            // QueryAsync<int> against that row, so Dapper took the first column -- the
            // string 'Success' -- and tried to make an int of it.
            var row = await connection.QueryFirstOrDefaultAsync<CreateClassRow>(
                "dbo.sp_CreateClass",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return (ProcResult.From(row?.Result), row?.ClassId);
        }

        public async Task<(List<ClassDTO> Classes, int TotalCount)> GetAllClassesAsync(
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
                "dbo.sp_GetAllClasses",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var classes = (await multi.ReadAsync<ClassDTO>()).ToList();
            var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

            return (classes, totalCount);
        }

        public async Task<ClassDTO?> GetClassByIdAsync(int schoolId, int classId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);

            var result = await connection.QueryAsync<ClassDTO>(
                "dbo.sp_GetClassById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.FirstOrDefault();
        }

        public async Task<ClassDetailsDTO?> GetClassDetailsAsync(int schoolId, int classId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);

            using var multi = await connection.QueryMultipleAsync(
                "dbo.sp_GetClassDetails",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var classInfo = await multi.ReadFirstOrDefaultAsync<ClassDTO>();
            if (classInfo == null)
                return null;

            var students = (await multi.ReadAsync<StudentDTO>()).ToList();
            var stats = await multi.ReadFirstOrDefaultAsync<ClassStatsDTO>();

            return new ClassDetailsDTO
            {
                ClassInfo = classInfo,
                Students = students,
                Stats = stats
            };
        }

        public async Task<ProcResult> UpdateClassAsync(int schoolId, int classId, ClassUpdateDTO classDto)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@ClassName", classDto.ClassName, DbType.String);
            parameters.Add("@Grade", classDto.Grade, DbType.String);
            parameters.Add("@Section", classDto.Section, DbType.String);
            parameters.Add("@ClassTeacherId", classDto.ClassTeacherId, DbType.Int32);
            parameters.Add("@MaxStudents", classDto.MaxStudents, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateClass", parameters);
        }

        public async Task<ProcResult> UpdateClassStatusAsync(int schoolId, int classId, bool isActive)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@IsActive", isActive, DbType.Boolean);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateClassStatus", parameters);
        }

        public async Task<ProcResult> DeleteClassAsync(int schoolId, int classId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_DeleteClass", parameters);
        }

        public async Task<List<StudentDTO>> GetClassStudentsAsync(int schoolId, int classId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);

            // sp_GetStudentsByClass does not exist and never did -- the deployed
            // procedure is sp_GetClassStudents, so this endpoint returned a 500.
            var result = await connection.QueryAsync<StudentDTO>(
                "dbo.sp_GetClassStudents",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        /// <summary>
        /// Runs a procedure whose whole output is one <c>Result</c> column and reports what
        /// it said. Shared by update, status and delete because all three have the same
        /// contract, and because that is one place to be right rather than three.
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

        /// <summary>sp_CreateClass' single row: the outcome, and the new id on success.</summary>
        private sealed class CreateClassRow
        {
            public string? Result { get; set; }

            /// <summary>Null on every failure branch -- the procedure selects it as NULL.</summary>
            public int? ClassId { get; set; }
        }

        public async Task<ClassTimetableDTO?> GetClassTimetableAsync(int schoolId, int classId, int? dayOfWeek = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@DayOfWeek", dayOfWeek, DbType.Int32);

            var result = await connection.QueryAsync<dynamic>(
                "dbo.sp_GetClassSchedule",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var scheduleData = result.ToList();
            if (!scheduleData.Any())
                return null;

            var timetable = new ClassTimetableDTO
            {
                ClassName = scheduleData.First().ClassName
            };

            var groupedByDay = scheduleData.GroupBy(s => new { s.DayOfWeek, s.DayName });

            foreach (var dayGroup in groupedByDay)
            {
                var daySchedule = new DayScheduleDTO
                {
                    DayOfWeek = dayGroup.Key.DayOfWeek,
                    DayName = dayGroup.Key.DayName,
                    Periods = dayGroup.Select(p => new PeriodDTO
                    {
                        StartTime = p.StartTime.ToString(),
                        EndTime = p.EndTime.ToString(),
                        SubjectName = p.SubjectName,
                        TeacherName = p.TeacherName,
                        Room = p.Room
                    }).ToList()
                };

                timetable.Timetable.Add(daySchedule);
            }

            return timetable;
        }
    }
}
