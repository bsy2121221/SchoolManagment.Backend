using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.DTOs.Schedule;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ITenantContext _tenantContext;

        public ScheduleService(IScheduleRepository scheduleRepository, ITenantContext tenantContext)
        {
            _scheduleRepository = scheduleRepository;
            _tenantContext = tenantContext;
        }

        public async Task<ScheduleOperationResponseDTO> CreateScheduleEntryAsync(ScheduleEntryCreateDTO entry)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new ScheduleOperationResponseDTO
                {
                    Result = "Error",
                    Message = "School context not found"
                };
            }

            return await _scheduleRepository.CreateOrUpdateScheduleEntryAsync(
                _tenantContext.SchoolId.Value,
                entry,
                null
            );
        }

        public async Task<ScheduleOperationResponseDTO> UpdateScheduleEntryAsync(int id, ScheduleEntryCreateDTO entry)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new ScheduleOperationResponseDTO
                {
                    Result = "Error",
                    Message = "School context not found"
                };
            }

            return await _scheduleRepository.CreateOrUpdateScheduleEntryAsync(
                _tenantContext.SchoolId.Value,
                entry,
                id
            );
        }

        public async Task<List<ScheduleEntryDTO>> GetTeacherScheduleAsync(int teacherId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ScheduleEntryDTO>();
            }

            return await _scheduleRepository.GetTeacherScheduleAsync(_tenantContext.SchoolId.Value, teacherId);
        }

        public async Task<List<ScheduleEntryDTO>> GetTeacherScheduleByDayAsync(int teacherId, int dayOfWeek)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ScheduleEntryDTO>();
            }

            return await _scheduleRepository.GetTeacherScheduleByDayAsync(
                _tenantContext.SchoolId.Value,
                teacherId,
                dayOfWeek
            );
        }

        public async Task<CurrentNextClassesResponseDTO> GetTeacherCurrentAndNextClassesAsync(int teacherId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new CurrentNextClassesResponseDTO();
            }

            return await _scheduleRepository.GetTeacherCurrentAndNextClassesAsync(
                _tenantContext.SchoolId.Value,
                teacherId
            );
        }

        public async Task<TeacherScheduleStatsDTO> GetTeacherScheduleStatsAsync(int teacherId)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new TeacherScheduleStatsDTO();
            }

            return await _scheduleRepository.GetTeacherScheduleStatsAsync(_tenantContext.SchoolId.Value, teacherId);
        }

        public async Task<List<ClassScheduleDTO>> GetClassScheduleAsync(int classId, int? dayOfWeek = null)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return new List<ClassScheduleDTO>();
            }

            return await _scheduleRepository.GetClassScheduleAsync(
                _tenantContext.SchoolId.Value,
                classId,
                dayOfWeek
            );
        }

        public async Task<(string Result, string Message)> DeleteScheduleEntryAsync(int id)
        {
            if (!_tenantContext.SchoolId.HasValue)
            {
                return ("Error", "School context not found");
            }

            return await _scheduleRepository.DeleteScheduleEntryAsync(_tenantContext.SchoolId.Value, id);
        }
    }
}
