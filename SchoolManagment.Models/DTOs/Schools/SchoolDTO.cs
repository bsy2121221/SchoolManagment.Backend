namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// One tenant, as sp_GetSchoolById / sp_GetSchoolByCode return it.
    /// Contact details are included, so this must never be served anonymously --
    /// see <see cref="SchoolBrandingDTO"/> for the public subset.
    /// </summary>
    public class SchoolDTO
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

        /// <summary>1-12. Drives which calendar months belong to a session.</summary>
        public byte AcademicYearStartMonth { get; set; } = 4;

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
