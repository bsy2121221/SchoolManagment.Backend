using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Entities;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbContext _dbContext;

        public AuthRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(User? User, List<RolePermission> Permissions)> LoginAsync(string username)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Username", username, DbType.String);

            using var multi = await connection.QueryMultipleAsync(
                "dbo.sp_Login",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // sp_Login selects from dbo.vw_Users, whose column names match the
            // User properties one for one, so Dapper maps the row directly. The
            // hand-written projection this replaces read a column called UserId,
            // which the procedure has never returned -- the first column is Id.
            var user = await multi.ReadFirstOrDefaultAsync<User>();

            // The grid must be read even when the login is refused, or the reader
            // is disposed with a result set outstanding.
            var permissions = (await multi.ReadAsync<RolePermission>()).ToList();

            return user == null ? (null, new List<RolePermission>()) : (user, permissions);
        }

        public async Task<bool> RecordLoginAsync(int userId, string ipAddress)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@IpAddress", ipAddress, DbType.String);

            await connection.ExecuteAsync(
                "dbo.sp_RecordLogin",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return true;
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Token", token, DbType.String);

            // sp_GetRefreshToken, not sp_ValidateRefreshToken: the latter returns
            // only IsValid and UserId, so the Id / Token / ExpiryDate / CreatedAt
            // this method needs were never in the result set.
            var result = await connection.QueryFirstOrDefaultAsync<RefreshToken>(
                "dbo.sp_GetRefreshToken",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<RefreshToken?> CreateRefreshTokenAsync(
            int userId, string token, DateTime expiryDate, string? ipAddress = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@Token", token, DbType.String);
            parameters.Add("@ExpiryDate", expiryDate, DbType.DateTime);
            parameters.Add("@IpAddress", ipAddress, DbType.String);

            var row = await connection.QueryFirstOrDefaultAsync<CreateTokenResult>(
                "dbo.sp_CreateRefreshToken",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (!Interpret(row?.Result).Success || row?.TokenId == null)
                return null;

            return new RefreshToken
            {
                Id = row.TokenId.Value,
                UserId = userId,
                Token = token,
                ExpiryDate = expiryDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> RevokeRefreshTokenAsync(string token, string? ipAddress, string? replacedByToken = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@Token", token, DbType.String);

            // The procedure's parameter is @IpAddress. It used to be sent as
            // @RevokedByIp -- the column name, not the parameter name -- which
            // SQL Server rejects as an unknown parameter.
            parameters.Add("@IpAddress", ipAddress, DbType.String);
            parameters.Add("@ReplacedByToken", replacedByToken, DbType.String);

            var result = await connection.QueryFirstOrDefaultAsync<string>(
                "dbo.sp_RevokeRefreshToken",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return Interpret(result).Success;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPasswordHash, string? ipAddress = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@NewPasswordHash", newPasswordHash, DbType.String);
            parameters.Add("@IpAddress", ipAddress, DbType.String);

            var result = await connection.QueryFirstOrDefaultAsync<string>(
                "dbo.sp_ChangePassword",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return Interpret(result).Success;
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(
            int userId, string newPasswordHash, bool requirePasswordChange,
            int? schoolId = null, int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@NewPasswordHash", newPasswordHash, DbType.String);
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);
            parameters.Add("@RequirePasswordChange", requirePasswordChange, DbType.Boolean);

            var result = await connection.QueryFirstOrDefaultAsync<string>(
                "dbo.sp_ResetPassword",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return Interpret(result);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId, DbType.Int32);

            var result = await connection.QueryAsync<User>(
                "dbo.sp_GetUserById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.FirstOrDefault();
        }

        public async Task<List<RolePermission>> GetUserPermissionsAsync(int userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId, DbType.Int32);

            var permissions = await connection.QueryAsync<RolePermission>(
                "dbo.sp_GetUserPermissions",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return permissions.ToList();
        }

        public async Task<School?> GetSchoolByIdAsync(int schoolId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);

            var result = await connection.QueryAsync<School>(
                "dbo.sp_GetSchoolById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.FirstOrDefault();
        }

        private sealed class CreateTokenResult
        {
            public string? Result { get; set; }
            public int? TokenId { get; set; }
        }

        /// <summary>
        /// The procedures report the literal 'Success' or a string starting
        /// 'Error: '. The old test for the word "successfully" matched nothing, so
        /// a password change that had already been committed was reported as a
        /// failure to the caller.
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
