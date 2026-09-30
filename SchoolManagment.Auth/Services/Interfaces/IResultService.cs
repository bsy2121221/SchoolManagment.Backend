using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Results;

namespace SchoolManagment.Auth.Services.Interfaces
{
    public interface IResultService
    {
        Task<ProcResult> AddOrUpdateResultAsync(ResultEntryDTO result);

        Task<(ProcResult Result, BulkGradeEntryResponseDTO Counts)> BulkGradeEntryAsync(
            BulkGradeEntryDTO bulkEntry);

        Task<List<ResultDTO>> GetStudentResultsAsync(int studentId);

        /// <summary>
        /// The class roll for one examination, with any marks already entered.
        ///
        /// <c>Authorised</c> is false only when the caller is a teacher with no assignment for this
        /// subject and class. It used to be impossible to tell that apart from three other
        /// outcomes: the old method returned an empty list for "no school context", "no user id",
        /// "the signed-in user is not a teacher" and "this class is empty" alike, so an
        /// administrator calling it got a 200 and no rows — a page that looked like an empty class
        /// rather than a door that was shut.
        /// </summary>
        Task<(bool Authorised, List<StudentForGradeEntryDTO> Students)> GetStudentsForGradeEntryAsync(
            int subjectId,
            int classId,
            int? examinationId = null
        );
    }
}
