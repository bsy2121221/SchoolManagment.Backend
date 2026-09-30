namespace SchoolManagment.Models.DTOs.Fees
{
    /// <summary>
    /// One billing period's row from <c>sp_GetFeeCollectionSummary</c>.
    ///
    /// Replaces <c>FeeCollectionSummaryDTO</c>, whose seven school-wide totals
    /// (<c>TotalExpected</c>, <c>StudentsPaidFully</c>, <c>CollectionPercentage</c>...) matched no
    /// column the procedure returns. Dapper filled every one of them with zero, and the repository
    /// read only the first of what is really a per-month result set.
    ///
    /// Grouped by the fee's billing period, not by payment date: a March fee paid in May is
    /// "collected" in March here, and in May on the dashboard's <c>FeesCollectedThisMonth</c>.
    /// </summary>
    public class FeeCollectionPeriodDTO
    {
        public int FeeYear { get; set; }
        public int FeeMonth { get; set; }
        /// <summary>How many active fees were billed for this period.</summary>
        public int FeeCount { get; set; }
        public decimal Billed { get; set; }
        /// <summary>Completed payments only; a refunded payment is not collected.</summary>
        public decimal Collected { get; set; }
        public decimal Outstanding { get; set; }
    }
}
