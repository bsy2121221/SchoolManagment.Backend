using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolManagment.API.Authorization;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Settings;

namespace SchoolManagment.API.Controllers
{
    /// <summary>
    /// The school's own configuration: sixty-four seeded rows across six categories,
    /// keyed by (category, settingKey) within the tenant.
    ///
    /// A setting is addressed by its category and key, never by its numeric id.
    /// That is not a stylistic choice -- sp_ResetSettings deletes the rows and
    /// re-seeds them, so ids do not survive a reset, and every write procedure is
    /// written against the pair anyway.
    ///
    /// Authorisation is the permission grid alone, with no role guard on top. The
    /// seed gives Settings to SuperAdmin and Admin and to nobody else, so that is
    /// already the narrow answer; adding [Authorize(Roles = "Admin")] as well would
    /// lock out a platform administrator who has switched into the school, which is
    /// exactly when they need it. The one exception is the grading scale below, which
    /// is read against the Results module instead.
    ///
    /// Every action is confined to the school named in the caller's token. Nothing
    /// here accepts a school id, so there is no cross-tenant surface to guard.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingService _settingService;

        public SettingsController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        /// <summary>
        /// Every active setting for the school, optionally narrowed to one category.
        ///
        /// Returned flat rather than grouped, and unpaged: sixty-four rows is one
        /// screen's worth, and a settings form wants all of them at once.
        /// </summary>
        /// <param name="category">
        /// One of SystemConfiguration, UserManagement, AcademicSettings,
        /// SecuritySettings, NotificationSettings, SystemInformation -- or any other
        /// category the school has added. Unknown categories return an empty list.
        /// </param>
        [HttpGet]
        [RequiresPermission(Constants.Modules.Settings, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<SettingDTO>>), 200)]
        public async Task<IActionResult> GetSettings([FromQuery] string? category = null)
        {
            var settings = await _settingService.GetSettingsAsync(category);

            return Ok(ApiResponse<List<SettingDTO>>.SuccessResponse(
                settings, "Settings retrieved successfully"));
        }

        /// <summary>
        /// The school's grading scale, as fn_CalculateGrade applies it -- the bands in
        /// the order they are tested, plus F for whatever falls below the last one.
        ///
        /// Guarded on Results:View, not Settings:View, and deliberately: the seeded
        /// grid gives Settings to administrators only, but a teacher marking papers and
        /// a parent reading a report card both need to know what turns 83% into a B.
        /// Everyone who may see a grade may see the scale that produced it.
        ///
        /// isDefault marks a band whose setting is missing or not a number, where the
        /// value shown is the function's own fallback rather than anything the school
        /// chose.
        /// </summary>
        [HttpGet("grades")]
        [RequiresPermission(Constants.Modules.Results, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<List<GradeThresholdDTO>>), 200)]
        public async Task<IActionResult> GetGradeThresholds()
        {
            var thresholds = await _settingService.GetGradeThresholdsAsync();

            return Ok(ApiResponse<List<GradeThresholdDTO>>.SuccessResponse(
                thresholds, "Grade thresholds retrieved successfully"));
        }

        /// <summary>
        /// One setting. 404 covers both "no such key" and "deleted", because the read
        /// procedure filters on IsActive and cannot tell them apart.
        /// </summary>
        [HttpGet("{category}/{settingKey}")]
        [RequiresPermission(Constants.Modules.Settings, Constants.PermissionActions.View)]
        [ProducesResponseType(typeof(ApiResponse<SettingDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> GetSetting(string category, string settingKey)
        {
            var setting = await _settingService.GetSettingAsync(category, settingKey);
            if (setting == null)
            {
                return NotFound(ApiResponse.FailureResult("Setting not found"));
            }

            return Ok(ApiResponse<SettingDTO>.SuccessResponse(setting, "Setting retrieved successfully"));
        }

        /// <summary>
        /// Adds a setting the seed does not define, or brings back one that was
        /// deleted -- the row is soft-deleted, so writing over it revives it, and the
        /// response says which of the two happened.
        ///
        /// 409 when an active setting with that category and key already exists.
        /// sp_SaveSetting would have overwritten it; refusing is what keeps
        /// Settings:Create from doubling as Settings:Edit.
        /// </summary>
        [HttpPost]
        [RequiresPermission(Constants.Modules.Settings, Constants.PermissionActions.Create)]
        [ProducesResponseType(typeof(ApiResponse<SettingDTO>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 409)]
        public async Task<IActionResult> CreateSetting([FromBody] SettingSaveDTO request)
        {
            var (result, message, setting, errors) = await _settingService.CreateSettingAsync(request);

            if (result == SettingResult.Conflict)
            {
                return Conflict(ApiResponse.FailureResult(message));
            }

            if (setting == null)
            {
                return BadRequest(ApiResponse.FailureResult(message, errors));
            }

            var created = result == SettingResult.Created;

            return CreatedAtAction(
                nameof(GetSetting),
                new { category = setting.Category, settingKey = setting.SettingKey },
                ApiResponse<SettingDTO>.SuccessResponse(
                    setting,
                    created ? "Setting created successfully" : "Setting restored successfully")
            );
        }

        /// <summary>
        /// Changes a setting's value, and its type if one is supplied. Omitted fields
        /// keep what is stored, so a form that only knows about values cannot blank out
        /// a description.
        ///
        /// 404 when the setting does not exist: creating one is a POST, which needs
        /// Settings:Create.
        /// </summary>
        [HttpPut("{category}/{settingKey}")]
        [RequiresPermission(Constants.Modules.Settings, Constants.PermissionActions.Edit)]
        [ProducesResponseType(typeof(ApiResponse<SettingDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> UpdateSetting(
            string category, string settingKey, [FromBody] SettingValueUpdateDTO request)
        {
            var (result, message, setting, errors) = await _settingService.UpdateSettingAsync(
                category, settingKey, request);

            if (result == SettingResult.NotFound)
            {
                return NotFound(ApiResponse.FailureResult(message));
            }

            if (setting == null)
            {
                return BadRequest(ApiResponse.FailureResult(message, errors));
            }

            return Ok(ApiResponse<SettingDTO>.SuccessResponse(setting, "Setting updated successfully"));
        }

        /// <summary>
        /// Saves a whole settings form in one transaction.
        ///
        /// A row the procedure cannot store is skipped rather than failing the batch,
        /// so a success here does not mean everything was written: read settingsSaved
        /// and settingsSkipped, and show the skipped list -- it names each dropped key
        /// and says whether the value did not match its type or the key was sent twice.
        /// A bare success toast over this response would hide discarded input.
        /// </summary>
        [HttpPut("bulk")]
        [RequiresPermission(Constants.Modules.Settings, Constants.PermissionActions.Edit)]
        [ProducesResponseType(typeof(ApiResponse<SettingsBulkSaveResultDTO>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> SaveSettings([FromBody] SettingsBulkSaveDTO request)
        {
            var (_, message, saved) = await _settingService.SaveSettingsAsync(request);

            if (saved == null)
            {
                return BadRequest(ApiResponse.FailureResult(message));
            }

            return Ok(ApiResponse<SettingsBulkSaveResultDTO>.SuccessResponse(saved, message));
        }

        /// <summary>
        /// Soft-deletes a setting: it disappears from every read, and a POST of the
        /// same category and key brings it back with its history intact.
        ///
        /// The six gradeThreshold* settings in AcademicSettings are refused, because
        /// fn_CalculateGrade reads them without filtering on IsActive -- a deleted
        /// threshold would vanish from this API while still deciding grades.
        /// </summary>
        [HttpDelete("{category}/{settingKey}")]
        [RequiresPermission(Constants.Modules.Settings, Constants.PermissionActions.Delete)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> DeleteSetting(string category, string settingKey)
        {
            var (result, message) = await _settingService.DeleteSettingAsync(category, settingKey);

            return result switch
            {
                SettingResult.Success => Ok(ApiResponse.SuccessResult(message)),
                SettingResult.NotFound => NotFound(ApiResponse.FailureResult(message)),
                _ => BadRequest(ApiResponse.FailureResult(message))
            };
        }

        /// <summary>
        /// Restores the seeded defaults. Destructive in a way the other endpoints are
        /// not: the rows in scope are hard-deleted and rewritten, so customised values
        /// are gone for good, not deactivated.
        ///
        /// Omitting the category resets all six of them. Naming one that has no rows is
        /// a 404 rather than a silent no-op, so a mistyped category cannot look like a
        /// successful reset.
        /// </summary>
        [HttpPost("reset")]
        [RequiresPermission(Constants.Modules.Settings, Constants.PermissionActions.Delete)]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ResetSettings([FromBody] SettingsResetDTO request)
        {
            var (result, message, restored) = await _settingService.ResetSettingsAsync(request);

            return result switch
            {
                SettingResult.Success => Ok(ApiResponse.SuccessResult(
                    $"{message} ({restored} settings restored)")),
                SettingResult.NotFound => NotFound(ApiResponse.FailureResult(message)),
                _ => BadRequest(ApiResponse.FailureResult(message))
            };
        }
    }
}
