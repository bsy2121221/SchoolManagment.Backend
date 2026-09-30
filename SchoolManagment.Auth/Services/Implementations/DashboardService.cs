using SchoolManagment.Auth.Helpers;
using SchoolManagment.Auth.Services.Interfaces;
using SchoolManagment.DataAccess.Repositories.Interfaces;
using SchoolManagment.Models.Common;
using SchoolManagment.Models.DTOs.Dashboard;
using SchoolManagment.Models.DTOs.Parents;

namespace SchoolManagment.Auth.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        /// <summary>How many audit rows the landing page carries. Ten is the procedure's own default.</summary>
        private const int RecentActivityCount = 10;

        /// <summary>Results shown on a student's card before they have to open their report card.</summary>
        private const int RecentResultCount = 5;

        /// <summary>Schools sampled for the platform card.</summary>
        private const int RecentSchoolsCount = 5;

        /// <summary>
        /// The window a parent's per-child attendance figures cover. Fixed rather than
        /// lifetime because a parent checking in wants to know about this month; a
        /// lifetime percentage barely moves and so says nothing about how term is going.
        /// </summary>
        private const int ChildAttendanceWindowDays = 30;

        private readonly IDashboardRepository _dashboardRepository;
        private readonly ITeacherRepository _teacherRepository;
        private readonly ISchoolService _schoolService;
        private readonly IScheduleService _scheduleService;
        private readonly ITeacherService _teacherService;
        private readonly IResultService _resultService;
        private readonly IFeeService _feeService;
        private readonly IAttendanceService _attendanceService;
        private readonly IParentService _parentService;
        private readonly ITenantContext _tenantContext;

        /// <summary>
        /// Nine dependencies, which is a lot, and is the shape of the problem rather
        /// than a sign of one: a landing page is by definition the union of every
        /// module's headline figure. The alternative -- one dashboard query that joins
        /// all of it -- would duplicate nine modules' worth of filtering rules and drift
        /// from them one fix at a time.
        /// </summary>
        public DashboardService(
            IDashboardRepository dashboardRepository,
            ITeacherRepository teacherRepository,
            ISchoolService schoolService,
            IScheduleService scheduleService,
            ITeacherService teacherService,
            IResultService resultService,
            IFeeService feeService,
            IAttendanceService attendanceService,
            IParentService parentService,
            ITenantContext tenantContext)
        {
            _dashboardRepository = dashboardRepository;
            _teacherRepository = teacherRepository;
            _schoolService = schoolService;
            _scheduleService = scheduleService;
            _teacherService = teacherService;
            _resultService = resultService;
            _feeService = feeService;
            _attendanceService = attendanceService;
            _parentService = parentService;
            _tenantContext = tenantContext;
        }

        public async Task<DashboardDTO> GetDashboardAsync()
        {
            var dashboard = new DashboardDTO
            {
                Role = _tenantContext.Role ?? string.Empty,
                SchoolId = _tenantContext.SchoolId,
                SchoolCode = _tenantContext.SchoolCode,

                // Server local time, to match the procedures: sp_GetDashboardStats
                // decides what "today" means with GETDATE(), so a UTC stamp here would
                // disagree with the counts it labels whenever the two fall on different
                // sides of midnight.
                GeneratedAt = DateTime.Now
            };

            if (!_tenantContext.UserId.HasValue)
            {
                // Unreachable through the controller, which is behind [Authorize]. Kept
                // because the alternative is a null-dereference if it ever is reachable.
                dashboard.Omitted.Add(new OmittedSectionDTO(
                    "*", "The session carries no user id, so nothing could be resolved."));
                return dashboard;
            }

            var userId = _tenantContext.UserId.Value;

            dashboard.RecentActivity = await _dashboardRepository.GetActivitiesAsync(
                _tenantContext.SchoolId, userId, RecentActivityCount);

            if (!_tenantContext.SchoolId.HasValue)
            {
                if (_tenantContext.IsSuperAdmin)
                {
                    dashboard.Platform = await BuildPlatformSectionAsync(dashboard.Omitted);
                }
                else
                {
                    // A non-platform account with no school in its token should not
                    // exist -- every other user row has a SchoolId. Say so rather than
                    // returning an empty page that looks like an empty school.
                    dashboard.Omitted.Add(new OmittedSectionDTO(
                        "school",
                        "This session has no school in scope, which for a non-platform " +
                        "role means the token is inconsistent with the account."));
                }

                return dashboard;
            }

            var schoolId = _tenantContext.SchoolId.Value;

            // Reports:View, not the individual modules the figures come from. A student
            // holds Attendance:View so they can see their own register; reading that as
            // leave to see the school's would publish the whole roll's numbers to every
            // pupil. The seeded grid gives Reports to SuperAdmin, Admin and Teacher,
            // which is exactly the population school-wide aggregates belong to.
            if (_tenantContext.HasPermission(Constants.Modules.Reports, Constants.PermissionActions.View))
            {
                dashboard.School = await BuildSchoolSectionAsync(schoolId, dashboard.Omitted);
            }
            else
            {
                dashboard.Omitted.Add(new OmittedSectionDTO(
                    "school",
                    "School-wide figures need Reports:View, which your role does not carry."));
            }

            switch (dashboard.Role)
            {
                case Constants.Roles.Teacher:
                    dashboard.Teacher = await BuildTeacherSectionAsync(schoolId, userId, dashboard.Omitted);
                    break;

                case Constants.Roles.Student:
                    dashboard.Student = await BuildStudentSectionAsync(schoolId, userId, dashboard.Omitted);
                    break;

                case Constants.Roles.Parent:
                    dashboard.Parent = await BuildParentSectionAsync(userId, dashboard.Omitted);
                    break;
            }

            return dashboard;
        }

        public async Task<SchoolStatsDTO?> GetSchoolStatsAsync()
        {
            if (!_tenantContext.SchoolId.HasValue)
                return null;

            // The omissions are discarded here rather than reported: this endpoint
            // returns the figures alone, and a client that needs to tell "withheld" from
            // "nothing to report" should read the full dashboard, which says which is
            // which. Trimming still happens -- the same method does it -- so the two
            // endpoints can never disagree about what a role may see.
            return await BuildSchoolSectionAsync(_tenantContext.SchoolId.Value, new List<OmittedSectionDTO>());
        }

        public async Task<List<ActivityDTO>> GetMyActivitiesAsync(int topCount)
        {
            if (!_tenantContext.UserId.HasValue)
                return new List<ActivityDTO>();

            return await _dashboardRepository.GetActivitiesAsync(
                _tenantContext.SchoolId, _tenantContext.UserId.Value, topCount);
        }

        /// <summary>
        /// Reads every figure, then removes the ones the caller's grid does not cover.
        /// Blanking after the fact rather than building a narrower query: the procedure
        /// returns one row of scalar subqueries, so asking for a subset would cost the
        /// same and mean maintaining eleven variants of it.
        /// </summary>
        private async Task<SchoolStatsDTO?> BuildSchoolSectionAsync(
            int schoolId, List<OmittedSectionDTO> omitted)
        {
            var stats = await _dashboardRepository.GetSchoolStatsAsync(schoolId);
            if (stats == null)
                return null;

            Trim(Constants.Modules.Students, () => stats.TotalStudents = null, "school.totalStudents");
            Trim(Constants.Modules.Teachers, () => stats.TotalTeachers = null, "school.totalTeachers");
            Trim(Constants.Modules.Parents, () => stats.TotalParents = null, "school.totalParents");
            Trim(Constants.Modules.Classes, () => stats.TotalClasses = null, "school.totalClasses");
            Trim(Constants.Modules.Subjects, () => stats.TotalSubjects = null, "school.totalSubjects");

            if (CanView(Constants.Modules.Attendance))
            {
                var marked = (stats.TodayPresent ?? 0) + (stats.TodayAbsent ?? 0);

                // Of the marks taken, not of the roll. Left null when nothing has been
                // marked today, because 0% would read as a school-wide absence rather
                // than as a register nobody has opened yet.
                stats.TodayAttendancePercentage = marked == 0
                    ? null
                    : Math.Round((stats.TodayPresent ?? 0) * 100m / marked, 2);
            }
            else
            {
                stats.TodayPresent = null;
                stats.TodayAbsent = null;
                omitted.Add(new OmittedSectionDTO(
                    "school.todayPresent, school.todayAbsent, school.todayAttendancePercentage",
                    "Needs Attendance:View."));
            }

            if (!CanView(Constants.Modules.Fees))
            {
                stats.OverdueFees = null;
                stats.FeesOutstandingAmount = null;
                stats.FeesCollectedThisMonth = null;
                omitted.Add(new OmittedSectionDTO(
                    "school.overdueFees, school.feesOutstandingAmount, school.feesCollectedThisMonth",
                    "Needs Fees:View."));
            }

            return stats;

            void Trim(string module, Action clear, string field)
            {
                if (CanView(module))
                    return;

                clear();
                omitted.Add(new OmittedSectionDTO(field, $"Needs {module}:View."));
            }
        }

        private async Task<PlatformSectionDTO?> BuildPlatformSectionAsync(List<OmittedSectionDTO> omitted)
        {
            if (!CanView(Constants.Modules.Schools))
            {
                omitted.Add(new OmittedSectionDTO(
                    "platform", "Platform figures need Schools:View."));
                return null;
            }

            var stats = await _schoolService.GetPlatformStatsAsync();
            var page = await _schoolService.GetSchoolsAsync(pageSize: RecentSchoolsCount);

            return new PlatformSectionDTO
            {
                Stats = stats,
                RecentSchools = page.Items,

                // From the paged read, not from the stats row: TotalCount is what the
                // list endpoint would report, so "showing 5 of 41" cannot disagree with
                // the list the user lands on next.
                TotalSchools = page.TotalCount
            };
        }

        private async Task<TeacherSectionDTO?> BuildTeacherSectionAsync(
            int schoolId, int userId, List<OmittedSectionDTO> omitted)
        {
            var teacherId = await _teacherRepository.GetTeacherIdByUserIdAsync(schoolId, userId);
            if (teacherId == null)
            {
                omitted.Add(new OmittedSectionDTO(
                    "teacher",
                    "This account has the Teacher role but no active teacher record, so " +
                    "there is no timetable to show."));
                return null;
            }

            var section = new TeacherSectionDTO { TeacherId = teacherId.Value };

            if (!CanView(Constants.Modules.Schedule))
            {
                omitted.Add(new OmittedSectionDTO(
                    "teacher.currentClass, teacher.nextClass, teacher.todaySchedule, teacher.scheduleStats",
                    "Needs Schedule:View."));
            }
            else
            {
                var currentAndNext = await _scheduleService.GetTeacherCurrentAndNextClassesAsync(teacherId.Value);
                section.CurrentClass = currentAndNext?.CurrentClass;
                section.NextClass = currentAndNext?.NextClass;

                section.TodaySchedule = await _scheduleService.GetTeacherScheduleByDayAsync(
                    teacherId.Value, IsoDayOfWeek(DateTime.Today));

                section.ScheduleStats = await _scheduleService.GetTeacherScheduleStatsAsync(teacherId.Value);
            }

            if (CanView(Constants.Modules.Classes))
            {
                section.Classes = await _teacherService.GetTeacherClassesAsync(teacherId.Value);
            }
            else
            {
                omitted.Add(new OmittedSectionDTO("teacher.classes", "Needs Classes:View."));
            }

            return section;
        }

        private async Task<StudentSectionDTO?> BuildStudentSectionAsync(
            int schoolId, int userId, List<OmittedSectionDTO> omitted)
        {
            var section = await _dashboardRepository.GetStudentSectionAsync(schoolId, userId);
            if (section == null)
            {
                omitted.Add(new OmittedSectionDTO(
                    "student",
                    "This account has the Student role but no active student record, so " +
                    "there is nothing to report against it."));
                return null;
            }

            if (CanView(Constants.Modules.Attendance))
            {
                // Null rather than 0% when the register has never been taken for this
                // student, which is the ordinary state at the start of a year.
                section.AttendancePercentage = section.TotalDays is null or 0
                    ? null
                    : Math.Round((section.PresentDays ?? 0) * 100m / section.TotalDays.Value, 2);
            }
            else
            {
                section.PresentDays = null;
                section.AbsentDays = null;
                section.TotalDays = null;
                omitted.Add(new OmittedSectionDTO(
                    "student.presentDays, student.absentDays, student.totalDays, student.attendancePercentage",
                    "Needs Attendance:View."));
            }

            if (CanView(Constants.Modules.Results))
            {
                var results = await _resultService.GetStudentResultsAsync(section.StudentId);

                // By exam date, not by entry date: a result typed in late still belongs
                // where the exam sat, which is the order a student reads them in.
                section.RecentResults = results
                    .OrderByDescending(result => result.ExamDate)
                    .Take(RecentResultCount)
                    .ToList();
            }
            else
            {
                section.TotalResults = null;
                section.AverageMarks = null;
                section.HighestMarks = null;
                omitted.Add(new OmittedSectionDTO(
                    "student.totalResults, student.averageMarks, student.highestMarks, student.recentResults",
                    "Needs Results:View."));
            }

            if (!CanView(Constants.Modules.Fees))
            {
                section.TotalFees = null;
                section.PaidAmount = null;
                section.PendingAmount = null;
                omitted.Add(new OmittedSectionDTO(
                    "student.totalFees, student.paidAmount, student.pendingAmount",
                    "Needs Fees:View."));
            }

            if (section.ClassId.HasValue && CanView(Constants.Modules.Schedule))
            {
                section.TodaySchedule = await _scheduleService.GetClassScheduleAsync(
                    section.ClassId.Value, IsoDayOfWeek(DateTime.Today));
            }
            else if (!CanView(Constants.Modules.Schedule))
            {
                omitted.Add(new OmittedSectionDTO("student.todaySchedule", "Needs Schedule:View."));
            }

            return section;
        }

        private async Task<ParentSectionDTO?> BuildParentSectionAsync(
            int userId, List<OmittedSectionDTO> omitted)
        {
            var profile = await _parentService.GetParentProfileAsync(userId);
            if (profile == null)
            {
                omitted.Add(new OmittedSectionDTO(
                    "parent",
                    "This account has the Parent role but no active parent record, so no " +
                    "children could be resolved."));
                return null;
            }

            var section = new ParentSectionDTO { ParentId = profile.Id };

            var canSeeAttendance = CanView(Constants.Modules.Attendance);
            var canSeeFees = CanView(Constants.Modules.Fees);

            if (!canSeeAttendance)
            {
                omitted.Add(new OmittedSectionDTO(
                    "children[].presentDays, children[].absentDays, children[].totalDays, " +
                    "children[].attendancePercentage",
                    "Needs Attendance:View."));
            }

            if (!canSeeFees)
            {
                omitted.Add(new OmittedSectionDTO(
                    "children[].outstandingAmount, children[].overdueFeeCount",
                    "Needs Fees:View."));
            }

            var windowEnd = DateTime.Today;
            var windowStart = windowEnd.AddDays(-ChildAttendanceWindowDays);

            foreach (var child in profile.Children)
            {
                var summary = Describe(child);

                // Read per child rather than through the school-wide summary: that
                // procedure takes a class id at its narrowest, so using it would pull
                // every classmate's figures into memory to pick out one row. These two
                // calls per child are bounded by the number of children linked to the
                // parent, which is small by nature.
                if (canSeeAttendance)
                {
                    var register = await _attendanceService.GetStudentAttendanceAsync(
                        child.StudentId, windowStart, windowEnd);

                    summary.PresentDays = register.Count(day => day.IsPresent);
                    summary.AbsentDays = register.Count(day => !day.IsPresent);
                    summary.TotalDays = register.Count;
                    summary.AttendancePercentage = register.Count == 0
                        ? null
                        : Math.Round(summary.PresentDays.Value * 100m / register.Count, 2);
                }

                if (canSeeFees)
                {
                    var fees = await _feeService.GetStudentFeesAsync(child.StudentId);
                    var feeList = fees.ToList();

                    // Only positive balances: an overpaid fee carries a negative balance
                    // and would otherwise quietly offset what is still owed elsewhere.
                    summary.OutstandingAmount = feeList
                        .Where(fee => fee.Balance > 0)
                        .Sum(fee => fee.Balance);

                    summary.OverdueFeeCount = feeList.Count(fee =>
                        string.Equals(fee.Status, "Overdue", StringComparison.OrdinalIgnoreCase));
                }

                section.Children.Add(summary);
            }

            return section;
        }

        private static ParentChildSummaryDTO Describe(ParentChildDTO child) => new()
        {
            StudentId = child.StudentId,
            StudentNumber = child.StudentNumber,
            FirstName = child.FirstName,
            LastName = child.LastName,
            Relationship = child.Relationship,
            ClassName = child.ClassName,
            Grade = child.Grade,
            Section = child.Section
        };

        private bool CanView(string module) =>
            _tenantContext.HasPermission(module, Constants.PermissionActions.View);

        /// <summary>
        /// The schedule tables number days 1 = Monday .. 7 = Sunday, which is ISO-8601
        /// and is not what <see cref="DateTime.DayOfWeek"/> gives: that enumerates
        /// Sunday = 0 .. Saturday = 6. Getting this wrong is silent -- every weekday
        /// shifts by one and Sunday reads as Saturday's timetable.
        /// </summary>
        private static int IsoDayOfWeek(DateTime date) =>
            date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
    }
}
