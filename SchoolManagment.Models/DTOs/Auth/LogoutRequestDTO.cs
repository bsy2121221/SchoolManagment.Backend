using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Auth
{
    public class LogoutRequestDTO
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
