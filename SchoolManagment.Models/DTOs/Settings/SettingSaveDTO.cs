using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Settings
{
    /// <summary>
    /// One setting to write. Used for a single create and as the element type of a
    /// bulk save, because sp_SaveSetting and sp_SaveMultipleSettings take the same
    /// five fields.
    ///
    /// Category and SettingKey are open: the procedures accept any name, so a
    /// school can add settings the seed never defined. The six seeded categories
    /// (SystemConfiguration, UserManagement, AcademicSettings, SecuritySettings,
    /// NotificationSettings, SystemInformation) are a convention, not a constraint,
    /// and are not enforced here -- rejecting an unseeded category would make the
    /// endpoint narrower than the table it writes to.
    /// </summary>
    public class SettingSaveDTO
    {
        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Setting key is required")]
        [StringLength(100, ErrorMessage = "Setting key cannot exceed 100 characters")]
        public string SettingKey { get; set; } = string.Empty;

        /// <summary>
        /// Required but allowed to be empty -- the seed itself stores '' for
        /// allowedIPs and schoolWebsite. Null is not allowed: the column is NOT
        /// NULL and fn_IsValidSettingValue rejects it outright.
        /// </summary>
        [Required(AllowEmptyStrings = true, ErrorMessage = "Setting value is required")]
        public string SettingValue { get; set; } = string.Empty;

        /// <summary>
        /// Checked against the value before the procedure is called, so a 'number'
        /// holding "abc" comes back as a field error on settingValue rather than as
        /// a bare procedure message. That matters more here than elsewhere:
        /// fn_CalculateGrade reads the gradeThreshold* settings as numbers, so one
        /// bad save changes every grade the school issues.
        ///
        /// Null and empty are accepted and mean 'string', because
        /// sp_SaveMultipleSettings defaults them that way and being stricter here
        /// than the procedure is how the two ends drift apart. Case and surrounding
        /// whitespace are tolerated for the same reason -- SQL's comparison is
        /// case-insensitive, so 'Boolean' reaches the procedure happily, and the
        /// service lower-cases it before the write.
        ///
        /// In a bulk save this is the one field whose rejection fails the whole
        /// batch instead of skipping the row: a name that is not a data type at all
        /// is a mistake in the request, and "Settings[4].DataType" says which entry
        /// to fix, where the procedure's skip count would not.
        /// </summary>
        [RegularExpression(@"^(?i)\s*(string|number|boolean|json)?\s*$",
            ErrorMessage = "Data type must be string, number, boolean or json")]
        public string? DataType { get; set; } = "string";

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters")]
        public string? Description { get; set; }
    }
}
