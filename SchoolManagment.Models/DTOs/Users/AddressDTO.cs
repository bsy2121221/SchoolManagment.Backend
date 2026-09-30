using System.ComponentModel.DataAnnotations;
using SchoolManagment.Models.Common;

namespace SchoolManagment.Models.DTOs.Users
{
    /// <summary>
    /// The structured address, for callers that want to send or read the parts
    /// rather than the single flattened line that UserDTO.Address still carries.
    /// </summary>
    public class AddressDTO
    {
        public int? Id { get; set; }

        [RegularExpression("^(Permanent|Current|Correspondence)$",
            ErrorMessage = "Address type must be Permanent, Current or Correspondence")]
        public string AddressType { get; set; } = Constants.AddressTypes.Permanent;

        [Required(ErrorMessage = "Address line 1 is required")]
        [StringLength(255, ErrorMessage = "Address line 1 cannot exceed 255 characters")]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Address line 2 cannot exceed 255 characters")]
        public string? AddressLine2 { get; set; }

        [StringLength(100, ErrorMessage = "Landmark cannot exceed 100 characters")]
        public string? Landmark { get; set; }

        [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
        public string? City { get; set; }

        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
        public string? State { get; set; }

        [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters")]
        public string? Country { get; set; }

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        public string? PostalCode { get; set; }

        public bool IsPrimary { get; set; } = true;
    }
}
