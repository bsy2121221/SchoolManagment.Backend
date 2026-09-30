using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Settings
{
    /// <summary>
    /// What a settings page posts when the user presses Save: every field on the
    /// form at once. sp_SaveMultipleSettings writes them in one transaction, so
    /// either the whole batch lands or none of it does.
    /// </summary>
    public class SettingsBulkSaveDTO
    {
        [Required(ErrorMessage = "At least one setting is required")]
        [MinLength(1, ErrorMessage = "At least one setting is required")]
        public List<SettingSaveDTO> Settings { get; set; } = new();
    }
}
