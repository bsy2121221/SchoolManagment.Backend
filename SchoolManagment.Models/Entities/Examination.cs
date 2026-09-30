namespace SchoolManagment.Models.Entities
{
    public class Examination
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public DateTime ExamDate { get; set; }
        public int MaxMarks { get; set; }
        public int PassingMarks { get; set; }
        public int? Duration { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
