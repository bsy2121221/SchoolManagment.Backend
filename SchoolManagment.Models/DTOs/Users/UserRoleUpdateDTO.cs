using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Users
{
    public class UserRoleUpdateDTO
    {
        /// <summary>dbo.Roles.Id. Custom roles start at 100, so 1 is the lowest
        /// valid value only because the SuperAdmin role exists -- moving anybody
        /// into it is refused by the database.</summary>
        [Required(ErrorMessage = "Role ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Role ID must be a positive number")]
        public int RoleId { get; set; }
    }
}
