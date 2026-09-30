using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Fees
{
    public class FeeTypeDTO
    {
        public int Id { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>Prefills new bills only. Changing it never re-prices a fee already billed.</summary>
        public decimal? DefaultAmount { get; set; }
        public bool IsActive { get; set; }
        public int SchoolId { get; set; }
    }

    public class FeeTypeCreateDTO
    {
        [Required(ErrorMessage = "Fee type name is required")]
        [StringLength(100, ErrorMessage = "Fee type name cannot exceed 100 characters")]
        public string FeeTypeName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Default amount must be between 0 and 999999.99")]
        public decimal? DefaultAmount { get; set; }
    }

    /// <summary>
    /// The created fee type's id. The <c>Result</c> string it used to carry moved to the envelope:
    /// the controller compared it against "Fee type created successfully", which the procedure never
    /// returns, so every create was reported as a failure after the row was written.
    /// </summary>
    public class FeeTypeResponseDTO
    {
        public int FeeTypeId { get; set; }
    }
}
