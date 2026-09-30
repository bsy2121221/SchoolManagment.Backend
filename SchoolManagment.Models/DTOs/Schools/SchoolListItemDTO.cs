namespace SchoolManagment.Models.DTOs.Schools
{
    /// <summary>
    /// A row of the tenant list. sp_GetSchools carries the headline counts with
    /// each school so the platform list does not need a follow-up call per row.
    /// </summary>
    public class SchoolListItemDTO : SchoolDTO
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalClasses { get; set; }
        public int TotalUsers { get; set; }
    }
}
