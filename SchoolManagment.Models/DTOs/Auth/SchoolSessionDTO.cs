namespace SchoolManagment.Models.DTOs.Auth
{
    /// <summary>
    /// The result of a SuperAdmin entering or leaving a school. Only the access
    /// token is reissued -- the caller keeps the refresh token they already hold.
    ///
    /// That is deliberate, and it has a consequence the client must handle: the
    /// acting school lives in the access token alone, so when that token expires
    /// and the client refreshes, the session drops back to platform scope. A
    /// client that wants to stay inside a school has to remember
    /// <see cref="SchoolId"/> and switch again after a refresh. Persisting the
    /// scope across refreshes would mean storing it on the RefreshTokens row,
    /// which is a schema change; a platform administrator's borrowed tenant
    /// context is also better re-affirmed than left sticky for seven days.
    ///
    /// Separately: holding this token does not make the tenant's own modules
    /// reachable. They are guarded by role, and none of those guards name
    /// SuperAdmin -- see the remarks on POST api/Schools/{schoolId}/switch.
    /// </summary>
    public class SchoolSessionDTO
    {
        /// <summary>Null after leaving a school -- the session is platform-wide
        /// again.</summary>
        public int? SchoolId { get; set; }

        public string? SchoolCode { get; set; }
        public string? SchoolName { get; set; }

        /// <summary>Replaces the access token the client is holding.</summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>Access token lifetime in seconds.</summary>
        public int ExpiresIn { get; set; }

        /// <summary>
        /// True while the token is scoped to a school. Equivalent to
        /// <see cref="SchoolId"/> having a value, but stated so a client does not
        /// have to infer intent from a nullable.
        /// </summary>
        public bool IsActingAsSchool { get; set; }
    }
}
