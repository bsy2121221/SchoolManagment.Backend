namespace SchoolManagment.Models.DTOs.Dashboard
{
    /// <summary>
    /// Everything a landing page needs, in one response.
    ///
    /// One endpoint serves all five roles rather than one endpoint per role. The
    /// sections are nullable and only the ones that apply to the caller are populated,
    /// so the client makes a single request and reads the section it has a screen for.
    /// The alternative -- /dashboard/admin, /dashboard/teacher and so on -- would have
    /// the client decide which URL its own token justifies, which is a decision the
    /// server has to make again anyway.
    ///
    /// Which sections appear:
    ///
    ///   * <see cref="Platform"/> -- a SuperAdmin with no school in scope. Switching
    ///     into a school replaces it with <see cref="School"/>.
    ///   * <see cref="School"/> -- anyone holding Reports:View, which the seeded grid
    ///     gives to SuperAdmin, Admin and Teacher. Not gated on the individual modules
    ///     it counts: a student holds Attendance:View so they can see their own
    ///     register, and that must not be read as leave to see the school's.
    ///   * <see cref="Teacher"/>, <see cref="Student"/>, <see cref="Parent"/> -- by
    ///     role, because "my own record" is not something the permission grid can
    ///     express. Each is resolved from the token's user id, so none of them can be
    ///     pointed at somebody else.
    ///   * <see cref="RecentActivity"/> -- always. It is the caller's own audit rows.
    ///
    /// A section that is absent is absent because it does not apply. A section that is
    /// present but has null fields inside it was trimmed by the permission grid, and
    /// <see cref="Omitted"/> says so.
    /// </summary>
    public class DashboardDTO
    {
        /// <summary>The role the sections below were chosen for.</summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>Null for a platform administrator who has not switched into a school.</summary>
        public int? SchoolId { get; set; }

        /// <summary>
        /// From the token rather than a lookup, which is why it is the code and not the
        /// school's display name -- the name would cost a round trip the login response
        /// already paid for.
        /// </summary>
        public string? SchoolCode { get; set; }

        /// <summary>
        /// Server time the figures were read. Worth showing: several of them are
        /// "today" counts, and a tab left open overnight is reporting yesterday.
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        public SchoolStatsDTO? School { get; set; }
        public PlatformSectionDTO? Platform { get; set; }
        public TeacherSectionDTO? Teacher { get; set; }
        public StudentSectionDTO? Student { get; set; }
        public ParentSectionDTO? Parent { get; set; }

        public List<ActivityDTO> RecentActivity { get; set; } = new();

        /// <summary>
        /// What was left out and why. Empty for a school administrator, who holds
        /// everything. Non-empty for a teacher, whose role carries no Fees row, and for
        /// any customised role -- so the UI can say "not available to your role" instead
        /// of drawing a card that reads zero.
        /// </summary>
        public List<OmittedSectionDTO> Omitted { get; set; } = new();
    }

    /// <summary>
    /// One thing the caller was not shown. Present so the difference between "nothing
    /// to report" and "not yours to see" survives the trip to the client.
    /// </summary>
    public class OmittedSectionDTO
    {
        /// <summary>
        /// The field or group withheld, named as it appears in the response --
        /// "school.feesOutstandingAmount", "children[].outstandingAmount".
        /// </summary>
        public string Field { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public OmittedSectionDTO()
        {
        }

        public OmittedSectionDTO(string field, string reason)
        {
            Field = field;
            Reason = reason;
        }
    }
}
