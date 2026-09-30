using Dapper;
using SchoolManagment.DataAccess.Context;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Fees;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace SchoolManagment.DataAccess.Repositories.Implementations
{
    /// <summary>
    /// Fees data access, rewritten in Phase 12. Of the fourteen call sites, three executed.
    ///
    /// <list type="bullet">
    /// <item>Every write declared <c>@Result</c> (and <c>@FeeTypeId</c>, <c>@PaymentId</c>,
    /// <c>@ReceiptNumber</c>) as OUTPUT parameters. None of the procedures declares one — they all
    /// report through a result row — so SQL Server refused each call with "is not a parameter".</item>
    /// <item><c>sp_MakeFeePayment</c> was called without <c>@PaidBy</c>, which it requires.</item>
    /// <item><c>sp_RefundPayment</c> was sent <c>@Reason</c>; it takes <c>@Remarks</c> and
    /// <c>@UserId</c>.</item>
    /// <item><c>GetFeeTypeById</c> sent <c>@FeeTypeId</c> to <c>sp_GetFeeTypes</c>, the history
    /// sent dates to a procedure with none, the receipt sent <c>@ReceiptNumber</c> to one keyed on
    /// <c>@PaymentId</c>, the outstanding report sent <c>@FeeTypeId</c> to one without it, and the
    /// collection summary sent dates to one that takes <c>@FeeYear</c>.</item>
    /// </list>
    ///
    /// Only <c>GetFeeTypes</c>, <c>GetStudentFees</c> and <c>GetFeeDetails</c> ran, and the last
    /// mapped into the wrong DTO. Every signature below was checked against the procedure it calls.
    /// </summary>
    public class FeeRepository : IFeeRepository
    {
        private readonly IDbContext _dbContext;

        /// <summary>
        /// PascalCase, matching the <c>OPENJSON ... WITH</c> paths in <c>sp_AssignFeesToStudent</c>
        /// exactly (<c>'$.FeeTypeId'</c>, <c>'$.DueDate'</c>...). Pinned for the same reason as in
        /// ResultRepository: a camelCase policy would not throw, it would match nothing, and every
        /// row would be counted as skipped.
        /// </summary>
        private static readonly JsonSerializerOptions AssignmentJsonOptions = new()
        {
            PropertyNamingPolicy = null,
        };

        public FeeRepository(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private sealed class ResultRow
        {
            public string? Result { get; set; }
        }

        private sealed class CreateFeeTypeRow
        {
            public string? Result { get; set; }
            public int? FeeTypeId { get; set; }
        }

        private sealed class AssignRow
        {
            public string? Result { get; set; }
            public int FeesCreated { get; set; }
            public int FeesSkipped { get; set; }
        }

        private sealed class PaymentRow
        {
            public string? Result { get; set; }
            public int? PaymentId { get; set; }
            public string? ReceiptNumber { get; set; }
        }

        /// <summary>
        /// The JSON element <c>sp_AssignFeesToStudent</c> reads. <c>DueDate</c> is a
        /// <c>yyyy-MM-dd</c> string rather than a <c>DateTime</c>, so what reaches
        /// <c>OPENJSON ... DATE</c> is a plain calendar date with no time or offset to interpret.
        /// </summary>
        private sealed class AssignmentJsonRow
        {
            public int FeeTypeId { get; set; }
            public decimal Amount { get; set; }
            public string DueDate { get; set; } = string.Empty;
            public int? FeeMonth { get; set; }
            public int? FeeYear { get; set; }
        }

        // ------------------------------------------------------------------ Fee types

        public async Task<(ProcResult Result, int? FeeTypeId)> CreateFeeTypeAsync(int schoolId, FeeTypeCreateDTO feeType)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeTypeName", feeType.FeeTypeName, DbType.String, size: 100);
            parameters.Add("@Description", feeType.Description, DbType.String, size: 255);
            parameters.Add("@DefaultAmount", feeType.DefaultAmount, DbType.Decimal, precision: 10, scale: 2);

            var row = await connection.QueryFirstOrDefaultAsync<CreateFeeTypeRow>(
                "sp_CreateFeeType", parameters, commandType: CommandType.StoredProcedure);

            return (ProcResult.From(row?.Result), row?.FeeTypeId);
        }

        public async Task<ProcResult> UpdateFeeTypeAsync(int schoolId, int feeTypeId, FeeTypeCreateDTO feeType)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeTypeId", feeTypeId, DbType.Int32);
            parameters.Add("@FeeTypeName", feeType.FeeTypeName, DbType.String, size: 100);
            parameters.Add("@Description", feeType.Description, DbType.String, size: 255);
            parameters.Add("@DefaultAmount", feeType.DefaultAmount, DbType.Decimal, precision: 10, scale: 2);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "sp_UpdateFeeType", parameters, commandType: CommandType.StoredProcedure);

            return ProcResult.From(row?.Result);
        }

        public async Task<ProcResult> DeleteFeeTypeAsync(int schoolId, int feeTypeId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeTypeId", feeTypeId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "sp_DeleteFeeType", parameters, commandType: CommandType.StoredProcedure);

            return ProcResult.From(row?.Result);
        }

        public async Task<IEnumerable<FeeTypeDTO>> GetFeeTypesAsync(int schoolId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);

            return await connection.QueryAsync<FeeTypeDTO>(
                "sp_GetFeeTypes", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<FeeTypeDTO?> GetFeeTypeByIdAsync(int schoolId, int feeTypeId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeTypeId", feeTypeId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<FeeTypeDTO>(
                "sp_GetFeeTypeById", parameters, commandType: CommandType.StoredProcedure);
        }

        // --------------------------------------------------------------- Student fees

        /// <remarks>
        /// Argument order (student, school) kept as it was: DashboardService reaches this through
        /// <c>IFeeService.GetStudentFeesAsync</c> for the parent dashboard.
        /// </remarks>
        public async Task<IEnumerable<StudentFeeDTO>> GetStudentFeesAsync(int studentId, int schoolId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);

            return await connection.QueryAsync<StudentFeeDTO>(
                "sp_GetStudentFees", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<FeeDetailsDTO?> GetFeeDetailsAsync(int schoolId, int feeId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeId", feeId, DbType.Int32);

            return await connection.QueryFirstOrDefaultAsync<FeeDetailsDTO>(
                "sp_GetFeeDetails", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<ProcResult> UpdateFeeAsync(int schoolId, int feeId, decimal amount, DateTime dueDate, int? userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeId", feeId, DbType.Int32);
            parameters.Add("@Amount", amount, DbType.Decimal, precision: 10, scale: 2);
            parameters.Add("@DueDate", dueDate.Date, DbType.Date);
            parameters.Add("@UserId", userId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "sp_UpdateFee", parameters, commandType: CommandType.StoredProcedure);

            return ProcResult.From(row?.Result);
        }

        public async Task<ProcResult> CancelFeeAsync(int schoolId, int feeId, int? userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeId", feeId, DbType.Int32);
            parameters.Add("@UserId", userId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "sp_CancelFee", parameters, commandType: CommandType.StoredProcedure);

            return ProcResult.From(row?.Result);
        }

        public async Task<(ProcResult Result, FeeAssignResponseDTO Counts)> AssignFeesToStudentAsync(
            int schoolId, int studentId, IReadOnlyList<FeeAssignmentItemDTO> fees)
        {
            using var connection = _dbContext.CreateConnection();

            var rows = fees.Select(fee => new AssignmentJsonRow
            {
                FeeTypeId = fee.FeeTypeId,
                Amount = fee.Amount,
                DueDate = fee.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                FeeMonth = fee.FeeMonth,
                FeeYear = fee.FeeYear,
            });

            var json = JsonSerializer.Serialize(rows, AssignmentJsonOptions);

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            // NVARCHAR(MAX). The default 4000 would cut a long batch mid-token.
            parameters.Add("@FeeAssignments", json, DbType.String, size: -1);

            var row = await connection.QueryFirstOrDefaultAsync<AssignRow>(
                "sp_AssignFeesToStudent", parameters, commandType: CommandType.StoredProcedure);

            return (ProcResult.From(row?.Result), new FeeAssignResponseDTO
            {
                FeesCreated = row?.FeesCreated ?? 0,
                FeesSkipped = row?.FeesSkipped ?? 0,
            });
        }

        public async Task<(ProcResult Result, FeeAssignResponseDTO Counts)> AssignFeeToClassAsync(
            int schoolId, int classId, FeeAssignmentItemDTO fee)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@FeeTypeId", fee.FeeTypeId, DbType.Int32);
            parameters.Add("@Amount", fee.Amount, DbType.Decimal, precision: 10, scale: 2);
            parameters.Add("@DueDate", fee.DueDate.Date, DbType.Date);
            parameters.Add("@FeeMonth", fee.FeeMonth, DbType.Int32);
            parameters.Add("@FeeYear", fee.FeeYear, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<AssignRow>(
                "sp_AssignFeeToClass", parameters, commandType: CommandType.StoredProcedure);

            return (ProcResult.From(row?.Result), new FeeAssignResponseDTO
            {
                FeesCreated = row?.FeesCreated ?? 0,
                FeesSkipped = row?.FeesSkipped ?? 0,
            });
        }

        // ------------------------------------------------------------------- Payments

        /// <remarks>
        /// <c>@PaymentStatus</c> is not sent, so the procedure's default of <c>Completed</c> applies.
        /// That default is also what engages its balance cap; a caller-supplied status bypassed it.
        /// </remarks>
        public async Task<(ProcResult Result, FeePaymentResponseDTO? Payment)> MakeFeePaymentAsync(
            int schoolId, FeePaymentCreateDTO payment, int paidBy)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeId", payment.FeeId, DbType.Int32);
            parameters.Add("@AmountPaid", payment.AmountPaid, DbType.Decimal, precision: 10, scale: 2);
            parameters.Add("@PaymentMethod", payment.PaymentMethod, DbType.String, size: 50);
            parameters.Add("@TransactionId", payment.TransactionId, DbType.String, size: 100);
            parameters.Add("@Remarks", payment.Remarks, DbType.String, size: 255);
            parameters.Add("@PaidBy", paidBy, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<PaymentRow>(
                "sp_MakeFeePayment", parameters, commandType: CommandType.StoredProcedure);

            var result = ProcResult.From(row?.Result);

            if (!result.Success || row?.PaymentId is not int paymentId || string.IsNullOrEmpty(row.ReceiptNumber))
            {
                return (result.Success
                    ? new ProcResult(false, "The payment was not given a receipt number.")
                    : result, null);
            }

            return (result, new FeePaymentResponseDTO
            {
                PaymentId = paymentId,
                ReceiptNumber = row.ReceiptNumber,
            });
        }

        public async Task<ProcResult> RefundPaymentAsync(int schoolId, int paymentId, string reason, int? userId)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@PaymentId", paymentId, DbType.Int32);
            parameters.Add("@Remarks", reason, DbType.String, size: 255);
            parameters.Add("@UserId", userId, DbType.Int32);

            var row = await connection.QueryFirstOrDefaultAsync<ResultRow>(
                "sp_RefundPayment", parameters, commandType: CommandType.StoredProcedure);

            return ProcResult.From(row?.Result);
        }

        public async Task<IEnumerable<PaymentHistoryDTO>> GetPaymentHistoryAsync(
            int schoolId, int? studentId, DateTime? startDate, DateTime? endDate)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@StudentId", studentId, DbType.Int32);
            parameters.Add("@StartDate", startDate?.Date, DbType.Date);
            parameters.Add("@EndDate", endDate?.Date, DbType.Date);

            return await connection.QueryAsync<PaymentHistoryDTO>(
                "sp_GetPaymentHistory", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<ReceiptDTO?> GetReceiptAsync(int schoolId, string receiptNumber)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ReceiptNumber", receiptNumber, DbType.String, size: 40);

            return await connection.QueryFirstOrDefaultAsync<ReceiptDTO>(
                "sp_GetReceipt", parameters, commandType: CommandType.StoredProcedure);
        }

        // -------------------------------------------------------------------- Reports

        public async Task<IEnumerable<OutstandingFeeDTO>> GetOutstandingFeesAsync(
            int schoolId, int? classId, int? feeTypeId, bool overdueOnly)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@ClassId", classId, DbType.Int32);
            parameters.Add("@OverdueOnly", overdueOnly, DbType.Boolean);
            parameters.Add("@FeeTypeId", feeTypeId, DbType.Int32);

            return await connection.QueryAsync<OutstandingFeeDTO>(
                "sp_GetOutstandingFees", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<FeeCollectionPeriodDTO>> GetFeeCollectionSummaryAsync(int schoolId, int? feeYear)
        {
            using var connection = _dbContext.CreateConnection();

            var parameters = new DynamicParameters();
            parameters.Add("@SchoolId", schoolId, DbType.Int32);
            parameters.Add("@FeeYear", feeYear, DbType.Int32);

            return await connection.QueryAsync<FeeCollectionPeriodDTO>(
                "sp_GetFeeCollectionSummary", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
