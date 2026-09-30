using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Students
{
    public class StudentPromoteDTO
    {
        [Required(ErrorMessage = "New class ID is required")]
        public int NewClassId { get; set; }

        [Required(ErrorMessage = "Academic year is required")]
        public int AcademicYear { get; set; }
    }
}
