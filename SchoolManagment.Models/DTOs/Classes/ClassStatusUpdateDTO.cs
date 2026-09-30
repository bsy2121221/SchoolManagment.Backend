using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Classes
{
    public class ClassStatusUpdateDTO
    {
        [Required(ErrorMessage = "IsActive status is required")]
        public bool IsActive { get; set; }
    }
}
