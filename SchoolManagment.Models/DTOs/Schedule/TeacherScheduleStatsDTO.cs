namespace SchoolManagment.Models.DTOs.Schedule
{
    public class TeacherScheduleStatsDTO
    {
        public int TotalClasses { get; set; }
        public int TotalSubjects { get; set; }
        public int TotalClassesAssigned { get; set; }
        public int DaysInWeek { get; set; }
        public TimeSpan? EarliestClass { get; set; }
        public TimeSpan? LatestClass { get; set; }
        public int TotalMinutesPerWeek { get; set; }
    }
}
