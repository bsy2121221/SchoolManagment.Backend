namespace SchoolManagment.Models.DTOs.Parents
{
    public class ParentProfileDTO
    {
        public int Id { get; set; }
        public string? Occupation { get; set; }
        public decimal? AnnualIncome { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // User details
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public bool RequirePasswordChange { get; set; }

        // School info
        public int SchoolId { get; set; }
        public string? SchoolCode { get; set; }
        public string? SchoolName { get; set; }

        // Children
        public List<ParentChildDTO> Children { get; set; } = new();
    }

    public class ParentChildDTO
    {
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
    }
}
