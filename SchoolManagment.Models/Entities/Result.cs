namespace SchoolManagment.Models.Entities
{
    public class Result
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }
        public int StudentId { get; set; }
        public int ExaminationId { get; set; }
        public int ObtainedMarks { get; set; }
        public string? Grade { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
