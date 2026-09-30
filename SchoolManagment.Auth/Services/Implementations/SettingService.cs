using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Settings;
using System.Globalization;
using System.Text.Json;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class SettingService : ISettingService
    {
        private const string NoSchoolInScope =
            "No school is in scope for this session. A platform administrator must switch " +
            "into a school before reading or changing its settings.";

        /// <summary>
        /// The grading scale, in the order fn_CalculateGrade tests it, with the
        /// fallback each band uses when its setting is missing or not a number.
        /// Duplicated from SQL rather than read from it because the function hardcodes
        /// them too -- there is no row to read when the row is what is missing.
        /// </summary>
        private static readonly (string Key, string Grade, decimal Default)[] GradeBands =
        {
            ("gradeThresholdAPlus", "A+", 90m),
            ("gradeThresholdA",     "A",  80m),
            ("gradeThresholdB",     "B",  70m),
            ("gradeThresholdC",     "C",  60m),
            ("gradeThresholdD",     "D",  50m),
            ("gradeThresholdE",     "E",  40m)
        };

        /// <summary>
        /// What TRY_CONVERT(DECIMAL(38,10), @Value) accepts: surrounding whitespace and
        /// a leading sign, but no exponent, no thousands separator and no currency
        /// symbol. Verified against the server rather than assumed.
        /// </summary>
        private const NumberStyles SqlDecimalStyles =
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

        private readonly ISettingRepository _settingRepository;
        private readonly ITenantContext _tenantContext;

        public SettingService(ISettingRepository settingRepository, ITenantContext tenantContext)
        {
            _settingRepository = settingRepository;
            _tenantContext = tenantContext;
        }

        private int? ActorUserId => _tenantContext.UserId;

        public async Task<List<SettingDTO>> GetSettingsAsync(string? category = null)
        {
            if (!_tenantContext.SchoolId.HasValue)
                return new List<SettingDTO>();

            var schoolId = _tenantContext.SchoolId.Value;

            return string.IsNullOrWhiteSpace(category)
                ? await _settingRepository.GetAllSettingsAsync(schoolId)
                : await _settingRepository.GetSettingsByCategoryAsync(schoolId, category);
        }

        public async Task<SettingDTO?> GetSettingAsync(string category, string settingKey)
        {
            if (!_tenantContext.SchoolId.HasValue)
                return null;

            return await _settingRepository.GetSettingByKeyAsync(
                _tenantContext.SchoolId.Value, category, settingKey);
        }

        public async Task<List<GradeThresholdDTO>> GetGradeThresholdsAsync()
        {
            if (!_tenantContext.SchoolId.HasValue)
                return new List<GradeThresholdDTO>();

            var stored = await _settingRepository.GetSettingsByCategoryAsync(
                _tenantContext.SchoolId.Value, Constants.SettingCategories.AcademicSettings);

            var byKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var setting in stored)
            {
                byKey[setting.SettingKey] = setting.SettingValue;
            }

            // Declared order, not sorted by value. fn_CalculateGrade tests the bands in
            // this sequence and returns the first that matches, so a school that has set
            // A above A+ really does never award A+ -- sorting here would hide that.
            var bands = new List<GradeThresholdDTO>(GradeBands.Length + 1);

            foreach (var (key, grade, fallback) in GradeBands)
            {
                var minPercentage = fallback;
                var configured = false;

                if (byKey.TryGetValue(key, out var raw)
                    && decimal.TryParse(raw, SqlDecimalStyles, CultureInfo.InvariantCulture, out var parsed))
                {
                    minPercentage = parsed;
                    configured = true;
                }

                bands.Add(new GradeThresholdDTO(grade, minPercentage, key, !configured));
            }

            // Not a setting: F is whatever is left below E.
            bands.Add(new GradeThresholdDTO("F", 0m, null, false));

            return bands;
        }

        public async Task<(SettingResult Result, string Message, SettingDTO? Setting, List<ErrorDetail>? Errors)>
            CreateSettingAsync(SettingSaveDTO setting)
        {
            if (!_tenantContext.SchoolId.HasValue)
                return (SettingResult.Error, NoSchoolInScope, null, null);

            var schoolId = _tenantContext.SchoolId.Value;
            var dataType = NormaliseDataType(setting.DataType);

            var invalid = Validate(dataType, setting.SettingValue);
            if (invalid != null)
                return (SettingResult.Error, invalid.Message, null, new List<ErrorDetail> { invalid });

            var existing = await _settingRepository.GetSettingByKeyAsync(
                schoolId, setting.Category, setting.SettingKey);

            if (existing != null)
            {
                return (SettingResult.Conflict,
                    $"Setting '{setting.SettingKey}' already exists in category " +
                    $"'{setting.Category}'. Change it with PUT instead.",
                    existing, null);
            }

            var payload = new SettingSaveDTO
            {
                Category = setting.Category,
                SettingKey = setting.SettingKey,
                SettingValue = setting.SettingValue,
                DataType = dataType,
                Description = setting.Description
            };

            return await SaveAndReadBackAsync(schoolId, payload);
        }

        public async Task<(SettingResult Result, string Message, SettingDTO? Setting, List<ErrorDetail>? Errors)>
            UpdateSettingAsync(string category, string settingKey, SettingValueUpdateDTO update)
        {
            if (!_tenantContext.SchoolId.HasValue)
                return (SettingResult.Error, NoSchoolInScope, null, null);

            var schoolId = _tenantContext.SchoolId.Value;

            var existing = await _settingRepository.GetSettingByKeyAsync(schoolId, category, settingKey);
            if (existing == null)
            {
                return (SettingResult.NotFound,
                    $"Setting '{settingKey}' was not found in category '{category}'.", null, null);
            }

            var dataType = string.IsNullOrWhiteSpace(update.DataType)
                ? existing.DataType
                : NormaliseDataType(update.DataType);

            var invalid = Validate(dataType, update.SettingValue);
            if (invalid != null)
                return (SettingResult.Error, invalid.Message, null, new List<ErrorDetail> { invalid });

            var payload = new SettingSaveDTO
            {
                // The stored spelling, not the route's. Category and key match
                // case-insensitively in SQL, so a request for 'academicsettings' would
                // otherwise rewrite the row's own casing on every save.
                Category = existing.Category,
                SettingKey = existing.SettingKey,
                SettingValue = update.SettingValue,
                DataType = dataType,
                Description = update.Description ?? existing.Description
            };

            return await SaveAndReadBackAsync(schoolId, payload);
        }

        public async Task<(SettingResult Result, string Message, SettingsBulkSaveResultDTO? Saved)>
            SaveSettingsAsync(SettingsBulkSaveDTO request)
        {
            if (!_tenantContext.SchoolId.HasValue)
                return (SettingResult.Error, NoSchoolInScope, null);

            // Sent whole, including the rows this predicts will be skipped: the
            // procedure is the authority on what it stores, and filtering first would
            // mean its counts described a payload it never saw.
            var skipped = ExplainSkips(request.Settings);

            var (result, message, saved, skippedCount) = await _settingRepository.SaveMultipleSettingsAsync(
                _tenantContext.SchoolId.Value, request.Settings, ActorUserId);

            if (result != SettingResult.Success)
                return (result, message, null);

            return (result, message, new SettingsBulkSaveResultDTO
            {
                SettingsSaved = saved,
                SettingsSkipped = skippedCount,
                Skipped = skipped
            });
        }

        public async Task<(SettingResult Result, string Message)> DeleteSettingAsync(
            string category, string settingKey)
        {
            if (!_tenantContext.SchoolId.HasValue)
                return (SettingResult.Error, NoSchoolInScope);

            if (IsGradeThreshold(category, settingKey))
            {
                // fn_CalculateGrade reads the gradeThreshold* rows without filtering on
                // IsActive. Deleting one would hide it from every read endpoint while it
                // carried on deciding grades -- the settings page would show the default
                // and the marks would use the old value. Changing it is the only safe
                // way to change it.
                return (SettingResult.Error,
                    $"'{settingKey}' is part of the grading scale and cannot be deleted. " +
                    "Set its value instead.");
            }

            return await _settingRepository.DeleteSettingAsync(
                _tenantContext.SchoolId.Value, category, settingKey, ActorUserId);
        }

        public async Task<(SettingResult Result, string Message, int Restored)> ResetSettingsAsync(
            SettingsResetDTO request)
        {
            if (!_tenantContext.SchoolId.HasValue)
                return (SettingResult.Error, NoSchoolInScope, 0);

            var category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category;

            return await _settingRepository.ResetSettingsAsync(
                _tenantContext.SchoolId.Value, category, ActorUserId);
        }

        /// <summary>
        /// Saves, then reads the row back so the caller gets the id and timestamps the
        /// procedure does not return. 'Updated' from a create means the row was still
        /// there, soft-deleted, and has been revived.
        /// </summary>
        private async Task<(SettingResult Result, string Message, SettingDTO? Setting, List<ErrorDetail>? Errors)>
            SaveAndReadBackAsync(int schoolId, SettingSaveDTO payload)
        {
            var (result, message) = await _settingRepository.SaveSettingAsync(schoolId, payload, ActorUserId);

            if (result is not (SettingResult.Created or SettingResult.Updated))
                return (result, message, null, null);

            var saved = await _settingRepository.GetSettingByKeyAsync(
                schoolId, payload.Category, payload.SettingKey);

            return (result, message, saved, null);
        }

        /// <summary>
        /// Which rows sp_SaveMultipleSettings will drop, and why. It reports only a
        /// count, so this reproduces its filter in the same order: the type check
        /// first, then de-duplication over what survived -- meaning a key listed twice
        /// with one bad value keeps the good one, whichever came first.
        ///
        /// The data-type branch is a backstop: over HTTP, SettingSaveDTO's regular
        /// expression rejects an unknown type before the batch is ever sent, with the
        /// index of the offending entry. It is kept because this method's contract is
        /// to describe what the procedure will do, and the procedure does skip such a
        /// row rather than refusing the batch.
        /// </summary>
        private static List<SkippedSettingDTO> ExplainSkips(List<SettingSaveDTO> settings)
        {
            var skipped = new List<SkippedSettingDTO>();
            var reasons = new string?[settings.Count];

            for (var i = 0; i < settings.Count; i++)
            {
                var dataType = NormaliseDataType(settings[i].DataType);

                if (!Constants.SettingDataTypes.All.Contains(dataType))
                {
                    reasons[i] = $"'{settings[i].DataType}' is not a valid data type";
                    continue;
                }

                if (!IsValidValue(dataType, settings[i].SettingValue))
                {
                    reasons[i] = $"Value is not a valid {dataType}";
                }
            }

            // Case-insensitive, because the unique key and the MERGE's join both are.
            var lastValid = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < settings.Count; i++)
            {
                if (reasons[i] == null)
                    lastValid[$"{settings[i].Category}\u0000{settings[i].SettingKey}"] = i;
            }

            for (var i = 0; i < settings.Count; i++)
            {
                var reason = reasons[i];

                if (reason == null
                    && lastValid[$"{settings[i].Category}\u0000{settings[i].SettingKey}"] != i)
                {
                    reason = "Superseded by a later entry for the same key";
                }

                if (reason != null)
                    skipped.Add(new SkippedSettingDTO(settings[i].Category, settings[i].SettingKey, reason));
            }

            return skipped;
        }

        private static bool IsGradeThreshold(string category, string settingKey) =>
            string.Equals(category, Constants.SettingCategories.AcademicSettings,
                          StringComparison.OrdinalIgnoreCase)
            && GradeBands.Any(band => string.Equals(band.Key, settingKey,
                                                    StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Blank means 'string', which is what sp_SaveMultipleSettings does with a
        /// missing dataType and what the column defaults to. Lower-cased because
        /// CK_Settings_DataType and fn_IsValidSettingValue both compare against
        /// lowercase literals.
        /// </summary>
        private static string NormaliseDataType(string? dataType) =>
            string.IsNullOrWhiteSpace(dataType)
                ? Constants.SettingDataTypes.String
                : dataType.Trim().ToLowerInvariant();

        private static ErrorDetail? Validate(string dataType, string? value)
        {
            if (!Constants.SettingDataTypes.All.Contains(dataType))
            {
                return new ErrorDetail(nameof(SettingSaveDTO.DataType),
                    "Data type must be string, number, boolean or json");
            }

            return IsValidValue(dataType, value)
                ? null
                : new ErrorDetail(nameof(SettingSaveDTO.SettingValue),
                    $"Value is not a valid {dataType}");
        }

        /// <summary>
        /// The C# twin of fn_IsValidSettingValue. Each branch was checked against the
        /// server, because guessing at these is how the two ends come to disagree:
        ///
        ///   * number  -- surrounding whitespace and a leading sign are fine; '1e3',
        ///                '1,000' and '$40' are not.
        ///   * boolean -- case-insensitive, and SQL ignores trailing spaces when
        ///                comparing strings, so 'true ' passes there and must here.
        ///                Leading spaces are significant in both.
        ///   * json    -- ISJSON on this server accepts objects and arrays only; a bare
        ///                '5', '"a"' or 'true' is not valid JSON to it, whatever
        ///                System.Text.Json thinks.
        ///
        /// One known divergence is left: a number with more than 29 significant digits
        /// overflows C#'s decimal but not always DECIMAL(38,10). It is caught by the
        /// procedure, which has the last word anyway.
        /// </summary>
        private static bool IsValidValue(string dataType, string? value)
        {
            if (value == null)
                return false;

            return dataType switch
            {
                Constants.SettingDataTypes.String => true,

                Constants.SettingDataTypes.Number =>
                    decimal.TryParse(value, SqlDecimalStyles, CultureInfo.InvariantCulture, out _),

                Constants.SettingDataTypes.Boolean =>
                    value.TrimEnd(' ').ToLowerInvariant() is "true" or "false" or "1" or "0",

                Constants.SettingDataTypes.Json => IsJsonObjectOrArray(value),

                _ => false
            };
        }

        private static bool IsJsonObjectOrArray(string value)
        {
            var start = value.AsSpan().TrimStart();
            if (start.IsEmpty || (start[0] != '{' && start[0] != '['))
                return false;

            try
            {
                using var _ = JsonDocument.Parse(value);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
