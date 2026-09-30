using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Teachers
{
    public class TeacherSubjectAssignmentDTO
    {
        [Required(ErrorMessage = "At least one subject is required")]
        public List<int> SubjectIds { get; set; } = new();
    }

    public class TeacherSubjectDTO
    {
        public int Id { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int TeacherSubjectId { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class TeacherClassSubjectAssignmentDTO
    {
        [Required(ErrorMessage = "Teacher ID is required")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "Subject ID is required")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Class ID is required")]
        public int ClassId { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class TeacherSubjectClassDTO
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
        public int ClassId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectGrade { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string ClassGrade { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }
}
