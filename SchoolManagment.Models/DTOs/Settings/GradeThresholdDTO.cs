namespace SchoolManagment.Models.DTOs.Settings
{
    /// <summary>
    /// One band of the school's grading scale, as fn_CalculateGrade applies it: a
    /// mark scores this grade when its percentage is at or above
    /// <see cref="MinPercentage"/> and below the next band up.
    ///
    /// Assembled from the six gradeThreshold* settings in AcademicSettings rather
    /// than stored as a scale, plus a bottom band for F, which has no setting
    /// because it is whatever is left below E.
    /// </summary>
    public class GradeThresholdDTO
    {
        /// <summary>A+, A, B, C, D, E or F -- the values fn_CalculateGrade returns.</summary>
        public string Grade { get; set; } = string.Empty;

        public decimal MinPercentage { get; set; }

        /// <summary>
        /// The setting this band came from, so a client can offer to edit it. Null
        /// for F, which is not configurable.
        /// </summary>
        public string? SettingKey { get; set; }

        /// <summary>
        /// True when the setting was missing or not a number and the band fell back
        /// to the default built into fn_CalculateGrade. Surfaced because a school
        /// seeing an unexpected scale needs to know the difference between a value it
        /// chose and a value nothing set.
        /// </summary>
        public bool IsDefault { get; set; }

        public GradeThresholdDTO()
        {
        }

        public GradeThresholdDTO(string grade, decimal minPercentage, string? settingKey, bool isDefault)
        {
            Grade = grade;
            MinPercentage = minPercentage;
            SettingKey = settingKey;
            IsDefault = isDefault;
        }
    }
}
