namespace SchoolManagment.Models.Entities
{
    public class Parent
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int UserId { get; set; }
        public string? Occupation { get; set; }
        public decimal? AnnualIncome { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class StudentParent
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int StudentId { get; set; }
        public int ParentId { get; set; }
        public string Relationship { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
