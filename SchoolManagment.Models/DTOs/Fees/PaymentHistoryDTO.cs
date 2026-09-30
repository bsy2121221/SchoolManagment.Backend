namespace SchoolManagment.Models.DTOs.Fees
{
    /// <summary>
    /// One row of <c>sp_GetPaymentHistory</c>: a student's history or the school ledger.
    ///
    /// <c>StudentId</c> is <c>Students.Id</c> now. It was a <c>string</c> holding the admission
    /// number, which is what the procedure used to return under that name; the admission number
    /// is <c>StudentNumber</c>, as on the outstanding report and the receipt.
    /// </summary>
    public class PaymentHistoryDTO
    {
        public int Id { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        /// <summary>Completed or Refunded in practice; the ledger includes refunds.</summary>
        public string PaymentStatus { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public int FeeId { get; set; }
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public decimal TotalAmount { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        /// <summary>The cashier. Null only if their account has since been removed.</summary>
        public string? ReceivedBy { get; set; }
        public int SchoolId { get; set; }
    }
}
