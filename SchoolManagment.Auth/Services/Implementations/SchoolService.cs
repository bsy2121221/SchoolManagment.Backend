using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Schools;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class SchoolService : ISchoolService
    {
        /// <summary>
        /// The same temporary password the other registration services use, and the
        /// same one sp_CreateSchool falls back to. Only ever stored alongside
        /// RequirePasswordChange.
        /// </summary>
        private const string TemporaryPassword = "Temp@123";

        private readonly ISchoolRepository _schoolRepository;
        private readonly IAuthRepository _authRepository;
        private readonly ITenantContext _tenantContext;

        public SchoolService(
            ISchoolRepository schoolRepository,
            IAuthRepository authRepository,
            ITenantContext tenantContext)
        {
            _schoolRepository = schoolRepository;
            _authRepository = authRepository;
            _tenantContext = tenantContext;
        }

        private int? ActorUserId => _tenantContext.UserId;

        public bool CanAccessSchool(int schoolId) =>
            _tenantContext.IsSuperAdmin || _tenantContext.SchoolId == schoolId;

        public async Task<PaginatedResponse<SchoolListItemDTO>> GetSchoolsAsync(
            string? searchTerm = null, bool? isActive = null, int page = 1, int pageSize = 20)
        {
            if (!_tenantContext.IsSuperAdmin)
            {
                return new PaginatedResponse<SchoolListItemDTO>(
                    new List<SchoolListItemDTO>(), 0, page, pageSize);
            }

            var (schools, totalCount) = await _schoolRepository.GetSchoolsAsync(
                searchTerm, isActive, page, pageSize);

            return new PaginatedResponse<SchoolListItemDTO>(schools, totalCount, page, pageSize);
        }

        public async Task<SchoolDTO?> GetSchoolByIdAsync(int schoolId)
        {
            if (!CanAccessSchool(schoolId))
                return null;

            return await _schoolRepository.GetSchoolByIdAsync(schoolId);
        }

        public async Task<SchoolDTO?> GetCurrentSchoolAsync()
        {
            if (!_tenantContext.SchoolId.HasValue)
                return null;

            return await _schoolRepository.GetSchoolByIdAsync(_tenantContext.SchoolId.Value);
        }

        public async Task<SchoolDTO?> GetSchoolByCodeAsync(string schoolCode)
        {
            if (!_tenantContext.IsSuperAdmin || string.IsNullOrWhiteSpace(schoolCode))
                return null;

            return await _schoolRepository.GetSchoolByCodeAsync(schoolCode);
        }

        public async Task<SchoolBrandingDTO?> GetBrandingAsync(int? schoolId, string? schoolCode)
        {
            // The procedure returns nothing when both are omitted, which would read
            // as "no such school" rather than "you asked nothing".
            if (!schoolId.HasValue && string.IsNullOrWhiteSpace(schoolCode))
                return null;

            return await _schoolRepository.GetSchoolBrandingAsync(schoolId, schoolCode);
        }

        public async Task<SchoolBrandingDTO?> GetBySubdomainAsync(string subdomain)
        {
            if (string.IsNullOrWhiteSpace(subdomain))
                return null;

            return await _schoolRepository.GetSchoolBySubdomainAsync(subdomain);
        }

        public async Task<(bool Success, string Message, SchoolCreateResultDTO? Result)> CreateSchoolAsync(
            SchoolCreateDTO school)
        {
            if (!_tenantContext.IsSuperAdmin)
                return (false, "Only a platform administrator may create a school.", null);

            // Checked against the sanitised form, because that is what gets stored
            // and what CK_Schools_SchoolCode measures. "DPS Noida" is 9 characters
            // and a valid code; "!!" is two and is not.
            //
            // The upper bound cannot trip while SchoolCreateDTO caps the raw value
            // at 12 -- sanitising only shortens. It stays because it is the bound
            // the database actually enforces, and it should not depend on an
            // annotation on a DTO to hold.
            var sanitisedCode = SanitiseCode(school.SchoolCode);
            if (sanitisedCode.Length < 3 || sanitisedCode.Length > 12)
            {
                return (false,
                    "School code must contain between 3 and 12 letters or digits " +
                    "once spaces and punctuation are removed.", null);
            }

            // Null hash, not a hash of the temporary password: sp_CreateSchool sets
            // RequirePasswordChange only when it has to supply the fallback itself,
            // so hashing "Temp@123" here would create an admin holding a shared
            // password with no obligation to change it.
            string? adminPasswordHash = null;

            if (!string.IsNullOrWhiteSpace(school.AdminPassword))
            {
                if (!PasswordHelper.IsValidPasswordFormat(school.AdminPassword))
                    return (false, Constants.ValidationMessages.InvalidPassword, null);

                adminPasswordHash = PasswordHelper.HashPassword(school.AdminPassword);
            }

            return await _schoolRepository.CreateSchoolAsync(school, adminPasswordHash, ActorUserId);
        }

        public async Task<(bool Success, string Message)> UpdateSchoolAsync(
            int schoolId, SchoolUpdateDTO school)
        {
            if (!CanAccessSchool(schoolId))
                return (false, "School not found.");

            return await _schoolRepository.UpdateSchoolAsync(schoolId, school, ActorUserId);
        }

        public async Task<(bool Success, string Message, bool? IsActive)> ToggleSchoolStatusAsync(
            int schoolId, bool isActive)
        {
            // Not CanAccessSchool: a school admin must not be able to suspend their
            // own tenant, which would lock every one of their users out at once.
            if (!_tenantContext.IsSuperAdmin)
                return (false, "Only a platform administrator may suspend or restore a school.", null);

            return await _schoolRepository.ToggleSchoolStatusAsync(schoolId, isActive, ActorUserId);
        }

        public async Task<(bool Success, string Message, SchoolAdminCreateResultDTO? Result)> CreateSchoolAdminAsync(
            int schoolId, SchoolAdminCreateDTO admin)
        {
            if (!CanAccessSchool(schoolId))
                return (false, "School not found.", null);

            var usingTemporaryPassword = string.IsNullOrWhiteSpace(admin.Password);

            if (!usingTemporaryPassword && !PasswordHelper.IsValidPasswordFormat(admin.Password!))
                return (false, Constants.ValidationMessages.InvalidPassword, null);

            var password = usingTemporaryPassword ? TemporaryPassword : admin.Password!;
            var passwordHash = PasswordHelper.HashPassword(password);

            var created = await _schoolRepository.CreateSchoolAdminAsync(
                schoolId, admin, passwordHash, ActorUserId);

            if (!created.Success || created.Result == null || !usingTemporaryPassword)
                return created;

            // sp_CreateSchoolAdmin does not pass @RequirePasswordChange to
            // sp_CreateUserAccount, so it defaults to 0 -- unlike sp_CreateSchool,
            // which sets it for the first admin. Left alone, an admin created with
            // the shared temporary password would never be made to change it. The
            // flag is set here rather than in SQL so the procedure keeps working
            // for anyone already calling it.
            var forced = await _authRepository.ResetPasswordAsync(
                created.Result.UserId,
                passwordHash,
                requirePasswordChange: true,
                schoolId: null,
                performedByUserId: ActorUserId);

            if (!forced.Success)
            {
                return (false,
                    $"Administrator '{created.Result.Username}' was created (id {created.Result.UserId}), " +
                    $"but could not be required to change the temporary password: {forced.Message}",
                    created.Result);
            }

            created.Result.RequiresPasswordChange = true;
            return created;
        }

        /// <summary>
        /// The C# twin of fn_SanitizeCode. Used only to validate, never to build the
        /// value sent to the database: the procedure sanitises for itself and
        /// reports the code it actually stored.
        ///
        /// ASCII only, deliberately -- char.IsLetterOrDigit would keep accented and
        /// non-Latin letters that the SQL function strips, so a name in another
        /// script would pass here and then be refused by the procedure.
        /// </summary>
        private static string SanitiseCode(string? text)
        {
            var upper = (text ?? string.Empty).ToUpperInvariant();
            return new string(upper.Where(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9').ToArray());
        }

        public async Task<PlatformStatsDTO?> GetPlatformStatsAsync()
        {
            if (!_tenantContext.IsSuperAdmin)
                return null;

            return await _schoolRepository.GetPlatformStatsAsync();
        }

        public async Task<List<SchoolUsageReportDTO>> GetSchoolUsageReportAsync(
            DateTime? fromDate, DateTime? toDate)
        {
            if (!_tenantContext.IsSuperAdmin)
                return new List<SchoolUsageReportDTO>();

            return await _schoolRepository.GetSchoolUsageReportAsync(fromDate, toDate);
        }
    }
}
