using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Results
{
    public class ResultEntryDTO
    {
        [Required(ErrorMessage = "Student ID is required")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Examination ID is required")]
        public int ExaminationId { get; set; }

        [Required(ErrorMessage = "Obtained marks is required")]
        [Range(0, 1000, ErrorMessage = "Obtained marks must be between 0 and 1000")]
        public int ObtainedMarks { get; set; }

        /// <summary>
        /// Leave this null so the school's own grading scale applies. See
        /// <see cref="GradeEntryRecordDTO.Grade"/> for why sending a letter is almost always
        /// wrong.
        /// </summary>
        [StringLength(5, ErrorMessage = "Grade cannot exceed 5 characters")]
        public string? Grade { get; set; }

        [StringLength(255, ErrorMessage = "Remarks cannot exceed 255 characters")]
        public string? Remarks { get; set; }
    }
}
