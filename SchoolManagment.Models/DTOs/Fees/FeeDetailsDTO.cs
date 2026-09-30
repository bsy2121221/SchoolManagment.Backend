namespace SchoolManagment.Models.DTOs.Fees
{
    /// <summary>
    /// <c>sp_GetFeeDetails</c>'s row. <c>GET /api/Fees/{feeId}</c> used to map this into
    /// <c>StudentFeeDTO</c>, which has no student columns, so the name and roll number the
    /// procedure returns were dropped and <c>Status</c> came back empty.
    /// </summary>
    public class FeeDetailsDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>Students.Id.</summary>
        public int StudentId { get; set; }
        /// <summary>The admission number.</summary>
        public string StudentNumber { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        /// <summary>Paid, Overdue or Pending.</summary>
        public string Status { get; set; } = string.Empty;
        public int SchoolId { get; set; }
    }
}
