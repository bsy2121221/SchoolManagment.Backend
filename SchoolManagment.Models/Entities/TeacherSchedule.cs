namespace SchoolManagment.Models.Entities
{
    public class TeacherSchedule
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public int ClassId { get; set; }
        public int DayOfWeek { get; set; } // 1 = Monday, 7 = Sunday
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Room { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
