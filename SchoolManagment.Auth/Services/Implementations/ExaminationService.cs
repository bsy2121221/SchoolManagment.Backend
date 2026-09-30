using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Examinations;

namespace SchoolManagment.Auth.Services.Implementations
{
    /// <summary>
    /// Examinations.
    ///
    /// The tenant gate is the same one every service in this project uses: no
    /// <c>SchoolId</c> on the token means no examinations to see and none to write, and the
    /// reads answer with an empty list rather than throwing because a SuperAdmin who has not
    /// switched into a school is in exactly that position.
    ///
    /// <c>IHttpContextAccessor</c> is gone. It was here to dig the <c>UserId</c> claim out of
    /// the request by hand, duplicating what <see cref="ITenantContext"/> already does from the
    /// same token -- the arrangement Phases 6 through 9 removed from four other services, and
    /// the only one that kept a second reading of the claims in play.
    /// </summary>
    public class ExaminationService : IExaminationService
    {
        private readonly IExaminationRepository _examinationRepository;
        private readonly ITenantContext _tenantContext;

        public ExaminationService(
            IExaminationRepository examinationRepository,
            ITenantContext tenantContext)
        {
            _examinationRepository = examinationRepository;
            _tenantContext = tenantContext;
        }

        public async Task<(ProcResult Result, int ExaminationId, bool WasCreated)> CreateOrUpdateExaminationAsync(
            ExaminationCreateDTO examination)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (new ProcResult(false, "No school context on this token."), 0, false);
            }

            return await _examinationRepository.CreateOrUpdateExaminationAsync(
                _tenantContext.SchoolId.Value,
                examination,
                _tenantContext.UserId
            );
        }

        public async Task<List<ExaminationDTO>> GetExaminationsAsync(int? classId = null)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ExaminationDTO>();
            }

            return await _examinationRepository.GetExaminationsAsync(_tenantContext.SchoolId.Value, classId);
        }

        public async Task<ExaminationDTO?> GetExaminationByIdAsync(
            int examinationId,
            bool includeInactive = false)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _examinationRepository.GetExaminationByIdAsync(
                _tenantContext.SchoolId.Value,
                examinationId,
                includeInactive
            );
        }

        public async Task<ProcResult> DeleteExaminationAsync(int examinationId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new ProcResult(false, "No school context on this token.");
            }

            return await _examinationRepository.DeleteExaminationAsync(
                _tenantContext.SchoolId.Value,
                examinationId
            );
        }

        public async Task<List<ExaminationResultsDTO>> GetExaminationResultsAsync(int examinationId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ExaminationResultsDTO>();
            }

            return await _examinationRepository.GetExaminationResultsAsync(
                _tenantContext.SchoolId.Value,
                examinationId
            );
        }
    }
}
