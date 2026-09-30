using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Users;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IUserService
    {
        Task<PaginatedResponse<UserDTO>> GetAllUsersAsync(
            string? role = null,
            int? roleId = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20
        );

        Task<UserDTO?> GetUserByIdAsync(int userId);
        Task<ProcResult> UpdateUserAsync(int userId, UserUpdateDTO user);

        /// <summary>
        /// Activate or deactivate. <c>isActive: true</c> is the only reactivation path in
        /// the API -- the Students, Teachers and Parents modules each deactivate without
        /// offering a way back -- and it restores the role-specific row along with the
        /// login. It does not restore the subject or parent links a delete switched off:
        /// those are re-established through their own modules.
        /// </summary>
        Task<ProcResult> UpdateUserStatusAsync(int userId, bool isActive);

        /// <summary>
        /// Refused by the database for students, teachers and parents, and for the
        /// platform SuperAdmin; the message says which. Refused here for the caller's own
        /// account, which the database cannot check because it does not know who is
        /// signed in.
        /// </summary>
        Task<ProcResult> ChangeUserRoleAsync(int userId, int roleId);

        Task<ProcResult> DeleteUserAsync(int userId);
        Task<ProfilePictureResponseDTO?> GetProfilePictureAsync(int userId);
        Task<ProcResult> UpdateProfilePictureAsync(int userId, byte[] pictureData, string fileName, string contentType);
        Task<ProcResult> DeleteProfilePictureAsync(int userId);
        Task<bool> CanAccessUser(int targetUserId);
    }
}
