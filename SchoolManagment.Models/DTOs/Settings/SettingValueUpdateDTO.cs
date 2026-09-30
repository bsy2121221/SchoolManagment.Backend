using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Settings
{
    /// <summary>
    /// Changes an existing setting. Category and key come from the route, so they
    /// cannot be changed by an update -- renaming a setting is a delete and a
    /// create, because the pair is the identity.
    /// </summary>
    public class SettingValueUpdateDTO
    {
        [Required(AllowEmptyStrings = true, ErrorMessage = "Setting value is required")]
        public string SettingValue { get; set; } = string.Empty;

        /// <summary>
        /// Optional. Left out, the setting keeps the type it already has, which is
        /// what a settings form normally wants -- it is editing a value, not
        /// redeclaring the field. Supplying it changes the type as well, and the new
        /// value is then checked against the new type.
        /// </summary>
        [RegularExpression("^(string|number|boolean|json)?$",
            ErrorMessage = "Data type must be string, number, boolean or json")]
        public string? DataType { get; set; }

        /// <summary>
        /// Optional, and omitting it keeps the stored description. Note this differs
        /// from sp_SaveSetting, which writes whatever it is given and would blank the
        /// description out; the service reads the existing row first and passes the
        /// old value through.
        /// </summary>
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }
    }
}
