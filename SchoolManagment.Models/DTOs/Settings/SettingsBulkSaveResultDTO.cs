namespace SchoolManagment.Models.DTOs.Settings
{
    /// <summary>
    /// What a bulk save actually did. sp_SaveMultipleSettings skips a row it cannot
    /// store rather than failing the batch -- one bad field should not discard the
    /// other thirty -- and returns only counts, which is enough to say "saved 28 of
    /// 30" but not enough to say which two were dropped or why.
    ///
    /// <see cref="Skipped"/> fills that in: the service works out the same
    /// exclusions the procedure applies and names them, so the page can mark the
    /// offending fields instead of showing a success toast over silently discarded
    /// input.
    /// </summary>
    public class SettingsBulkSaveResultDTO
    {
        /// <summary>Rows the procedure inserted or updated.</summary>
        public int SettingsSaved { get; set; }

        /// <summary>
        /// Rows it declined, as counted by the procedure itself. This is the
        /// authoritative number; <see cref="Skipped"/> is the explanation, and if the
        /// two ever disagree the count is what happened.
        /// </summary>
        public int SettingsSkipped { get; set; }

        public List<SkippedSettingDTO> Skipped { get; set; } = new();
    }

    /// <summary>One row a bulk save did not store, and why.</summary>
    public class SkippedSettingDTO
    {
        public string Category { get; set; } = string.Empty;
        public string SettingKey { get; set; } = string.Empty;

        /// <summary>
        /// Plain enough to show a user: either the value did not match its declared
        /// type, or the same key appeared twice in the payload.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        public SkippedSettingDTO()
        {
        }

        public SkippedSettingDTO(string category, string settingKey, string reason)
        {
            Category = category;
            SettingKey = settingKey;
            Reason = reason;
        }
    }
}
