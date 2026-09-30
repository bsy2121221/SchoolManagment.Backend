namespace SchoolManagment.Models.DTOs.Schedule
{
    public class CurrentNextClassDTO
    {
        public string ClassType { get; set; } = string.Empty; // "Current" or "Next"
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public int DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Room { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string DayName { get; set; } = string.Empty;
        public string StartTimeFormatted { get; set; } = string.Empty;
        public string EndTimeFormatted { get; set; } = string.Empty;
    }

    public class CurrentNextClassesResponseDTO
    {
        public CurrentNextClassDTO? CurrentClass { get; set; }
        public CurrentNextClassDTO? NextClass { get; set; }
    }
}
