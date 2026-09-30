using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Settings;

namespace SchoolManagment.Auth.Services.Interfaces
{
    /// <summary>
    /// Settings for the school the caller's token names. Nothing here takes a school
    /// id, so there is no way to reach another tenant's settings through it -- a
    /// platform administrator sees settings only while switched into a school, and
    /// otherwise gets nothing.
    ///
    /// Two rules live here rather than in SQL, because sp_SaveSetting is a single
    /// upsert and the API needs create and update to mean different things:
    ///
    ///   * <see cref="CreateSettingAsync"/> refuses when the setting already exists,
    ///     so Settings:Create cannot be used to overwrite a value. It does allow
    ///     writing over a soft-deleted setting, which is how a deleted one comes
    ///     back -- the procedure updates the row that was never removed.
    ///   * <see cref="UpdateSettingAsync"/> refuses when it does not, so
    ///     Settings:Edit cannot be used to invent one.
    ///
    /// Without that split either permission would grant the other, since the
    /// procedure cannot tell which was intended.
    /// </summary>
    public interface ISettingService
    {
        /// <summary>
        /// Every active setting for the school, or one category's worth. Empty when
        /// no school is in scope rather than an error, so a platform administrator's
        /// settings screen renders blank instead of failing.
        /// </summary>
        Task<List<SettingDTO>> GetSettingsAsync(string? category = null);

        Task<SettingDTO?> GetSettingAsync(string category, string settingKey);

        /// <summary>
        /// The school's grading scale, exactly as fn_CalculateGrade would apply it,
        /// including its fallbacks for a threshold that is missing or not a number.
        /// </summary>
        Task<List<GradeThresholdDTO>> GetGradeThresholdsAsync();

        /// <summary>
        /// <see cref="SettingResult.Conflict"/> when an active setting with that
        /// category and key already exists.
        /// </summary>
        Task<(SettingResult Result, string Message, SettingDTO? Setting, List<ErrorDetail>? Errors)> CreateSettingAsync(
            SettingSaveDTO setting);

        /// <summary>
        /// <see cref="SettingResult.NotFound"/> when there is no active setting to
        /// change. DataType and Description are carried over from the stored row when
        /// the request omits them.
        /// </summary>
        Task<(SettingResult Result, string Message, SettingDTO? Setting, List<ErrorDetail>? Errors)> UpdateSettingAsync(
            string category, string settingKey, SettingValueUpdateDTO update);

        /// <summary>
        /// Writes a whole form at once. Never partially fails -- the procedure runs
        /// one transaction -- but individual rows can be skipped, and the result names
        /// them.
        /// </summary>
        Task<(SettingResult Result, string Message, SettingsBulkSaveResultDTO? Saved)> SaveSettingsAsync(
            SettingsBulkSaveDTO request);

        /// <summary>
        /// Soft delete. Refuses the six gradeThreshold* settings: fn_CalculateGrade
        /// reads them without filtering on IsActive, so deleting one hides it from
        /// every read endpoint while it carries on deciding grades.
        /// </summary>
        Task<(SettingResult Result, string Message)> DeleteSettingAsync(string category, string settingKey);

        /// <summary>
        /// Discards customisation and restores the seeded defaults for one category,
        /// or for all of them when none is named.
        /// </summary>
        Task<(SettingResult Result, string Message, int Restored)> ResetSettingsAsync(SettingsResetDTO request);
    }
}
