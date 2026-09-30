using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Fees;

namespace SchoolManagment.Auth.Services.Implementations
{
    /// <summary>
    /// Fees, tenant-scoped through <see cref="ITenantContext"/>.
    ///
    /// The school comes from the token and so does the actor. <c>FeePayments.PaidBy</c> is the
    /// signed-in cashier, and the fee edit, cancel and refund audit rows name the same user, so none
    /// of them can be attributed to someone else by a request body.
    /// </summary>
    public class FeeService : IFeeService
    {
        private readonly IFeeRepository _feeRepository;
        private readonly ITenantContext _tenantContext;

        public FeeService(IFeeRepository feeRepository, ITenantContext tenantContext)
        {
            _feeRepository = feeRepository;
            _tenantContext = tenantContext;
        }

        private int SchoolId => _tenantContext.SchoolId!.Value;

        public Task<(ProcResult Result, int? FeeTypeId)> CreateFeeTypeAsync(FeeTypeCreateDTO feeType)
        {
            feeType.FeeTypeName = feeType.FeeTypeName.Trim();
            return _feeRepository.CreateFeeTypeAsync(SchoolId, feeType);
        }

        public Task<ProcResult> UpdateFeeTypeAsync(int feeTypeId, FeeTypeCreateDTO feeType)
        {
            // sp_UpdateFeeType does not trim, unlike sp_CreateFeeType, so "Tuition " would pass its
            // duplicate check against "Tuition" and then collide with UQ_FeeTypes_School_Name.
            feeType.FeeTypeName = feeType.FeeTypeName.Trim();
            return _feeRepository.UpdateFeeTypeAsync(SchoolId, feeTypeId, feeType);
        }

        public Task<ProcResult> DeleteFeeTypeAsync(int feeTypeId) =>
            _feeRepository.DeleteFeeTypeAsync(SchoolId, feeTypeId);

        public Task<IEnumerable<FeeTypeDTO>> GetFeeTypesAsync() =>
            _feeRepository.GetFeeTypesAsync(SchoolId);

        public Task<FeeTypeDTO?> GetFeeTypeByIdAsync(int feeTypeId) =>
            _feeRepository.GetFeeTypeByIdAsync(SchoolId, feeTypeId);

        public Task<IEnumerable<StudentFeeDTO>> GetStudentFeesAsync(int studentId) =>
            _feeRepository.GetStudentFeesAsync(studentId, SchoolId);

        public Task<FeeDetailsDTO?> GetFeeDetailsAsync(int feeId) =>
            _feeRepository.GetFeeDetailsAsync(SchoolId, feeId);

        public Task<ProcResult> UpdateFeeAsync(int feeId, decimal amount, DateTime dueDate) =>
            _feeRepository.UpdateFeeAsync(SchoolId, feeId, amount, dueDate, _tenantContext.UserId);

        public Task<ProcResult> CancelFeeAsync(int feeId) =>
            _feeRepository.CancelFeeAsync(SchoolId, feeId, _tenantContext.UserId);

        public Task<(ProcResult Result, FeeAssignResponseDTO Counts)> AssignFeesToStudentAsync(
            int studentId, IReadOnlyList<FeeAssignmentItemDTO> fees) =>
            _feeRepository.AssignFeesToStudentAsync(SchoolId, studentId, fees);

        public Task<(ProcResult Result, FeeAssignResponseDTO Counts)> AssignFeeToClassAsync(
            int classId, FeeAssignmentItemDTO fee) =>
            _feeRepository.AssignFeeToClassAsync(SchoolId, classId, fee);

        public async Task<(ProcResult Result, FeePaymentResponseDTO? Payment)> MakeFeePaymentAsync(FeePaymentCreateDTO payment)
        {
            // PaidBy is NOT NULL and names the cashier on the receipt. There is always a user on a
            // request that reached here, but a missing claim must refuse rather than record an
            // unattributed payment.
            if (_tenantContext.UserId is not int paidBy)
            {
                return (new ProcResult(false, "The signed-in user could not be identified."), null);
            }

            payment.TransactionId = string.IsNullOrWhiteSpace(payment.TransactionId) ? null : payment.TransactionId.Trim();
            payment.Remarks = string.IsNullOrWhiteSpace(payment.Remarks) ? null : payment.Remarks.Trim();

            return await _feeRepository.MakeFeePaymentAsync(SchoolId, payment, paidBy);
        }

        public Task<ProcResult> RefundPaymentAsync(int paymentId, string reason) =>
            _feeRepository.RefundPaymentAsync(SchoolId, paymentId, reason.Trim(), _tenantContext.UserId);

        public Task<IEnumerable<PaymentHistoryDTO>> GetPaymentHistoryAsync(
            int? studentId, DateTime? startDate = null, DateTime? endDate = null) =>
            _feeRepository.GetPaymentHistoryAsync(SchoolId, studentId, startDate, endDate);

        public Task<ReceiptDTO?> GetReceiptAsync(string receiptNumber) =>
            _feeRepository.GetReceiptAsync(SchoolId, receiptNumber.Trim());

        public Task<IEnumerable<OutstandingFeeDTO>> GetOutstandingFeesAsync(
            int? classId = null, int? feeTypeId = null, bool overdueOnly = false) =>
            _feeRepository.GetOutstandingFeesAsync(SchoolId, classId, feeTypeId, overdueOnly);

        public Task<IEnumerable<FeeCollectionPeriodDTO>> GetFeeCollectionSummaryAsync(int? feeYear = null) =>
            _feeRepository.GetFeeCollectionSummaryAsync(SchoolId, feeYear);
    }
}
