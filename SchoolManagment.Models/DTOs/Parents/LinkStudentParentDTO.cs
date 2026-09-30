using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Parents
{
    public class LinkStudentParentDTO
    {
        [Required(ErrorMessage = "Student ID is required")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Parent ID is required")]
        public int ParentId { get; set; }

        [Required(ErrorMessage = "Relationship is required")]
        [StringLength(20, ErrorMessage = "Relationship cannot exceed 20 characters")]
        public string Relationship { get; set; } = string.Empty; // Father, Mother, Guardian
    }

    public class UnlinkStudentParentDTO
    {
        [Required(ErrorMessage = "Student ID is required")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Parent ID is required")]
        public int ParentId { get; set; }
    }
}
