using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.DTOs.Schools;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class SchoolRepository : ISchoolRepository
    {
        private readonly IDbContext _dbContext;

        public SchoolRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(List<SchoolListItemDTO> Schools, int TotalCount)> GetSchoolsAsync(
            string? searchTerm = null, bool? isActive = null, int page = 1, int pageSize = 50)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SearchTerm", searchTerm, DbType.String);
            parameters.Add("@IsActive", isActive, DbType.Boolean);
            parameters.Add("@PageNumber", page, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);

            // One result set, not two: sp_GetSchools carries the grand total in a
            // COUNT(*) OVER () column on every row rather than emitting a second
            // set the way sp_GetAllSubjects does.
            var rows = (await connection.QueryAsync<SchoolListRow>(
                "dbo.sp_GetSchools",
                parameters,
                commandType: CommandType.StoredProcedure
            )).ToList();

            var totalCount = rows.FirstOrDefault()?.TotalCount ?? 0;

            // Upcast rather than copied field by field. System.Text.Json serialises
            // a List<SchoolListItemDTO> against the declared element type, so
            // TotalCount does not leak onto each item in the response.
            return (rows.Cast<SchoolListItemDTO>().ToList(), totalCount);
        }

        public async Task<SchoolDTO?> GetSchoolByIdAsync(int schoolId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<SchoolDTO>(
                "dbo.sp_GetSchoolById",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<SchoolDTO?> GetSchoolByCodeAsync(string schoolCode)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolCode", schoolCode, DbType.String);

            return await connection.QueryFirstOrDefaultAsync<SchoolDTO>(
                "dbo.sp_GetSchoolByCode",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<SchoolBrandingDTO?> GetSchoolBySubdomainAsync(string subdomain)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Subdomain", subdomain, DbType.String);

            return await connection.QueryFirstOrDefaultAsync<SchoolBrandingDTO>(
                "dbo.sp_GetSchoolBySubdomain",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<SchoolBrandingDTO?> GetSchoolBrandingAsync(int? schoolId, string? schoolCode)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@SchoolCode", schoolCode, DbType.String);

            return await connection.QueryFirstOrDefaultAsync<SchoolBrandingDTO>(
                "dbo.sp_GetSchoolBranding",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<(bool Success, string Message, SchoolCreateResultDTO? Result)> CreateSchoolAsync(
            SchoolCreateDTO school, string? adminPasswordHash, int? createdByUserId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolCode", school.SchoolCode, DbType.String);
            parameters.Add("@SchoolName", school.SchoolName, DbType.String);
            parameters.Add("@Subdomain", school.Subdomain, DbType.String);
            parameters.Add("@Address", school.Address, DbType.String);
            parameters.Add("@City", school.City, DbType.String);
            parameters.Add("@State", school.State, DbType.String);
            parameters.Add("@Country", school.Country, DbType.String);
            parameters.Add("@PostalCode", school.PostalCode, DbType.String);
            parameters.Add("@ContactEmail", school.ContactEmail, DbType.String);
            parameters.Add("@ContactPhone", school.ContactPhone, DbType.String);
            parameters.Add("@PrincipalName", school.PrincipalName, DbType.String);
            parameters.Add("@LogoUrl", school.LogoUrl, DbType.String);
            parameters.Add("@ThemeColor", school.ThemeColor, DbType.String);
            parameters.Add("@AcademicYearStartMonth", school.AcademicYearStartMonth, DbType.Byte);
            parameters.Add("@AdminEmail", school.AdminEmail, DbType.String);
            parameters.Add("@AdminFirstName", school.AdminFirstName, DbType.String);
            parameters.Add("@AdminLastName", school.AdminLastName, DbType.String);
            parameters.Add("@AdminPhoneNumber", school.AdminPhoneNumber, DbType.String);
            parameters.Add("@AdminPasswordHash", adminPasswordHash, DbType.String);
            parameters.Add("@SeedSubjects", school.SeedSubjects, DbType.Boolean);
            parameters.Add("@CreatedByUserId", createdByUserId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<CreateSchoolRow>(
                "dbo.sp_CreateSchool",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var (success, message) = Interpret(row?.Result);
            if (!success || row?.SchoolId == null)
            {
                return (false, success ? "The school was not created." : message, null);
            }

            return (true, message, new SchoolCreateResultDTO
            {
                SchoolId = row.SchoolId.Value,
                SchoolCode = row.SchoolCode ?? school.SchoolCode,
                AdminUserId = row.AdminUserId ?? 0,
                AdminUsername = row.AdminUsername ?? string.Empty,

                // The procedure sets RequirePasswordChange exactly when it had to
                // supply the temporary password itself.
                RequiresPasswordChange = adminPasswordHash == null
            });
        }

        public async Task<(bool Success, string Message)> UpdateSchoolAsync(
            int schoolId, SchoolUpdateDTO school, int? updatedByUserId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@SchoolName", school.SchoolName, DbType.String);
            parameters.Add("@Subdomain", school.Subdomain, DbType.String);
            parameters.Add("@Address", school.Address, DbType.String);
            parameters.Add("@City", school.City, DbType.String);
            parameters.Add("@State", school.State, DbType.String);
            parameters.Add("@Country", school.Country, DbType.String);
            parameters.Add("@PostalCode", school.PostalCode, DbType.String);
            parameters.Add("@ContactEmail", school.ContactEmail, DbType.String);
            parameters.Add("@ContactPhone", school.ContactPhone, DbType.String);
            parameters.Add("@PrincipalName", school.PrincipalName, DbType.String);
            parameters.Add("@LogoUrl", school.LogoUrl, DbType.String);
            parameters.Add("@ThemeColor", school.ThemeColor, DbType.String);
            parameters.Add("@AcademicYearStartMonth", school.AcademicYearStartMonth, DbType.Byte);
            parameters.Add("@ClearSubdomain", school.ClearSubdomain, DbType.Boolean);
            parameters.Add("@UpdatedByUserId", updatedByUserId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "dbo.sp_UpdateSchool",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return Interpret(row?.Result);
        }

        public async Task<(bool Success, string Message, bool? IsActive)> ToggleSchoolStatusAsync(
            int schoolId, bool isActive, int? updatedByUserId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@IsActive", isActive, DbType.Boolean);
            parameters.Add("@UpdatedByUserId", updatedByUserId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ToggleStatusRow>(
                "dbo.sp_ToggleSchoolStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var (success, message) = Interpret(row?.Result);
            return (success, message, success ? row?.IsActive : null);
        }

        public async Task<(bool Success, string Message, SchoolAdminCreateResultDTO? Result)> CreateSchoolAdminAsync(
            int schoolId, SchoolAdminCreateDTO admin, string passwordHash, int? createdByUserId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Email", admin.Email, DbType.String);
            parameters.Add("@PasswordHash", passwordHash, DbType.String);
            parameters.Add("@FirstName", admin.FirstName, DbType.String);
            parameters.Add("@LastName", admin.LastName, DbType.String);
            parameters.Add("@PhoneNumber", admin.PhoneNumber, DbType.String);
            parameters.Add("@Username", admin.Username, DbType.String);
            parameters.Add("@CreatedByUserId", createdByUserId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<CreateAdminRow>(
                "dbo.sp_CreateSchoolAdmin",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var (success, message) = Interpret(row?.Result);
            if (!success || row?.UserId == null)
            {
                return (false, success ? "The administrator was not created." : message, null);
            }

            return (true, message, new SchoolAdminCreateResultDTO
            {
                UserId = row.UserId.Value,
                Username = row.Username ?? string.Empty
            });
        }

        public async Task<PlatformStatsDTO?> GetPlatformStatsAsync()
        {
            using var connection = _dbContext.CreateConnection();

            return await connection.QueryFirstOrDefaultAsync<PlatformStatsDTO>(
                "dbo.sp_GetPlatformStats",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<List<SchoolUsageReportDTO>> GetSchoolUsageReportAsync(
            DateTime? fromDate, DateTime? toDate)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@FromDate", fromDate, DbType.Date);
            parameters.Add("@ToDate", toDate, DbType.Date);

            var rows = await connection.QueryAsync<SchoolUsageReportDTO>(
                "dbo.sp_GetSchoolUsageReport",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rows.ToList();
        }

        /// <summary>
        /// sp_GetSchools repeats the grand total on every row. Kept private so the
        /// column stays an implementation detail of paging rather than part of the
        /// list contract.
        /// </summary>
        private sealed class SchoolListRow : SchoolListItemDTO
        {
            public int TotalCount { get; set; }
        }

        private sealed class ResultRow
        {
            public string? Result { get; set; }
        }

        private sealed class CreateSchoolRow
        {
            public string? Result { get; set; }
            public int? SchoolId { get; set; }
            public string? SchoolCode { get; set; }
            public int? AdminUserId { get; set; }
            public string? AdminUsername { get; set; }
        }

        private sealed class ToggleStatusRow
        {
            public string? Result { get; set; }
            public bool? IsActive { get; set; }
        }

        private sealed class CreateAdminRow
        {
            public string? Result { get; set; }
            public int? UserId { get; set; }
            public string? Username { get; set; }
        }

        /// <summary>
        /// Every write procedure in 03_Procs_Platform.sql reports either the
        /// literal 'Success' or a string starting 'Error: '. Matching the literal
        /// is the contract -- the same reading RoleRepository uses.
        /// </summary>
        private static (bool Success, string Message) Interpret(string? result)
        {
            if (string.IsNullOrWhiteSpace(result))
                return (false, "The operation returned no result.");

            if (string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase))
                return (true, result);

            return (false, result);
        }
    }
}
