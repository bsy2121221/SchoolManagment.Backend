namespace SchoolManagment.Models.Entities
{
    public class Attendance
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int StudentId { get; set; }
        public int ClassId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public bool IsPresent { get; set; }
        public string? Remarks { get; set; }
        public int MarkedBy { get; set; }
        public DateTime MarkedAt { get; set; }
    }
}
