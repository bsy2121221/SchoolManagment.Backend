namespace SchoolManagment.Models.DTOs.Schedule
{
    public class ScheduleOperationResponseDTO
    {
        /// <summary>"Success", "Conflict" (the slot is taken) or "Error".</summary>
        public string Result { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int? Id { get; set; }

        /// <summary>On a "Conflict", the id of the entry already in the slot.</summary>
        public int? ConflictWith { get; set; }
    }
}
