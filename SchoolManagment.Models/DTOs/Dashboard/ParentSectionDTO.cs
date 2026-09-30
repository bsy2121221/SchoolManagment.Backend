namespace SchoolManagment.Models.DTOs.Dashboard
{
    /// <summary>
    /// A parent's own landing page: one card per linked child.
    ///
    /// Scoped to the signed-in parent, resolved from the token's user id, and to the
    /// children actually linked to them in StudentParents. A parent cannot reach a
    /// child they are not linked to through this endpoint.
    /// </summary>
    public class ParentSectionDTO
    {
        /// <summary>dbo.Parents.Id for the signed-in user.</summary>
        public int ParentId { get; set; }

        public List<ParentChildSummaryDTO> Children { get; set; } = new();
    }

    /// <summary>
    /// One child, with the two numbers a parent opens the app to check.
    ///
    /// Deliberately not the full student profile: a parent who wants marks or a fee
    /// breakdown follows the link. This is the summary that decides whether they need
    /// to.
    /// </summary>
    public class ParentChildSummaryDTO
    {
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        /// <summary>'Father', 'Mother', 'Guardian' -- from the StudentParents link.</summary>
        public string Relationship { get; set; } = string.Empty;

        public string ClassName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;

        /// <summary>
        /// Days present, absent and marked over the last month, not since admission --
        /// a parent checking in wants to know about the term in progress, and a lifetime
        /// percentage barely moves. Null when the parent's role does not carry
        /// Attendance:View, where a zero would read as a month of absence.
        /// </summary>
        public int? PresentDays { get; set; }

        public int? AbsentDays { get; set; }
        public int? TotalDays { get; set; }

        /// <summary>
        /// Null when no attendance has been marked for this child in the window, and
        /// equally when attendance was withheld. <see cref="DashboardDTO.Omitted"/>
        /// distinguishes the two.
        /// </summary>
        public decimal? AttendancePercentage { get; set; }

        /// <summary>
        /// Total still owed across the child's active fees. Null when the parent's role
        /// does not carry Fees:View -- the seeded grid does give it to them, but a
        /// customised role need not, and a zero would be indistinguishable from
        /// "nothing owed".
        /// </summary>
        public decimal? OutstandingAmount { get; set; }

        /// <summary>
        /// How many of those fees are past their due date. Null under the same
        /// condition as <see cref="OutstandingAmount"/>.
        /// </summary>
        public int? OverdueFeeCount { get; set; }
    }
}
