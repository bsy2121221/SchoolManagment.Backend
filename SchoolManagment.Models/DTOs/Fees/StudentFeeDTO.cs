namespace SchoolManagment.Models.DTOs.Fees
{
    public class StudentFeeDTO
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty; // Paid, Pending, Overdue
        public int SchoolId { get; set; }
    }
}
