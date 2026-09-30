namespace SchoolManagment.Models.DTOs.Parents
{
    /// <summary>
    /// What a successful registration hands back: the generated login and the two ids the
    /// caller needs to go on working with the new parent.
    ///
    /// There is deliberately no <c>Result</c> property. The procedure's outcome now travels
    /// as a <see cref="SchoolManagment.Models.Common.ProcResult"/> alongside this object, so
    /// a failure cannot arrive disguised as a populated response -- the same change made to
    /// TeacherRegistrationResponseDTO.
    /// </summary>
    public class ParentRegistrationResponseDTO
    {
        public int UserId { get; set; }
        public int ParentId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}
