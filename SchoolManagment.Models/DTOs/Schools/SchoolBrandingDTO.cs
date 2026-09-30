namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// The public face of a tenant: enough to brand a login page before anyone
    /// has signed in, and nothing more. Contact details, counts and the rest of
    /// <see cref="SchoolDTO"/> are deliberately absent, because the endpoints
    /// serving this are anonymous and a competitor or scraper can reach them.
    /// </summary>
    public class SchoolBrandingDTO
    {
        public int SchoolId { get; set; }
        public string SchoolCode { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;

        /// <summary>Set by the by-subdomain lookup; null from the branding
        /// lookup, which does not select it.</summary>
        public string? Subdomain { get; set; }

        public string? LogoUrl { get; set; }
        public string ThemeColor { get; set; } = "#1976d2";

        /// <summary>
        /// Returned so the login page can say the school is suspended rather than
        /// letting its staff fail authentication with no explanation.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
