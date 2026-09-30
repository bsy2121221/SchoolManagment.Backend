using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Examinations
{
    public class ExaminationUpdateDTO
    {
        [Required(ErrorMessage = "Exam name is required")]
        [StringLength(100, ErrorMessage = "Exam name cannot exceed 100 characters")]
        public string ExamName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Exam type is required")]
        [StringLength(50, ErrorMessage = "Exam type cannot exceed 50 characters")]
        public string ExamType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Class ID is required")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Subject ID is required")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "Exam date is required")]
        public DateTime ExamDate { get; set; }

        [Required(ErrorMessage = "Maximum marks is required")]
        [Range(1, 1000, ErrorMessage = "Maximum marks must be between 1 and 1000")]
        public int MaxMarks { get; set; }

        [Required(ErrorMessage = "Passing marks is required")]
        [Range(0, 1000, ErrorMessage = "Passing marks must be between 0 and 1000")]
        public int PassingMarks { get; set; }

        [Range(1, 600, ErrorMessage = "Duration must be between 1 and 600 minutes")]
        public int? Duration { get; set; }
    }
}
