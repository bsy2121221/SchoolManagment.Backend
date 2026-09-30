namespace SchoolManagment.Models.Entities
{
    /// <summary>
    /// dbo.Persons -- the human. Split out of Users so that identity data is
    /// stored once and is not coupled to having a login.
    ///
    /// Deliberately carries no DateOfBirth or Gender: dbo.Students owns those,
    /// and duplicating them here would give two answers to one question.
    /// </summary>
    public class Person
    {
        public int Id { get; set; }

        /// <summary>NULL only for the platform SuperAdmin's person row.</summary>
        public int? SchoolId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? AlternatePhoneNumber { get; set; }

        public byte[]? ProfilePicture { get; set; }
        public string? ProfilePictureFileName { get; set; }
        public string? ProfilePictureContentType { get; set; }
        public DateTime? ProfilePictureUploadDate { get; set; }

        public bool IsActive { get; set; } = true;

        public int? CreatedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
