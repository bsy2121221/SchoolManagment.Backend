using SchoolManagment.Models.DTOs.Auth;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request, string ipAddress);
        Task<LoginResponseDTO?> RefreshTokenAsync(string refreshToken, string ipAddress);
        Task<bool> LogoutAsync(string refreshToken, string ipAddress);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequestDTO request);

        /// <summary>
        /// Returns the database's own explanation on refusal ("user is in another
        /// school", "user not found"), which the endpoint passes on rather than
        /// reporting a bare false.
        /// </summary>
        Task<(bool Success, string Message)> ResetPasswordAsync(
            ResetPasswordRequestDTO request, int performedByUserId, int? schoolId = null);

        /// <summary>
        /// Reissues a platform administrator's access token scoped to
        /// <paramref name="schoolId"/>, or back to platform scope when it is null.
        /// Refused for anyone who is not a SuperAdmin: a user with a school of their
        /// own has no second scope to move to.
        ///
        /// Only the access token changes. The refresh token is untouched, and so is
        /// unaware of the switch -- see <see cref="SchoolSessionDTO"/> for what that
        /// means for the client.
        /// </summary>
        Task<(bool Success, string Message, SchoolSessionDTO? Session)> SwitchSchoolAsync(
            int userId, int? schoolId);
    }
}
