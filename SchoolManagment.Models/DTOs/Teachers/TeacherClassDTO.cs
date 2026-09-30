namespace SchoolManagment.Models.DTOs.Teachers
{
    public class TeacherClassDTO
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public int MaxStudents { get; set; }
        public int StudentCount { get; set; }
    }
}
