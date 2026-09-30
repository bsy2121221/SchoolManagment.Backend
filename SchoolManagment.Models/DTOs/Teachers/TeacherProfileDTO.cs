namespace SchoolManagment.Models.DTOs.Teachers
{
    public class TeacherProfileDTO
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Aggregated info
        public string? AssignedSubjects { get; set; }
        public string? AssignedClasses { get; set; }

        // School info
        public int SchoolId { get; set; }
        public string? SchoolCode { get; set; }
        public string? SchoolName { get; set; }

        // Statistics
        public TeacherProfileStatsDTO? Stats { get; set; }
    }

    public class TeacherProfileStatsDTO
    {
        public int ClassesAssigned { get; set; }
        public int StudentsUnderCare { get; set; }
        public int SubjectsAssigned { get; set; }
        public int AttendanceMarkedLastMonth { get; set; }
        public int ResultsEnteredLastMonth { get; set; }
    }
}
