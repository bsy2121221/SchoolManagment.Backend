using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Auth;
using SchoolManagment.Models.DTOs.Roles;
using SchoolManagment.Models.Entities;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtHelper _jwtHelper;

        public AuthService(IAuthRepository authRepository, IJwtHelper jwtHelper)
        {
            _authRepository = authRepository;
            _jwtHelper = jwtHelper;
        }

        public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request, string ipAddress)
        {
            var (user, permissions) = await _authRepository.LoginAsync(request.Username);
            if (user == null)
                return null;

            if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
                return null;

            if (!user.IsActive)
                return null;

            var school = user.SchoolId.HasValue
                ? await _authRepository.GetSchoolByIdAsync(user.SchoolId.Value)
                : null;

            if (school != null && !school.IsActive)
                return null;

            var accessToken = _jwtHelper.GenerateAccessToken(user, school, permissions);
            var refreshToken = _jwtHelper.GenerateRefreshToken();

            var refreshTokenExpiry = DateTime.UtcNow.AddDays(Constants.Settings.RefreshTokenExpiryDays);
            await _authRepository.CreateRefreshTokenAsync(user.Id, refreshToken, refreshTokenExpiry, ipAddress);

            await _authRepository.RecordLoginAsync(user.Id, ipAddress);

            return BuildResponse(user, school, permissions, accessToken, refreshToken);
        }

        public async Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken, string ipAddress)
        {
            var storedToken = await _authRepository.GetRefreshTokenAsync(refreshToken);
            if (storedToken == null || !storedToken.IsActive || storedToken.ExpiryDate < DateTime.UtcNow)
                return null;

            var user = await _authRepository.GetUserByIdAsync(storedToken.UserId);
            if (user == null || !user.IsActive)
                return null;

            var school = user.SchoolId.HasValue
                ? await _authRepository.GetSchoolByIdAsync(user.SchoolId.Value)
                : null;

            if (school != null && !school.IsActive)
                return null;

            // Re-read rather than copied from the old token: this is what makes a
            // permission change take effect at the next refresh instead of
            // lingering until the user signs in again.
            var permissions = await _authRepository.GetUserPermissionsAsync(user.Id);

            var newAccessToken = _jwtHelper.GenerateAccessToken(user, school, permissions);
            var newRefreshToken = _jwtHelper.GenerateRefreshToken();

            var newRefreshTokenExpiry = DateTime.UtcNow.AddDays(Constants.Settings.RefreshTokenExpiryDays);
            await _authRepository.CreateRefreshTokenAsync(user.Id, newRefreshToken, newRefreshTokenExpiry, ipAddress);

            await _authRepository.RevokeRefreshTokenAsync(refreshToken, ipAddress, newRefreshToken);

            return BuildResponse(user, school, permissions, newAccessToken, newRefreshToken);
        }

        public async Task<bool> LogoutAsync(string refreshToken, string ipAddress)
        {
            return await _authRepository.RevokeRefreshTokenAsync(refreshToken, ipAddress);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequestDTO request)
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            if (!PasswordHelper.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                return false;

            var newPasswordHash = PasswordHelper.HashPassword(request.NewPassword);
            return await _authRepository.ChangePasswordAsync(userId, newPasswordHash);
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(
            ResetPasswordRequestDTO request, int performedByUserId, int? schoolId = null)
        {
            var newPasswordHash = PasswordHelper.HashPassword(request.NewPassword);

            // schoolId null means the caller is a SuperAdmin, so the procedure
            // skips the tenant check. An admin's own SchoolId confines the reset
            // to their school, which is what stops one school's admin resetting
            // another school's password.
            return await _authRepository.ResetPasswordAsync(
                request.UserId,
                newPasswordHash,
                request.RequirePasswordChange,
                schoolId,
                performedByUserId
            );
        }

        public async Task<(bool Success, string Message, SchoolSessionDTO? Session)> SwitchSchoolAsync(
            int userId, int? schoolId)
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null || !user.IsActive)
                return (false, "User not found.", null);

            // Checked on the account rather than on the token alone, so a stale or
            // hand-edited claim cannot borrow a tenant.
            if (user.RoleId != Constants.RoleIds.SuperAdmin || user.SchoolId.HasValue)
                return (false, "Only a platform administrator can act inside a school.", null);

            School? school = null;
            if (schoolId.HasValue)
            {
                school = await _authRepository.GetSchoolByIdAsync(schoolId.Value);
                if (school == null)
                    return (false, "School not found.", null);

                // A suspended school refuses its own users at login; letting a
                // platform administrator in through the side door would hide the
                // suspension from whoever is investigating it.
                if (!school.IsActive)
                    return (false, $"'{school.SchoolName}' is suspended.", null);
            }

            // Re-read for the same reason the refresh path re-reads them: the new
            // token should carry current permissions, not the ones it replaces.
            var permissions = await _authRepository.GetUserPermissionsAsync(user.Id);

            var session = new SchoolSessionDTO
            {
                SchoolId = school?.Id,
                SchoolCode = school?.SchoolCode,
                SchoolName = school?.SchoolName,
                AccessToken = _jwtHelper.GenerateAccessToken(user, school, permissions),
                ExpiresIn = Constants.Settings.AccessTokenExpiryMinutes * 60,
                IsActingAsSchool = school != null
            };

            var message = school == null
                ? "Returned to platform scope."
                : $"Now acting inside '{school.SchoolName}'.";

            return (true, message, session);
        }

        private static LoginResponseDTO BuildResponse(
            User user,
            School? school,
            IEnumerable<RolePermission> permissions,
            string accessToken,
            string refreshToken)
        {
            return new LoginResponseDTO
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                RoleId = user.RoleId,
                SchoolId = user.SchoolId,
                SchoolCode = school?.SchoolCode,
                SchoolName = school?.SchoolName,
                RequirePasswordChange = user.RequirePasswordChange,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = Constants.Settings.AccessTokenExpiryMinutes * 60,
                Permissions = permissions.Select(p => new RolePermissionDTO
                {
                    RoleId = user.RoleId,
                    RoleName = user.Role,
                    ModuleName = p.ModuleName,
                    CanView = p.CanView,
                    CanCreate = p.CanCreate,
                    CanEdit = p.CanEdit,
                    CanDelete = p.CanDelete,
                    IsActive = true
                }).ToList()
            };
        }
    }
}
