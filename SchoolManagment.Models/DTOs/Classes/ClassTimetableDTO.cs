namespace SchoolManagment.Models.DTOs.Classes
{
    public class ClassTimetableDTO
    {
        public string ClassName { get; set; } = string.Empty;
        public List<DayScheduleDTO> Timetable { get; set; } = new();
    }

    public class DayScheduleDTO
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public List<PeriodDTO> Periods { get; set; } = new();
    }

    public class PeriodDTO
    {
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string? Room { get; set; }
    }
}
