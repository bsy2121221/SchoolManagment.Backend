namespace SchoolManagment.Models.DTOs.Schedule
{
    public class ClassScheduleDTO
    {
        public int Id { get; set; }
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string StartTimeFormatted { get; set; } = string.Empty;
        public string EndTimeFormatted { get; set; } = string.Empty;
        public string? Room { get; set; }

        // Subject details
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;

        // Teacher details
        public int TeacherId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;

        // Class details
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
    }
}
