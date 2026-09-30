using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Attendance
{
    public class AttendanceBulkMarkDTO
    {
        [Required(ErrorMessage = "Class ID is required")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Attendance date is required")]
        public DateTime AttendanceDate { get; set; }

        [Required(ErrorMessage = "At least one attendance record is required")]
        [MinLength(1, ErrorMessage = "At least one attendance record is required")]
        public List<AttendanceRecordDTO> Records { get; set; } = new();
    }

    public class AttendanceRecordDTO
    {
        [Required(ErrorMessage = "Student ID is required")]
        public int StudentId { get; set; }

        /// <summary>
        /// Present or absent. There is no third state on the wire: an unmarked student is
        /// one who is not in <see cref="AttendanceBulkMarkDTO.Records"/> at all, because
        /// <c>Attendance.IsPresent</c> is <c>NOT NULL</c> and a row therefore cannot say
        /// "no decision yet".
        ///
        /// Note the <c>[Required]</c> this property used to carry did nothing: a
        /// non-nullable <c>bool</c> always has a value, so an omitted field bound to
        /// <c>false</c> and passed validation as "absent". Removed rather than left to
        /// imply a check that was not happening.
        /// </summary>
        public bool IsPresent { get; set; }

        [StringLength(255, ErrorMessage = "Remarks cannot exceed 255 characters")]
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// What <c>sp_MarkAttendanceBulk</c> counted.
    ///
    /// The <c>Result</c> property this class used to carry is gone, as
    /// <c>TeacherRegistrationResponseDTO</c>'s and <c>ParentRegistrationResponseDTO</c>'s
    /// were before it: the outcome of a procedure is a <c>ProcResult</c> travelling beside
    /// the payload, not a magic string inside it. A DTO that carries its own success flag
    /// makes every caller re-implement the comparison, which is how the
    /// <c>Contains("successfully")</c> family of defects started.
    /// </summary>
    public class AttendanceBulkMarkResponseDTO
    {
        /// <summary>
        /// Rows the MERGE touched -- inserted plus updated. Re-submitting an identical
        /// register still counts every row as marked, because the UPDATE branch runs
        /// regardless of whether the values differ.
        /// </summary>
        public int RecordsMarked { get; set; }

        /// <summary>
        /// Records sent for a student who is not actively enrolled in that class, and so
        /// were dropped rather than filed against the wrong register. Non-zero here means
        /// the caller's idea of the class roll is stale, which is worth telling them.
        /// </summary>
        public int RecordsSkipped { get; set; }
    }
}
