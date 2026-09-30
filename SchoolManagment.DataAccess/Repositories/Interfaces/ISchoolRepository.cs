using SchoolManagment.Models.DTOs.Schools;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    /// <summary>
    /// The tenant table itself. Unlike every other repository here, these methods
    /// take the school id as an argument instead of receiving it from the caller's
    /// token -- they have to, because a platform administrator addresses schools
    /// they are not a member of. Nothing in this interface enforces who may reach
    /// which school; SchoolService does, and is the only place that should.
    /// </summary>
    public interface ISchoolRepository
    {
        Task<(List<SchoolListItemDTO> Schools, int TotalCount)> GetSchoolsAsync(
            string? searchTerm = null, bool? isActive = null, int page = 1, int pageSize = 50);

        Task<SchoolDTO?> GetSchoolByIdAsync(int schoolId);

        Task<SchoolDTO?> GetSchoolByCodeAsync(string schoolCode);

        Task<SchoolBrandingDTO?> GetSchoolBySubdomainAsync(string subdomain);

        /// <summary>
        /// Public branding by id or by code. Supply exactly one; the procedure
        /// prefers the id and returns nothing when both are omitted.
        /// </summary>
        Task<SchoolBrandingDTO?> GetSchoolBrandingAsync(int? schoolId, string? schoolCode);

        /// <summary>
        /// <paramref name="adminPasswordHash"/> null hands the procedure its own
        /// shared temporary password and sets RequirePasswordChange -- which is why
        /// the service must pass null rather than hashing a default itself.
        /// </summary>
        Task<(bool Success, string Message, SchoolCreateResultDTO? Result)> CreateSchoolAsync(
            SchoolCreateDTO school, string? adminPasswordHash, int? createdByUserId);

        Task<(bool Success, string Message)> UpdateSchoolAsync(
            int schoolId, SchoolUpdateDTO school, int? updatedByUserId);

        /// <summary>
        /// Suspends or restores a tenant. Suspending also revokes its users'
        /// refresh tokens, so nobody stays signed in on an old token.
        /// </summary>
        Task<(bool Success, string Message, bool? IsActive)> ToggleSchoolStatusAsync(
            int schoolId, bool isActive, int? updatedByUserId);

        Task<(bool Success, string Message, SchoolAdminCreateResultDTO? Result)> CreateSchoolAdminAsync(
            int schoolId, SchoolAdminCreateDTO admin, string passwordHash, int? createdByUserId);

        Task<PlatformStatsDTO?> GetPlatformStatsAsync();

        /// <summary>Defaults to the last 30 days when either bound is omitted.</summary>
        Task<List<SchoolUsageReportDTO>> GetSchoolUsageReportAsync(DateTime? fromDate, DateTime? toDate);
    }
}
