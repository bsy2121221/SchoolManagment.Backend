using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Users;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    /// <summary>
    /// Every write returns <see cref="ProcResult"/> rather than <c>bool</c>.
    ///
    /// The procedures behind this module are the most talkative in the database:
    /// <c>sp_DeleteUser</c> alone refuses in eight distinguishable ways ("Cannot delete
    /// admin users", "Cannot delete teacher who is assigned to active classes",
    /// "…who has historical attendance data", and the student's attendance / results /
    /// fees trio), and <c>sp_ChangeUserRole</c> in three. All of them were reaching the
    /// caller as a single unexplained <c>false</c>.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// <paramref name="role"/> still accepts a role name or code, for callers
        /// that filter by the string the UI shows; <paramref name="roleId"/> is the
        /// exact key. Pass one or neither -- the procedure applies both if both
        /// arrive, which would narrow the result to their intersection.
        /// </summary>
        Task<(List<UserDTO> Users, int TotalCount)> GetAllUsersAsync(
            int schoolId,
            string? role = null,
            int? roleId = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20
        );

        Task<UserDTO?> GetUserByIdAsync(int schoolId, int userId);

        /// <summary>
        /// Writes the login, the person and the primary address in one procedure
        /// call. <paramref name="modifiedBy"/> is stamped on all three so the
        /// audit trail says who did it, not just when.
        /// </summary>
        Task<ProcResult> UpdateUserAsync(int schoolId, int userId, UserUpdateDTO user, int? modifiedBy = null);

        /// <summary>
        /// Activates or deactivates the login, the person row, the role-specific row
        /// (Students / Teachers / Parents) and, on deactivation, every live refresh
        /// token. This is the only way back from <see cref="DeleteUserAsync"/>, which is
        /// the same soft delete with guard rails in front of it.
        /// </summary>
        Task<ProcResult> UpdateUserStatusAsync(int schoolId, int userId, bool isActive, int? modifiedBy = null);

        /// <summary>
        /// Moves a user to another role. Refused for students, teachers and
        /// parents -- their role is implied by the Students/Teachers/Parents row
        /// that owns them -- and for the platform SuperAdmin.
        /// </summary>
        Task<ProcResult> ChangeUserRoleAsync(
            int schoolId, int userId, int roleId, int? modifiedBy = null);

        /// <summary>
        /// Soft delete: deactivates the login and cascades through whichever
        /// role-specific rows the user owns. Refused outright for admins, and for anyone
        /// carrying history the school has to keep.
        /// </summary>
        Task<ProcResult> DeleteUserAsync(int schoolId, int userId, int? performedByUserId = null);

        Task<ProfilePictureResponseDTO?> GetProfilePictureAsync(int schoolId, int userId);

        Task<ProcResult> UpdateProfilePictureAsync(
            int schoolId, int userId, byte[] pictureData, string fileName, string contentType,
            int? modifiedBy = null);

        Task<ProcResult> DeleteProfilePictureAsync(int schoolId, int userId, int? modifiedBy = null);
    }
}
