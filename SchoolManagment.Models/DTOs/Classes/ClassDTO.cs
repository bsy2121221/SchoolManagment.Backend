namespace SchoolManagment.Models.DTOs.Classes
{
    public class ClassDTO
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public int? ClassTeacherId { get; set; }
        public string? ClassTeacherName { get; set; }
        public int TotalStudents { get; set; }
        public int MaxStudents { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
