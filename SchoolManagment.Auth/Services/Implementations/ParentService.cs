using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Parents;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class ParentService : IParentService
    {
        /// <summary>
        /// What every write returns when the caller's token carries no school, as in
        /// TeacherService. A SuperAdmin who has not switched into a school is the usual
        /// case, and "Failed to update parent" told them nothing about how to fix it.
        /// </summary>
        private static readonly ProcResult NoSchoolInScope = new(
            false,
            "No school is in scope for this request. Switch into a school before managing its parents."
        );

        /// <summary>The default handed to a new parent, changed on first sign-in.</summary>
        private const string DefaultPassword = "Temp@123";

        private readonly IParentRepository _parentRepository;
        private readonly ITenantContext _tenantContext;

        public ParentService(IParentRepository parentRepository, ITenantContext tenantContext)
        {
            _parentRepository = parentRepository;
            _tenantContext = tenantContext;
        }

        /// <summary>
        /// Who the procedures record in the audit trail, read from the tenant context as the
        /// other services do.
        /// </summary>
        private int? ActorUserId => _tenantContext.UserId;

        public async Task<(ProcResult Result, ParentRegistrationResponseDTO? Parent)> RegisterParentAsync(
            ParentRegistrationDTO parent)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return (NoSchoolInScope, null);
            }

            // An admin may set the first password; most do not, and the shared Temp@123
            // applies. Either way the hashing happens here, never in SQL, and the procedure
            // sets RequirePasswordChange so the parent replaces it on first sign-in.
            var passwordHash = string.IsNullOrWhiteSpace(parent.Password)
                ? PasswordHelper.HashPassword(DefaultPassword)
                : PasswordHelper.HashPassword(parent.Password);

            return await _parentRepository.RegisterParentAsync(
                _tenantContext.SchoolId.Value,
                parent,
                passwordHash,
                ActorUserId
            );
        }

        public async Task<List<ParentDTO>> GetAllParentsAsync(bool includeInactive = false)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ParentDTO>();
            }

            return await _parentRepository.GetAllParentsAsync(
                _tenantContext.SchoolId.Value,
                includeInactive
            );
        }

        public async Task<ParentDTO?> GetParentByIdAsync(int parentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _parentRepository.GetParentByIdAsync(_tenantContext.SchoolId.Value, parentId);
        }

        public async Task<ParentProfileDTO?> GetParentProfileAsync(int userId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return null;
            }

            return await _parentRepository.GetParentProfileAsync(_tenantContext.SchoolId.Value, userId);
        }

        public async Task<ProcResult> UpdateParentAsync(int parentId, ParentUpdateDTO parent)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _parentRepository.UpdateParentAsync(
                _tenantContext.SchoolId.Value,
                parentId,
                parent,
                ActorUserId
            );
        }

        public async Task<ProcResult> DeleteParentAsync(int parentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _parentRepository.DeleteParentAsync(
                _tenantContext.SchoolId.Value,
                parentId,
                ActorUserId
            );
        }

        public async Task<ProcResult> LinkStudentParentAsync(LinkStudentParentDTO link)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _parentRepository.LinkStudentParentAsync(
                _tenantContext.SchoolId.Value,
                link.StudentId,
                link.ParentId,
                link.Relationship,
                ActorUserId
            );
        }

        public async Task<ProcResult> UnlinkStudentParentAsync(UnlinkStudentParentDTO unlink)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return NoSchoolInScope;
            }

            return await _parentRepository.UnlinkStudentParentAsync(
                _tenantContext.SchoolId.Value,
                unlink.StudentId,
                unlink.ParentId,
                ActorUserId
            );
        }

        public async Task<List<ParentDTO>> GetParentsByStudentAsync(int studentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ParentDTO>();
            }

            return await _parentRepository.GetParentsByStudentAsync(_tenantContext.SchoolId.Value, studentId);
        }

        public async Task<List<ParentChildDTO>> GetChildrenByParentAsync(int parentId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ParentChildDTO>();
            }

            return await _parentRepository.GetChildrenByParentAsync(_tenantContext.SchoolId.Value, parentId);
        }
    }
}
