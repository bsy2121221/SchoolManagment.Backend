using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Teachers
{
    public class TeacherRegistrationDTO
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

        [StringLength(15, ErrorMessage = "Phone number cannot exceed 15 characters")]
        public string? PhoneNumber { get; set; }

        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "Subject cannot exceed 100 characters")]
        public string? Subject { get; set; }

        [StringLength(255, ErrorMessage = "Qualification cannot exceed 255 characters")]
        public string? Qualification { get; set; }

        [Range(0, 50, ErrorMessage = "Experience must be between 0 and 50 years")]
        public int? Experience { get; set; }

        [Range(0, 9999999.99, ErrorMessage = "Salary must be a positive value")]
        public decimal? Salary { get; set; }

        public List<int>? SubjectIds { get; set; }
    }
}
