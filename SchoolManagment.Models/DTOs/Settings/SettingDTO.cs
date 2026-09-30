namespace SchoolManagment.Models.DTOs.Settings
{
    /// <summary>
    /// One row of dbo.Settings, as the three read procedures return it.
    ///
    /// A setting is addressed by (Category, SettingKey) rather than by Id: that
    /// pair is the unique key, it is what every write procedure takes, and it is
    /// stable across a reset -- which deletes and re-seeds the rows, so the Id a
    /// client cached would be gone.
    /// </summary>
    public class SettingDTO
    {
        public int Id { get; set; }
        public int SchoolId { get; set; }

        public string Category { get; set; } = string.Empty;
        public string SettingKey { get; set; } = string.Empty;

        /// <summary>
        /// Always a string, whatever <see cref="DataType"/> says -- the column is
        /// NVARCHAR(MAX). The client converts; the procedures only guarantee that
        /// the text is convertible to the declared type.
        /// </summary>
        public string SettingValue { get; set; } = string.Empty;

        /// <summary>One of string, number, boolean, json (CK_Settings_DataType).</summary>
        public string DataType { get; set; } = "string";

        public string? Description { get; set; }

        /// <summary>
        /// Always true on anything the read procedures return -- they filter on it.
        /// Carried anyway because it is the column a delete actually changes.
        /// </summary>
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Null when the setting was written by someone outside the school -- a
        /// platform administrator has no Users row in that tenant, and
        /// (SchoolId, CreatedBy) is a composite foreign key, so the procedures
        /// store NULL rather than failing the save.
        /// </summary>
        public int? CreatedBy { get; set; }

        public int? UpdatedBy { get; set; }
    }
}
