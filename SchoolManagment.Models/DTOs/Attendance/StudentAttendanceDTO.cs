namespace SchoolManagment.Models.DTOs.Attendance
{
    public class StudentAttendanceDTO
    {
        public int Id { get; set; }
        public DateTime AttendanceDate { get; set; }
        public bool IsPresent { get; set; }
        public string? Remarks { get; set; }
        public DateTime? MarkedAt { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
    }
}
