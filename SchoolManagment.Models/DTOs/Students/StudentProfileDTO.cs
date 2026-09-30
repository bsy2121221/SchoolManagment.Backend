namespace SchoolManagment.Models.DTOs.Students
{
    public class StudentProfileDTO
    {
        public StudentDTO? StudentInfo { get; set; }
        public AcademicStatsDTO? AcademicStats { get; set; }
        public List<SubjectDTO>? Subjects { get; set; }
        public FeeStatusDTO? FeeStatus { get; set; }
    }

    public class AcademicStatsDTO
    {
        public int TotalSubjects { get; set; }
        public decimal? AverageMarks { get; set; }
        public string? Grade { get; set; }
        public int? Rank { get; set; }
        public decimal AttendancePercentage { get; set; }
    }

    public class SubjectDTO
    {
        public int Id { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
    }

    public class FeeStatusDTO
    {
        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PendingFees { get; set; }
    }
}
