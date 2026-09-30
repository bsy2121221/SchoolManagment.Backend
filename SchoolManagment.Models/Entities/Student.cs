namespace SchoolManagment.Models.Entities
{
    public class Student
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int UserId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public int? ClassId { get; set; }
        public string? RollNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string? BloodGroup { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
