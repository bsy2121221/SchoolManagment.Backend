namespace SchoolManagment.Models.DTOs.Schedule
{
    public class ScheduleEntryDTO
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public int DayOfWeek { get; set; } // 1 = Monday, 7 = Sunday
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Room { get; set; }
        public bool IsActive { get; set; }

        // Subject details
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;

        // Class details
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;

        // Formatted data
        public string DayName { get; set; } = string.Empty;
        public string StartTimeFormatted { get; set; } = string.Empty;
        public string EndTimeFormatted { get; set; } = string.Empty;
    }
}
