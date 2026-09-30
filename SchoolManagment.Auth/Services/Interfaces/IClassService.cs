using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Classes;
using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IClassService
    {
        /// <summary>
        /// The outcome, and the new id on success. The message matters here: the duplicate
        /// check in sp_CreateClass is not filtered on IsActive, so a grade and section can
        /// be refused because a *soft-deleted* class still holds it -- which is
        /// inexplicable to anyone looking at a list of active classes.
        /// </summary>
        Task<(ProcResult Result, int? ClassId)> CreateClassAsync(ClassCreateDTO classDto);

        Task<PaginatedResponse<ClassDTO>> GetAllClassesAsync(
            string? grade = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 20
        );

        Task<ClassDTO?> GetClassByIdAsync(int classId);
        Task<ClassDetailsDTO?> GetClassDetailsAsync(int classId);
        Task<ProcResult> UpdateClassAsync(int classId, ClassUpdateDTO classDto);
        Task<ProcResult> UpdateClassStatusAsync(int classId, bool isActive);
        Task<ProcResult> DeleteClassAsync(int classId);
        Task<List<StudentDTO>> GetClassStudentsAsync(int classId);
        Task<ClassTimetableDTO?> GetClassTimetableAsync(int classId, int? dayOfWeek = null);
    }
}
