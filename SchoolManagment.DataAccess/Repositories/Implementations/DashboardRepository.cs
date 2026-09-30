using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.DTOs.Dashboard;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbContext _dbContext;

        public DashboardRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SchoolStatsDTO?> GetSchoolStatsAsync(int schoolId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);

            // The procedure's column names match this DTO's properties one for one,
            // including the three it gained after the original seven, so there is no
            // projection to keep in step.
            return await connection.QueryFirstOrDefaultAsync<SchoolStatsDTO>(
                "dbo.sp_GetDashboardStats",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<ActivityDTO>> GetActivitiesAsync(int? schoolId, int userId, int topCount)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@TopCount", topCount, DbType.Int32);

            var activities = await connection.QueryAsync<ActivityDTO>(
                "dbo.sp_GetProfileActivities",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return activities.ToList();
        }

        public async Task<StudentSectionDTO?> GetStudentSectionAsync(int schoolId, int userId)
        {
            using var connection = _dbContext.CreateConnection();

            // sp_GetStudentProfileStats resolves the student from the user id itself but
            // returns only totals, so the identity has to be read separately. Inline,
            // the way TeacherRepository.GetTeacherIdByUserIdAsync reads a teacher id --
            // a two-column lookup does not earn a stored procedure.
            //
            // LEFT JOIN because Students.ClassId is nullable: a student admitted but not
            // yet placed in a class still has a row, and dropping them here would make
            // them look deleted.
            const string identitySql = @"
                SELECT s.Id          AS StudentId,
                       s.StudentId   AS StudentNumber,
                       s.RollNumber,
                       s.ClassId,
                       c.ClassName
                FROM dbo.Students AS s
                LEFT JOIN dbo.Classes AS c
                       ON c.SchoolId = s.SchoolId AND c.Id = s.ClassId
                WHERE s.SchoolId = @SchoolId
                  AND s.UserId   = @UserId
                  AND s.IsActive = 1;";

            var section = await connection.QueryFirstOrDefaultAsync<StudentSectionDTO>(
                identitySql,
                new { SchoolId = schoolId, UserId = userId }
            );

            if (section == null)
                return null;

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);

            var stats = await connection.QueryFirstOrDefaultAsync<StudentStatsRow>(
                "dbo.sp_GetStudentProfileStats",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (stats != null)
            {
                section.PresentDays = stats.PresentDays;
                section.AbsentDays = stats.AbsentDays;
                section.TotalDays = stats.TotalDays;
                section.TotalResults = stats.TotalResults;
                section.AverageMarks = stats.AverageMarks;
                section.HighestMarks = stats.HighestMarks;
                section.TotalFees = stats.TotalFees;
                section.PaidAmount = stats.PaidAmount;
                section.PendingAmount = stats.PendingAmount;
                section.SubjectCount = stats.SubjectCount;
            }

            return section;
        }

        /// <summary>
        /// sp_GetStudentProfileStats' single row. Kept separate from
        /// <see cref="StudentSectionDTO"/> so the identity read cannot be silently
        /// overwritten by a NULL from the stats read -- AverageMarks and HighestMarks are
        /// NULL for a student with no results, and mapping both result sets onto one
        /// object would let the second one blank the first one's ClassId.
        /// </summary>
        private sealed class StudentStatsRow
        {
            public int PresentDays { get; set; }
            public int AbsentDays { get; set; }
            public int TotalDays { get; set; }
            public int TotalResults { get; set; }
            public double? AverageMarks { get; set; }
            public int? HighestMarks { get; set; }
            public int TotalFees { get; set; }
            public decimal PaidAmount { get; set; }
            public decimal PendingAmount { get; set; }
            public int SubjectCount { get; set; }
        }
    }
}
