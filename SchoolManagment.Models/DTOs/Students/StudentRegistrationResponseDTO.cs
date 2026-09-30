namespace SchoolManagment.Models.DTOs.Students
{
    public class StudentRegistrationResponseDTO
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public string? RollNumber { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }
}
