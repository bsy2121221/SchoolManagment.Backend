using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// Adds a further Admin to a school that already exists. The first admin comes
    /// from <see cref="SchoolCreateDTO"/>; this is for the second onwards.
    /// </summary>
    public class SchoolAdminCreateDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Optional. The procedure names the account CODE_ADMIN2, CODE_ADMIN3 and
        /// so on when this is omitted; anything supplied is sanitised and gets the
        /// school code prefixed if it is missing one.
        /// </summary>
        [StringLength(80, ErrorMessage = "Username cannot exceed 80 characters")]
        public string? Username { get; set; }

        /// <summary>
        /// Optional. Omit it and the account is created with the shared temporary
        /// password and must change it at first login.
        /// </summary>
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Password must be at least 8 characters")]
        public string? Password { get; set; }
    }
}
