using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Fees;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IFeeService
    {
        // Fee Types
        Task<(ProcResult Result, int? FeeTypeId)> CreateFeeTypeAsync(FeeTypeCreateDTO feeType);
        Task<ProcResult> UpdateFeeTypeAsync(int feeTypeId, FeeTypeCreateDTO feeType);
        Task<ProcResult> DeleteFeeTypeAsync(int feeTypeId);
        Task<IEnumerable<FeeTypeDTO>> GetFeeTypesAsync();
        Task<FeeTypeDTO?> GetFeeTypeByIdAsync(int feeTypeId);

        // Student Fees
        /// <summary>Also the parent dashboard's source for a child's fees. Keep the signature.</summary>
        Task<IEnumerable<StudentFeeDTO>> GetStudentFeesAsync(int studentId);
        Task<FeeDetailsDTO?> GetFeeDetailsAsync(int feeId);
        Task<ProcResult> UpdateFeeAsync(int feeId, decimal amount, DateTime dueDate);
        Task<ProcResult> CancelFeeAsync(int feeId);
        Task<(ProcResult Result, FeeAssignResponseDTO Counts)> AssignFeesToStudentAsync(
            int studentId, IReadOnlyList<FeeAssignmentItemDTO> fees);
        Task<(ProcResult Result, FeeAssignResponseDTO Counts)> AssignFeeToClassAsync(
            int classId, FeeAssignmentItemDTO fee);

        // Payments
        Task<(ProcResult Result, FeePaymentResponseDTO? Payment)> MakeFeePaymentAsync(FeePaymentCreateDTO payment);
        Task<ProcResult> RefundPaymentAsync(int paymentId, string reason);
        Task<IEnumerable<PaymentHistoryDTO>> GetPaymentHistoryAsync(
            int? studentId, DateTime? startDate = null, DateTime? endDate = null);
        Task<ReceiptDTO?> GetReceiptAsync(string receiptNumber);

        // Reports
        Task<IEnumerable<OutstandingFeeDTO>> GetOutstandingFeesAsync(
            int? classId = null, int? feeTypeId = null, bool overdueOnly = false);
        Task<IEnumerable<FeeCollectionPeriodDTO>> GetFeeCollectionSummaryAsync(int? feeYear = null);
    }
}
