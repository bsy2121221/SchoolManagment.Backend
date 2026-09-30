namespace SchoolManagment.Models.DTOs.Students
{
    public class StudentDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? RollNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string? BloodGroup { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
