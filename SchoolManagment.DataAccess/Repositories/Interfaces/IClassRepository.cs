using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Classes;
using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface IClassRepository
    {
        /// <summary>The outcome, and the new id -- null on every failure branch.</summary>
        Task<(ProcResult Result, int? ClassId)> CreateClassAsync(int schoolId, ClassCreateDTO classDto);

        Task<(List<ClassDTO> Classes, int TotalCount)> GetAllClassesAsync(
            int schoolId,
            string? grade = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20
        );

        Task<ClassDTO?> GetClassByIdAsync(int schoolId, int classId);
        Task<ClassDetailsDTO?> GetClassDetailsAsync(int schoolId, int classId);
        /// <summary>
        /// These three carry the procedure's own message. It is the only thing that tells a
        /// user which of several conditions they have to clear -- an enrolled student, a
        /// term of attendance, a grade and section a soft-deleted class still occupies.
        /// </summary>
        Task<ProcResult> UpdateClassAsync(int schoolId, int classId, ClassUpdateDTO classDto);
        Task<ProcResult> UpdateClassStatusAsync(int schoolId, int classId, bool isActive);
        Task<ProcResult> DeleteClassAsync(int schoolId, int classId);
        Task<List<StudentDTO>> GetClassStudentsAsync(int schoolId, int classId);
        Task<ClassTimetableDTO?> GetClassTimetableAsync(int schoolId, int classId, int? dayOfWeek = null);
    }
}
