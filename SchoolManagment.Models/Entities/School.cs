namespace SchoolManagment.Models.Entities
{
    public class School
    {
        public int Id { get; set; }
        public string SchoolCode { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string? Subdomain { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? PrincipalName { get; set; }
        public string? LogoUrl { get; set; }
        public string ThemeColor { get; set; } = "#1976d2";
        public byte AcademicYearStartMonth { get; set; } = 4;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
