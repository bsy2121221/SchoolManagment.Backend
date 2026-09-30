namespace SchoolManagment.Models.Entities
{
    /// <summary>
    /// A login. Account and credential data only -- everything about the human
    /// behind it lives on <see cref="Person"/>, and their addresses on
    /// <see cref="Address"/>.
    ///
    /// The flat FirstName / LastName / PhoneNumber / Address / Role / picture
    /// properties below are NOT columns on dbo.Users. They are what
    /// dbo.vw_Users returns, and that view is what every read path selects from,
    /// so Dapper still materialises a whole user in one hop. Writes must go
    /// through dbo.Users / dbo.Persons / dbo.Addresses (in practice through
    /// sp_CreateUserAccount and sp_UpdateUserIdentity) -- the view is read-only.
    /// </summary>
    public class User
    {
        // --- dbo.Users -------------------------------------------------------
        public int Id { get; set; }

        /// <summary>NULL for exactly one row: the platform SuperAdmin.</summary>
        public int? SchoolId { get; set; }

        /// <summary>dbo.Persons.Id. One person, one login (UQ_Users_PersonId).</summary>
        public int PersonId { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>dbo.Roles.Id. See Constants.RoleIds -- 1..5 are a contract.</summary>
        public int RoleId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool RequirePasswordChange { get; set; } = false;
        public DateTime? LastLoginAt { get; set; }

        // --- audit -----------------------------------------------------------
        /// <summary>Users.Id of whoever created this account. Self-referencing
        /// for the first account in a school (and for the SuperAdmin).</summary>
        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- from dbo.vw_Users (read-only projections) ------------------------
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>Computed by the view as FirstName + ' ' + LastName.</summary>
        public string? FullName { get; set; }

        public string? PhoneNumber { get; set; }
        public string? AlternatePhoneNumber { get; set; }

        /// <summary>Role name, e.g. "Admin". Kept for the ClaimTypes.Role claim
        /// and for the [Authorize(Roles = ...)] attributes already in place.</summary>
        public string Role { get; set; } = string.Empty;
        public string? RoleCode { get; set; }

        /// <summary>The primary active address, flattened to one line by the
        /// view. Structured parts follow.</summary>
        public string? Address { get; set; }
        public int? AddressId { get; set; }
        public string? AddressType { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Landmark { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        public byte[]? ProfilePicture { get; set; }
        public string? ProfilePictureFileName { get; set; }
        public string? ProfilePictureContentType { get; set; }
        public DateTime? ProfilePictureUploadDate { get; set; }

        public string? CreatedByUsername { get; set; }
        public string? ModifiedByUsername { get; set; }

        // --- joined by the login / lookup procs -------------------------------
        public string? SchoolCode { get; set; }
        public string? SchoolName { get; set; }

        /// <summary>The role-specific business key: admission number for a
        /// student, employee id for a teacher. Called RoleId by the old procs --
        /// renamed because RoleId is now the FK to dbo.Roles.</summary>
        public string? RoleIdentifier { get; set; }
    }
}
