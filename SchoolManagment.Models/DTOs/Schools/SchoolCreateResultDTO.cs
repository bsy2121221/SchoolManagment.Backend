namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// What sp_CreateSchool reports back. The admin's username is generated
    /// (CODE_ADMIN), so the caller cannot know it without being told.
    /// </summary>
    public class SchoolCreateResultDTO
    {
        public int SchoolId { get; set; }

        /// <summary>The sanitised code that was actually stored, which may differ
        /// from what was sent.</summary>
        public string SchoolCode { get; set; } = string.Empty;

        public int AdminUserId { get; set; }
        public string AdminUsername { get; set; } = string.Empty;

        /// <summary>
        /// True when no password was supplied and the account therefore holds the
        /// shared temporary password and must change it at first login. The
        /// password itself is never returned.
        /// </summary>
        public bool RequiresPasswordChange { get; set; }
    }
}
