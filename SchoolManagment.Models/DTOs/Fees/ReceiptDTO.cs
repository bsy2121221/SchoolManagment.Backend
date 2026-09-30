namespace SchoolManagment.Models.DTOs.Fees
{
    /// <summary>
    /// Everything a printed receipt needs, from <c>sp_GetReceipt</c>.
    ///
    /// <c>GET receipts/{receiptNumber}</c> used to map this into <c>PaymentHistoryDTO</c>, which
    /// has none of the school or class columns, and passed <c>@ReceiptNumber</c> to a procedure that
    /// only took <c>@PaymentId</c>, so it never returned anything.
    /// </summary>
    public class ReceiptDTO
    {
        public int PaymentId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? Remarks { get; set; }

        public int FeeId { get; set; }
        public decimal FeeAmount { get; set; }
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public DateTime DueDate { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        /// <summary>Completed payments against this fee as of now, not as of this receipt.</summary>
        public decimal FeeTotalPaid { get; set; }
        /// <summary>What is still owed on this fee as of now, not as of this receipt.</summary>
        public decimal FeeBalance { get; set; }

        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ClassName { get; set; }
        public string? Grade { get; set; }
        public string? Section { get; set; }

        public string? ReceivedBy { get; set; }
        public string SchoolName { get; set; } = string.Empty;
        public string SchoolCode { get; set; } = string.Empty;
        public string? SchoolAddress { get; set; }
        public string? SchoolPhone { get; set; }
    }
}
