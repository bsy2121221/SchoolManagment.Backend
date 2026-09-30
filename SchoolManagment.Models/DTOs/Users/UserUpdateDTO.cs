using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Users
{
    public class UserUpdateDTO
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
        /// Free-text address, kept for existing clients; written to the primary
        /// address's AddressLine1. Ignored when <see cref="AddressDetails"/> is
        /// supplied. Leave both null to leave the address untouched --
        /// sp_UpdateUserIdentity only writes the address when at least one
        /// address value arrives.
        /// </summary>
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string? Address { get; set; }

        /// <summary>The structured alternative to <see cref="Address"/>.</summary>
        public AddressDTO? AddressDetails { get; set; }
    }
}
