using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Schools;

namespace SchoolManagment.Auth.Services.Interfaces
{
    /// <summary>
    /// Tenant administration. This is the one service in the solution whose
    /// methods take a school id from the caller rather than from the token, so it
    /// is also the one place where tenant isolation has to be asserted rather than
    /// inherited -- every method below either confines itself to
    /// <see cref="CanAccessSchool"/> or requires a platform administrator.
    /// </summary>
    public interface ISchoolService
    {
        /// <summary>
        /// True when the caller may address <paramref name="schoolId"/>: a platform
        /// administrator may address any school, anybody else only their own.
        /// </summary>
        bool CanAccessSchool(int schoolId);

        /// <summary>The tenant list. Platform administrators only.</summary>
        Task<PaginatedResponse<SchoolListItemDTO>> GetSchoolsAsync(
            string? searchTerm = null, bool? isActive = null, int page = 1, int pageSize = 20);

        /// <summary>
        /// One school. Returns null both when the school does not exist and when
        /// the caller may not see it, so a school admin probing ids cannot tell
        /// which other tenants exist.
        /// </summary>
        Task<SchoolDTO?> GetSchoolByIdAsync(int schoolId);

        /// <summary>
        /// The caller's own school, from the token. Null for a platform
        /// administrator who is not currently acting inside one.
        /// </summary>
        Task<SchoolDTO?> GetCurrentSchoolAsync();

        /// <summary>By school code. Platform administrators only.</summary>
        Task<SchoolDTO?> GetSchoolByCodeAsync(string schoolCode);

        /// <summary>Public branding. Safe for anonymous callers.</summary>
        Task<SchoolBrandingDTO?> GetBrandingAsync(int? schoolId, string? schoolCode);

        /// <summary>Public branding by subdomain. Safe for anonymous callers.</summary>
        Task<SchoolBrandingDTO?> GetBySubdomainAsync(string subdomain);

        /// <summary>
        /// Onboards a tenant together with its first admin. Platform
        /// administrators only.
        /// </summary>
        Task<(bool Success, string Message, SchoolCreateResultDTO? Result)> CreateSchoolAsync(
            SchoolCreateDTO school);

        /// <summary>Partial update, confined to a school the caller may address.</summary>
        Task<(bool Success, string Message)> UpdateSchoolAsync(int schoolId, SchoolUpdateDTO school);

        /// <summary>Suspend or restore a tenant. Platform administrators only.</summary>
        Task<(bool Success, string Message, bool? IsActive)> ToggleSchoolStatusAsync(
            int schoolId, bool isActive);

        /// <summary>Adds a further admin to a school the caller may address.</summary>
        Task<(bool Success, string Message, SchoolAdminCreateResultDTO? Result)> CreateSchoolAdminAsync(
            int schoolId, SchoolAdminCreateDTO admin);

        /// <summary>Totals across all tenants. Platform administrators only.</summary>
        Task<PlatformStatsDTO?> GetPlatformStatsAsync();

        /// <summary>Per-tenant activity. Platform administrators only.</summary>
        Task<List<SchoolUsageReportDTO>> GetSchoolUsageReportAsync(DateTime? fromDate, DateTime? toDate);
    }
}
