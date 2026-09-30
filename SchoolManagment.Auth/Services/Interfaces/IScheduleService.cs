using SchoolManagment.Models.DTOs.Schedule;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IScheduleService
    {
        Task<ScheduleOperationResponseDTO> CreateScheduleEntryAsync(ScheduleEntryCreateDTO entry);

        Task<ScheduleOperationResponseDTO> UpdateScheduleEntryAsync(int id, ScheduleEntryCreateDTO entry);

        Task<List<ScheduleEntryDTO>> GetTeacherScheduleAsync(int teacherId);

        Task<List<ScheduleEntryDTO>> GetTeacherScheduleByDayAsync(int teacherId, int dayOfWeek);

        Task<CurrentNextClassesResponseDTO> GetTeacherCurrentAndNextClassesAsync(int teacherId);

        Task<TeacherScheduleStatsDTO> GetTeacherScheduleStatsAsync(int teacherId);

        Task<List<ClassScheduleDTO>> GetClassScheduleAsync(int classId, int? dayOfWeek = null);

        Task<(string Result, string Message)> DeleteScheduleEntryAsync(int id);
    }
}
