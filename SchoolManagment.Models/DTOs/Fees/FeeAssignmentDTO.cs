using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Fees
{
    /// <summary>
    /// One fee to bill. <c>FeeMonth</c> and <c>FeeYear</c> default to the due date's, and together
    /// with the fee type they are what makes a bill unique: a second bill for the same type and
    /// period is skipped, not duplicated.
    /// </summary>
    public class FeeAssignmentItemDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Choose a fee type")]
        public int FeeTypeId { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999999.99")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }

        [Range(1, 12, ErrorMessage = "Billing month must be between 1 and 12")]
        public int? FeeMonth { get; set; }

        [Range(2000, 2200, ErrorMessage = "Billing year must be between 2000 and 2200")]
        public int? FeeYear { get; set; }
    }

    /// <summary>Body of <c>POST /api/Fees/students/{studentId}</c>.</summary>
    public class StudentFeeAssignDTO
    {
        [Required]
        [MinLength(1, ErrorMessage = "Add at least one fee")]
        [MaxLength(50, ErrorMessage = "At most 50 fees can be billed at once")]
        public List<FeeAssignmentItemDTO> Fees { get; set; } = new();
    }

    /// <summary>Body of <c>POST /api/Fees/classes/{classId}</c>: one fee, every active student.</summary>
    public class ClassFeeAssignDTO : FeeAssignmentItemDTO
    {
    }

    /// <summary>
    /// What a billing write did. <c>FeesCreated + FeesSkipped</c> is what was asked for; a 200 with
    /// a non-zero <c>FeesSkipped</c> is a partial success and must be reported as one.
    /// </summary>
    public class FeeAssignResponseDTO
    {
        public int FeesCreated { get; set; }
        public int FeesSkipped { get; set; }
    }
}
