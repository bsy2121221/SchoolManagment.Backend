namespace SchoolManagment.Models.DTOs.Settings
{
    /// <summary>
    /// The Result column of every procedure in 13_Procs_Settings.sql, named after
    /// the literals themselves so the mapping is a lookup rather than a guess.
    ///
    /// Error is 0 deliberately: an unrecognised or missing literal lands on it by
    /// default, so a procedure that starts reporting something new is treated as a
    /// failure instead of being read as a silent success. That is the mistake the
    /// older repositories make by testing whether a message merely contains the
    /// word "successfully".
    /// </summary>
    public enum SettingResult
    {
        /// <summary>Also covers "no row came back at all".</summary>
        Error = 0,

        /// <summary>sp_DeleteSetting and sp_ResetSettings on success.</summary>
        Success,

        /// <summary>sp_SaveSetting inserted a new row.</summary>
        Created,

        /// <summary>
        /// sp_SaveSetting updated an existing row. Also what comes back when a
        /// soft-deleted setting is revived, because the row was still there.
        /// </summary>
        Updated,

        /// <summary>
        /// The setting, or the category, does not exist. Distinct from Error so the
        /// controller can answer 404 rather than 400.
        /// </summary>
        NotFound,

        /// <summary>
        /// The only member no procedure emits. sp_SaveSetting is an upsert, so it
        /// would happily let a create overwrite an existing setting; the service
        /// refuses instead, and this is how it says so. See ISettingService.
        /// </summary>
        Conflict
    }
}
