using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Users
{
    public class UserStatusUpdateDTO
    {
        [Required(ErrorMessage = "IsActive status is required")]
        public bool IsActive { get; set; }
    }
}
