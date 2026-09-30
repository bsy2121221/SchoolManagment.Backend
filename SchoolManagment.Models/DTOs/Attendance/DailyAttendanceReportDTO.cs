namespace SchoolManagment.Models.DTOs.Attendance
{
    public class DailyAttendanceReportDTO
    {
        public DateTime AttendanceDate { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int TotalMarked { get; set; }
        public decimal? AttendancePercentage { get; set; }
    }
}
