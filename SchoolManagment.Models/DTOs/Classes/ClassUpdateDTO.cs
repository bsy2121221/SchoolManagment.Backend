using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Classes
{
    public class ClassUpdateDTO
    {
        [Required(ErrorMessage = "Class name is required")]
        [StringLength(50, ErrorMessage = "Class name cannot exceed 50 characters")]
        public string ClassName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Grade is required")]
        [StringLength(10, ErrorMessage = "Grade cannot exceed 10 characters")]
        public string Grade { get; set; } = string.Empty;

        [Required(ErrorMessage = "Section is required")]
        [StringLength(5, ErrorMessage = "Section cannot exceed 5 characters")]
        public string Section { get; set; } = string.Empty;

        public int? ClassTeacherId { get; set; }

        [Range(1, 200, ErrorMessage = "Max students must be between 1 and 200")]
        public int MaxStudents { get; set; }
    }
}
