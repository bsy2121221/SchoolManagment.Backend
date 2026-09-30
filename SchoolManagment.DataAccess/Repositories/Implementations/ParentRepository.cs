using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Parents;
using System.Data;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    public class ParentRepository : IParentRepository
    {
        private readonly IDbContext _dbContext;

        public ParentRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(ProcResult Result, ParentRegistrationResponseDTO? Parent)> RegisterParentAsync(
            int schoolId,
            ParentRegistrationDTO parent,
            string passwordHash,
            int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FirstName", parent.FirstName, DbType.String);
            parameters.Add("@LastName", parent.LastName, DbType.String);
            parameters.Add("@Email", parent.Email, DbType.String);
            parameters.Add("@PhoneNumber", parent.PhoneNumber, DbType.String);
            parameters.Add("@Address", parent.Address, DbType.String);
            parameters.Add("@Occupation", parent.Occupation, DbType.String);
            parameters.Add("@AnnualIncome", parent.AnnualIncome, DbType.Decimal);
            parameters.Add("@PasswordHash", passwordHash, DbType.String);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<RegisterParentRow>(
                "dbo.sp_RegisterParent",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var result = ProcResult.From(row?.Result);

            // Every failure branch selects the id columns as NULL, so there is nothing to
            // hand back on one; the reason -- a duplicate email, a deactivated school -- is
            // in the ProcResult.
            if (!result.Success || row is null)
            {
                return (result, null);
            }

            return (
                result,
                new ParentRegistrationResponseDTO
                {
                    UserId = row.UserId ?? 0,
                    ParentId = row.ParentId ?? 0,
                    Username = row.Username ?? string.Empty
                }
            );
        }

        public async Task<List<ParentDTO>> GetAllParentsAsync(int schoolId, bool includeInactive = false)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@IncludeInactive", includeInactive, DbType.Boolean);

            var parents = await connection.QueryAsync<ParentDTO>(
                "dbo.sp_GetAllParents",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parents.ToList();
        }

        public async Task<ParentDTO?> GetParentByIdAsync(int schoolId, int parentId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ParentId", parentId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<ParentDTO>(
                "dbo.sp_GetParentById",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<ParentProfileDTO?> GetParentProfileAsync(int schoolId, int userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);

            using var reader = await connection.QueryMultipleAsync(
                "dbo.sp_GetParentProfile",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var profile = await reader.ReadFirstOrDefaultAsync<ParentProfileDTO>();

            if (profile is null)
            {
                return null;
            }

            profile.Children = (await reader.ReadAsync<ParentChildDTO>()).ToList();

            return profile;
        }

        public async Task<ProcResult> UpdateParentAsync(
            int schoolId,
            int parentId,
            ParentUpdateDTO parent,
            int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ParentId", parentId, DbType.Int32);
            parameters.Add("@FirstName", parent.FirstName, DbType.String);
            parameters.Add("@LastName", parent.LastName, DbType.String);
            parameters.Add("@Email", parent.Email, DbType.String);
            parameters.Add("@PhoneNumber", parent.PhoneNumber, DbType.String);
            parameters.Add("@Address", parent.Address, DbType.String);
            parameters.Add("@Occupation", parent.Occupation, DbType.String);
            parameters.Add("@AnnualIncome", parent.AnnualIncome, DbType.Decimal);
            parameters.Add("@ModifiedBy", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_UpdateParent", parameters);
        }

        public async Task<ProcResult> DeleteParentAsync(int schoolId, int parentId, int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ParentId", parentId, DbType.Int32);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_DeleteParent", parameters);
        }

        public async Task<ProcResult> LinkStudentParentAsync(
            int schoolId,
            int studentId,
            int parentId,
            string relationship,
            int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@ParentId", parentId, DbType.Int32);
            parameters.Add("@Relationship", relationship, DbType.String);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            // sp_LinkStudentParent is a MERGE, so this both creates a link and re-labels or
            // re-activates one that already exists.
            return await ExecuteReportingAsync(connection, "dbo.sp_LinkStudentParent", parameters);
        }

        public async Task<ProcResult> UnlinkStudentParentAsync(
            int schoolId,
            int studentId,
            int parentId,
            int? performedByUserId = null)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@ParentId", parentId, DbType.Int32);
            parameters.Add("@PerformedByUserId", performedByUserId, DbType.Int32);

            return await ExecuteReportingAsync(connection, "dbo.sp_UnlinkStudentParent", parameters);
        }

        public async Task<List<ParentDTO>> GetParentsByStudentAsync(int schoolId, int studentId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);

            var parents = await connection.QueryAsync<ParentDTO>(
                "dbo.sp_GetParentsByStudent",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parents.ToList();
        }

        public async Task<List<ParentChildDTO>> GetChildrenByParentAsync(int schoolId, int parentId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ParentId", parentId, DbType.Int32);

            var children = await connection.QueryAsync<ParentChildDTO>(
                "dbo.sp_GetChildrenByParent",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return children.ToList();
        }

        /// <summary>
        /// Runs a procedure whose whole output is one <c>Result</c> column and reports what
        /// it said.
        ///
        /// The four writes routed through here previously returned <c>bool</c> or a bare
        /// string, and threw the sentence away. <c>sp_DeleteParent</c> refuses with "Cannot
        /// delete a parent who has recorded fee payments"; <c>sp_UpdateParent</c> refuses
        /// separately with "Parent not found in this school" and "Email already exists";
        /// <c>sp_UnlinkStudentParent</c> distinguishes "that parent is not linked to this
        /// student" from a link that was already inactive. The API answered every one of
        /// them with "Failed to update parent" or "Failed to unlink parent from student".
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
        /// sp_RegisterParent's single row. Read into this rather than onto the response DTO
        /// so that the outcome column has somewhere to land.
        /// </summary>
        private sealed class RegisterParentRow
        {
            public string? Result { get; set; }
            public int? UserId { get; set; }
            public int? ParentId { get; set; }
            public string? Username { get; set; }
        }
    }
}
