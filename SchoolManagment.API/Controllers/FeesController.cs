using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Fees;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// Fee types, billing, payments, receipts and the two finance reports.
    ///
    /// Rewritten in Phase 12. Before it, three of the fourteen endpoints could execute at all (see
    /// FeeRepository), and every write compared the procedure's result against a sentence it never
    /// returns — "Fee type created successfully", "Payment processed successfully" — so the few
    /// writes that might have worked would still have been reported as failures. It is also the
    /// fourth controller found carrying no <c>[RequiresPermission]</c>; every action has one now.
    ///
    /// <b>Everything except the fee-type list is AdminOnly.</b> The seeded grid gives Student
    /// <c>Fees:View</c> and Parent <c>Fees:View</c> + <c>Fees:Create</c>, but:
    /// <list type="bullet">
    /// <item>Recording a payment asserts that the school has received money. A parent "creating" a
    /// payment would be certifying their own cash with no gateway behind it, so payments stay with
    /// the school's own staff whatever the grid says.</item>
    /// <item>The per-student reads take a <c>Students.Id</c> and nothing here consults
    /// <c>StudentParents</c>, so opening them to every holder of <c>Fees:View</c> would let any
    /// parent read any family's account by walking ids. The parent dashboard already shows each
    /// child's fees through a path that is scoped.</item>
    /// </list>
    /// The fee-type list stays AllSchoolUsers: it is a price list, not anyone's account.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FeesController : ControllerBase
    {
        /// <summary>The longest ledger window one request may ask for.</summary>
        private const int MaxLedgerDays = 366;

        private readonly IFeeService _feeService;

        public FeesController(IFeeService feeService)
        {
            _feeService = feeService;
        }

        private BadRequestObjectResult ValidationFailure()
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => new ErrorDetail("", e.ErrorMessage))
                .ToList();
            return BadRequest(ApiResponse.FailureResult("Validation failed", errors));
        }

        // ================================================================= Fee types

        /// <summary>
        /// Create a fee type.
        /// </summary>
        /// <remarks>
        /// A name that matches a previously deleted fee type revives that row with the new
        /// description and default, rather than failing on the unique name.
        /// </remarks>
        /// <response code="200">Created; <c>data.feeTypeId</c> is its id</response>
        /// <response code="400">Invalid, or a fee type with this name already exists</response>
        [HttpPost("fee-types")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<FeeTypeResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> CreateFeeType([FromBody] FeeTypeCreateDTO feeType)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var (result, feeTypeId) = await _feeService.CreateFeeTypeAsync(feeType);

            if (!result.Success || feeTypeId is not int id)
            {
                return BadRequest(ApiResponse.FailureResult(
                    result.Success ? "The fee type was not created." : result.Message));
            }

            return Ok(ApiResponse<FeeTypeResponseDTO>.SuccessResponse(
                new FeeTypeResponseDTO { FeeTypeId = id }, "Fee type created"));
        }

        /// <summary>
        /// Rename a fee type or change its description or default amount.
        /// </summary>
        /// <remarks>
        /// The default applies to fees billed from now on. Fees already billed keep their amount,
        /// because rewriting them would change what students have already been invoiced.
        /// </remarks>
        [HttpPut("fee-types/{feeTypeId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Edit)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> UpdateFeeType(int feeTypeId, [FromBody] FeeTypeCreateDTO feeType)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var result = await _feeService.UpdateFeeTypeAsync(feeTypeId, feeType);

            if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

            return Ok(ApiResponse.SuccessResult("Fee type updated"));
        }

        /// <summary>
        /// Delete (deactivate) a fee type.
        /// </summary>
        /// <remarks>
        /// Refused while any active fee references it — including fees that are fully paid. A fee
        /// type that has ever been billed and paid can therefore never be deleted; cancelling the
        /// unpaid fees is the most that can be done.
        /// </remarks>
        [HttpDelete("fee-types/{feeTypeId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Delete)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> DeleteFeeType(int feeTypeId)
        {
            var result = await _feeService.DeleteFeeTypeAsync(feeTypeId);

            if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

            return Ok(ApiResponse.SuccessResult("Fee type deleted"));
        }

        /// <summary>
        /// Every active fee type in the school, by name.
        /// </summary>
        [HttpGet("fee-types")]
        [Authorize(Policy = Constants.AuthPolicies.AllSchoolUsers)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FeeTypeDTO>>), 200)]
        public async Task<IActionResult> GetFeeTypes()
        {
            var feeTypes = await _feeService.GetFeeTypesAsync();
            return Ok(ApiResponse<IEnumerable<FeeTypeDTO>>.SuccessResponse(feeTypes, "Fee types retrieved"));
        }

        /// <summary>
        /// One active fee type.
        /// </summary>
        [HttpGet("fee-types/{feeTypeId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AllSchoolUsers)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<FeeTypeDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetFeeTypeById(int feeTypeId)
        {
            var feeType = await _feeService.GetFeeTypeByIdAsync(feeTypeId);

            if (feeType == null) return NotFound(ApiResponse.FailureResult("Fee type not found"));

            return Ok(ApiResponse<FeeTypeDTO>.SuccessResponse(feeType, "Fee type retrieved"));
        }

        // ================================================================== Billing

        /// <summary>
        /// Every active fee billed to one student, newest period first, with paid and balance.
        /// </summary>
        /// <param name="studentId">Students.Id</param>
        [HttpGet("students/{studentId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<StudentFeeDTO>>), 200)]
        public async Task<IActionResult> GetStudentFees(int studentId)
        {
            var fees = await _feeService.GetStudentFeesAsync(studentId);
            return Ok(ApiResponse<IEnumerable<StudentFeeDTO>>.SuccessResponse(fees, "Student fees retrieved"));
        }

        /// <summary>
        /// Bill one or more fees to a student.
        /// </summary>
        /// <remarks>
        /// A fee for a type and billing period the student already has is skipped, not duplicated,
        /// as are rows with an inactive fee type or an out-of-range period. A 200 with a non-zero
        /// <c>feesSkipped</c> is a partial success.
        /// </remarks>
        /// <param name="studentId">Students.Id</param>
        /// <param name="request">The fees to bill</param>
        [HttpPost("students/{studentId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<FeeAssignResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> AssignFeesToStudent(int studentId, [FromBody] StudentFeeAssignDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var (result, counts) = await _feeService.AssignFeesToStudentAsync(studentId, request.Fees);

            if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

            return Ok(ApiResponse<FeeAssignResponseDTO>.SuccessResponse(counts, AssignMessage(counts)));
        }

        /// <summary>
        /// Bill one fee to every active student in a class.
        /// </summary>
        /// <remarks>
        /// Students already billed this type for this period are skipped and counted.
        /// </remarks>
        /// <param name="classId">Classes.Id</param>
        /// <param name="request">The fee to bill</param>
        [HttpPost("classes/{classId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<FeeAssignResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> AssignFeeToClass(int classId, [FromBody] ClassFeeAssignDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var (result, counts) = await _feeService.AssignFeeToClassAsync(classId, request);

            if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

            return Ok(ApiResponse<FeeAssignResponseDTO>.SuccessResponse(counts, AssignMessage(counts)));
        }

        private static string AssignMessage(FeeAssignResponseDTO counts) =>
            counts.FeesSkipped == 0
                ? $"Billed {counts.FeesCreated} fee(s)."
                : $"Billed {counts.FeesCreated} fee(s); {counts.FeesSkipped} skipped.";

        /// <summary>
        /// One fee, with the student it is billed to and its paid and balance figures.
        /// </summary>
        [HttpGet("{feeId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<FeeDetailsDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetFeeDetails(int feeId)
        {
            var fee = await _feeService.GetFeeDetailsAsync(feeId);

            if (fee == null) return NotFound(ApiResponse.FailureResult("Fee not found"));

            return Ok(ApiResponse<FeeDetailsDTO>.SuccessResponse(fee, "Fee retrieved"));
        }

        /// <summary>
        /// Correct a fee's amount or due date.
        /// </summary>
        /// <remarks>
        /// Refused below what has already been paid against it. Audited as <c>Fee.Update</c> with
        /// the old and new values.
        /// </remarks>
        [HttpPut("{feeId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Edit)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> UpdateFee(int feeId, [FromBody] FeeUpdateDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var result = await _feeService.UpdateFeeAsync(feeId, request.Amount, request.DueDate);

            if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

            return Ok(ApiResponse.SuccessResult("Fee updated"));
        }

        /// <summary>
        /// Cancel a fee billed by mistake.
        /// </summary>
        /// <remarks>
        /// Refused while any completed payment stands against it: refund those first. The fee is
        /// deactivated, not erased, and billing the same type and period again revives it.
        /// </remarks>
        [HttpDelete("{feeId:int}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Delete)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> CancelFee(int feeId)
        {
            var result = await _feeService.CancelFeeAsync(feeId);

            if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

            return Ok(ApiResponse.SuccessResult("Fee cancelled"));
        }

        // ================================================================= Payments

        /// <summary>
        /// Record a payment received against one fee.
        /// </summary>
        /// <remarks>
        /// Always recorded as <c>Completed</c>, dated today, with the signed-in user as the cashier.
        /// Refused above the outstanding balance.
        ///
        /// <b>Not idempotent.</b> This is an insert, and nothing on the request identifies a retry,
        /// so a double-submitted form records two payments. The balance cap bounds the damage — a
        /// second full payment is refused as "already paid in full" — but a second partial payment
        /// is accepted. The client must not resubmit on a timeout without checking the history.
        /// </remarks>
        /// <response code="200">Recorded; <c>data.receiptNumber</c> is the printed receipt number</response>
        /// <response code="400">Invalid, or refused (over the balance, fee not found)</response>
        [HttpPost("payments")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<FeePaymentResponseDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> MakeFeePayment([FromBody] FeePaymentCreateDTO payment)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var (result, recorded) = await _feeService.MakeFeePaymentAsync(payment);

            if (!result.Success || recorded == null)
            {
                return BadRequest(ApiResponse.FailureResult(result.Message));
            }

            return Ok(ApiResponse<FeePaymentResponseDTO>.SuccessResponse(
                recorded, $"Payment recorded. Receipt {recorded.ReceiptNumber}."));
        }

        /// <summary>
        /// The school's payment ledger for a date range, newest first.
        /// </summary>
        /// <remarks>
        /// Both dates are required and inclusive, on the payment date, and the window may not exceed
        /// 366 days — the ledger is unpaged. Refunded payments are included; they are part of it.
        /// </remarks>
        [HttpGet("payments")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentHistoryDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetPayments(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (startDate is not DateTime from || endDate is not DateTime to)
            {
                return BadRequest(ApiResponse.FailureResult("startDate and endDate are both required"));
            }

            if (to.Date < from.Date)
            {
                return BadRequest(ApiResponse.FailureResult("endDate cannot be before startDate"));
            }

            if ((to.Date - from.Date).TotalDays >= MaxLedgerDays)
            {
                return BadRequest(ApiResponse.FailureResult($"The range cannot exceed {MaxLedgerDays} days"));
            }

            var payments = await _feeService.GetPaymentHistoryAsync(null, from, to);
            return Ok(ApiResponse<IEnumerable<PaymentHistoryDTO>>.SuccessResponse(payments, "Payments retrieved"));
        }

        /// <summary>
        /// Refund a completed payment.
        /// </summary>
        /// <remarks>
        /// The payment is marked <c>Refunded</c>, not deleted, so its receipt number stays in the
        /// ledger; it stops counting toward every balance. The reason replaces the payment's
        /// remarks. Audited as <c>Fee.Refund</c>. There is no way to reverse a refund.
        /// </remarks>
        [HttpPost("payments/{paymentId:int}/refund")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.Edit)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> RefundPayment(int paymentId, [FromBody] RefundRequestDTO request)
        {
            if (!ModelState.IsValid) return ValidationFailure();

            var result = await _feeService.RefundPaymentAsync(paymentId, request.Reason);

            if (!result.Success) return BadRequest(ApiResponse.FailureResult(result.Message));

            return Ok(ApiResponse.SuccessResult("Payment refunded"));
        }

        /// <summary>
        /// Every payment against one student's fees, newest first, including refunds.
        /// </summary>
        /// <param name="studentId">Students.Id</param>
        /// <param name="startDate">Optional, inclusive, on the payment date</param>
        /// <param name="endDate">Optional, inclusive</param>
        [HttpGet("students/{studentId:int}/payment-history")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PaymentHistoryDTO>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> GetPaymentHistory(
            int studentId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            if (startDate.HasValue && endDate.HasValue && endDate.Value.Date < startDate.Value.Date)
            {
                return BadRequest(ApiResponse.FailureResult("endDate cannot be before startDate"));
            }

            var history = await _feeService.GetPaymentHistoryAsync(studentId, startDate, endDate);
            return Ok(ApiResponse<IEnumerable<PaymentHistoryDTO>>.SuccessResponse(history, "Payment history retrieved"));
        }

        /// <summary>
        /// Everything needed to print one receipt, by its receipt number.
        /// </summary>
        [HttpGet("receipts/{receiptNumber}")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<ReceiptDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetReceipt(string receiptNumber)
        {
            if (string.IsNullOrWhiteSpace(receiptNumber) || receiptNumber.Length > 40)
            {
                return NotFound(ApiResponse.FailureResult("Receipt not found"));
            }

            var receipt = await _feeService.GetReceiptAsync(receiptNumber);

            if (receipt == null) return NotFound(ApiResponse.FailureResult("Receipt not found"));

            return Ok(ApiResponse<ReceiptDTO>.SuccessResponse(receipt, "Receipt retrieved"));
        }

        // ================================================================== Reports

        /// <summary>
        /// Every unpaid fee, soonest due first, optionally for one class or fee type.
        /// </summary>
        /// <remarks>
        /// Includes students who have left the school, flagged by <c>studentIsActive</c>: they still
        /// owe the money, and the dashboard's outstanding total counts them.
        /// </remarks>
        [HttpGet("outstanding")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<OutstandingFeeDTO>>), 200)]
        public async Task<IActionResult> GetOutstandingFees(
            [FromQuery] int? classId = null,
            [FromQuery] int? feeTypeId = null,
            [FromQuery] bool overdueOnly = false)
        {
            var fees = await _feeService.GetOutstandingFeesAsync(classId, feeTypeId, overdueOnly);
            return Ok(ApiResponse<IEnumerable<OutstandingFeeDTO>>.SuccessResponse(fees, "Outstanding fees retrieved"));
        }

        /// <summary>
        /// Billed, collected and outstanding per billing period, newest first.
        /// </summary>
        /// <remarks>
        /// Grouped by the fee's billing month, not the payment date — a March fee paid in May is
        /// collected in March here. Omit <c>feeYear</c> for every year.
        /// </remarks>
        [HttpGet("collection-summary")]
        [Authorize(Policy = Constants.AuthPolicies.AdminOnly)]
        [RequiresPermission(Constants.Modules.Fees, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FeeCollectionPeriodDTO>>), 200)]
        public async Task<IActionResult> GetFeeCollectionSummary([FromQuery] int? feeYear = null)
        {
            var summary = await _feeService.GetFeeCollectionSummaryAsync(feeYear);
            return Ok(ApiResponse<IEnumerable<FeeCollectionPeriodDTO>>.SuccessResponse(summary, "Collection summary retrieved"));
        }
    }
}
