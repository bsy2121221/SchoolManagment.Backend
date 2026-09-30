using SchoolManagment.Models.DTOs.Settings;

namespace SchoolManagment.DataAccess.Repositories.Interfaces
{
    /// <summary>
    /// dbo.Settings, one school at a time. Every method takes the school id as its
    /// first argument because every procedure in 13_Procs_Settings.sql does: the
    /// key is (SchoolId, Category, SettingKey), and settings used to be global --
    /// two schools shared one 'SystemInformation' category and overwrote each
    /// other's name and principal.
    ///
    /// The id comes from the caller's token, resolved by SettingService. Nothing
    /// here checks it.
    /// </summary>
    public interface ISettingRepository
    {
        Task<List<SettingDTO>> GetAllSettingsAsync(int schoolId);

        Task<List<SettingDTO>> GetSettingsByCategoryAsync(int schoolId, string category);

        /// <summary>
        /// Null when the setting does not exist *or* has been deleted -- the
        /// procedure filters on IsActive, so the two are indistinguishable from
        /// here. Saving over the second case revives it.
        /// </summary>
        Task<SettingDTO?> GetSettingByKeyAsync(int schoolId, string category, string settingKey);

        /// <summary>
        /// Upsert. <see cref="SettingResult.Created"/> for a new row,
        /// <see cref="SettingResult.Updated"/> for an existing one -- including one
        /// that was soft-deleted, which this brings back.
        /// </summary>
        Task<(SettingResult Result, string Message)> SaveSettingAsync(
            int schoolId, SettingSaveDTO setting, int? userId);

        /// <summary>
        /// One transaction, one MERGE. Rows the procedure cannot store are skipped
        /// and counted rather than failing the batch, and a key listed twice keeps
        /// its last occurrence.
        /// </summary>
        Task<(SettingResult Result, string Message, int Saved, int Skipped)> SaveMultipleSettingsAsync(
            int schoolId, List<SettingSaveDTO> settings, int? userId);

        /// <summary>
        /// Soft delete. <see cref="SettingResult.NotFound"/> when there was no
        /// active row to deactivate.
        /// </summary>
        Task<(SettingResult Result, string Message)> DeleteSettingAsync(
            int schoolId, string category, string settingKey, int? userId);

        /// <summary>
        /// Hard-deletes the rows in scope and re-seeds the school's defaults.
        /// <paramref name="category"/> null resets every category.
        /// </summary>
        Task<(SettingResult Result, string Message, int Restored)> ResetSettingsAsync(
            int schoolId, string? category, int? userId);
    }
}
