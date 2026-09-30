namespace SchoolManagment.Models.DTOs.Fees
{
    /// <summary>
    /// One unpaid fee from <c>sp_GetOutstandingFees</c>.
    ///
    /// The class columns are nullable: the procedure LEFT JOINs <c>Classes</c>, and a student with
    /// no class used to make Dapper throw on <c>int ClassId</c>, failing the whole report for one row.
    /// </summary>
    public class OutstandingFeeDTO
    {
        public int FeeId { get; set; }
        /// <summary>Students.Id.</summary>
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        /// <summary>False for a student who has left. They still owe it, so they are listed.</summary>
        public bool StudentIsActive { get; set; }
        public int? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? Grade { get; set; }
        public string? Section { get; set; }
        public int FeeTypeId { get; set; }
        public string FeeTypeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public DateTime DueDate { get; set; }
        public int FeeMonth { get; set; }
        public int FeeYear { get; set; }
        public int DaysOverdue { get; set; }
        /// <summary>Overdue or Pending.</summary>
        public string Status { get; set; } = string.Empty;
    }
}
