namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// Totals across every tenant, for the SuperAdmin's landing page. The user
    /// counts are by role, so a custom role is not counted in any of them.
    /// </summary>
    public class PlatformStatsDTO
    {
        public int TotalSchools { get; set; }
        public int ActiveSchools { get; set; }
        public int InactiveSchools { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalParents { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalClasses { get; set; }
        public int SchoolsAddedLast30Days { get; set; }
    }
}
