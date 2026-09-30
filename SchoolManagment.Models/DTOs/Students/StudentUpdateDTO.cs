using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Students
{
    public class StudentUpdateDTO
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

        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [StringLength(100, ErrorMessage = "Father name cannot exceed 100 characters")]
        public string? FatherName { get; set; }

        [StringLength(100, ErrorMessage = "Mother name cannot exceed 100 characters")]
        public string? MotherName { get; set; }

        [StringLength(5, ErrorMessage = "Blood group cannot exceed 5 characters")]
        public string? BloodGroup { get; set; }

        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string? Address { get; set; }
    }
}
