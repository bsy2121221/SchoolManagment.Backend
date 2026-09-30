using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// Onboards a tenant. sp_CreateSchool does the school, its first admin, the
    /// number sequences, the default fee types, the default settings and an
    /// optional starter subject list in one transaction -- so everything the new
    /// school needs to be usable is described here.
    ///
    /// The subdomain bounds below mirror CK_Schools_Subdomain, which the procedure
    /// only lower-cases; uppercase input is therefore accepted and normalised, but
    /// a character the constraint forbids is rejected here rather than at the
    /// constraint.
    /// </summary>
    public class SchoolCreateDTO
    {
        /// <summary>
        /// Punctuation and case are free: "dps-noida" and "DPS Noida" both become
        /// DPSNOIDA, because fn_SanitizeCode strips everything outside A-Z0-9 and
        /// upper-cases the rest. So no character pattern is imposed here -- that
        /// would reject the punctuated forms the procedure is written to accept --
        /// and SchoolService checks the sanitised length instead.
        ///
        /// The 12-character cap counts the punctuation too, which looks stricter
        /// than it is: sp_CreateSchool declares @SchoolCode as NVARCHAR(12), so
        /// SQL Server truncates a longer value on the way in, *before* the
        /// procedure sanitises it. "zz-test-school" would arrive as "zz-test-scho"
        /// and be stored as ZZTESTSCHO -- a code the caller never asked for and has
        /// no way to predict. Refusing it here is the only way the code that comes
        /// back is the code that was sent.
        /// </summary>
        [Required(ErrorMessage = "School code is required")]
        [StringLength(12, ErrorMessage =
            "School code cannot exceed 12 characters, counting any spaces or punctuation that will be stripped from it")]
        public string SchoolCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "School name is required")]
        [StringLength(150, ErrorMessage = "School name cannot exceed 150 characters")]
        public string SchoolName { get; set; } = string.Empty;

        /// <summary>
        /// Optional. When set it must be unique across the platform, because it
        /// is what resolves a wildcard host to a tenant.
        /// </summary>
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
        public string ThemeColor { get; set; } = "#1976d2";

        [Range(1, 12, ErrorMessage = "Academic year start month must be between 1 and 12")]
        public byte AcademicYearStartMonth { get; set; } = 4;

        /// <summary>
        /// The first admin's email. Their username is derived by the procedure as
        /// CODE_ADMIN and cannot be chosen.
        /// </summary>
        [Required(ErrorMessage = "Admin email is required")]
        [EmailAddress(ErrorMessage = "Invalid admin email format")]
        [StringLength(100, ErrorMessage = "Admin email cannot exceed 100 characters")]
        public string AdminEmail { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Admin first name cannot exceed 50 characters")]
        public string AdminFirstName { get; set; } = "School";

        [StringLength(50, ErrorMessage = "Admin last name cannot exceed 50 characters")]
        public string AdminLastName { get; set; } = "Admin";

        [StringLength(15, ErrorMessage = "Admin phone number cannot exceed 15 characters")]
        public string? AdminPhoneNumber { get; set; }

        /// <summary>
        /// Optional. Leave it out and the admin gets the shared temporary
        /// password with RequirePasswordChange set, which is the normal path --
        /// supplying one here means the plaintext travelled through whoever
        /// filled the form in.
        /// </summary>
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "Admin password must be at least 8 characters")]
        public string? AdminPassword { get; set; }

        /// <summary>
        /// Writes English, Maths, Science, Social Studies, Hindi and Computer
        /// Science for grades 1-12. Off for a school that will import its own.
        /// </summary>
        public bool SeedSubjects { get; set; } = true;
    }
}
