using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.DTOs.Settings;
using System.Data;
using System.Text.Json;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class SettingRepository : ISettingRepository
    {
        /// <summary>
        /// sp_SaveMultipleSettings reads the payload with OPENJSON ... WITH paths of
        /// '$.category', '$.settingKey' and so on, so the property names have to be
        /// camel-cased. Nothing else about the payload is negotiable, which is why
        /// this lives next to the call rather than being a shared serialiser setting.
        /// </summary>
        private static readonly JsonSerializerOptions PayloadOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly IDbContext _dbContext;

        public SettingRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SettingDTO>> GetAllSettingsAsync(int schoolId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);

            var settings = await connection.QueryAsync<SettingDTO>(
                "dbo.sp_GetAllSettings",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return settings.ToList();
        }

        public async Task<List<SettingDTO>> GetSettingsByCategoryAsync(int schoolId, string category)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Category", category, DbType.String);

            var settings = await connection.QueryAsync<SettingDTO>(
                "dbo.sp_GetSettingsByCategory",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return settings.ToList();
        }

        public async Task<SettingDTO?> GetSettingByKeyAsync(int schoolId, string category, string settingKey)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Category", category, DbType.String);
            parameters.Add("@SettingKey", settingKey, DbType.String);

            return await connection.QueryFirstOrDefaultAsync<SettingDTO>(
                "dbo.sp_GetSettingByKey",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<(SettingResult Result, string Message)> SaveSettingAsync(
            int schoolId, SettingSaveDTO setting, int? userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Category", setting.Category, DbType.String);
            parameters.Add("@SettingKey", setting.SettingKey, DbType.String);
            parameters.Add("@SettingValue", setting.SettingValue, DbType.String);
            parameters.Add("@DataType", setting.DataType, DbType.String);
            parameters.Add("@Description", setting.Description, DbType.String);
            parameters.Add("@UserId", userId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "dbo.sp_SaveSetting",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return Interpret(row);
        }

        public async Task<(SettingResult Result, string Message, int Saved, int Skipped)> SaveMultipleSettingsAsync(
            int schoolId, List<SettingSaveDTO> settings, int? userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@SettingsJson", JsonSerializer.Serialize(settings, PayloadOptions), DbType.String);

            var row = await connection.QueryFirstOrDefaultAsync<BulkResultRow>(
                "dbo.sp_SaveMultipleSettings",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var (result, message) = Interpret(row);
            return (result, message, row?.SettingsSaved ?? 0, row?.SettingsSkipped ?? 0);
        }

        public async Task<(SettingResult Result, string Message)> DeleteSettingAsync(
            int schoolId, string category, string settingKey, int? userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Category", category, DbType.String);
            parameters.Add("@SettingKey", settingKey, DbType.String);
            parameters.Add("@UserId", userId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "dbo.sp_DeleteSetting",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return Interpret(row);
        }

        public async Task<(SettingResult Result, string Message, int Restored)> ResetSettingsAsync(
            int schoolId, string? category, int? userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Category", category, DbType.String);
            parameters.Add("@UserId", userId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ResetResultRow>(
                "dbo.sp_ResetSettings",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var (result, message) = Interpret(row);
            return (result, message, row?.SettingsRestored ?? 0);
        }

        /// <summary>
        /// Shape shared by every write procedure here: a Result literal plus a
        /// message meant to be shown. That is a different contract from the platform
        /// procedures, which pack both into one column.
        /// </summary>
        private class ResultRow
        {
            public string? Result { get; set; }
            public string? Message { get; set; }
        }

        private sealed class BulkResultRow : ResultRow
        {
            public int SettingsSaved { get; set; }
            public int SettingsSkipped { get; set; }
        }

        private sealed class ResetResultRow : ResultRow
        {
            public int SettingsRestored { get; set; }
        }

        /// <summary>
        /// Maps the Result literal onto <see cref="SettingResult"/>. Anything
        /// unrecognised -- including no row at all -- is an error, because the only
        /// safe reading of a literal this code does not know is that the operation
        /// did not do what was asked.
        /// </summary>
        private static (SettingResult Result, string Message) Interpret(ResultRow? row)
        {
            var message = string.IsNullOrWhiteSpace(row?.Message)
                ? "The operation returned no result."
                : row!.Message!;

            var result = row?.Result switch
            {
                "Created" => SettingResult.Created,
                "Updated" => SettingResult.Updated,
                "Success" => SettingResult.Success,
                "NotFound" => SettingResult.NotFound,
                _ => SettingResult.Error
            };

            return (result, message);
        }
    }
}
