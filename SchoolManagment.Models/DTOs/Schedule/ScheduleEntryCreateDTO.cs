using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Schedule
{
    public class ScheduleEntryCreateDTO
    {
        [Required(ErrorMessage = "Teacher ID is required")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Subject ID is required")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Class ID is required")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Day of week is required")]
        [Range(1, 7, ErrorMessage = "Day of week must be between 1 (Monday) and 7 (Sunday)")]
        public int DayOfWeek { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public TimeSpan EndTime { get; set; }

        [StringLength(50, ErrorMessage = "Room cannot exceed 50 characters")]
        public string? Room { get; set; }
    }
}
