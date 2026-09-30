using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Parents;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    /// <summary>
    /// Parents, over the procedures in 17_Procs_Parents.sql.
    ///
    /// Eight of the nine methods below used to carry their own inline SQL, written against
    /// the pre-split schema: names, the phone number and the address moved to
    /// <c>dbo.Persons</c> and <c>dbo.Addresses</c> some time ago, and the role became
    /// <c>Users.RoleId</c>, so statements naming <c>Users.FirstName</c> or
    /// <c>Users.Role</c> could not execute at all. Reads now go through
    /// <c>dbo.vw_Users</c>, which re-flattens the three tables, and writes go through
    /// procedures that own their own transaction.
    /// </summary>
    public interface IParentRepository
    {
        /// <summary>
        /// Creates the Persons, Users, Addresses and Parents rows in one transaction, with
        /// a username drawn from the school's 'Parent' sequence.
        /// </summary>
        Task<(ProcResult Result, ParentRegistrationResponseDTO? Parent)> RegisterParentAsync(
            int schoolId,
            ParentRegistrationDTO parent,
            string passwordHash,
            int? performedByUserId = null
        );

        /// <summary>
        /// Every parent in the school, each with their children summarised.
        /// Unpaginated and unsearchable -- the procedure takes neither -- so callers filter
        /// client-side. <paramref name="includeInactive"/> is what makes a soft-deleted
        /// parent reachable at all.
        /// </summary>
        Task<List<ParentDTO>> GetAllParentsAsync(int schoolId, bool includeInactive = false);

        /// <summary>Keyed on Parents.Id. Returns deactivated parents too.</summary>
        Task<ParentDTO?> GetParentByIdAsync(int schoolId, int parentId);

        /// <summary>
        /// Keyed on dbo.Users.Id, because the parent's own screen has only the token's
        /// UserId. Reads two result sets and stitches the children onto the profile.
        /// </summary>
        Task<ParentProfileDTO?> GetParentProfileAsync(int schoolId, int userId);

        /// <summary>
        /// Admin edit, keyed on Parents.Id. A replacement rather than a patch: a null phone
        /// number or address clears what is on file.
        /// </summary>
        Task<ProcResult> UpdateParentAsync(
            int schoolId,
            int parentId,
            ParentUpdateDTO parent,
            int? performedByUserId = null
        );

        /// <summary>
        /// Soft delete. Refuses a parent who has recorded fee payments; otherwise
        /// deactivates their children links along with the account.
        /// </summary>
        Task<ProcResult> DeleteParentAsync(int schoolId, int parentId, int? performedByUserId = null);

        /// <summary>
        /// Attaches a parent to a student, or re-activates and re-labels an existing link.
        /// <paramref name="relationship"/> must be Father, Mother or Guardian.
        /// </summary>
        Task<ProcResult> LinkStudentParentAsync(
            int schoolId,
            int studentId,
            int parentId,
            string relationship,
            int? performedByUserId = null
        );

        /// <summary>
        /// Detaches a parent from a student. Reports success when the link is already
        /// inactive, and an error only when no link between the two has ever existed.
        /// </summary>
        Task<ProcResult> UnlinkStudentParentAsync(
            int schoolId,
            int studentId,
            int parentId,
            int? performedByUserId = null
        );

        /// <summary>
        /// Who to contact about one student. The only call that fills
        /// <see cref="ParentDTO.Relationship"/>.
        /// </summary>
        Task<List<ParentDTO>> GetParentsByStudentAsync(int schoolId, int studentId);

        /// <summary>Keyed on Parents.Id, unlike sp_GetStudentChildren which takes the user id.</summary>
        Task<List<ParentChildDTO>> GetChildrenByParentAsync(int schoolId, int parentId);
    }
}
