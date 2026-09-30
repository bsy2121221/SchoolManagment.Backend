using SchoolManagment.Models.DTOs.Schools;

namespace SchoolManagment.Models.DTOs.Dashboard
{
    /// <summary>
    /// The platform administrator's landing page: the installation as a whole, rather
    /// than any one school.
    ///
    /// Present only for a SuperAdmin with no school in scope. Once they switch into a
    /// school the token carries that school's id and they get the school section
    /// instead, which is the point of switching.
    /// </summary>
    public class PlatformSectionDTO
    {
        public PlatformStatsDTO? Stats { get; set; }

        /// <summary>
        /// The first page of schools, newest first, with each one's head counts --
        /// enough for a "recently added" panel without a second request. Not the whole
        /// estate: a platform with hundreds of schools needs the paginated endpoint,
        /// and <see cref="TotalSchools"/> is how the UI knows there is more.
        /// </summary>
        public List<SchoolListItemDTO> RecentSchools { get; set; } = new();

        /// <summary>
        /// How many schools exist in total, so the UI can decide whether to link on to
        /// the full list rather than inferring it from the length of the sample.
        /// </summary>
        public int TotalSchools { get; set; }
    }
}
