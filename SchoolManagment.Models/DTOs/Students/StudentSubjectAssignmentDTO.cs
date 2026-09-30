using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Students
{
    public class StudentSubjectAssignmentDTO
    {
        [Required(ErrorMessage = "Subject IDs are required")]
        [MinLength(1, ErrorMessage = "At least one subject must be selected")]
        public List<int> SubjectIds { get; set; } = new();
    }
}
