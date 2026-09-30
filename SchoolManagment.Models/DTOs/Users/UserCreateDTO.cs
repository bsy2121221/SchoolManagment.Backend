using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Users
{
    public class UserCreateDTO
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Free-text address, kept for existing clients. It is stored verbatim as
        /// the primary address's AddressLine1, so it reads back unchanged.
        /// Ignored when <see cref="AddressDetails"/> is supplied.
        /// </summary>
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string? Address { get; set; }

        /// <summary>The structured alternative to <see cref="Address"/>.</summary>
        public AddressDTO? AddressDetails { get; set; }

        /// <summary>
        /// Role name or code, e.g. "Teacher" or "TEACHER". Still accepted so
        /// existing clients keep working; resolved to an id by fn_RoleId. Ignored
        /// when <see cref="RoleId"/> is set.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>dbo.Roles.Id. Preferred over <see cref="Role"/>.</summary>
        public int? RoleId { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public string? Password { get; set; }

        public bool RequirePasswordChange { get; set; } = true;
    }
}
