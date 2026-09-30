using SchoolManagment.Models.Entities;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        /// <summary>
        /// Looks the account up and returns it together with the permission grid
        /// for its role. sp_Login emits both in one round trip, and both are
        /// withheld together when the account or its school is ineligible.
        ///
        /// The password is NOT checked here -- the hash comes back so
        /// AuthService can verify it with BCrypt.
        /// </summary>
        Task<(User? User, List<RolePermission> Permissions)> LoginAsync(string username);

        Task<bool> RecordLoginAsync(int userId, string ipAddress);

        Task<RefreshToken?> GetRefreshTokenAsync(string token);

        Task<RefreshToken?> CreateRefreshTokenAsync(int userId, string token, DateTime expiryDate, string? ipAddress = null);

        Task<bool> RevokeRefreshTokenAsync(string token, string? ipAddress, string? replacedByToken = null);

        Task<bool> ChangePasswordAsync(int userId, string newPasswordHash, string? ipAddress = null);

        /// <summary>
        /// <paramref name="schoolId"/> null means the caller is a SuperAdmin and
        /// the tenant check is skipped; anything else confines the reset to that
        /// school.
        /// </summary>
        Task<(bool Success, string Message)> ResetPasswordAsync(
            int userId, string newPasswordHash, bool requirePasswordChange,
            int? schoolId = null, int? performedByUserId = null);

        Task<User?> GetUserByIdAsync(int userId);

        /// <summary>The effective permission grid for one user, for the refresh
        /// path -- re-read so a permission change takes effect at the next
        /// refresh rather than at the next full login.</summary>
        Task<List<RolePermission>> GetUserPermissionsAsync(int userId);

        Task<School?> GetSchoolByIdAsync(int schoolId);
    }
}
