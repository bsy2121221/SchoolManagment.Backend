using SchoolManagment.Models.DTOs.Students;

namespace SchoolManagment.Models.DTOs.Classes
{
    public class ClassDetailsDTO
    {
        public ClassDTO? ClassInfo { get; set; }
        public List<StudentDTO>? Students { get; set; }
        public ClassStatsDTO? Stats { get; set; }
    }

    /// <summary>
    /// One row from <c>sp_GetClassStats</c>. The property names are the procedure's
    /// column names -- Dapper matches by name, so a mismatch here does not fail, it
    /// silently returns a default. That is how this type previously reported
    /// <c>ActiveStudents = 0</c> and <c>AverageAttendance = 0</c> for every class.
    /// </summary>
    public class ClassStatsDTO
    {
        /// <summary>Active students on the roll. There is no separate inactive count:
        /// the procedure already filters on <c>IsActive = 1</c>.</summary>
        public int TotalStudents { get; set; }

        /// <summary>The class' capacity, for the "31 / 40" the list and details show.</summary>
        public int MaxStudents { get; set; }

        public int MaleStudents { get; set; }
        public int FemaleStudents { get; set; }

        /// <summary>
        /// Percent present across the marks taken in the last 30 days, to two decimals.
        /// <c>null</c> when the register has not been opened in the window at all --
        /// distinct from 0, which means marks were taken and nobody was there.
        /// </summary>
        public decimal? AverageAttendance { get; set; }

        /// <summary>Individual present/absent marks in the window, not student-days.</summary>
        public int PresentLast30Days { get; set; }
        public int AbsentLast30Days { get; set; }

        public int TotalExaminations { get; set; }

        /// <summary>Fee records raised against this class' students.</summary>
        public int TotalFees { get; set; }

        /// <summary>Of those, past due and not yet covered by completed payments.</summary>
        public int OverdueFees { get; set; }
    }
}
