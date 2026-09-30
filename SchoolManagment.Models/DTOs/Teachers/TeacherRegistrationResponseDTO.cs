namespace SchoolManagment.Models.DTOs.Teachers
{
    /// <summary>
    /// What an admin needs to hand the new teacher: the username the procedure
    /// allocated, and the employee number it took from the school's sequence.
    ///
    /// The outcome no longer travels here. It used to be a <c>Result</c> string on
    /// this DTO, which meant the success flag and the payload were the same object
    /// and "did it work" was a string comparison at the controller. It is a
    /// <see cref="Common.ProcResult"/> alongside now, as in Students.
    /// </summary>
    public class TeacherRegistrationResponseDTO
    {
        /// <summary>dbo.Users.Id. The profile endpoints are keyed on this, not on TeacherRecordId.</summary>
        public int UserId { get; set; }

        /// <summary>dbo.Teachers.Id. Every write and assignment endpoint is keyed on this.</summary>
        public int TeacherRecordId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string EmployeeId { get; set; } = string.Empty;
    }
}
