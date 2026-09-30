using SchoolManagment.Models.DTOs.Schedule;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    public interface IScheduleRepository
    {
        Task<ScheduleOperationResponseDTO> CreateOrUpdateScheduleEntryAsync(
            int schoolId,
            ScheduleEntryCreateDTO entry,
            int? id = null
        );

        Task<List<ScheduleEntryDTO>> GetTeacherScheduleAsync(int schoolId, int teacherId);

        Task<List<ScheduleEntryDTO>> GetTeacherScheduleByDayAsync(int schoolId, int teacherId, int dayOfWeek);

        Task<CurrentNextClassesResponseDTO> GetTeacherCurrentAndNextClassesAsync(int schoolId, int teacherId);

        Task<TeacherScheduleStatsDTO> GetTeacherScheduleStatsAsync(int schoolId, int teacherId);

        Task<List<ClassScheduleDTO>> GetClassScheduleAsync(int schoolId, int classId, int? dayOfWeek = null);

        Task<(string Result, string Message)> DeleteScheduleEntryAsync(int schoolId, int id);
    }
}
