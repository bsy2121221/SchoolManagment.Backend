using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Users;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbContext _dbContext;

        public UserRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(List<UserDTO> Users, int TotalCount)> GetAllUsersAsync(
            int schoolId,
            string? role = null,
            int? roleId = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20
        )
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@Role", role, DbType.String);
            parameters.Add("@RoleId", roleId, DbType.Int32);
            parameters.Add("@IsActive", isActive, DbType.Boolean);
            parameters.Add("@SearchTerm", searchTerm, DbType.String);
            parameters.Add("@Page", page, DbType.Int32);
            parameters.Add("@PageSize", pageSize, DbType.Int32);

            using var multi = await connection.QueryMultipleAsync(
                "dbo.sp_GetAllUsers",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var users = (await multi.ReadAsync<UserDTO>()).ToList();
            var totalCount = await multi.ReadFirstOrDefaultAsync<int>();

            return (users, totalCount);
        }

        public async Task<UserDTO?> GetUserByIdAsync(int schoolId, int userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);

            var result = await connection.QueryAsync<UserDTO>(
                "dbo.sp_GetUserById",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.FirstOrDefault();
        }

        public async Task<ProcResult> UpdateUserAsync(
            int schoolId, int userId, UserUpdateDTO user, int? modifiedBy = null)
        {
            using var connection = _dbContext.CreateConnection();

            var address = user.AddressDetails;

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@FirstName", user.FirstName, DbType.String);
            parameters.Add("@LastName", user.LastName, DbType.String);
            parameters.Add("@Email", user.Email, DbType.String);
            parameters.Add("@PhoneNumber", user.PhoneNumber, DbType.String);

            // @Address is the free-text form and only applies when no structured
            // address was sent: the procedure treats it as AddressLine1, so
            // sending both would set the line twice with different values.
            parameters.Add("@Address", address == null ? user.Address : null, DbType.String);
            parameters.Add("@AddressLine1", address?.AddressLine1, DbType.String);
            parameters.Add("@AddressLine2", address?.AddressLine2, DbType.String);
            parameters.Add("@City", address?.City, DbType.String);
            parameters.Add("@State", address?.State, DbType.String);
            parameters.Add("@Country", address?.Country, DbType.String);
            parameters.Add("@PostalCode", address?.PostalCode, DbType.String);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateUserProfile", parameters);
        }

        public async Task<ProcResult> UpdateUserStatusAsync(
            int schoolId, int userId, bool isActive, int? modifiedBy = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@IsActive", isActive, DbType.Boolean);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            // sp_UpdateUserStatus is a thin pass-through to sp_ToggleUserStatus, and the
            // inner procedure is what SELECTs the Result column. Dapper reads the first
            // result set either way, so the nesting is invisible here.
            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateUserStatus", parameters);
        }

        public async Task<ProcResult> ChangeUserRoleAsync(
            int schoolId, int userId, int roleId, int? modifiedBy = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@RoleId", roleId, DbType.Int32);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_ChangeUserRole", parameters);
        }

        public async Task<ProcResult> DeleteUserAsync(int schoolId, int userId, int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_DeleteUser", parameters);
        }

        public async Task<ProfilePictureResponseDTO?> GetProfilePictureAsync(int schoolId, int userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);

            var picture = await connection.QueryFirstOrDefaultAsync<ProfilePictureRow>(
                "dbo.sp_GetProfilePicture",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // No row means no such user; a row with no bytes means a user who has never
            // uploaded one. The endpoint answers 404 for both, which is the truth the
            // caller can act on: there is nothing to show.
            if (picture?.ProfilePicture is null || picture.ProfilePicture.Length == 0)
                return null;

            return new ProfilePictureResponseDTO
            {
                Data = picture.ProfilePicture,
                // Both fall back rather than being required: the columns are written
                // together with the bytes, but a row loaded by an older path could carry
                // the image without them, and a byte array with no content type is
                // unservable.
                ContentType = string.IsNullOrWhiteSpace(picture.ProfilePictureContentType)
                    ? "image/jpeg"
                    : picture.ProfilePictureContentType,
                FileName = string.IsNullOrWhiteSpace(picture.ProfilePictureFileName)
                    ? "profile.jpg"
                    : picture.ProfilePictureFileName
            };
        }

        public async Task<ProcResult> UpdateProfilePictureAsync(
            int schoolId, int userId, byte[] pictureData, string fileName, string contentType,
            int? modifiedBy = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@ProfilePicture", pictureData, DbType.Binary);

            // The procedure's parameters are @FileName / @ContentType. They used
            // to be sent as @ProfilePictureFileName / @ProfilePictureContentType,
            // which SQL Server rejects outright as unknown parameter names.
            parameters.Add("@FileName", fileName, DbType.String);
            parameters.Add("@ContentType", contentType, DbType.String);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateProfilePicture", parameters);
        }

        public async Task<ProcResult> DeleteProfilePictureAsync(int schoolId, int userId, int? modifiedBy = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);
            parameters.Add("@ModifiedBy", modifiedBy, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_DeleteProfilePicture", parameters);
        }

        /// <summary>
        /// Runs a procedure whose whole output is one <c>Result</c> column and reports what
        /// it said.
        ///
        /// This replaced a local <c>Interpret</c> helper that read the column correctly and
        /// then returned <c>bool</c> from five of the six writes -- the quieter half of the
        /// pair described on <see cref="ProcResult"/>, and the same defect
        /// <c>TeacherRepository</c> had. It costs more here than anywhere else in the
        /// project: <c>sp_DeleteUser</c> has eight distinct refusals with eight different
        /// remedies, and all of them arrived at the user as "Failed to delete user".
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
        /// sp_GetProfilePicture's single row. Typed rather than <c>dynamic</c>: the
        /// dynamic version was where both of this project's standing CS8602 warnings
        /// came from, and a null-dereference warning on the one method that reads
        /// user-supplied bytes is not a warning to live with.
        /// </summary>
        private sealed class ProfilePictureRow
        {
            public byte[]? ProfilePicture { get; set; }
            public string? ProfilePictureFileName { get; set; }
            public string? ProfilePictureContentType { get; set; }
            public DateTime? ProfilePictureUploadDate { get; set; }
        }
    }
}
