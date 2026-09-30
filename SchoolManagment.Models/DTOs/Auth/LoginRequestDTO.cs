using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SchoolManagment.Models.DTOs.Auth
{
    public class LoginRequestDTO
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        [StringLength(50,MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;
    }
}
