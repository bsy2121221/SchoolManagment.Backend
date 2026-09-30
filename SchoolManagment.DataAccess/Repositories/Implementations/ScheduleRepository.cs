using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.DTOs.Schedule;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly IDbContext _dbContext;

        public ScheduleRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ScheduleOperationResponseDTO> CreateOrUpdateScheduleEntryAsync(
            int schoolId,
            ScheduleEntryCreateDTO entry,
            int? id = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new
            {
                SchoolId = schoolId,
                Id = id,
                entry.TeacherId,
                entry.SubjectId,
                entry.ClassId,
                entry.DayOfWeek,
                StartTime = entry.StartTime,
                EndTime = entry.EndTime,
                entry.Room
            };

            var result = await connection.QueryFirstOrDefaultAsync<ScheduleOperationResponseDTO>(
                "sp_CreateOrUpdateScheduleEntry",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result ?? new ScheduleOperationResponseDTO { Result = "Error", Message = "Operation failed" };
        }

        public async Task<List<ScheduleEntryDTO>> GetTeacherScheduleAsync(int schoolId, int teacherId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new
            {
                SchoolId = schoolId,
                TeacherId = teacherId
            };

            var schedule = await connection.QueryAsync<ScheduleEntryDTO>(
                "sp_GetTeacherSchedule",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return schedule.ToList();
        }

        public async Task<List<ScheduleEntryDTO>> GetTeacherScheduleByDayAsync(int schoolId, int teacherId, int dayOfWeek)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new
            {
                SchoolId = schoolId,
                TeacherId = teacherId,
                DayOfWeek = dayOfWeek
            };

            var schedule = await connection.QueryAsync<ScheduleEntryDTO>(
                "sp_GetTeacherScheduleByDay",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return schedule.ToList();
        }

        public async Task<CurrentNextClassesResponseDTO> GetTeacherCurrentAndNextClassesAsync(int schoolId, int teacherId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new
            {
                SchoolId = schoolId,
                TeacherId = teacherId
            };

            using var multi = await connection.QueryMultipleAsync(
                "sp_GetTeacherCurrentAndNextClasses",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var currentClass = await multi.ReadFirstOrDefaultAsync<CurrentNextClassDTO>();
            var nextClass = await multi.ReadFirstOrDefaultAsync<CurrentNextClassDTO>();

            return new CurrentNextClassesResponseDTO
            {
                CurrentClass = currentClass,
                NextClass = nextClass
            };
        }

        public async Task<TeacherScheduleStatsDTO> GetTeacherScheduleStatsAsync(int schoolId, int teacherId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new
            {
                SchoolId = schoolId,
                TeacherId = teacherId
            };

            var stats = await connection.QueryFirstOrDefaultAsync<TeacherScheduleStatsDTO>(
                "sp_GetTeacherScheduleStats",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return stats ?? new TeacherScheduleStatsDTO();
        }

        public async Task<List<ClassScheduleDTO>> GetClassScheduleAsync(int schoolId, int classId, int? dayOfWeek = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new
            {
                SchoolId = schoolId,
                ClassId = classId,
                DayOfWeek = dayOfWeek
            };

            var schedule = await connection.QueryAsync<ClassScheduleDTO>(
                "sp_GetClassSchedule",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return schedule.ToList();
        }

        public async Task<(string Result, string Message)> DeleteScheduleEntryAsync(int schoolId, int id)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new
            {
                SchoolId = schoolId,
                Id = id
            };

            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_DeleteScheduleEntry",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return (
                Result: result?.Result ?? "Error",
                Message: result?.Message ?? "Operation failed"
            );
        }
    }
}
