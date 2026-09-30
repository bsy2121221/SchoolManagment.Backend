namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// Per-tenant activity over a date window, for billing or capacity work. The
    /// counts are window-bound; the head counts are current.
    /// </summary>
    public class SchoolUsageReportDTO
    {
        public int SchoolId { get; set; }
        public string SchoolCode { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int ActiveStudents { get; set; }
        public int ActiveTeachers { get; set; }
        public int AttendanceRecords { get; set; }
        public int ResultsEntered { get; set; }
        public decimal FeesCollected { get; set; }

        /// <summary>Null for a school nobody has ever signed in to.</summary>
        public DateTime? LastLoginAt { get; set; }
    }
}
