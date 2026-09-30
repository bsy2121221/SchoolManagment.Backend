using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.DTOs.Roles;
using SchoolManagment.Models.Entities;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IDbContext _dbContext;

        public RoleRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<RoleDTO>> GetRolesAsync(bool includeInactive = false)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

            var roles = await connection.QueryAsync<RoleDTO>(
                "dbo.sp_GetRoles",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return roles.ToList();
        }

        public async Task<RoleDTO?> GetRoleByIdAsync(int roleId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@RoleId", roleId, DbType.Int32);

            using var multi = await connection.QueryMultipleAsync(
                "dbo.sp_GetRoleById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var role = await multi.ReadFirstOrDefaultAsync<RoleDTO>();
            if (role == null)
                return null;

            // Second set is sp_GetRolePermissions. It is always read, even when
            // empty, or the reader would be disposed with a set outstanding.
            role.Permissions = (await multi.ReadAsync<RolePermissionDTO>()).ToList();
            return role;
        }

        public async Task<List<RolePermissionDTO>> GetRolePermissionsAsync(int? roleId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@RoleId", roleId, DbType.Int32);

            var permissions = await connection.QueryAsync<RolePermissionDTO>(
                "dbo.sp_GetRolePermissions",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return permissions.ToList();
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

        public async Task<(bool Success, string Message, int? RoleId)> CreateRoleAsync(
            RoleCreateDTO role, int? createdBy)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@RoleName", role.RoleName, DbType.String);
            parameters.Add("@RoleCode", role.RoleCode, DbType.String);
            parameters.Add("@Description", role.Description, DbType.String);
            parameters.Add("@CreatedBy", createdBy, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ProcResult>(
                "dbo.sp_CreateRole",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var (success, message) = Interpret(row);
            return (success, message, success ? row?.RoleId : null);
        }

        public async Task<(bool Success, string Message)> UpdateRoleAsync(
            int roleId, RoleUpdateDTO role, int? modifiedBy)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@RoleId", roleId, DbType.Int32);
            parameters.Add("@RoleName", role.RoleName, DbType.String);
            parameters.Add("@Description", role.Description, DbType.String);
            parameters.Add("@IsActive", role.IsActive, DbType.Boolean);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            return await ExecuteResultAsync(connection, "dbo.sp_UpdateRole", parameters);
        }

        public async Task<(bool Success, string Message)> DeleteRoleAsync(int roleId, int? deletedBy)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@RoleId", roleId, DbType.Int32);
            parameters.Add("@DeletedBy", deletedBy, DbType.Int32);

            return await ExecuteResultAsync(connection, "dbo.sp_DeleteRole", parameters);
        }

        public async Task<(bool Success, string Message)> SaveRolePermissionAsync(
            int roleId, RolePermissionSaveDTO permission, int? modifiedBy)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@RoleId", roleId, DbType.Int32);
            parameters.Add("@ModuleName", permission.ModuleName, DbType.String);
            parameters.Add("@CanView", permission.CanView, DbType.Boolean);
            parameters.Add("@CanCreate", permission.CanCreate, DbType.Boolean);
            parameters.Add("@CanEdit", permission.CanEdit, DbType.Boolean);
            parameters.Add("@CanDelete", permission.CanDelete, DbType.Boolean);
            parameters.Add("@IsActive", permission.IsActive, DbType.Boolean);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            return await ExecuteResultAsync(connection, "dbo.sp_SaveRolePermission", parameters);
        }

        public async Task<(bool Success, string Message)> DeleteRolePermissionAsync(
            int roleId, string moduleName, int? deletedBy)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@RoleId", roleId, DbType.Int32);
            parameters.Add("@ModuleName", moduleName, DbType.String);
            parameters.Add("@DeletedBy", deletedBy, DbType.Int32);

            return await ExecuteResultAsync(connection, "dbo.sp_DeleteRolePermission", parameters);
        }

        /// <summary>Shape of the single row every write procedure returns.</summary>
        private sealed class ProcResult
        {
            public string? Result { get; set; }
            public int? RoleId { get; set; }
        }

        private static async Task<(bool Success, string Message)> ExecuteResultAsync(
            IDbConnection connection, string procedure, DynamicParameters parameters)
        {
            var row = await connection.QueryFirstOrDefaultAsync<ProcResult>(
                procedure, parameters, commandType: CommandType.StoredProcedure);

            return Interpret(row);
        }

        /// <summary>
        /// Every procedure in 03_Procs_Platform.sql reports either the literal
        /// 'Success' or a string starting 'Error: '. Matching the literal is the
        /// contract; matching on the word "successfully" -- which nothing emits --
        /// is the bug this replaces elsewhere in the codebase.
        /// </summary>
        private static (bool Success, string Message) Interpret(ProcResult? row)
        {
            var result = row?.Result;

            if (string.IsNullOrWhiteSpace(result))
                return (false, "The operation returned no result.");

            if (string.Equals(result, "Success", StringComparison.OrdinalIgnoreCase))
                return (true, result);

            // The prefix is the contract, not part of the message a user reads.
            const string prefix = "Error: ";
            return (false, result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? result[prefix.Length..]
                : result);
        }
    }
}
