using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// A partial update: sp_UpdateSchool keeps the stored value for every field
    /// left null, so a caller sends only what changed.
    ///
    /// SchoolCode is deliberately absent. It is embedded verbatim in every
    /// username and admission number already issued, so it cannot be edited.
    /// </summary>
    public class SchoolUpdateDTO
    {
        [StringLength(150, ErrorMessage = "School name cannot exceed 150 characters")]
        public string? SchoolName { get; set; }

        [StringLength(63, MinimumLength = 2,
            ErrorMessage = "Subdomain must be between 2 and 63 characters")]
        [RegularExpression(@"^[A-Za-z0-9]([A-Za-z0-9-]*[A-Za-z0-9])?$",
            ErrorMessage = "Subdomain may contain only letters, digits and hyphens, and cannot start or end with a hyphen")]
        public string? Subdomain { get; set; }

        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters")]
        public string? Address { get; set; }

        [StringLength(80, ErrorMessage = "City cannot exceed 80 characters")]
        public string? City { get; set; }

        [StringLength(80, ErrorMessage = "State cannot exceed 80 characters")]
        public string? State { get; set; }

        [StringLength(80, ErrorMessage = "Country cannot exceed 80 characters")]
        public string? Country { get; set; }

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        public string? PostalCode { get; set; }

        [EmailAddress(ErrorMessage = "Invalid contact email format")]
        [StringLength(100, ErrorMessage = "Contact email cannot exceed 100 characters")]
        public string? ContactEmail { get; set; }

        [StringLength(20, ErrorMessage = "Contact phone cannot exceed 20 characters")]
        public string? ContactPhone { get; set; }

        [StringLength(100, ErrorMessage = "Principal name cannot exceed 100 characters")]
        public string? PrincipalName { get; set; }

        [StringLength(500, ErrorMessage = "Logo URL cannot exceed 500 characters")]
        public string? LogoUrl { get; set; }

        [StringLength(20, ErrorMessage = "Theme colour cannot exceed 20 characters")]
        [RegularExpression(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$",
            ErrorMessage = "Theme colour must be a hex colour such as #1976d2")]
        public string? ThemeColor { get; set; }

        [Range(1, 12, ErrorMessage = "Academic year start month must be between 1 and 12")]
        public byte? AcademicYearStartMonth { get; set; }

        /// <summary>
        /// Removing a subdomain needs its own flag: a null Subdomain means "leave
        /// it alone", so there would otherwise be no way to express "clear it".
        /// </summary>
        public bool ClearSubdomain { get; set; }
    }
}
