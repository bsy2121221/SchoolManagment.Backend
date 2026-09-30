using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Roles
{
    public class RoleUpdateDTO
    {
        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
