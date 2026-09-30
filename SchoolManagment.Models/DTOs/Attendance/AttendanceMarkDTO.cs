using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Attendance
{
    public class AttendanceMarkDTO
    {
        [Required(ErrorMessage = "Student ID is required")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Class ID is required")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Attendance date is required")]
        public DateTime AttendanceDate { get; set; }

        [Required(ErrorMessage = "Attendance status is required")]
        public bool IsPresent { get; set; }

        [StringLength(255, ErrorMessage = "Remarks cannot exceed 255 characters")]
        public string? Remarks { get; set; }
    }
}
