using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Teachers;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface ITeacherRepository
    {
        Task<(ProcResult Result, TeacherRegistrationResponseDTO? Teacher)> RegisterTeacherAsync(
            int schoolId,
            TeacherRegistrationDTO teacher,
            string passwordHash,
            int? performedByUserId = null
        );

        /// <summary>
        /// Every active teacher in the school, optionally narrowed to those who teach one
        /// subject. Unpaginated and unsearchable: sp_GetTeachersWithDetails takes no page,
        /// no search term and hard-filters IsActive, so a deleted teacher is unreachable
        /// through this call.
        /// </summary>
        Task<List<TeacherDTO>> GetAllTeachersAsync(int schoolId, int? subjectId = null);

        /// <summary>Keyed on dbo.Users.Id, unlike every write below.</summary>
        Task<TeacherProfileDTO?> GetTeacherProfileAsync(int schoolId, int userId);

        Task<TeacherProfileStatsDTO?> GetTeacherProfileStatsAsync(int schoolId, int userId);

        /// <summary>
        /// Admin edit. A non-empty <c>SubjectIds</c> replaces the teacher's subject links
        /// outright; null leaves them untouched.
        /// </summary>
        Task<ProcResult> UpdateTeacherAsync(
            int schoolId,
            int teacherId,
            TeacherUpdateDTO teacher,
            int? performedByUserId = null
        );

        /// <summary>Self-service edit. Salary and subject links are not reachable from here.</summary>
        Task<ProcResult> UpdateTeacherProfileAsync(int schoolId, int userId, TeacherUpdateDTO teacher);

        Task<ProcResult> DeleteTeacherAsync(int schoolId, int teacherId, int? performedByUserId = null);

        /// <summary>
        /// Replaces the teacher's whole subject set: anything absent from
        /// <paramref name="subjectIds"/> is deactivated. <c>SubjectsAssigned</c> is what
        /// survived, not what was added.
        /// </summary>
        Task<(ProcResult Result, int SubjectsAssigned)> AssignSubjectsToTeacherAsync(
            int schoolId,
            int teacherId,
            List<int> subjectIds
        );

        Task<List<TeacherSubjectDTO>> GetTeacherSubjectsAsync(int schoolId, int teacherId);

        Task<List<TeacherClassDTO>> GetTeacherClassesAsync(int schoolId, int teacherId);

        Task<List<TeacherSubjectClassDTO>> GetTeacherSubjectAssignmentsAsync(int schoolId, int teacherId);

        /// <summary>
        /// Upserts one subject-in-one-class assignment. <paramref name="isActive"/> false
        /// withdraws it -- the procedure is a MERGE, so this is also the un-assign path.
        /// </summary>
        Task<(ProcResult Result, int? AssignmentId)> AssignTeacherToSubjectClassAsync(
            int schoolId,
            int teacherId,
            int subjectId,
            int classId,
            bool isActive = true
        );

        Task<int?> GetTeacherIdByUserIdAsync(int schoolId, int userId);
    }
}
