using SchoolManagment.Models.DTOs.Roles;

namespace SchoolManagment.Models.DTOs.Auth
{
    public class LoginResponseDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>Role name, unchanged.</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>dbo.Roles.Id -- see Constants.RoleIds.</summary>
        public int RoleId { get; set; }

        public int? SchoolId { get; set; }
        public string? SchoolCode { get; set; }
        public string? SchoolName { get; set; }
        public bool RequirePasswordChange { get; set; }
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }

        /// <summary>
        /// The permission grid that is also baked into the access token, so the
        /// UI can hide what the user cannot do without a second call. A module
        /// absent from this list is denied.
        /// </summary>
        public List<RolePermissionDTO> Permissions { get; set; } = new();
    }
}
