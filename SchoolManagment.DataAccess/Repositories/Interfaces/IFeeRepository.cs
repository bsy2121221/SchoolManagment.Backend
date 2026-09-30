using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Fees;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface IFeeRepository
    {
        // Fee Types
        Task<(ProcResult Result, int? FeeTypeId)> CreateFeeTypeAsync(int schoolId, FeeTypeCreateDTO feeType);
        Task<ProcResult> UpdateFeeTypeAsync(int schoolId, int feeTypeId, FeeTypeCreateDTO feeType);
        Task<ProcResult> DeleteFeeTypeAsync(int schoolId, int feeTypeId);
        Task<IEnumerable<FeeTypeDTO>> GetFeeTypesAsync(int schoolId);
        Task<FeeTypeDTO?> GetFeeTypeByIdAsync(int schoolId, int feeTypeId);

        // Student Fees
        Task<IEnumerable<StudentFeeDTO>> GetStudentFeesAsync(int studentId, int schoolId);
        Task<FeeDetailsDTO?> GetFeeDetailsAsync(int schoolId, int feeId);
        Task<ProcResult> UpdateFeeAsync(int schoolId, int feeId, decimal amount, DateTime dueDate, int? userId);
        Task<ProcResult> CancelFeeAsync(int schoolId, int feeId, int? userId);
        Task<(ProcResult Result, FeeAssignResponseDTO Counts)> AssignFeesToStudentAsync(
            int schoolId, int studentId, IReadOnlyList<FeeAssignmentItemDTO> fees);
        Task<(ProcResult Result, FeeAssignResponseDTO Counts)> AssignFeeToClassAsync(
            int schoolId, int classId, FeeAssignmentItemDTO fee);

        // Payments
        Task<(ProcResult Result, FeePaymentResponseDTO? Payment)> MakeFeePaymentAsync(
            int schoolId, FeePaymentCreateDTO payment, int paidBy);
        Task<ProcResult> RefundPaymentAsync(int schoolId, int paymentId, string reason, int? userId);
        Task<IEnumerable<PaymentHistoryDTO>> GetPaymentHistoryAsync(
            int schoolId, int? studentId, DateTime? startDate, DateTime? endDate);
        Task<ReceiptDTO?> GetReceiptAsync(int schoolId, string receiptNumber);

        // Reports
        Task<IEnumerable<OutstandingFeeDTO>> GetOutstandingFeesAsync(
            int schoolId, int? classId, int? feeTypeId, bool overdueOnly);
        Task<IEnumerable<FeeCollectionPeriodDTO>> GetFeeCollectionSummaryAsync(int schoolId, int? feeYear);
    }
}
