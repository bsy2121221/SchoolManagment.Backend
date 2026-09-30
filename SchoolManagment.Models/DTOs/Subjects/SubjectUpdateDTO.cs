using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Subjects
{
    public class SubjectUpdateDTO
    {
        [Required(ErrorMessage = "Subject name is required")]
        [StringLength(100, ErrorMessage = "Subject name cannot exceed 100 characters")]
        public string SubjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject code is required")]
        [StringLength(20, ErrorMessage = "Subject code cannot exceed 20 characters")]
        public string SubjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grade is required")]
        [StringLength(10, ErrorMessage = "Grade cannot exceed 10 characters")]
        public string Grade { get; set; } = string.Empty;
    }
}
