namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>What sp_CreateSchoolAdmin reports back.</summary>
    public class SchoolAdminCreateResultDTO
    {
        public int UserId { get; set; }

        /// <summary>The generated or prefixed username actually stored.</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// True when the account holds the shared temporary password and must
        /// change it at first login. The password itself is never returned.
        /// </summary>
        public bool RequiresPasswordChange { get; set; }
    }
}
