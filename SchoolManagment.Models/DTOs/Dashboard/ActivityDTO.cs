namespace SchoolManagment.Models.DTOs.Dashboard
{
    /// <summary>
    /// One row of the signed-in user's own audit history, as sp_GetProfileActivities
    /// returns it.
    ///
    /// These are real AuditLog rows. The procedure's header records that the previous
    /// version synthesised them from Users.UpdatedAt, which gave every user a
    /// "Password Change" entry they had never performed; nothing here invents an
    /// event that did not happen.
    /// </summary>
    public class ActivityDTO
    {
        /// <summary>AuditLog.Action -- 'Login', 'Update', 'Create' and so on.</summary>
        public string ActivityType { get; set; } = string.Empty;

        public DateTime ActivityDate { get; set; }

        /// <summary>
        /// The action with whatever context the row carries, already assembled by the
        /// procedure. Meant to be displayed as-is.
        /// </summary>
        public string ActivityDescription { get; set; } = string.Empty;

        /// <summary>The table the action touched, when the row names one.</summary>
        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        public string? IpAddress { get; set; }
    }
}
