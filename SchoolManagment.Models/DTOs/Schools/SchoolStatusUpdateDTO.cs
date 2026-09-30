namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// Suspends or restores a tenant. Sent as a body rather than a query value to
    /// match the status endpoints on the other modules.
    /// </summary>
    public class SchoolStatusUpdateDTO
    {
        public bool IsActive { get; set; }
    }
}
