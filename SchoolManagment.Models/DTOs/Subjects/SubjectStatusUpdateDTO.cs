using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Subjects
{
    public class SubjectStatusUpdateDTO
    {
        [Required(ErrorMessage = "IsActive status is required")]
        public bool IsActive { get; set; }
    }
}
