using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IStudentService
    {
        /// <summary>
        /// <c>Student</c> is null unless <c>Result.Success</c>; the reason a
        /// registration was refused ("Class 10A is full", "Email already exists")
        /// is in <c>Result.Message</c> and belongs in the response.
        /// </summary>
        Task<(ProcResult Result, StudentRegistrationResponseDTO? Student)> RegisterStudentAsync(
            StudentRegistrationDTO student
        );

        Task<PaginatedResponse<StudentDTO>> GetAllStudentsAsync(
            int? classId = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20
        );

        Task<StudentDTO?> GetStudentByIdAsync(int studentId);
        Task<StudentProfileDTO?> GetStudentProfileAsync(int studentId);
        Task<ProcResult> UpdateStudentAsync(int studentId, StudentUpdateDTO student);
        Task<ProcResult> DeleteStudentAsync(int studentId);
        Task<ProcResult> PromoteStudentAsync(int studentId, StudentPromoteDTO promotion);

        // Subject assignments
        Task<ProcResult> AssignSubjectsToStudentAsync(int studentId, StudentSubjectAssignmentDTO assignment);
        Task<List<SubjectDTO>> GetStudentSubjectsAsync(int studentId);
        Task<ProcResult> RemoveStudentSubjectAsync(int studentId, int subjectId);

        // By class
        Task<List<StudentDTO>> GetStudentsByClassAsync(int classId);
    }
}
