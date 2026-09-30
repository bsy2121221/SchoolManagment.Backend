namespace SchoolManagment.Models.DTOs.Teachers
{
    public class TeacherDTO
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Qualification { get; set; }
        public int? Experience { get; set; }
        public decimal? Salary { get; set; }
        public DateTime? JoinDate { get; set; }
        public bool IsActive { get; set; }

        // User details
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public bool RequirePasswordChange { get; set; }

        // Aggregated data
        public string? SubjectNames { get; set; }
        public string? SubjectIds { get; set; }
        public int SchoolId { get; set; }
    }
}
