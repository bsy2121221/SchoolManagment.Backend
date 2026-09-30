namespace SchoolManagment.Models.Common
{
    public static class Constants
    {
        public static class Roles
        {
            public const string SuperAdmin = "SuperAdmin";
            public const string Admin = "Admin";
            public const string Teacher = "Teacher";
            public const string Student = "Student";
            public const string Parent = "Parent";
        }

        /// <summary>
        /// dbo.Roles.Id for the five system roles. These numbers are a contract
        /// with the database, not an implementation detail: Roles.Id is not an
        /// identity column, the five rows are seeded by 01_Schema.sql, and
        /// CK_Users_SchoolScope names SuperAdmin as the literal 1 because a
        /// CHECK constraint cannot join to another table. Custom roles start at
        /// 100 (CK_Roles_Id), so nothing here will ever collide with one.
        /// </summary>
        public static class RoleIds
        {
            public const int SuperAdmin = 1;
            public const int Admin = 2;
            public const int Teacher = 3;
            public const int Student = 4;
            public const int Parent = 5;

            /// <summary>Lowest id a user-defined role may take.</summary>
            public const int FirstCustomRole = 100;
        }

        /// <summary>
        /// The permission grid's columns. Must stay in step with the module list
        /// seeded into dbo.RolePermissions in 01_Schema.sql -- a name that is not
        /// in that grid can never be granted, and RequiresPermission on an
        /// unknown module denies everyone.
        /// </summary>
        public static class Modules
        {
            public const string Schools = "Schools";
            public const string Users = "Users";
            public const string Roles = "Roles";
            public const string Students = "Students";
            public const string Teachers = "Teachers";
            public const string Parents = "Parents";
            public const string Classes = "Classes";
            public const string Subjects = "Subjects";
            public const string Attendance = "Attendance";
            public const string Examinations = "Examinations";
            public const string Results = "Results";
            public const string Fees = "Fees";
            public const string Schedule = "Schedule";
            public const string Settings = "Settings";
            public const string Reports = "Reports";

            public static readonly string[] All =
            {
                Schools, Users, Roles, Students, Teachers, Parents, Classes,
                Subjects, Attendance, Examinations, Results, Fees, Schedule,
                Settings, Reports
            };
        }

        /// <summary>The four flags on a RolePermissions row.</summary>
        public static class PermissionActions
        {
            public const string View = "View";
            public const string Create = "Create";
            public const string Edit = "Edit";
            public const string Delete = "Delete";
        }

        /// <summary>CK_Addresses_AddressType allows exactly these.</summary>
        public static class AddressTypes
        {
            public const string Permanent = "Permanent";
            public const string Current = "Current";
            public const string Correspondence = "Correspondence";
        }

        public static class JwtClaims
        {
            public const string UserId = "user_id";
            public const string Username = "username";
            public const string Email = "email";
            public const string Role = "role";

            /// <summary>The numeric dbo.Roles.Id, so authorization does not have
            /// to match role names as strings.</summary>
            public const string RoleId = "role_id";

            /// <summary>One claim per module the role can touch, packed as
            /// "Module:VCED" -- see PermissionClaim.</summary>
            public const string Permission = "perm";

            public const string SchoolId = "school_id";
            public const string SchoolCode = "school_code";
        }

        /// <summary>
        /// Encoding for the "perm" claims. One claim per module, value
        /// "Module:VCED" where each letter is present only if the flag is set --
        /// "Users:VE" means view and edit. Packing all four flags into one claim
        /// keeps the token small; fifteen modules would otherwise cost sixty
        /// claims.
        /// </summary>
        public static class PermissionClaim
        {
            public const char View = 'V';
            public const char Create = 'C';
            public const char Edit = 'E';
            public const char Delete = 'D';
            public const char Separator = ':';
        }

        public static class AuthPolicies
        {
            public const string SuperAdminOnly = "SuperAdminOnly";
            public const string AdminOnly = "AdminOnly";
            public const string TeacherOnly = "TeacherOnly";
            public const string StudentOnly = "StudentOnly";
            public const string AdminOrTeacher = "AdminOrTeacher";
            public const string AllSchoolUsers = "AllSchoolUsers";
        }

        public static class ValidationMessages
        {
            public const string Required = "{0} is required";
            public const string InvalidEmail = "Invalid email format";
            public const string InvalidPassword = "Password must be at least 8 characters with uppercase, lowercase, number, and special character";
            public const string PasswordMismatch = "Passwords do not match";
            public const string InvalidCredentials = "Invalid username or password";
            public const string AccountInactive = "Account is inactive";
            public const string AccountLocked = "Account is locked";
            public const string InvalidToken = "Invalid or expired token";
            public const string Unauthorized = "You are not authorized to perform this action";
        }

        public static class Settings
        {
            public const int DefaultPageSize = 20;
            public const int MaxPageSize = 100;
            public const int AccessTokenExpiryMinutes = 60;
            public const int RefreshTokenExpiryDays = 7;
            public const int MaxLoginAttempts = 5;
            public const int LockoutDurationMinutes = 30;
        }

        public static class Gender
        {
            public const string Male = "Male";
            public const string Female = "Female";
            public const string Other = "Other";
        }

        public static class PaymentMethod
        {
            public const string Cash = "Cash";
            public const string Card = "Card";
            public const string UPI = "UPI";
            public const string BankTransfer = "BankTransfer";
            public const string Cheque = "Cheque";
        }

        public static class PaymentStatus
        {
            public const string Completed = "Completed";
            public const string Pending = "Pending";
            public const string Failed = "Failed";
            public const string Refunded = "Refunded";
        }

        public static class ExamType
        {
            public const string UnitTest = "UnitTest";
            public const string MidTerm = "Mid-Term";
            public const string Final = "Final";
            public const string PreBoard = "Pre-Board";
            public const string Board = "Board";
        }

        /// <summary>CK_Settings_DataType allows exactly these.</summary>
        public static class SettingDataTypes
        {
            public const string String = "string";
            public const string Number = "number";
            public const string Boolean = "boolean";
            public const string Json = "json";

            public static readonly string[] All = { String, Number, Boolean, Json };
        }

        /// <summary>
        /// The categories sp_SeedSchoolSettings writes. Not a constraint -- the
        /// procedures accept any category name -- but the set a settings page builds
        /// its tabs from, and the only names a reset can be asked for on a school
        /// that has never been customised.
        /// </summary>
        public static class SettingCategories
        {
            public const string SystemConfiguration = "SystemConfiguration";
            public const string UserManagement = "UserManagement";
            public const string AcademicSettings = "AcademicSettings";
            public const string SecuritySettings = "SecuritySettings";
            public const string NotificationSettings = "NotificationSettings";
            public const string SystemInformation = "SystemInformation";

            public static readonly string[] All =
            {
                SystemConfiguration, UserManagement, AcademicSettings,
                SecuritySettings, NotificationSettings, SystemInformation
            };
        }
    }
}
