namespace SchoolManagment.Models.Entities
{
    public class Teacher
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int UserId { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Qualification { get; set; }
        public int? Experience { get; set; }
        public decimal? Salary { get; set; }
        public DateTime? JoinDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
