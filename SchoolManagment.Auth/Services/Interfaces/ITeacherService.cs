using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Teachers;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface ITeacherService
    {
        Task<(ProcResult Result, TeacherRegistrationResponseDTO? Teacher)> RegisterTeacherAsync(
            TeacherRegistrationDTO teacher
        );

        Task<List<TeacherDTO>> GetAllTeachersAsync(int? subjectId = null);

        /// <summary>Keyed on dbo.Users.Id. Returns null for a deactivated teacher.</summary>
        Task<TeacherProfileDTO?> GetTeacherProfileAsync(int userId);

        Task<ProcResult> UpdateTeacherAsync(int teacherId, TeacherUpdateDTO teacher);

        /// <summary>The caller's own profile, resolved from the token's UserId claim.</summary>
        Task<ProcResult> UpdateTeacherProfileAsync(TeacherUpdateDTO teacher);

        Task<ProcResult> DeleteTeacherAsync(int teacherId);

        Task<(ProcResult Result, int SubjectsAssigned)> AssignSubjectsToTeacherAsync(
            int teacherId,
            List<int> subjectIds
        );

        Task<List<TeacherSubjectDTO>> GetTeacherSubjectsAsync(int teacherId);

        Task<List<TeacherClassDTO>> GetTeacherClassesAsync(int teacherId);

        Task<List<TeacherSubjectClassDTO>> GetTeacherSubjectAssignmentsAsync(int teacherId);

        Task<(ProcResult Result, int? AssignmentId)> AssignTeacherToSubjectClassAsync(
            int teacherId,
            int subjectId,
            int classId,
            bool isActive = true
        );
    }
}
