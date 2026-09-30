namespace SchoolManagment.Models.Common
{
    /// <summary>
    /// The outcome of a stored procedure that reports itself through a single
    /// <c>Result</c> column: the literal <c>'Success'</c>, or <c>'Error: '</c> followed by
    /// something worth showing a person.
    ///
    /// This type exists because three repositories tested for the wrong word. The
    /// procedures return <c>'Success'</c>, but ClassRepository, StudentRepository and
    /// SubjectRepository asked whether the message contained <c>"successfully"</c> --
    /// which it never does. Twelve write operations across classes, students and subjects
    /// therefore reported failure after doing the work: the row changed and the API said
    /// it had not. TeacherRepository and ExaminationRepository compared against
    /// <c>"Success"</c> and were correct, which is exactly how a convention kept in five
    /// separate places drifts.
    ///
    /// The second thing it fixes is the discarding. Those procedures explain themselves
    /// -- "Cannot delete class with active students", "Capacity cannot be less than the 34
    /// students already enrolled" -- and collapsing that to <c>bool</c> left the API saying
    /// "Failed to delete class" with no hint as to which of three conditions to clear.
    /// </summary>
    /// <param name="Success">True only for an explicit <c>'Success'</c> from the procedure.</param>
    /// <param name="Message">
    /// On failure, the reason with the <c>'Error: '</c> prefix removed, ready to show a
    /// user. Empty on success -- the caller words its own confirmation, because the
    /// procedure does not know whether it was reached by a create or a revival.
    /// </param>
    public sealed record ProcResult(bool Success, string Message)
    {
        /// <summary>What a procedure that returned no row at all is reported as.</summary>
        private const string NoResultMessage =
            "The database reported no outcome for this operation.";

        private const string SuccessToken = "Success";
        private const string ErrorPrefix = "Error: ";

        /// <summary>
        /// Reads the <c>Result</c> column.
        ///
        /// Anything that is not <c>'Success'</c> is a failure, including a null or empty
        /// string. That direction matters: treating an unrecognised value as success would
        /// make a procedure that stopped reporting properly look like one that works.
        /// </summary>
        public static ProcResult From(string? result)
        {
            if (string.IsNullOrWhiteSpace(result))
                return new ProcResult(false, NoResultMessage);

            var trimmed = result.Trim();

            if (trimmed.Equals(SuccessToken, StringComparison.OrdinalIgnoreCase))
                return new ProcResult(true, string.Empty);

            // The prefix is for the log, not the reader: "Error: Cannot delete class with
            // active students" reads worse in a toast than the sentence on its own.
            var message = trimmed.StartsWith(ErrorPrefix, StringComparison.OrdinalIgnoreCase)
                ? trimmed[ErrorPrefix.Length..].Trim()
                : trimmed;

            return new ProcResult(false, message.Length == 0 ? NoResultMessage : message);
        }

        /// <summary>
        /// For a caller that genuinely has nowhere to put the reason -- a background task,
        /// or a delete whose only outcome is "the row is gone either way". Named so that
        /// throwing the message away is a visible decision at the call site rather than
        /// the default.
        /// </summary>
        public bool Succeeded => Success;
    }
}
