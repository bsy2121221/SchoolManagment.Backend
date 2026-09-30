using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Users;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantContext _tenantContext;

        public UserService(IUserRepository userRepository, ITenantContext tenantContext)
        {
            _userRepository = userRepository;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Who is making the change, for CreatedBy / ModifiedBy. Taken from the
        /// token, never from the request body -- a caller must not be able to
        /// attribute their edit to somebody else.
        /// </summary>
        private int? ActorUserId => _tenantContext.UserId;

        /// <summary>
        /// A request whose token carries no school. Only the platform SuperAdmin is in
        /// that position, and this controller is school-scoped, so there is nothing to
        /// operate on rather than something that failed.
        /// </summary>
        private static ProcResult NoSchool => new(false, "No school in scope for this request.");

        public async Task<PaginatedResponse<UserDTO>> GetAllUsersAsync(
            string? role = null,
            int? roleId = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20
        )
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new PaginatedResponse<UserDTO>(new List<UserDTO>(), 0, page, pageSize);
            }

            var (users, totalCount) = await _userRepository.GetAllUsersAsync(
                _tenantContext.SchoolId.Value,
                role,
                roleId,
                isActive,
                searchTerm,
                page,
                pageSize
            );

            return new PaginatedResponse<UserDTO>(users, totalCount, page, pageSize);
        }

        public async Task<UserDTO?> GetUserByIdAsync(int userId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _userRepository.GetUserByIdAsync(_tenantContext.SchoolId.Value, userId);
        }

        public async Task<ProcResult> UpdateUserAsync(int userId, UserUpdateDTO user)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchool;
            }

            return await _userRepository.UpdateUserAsync(
                _tenantContext.SchoolId.Value, userId, user, ActorUserId);
        }

        public async Task<ProcResult> UpdateUserStatusAsync(int userId, bool isActive)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchool;
            }

            if (userId == _tenantContext.UserId && !isActive)
            {
                // Same reasoning as the role change below, and the same reason the
                // database cannot do it: deactivating yourself revokes your own tokens
                // mid-request, and nothing in the school can undo it except another admin
                // who may not exist.
                return new ProcResult(false, "You cannot deactivate your own account.");
            }

            return await _userRepository.UpdateUserStatusAsync(
                _tenantContext.SchoolId.Value, userId, isActive, ActorUserId);
        }

        public async Task<ProcResult> ChangeUserRoleAsync(int userId, int roleId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchool;
            }

            if (userId == _tenantContext.UserId)
            {
                // Checked here rather than in SQL: the database has no idea who is
                // signed in, and letting an admin demote themselves is how a
                // school ends up with nobody who can undo it.
                return new ProcResult(false, "You cannot change your own role.");
            }

            return await _userRepository.ChangeUserRoleAsync(
                _tenantContext.SchoolId.Value, userId, roleId, ActorUserId);
        }

        public async Task<ProcResult> DeleteUserAsync(int userId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchool;
            }

            if (userId == _tenantContext.UserId)
            {
                return new ProcResult(false, "You cannot delete your own account.");
            }

            return await _userRepository.DeleteUserAsync(
                _tenantContext.SchoolId.Value, userId, ActorUserId);
        }

        public async Task<ProfilePictureResponseDTO?> GetProfilePictureAsync(int userId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _userRepository.GetProfilePictureAsync(_tenantContext.SchoolId.Value, userId);
        }

        public async Task<ProcResult> UpdateProfilePictureAsync(int userId, byte[] pictureData, string fileName, string contentType)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchool;
            }

            return await _userRepository.UpdateProfilePictureAsync(
                _tenantContext.SchoolId.Value,
                userId,
                pictureData,
                fileName,
                contentType,
                ActorUserId
            );
        }

        public async Task<ProcResult> DeleteProfilePictureAsync(int userId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchool;
            }

            return await _userRepository.DeleteProfilePictureAsync(
                _tenantContext.SchoolId.Value, userId, ActorUserId);
        }

        public async Task<bool> CanAccessUser(int targetUserId)
        {
            // Admins can access all users in their school. Matched on the role id
            // so renaming the role does not silently revoke this.
            if (_tenantContext.RoleId == Constants.RoleIds.Admin
                || _tenantContext.RoleId == Constants.RoleIds.SuperAdmin)
            {
                return true;
            }

            // Users can only access their own profile
            return _tenantContext.UserId == targetUserId;
        }
    }
}
