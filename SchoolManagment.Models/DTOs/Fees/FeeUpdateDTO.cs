using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Fees
{
    /// <summary>Correct what one student owes for one fee. Refused below what has already been paid.</summary>
    public class FeeUpdateDTO
    {
        [Required]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999999.99")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }
    }

    public class RefundRequestDTO
    {
        /// <summary>Stored in the payment's Remarks, replacing what was there.</summary>
        [Required(ErrorMessage = "A reason for the refund is required")]
        [StringLength(255, ErrorMessage = "Reason cannot exceed 255 characters")]
        public string Reason { get; set; } = string.Empty;
    }
}
