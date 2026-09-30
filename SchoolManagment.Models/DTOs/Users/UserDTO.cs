namespace SchoolManagment.Models.DTOs.Users
{
    /// <summary>
    /// Deliberately still flat. Users, Persons and Addresses are three tables in
    /// the database now, but the read procs go through dbo.vw_Users and return
    /// the same column names they always did, so no existing client field path
    /// breaks. RoleId, the audit columns and the structured address parts are
    /// additions.
    /// </summary>
    public class UserDTO
    {
        public int Id { get; set; }
        public int? SchoolId { get; set; }

        /// <summary>dbo.Persons.Id, for callers that edit the person directly.</summary>
        public int PersonId { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AlternatePhoneNumber { get; set; }

        /// <summary>The primary address flattened to one line, as before.</summary>
        public string? Address { get; set; }

        // Structured address, from the same primary Addresses row.
        public int? AddressId { get; set; }
        public string? AddressType { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Landmark { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        /// <summary>dbo.Roles.Id -- see Constants.RoleIds.</summary>
        public int RoleId { get; set; }

        /// <summary>Role name. Unchanged in meaning and still the field the UI
        /// displays.</summary>
        public string Role { get; set; } = string.Empty;
        public string? RoleCode { get; set; }

        public bool IsActive { get; set; }
        public bool RequirePasswordChange { get; set; }
        public bool HasProfilePicture { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Audit trail.
        public int? CreatedBy { get; set; }
        public string? CreatedByUsername { get; set; }
        public int? ModifiedBy { get; set; }
        public string? ModifiedByUsername { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public string? SchoolCode { get; set; }
        public string? SchoolName { get; set; }

        /// <summary>Admission number or employee id. This was called RoleId by
        /// the old procs; renamed because RoleId now means dbo.Roles.Id.</summary>
        public string? RoleIdentifier { get; set; }
    }
}
