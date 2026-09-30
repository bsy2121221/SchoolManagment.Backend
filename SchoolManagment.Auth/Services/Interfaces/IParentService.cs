using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Parents;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IParentService
    {
        Task<(ProcResult Result, ParentRegistrationResponseDTO? Parent)> RegisterParentAsync(
            ParentRegistrationDTO parent
        );

        /// <summary>
        /// Every parent in the caller's school. <paramref name="includeInactive"/> adds the
        /// soft-deleted ones, which are otherwise unreachable through the API.
        /// </summary>
        Task<List<ParentDTO>> GetAllParentsAsync(bool includeInactive = false);

        Task<ParentDTO?> GetParentByIdAsync(int parentId);

        /// <summary>Keyed on dbo.Users.Id; carries the children list.</summary>
        Task<ParentProfileDTO?> GetParentProfileAsync(int userId);

        Task<ProcResult> UpdateParentAsync(int parentId, ParentUpdateDTO parent);

        Task<ProcResult> DeleteParentAsync(int parentId);

        Task<ProcResult> LinkStudentParentAsync(LinkStudentParentDTO link);

        Task<ProcResult> UnlinkStudentParentAsync(UnlinkStudentParentDTO unlink);

        Task<List<ParentDTO>> GetParentsByStudentAsync(int studentId);

        Task<List<ParentChildDTO>> GetChildrenByParentAsync(int parentId);
    }
}
