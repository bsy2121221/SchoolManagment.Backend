using System.ComponentModel.DataAnnotations;

namespace SchoolManagment.Models.DTOs.Settings
{
    /// <summary>
    /// Restores seeded defaults. Destructive in a way the other write endpoints are
    /// not: sp_ResetSettings hard-deletes the rows in scope and re-seeds them, so
    /// every customised value in that scope is gone and cannot be recovered from the
    /// table.
    ///
    /// Sent as a body rather than a query value so that omitting the category -- the
    /// form that resets everything -- has to be written out rather than being what
    /// happens when a parameter is forgotten.
    /// </summary>
    public class SettingsResetDTO
    {
        /// <summary>
        /// Null or empty resets every category for the school. Naming one limits the
        /// damage to it; a category with no rows is rejected rather than silently
        /// resetting nothing, so a typo cannot read as success.
        /// </summary>
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string? Category { get; set; }
    }
}
