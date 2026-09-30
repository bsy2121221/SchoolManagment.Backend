using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface IStudentRepository
    {
        /// <summary>
        /// Returns the procedure's own verdict alongside the new student. The
        /// procedure refuses a full class, a class in another school, a duplicate
        /// email and a username collision, each with a sentence worth showing --
        /// and <c>Student</c> is null for every one of them.
        /// </summary>
        Task<(ProcResult Result, StudentRegistrationResponseDTO? Student)> RegisterStudentAsync(
            int schoolId,
            StudentRegistrationDTO student,
            string passwordHash,
            int? performedByUserId = null
        );

        Task<(List<StudentDTO> Students, int TotalCount)> GetAllStudentsAsync(
            int schoolId,
            int? classId = null,
            bool? isActive = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 20
        );

        Task<StudentDTO?> GetStudentByIdAsync(int schoolId, int studentId);
        Task<StudentProfileDTO?> GetStudentProfileAsync(int schoolId, int studentId);

        Task<ProcResult> UpdateStudentAsync(
            int schoolId,
            int studentId,
            StudentUpdateDTO student,
            int? performedByUserId = null
        );

        Task<ProcResult> DeleteStudentAsync(int schoolId, int studentId, int? performedByUserId = null);

        Task<ProcResult> PromoteStudentAsync(
            int schoolId,
            int studentId,
            int newClassId,
            int academicYear,
            int? performedByUserId = null
        );

        // Subject assignments
        Task<ProcResult> AssignSubjectsToStudentAsync(int schoolId, int studentId, List<int> subjectIds);
        Task<List<SubjectDTO>> GetStudentSubjectsAsync(int schoolId, int studentId);
        Task<ProcResult> RemoveStudentSubjectAsync(int schoolId, int studentId, int subjectId);

        // By class
        Task<List<StudentDTO>> GetStudentsByClassAsync(int schoolId, int classId);
    }
}
